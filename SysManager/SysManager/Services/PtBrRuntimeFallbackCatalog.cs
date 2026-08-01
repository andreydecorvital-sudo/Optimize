// Optimize · safe pt-BR fallback for runtime status/error prefixes
// Original project: laurentiu021/SystemManager · MIT License

namespace SysManager.Services;

/// <summary>
/// Handles dynamic messages whose error/detail suffix is supplied at runtime.
/// Only known user-facing prefixes are replaced; the actual system/vendor error text remains
/// intact, which is preferable to inventing a translation for technical diagnostics.
/// </summary>
public static class PtBrRuntimeFallbackCatalog
{
    private static readonly (string English, string Portuguese)[] Replacements =
    [
        ("Export failed (access denied):", "Falha ao exportar (acesso negado):"),
        ("Import failed (access denied):", "Falha ao importar (acesso negado):"),
        ("Failed to save report (access denied):", "Falha ao salvar o relatório (acesso negado):"),
        ("Network error — could not reach GitHub:", "Erro de rede — não foi possível acessar o GitHub:"),
        ("Could not read Defender status:", "Não foi possível ler o estado do Defender:"),
        ("Could not change Controlled Folder Access:", "Não foi possível alterar o Acesso Controlado a Pastas:"),
        ("Could not add the exclusion:", "Não foi possível adicionar a exclusão:"),
        ("Could not remove the exclusion:", "Não foi possível remover a exclusão:"),
        ("Could not create a restore point:", "Não foi possível criar um ponto de restauração:"),
        ("Could not start the restore:", "Não foi possível iniciar a restauração:"),
        ("Could not read status:", "Não foi possível ler o estado:"),
        ("Could not start service", "Não foi possível iniciar o serviço"),
        ("Could not stop service", "Não foi possível parar o serviço"),
        ("Couldn't copy to clipboard:", "Não foi possível copiar para a área de transferência:"),
        ("Couldn't copy:", "Não foi possível copiar:"),
        ("Couldn't open", "Não foi possível abrir"),
        ("Couldn't change", "Não foi possível alterar"),
        ("Cannot access update file:", "Não foi possível acessar o arquivo de atualização:"),
        ("Cannot lock update file:", "Não foi possível bloquear o arquivo de atualização:"),
        ("Cannot start —", "Não é possível iniciar —"),
        ("Cannot restore now —", "Não é possível restaurar agora —"),
        ("Cannot apply now —", "Não é possível aplicar agora —"),
        ("Display change failed:", "Falha ao alterar a configuração de vídeo:"),
        ("Health check failed:", "Falha na verificação de saúde:"),
        ("Cleanup failed:", "Falha na limpeza:"),
        ("Read settings failed:", "Falha ao ler as configurações:"),
        ("Power plan change failed:", "Falha ao alterar o plano de energia:"),
        ("Visual effects change failed:", "Falha ao alterar os efeitos visuais:"),
        ("Game Mode change failed:", "Falha ao alterar o Modo de Jogo:"),
        ("Xbox Game Bar change failed:", "Falha ao alterar a Xbox Game Bar:"),
        ("GPU setting change failed:", "Falha ao alterar a configuração da GPU:"),
        ("Processor state change failed:", "Falha ao alterar o estado do processador:"),
        ("Restore point creation failed:", "Falha ao criar o ponto de restauração:"),
        ("RAM trim failed:", "Falha ao liberar memória de trabalho:"),
        ("Hibernation toggle failed:", "Falha ao alterar a hibernação:"),
        ("Restore all settings failed:", "Falha ao restaurar todas as configurações:"),
        ("Drive enumeration failed:", "Falha ao listar unidades:"),
        ("CheckDiskHealth failed:", "Falha ao verificar a saúde dos discos:"),
        ("CheckMemoryErrors failed:", "Falha ao verificar erros de memória:"),
        ("Service scan failed:", "Falha ao analisar serviços:"),
        ("Start service failed:", "Falha ao iniciar o serviço:"),
        ("Stop service failed:", "Falha ao parar o serviço:"),
        ("Disable service failed:", "Falha ao desativar o serviço:"),
        ("Enable service failed:", "Falha ao ativar o serviço:"),
        ("Scan failed:", "Falha na análise:"),
        ("Analysis failed:", "Falha na análise:"),
        ("Clean failed:", "Falha na limpeza:"),
        ("Check failed:", "Falha na verificação:"),
        ("Export failed:", "Falha ao exportar:"),
        ("Import failed:", "Falha ao importar:"),
        ("Download failed:", "Falha no download:"),
        ("Update failed:", "Falha na atualização:"),
        ("Failed to save report:", "Falha ao salvar o relatório:"),
        ("Failed to generate report:", "Falha ao gerar o relatório:"),
        ("Failed to read registry:", "Falha ao ler o Registro:"),
        ("Failed to set DNS:", "Falha ao configurar o DNS:"),
        ("Failed to reset DNS:", "Falha ao redefinir o DNS:"),
        ("Failed to restore DNS:", "Falha ao restaurar o DNS:"),
        ("Error reading hosts file:", "Erro ao ler o arquivo hosts:"),
        ("Error saving hosts file:", "Erro ao salvar o arquivo hosts:"),
        ("Error restoring hosts file:", "Erro ao restaurar o arquivo hosts:"),
        ("Event log error:", "Erro no Log de Eventos:"),
        ("Windows Update Agent error:", "Erro do Agente do Windows Update:"),
        ("WUA error:", "Erro WUA:"),
        ("WMI error:", "Erro WMI:"),
        ("Memory API unavailable:", "API de memória indisponível:"),
        ("Network:", "Rede:"),
        ("Recommended action:", "Ação recomendada:"),
        ("Active plan:", "Plano ativo:"),
        ("Last scan:", "Última análise:"),
        ("Scan at", "Análise às"),
        ("Querying winget list…", "Consultando lista do WinGet…"),
        ("Purging standby list…", "Liberando memória em espera…"),
        ("Error on", "Erro em"),
        ("Error:", "Erro:"),
        ("Failed:", "Falha:"),
    ];

    public static string Translate(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return text ?? string.Empty;
        var result = text;
        foreach (var (english, portuguese) in Replacements)
            result = result.Replace(english, portuguese, StringComparison.OrdinalIgnoreCase);
        return result;
    }
}
