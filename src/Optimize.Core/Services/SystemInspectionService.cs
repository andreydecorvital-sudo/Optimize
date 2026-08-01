using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Management;
using Microsoft.Win32;
using Optimize.Core.Models;

namespace Optimize.Core.Services;

public sealed class SystemInspectionService : ISystemInspectionService
{
    public Task<SystemSnapshot> InspectAsync(CancellationToken cancellationToken = default)
    {
        return Task.Run(() => Inspect(cancellationToken), cancellationToken);
    }

    private static SystemSnapshot Inspect(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        OperatingSystemData os = ReadOperatingSystemData();
        string processor = ReadFirstWmiValue("SELECT Name FROM Win32_Processor", "Name");
        string graphics = ReadAllWmiValues("SELECT Name FROM Win32_VideoController", "Name");
        IReadOnlyList<DriveSnapshot> drives = ReadDrives();
        int startupItemCount = CountStartupItems();
        int runningProcessCount = CountRunningProcesses();
        bool restartPending = IsRestartPending();
        TimeSpan uptime = TimeSpan.FromMilliseconds(Environment.TickCount64);

        cancellationToken.ThrowIfCancellationRequested();

        double memoryUsage = os.TotalMemoryGb <= 0
            ? 0
            : Math.Clamp(((os.TotalMemoryGb - os.AvailableMemoryGb) / os.TotalMemoryGb) * 100, 0, 100);

        List<Recommendation> recommendations = BuildRecommendations(
            drives,
            startupItemCount,
            memoryUsage,
            restartPending,
            uptime);

        int score = Math.Clamp(100 - recommendations.Sum(item => item.ScoreImpact), 0, 100);

        return new SystemSnapshot
        {
            CapturedAt = DateTime.Now,
            ComputerName = Environment.MachineName,
            OperatingSystem = os.Caption,
            OperatingSystemVersion = $"{os.Version} (build {os.BuildNumber})",
            Processor = processor,
            GraphicsAdapter = graphics,
            LogicalProcessorCount = Environment.ProcessorCount,
            TotalMemoryGb = Math.Round(os.TotalMemoryGb, 1),
            AvailableMemoryGb = Math.Round(os.AvailableMemoryGb, 1),
            MemoryUsagePercent = Math.Round(memoryUsage, 0),
            Uptime = uptime,
            RunningProcessCount = runningProcessCount,
            StartupItemCount = startupItemCount,
            RestartPending = restartPending,
            Score = score,
            HealthLabel = GetHealthLabel(score),
            Drives = drives,
            Recommendations = recommendations
        };
    }

    private static OperatingSystemData ReadOperatingSystemData()
    {
        using var searcher = new ManagementObjectSearcher(
            "SELECT Caption, Version, BuildNumber, TotalVisibleMemorySize, FreePhysicalMemory FROM Win32_OperatingSystem");
        using ManagementObjectCollection results = searcher.Get();

        foreach (ManagementObject item in results)
        {
            using (item)
            {
                double totalKb = ToDouble(item["TotalVisibleMemorySize"]);
                double freeKb = ToDouble(item["FreePhysicalMemory"]);

                return new OperatingSystemData(
                    ReadText(item["Caption"], "Windows"),
                    ReadText(item["Version"], "desconhecida"),
                    ReadText(item["BuildNumber"], "desconhecido"),
                    totalKb / 1024d / 1024d,
                    freeKb / 1024d / 1024d);
            }
        }

        return new OperatingSystemData(
            Environment.OSVersion.VersionString,
            Environment.OSVersion.Version.ToString(),
            "desconhecido",
            0,
            0);
    }

    private static string ReadFirstWmiValue(string query, string propertyName)
    {
        using var searcher = new ManagementObjectSearcher(query);
        using ManagementObjectCollection results = searcher.Get();

        foreach (ManagementObject item in results)
        {
            using (item)
            {
                return ReadText(item[propertyName], "Não identificado");
            }
        }

        return "Não identificado";
    }

