namespace Optimize.Core.Models;

public sealed record DriveSnapshot(
    string Name,
    string Label,
    string FileSystem,
    double TotalGb,
    double FreeGb,
    double FreePercent,
    bool IsSystemDrive);
