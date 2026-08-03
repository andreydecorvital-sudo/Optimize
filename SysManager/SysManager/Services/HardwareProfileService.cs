// Optimize · hardware capability detection
// Based on SysManager (MIT) — original license preserved in repository.

using System.Management;
using Serilog;
using SysManager.Models;

namespace SysManager.Services;

/// <summary>
/// Builds the hardware context every recommendation must be evaluated against.
/// No serial number, device UUID or other unique identifier is collected.
/// </summary>
public sealed class HardwareProfileService
{
    private readonly SystemInfoService _systemInfo;
    private HardwareProfile? _cached;
    private readonly SemaphoreSlim _gate = new(1, 1);

    public HardwareProfileService(SystemInfoService systemInfo)
    {
        _systemInfo = systemInfo;
    }

    public async Task<HardwareProfile> CaptureAsync(bool forceRefresh = false, CancellationToken ct = default)
    {
        if (!forceRefresh && _cached is not null) return _cached;

        await _gate.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            if (!forceRefresh && _cached is not null) return _cached;

            var snapshot = await _systemInfo.CaptureAsync(ct).ConfigureAwait(false);
            var computer = QueryComputerSystem();
            var board = QueryBaseBoard();
            var bios = QueryBios();
            var gpus = QueryGpus();
            var laptop = computer.IsLaptop || QueryHasBattery();

            _cached = new HardwareProfile(
                computer.Manufacturer,
                computer.Model,
                laptop,
                board.Manufacturer,
                board.Model,
                bios.Manufacturer,
                bios.Version,
                snapshot.Cpu.Name,
                DetectCpuVendor(snapshot.Cpu.Name),
                snapshot.Cpu.Cores,
                snapshot.Cpu.LogicalProcessors,
                snapshot.Cpu.MaxClockMHz,
                snapshot.Memory.TotalGB,
                snapshot.Memory.Modules,
                gpus,
                snapshot.Disks,
                snapshot.Os.Caption,
                snapshot.Os.BuildNumber,
                snapshot.Os.Architecture);

            return _cached;
        }
        finally
        {
            _gate.Release();
        }
    }

    private static CpuVendor DetectCpuVendor(string name)
    {
        if (name.Contains("AMD", StringComparison.OrdinalIgnoreCase) ||
            name.Contains("Ryzen", StringComparison.OrdinalIgnoreCase)) return CpuVendor.Amd;
        if (name.Contains("Intel", StringComparison.OrdinalIgnoreCase) ||
            name.Contains("Core(TM)", StringComparison.OrdinalIgnoreCase)) return CpuVendor.Intel;
        if (name.Contains("Qualcomm", StringComparison.OrdinalIgnoreCase) ||
            name.Contains("Snapdragon", StringComparison.OrdinalIgnoreCase)) return CpuVendor.Qualcomm;
        return string.IsNullOrWhiteSpace(name) || name.Contains("Unknown", StringComparison.OrdinalIgnoreCase)
            ? CpuVendor.Unknown
            : CpuVendor.Other;
    }

    private static GpuVendor DetectGpuVendor(string name)
    {
        if (name.Contains("NVIDIA", StringComparison.OrdinalIgnoreCase) ||
            name.Contains("GeForce", StringComparison.OrdinalIgnoreCase) ||
            name.Contains("Quadro", StringComparison.OrdinalIgnoreCase)) return GpuVendor.Nvidia;
        if (name.Contains("AMD", StringComparison.OrdinalIgnoreCase) ||
            name.Contains("Radeon", StringComparison.OrdinalIgnoreCase) ||
            name.Contains("ATI", StringComparison.OrdinalIgnoreCase)) return GpuVendor.Amd;
        if (name.Contains("Intel", StringComparison.OrdinalIgnoreCase) ||
            name.Contains("Arc", StringComparison.OrdinalIgnoreCase) ||
            name.Contains("Iris", StringComparison.OrdinalIgnoreCase) ||
            name.Contains("UHD", StringComparison.OrdinalIgnoreCase)) return GpuVendor.Intel;
        return string.IsNullOrWhiteSpace(name) ? GpuVendor.Unknown : GpuVendor.Other;
    }

    private static List<GpuProfile> QueryGpus()
    {
        List<GpuProfile> result = [];
        try
        {
            using var searcher = new ManagementObjectSearcher(
                "SELECT Name,DriverVersion,AdapterRAM FROM Win32_VideoController");
            using var collection = searcher.Get();
            foreach (ManagementObject mo in collection)
            {
                using (mo)
                {
                    var name = mo["Name"]?.ToString()?.Trim() ?? "GPU desconhecida";
                    var driver = mo["DriverVersion"]?.ToString()?.Trim() ?? "";
                    double? memoryGb = null;
                    try
                    {
                        var bytes = Convert.ToUInt64(mo["AdapterRAM"] ?? 0UL);
                        if (bytes > 0) memoryGb = Math.Round(bytes / 1024d / 1024d / 1024d, 1);
                    }
                    catch (Exception ex) when (ex is FormatException or InvalidCastException or OverflowException)
                    {
                        memoryGb = null;
                    }

                    var vendor = DetectGpuVendor(name);
                    // Used only as UX context; compatibility decisions never depend on this heuristic.
                    var integrated = vendor == GpuVendor.Intel ||
                        name.Contains("Radeon Graphics", StringComparison.OrdinalIgnoreCase) ||
                        name.Contains("Vega", StringComparison.OrdinalIgnoreCase);

                    result.Add(new GpuProfile(name, vendor, driver, memoryGb, integrated));
                }
            }
        }
        catch (Exception ex) when (ex is ManagementException or System.Runtime.InteropServices.COMException)
        {
            Log.Debug("Optimize GPU inventory unavailable: {Error}", ex.Message);
        }
        return result;
    }

    private static (string Manufacturer, string Model, bool IsLaptop) QueryComputerSystem()
    {
        try
        {
            using var searcher = new ManagementObjectSearcher(
                "SELECT Manufacturer,Model,PCSystemType FROM Win32_ComputerSystem");
            using var collection = searcher.Get();
            foreach (ManagementObject mo in collection)
            {
                using (mo)
                {
                    var manufacturer = mo["Manufacturer"]?.ToString()?.Trim() ?? "";
                    var model = mo["Model"]?.ToString()?.Trim() ?? "";
                    var type = Convert.ToUInt16(mo["PCSystemType"] ?? 0);
                    return (manufacturer, model, type == 2);
                }
            }
        }
        catch (Exception ex) when (ex is ManagementException or System.Runtime.InteropServices.COMException or FormatException or InvalidCastException)
        {
            Log.Debug("Optimize computer model inventory unavailable: {Error}", ex.Message);
        }
        return ("", "", false);
    }

    private static (string Manufacturer, string Model) QueryBaseBoard()
    {
        try
        {
            using var searcher = new ManagementObjectSearcher("SELECT Manufacturer,Product FROM Win32_BaseBoard");
            using var collection = searcher.Get();
            foreach (ManagementObject mo in collection)
            {
                using (mo)
                {
                    return (
                        mo["Manufacturer"]?.ToString()?.Trim() ?? "",
                        mo["Product"]?.ToString()?.Trim() ?? "");
                }
            }
        }
        catch (Exception ex) when (ex is ManagementException or System.Runtime.InteropServices.COMException)
        {
            Log.Debug("Optimize motherboard inventory unavailable: {Error}", ex.Message);
        }
        return ("", "");
    }

    private static (string Manufacturer, string Version) QueryBios()
    {
        try
        {
            using var searcher = new ManagementObjectSearcher("SELECT Manufacturer,SMBIOSBIOSVersion FROM Win32_BIOS");
            using var collection = searcher.Get();
            foreach (ManagementObject mo in collection)
            {
                using (mo)
                {
                    return (
                        mo["Manufacturer"]?.ToString()?.Trim() ?? "",
                        mo["SMBIOSBIOSVersion"]?.ToString()?.Trim() ?? "");
                }
            }
        }
        catch (Exception ex) when (ex is ManagementException or System.Runtime.InteropServices.COMException)
        {
            Log.Debug("Optimize BIOS inventory unavailable: {Error}", ex.Message);
        }
        return ("", "");
    }

    private static bool QueryHasBattery()
    {
        try
        {
            using var searcher = new ManagementObjectSearcher("SELECT DeviceID FROM Win32_Battery");
            using var collection = searcher.Get();
            return collection.Count > 0;
        }
        catch (Exception ex) when (ex is ManagementException or System.Runtime.InteropServices.COMException)
        {
            Log.Debug("Optimize battery detection unavailable: {Error}", ex.Message);
            return false;
        }
    }
}
