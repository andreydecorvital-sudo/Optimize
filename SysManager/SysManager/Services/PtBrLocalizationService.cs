// Optimize / SysManager · pt-BR localization layer
// Original project: laurentiu021/SystemManager · MIT License

using System.Globalization;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;

namespace SysManager.Services;

/// <summary>
/// Camada de localização pt-BR usada durante a transformação do SysManager em Optimize.
/// Mantém IDs técnicos e nomes de tecnologias, traduzindo somente textos voltados ao usuário.
/// A migração futura deve mover os textos para ResourceDictionary/RESX por idioma.
/// </summary>
public static class PtBrLocalizationService
{
    private static readonly Dictionary<string, string> Exact = new(StringComparer.OrdinalIgnoreCase)
    {
        // Shell
        ["SysManager"] = "Optimize",
        ["System toolkit"] = "Ferramentas do sistema",
        ["Administrator"] = "Administrador",
        ["Standard user"] = "Usuário padrão",
        ["SysManager — Administrator"] = "Optimize — Administrador",
        ["PREVIEW"] = "PRÉVIA",
        ["Preview"] = "Prévia",
        ["Appearance"] = "Aparência",
        ["Dismiss"] = "Fechar",
        ["Close"] = "Fechar",
        ["Cancel"] = "Cancelar",
        ["Confirm"] = "Confirmar",
        ["Apply"] = "Aplicar",
        ["Save"] = "Salvar",
        ["Delete"] = "Excluir",
        ["Remove"] = "Remover",
        ["Restore"] = "Restaurar",
        ["Refresh"] = "Atualizar",
        ["Search"] = "Pesquisar",
        ["Start"] = "Iniciar",
        ["Stop"] = "Parar",
        ["Run"] = "Executar",
        ["Open"] = "Abrir",
        ["Export"] = "Exportar",
        ["Import"] = "Importar",
        ["Settings"] = "Configurações",
        ["Enabled"] = "Ativado",
        ["Disabled"] = "Desativado",
        ["Enable"] = "Ativar",
        ["Disable"] = "Desativar",
        ["Status"] = "Status",
        ["Name"] = "Nome",
        ["Description"] = "Descrição",
        ["Details"] = "Detalhes",
        ["Recommended"] = "Recomendado",
        ["Warning"] = "Aviso",
        ["Error"] = "Erro",
        ["Success"] = "Sucesso",
        ["Loading..."] = "Carregando...",
        ["Scanning..."] = "Analisando...",
        ["No data"] = "Sem dados",
        ["Unknown"] = "Desconhecido",
        ["Never"] = "Nunca",
        ["Today"] = "Hoje",

        // Navigation groups
        ["Dashboard"] = "Visão geral",
        ["System"] = "Sistema",
        ["Gaming & Profiles"] = "Jogos e perfis",
        ["Monitor"] = "Monitoramento",
        ["Cleanup"] = "Limpeza",
        ["Storage"] = "Armazenamento",
        ["Network"] = "Rede",
        ["Apps"] = "Aplicativos",
        ["Privacy & Security"] = "Privacidade e segurança",
        ["Customization"] = "Personalização",
        ["Info"] = "Informações",
        ["Advanced"] = "Avançado",

        // Navigation items
        ["System Health"] = "Saúde do sistema",
        ["Windows Update"] = "Windows Update",
        ["Performance Mode"] = "Modo de desempenho",
        ["Services"] = "Serviços",
        ["Startup Manager"] = "Inicialização",
        ["Windows Features"] = "Recursos do Windows",
        ["Restore Points"] = "Pontos de restauração",
        ["Task Scheduler"] = "Agendador de tarefas",
        ["Boot Analyzer"] = "Análise de inicialização",
        ["System Fixes"] = "Correções do sistema",
        ["Tweaks Hub"] = "Central de ajustes",
        ["Gaming Profile"] = "Perfil para jogos",
        ["Standby List Cleaner"] = "Limpeza da memória em espera",
        ["Timer Resolution"] = "Resolução do temporizador",
        ["CPU Core Affinity"] = "Afinidade dos núcleos da CPU",
        ["Display Profiles"] = "Perfis de vídeo",
        ["Process Manager"] = "Gerenciador de processos",
        ["Resource History"] = "Histórico de recursos",
        ["Camera/Mic/Location"] = "Câmera / microfone / localização",
        ["App Alerts"] = "Alertas de aplicativos",
        ["File Lock Detector"] = "Detector de arquivos bloqueados",
        ["Settings Watchdog"] = "Monitor de configurações",
        ["Bandwidth Monitor"] = "Monitor de largura de banda",
        ["Quick Cleanup"] = "Limpeza rápida",
        ["Deep Cleanup"] = "Limpeza profunda",
        ["Shortcut Cleaner"] = "Limpeza de atalhos",
        ["Scheduled Maintenance"] = "Manutenção agendada",
        ["Disk Analyzer"] = "Analisador de disco",
        ["Duplicate Finder"] = "Arquivos duplicados",
        ["Ping"] = "Ping",
        ["Traceroute"] = "Rastreamento de rota",
        ["Speed Test"] = "Teste de velocidade",
        ["Network Repair"] = "Reparo de rede",
        ["DNS & Hosts"] = "DNS e Hosts",
        ["App Updates"] = "Atualizações de aplicativos",
        ["Bulk Installer"] = "Instalação em lote",
        ["Uninstaller"] = "Desinstalador",
        ["Privacy & Telemetry"] = "Privacidade e telemetria",
        ["File Shredder"] = "Exclusão segura de arquivos",
        ["App Blocker"] = "Bloqueador de aplicativos",
        ["Debloater & Ads"] = "Remover bloatware e anúncios",
        ["Browser Cleaner"] = "Limpeza de navegadores",
        ["Edge/OneDrive Remover"] = "Remover Edge / OneDrive",
        ["Defender Tweaks"] = "Ajustes do Defender",
        ["Notification Blocker"] = "Bloqueador de notificações",
        ["Context Menu"] = "Menu de contexto",
        ["Dark Mode Scheduler"] = "Agendamento do modo escuro",
        ["Volume Control"] = "Controle de volume",
        ["Drivers"] = "Drivers",
        ["Battery Health"] = "Saúde da bateria",
        ["System Logs"] = "Logs do sistema",
        ["System Report"] = "Relatório do sistema",
        ["Legacy Panels"] = "Painéis clássicos",
        ["About"] = "Sobre",
        ["Profile Export/Import"] = "Exportar / importar perfil",
        ["CLI Interface"] = "Interface de linha de comando",
        ["Environment Variables"] = "Variáveis de ambiente",

        // Common dashboard / diagnostic vocabulary
        ["System Health"] = "Saúde do sistema",
        ["Health"] = "Saúde",
        ["CPU"] = "CPU",
        ["GPU"] = "GPU",
        ["Memory"] = "Memória",
        ["Disk"] = "Disco",
        ["Temperature"] = "Temperatura",
        ["Uptime"] = "Tempo ligado",
        ["Processes"] = "Processos",
        ["Startup"] = "Inicialização",
        ["Updates"] = "Atualizações",
        ["Drivers"] = "Drivers",
        ["Performance"] = "Desempenho",
        ["Power"] = "Energia",
        ["Security"] = "Segurança",
        ["Privacy"] = "Privacidade",
        ["Recent activity"] = "Atividade recente",
        ["Quick actions"] = "Ações rápidas",
        ["System information"] = "Informações do sistema",
        ["Operating System"] = "Sistema operacional",
        ["Processor"] = "Processador",
        ["Graphics"] = "Gráficos",
        ["Motherboard"] = "Placa-mãe",
        ["BIOS"] = "BIOS",
        ["Installed RAM"] = "RAM instalada",
        ["Available"] = "Disponível",
        ["Used"] = "Em uso",
        ["Free"] = "Livre",
        ["Total"] = "Total",
        ["Healthy"] = "Saudável",
        ["Good"] = "Bom",
        ["Attention"] = "Atenção",
        ["Critical"] = "Crítico",
        ["Check for updates"] = "Verificar atualizações",
        ["Create restore point"] = "Criar ponto de restauração",
        ["Run as administrator"] = "Executar como administrador",
        ["Requires administrator"] = "Requer administrador",
        ["Administrator privileges required"] = "Privilégios de administrador necessários",
    };

