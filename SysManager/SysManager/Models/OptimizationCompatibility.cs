// Optimize · compatibility gate result
// Based on SysManager (MIT) — original license preserved in repository.

namespace SysManager.Models;

public enum CompatibilityState
{
    Supported,
    RequiresConfirmation,
    GuidanceOnly,
    Blocked
}

public sealed record OptimizationCompatibility(
    string OptimizationId,
    CompatibilityState State,
    string Reason,
    string HardwareContext)
{
    public bool MayExecute => State is CompatibilityState.Supported or CompatibilityState.RequiresConfirmation;
    public bool MustAskUser => State == CompatibilityState.RequiresConfirmation;
}
