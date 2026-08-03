// Optimize · mission experience + cross-vendor telemetry for Dashboard
// Based on SysManager (MIT) — original license preserved in repository.

using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Serilog;
using SysManager.Models;
using SysManager.Services;

namespace SysManager.ViewModels;

public sealed partial class DashboardViewModel
{
    private readonly ObservableCollection<OptimizationMission> _optimizeMissions = new();
    private readonly GpuTelemetryService _optimizeGpuTelemetry = new();
    private readonly CancellationTokenSource _optimizeTelemetryCts = new();
    private int _missionLoadStarted;
    private int _telemetryStarted;

    [ObservableProperty] private bool _isOptimizeMissionsLoading;
    [ObservableProperty] private string _optimizeHardwareSummary = "Identificando seu hardware...";
    [ObservableProperty] private string _optimizeMissionStatus = "Preparando recomendações específicas para este PC...";

    /// <summary>
    /// Binding to this property starts the first mission analysis lazily, so the original
    /// Dashboard startup remains fast and no additional WMI/sensor work runs unless the panel exists.
    /// </summary>
    public ObservableCollection<OptimizationMission> OptimizeMissions
    {
        get
        {
            EnsureOptimizeMissionsStarted();
            return _optimizeMissions;
        }
    }

    private void EnsureOptimizeMissionsStarted()
    {
        if (Interlocked.Exchange(ref _missionLoadStarted, 1) != 0) return;
        StartCrossVendorGpuTelemetry();
        _ = RefreshOptimizeMissionsAsync();
    }

    private void StartCrossVendorGpuTelemetry()
    {
        if (Interlocked.Exchange(ref _telemetryStarted, 1) != 0) return;
        var ct = _optimizeTelemetryCts.Token;

        _ = Task.Run(async () =>
        {
            while (!ct.IsCancellationRequested)
            {
                try
                {
                    if (!IsActive)
                    {
                        await Task.Delay(1000, ct).ConfigureAwait(false);
                        continue;
                    }

                    var gpus = await _optimizeGpuTelemetry.ReadAsync(ct).ConfigureAwait(false);
                    var primary = gpus
                        .OrderBy(g => g.Vendor == GpuVendor.Unknown || g.Vendor == GpuVendor.Other)
                        .ThenBy(g => g.Name.Contains("Microsoft", StringComparison.OrdinalIgnoreCase))
                        .ThenByDescending(g => g.MemoryTotalGB ?? 0)
                        .FirstOrDefault();

                    if (primary is not null)
                    {
                        System.Windows.Application.Current?.Dispatcher.BeginInvoke(() =>
                        {
                            GpuName = primary.Name;
                            if (primary.LoadPercent.HasValue)
                                GpuPercent = Math.Clamp(primary.LoadPercent.Value, 0, 100);
                            if (!string.IsNullOrWhiteSpace(primary.MemoryDisplay))
                                GpuVram = primary.MemoryDisplay;
                        });
                    }

                    await Task.Delay(750, ct).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    Log.Debug(ex, "Optimize GPU telemetry polling failed");
                    await Task.Delay(2000, ct).ConfigureAwait(false);
                }
            }
        }, ct);
    }

    [RelayCommand]
    private async Task RefreshOptimizeMissionsAsync()
    {
        if (IsOptimizeMissionsLoading) return;
        IsOptimizeMissionsLoading = true;
        OptimizeMissionStatus = "Analisando hardware, temperaturas e estado do Windows...";

        try
        {
            var service = new OptimizeMissionService(_sys, _temps);
            var result = await service.AnalyzeAsync().ConfigureAwait(true);

            OptimizeHardwareSummary = BuildHardwareSummary(result.Hardware);
            _optimizeMissions.Clear();
            foreach (var mission in result.Missions)
                _optimizeMissions.Add(mission);

            OptimizeMissionStatus = result.Missions.Count switch
            {
                0 => "Nenhuma missão importante encontrada agora.",
                1 => "1 missão recomendada para este PC.",
                _ => $"{result.Missions.Count} missões recomendadas para este PC."
            };
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Optimize mission analysis failed");
            OptimizeMissionStatus = "Não foi possível concluir as missões. Tente atualizar a análise.";
        }
        finally
        {
            IsOptimizeMissionsLoading = false;
        }
    }

    private static string BuildHardwareSummary(HardwareProfile hardware)
    {
        var form = hardware.IsLaptop ? "Notebook" : "PC";
        return $"{form} · {hardware.CpuName} · {hardware.GpuSummary} · {hardware.MemoryGB:0.#} GB RAM";
    }
}