    private static readonly (string English, string Portuguese)[] PhraseReplacements =
    [
        ("Run as administrator", "Executar como administrador"),
        ("Requires administrator", "Requer administrador"),
        ("System Health", "Saúde do sistema"),
        ("System Information", "Informações do sistema"),
        ("Check for updates", "Verificar atualizações"),
        ("Create restore point", "Criar ponto de restauração"),
        ("No updates available", "Nenhuma atualização disponível"),
        ("Update available", "Atualização disponível"),
        ("Last checked", "Última verificação"),
        ("Last scan", "Última análise"),
        ("Scan now", "Analisar agora"),
        ("Refresh all", "Atualizar tudo"),
        ("Recent activity", "Atividade recente"),
        ("Quick actions", "Ações rápidas"),
        ("in development", "em desenvolvimento"),
    ];

    public static void ConfigureCulture()
    {
        var culture = CultureInfo.GetCultureInfo("pt-BR");
        CultureInfo.DefaultThreadCurrentCulture = culture;
        CultureInfo.DefaultThreadCurrentUICulture = culture;
        Thread.CurrentThread.CurrentCulture = culture;
        Thread.CurrentThread.CurrentUICulture = culture;
    }

    public static string Translate(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return text ?? string.Empty;
        var trimmed = text.Trim();
        if (Exact.TryGetValue(trimmed, out var translated))
            return PreserveOuterWhitespace(text, translated);

        var result = text;
        foreach (var (english, portuguese) in PhraseReplacements)
            result = result.Replace(english, portuguese, StringComparison.OrdinalIgnoreCase);
        return result;
    }

