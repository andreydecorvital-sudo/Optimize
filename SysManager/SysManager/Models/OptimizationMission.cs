// Optimize · user-facing mission model
// Based on SysManager (MIT) — original license preserved in repository.

namespace SysManager.Models;

public enum MissionPriority
{
    Info,
    Low,
    Medium,
    High,
    Critical
}

public enum MissionRisk
{
    None,
    Low,
    Moderate,
    GuidanceOnly
}

public sealed record OptimizationMission(
    string Id,
    string Title,
    string Summary,
    string WhyItMatters,
    string Evidence,
    string HardwareContext,
    string ActionLabel,
    string TargetNavId,
    MissionPriority Priority,
    MissionRisk Risk,
    bool RequiresAdministrator,
    bool CanAutoApply)
{
    public string PriorityLabel => Priority switch
    {
        MissionPriority.Critical => "Crítica",
        MissionPriority.High => "Alta",
        MissionPriority.Medium => "Média",
        MissionPriority.Low => "Baixa",
        _ => "Informativa"
    };

    public string RiskLabel => Risk switch
    {
        MissionRisk.GuidanceOnly => "Somente orientação",
        MissionRisk.Moderate => "Risco moderado",
        MissionRisk.Low => "Baixo risco",
        _ => "Sem alteração"
    };
}