    private static string ReadAllWmiValues(string query, string propertyName)
    {
        var values = new List<string>();
        using var searcher = new ManagementObjectSearcher(query);
        using ManagementObjectCollection results = searcher.Get();

        foreach (ManagementObject item in results)
        {
            using (item)
            {
                string value = ReadText(item[propertyName], string.Empty);
                if (!string.IsNullOrWhiteSpace(value))
                {
                    values.Add(value);
                }
            }
        }

        return values.Count == 0
            ? "Não identificada"
            : string.Join(" · ", values.Distinct(StringComparer.OrdinalIgnoreCase));
    }

    private static IReadOnlyList<DriveSnapshot> ReadDrives()
    {
        string systemDrive = Path.GetPathRoot(Environment.SystemDirectory) ?? "C:\\";
        var drives = new List<DriveSnapshot>();

        foreach (DriveInfo drive in DriveInfo.GetDrives())
        {
            try
            {
                if (!drive.IsReady || drive.DriveType is not (DriveType.Fixed or DriveType.Removable))
                {
                    continue;
                }

                double totalGb = drive.TotalSize / 1024d / 1024d / 1024d;
                double freeGb = drive.AvailableFreeSpace / 1024d / 1024d / 1024d;
                double freePercent = totalGb <= 0 ? 0 : (freeGb / totalGb) * 100;

                drives.Add(new DriveSnapshot(
                    drive.Name,
                    string.IsNullOrWhiteSpace(drive.VolumeLabel) ? "Disco local" : drive.VolumeLabel,
                    drive.DriveFormat,
                    Math.Round(totalGb, 1),
                    Math.Round(freeGb, 1),
                    Math.Round(freePercent, 0),
                    string.Equals(drive.Name, systemDrive, StringComparison.OrdinalIgnoreCase)));
            }
            catch (IOException)
            {
                // A unidade pode ficar indisponível entre a enumeração e a leitura.
            }
            catch (UnauthorizedAccessException)
            {
                // A inspeção segue mesmo quando uma unidade não permite leitura.
            }
        }

        return drives;
    }

    private static int CountStartupItems()
    {
        const string runPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
        var locations = new (RegistryHive Hive, RegistryView View)[]
        {
            (RegistryHive.LocalMachine, RegistryView.Registry64),
            (RegistryHive.LocalMachine, RegistryView.Registry32),
            (RegistryHive.CurrentUser, RegistryView.Registry64),
            (RegistryHive.CurrentUser, RegistryView.Registry32)
        };

        int count = 0;
        foreach ((RegistryHive hive, RegistryView view) in locations)
        {
            try
            {
                using RegistryKey baseKey = RegistryKey.OpenBaseKey(hive, view);
                using RegistryKey? key = baseKey.OpenSubKey(runPath, writable: false);
                count += key?.ValueCount ?? 0;
            }
            catch (Security.SecurityException)
            {
                // A ausência de permissão não deve impedir o restante do diagnóstico.
            }
            catch (UnauthorizedAccessException)
            {
                // A ausência de permissão não deve impedir o restante do diagnóstico.
            }
        }

        return count;
    }

    private static int CountRunningProcesses()
    {
        Process[] processes = Process.GetProcesses();
        try
        {
            return processes.Length;
        }
        finally
        {
            foreach (Process process in processes)
            {
                process.Dispose();
            }
        }
    }

    private static bool IsRestartPending()
    {
        string[] keys =
        {
            @"SOFTWARE\Microsoft\Windows\CurrentVersion\Component Based Servicing\RebootPending",
            @"SOFTWARE\Microsoft\Windows\CurrentVersion\WindowsUpdate\Auto Update\RebootRequired"
        };

        using RegistryKey localMachine = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64);
        foreach (string path in keys)
        {
            using RegistryKey? key = localMachine.OpenSubKey(path, writable: false);
            if (key is not null)
            {
                return true;
            }
        }

        using RegistryKey? sessionManager = localMachine.OpenSubKey(
            @"SYSTEM\CurrentControlSet\Control\Session Manager",
            writable: false);

