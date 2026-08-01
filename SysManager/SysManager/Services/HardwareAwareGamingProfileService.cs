// Optimize · hardware-aware gate around the upstream reversible GamingProfileService
// Original SystemManager project is MIT-licensed; LICENSE is preserved in repository.

using SysManager.Models;

namespace SysManager.Services;

/// <summary>
/// Mandatory preflight for Gaming Profile. The upstream engine still owns snapshots/revert,
/// while this layer removes any requested step that has not passed Optimize compatibility policy.
/// </summary>
public sealed class HardwareAwareGamingProfileService : IGamingProfileService
{
    private readonly GamingProfileService _inner;
    private readonly HardwareProfileService _hardware;
    private readonly OptimizationCompatibilityService _compatibility;
    private readonly PowerContextService _power;

    public HardwareAwareGamingProfileService(
        GamingProfileService inner,
        HardwareProfileService hardware,
        OptimizationCompatibilityService compatibility,
        PowerContextService power)
    {
        _inner = inner;
        _hardware = hardware;
        _compatibility = compatibility;
        _power = power;
    }

    public bool IsActive => _inner.IsActive;
    public int? BoundGamePid => _inner.BoundGamePid;
    public bool HasPendingRecovery => _inner.HasPendingRecovery;

    public event EventHandler? SessionAutoReverted
    {
        add => _inner.SessionAutoReverted += value;
        remove => _inner.SessionAutoReverted -= value;
    }

    public async Task<GamingApplyResult> ApplyAsync(
        GamingProfile requested,
        GameTarget? game,
        CancellationToken ct = default)
    {
        var hardware = await _hardware.CaptureAsync(false, ct).ConfigureAwait(false);
        var onBattery = _power.IsRunningOnBattery();
        List<GamingStepOutcome> preflight = [];

        bool Allow(bool requestedValue, string optimizationId, string label)
        {
            if (!requestedValue) return false;
            var result = _compatibility.Evaluate(optimizationId, hardware, onBattery);
            if (result.MayExecute) return true;

            preflight.Add(new GamingStepOutcome(
                label,
                GamingStepStatus.SkippedNoChange,
                $"Bloqueado pelo Optimize: {result.Reason}"));
            return false;
        }

        var effective = requested with
        {
            UltimatePerformancePlan = Allow(
                requested.UltimatePerformancePlan,
                OptimizationCompatibilityService.UltimatePerformance,
                "Plano de energia de alto desempenho"),

            DisableVisualEffects = Allow(
                requested.DisableVisualEffects,
                OptimizationCompatibilityService.VisualEffects,
                "Reduzir efeitos visuais"),

            FinestTimerResolution = Allow(
                requested.FinestTimerResolution,
                OptimizationCompatibilityService.TimerResolution,
                "Resolução fina do temporizador"),

            HighGameCpuPriority = Allow(
                requested.HighGameCpuPriority && game is not null,
                OptimizationCompatibilityService.CpuPriority,
                "Prioridade de CPU do jogo"),

            PinGameToPerformanceCores = Allow(
                requested.PinGameToPerformanceCores && game is not null,
                OptimizationCompatibilityService.CpuAffinity,
                "Afinidade dos núcleos da CPU"),

            PurgeStandbyMemory = Allow(
                requested.PurgeStandbyMemory,
                OptimizationCompatibilityService.StandbyMemoryPurge,
                "Liberar memória em espera"),

            PauseSearchIndexing = Allow(
                requested.PauseSearchIndexing,
                OptimizationCompatibilityService.PauseSearchIndexing,
                "Pausar indexação do Windows"),

            SilenceNotifications = Allow(
                requested.SilenceNotifications,
                OptimizationCompatibilityService.SilenceNotifications,
                "Silenciar notificações")
        };

        if (!effective.HasAnyEnabled)
            return new GamingApplyResult(preflight, RestorePointCreated: false);

        var applied = await _inner.ApplyAsync(effective, game, ct).ConfigureAwait(false);
        if (preflight.Count == 0) return applied;

        return new GamingApplyResult(preflight.Concat(applied.Steps).ToList(), applied.RestorePointCreated);
    }

    public Task RevertAsync(CancellationToken ct = default) => _inner.RevertAsync(ct);
    public GamingProfile LoadLastConfig() => _inner.LoadLastConfig();
    public void SaveLastConfig(GamingProfile profile) => _inner.SaveLastConfig(profile);
    public Task RecoverPendingAsync(CancellationToken ct = default) => _inner.RecoverPendingAsync(ct);
}
