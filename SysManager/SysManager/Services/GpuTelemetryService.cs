// Optimize · cross-vendor GPU telemetry
// Original SystemManager project: MIT License

using System.Management;
using LibreHardwareMonitor.Hardware;
using Serilog;
using SysManager.Models;

namespace SysManager.Services;

/// <summary>
/// Reads live GPU telemetry without assuming a specific vendor or model.
/// LibreHardwareMonitor is the primary source for NVIDIA, AMD and Intel GPUs; WMI supplies
/// adapter identity/VRAM as a fallback. A missing sensor is represented as null, never guessed.
/// </summary>
public sealed class GpuTelemetryService : IDisposable
{
    private readonly Lock _gate = new();
    private Computer? _computer;
    private bool _openFailed;
    private bool _disposed;
    private IReadOnlyList<(string Name, GpuVendor Vendor, double? MemoryGB)>? _wmiAdapters;

    public Task<IReadOnlyList<GpuTelemetry>> ReadAsync(CancellationToken ct = default)
        => Task.Run(Read, ct);

    private IReadOnlyList<GpuTelemetry> Read()
    {
        lock (_gate)
        {
            if (_disposed) return [];

            var fallback = _wmiAdapters ??= ReadWmiAdapters();
            var live = ReadLibreHardwareMonitor();
            if (live.Count == 0)
            {
                return fallback.Select(a => new GpuTelemetry(
                    a.Name, a.Vendor, null, null, null, a.MemoryGB)).ToList();
            }

            // Enrich LHM entries with the closest WMI adapter's static VRAM value when possible.
            return live.Select(g =>
            {
                var wmi = fallback.FirstOrDefault(a => NamesLikelyMatch(a.Name, g.Name) || a.Vendor == g.Vendor);
                return g with { MemoryTotalGB = g.MemoryTotalGB ?? wmi.MemoryGB };
            }).ToList();
        }
    }

    private List<GpuTelemetry> ReadLibreHardwareMonitor()
    {
        List<GpuTelemetry> result = [];
        if (_openFailed) return result;

        try
        {
            _computer ??= OpenComputer();
            foreach (var hardware in _computer.Hardware)
            {
                if (hardware.HardwareType is not (HardwareType.GpuNvidia or HardwareType.GpuAmd or HardwareType.GpuIntel))
                    continue;

                hardware.Update();
                foreach (var sub in hardware.SubHardware)
                    sub.Update();

                var sensors = hardware.Sensors.Concat(hardware.SubHardware.SelectMany(s => s.Sensors)).ToList();

                var load = sensors
                    .Where(s => s.SensorType == SensorType.Load && s.Value.HasValue)
                    .OrderByDescending(s => IsGpuCoreName(s.Name))
                    .ThenByDescending(s => s.Value)
                    .FirstOrDefault();

                var temp = sensors
                    .Where(s => s.SensorType == SensorType.Temperature && s.Value.HasValue && s.Value > 0)
                    .OrderByDescending(s => IsGpuCoreName(s.Name))
                    .FirstOrDefault();

                // LHM exposes memory differently between GPU backends/driver generations.
                // Only consume values when their sensor name clearly states Used/Total;
                // unknown units are ignored rather than converted speculatively.
                double? usedGb = null;
                double? totalGb = null;
                foreach (var sensor in sensors.Where(s => s.Value.HasValue))
                {
                    if (sensor.SensorType is not (SensorType.Data or SensorType.SmallData)) continue;
                    var name = sensor.Name;
                    if (!name.Contains("Memory", StringComparison.OrdinalIgnoreCase) &&
                        !name.Contains("VRAM", StringComparison.OrdinalIgnoreCase)) continue;

                    // Data sensors in LibreHardwareMonitor use GB for these GPU-memory metrics.
                    if (name.Contains("Used", StringComparison.OrdinalIgnoreCase)) usedGb = sensor.Value;
                    if (name.Contains("Total", StringComparison.OrdinalIgnoreCase)) totalGb = sensor.Value;
                }

                result.Add(new GpuTelemetry(
                    hardware.Name,
                    hardware.HardwareType switch
                    {
                        HardwareType.GpuNvidia => GpuVendor.Nvidia,
                        HardwareType.GpuAmd => GpuVendor.Amd,
                        HardwareType.GpuIntel => GpuVendor.Intel,
                        _ => GpuVendor.Unknown
                    },
                    load?.Value,
                    temp?.Value,
                    usedGb,
                    totalGb));
            }
        }
        catch (Exception ex)
        {
            // Some machines/drivers refuse sensor initialization without elevation. Do not keep
            // hammering native initialization every 300 ms; fall back to WMI identity instead.
            _openFailed = true;
            Log.Debug("Optimize cross-vendor GPU telemetry unavailable: {Error}", ex.Message);
            try { _computer?.Close(); } catch { /* best effort */ }
            _computer = null;
        }

        return result;
    }