    public static void Attach(Window window)
    {
        ConfigureCulture();

        window.Loaded += (_, _) =>
        {
            TranslateWindow(window);
            window.Title = Translate(window.Title);
        };

        // Views são criadas de forma lazy. Capturar Loaded no shell permite traduzir
        // controles das páginas conforme o usuário as abre, sem materializar 50+ VMs no startup.
        window.AddHandler(FrameworkElement.LoadedEvent,
            new RoutedEventHandler((_, e) =>
            {
                if (e.OriginalSource is DependencyObject source)
                    TranslateVisualTree(source);
            }), true);
    }

    public static void TranslateWindow(Window window)
    {
        TranslateVisualTree(window);
        if (window.DataContext is ViewModels.MainWindowViewModel vm)
        {
            window.Title = vm.IsElevated ? "Optimize — Administrador" : "Optimize";
        }
    }

    private static void TranslateVisualTree(DependencyObject root)
    {
        TranslateElement(root);

        var count = VisualTreeHelper.GetChildrenCount(root);
        for (var i = 0; i < count; i++)
            TranslateVisualTree(VisualTreeHelper.GetChild(root, i));
    }

    private static void TranslateElement(DependencyObject element)
    {
        if (element is TextBlock text && !string.IsNullOrWhiteSpace(text.Text))
            text.SetCurrentValue(TextBlock.TextProperty, Translate(text.Text));

        if (element is ContentControl contentControl && contentControl.Content is string content)
            contentControl.SetCurrentValue(ContentControl.ContentProperty, Translate(content));

        if (element is HeaderedContentControl headeredControl && headeredControl.Header is string header)
            headeredControl.SetCurrentValue(HeaderedContentControl.HeaderProperty, Translate(header));

        if (element is HeaderedItemsControl headeredItems && headeredItems.Header is string itemsHeader)
            headeredItems.SetCurrentValue(HeaderedItemsControl.HeaderProperty, Translate(itemsHeader));

        if (element is FrameworkElement fe)
        {
            if (fe.ToolTip is string tooltip)
                fe.SetCurrentValue(FrameworkElement.ToolTipProperty, Translate(tooltip));

            var automationName = AutomationProperties.GetName(fe);
            if (!string.IsNullOrWhiteSpace(automationName))
                AutomationProperties.SetName(fe, Translate(automationName));
        }
    }

    private static string PreserveOuterWhitespace(string original, string translated)
    {
        var leading = original.Length - original.TrimStart().Length;
        var trailing = original.Length - original.TrimEnd().Length;
        return new string(' ', leading) + translated + new string(' ', trailing);
    }
}
