// Optimize · hardware-aware mission recommendation engine
// Based on SysManager (MIT) — original license preserved in repository.

using System.IO;
using SysManager.Helpers;
using SysManager.Models;

namespace SysManager.Services;

/// <summary>
/// Converts raw diagnostics into plain-language missions. This service only recommends;
/// execution remains behind audited/reversible services and compatibility gates.
/// </summary>
public sealed class OptimizeMissionService
{
    private readonly SystemInfoService _systemInfo;
    private readonly HardwareProfileService _hardware;
    private readonly TemperatureService _temperatures;

    public OptimizeMissionService(SystemInfoService systemInfo, TemperatureService temperatures)
    {
        _systemInfo = systemInfo;
        _temperatures = temperatures;
        _hardware = new HardwareProfileService(systemInfo);
    }

    public async Task<(HardwareProfile Hardware, IReadOnlyList<OptimizationMission> Missions)> AnalyzeAsync(
        CancellationToken ct = default)
    {
        var profileTask = _hardware.CaptureAsync(false, ct);
        var snapshotTask = _systemInfo.CaptureAsync(ct);
        var temperatureTask = ReadTemperaturesSafeAsync();

        await Task.WhenAll(profileTask, snapshotTask, temperatureTask).ConfigureAwait(false);

        var profile = await profileTask.ConfigureAwait(false);
        var snapshot = await snapshotTask.ConfigureAwait(false);
        var temperatures = await temperatureTask.ConfigureAwait(false);
        List<OptimizationMission> missions = [];

        AddPermissionMission(missions, profile);
        AddThermalMissions(missions, profile, temperatures);
        AddMemoryMissions(missions, profile, snapshot);
        AddStorageMissions(missions, profile);
        AddUptimeMission(missions, profile, snapshot);
        AddGpuMission(missions, profile);
        AddHybridGraphicsMission(missions, profile);
        AddMemoryConfigurationMission(missions, profile);

        return (profile, missions
            .OrderByDescending(m => m.Priority)
            .ThenBy(m => m.Title, StringComparer.CurrentCultureIgnoreCase)
            .ToList());
    }

    private static void AddPermissionMission(List<OptimizationMission> missions, HardwareProfile profile)
    {
        if (AdminHelper.IsElevated()) return;

        missions.Add(new OptimizationMission(
            "diagnostic-admin",
            "Liberar diagnóstico completo",
            "Alguns sensores e verificações profundas do Windows estão limitados nesta execução.",
            "Temperatura da CPU, telemetria completa de GPUs AMD/Intel e várias correções precisam de acesso administrativo para serem avaliadas com segurança.",
            "O Optimize está aberto como usuário padrão.",
            HardwareLabel(profile),
            "Executar como administrador",
            "nav-dashboard",
            MissionPriority.High,
            MissionRisk.None,
            RequiresAdministrator: true,
            CanAutoApply: false));
    }

    private static void AddThermalMissions(
        List<OptimizationMission> missions,
        HardwareProfile profile,
        IReadOnlyList<TemperatureReading> readings)
    {
        foreach (var reading in readings.Where(r => r.TemperatureC.HasValue))
        {
            var temperature = reading.TemperatureC!.Value;
            var component = reading.Component ?? "Componente";
            var isCpu = component.Contains("CPU", StringComparison.OrdinalIgnoreCase);
            var isGpuHotSpot = component.Contains("Hot Spot", StringComparison.OrdinalIgnoreCase) ||
                               component.Contains("Junction", StringComparison.OrdinalIgnoreCase);
            var isGpu = component.Contains("GPU", StringComparison.OrdinalIgnoreCase);

            var critical = (isCpu && temperature >= 95) ||
                           (isGpuHotSpot && temperature >= 105) ||
                           (isGpu && !isGpuHotSpot && temperature >= 90);
            var high = (isCpu && temperature >= 88) ||
                       (isGpuHotSpot && temperature >= 98) ||
                       (isGpu && !isGpuHotSpot && temperature >= 84);

            if (!critical && !high) continue;

            missions.Add(new OptimizationMission(
                $"thermal-{component.ToLowerInvariant().Replace(' ', '-')}",
                critical ? "Temperatura crítica detectada" : "Temperatura elevada detectada",
                $"{reading.SensorName} chegou a {temperature:0} °C.",
                "Temperatura alta pode reduzir clocks automaticamente, causar travamentos e mascarar qualquer ganho de otimização por software.",
                $"Sensor: {reading.SensorName} · {temperature:0.0} °C",
                HardwareLabel(profile),
                "Ver diagnóstico térmico",
                "nav-system-health",
                critical ? MissionPriority.Critical : MissionPriority.High,
                MissionRisk.None,
                RequiresAdministrator: false,
                CanAutoApply: false));
        }
    }

