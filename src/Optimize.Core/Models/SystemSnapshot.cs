namespace Optimize.Core.Models;

public sealed class SystemSnapshot
{
    public DateTime CapturedAt { get; init; }

    public string ComputerName { get; init; } = string.Empty;

    public string OperatingSystem { get; init; } = string.Empty;

    public string OperatingSystemVersion { get; init; } = string.Empty;

    public string Processor { get; init; } = string.Empty;

    public string GraphicsAdapter { get; init; } = string.Empty;

    public int LogicalProcessorCount { get; init; }

    public double TotalMemoryGb { get; init; }

    public double AvailableMemoryGb { get; init; }

    public double MemoryUsagePercent { get; init; }

    public TimeSpan Uptime { get; init; }

    public int RunningProcessCount { get; init; }

    public int StartupItemCount { get; init; }

    public bool RestartPending { get; init; }

    public int Score { get; init; }

    public string HealthLabel { get; init; } = string.Empty;

    public IReadOnlyList<DriveSnapshot> Drives { get; init; } = Array.Empty<DriveSnapshot>();

    public IReadOnlyList<Recommendation> Recommendations { get; init; } = Array.Empty<Recommendation>();
}