        return sessionManager?.GetValue("PendingFileRenameOperations") is not null;
    }

    private static List<Recommendation> BuildRecommendations(
        IReadOnlyList<DriveSnapshot> drives,
        int startupItemCount,
        double memoryUsagePercent,
        bool restartPending,
        TimeSpan uptime)
    {
        var recommendations = new List<Recommendation>();
        DriveSnapshot? systemDrive = drives.FirstOrDefault(drive => drive.IsSystemDrive);

        if (systemDrive is not null && systemDrive.FreePercent < 12)
        {
            recommendations.Add(new Recommendation(
                "Pouco espaço no disco do Windows",
                $"A unidade {systemDrive.Name} possui apenas {systemDrive.FreePercent:0}% livre. Isso pode afetar atualizações, memória virtual e desempenho.",
                RecommendationSeverity.Critical,
                20));
        }
        else if (systemDrive is not null && systemDrive.FreePercent < 22)
        {
            recommendations.Add(new Recommendation(
                "Espaço do sistema abaixo do ideal",
                $"A unidade {systemDrive.Name} está com {systemDrive.FreePercent:0}% livre. Vale revisar arquivos temporários e aplicativos grandes.",
                RecommendationSeverity.Warning,
                10));
        }

        if (memoryUsagePercent >= 90)
        {
            recommendations.Add(new Recommendation(
                "Uso de memória muito alto",
                $"A memória está com aproximadamente {memoryUsagePercent:0}% de uso durante a inspeção.",
                RecommendationSeverity.Critical,
                18));
        }
        else if (memoryUsagePercent >= 80)
        {
            recommendations.Add(new Recommendation(
                "Uso de memória elevado",
                $"A memória está com aproximadamente {memoryUsagePercent:0}% de uso. Vamos identificar os processos responsáveis.",
                RecommendationSeverity.Warning,
                9));
        }

        if (startupItemCount > 20)
        {
            recommendations.Add(new Recommendation(
                "Muitos programas iniciando com o Windows",
                $"Foram detectadas {startupItemCount} entradas básicas de inicialização. Elas serão revisadas individualmente antes de qualquer alteração.",
                RecommendationSeverity.Warning,
                12));
        }
        else if (startupItemCount > 10)
        {
            recommendations.Add(new Recommendation(
                "Inicialização pode ser simplificada",
                $"Foram detectadas {startupItemCount} entradas básicas de inicialização.",
                RecommendationSeverity.Information,
                5));
        }

        if (restartPending)
        {
            recommendations.Add(new Recommendation(
                "Reinicialização pendente",
                "O Windows possui alterações pendentes. Reiniciar pode concluir atualizações e reparos já aplicados.",
                RecommendationSeverity.Information,
                3));
        }

        if (uptime.TotalDays >= 14)
        {
            recommendations.Add(new Recommendation(
                "Windows ligado há muitos dias",
                $"O sistema está sem reinicialização completa há cerca de {uptime.TotalDays:0} dias.",
                RecommendationSeverity.Information,
                3));
        }

        if (recommendations.Count == 0)
        {
            recommendations.Add(new Recommendation(
                "Nenhum gargalo básico detectado",
                "O primeiro diagnóstico não encontrou problemas evidentes. As próximas versões analisarão temperaturas, drivers, eventos e desempenho em tempo real.",
                RecommendationSeverity.Information,
                0));
        }

        return recommendations;
    }

    private static string GetHealthLabel(int score)
    {
        return score switch
        {
            >= 90 => "Excelente",
            >= 75 => "Bom",
            >= 55 => "Atenção",
            _ => "Crítico"
        };
    }

    private static string ReadText(object? value, string fallback)
    {
        string? text = Convert.ToString(value, CultureInfo.InvariantCulture)?.Trim();
        return string.IsNullOrWhiteSpace(text) ? fallback : text;
    }

    private static double ToDouble(object? value)
    {
        return value is null ? 0 : Convert.ToDouble(value, CultureInfo.InvariantCulture);
    }

    private sealed record OperatingSystemData(
        string Caption,
        string Version,
        string BuildNumber,
        double TotalMemoryGb,
        double AvailableMemoryGb);
}
