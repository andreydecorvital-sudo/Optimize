// Optimize · hardware-aware optimization profile
// Based on SysManager (MIT) — original license preserved in repository.

namespace SysManager.Models;

public enum CpuVendor
{
    Unknown,
    Amd,
    Intel,
    Qualcomm,
    Other
}

public enum GpuVendor
{
    Unknown,
    Nvidia,
    Amd,
    Intel,
    Other
}

public sealed record GpuProfile(
    string Name,
    GpuVendor Vendor,
    string DriverVersion,
    double? DedicatedMemoryGB,
    bool IsIntegrated);

/// <summary>
/// Hardware identity used by Optimize recommendation/compatibility rules.
/// Intentionally excludes serial numbers, UUIDs and other device identifiers.
/// </summary>
public sealed record HardwareProfile(
    string ComputerManufacturer,
    string ComputerModel,
    bool IsLaptop,
    string MotherboardManufacturer,
    string MotherboardModel,
    string BiosManufacturer,
    string BiosVersion,
    string CpuName,
    CpuVendor CpuVendor,
    uint CpuCores,
    uint CpuThreads,
    uint CpuMaxClockMHz,
    double MemoryGB,
    IReadOnlyList<MemoryModule> MemoryModules,
    IReadOnlyList<GpuProfile> Gpus,
    IReadOnlyList<DiskInfo> Disks,
    string WindowsName,
    string WindowsBuild,
    string WindowsArchitecture)
{
    public bool HasNvidiaGpu => Gpus.Any(g => g.Vendor == GpuVendor.Nvidia);
    public bool HasAmdGpu => Gpus.Any(g => g.Vendor == GpuVendor.Amd);
    public bool HasIntelGpu => Gpus.Any(g => g.Vendor == GpuVendor.Intel);
    public bool HasHybridGraphics => Gpus.Select(g => g.Vendor).Where(v => v != GpuVendor.Unknown).Distinct().Count() > 1;

    public string GpuSummary => Gpus.Count == 0
        ? "GPU não identificada"
        : string.Join(" + ", Gpus.Select(g => g.Name));

    public string MemorySummary
    {
        get
        {
            var speeds = MemoryModules.Where(m => m.SpeedMHz > 0).Select(m => m.SpeedMHz).Distinct().Order().ToArray();
            var speedText = speeds.Length == 0 ? "frequência não informada" : string.Join("/", speeds) + " MHz";
            return $"{MemoryGB:0.#} GB · {MemoryModules.Count} módulo(s) · {speedText}";
        }
    }
}
