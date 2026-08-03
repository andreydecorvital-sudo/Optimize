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
/// adapter identity as a fallback. A missing sensor is represented as null, never guessed.
/// </summary>
public sealed class GpuTelemetryService : IDisposable
{
    private readonly Lock _gate = new();
    private Computer? _computer;
    private bool _openFailed;
    private bool _disposed;
    private IReadOnlyList<(string Name, GpuVendor Vendor)>? _wmiAdapters;

    public Task<IReadOnlyList<GpuTelemetry>> ReadAsync(CancellationToken ct = default)
        => Task.Run(Read, ct);

    private IReadOnlyList<GpuTelemetry> Read()
    {
        lock (_gate)
        {
            if (_disposed) return [];

            var fallback = _wmiAdapters ??= ReadWmiAdapters();
            var live = ReadLibreHardwareMonitor();
            if (live.Count > 0) return live;

            return fallback.Select(a => new GpuTelemetry(
                a.Name, a.Vendor, null, null, null, null)).ToList();
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
                    MemoryUsedGB: null,
                    MemoryTotalGB: null));
            }
        }
        catch (Exception ex)
        {
            // Some machines/drivers refuse sensor initialization. Do not keep hammering native
            // initialization every polling interval; fall back to WMI identity instead.
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

    private static IReadOnlyList<(string Name, GpuVendor Vendor)> ReadWmiAdapters()
    {
        List<(string Name, GpuVendor Vendor)> result = [];
        try
        {
            using var searcher = new ManagementObjectSearcher("SELECT Name FROM Win32_VideoController");
            using var collection = searcher.Get();
            foreach (ManagementObject mo in collection)
            {
                using (mo)
                {
                    var name = mo["Name"]?.ToString()?.Trim() ?? "GPU desconhecida";
                    result.Add((name, DetectVendor(name)));
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
