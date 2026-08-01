// Optimize / SysManager · pt-BR localization layer
// Original project: laurentiu021/SystemManager · MIT License

using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Media;

namespace SysManager.Services;

/// <summary>
/// Camada pt-BR usada durante a transformação do SysManager em Optimize.
/// Além do carregamento inicial, observa propriedades de texto vinculadas para que
/// mensagens atualizadas por ViewModels também sejam traduzidas.
/// </summary>
public static class PtBrLocalizationService
{
    private static readonly ConditionalWeakTable<DependencyObject, object> Observed = new();
    private static readonly object ObservedMarker = new();

    private static readonly Dictionary<string, string> Exact = new(StringComparer.OrdinalIgnoreCase)
    {
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
        ["Yes"] = "Sim",
        ["No"] = "Não",
        ["On"] = "Ligado",
        ["Off"] = "Desligado",
        ["Active"] = "Ativo",
        ["Inactive"] = "Inativo",
        ["Running"] = "Em execução",
        ["Stopped"] = "Parado",
        ["Pending"] = "Pendente",
        ["Available"] = "Disponível",
        ["Installed"] = "Instalado",
        ["Not installed"] = "Não instalado",
        ["Requires admin"] = "Requer administrador",
        ["N/A"] = "N/D",

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
        ["System Health"] = "Saúde do sistema",
        ["Windows Update"] = "Windows Update",
        ["Performance Mode"] = "Modo de desempenho",
        ["Services"] = "Serviços",
        ["Startup Manager"] = "Inicialização",
        ["Windows Features"] = "Recursos do Windows",
        ["Restore Points"] = "Pontos de restauração",
        ["Task Scheduler"] = "Agendador de tarefas",
        ["Boot Analyzer"] = "Análise da inicialização",
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

        ["Scan system"] = "Analisar sistema",
        ["Quick Tune-Up"] = "Otimização rápida",
        ["System Health Score"] = "Pontuação de saúde do sistema",
        ["Computing health score…"] = "Calculando a saúde do sistema…",
        ["MEMORY"] = "MEMÓRIA",
        ["TEMPERATURES"] = "TEMPERATURAS",
        ["GB used"] = "GB em uso",
        ["GB available"] = "GB disponíveis",
        ["Run as administrator"] = "Executar como administrador",
        ["Run as admin for all sensors"] = "Executar como administrador para todos os sensores",
        ["Some features require administrator privileges for full data."] = "Alguns recursos precisam de privilégios de administrador para mostrar todos os dados.",
        ["Running as administrator — all sensors and quick actions have full access."] = "Executando como administrador — todos os sensores e ações rápidas têm acesso completo.",
        ["Without admin, only NVIDIA GPU and disk SMART temperatures are available."] = "Sem administrador, apenas temperaturas da GPU NVIDIA e dados SMART dos discos ficam disponíveis.",
        ["Run as administrator for all temperature sensors"] = "Executar como administrador para acessar todos os sensores de temperatura",
        ["Recent activity"] = "Atividade recente",
        ["Quick actions"] = "Ações rápidas",
        ["System information"] = "Informações do sistema",
        ["Operating System"] = "Sistema operacional",
        ["Processor"] = "Processador",
        ["Graphics"] = "Gráficos",
        ["Motherboard"] = "Placa-mãe",
        ["Installed RAM"] = "RAM instalada",
        ["Free"] = "Livre",
        ["Used"] = "Em uso",
        ["Total"] = "Total",
        ["Healthy"] = "Saudável",
        ["Good"] = "Bom",
        ["Attention"] = "Atenção",
        ["Critical"] = "Crítico",
        ["Check for updates"] = "Verificar atualizações",
        ["Create restore point"] = "Criar ponto de restauração",
        ["Administrator privileges required"] = "Privilégios de administrador necessários",
        ["Checking disk health..."] = "Verificando saúde dos discos...",
        ["Checking app updates..."] = "Verificando atualizações de aplicativos...",
        ["Checking memory health..."] = "Verificando saúde da memória...",
        ["Checking Event Log..."] = "Verificando logs de eventos...",
        ["Checking Windows features..."] = "Verificando recursos do Windows...",
        ["No updates available"] = "Nenhuma atualização disponível",
        ["Update available"] = "Atualização disponível",
        ["Last checked"] = "Última verificação",
        ["Last scan"] = "Última análise",
        ["Scan now"] = "Analisar agora",
        ["Refresh all"] = "Atualizar tudo",
        ["Hottest Core"] = "Núcleo mais quente",
        ["Other"] = "Outro",
        ["Degraded"] = "Degradado",
        ["Stressed"] = "Sob estresse",
        ["Predictive Failure"] = "Falha prevista",
        ["Non-Recoverable Error"] = "Erro irrecuperável",
        ["Starting"] = "Iniciando",
        ["Stopping"] = "Parando",
    };

    private static readonly (string English, string Portuguese)[] PhraseReplacements =
    [
        ("SysManager", "Optimize"),
        ("Run as administrator", "Executar como administrador"),
        ("Requires administrator", "Requer administrador"),
        ("Requires admin", "Requer administrador"),
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
        ("Quick Tune-Up", "Otimização rápida"),
        ("Scan system", "Analisar sistema"),
        ("in development", "em desenvolvimento"),
        ("Checking disk health", "Verificando saúde dos discos"),
        ("Checking app updates", "Verificando atualizações de aplicativos"),
        ("Checking memory health", "Verificando saúde da memória"),
        ("Checking Event Log", "Verificando logs de eventos"),
        ("Checking Windows features", "Verificando recursos do Windows"),
        ("GB used", "GB em uso"),
        ("GB available", "GB disponíveis"),
        ("Unknown CPU", "CPU desconhecida"),
        ("Unknown service", "Serviço desconhecido"),
        ("Could not be applied", "Não foi possível aplicar"),
        ("Already in the desired state", "Já está no estado desejado"),
        ("Needs administrator", "Precisa de administrador"),
        ("Hottest Core", "Núcleo mais quente"),
        ("Drive ", "Unidade "),
        ("Code ", "Código "),
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
        Observe(window);
        window.Loaded += (_, _) => TranslateWindow(window);
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
            window.Title = vm.IsElevated ? "Optimize — Administrador" : "Optimize";
    }

