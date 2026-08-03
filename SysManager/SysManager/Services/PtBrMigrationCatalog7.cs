// Optimize · translations discovered by rendered UI audit
// Original project: laurentiu021/SystemManager · MIT License

namespace SysManager.Services;

public static class PtBrMigrationCatalog7
{
    private static readonly Dictionary<string, string> Exact = new(StringComparer.OrdinalIgnoreCase)
    {
        ["No activity recorded yet"] = "Nenhuma atividade registrada ainda",
    };

    public static bool TryTranslate(string? text, out string translated)
    {
        translated = text ?? string.Empty;
        if (string.IsNullOrWhiteSpace(text)) return false;

        var trimmed = text.Trim();
        if (!Exact.TryGetValue(trimmed, out var value)) return false;

        var leading = text.Length - text.TrimStart().Length;
        var trailing = text.Length - text.TrimEnd().Length;
        translated = new string(' ', leading) + value + new string(' ', trailing);
        return true;
    }
}
