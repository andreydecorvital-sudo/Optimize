// Optimize · mission experience for Dashboard
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
    private int _missionLoadStarted;

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
        _ = RefreshOptimizeMissionsAsync();
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