    private static void TranslateVisualTree(DependencyObject root)
    {
        TranslateElement(root);
        Observe(root);
        var count = VisualTreeHelper.GetChildrenCount(root);
        for (var i = 0; i < count; i++)
            TranslateVisualTree(VisualTreeHelper.GetChild(root, i));
    }

    private static void TranslateElement(DependencyObject element)
    {
        switch (element)
        {
            case TextBlock text:
                TranslateTextBlock(text);
                break;
            case ContentControl contentControl when contentControl.Content is string content:
                SetTranslatedContent(contentControl, content);
                break;
        }

        if (element is HeaderedContentControl headeredControl && headeredControl.Header is string header)
            headeredControl.SetCurrentValue(HeaderedContentControl.HeaderProperty, Translate(header));

        if (element is HeaderedItemsControl headeredItems && headeredItems.Header is string itemsHeader)
            headeredItems.SetCurrentValue(HeaderedItemsControl.HeaderProperty, Translate(itemsHeader));

        if (element is DataGrid dataGrid)
        {
            foreach (var column in dataGrid.Columns)
                if (column.Header is string headerText)
                    column.Header = Translate(headerText);
        }

        if (element is FrameworkElement fe)
        {
            if (fe.ToolTip is string tooltip)
                fe.SetCurrentValue(FrameworkElement.ToolTipProperty, Translate(tooltip));

            var automationName = AutomationProperties.GetName(fe);
            if (!string.IsNullOrWhiteSpace(automationName))
                AutomationProperties.SetName(fe, Translate(automationName));
        }
    }

    private static void TranslateTextBlock(TextBlock text)
    {
        if (!string.IsNullOrWhiteSpace(text.Text))
        {
            var translated = Translate(text.Text);
            if (!string.Equals(text.Text, translated, StringComparison.Ordinal))
                text.SetCurrentValue(TextBlock.TextProperty, translated);
        }

        foreach (var inline in text.Inlines)
        {
            if (inline is not Run run || string.IsNullOrWhiteSpace(run.Text)) continue;
            var translated = Translate(run.Text);
            if (!string.Equals(run.Text, translated, StringComparison.Ordinal))
                run.SetCurrentValue(Run.TextProperty, translated);
            Observe(run);
        }
    }

    private static void SetTranslatedContent(ContentControl control, string content)
    {
        var translated = Translate(content);
        if (!string.Equals(content, translated, StringComparison.Ordinal))
            control.SetCurrentValue(ContentControl.ContentProperty, translated);
    }

    private static void Observe(DependencyObject element)
    {
        if (Observed.TryGetValue(element, out _)) return;
        Observed.Add(element, ObservedMarker);

        switch (element)
        {
            case TextBlock text:
                DependencyPropertyDescriptor.FromProperty(TextBlock.TextProperty, typeof(TextBlock))
                    ?.AddValueChanged(text, DynamicTextChanged);
                foreach (var inline in text.Inlines)
                    if (inline is Run run) Observe(run);
                break;
            case Run run:
                DependencyPropertyDescriptor.FromProperty(Run.TextProperty, typeof(Run))
                    ?.AddValueChanged(run, DynamicRunChanged);
                break;
            case ContentControl content:
                DependencyPropertyDescriptor.FromProperty(ContentControl.ContentProperty, typeof(ContentControl))
                    ?.AddValueChanged(content, DynamicContentChanged);
                break;
            case Window window:
                DependencyPropertyDescriptor.FromProperty(Window.TitleProperty, typeof(Window))
                    ?.AddValueChanged(window, DynamicTitleChanged);
                break;
        }
    }

    private static void DynamicTextChanged(object? sender, EventArgs e)
    {
        if (sender is TextBlock text) TranslateTextBlock(text);
    }

    private static void DynamicRunChanged(object? sender, EventArgs e)
    {
        if (sender is not Run run || string.IsNullOrWhiteSpace(run.Text)) return;
        var translated = Translate(run.Text);
        if (!string.Equals(run.Text, translated, StringComparison.Ordinal))
            run.SetCurrentValue(Run.TextProperty, translated);
    }

    private static void DynamicContentChanged(object? sender, EventArgs e)
    {
        if (sender is ContentControl control && control.Content is string text)
            SetTranslatedContent(control, text);
    }

    private static void DynamicTitleChanged(object? sender, EventArgs e)
    {
        if (sender is not Window window || string.IsNullOrWhiteSpace(window.Title)) return;
        var translated = Translate(window.Title);
        if (!string.Equals(window.Title, translated, StringComparison.Ordinal))
            window.SetCurrentValue(Window.TitleProperty, translated);
    }

    private static string PreserveOuterWhitespace(string original, string translated)
    {
        var leading = original.Length - original.TrimStart().Length;
        var trailing = original.Length - original.TrimEnd().Length;
        return new string(' ', leading) + translated + new string(' ', trailing);
    }
}
