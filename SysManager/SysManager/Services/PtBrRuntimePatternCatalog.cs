// Optimize · pt-BR translations for interpolated runtime strings
// Original project: laurentiu021/SystemManager · MIT License

using System.Text.RegularExpressions;

namespace SysManager.Services;

/// <summary>
/// Translates dynamic messages after their runtime values have already been interpolated.
/// Patterns are deliberately narrow: unknown/vendor/system text is left untouched rather
/// than being rewritten speculatively.
/// </summary>
public static class PtBrRuntimePatternCatalog
{
    private static readonly (Regex Pattern, MatchEvaluator Replace)[] Patterns =
    [
        Rule(@"^Restart recommended — (?<days>\d+) days uptime$", m =>
            $"Reinicialização recomendada — {m.Groups["days"].Value} dias ligado"),
        Rule(@"^(?<n>\d+) network layers are showing trouble\. Check the per-target stats to localize\.$", m =>
            $"{m.Groups["n"].Value} camadas da rede apresentam problemas. Consulte os dados de cada destino para localizar a causa."),
        Rule(@"^(?<disk>.+) health degraded — consider backup$", m =>
            $"Saúde de {m.Groups["disk"].Value} degradada — considere fazer backup"),
        Rule(@"^Upload measurement failed \(HTTP (?<code>\d+)\)$", m =>
            $"Falha ao medir upload (HTTP {m.Groups["code"].Value})"),
        Rule(@"^Loaded (?<count>\d+) currently installed applications\.$", m =>
            $"{m.Groups["count"].Value} aplicativos instalados foram carregados."),
        Rule(@"^(?<count>\d+) applications found$", m =>
            $"{m.Groups["count"].Value} aplicativos encontrados"),
        Rule(@"^Failed to block (?<name>.+) — check admin privileges\.$", m =>
            $"Falha ao bloquear {m.Groups["name"].Value} — verifique os privilégios de administrador."),
        Rule(@"^Unblocked (?<count>\d+) applications?\.?$", m =>
            $"{m.Groups["count"].Value} aplicativo(s) desbloqueado(s)."),
        Rule(@"^Saved preset ""(?<name>.+)"" with (?<count>\d+) apps?\.?$", m =>
            $"Predefinição \"{m.Groups["name"].Value}\" salva com {m.Groups["count"].Value} aplicativo(s)."),
        Rule(@"^(?<name>.+) · (?<charge>\d+)% · Health (?<health>\d+)% · (?<status>.+)$", m =>
            $"{m.Groups["name"].Value} · {m.Groups["charge"].Value}% · Saúde {m.Groups["health"].Value}% · {m.Groups["status"].Value}"),
        Rule(@"^Removed (?<count>\d+) file\(s\)\. Re-scanning…$", m =>
            $"{m.Groups["count"].Value} arquivo(s) removido(s). Analisando novamente…"),
        Rule(@"^(?<count>\d+) files removed\.$", m =>
            $"{m.Groups["count"].Value} arquivos removidos."),
        Rule(@"^Installing (?<name>.+) \((?<current>\d+)/(?<total>\d+)\)…$", m =>
            $"Instalando {m.Groups["name"].Value} ({m.Groups["current"].Value}/{m.Groups["total"].Value})…"),
        Rule(@"^Failed \(exit (?<code>-?\d+)\)$", m =>
            $"Falha (código de saída {m.Groups["code"].Value})"),
        Rule(@"^Cannot start — (?<operation>.+) is already running\.$", m =>
            $"Não é possível iniciar — {m.Groups["operation"].Value} já está em execução."),
        Rule(@"^(?<tool>.+) finished with exit code (?<code>-?\d+)\. Check the console output for details\.$", m =>
            $"{m.Groups["tool"].Value} terminou com código de saída {m.Groups["code"].Value}. Consulte a saída do console para detalhes."),
        Rule(@"^→ Go to (?<tab>.+) for more details$", m =>
            $"→ Ir para {m.Groups["tab"].Value} para mais detalhes"),
        Rule(@"^Found (?<count>\d+) files ≥ (?<size>[\d.,]+) MB in (?<location>.+)\.$", m =>
            $"Encontrados {m.Groups["count"].Value} arquivos ≥ {m.Groups["size"].Value} MB em {m.Groups["location"].Value}."),
        Rule(@"^(?<count>\d+) files found ≥ (?<size>[\d.,]+) MB$", m =>
            $"{m.Groups["count"].Value} arquivos encontrados ≥ {m.Groups["size"].Value} MB"),
        Rule(@"^Keep these settings\? Reverting in (?<seconds>\d+)s…$", m =>
            $"Manter estas configurações? Revertendo em {m.Groups["seconds"].Value}s…"),
        Rule(@"^Complete — (?<ok>\d+) shredded, (?<failed>\d+) failed\.$", m =>
            $"Concluído — {m.Groups["ok"].Value} excluído(s) com segurança, {m.Groups["failed"].Value} falha(s)."),
        Rule(@"^Loaded (?<count>\d+) events from (?<log>.+)$", m =>
            $"{m.Groups["count"].Value} eventos carregados de {m.Groups["log"].Value}"),
        Rule(@"^Loaded (?<count>\d+) processes\.$", m =>
            $"{m.Groups["count"].Value} processos carregados."),
        Rule(@"^(?<count>\d+) processes · (?<memory>.+) total memory$", m =>
            $"{m.Groups["count"].Value} processos · {m.Groups["memory"].Value} de memória total"),
        Rule(@"^Keeping (?<days>\d+) days of history\.$", m =>
            $"Mantendo {m.Groups["days"].Value} dias de histórico."),
        Rule(@"^Loaded (?<total>\d+) services \((?<running>\d+) running\)\.$", m =>
            $"{m.Groups["total"].Value} serviços carregados ({m.Groups["running"].Value} em execução)."),
        Rule(@"^(?<total>\d+) services \((?<running>\d+) running\)$", m =>
            $"{m.Groups["total"].Value} serviços ({m.Groups["running"].Value} em execução)"),
        Rule(@"^Start service ""(?<name>.+)""\?$", m =>
            $"Iniciar o serviço \"{m.Groups["name"].Value}\"?"),
        Rule(@"^Stop service ""(?<name>.+)""\?\s+This may affect system functionality\.$", m =>
            $"Parar o serviço \"{m.Groups["name"].Value}\"? Isso pode afetar funcionalidades do sistema."),
        Rule(@"^Disable service ""(?<name>.+)""\?\s+This prevents the service from starting automatically\.$", m =>
            $"Desativar o serviço \"{m.Groups["name"].Value}\"? Isso impede que ele inicie automaticamente."),
        Rule(@"^✓ (?<name>.+) set to Disabled\.$", m =>
            $"✓ {m.Groups["name"].Value} definido como Desativado."),
        Rule(@"^(?<enabled>\d+) enabled · (?<disabled>\d+) disabled · (?<total>\d+) total$", m =>
            $"{m.Groups["enabled"].Value} ativado(s) · {m.Groups["disabled"].Value} desativado(s) · {m.Groups["total"].Value} total"),
        Rule(@"^(?<disks>\d+) disks, (?<modules>\d+) RAM modules$", m =>
            $"{m.Groups["disks"].Value} disco(s), {m.Groups["modules"].Value} módulo(s) de RAM"),
        Rule(@"^Scanning (?<count>\d+) drive\(s\)\.\.\.$", m =>
            $"Analisando {m.Groups["count"].Value} unidade(s)..."),
        Rule(@"^Running chkdsk (?<drive>.+) \(read-only\)\.\.\.$", m =>
            $"Executando chkdsk {m.Groups["drive"].Value} (somente leitura)..."),
        Rule(@"^Couldn't change ""(?<name>.+)"" — this usually needs administrator rights\.$", m =>
            $"Não foi possível alterar \"{m.Groups["name"].Value}\" — normalmente isso exige direitos de administrador."),
        Rule(@"^Found (?<count>\d+) installed applications\.?$", m =>
            $"{m.Groups["count"].Value} aplicativos instalados encontrados."),
        Rule(@"^Uninstalling (?<name>.+) \((?<current>\d+)/(?<total>\d+)\)\.\.\.$", m =>
            $"Desinstalando {m.Groups["name"].Value} ({m.Groups["current"].Value}/{m.Groups["total"].Value})..."),
        Rule(@"^Found (?<count>\d+) features \((?<enabled>\d+) enabled\)\.$", m =>
            $"{m.Groups["count"].Value} recursos encontrados ({m.Groups["enabled"].Value} ativados)."),
        Rule(@"^(?<count>\d+) updates found$", m =>
            $"{m.Groups["count"].Value} atualizações encontradas"),
        Rule(@"^(?<count>\d+) history entries\.?$", m =>
            $"{m.Groups["count"].Value} entradas no histórico"),
        Rule(@"^Installing (?<count>\d+) update\(s\) \(do not reboot\)…$", m =>
            $"Instalando {m.Groups["count"].Value} atualização(ões) (não reinicie)…")
    ];

    public static bool TryTranslate(string? text, out string translated)
    {
        translated = text ?? string.Empty;
        if (string.IsNullOrWhiteSpace(text)) return false;

        foreach (var (pattern, replace) in Patterns)
        {
            var match = pattern.Match(text);
            if (!match.Success) continue;
            translated = replace(match);
            return true;
        }
        return false;
    }

    private static (Regex Pattern, MatchEvaluator Replace) Rule(string pattern, MatchEvaluator replace)
        => (new Regex(pattern, RegexOptions.Compiled | RegexOptions.CultureInvariant), replace);
}
