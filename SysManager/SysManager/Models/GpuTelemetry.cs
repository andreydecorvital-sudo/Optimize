// Optimize · cross-vendor GPU telemetry
// Original SystemManager project: MIT License

namespace SysManager.Models;

public sealed record GpuTelemetry(
    string Name,
    GpuVendor Vendor,
    double? LoadPercent,
    double? TemperatureC,
    double? MemoryUsedGB,
    double? MemoryTotalGB)
{
    public string MemoryDisplay => MemoryUsedGB.HasValue && MemoryTotalGB.HasValue
        ? $"{MemoryUsedGB:0.0} / {MemoryTotalGB:0.0} GB VRAM"
        : MemoryTotalGB.HasValue
            ? $"{MemoryTotalGB:0.0} GB VRAM"
            : string.Empty;
}
