// Optimize · pt-BR translations for interpolated runtime strings
// Original project: laurentiu021/SystemManager · MIT License

using System.Text.RegularExpressions;

namespace SysManager.Services;

/// <summary>
/// Handles messages whose final text contains runtime values, where an exact source-string
/// dictionary cannot match after interpolation. Patterns are intentionally narrow to avoid
/// rewriting arbitrary system/vendor text.
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

        Rule(@"^Could not enable the required privilege \((?<error>.+)\)\.$", m =>
            $"Não foi possível habilitar o privilégio necessário ({m.Groups["error"].Value})."),

        Rule(@"^Failed \(install code (?<code>.+)\)$", m =>
            $"Falha (código de instalação {m.Groups["code"].Value})"),

        Rule(@"^You're up to date\. Running v(?<version>.+)\.$", m =>
            $"Você está na versão mais recente. Executando v{m.Groups["version"].Value}."),

        Rule(@"^(?<error>.+)\. Click Retry to try again\.$", m =>
            $"{m.Groups["error"].Value}. Clique em Tentar novamente."),

        Rule(@"^(?<error>.+)\. This usually means a network issue or firewall blocking the connection\. Try 'Manual download' as fallback\.$", m =>
            $"{m.Groups["error"].Value}. Isso normalmente indica problema de rede ou bloqueio pelo firewall. Tente 'Download manual' como alternativa."),

        Rule(@"^Loaded (?<count>\d+) currently installed applications\.$", m =>
            $"{m.Groups["count"].Value} aplicativos instalados foram carregados."),

        Rule(@"^(?<count>\d+) applications found$", m =>
            $"{m.Groups["count"].Value} aplicativos encontrados"),

        Rule(@"^Failed to block (?<name>.+) — check admin privileges\.$", m =>
            $"Falha ao bloquear {m.Groups["name"].Value} — verifique os privilégios de administrador."),

        Rule(@"^Unblocked (?<count>\d+) application(?<plural>s?)\.?$", m =>
            $"{m.Groups["count"].Value} aplicativo(s) desbloqueado(s)."),

        Rule(@"^Saved preset \"(?<name>.+)\" with (?<count>\d+) app(?<plural>s?)\.?$", m =>
            $"Predefinição \"{m.Groups["name"].Value}\" salva com {m.Groups["count"].Value} aplicativo(s)."),

        Rule(@"^(?<name>.+) · (?<charge>\d+)% · Health (?<health>\d+)% · (?<status>.+)$", m =>
            $"{m.Groups["name"].Value} · {m.Groups["charge"].Value}% · Saúde {m.Groups["health"].Value}% · {m.Groups["status"].Value}"),

        Rule(@"^requires elevation · (?<status>.+)$", m =>
            $"requer elevação · {m.Groups["status"].Value}"),

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

        Rule(@"^Scan finished with exit code (?<code>-?\d+)\. Check the console output for details\.$", m =>
            $"A análise terminou com código de saída {m.Groups["code"].Value}. Consulte a saída do console para detalhes."),

        Rule(@"^DISM finished with exit code (?<code>-?\d+)\. Check the console output for details\.$", m =>
            $"O DISM terminou com código de saída {m.Groups["code"].Value}. Consulte a saída do console para detalhes."),

        Rule(@"^Could not toggle (?<name>.+) — protected by Windows \(owned by TrustedInstaller\)\.$", m =>
            $"Não foi possível alterar {m.Groups["name"].Value} — protegido pelo Windows (propriedade do TrustedInstaller)."),

        Rule(@"^Pinned (?<name>.+) to (?<count>\d+) core\(s\)\. Reverts when the process exits\.$", m =>
            $"{m.Groups["name"].Value} fixado em {m.Groups["count"].Value} núcleo(s). A alteração é revertida quando o processo encerra."),

        Rule(@"^→ Go to (?<tab>.+) for more details$", m =>
            $"→ Ir para {m.Groups["tab"].Value} para mais detalhes"),

        Rule(@"^Selected (?<count>\d+) commonly-removed app(?<plural>s?)\. Review, then Remove selected\.$", m =>
            $"{m.Groups["count"].Value} aplicativo(s) comum(ns) selecionado(s). Revise e depois use Remover selecionados."),

        Rule(@"^\[(?<current>\d+)/(?<total>\d+)\]\s+(?<category>.+)$", m =>
            $"[{m.Groups["current"].Value}/{m.Groups["total"].Value}]  {m.Groups["category"].Value}"),

        Rule(@"^Found (?<count>\d+) files ≥ (?<size>[\d.,]+) MB in (?<location>.+)\.$", m =>
            $"Encontrados {m.Groups["count"].Value} arquivos ≥ {m.Groups["size"].Value} MB em {m.Groups["location"].Value}."),

        Rule(@"^(?<count>\d+) files found ≥ (?<size>[\d.,]+) MB$", m =>
            $"{m.Groups["count"].Value} arquivos encontrados ≥ {m.Groups["size"].Value} MB"),

        Rule(@"^Keep these settings\? Reverting in (?<seconds>\d+)s…$", m =>
            $"Manter estas configurações? Revertendo em {m.Groups["seconds"].Value}s…"),

        Rule(@"^Added (?<scope>.+) variable '(?<name>.+)' — press Apply to save\.$", m =>
            $"Variável {m.Groups["scope"].Value} '{m.Groups["name"].Value}' adicionada — clique em Aplicar para salvar."),

        Rule(@"^Remove the (?<scope>.+) variable '(?<name>.+)'\?$", m =>
            $"Remover a variável {m.Groups["scope"].Value} '{m.Groups["name"].Value}'?"),

        Rule(@"^Removed '(?<name>.+)' locally — press Apply to delete it\.$", m =>
            $"'{m.Groups["name"].Value}' removida localmente — clique em Aplicar para confirmar a exclusão."),

        Rule(@"^Couldn't end (?<name>.+) — it may need administrator rights, or it already exited\.$", m =>
            $"Não foi possível encerrar {m.Groups["name"].Value} — pode exigir administrador ou o processo já pode ter sido encerrado."),

        Rule(@"^Complete — (?<ok>\d+) shredded, (?<failed>\d+) failed\.$", m =>
            $"Concluído — {m.Groups["ok"].Value} excluído(s) com segurança, {m.Groups["failed"].Value} falha(s)."),

        Rule(@"^Loaded (?<count>\d+) events from (?<log>.+)$", m =>
            $"{m.Groups["count"].Value} eventos carregados de {m.Groups["log"].Value}"),

        Rule(@"^(?<tool>.+) completed successfully$", m =>
            $"{m.Groups["tool"].Value} concluído com sucesso"),

        Rule(@"^✓ Trimmed working set of (?<count>\d+) processes\.$", m =>
            $"✓ Memória de trabalho reduzida em {m.Groups["count"].Value} processo(s)."),

        Rule(@"^Loaded (?<count>\d+) processes\.$", m =>
            $"{m.Groups["count"].Value} processos carregados."),

        Rule(@"^(?<count>\d+) processes · (?<memory>.+) total memory$", m =>
            $"{m.Groups["count"].Value} processos · {m.Groups["memory"].Value} de memória total"),

        Rule(@"^Keeping (?<days>\d+) days of history\.$", m =>
            $"Mantendo {m.Groups["days"].Value} dias de histórico."),

        Rule(@"^#(?<number>\d+) — (?<description>.+)$", m =>
            $"#{m.Groups["number"].Value} — {m.Groups["description"].Value}"),

        Rule(@"^Loaded (?<total>\d+) services \((?<running>\d+) running\)\.$", m =>
            $"{m.Groups["total"].Value} serviços carregados ({m.Groups["running"].Value} em execução)."),

        Rule(@"^(?<total>\d+) services \((?<running>\d+) running\)$", m =>
            $"{m.Groups["total"].Value} serviços ({m.Groups["running"].Value} em execução)"),

        Rule(@"^Start service \"(?<name>.+)\"\?$", m =>
            $"Iniciar o serviço \"{m.Groups["name"].Value}\"?"),

        Rule(@"^Stop service \"(?<name>.+)\"\?\s+This may affect system functionality\.$", m =>
            $"Parar o serviço \"{m.Groups["name"].Value}\"? Isso pode afetar funcionalidades do sistema."),

        Rule(@"^Disable service \"(?<name>.+)\"\?\s+This prevents the service from starting automatically\.$", m =>
            $"Desativar o serviço \"{m.Groups["name"].Value}\"? Isso impede que ele inicie automaticamente."),

        Rule(@"^✓ (?<name>.+) set to Disabled\.$", m =>
            $"✓ {m.Groups["name"].Value} definido como Desativado."),

        Rule(@"^(?<enabled>\d+) enabled · (?<disabled>\d+) disabled · (?<total>\d+) total$", m =>
            $"{m.Groups["enabled"].Value} ativado(s) · {m.Groups["disabled"].Value} desativado(s) · {m.Groups["total"].Value} total"),

        Rule(@"^(?<fix>.+) did not complete — see the output for details\.$", m =>
            $"{m.Groups["fix"].Value} não foi concluído — consulte a saída para detalhes."),

        Rule(@"^(?<disks>\d+) disks, (?<modules>\d+) RAM modules$", m =>
            $"{m.Groups["disks"].Value} disco(s), {m.Groups["modules"].Value} módulo(s) de RAM"),

        Rule(@"^Scanning (?<count>\d+) drive\(s\)\.\.\.$", m =>
            $"Analisando {m.Groups["count"].Value} unidade(s)..."),

        Rule(@"^\[(?<current>\d+)/(?<total>\d+)\] Scanning (?<drive>.+) — (?<label>.*)$", m =>
            $"[{m.Groups["current"].Value}/{m.Groups["total"].Value}] Analisando {m.Groups["drive"].Value} — {m.Groups["label"].Value}"),

        Rule(@"^Running chkdsk (?<drive>.+) \(read-only\)\.\.\.$", m =>
            $"Executando chkdsk {m.Groups["drive"].Value} (somente leitura)..."),

        Rule(@"^Couldn't change \"(?<name>.+)\" — this usually needs administrator rights\.$", m =>
            $"Não foi possível alterar \"{m.Groups["name"].Value}\" — normalmente isso exige direitos de administrador."),

        Rule(@"^Auto-trace running \((?<host>.+)\)$", m =>
            $"Rastreamento automático em execução ({m.Groups["host"].Value})"),

        Rule(@"^(?<essential>\d+) essential · (?<advanced>\d+) advanced tweak\(s\)\. Tick the ones you want, then Apply or Undo\.$", m =>
            $"{m.Groups["essential"].Value} essencial(is) · {m.Groups["advanced"].Value} ajuste(s) avançado(s). Marque os desejados e use Aplicar ou Desfazer."),

        Rule(@"^Found (?<count>\d+) installed applications\.$", m =>
            $"{m.Groups["count"].Value} aplicativos instalados encontrados."),

        Rule(@"^Found (?<count>\d+) installed applications$", m =>
            $"{m.Groups["count"].Value} aplicativos instalados encontrados"),

        Rule(@"^Uninstalling (?<name>.+) \((?<current>\d+)/(?<total>\d+)\)\.\.\.$", m =>
            $"Desinstalando {m.Groups["name"].Value} ({m.Groups["current"].Value}/{m.Groups["total"].Value})..."),

        Rule(@"^Found (?<count>\d+) features \((?<enabled>\d+) enabled\)\.$", m =>
            $"{m.Groups["count"].Value} recursos encontrados ({m.Groups["enabled"].Value} ativados)."),

        Rule(@"^Failed to (?<action>.+) (?<feature>.+)\. Check permissions\.$", m =>
            $"Falha ao {m.Groups["action"].Value} {m.Groups["feature"].Value}. Verifique as permissões."),

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