    private static void AddMemoryMissions(
        List<OptimizationMission> missions,
        HardwareProfile profile,
        SystemSnapshot snapshot)
    {
        if (snapshot.Memory.UsedPercent < 82) return;

        var critical = snapshot.Memory.UsedPercent >= 92;
        missions.Add(new OptimizationMission(
            "memory-pressure",
            critical ? "Memória RAM quase esgotada" : "Uso de RAM acima do ideal",
            $"O Windows está usando {snapshot.Memory.UsedPercent:0}% da memória ({snapshot.Memory.UsedGB:0.0} de {snapshot.Memory.TotalGB:0.0} GB).",
            "Quando falta RAM, o Windows passa a usar mais paginação em disco; isso pode causar stutter, lentidão e quedas de FPS mesmo com CPU e GPU fortes.",
            $"{snapshot.Memory.AvailableGB:0.0} GB disponíveis no momento da análise.",
            profile.MemorySummary,
            "Descobrir o que está consumindo RAM",
            "nav-processes",
            critical ? MissionPriority.Critical : MissionPriority.High,
            MissionRisk.None,
            RequiresAdministrator: false,
            CanAutoApply: false));
    }

    private static void AddStorageMissions(List<OptimizationMission> missions, HardwareProfile profile)
    {
        try
        {
            foreach (var drive in DriveInfo.GetDrives().Where(d => d.IsReady && d.DriveType == DriveType.Fixed))
            {
                if (drive.TotalSize <= 0) continue;
                var freePercent = drive.AvailableFreeSpace * 100d / drive.TotalSize;
                if (freePercent >= 15) continue;

                missions.Add(new OptimizationMission(
                    $"storage-space-{drive.Name.TrimEnd('\\').Replace(':', '-')}",
                    freePercent < 8 ? "Unidade quase sem espaço" : "Pouco espaço livre",
                    $"A unidade {drive.Name.TrimEnd('\\')} tem apenas {freePercent:0}% livre.",
                    "Pouco espaço pode afetar atualizações, cache, paginação, instalação de jogos e desempenho geral do Windows.",
                    $"{drive.AvailableFreeSpace / 1024d / 1024d / 1024d:0.0} GB livres de {drive.TotalSize / 1024d / 1024d / 1024d:0.0} GB.",
                    HardwareLabel(profile),
                    "Analisar armazenamento",
                    "nav-disk-analyzer",
                    freePercent < 8 ? MissionPriority.Critical : MissionPriority.High,
                    MissionRisk.None,
                    RequiresAdministrator: false,
                    CanAutoApply: false));
            }
        }
        catch (IOException)
        {
            // Storage mission is optional; the rest of the analysis must still complete.
        }
    }

    private static void AddUptimeMission(
        List<OptimizationMission> missions,
        HardwareProfile profile,
        SystemSnapshot snapshot)
    {
        if (snapshot.Os.Uptime < TimeSpan.FromDays(10)) return;

        missions.Add(new OptimizationMission(
            "long-uptime",
            "Windows está ligado há bastante tempo",
            $"A sessão atual está ativa há {snapshot.Os.Uptime.Days} dias.",
            "Uma reinicialização real pode concluir atualizações pendentes, reiniciar drivers e limpar estados temporários que não desaparecem ao apenas suspender o computador.",
            $"Tempo ligado: {snapshot.Os.Uptime.Days}d {snapshot.Os.Uptime.Hours}h.",
            HardwareLabel(profile),
            "Ver estado do Windows",
            "nav-windows-update",
            MissionPriority.Low,
            MissionRisk.None,
            RequiresAdministrator: false,
            CanAutoApply: false));
    }

