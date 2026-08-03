// Optimize · live pt-BR surface audit
// Original project: laurentiu021/SystemManager · MIT License

using System.Collections.Concurrent;
using System.IO;
using System.Text.RegularExpressions;

namespace SysManager.Services;

/// <summary>
/// Records strings that still look English after the runtime translator has processed the
/// actual WPF control. This catches text that source-only audits miss: templates, popups,
/// bound values, enum labels and controls created after startup.
/// </summary>
internal static partial class PtBrLiveAuditService
{
    private static readonly Lock FileGate = new();
    private static readonly ConcurrentDictionary<string, byte> Seen = new(StringComparer.Ordinal);
    private static int _started;

    private static readonly HashSet<string> EnglishHints = new(StringComparer.OrdinalIgnoreCase)
    {
        "about", "active", "advanced", "administrator", "all", "and", "apply",
        "available", "back", "battery", "cancel", "check", "choose", "cleanup",
        "close", "completed", "configuration", "confirm", "create", "current",
        "delete", "details", "disable", "disabled", "download", "drive", "driver",
        "empty", "enable", "enabled", "error", "export", "failed", "file", "files",
        "folder", "from", "health", "history", "import", "installed", "loading",
        "memory", "mode", "monitor", "network", "next", "no", "open", "performance",
        "privacy", "process", "processes", "profile", "quick", "ready", "recommended",
        "refresh", "remove", "repair", "requires", "restore", "running", "save",
        "scan", "search", "security", "select", "selected", "service", "services",
        "settings", "start", "status", "stop", "successful", "system", "the", "this",
        "update", "updates", "warning", "with", "yes", "your"
    };

    private static readonly HashSet<string> PortugueseHints = new(StringComparer.OrdinalIgnoreCase)
    {
        "abrir", "administrador", "análise", "analisar", "aplicar", "arquivo", "arquivos",
        "atualização", "atualizações", "atualizar", "ativado", "ativar", "bateria",
        "cancelar", "carregando", "concluído", "configuração", "configurações", "confirmar",
        "criar", "desativado", "desativar", "detalhes", "disponível", "erro", "excluir",
        "exportar", "falha", "fechar", "memória", "modo", "monitor", "não", "pasta",
        "perfil", "processo", "processos", "recomendado", "rede", "remover", "reparar",
        "restaurar", "salvar", "saúde", "segurança", "selecionar", "serviço", "serviços",
        "sistema", "status", "verificação", "verificar"
    };

    private static readonly HashSet<string> TechnicalExact = new(StringComparer.OrdinalIgnoreCase)
    {
        "AMD", "BIOS", "CLI", "CPU", "CSV", "Defender", "DISM", "DNS", "Docker",
        "Edge", "EXPO", "FPS", "GB", "GHz", "GPU", "HDD", "HTTP", "HTTPS", "Intel",
        "IPv4", "IPv6", "KB", "MB", "MHz", "NVIDIA", "NVMe", "OneDrive", "PATH",
        "PowerShell", "RAM", "Resizable BAR", "SAM", "SATA", "SFC", "SMART", "SSD",
        "TEMP", "TCP", "UDP", "UEFI", "USB", "VRAM", "WebView2", "WinGet", "Windows",
        "Windows Update", "Winget", "WSL", "XML", "XMP"
    };

    private static string LogPath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "Optimize",
        "ptbr-live-audit.log");

    public static void StartSession()
    {
        if (Interlocked.Exchange(ref _started, 1) != 0) return;

        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(LogPath)!);
            File.WriteAllText(
                LogPath,
                $"Optimize · auditoria da interface em execução · {DateTime.Now:yyyy-MM-dd HH:mm:ss}{Environment.NewLine}" +
                "Strings abaixo permaneceram com aparência de inglês após a tradução em tempo real." + Environment.NewLine +
                "============================================================" + Environment.NewLine);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // Audit logging must never prevent the application from opening.
        }
    }

    public static void Inspect(string controlType, string propertyName, string text)
    {
        if (!LooksEnglish(text)) return;

        var normalized = WhitespaceRegex().Replace(text, " ").Trim();
        if (normalized.Length > 500) normalized = normalized[..500] + "…";

        var key = $"{controlType}.{propertyName}|{normalized}";
        if (!Seen.TryAdd(key, 0)) return;

        try
        {
            lock (FileGate)
            {
                File.AppendAllText(
                    LogPath,
                    $"[{DateTime.Now:HH:mm:ss}] {controlType}.{propertyName}: {normalized}{Environment.NewLine}");
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // Logging is diagnostic only.
        }
    }

    private static bool LooksEnglish(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return false;
        var trimmed = text.Trim();
        if (TechnicalExact.Contains(trimmed)) return false;
        if (trimmed.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
            || trimmed.StartsWith("https://", StringComparison.OrdinalIgnoreCase)
            || trimmed.StartsWith("file://", StringComparison.OrdinalIgnoreCase)
            || trimmed.StartsWith("pack://", StringComparison.OrdinalIgnoreCase))
            return false;
        if (trimmed.Contains('\\') || trimmed.Contains("::", StringComparison.Ordinal))
            return false;

        var words = WordRegex().Matches(trimmed).Cast<Match>().Select(match => match.Value).ToArray();
        if (words.Length == 0) return false;

        var english = words.Count(EnglishHints.Contains);
        var portuguese = words.Count(PortugueseHints.Contains);
        return english > 0 && english >= portuguese;
    }

    [GeneratedRegex(@"\s+")]
    private static partial Regex WhitespaceRegex();

    [GeneratedRegex(@"[A-Za-zÀ-ÿ]+")]
    private static partial Regex WordRegex();
}