    private static Computer OpenComputer()
    {
        var computer = new Computer
        {
            IsGpuEnabled = true
        };
        computer.Open();
        return computer;
    }

    private static bool IsGpuCoreName(string name)
        => name.Contains("GPU Core", StringComparison.OrdinalIgnoreCase) ||
           name.Equals("GPU", StringComparison.OrdinalIgnoreCase) ||
           name.Contains("Core", StringComparison.OrdinalIgnoreCase);

    private static IReadOnlyList<(string Name, GpuVendor Vendor, double? MemoryGB)> ReadWmiAdapters()
    {
        List<(string Name, GpuVendor Vendor, double? MemoryGB)> result = [];
        try
        {
            using var searcher = new ManagementObjectSearcher(
                "SELECT Name,AdapterRAM FROM Win32_VideoController");
            using var collection = searcher.Get();
            foreach (ManagementObject mo in collection)
            {
                using (mo)
                {
                    var name = mo["Name"]?.ToString()?.Trim() ?? "GPU desconhecida";
                    double? memory = null;
                    try
                    {
                        var bytes = Convert.ToUInt64(mo["AdapterRAM"] ?? 0UL);
                        if (bytes > 0) memory = Math.Round(bytes / 1024d / 1024d / 1024d, 1);
                    }
                    catch (Exception ex) when (ex is FormatException or InvalidCastException or OverflowException)
                    {
                        memory = null;
                    }
                    result.Add((name, DetectVendor(name), memory));
                }
            }
        }
        catch (Exception ex) when (ex is ManagementException or System.Runtime.InteropServices.COMException)
        {
            Log.Debug("Optimize WMI GPU fallback unavailable: {Error}", ex.Message);
        }
        return result;
    }

    private static GpuVendor DetectVendor(string name)
    {
        if (name.Contains("NVIDIA", StringComparison.OrdinalIgnoreCase) || name.Contains("GeForce", StringComparison.OrdinalIgnoreCase))
            return GpuVendor.Nvidia;
        if (name.Contains("AMD", StringComparison.OrdinalIgnoreCase) || name.Contains("Radeon", StringComparison.OrdinalIgnoreCase))
            return GpuVendor.Amd;
        if (name.Contains("Intel", StringComparison.OrdinalIgnoreCase) || name.Contains("Arc", StringComparison.OrdinalIgnoreCase) ||
            name.Contains("Iris", StringComparison.OrdinalIgnoreCase) || name.Contains("UHD", StringComparison.OrdinalIgnoreCase))
            return GpuVendor.Intel;
        return GpuVendor.Other;
    }

    private static bool NamesLikelyMatch(string a, string b)
    {
        static string Normalize(string value) => new(value
            .Where(char.IsLetterOrDigit)
            .Select(char.ToUpperInvariant)
            .ToArray());

        var na = Normalize(a);
        var nb = Normalize(b);
        return na.Contains(nb, StringComparison.Ordinal) || nb.Contains(na, StringComparison.Ordinal);
    }

    public void Dispose()
    {
        lock (_gate)
        {
            if (_disposed) return;
            _disposed = true;
            try { _computer?.Close(); }
            catch (Exception ex) { Log.Debug(ex, "Optimize GPU telemetry close failed"); }
            _computer = null;
        }
    }
}