    private static void AddGpuMission(List<OptimizationMission> missions, HardwareProfile profile)
    {
        if (profile.Gpus.Count == 0) return;

        var primary = profile.Gpus.FirstOrDefault(g => !g.IsIntegrated) ?? profile.Gpus[0];
        var (title, summary) = primary.Vendor switch
        {
            GpuVendor.Nvidia => ("Perfil NVIDIA detectado", "O Optimize vai usar regras específicas para NVIDIA e nunca aplicar ajustes AMD/Intel nesta GPU."),
            GpuVendor.Amd => ("Perfil AMD Radeon detectado", "O Optimize vai usar regras específicas para AMD e nunca aplicar ajustes NVIDIA/Intel nesta GPU."),
            GpuVendor.Intel => ("Perfil gráfico Intel detectado", "O Optimize vai usar regras específicas para Intel e nunca aplicar ajustes AMD/NVIDIA nesta GPU."),
            _ => ("GPU identificada", "O Optimize reconheceu o adaptador, mas manterá ajustes específicos de fabricante bloqueados até comprovar compatibilidade.")
        };

        missions.Add(new OptimizationMission(
            "gpu-profile",
            title,
            summary,
            "Drivers, energia, frame pacing e recursos gráficos variam por fabricante e geração; por isso o Optimize não usa uma receita única para todas as placas.",
            string.IsNullOrWhiteSpace(primary.DriverVersion)
                ? $"GPU: {primary.Name}"
                : $"GPU: {primary.Name} · driver {primary.DriverVersion}",
            HardwareLabel(profile),
            "Revisar perfil para jogos",
            "nav-gaming-profile",
            MissionPriority.Info,
            MissionRisk.None,
            RequiresAdministrator: false,
            CanAutoApply: false));
    }

    private static void AddHybridGraphicsMission(List<OptimizationMission> missions, HardwareProfile profile)
    {
        if (!profile.HasHybridGraphics) return;

        missions.Add(new OptimizationMission(
            "hybrid-graphics",
            "Mais de uma arquitetura gráfica detectada",
            "Este PC possui GPUs de fabricantes diferentes. Jogos e programas podem acabar usando o adaptador errado.",
            "Em notebooks e sistemas híbridos, escolher a GPU correta por aplicativo pode ser mais importante que vários tweaks genéricos de Windows.",
            string.Join(" · ", profile.Gpus.Select(g => g.Name)),
            HardwareLabel(profile),
            "Revisar GPUs e perfis",
            "nav-gaming-profile",
            MissionPriority.Medium,
            MissionRisk.GuidanceOnly,
            RequiresAdministrator: false,
            CanAutoApply: false));
    }

    private static void AddMemoryConfigurationMission(List<OptimizationMission> missions, HardwareProfile profile)
    {
        if (profile.MemoryModules.Count < 2) return;

        var speeds = profile.MemoryModules.Where(m => m.SpeedMHz > 0).Select(m => m.SpeedMHz).Distinct().ToArray();
        var capacities = profile.MemoryModules.Select(m => Math.Round(m.CapacityGB, 1)).Distinct().ToArray();
        if (speeds.Length <= 1 && capacities.Length <= 1) return;

        missions.Add(new OptimizationMission(
            "mixed-memory",
            "Módulos de memória diferentes detectados",
            "Os módulos instalados não têm a mesma capacidade e/ou frequência informada pelo sistema.",
            "Misturas podem funcionar normalmente, mas também podem limitar frequência, canais de memória ou estabilidade. O Optimize não altera XMP/EXPO automaticamente.",
            string.Join(" · ", profile.MemoryModules.Select(m => $"{m.CapacityGB:0.#} GB @ {m.SpeedMHz} MHz")),
            profile.MemorySummary,
            "Entender a configuração da RAM",
            "nav-system-health",
            MissionPriority.Medium,
            MissionRisk.GuidanceOnly,
            RequiresAdministrator: false,
            CanAutoApply: false));
    }

    private async Task<IReadOnlyList<TemperatureReading>> ReadTemperaturesSafeAsync()
    {
        try
        {
            return await _temperatures.ReadAllAsync(includeStorage: false).ConfigureAwait(false);
        }
        catch
        {
            return [];
        }
    }

    private static string HardwareLabel(HardwareProfile profile)
    {
        var machine = string.Join(" ", new[] { profile.ComputerManufacturer, profile.ComputerModel }
            .Where(s => !string.IsNullOrWhiteSpace(s)));
        if (string.IsNullOrWhiteSpace(machine)) machine = profile.IsLaptop ? "Notebook" : "PC";
        return $"{machine} · {profile.CpuName} · {profile.GpuSummary}";
    }
}
