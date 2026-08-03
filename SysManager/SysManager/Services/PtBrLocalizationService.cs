// Optimize / SysManager · pt-BR localization runtime
// Original project: laurentiu021/SystemManager · MIT License

using System.Collections.Concurrent;
using System.ComponentModel;
using System.Globalization;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using Serilog;

namespace SysManager.Services;

/// <summary>
/// Applies pt-BR translations to the real WPF interface, including controls created after
/// startup, popups, context menus, tooltips, dialogs and custom dependency properties.
/// Localization is best-effort per element: an incompatible third-party control is logged and
/// skipped instead of preventing the application from opening.
/// </summary>
public static class PtBrLocalizationService
{
    private static readonly ConditionalWeakTable<DependencyObject, object> Observed = new();
    private static readonly ConditionalWeakTable<Window, object> AttachedWindows = new();
    private static readonly object Marker = new();
    private static readonly ConcurrentDictionary<Type, StringPropertyAccessor[]> AccessorCache = new();
    private static int _globalHandlersRegistered;

    private static readonly string[] UiStringPropertyNames =
    [
        "Text", "Content", "Header", "Title", "ToolTip", "PlaceholderText",
        "Placeholder", "Watermark", "Description", "Message", "Subtitle",
        "Caption", "Footer", "Label", "Detail", "Status", "Summary",
        "OnContent", "OffContent", "PrimaryButtonText", "SecondaryButtonText",
        "CloseButtonText", "AcceptButtonText", "CancelButtonText", "ButtonText",
        "EmptyText", "NoResultsText", "SearchPlaceholderText", "ClearButtonToolTip",
        "MoreButtonToolTip", "OpenButtonToolTip", "CloseButtonToolTip"
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
        if (PtBrMigrationCatalog.TryTranslate(text, out var migrated)) return migrated;
        if (PtBrMigrationCatalog2.TryTranslate(text, out migrated)) return migrated;
        if (PtBrMigrationCatalog3.TryTranslate(text, out migrated)) return migrated;
        if (PtBrMigrationCatalog4.TryTranslate(text, out migrated)) return migrated;
        if (PtBrMigrationCatalog5.TryTranslate(text, out migrated)) return migrated;
        if (PtBrMigrationCatalog6.TryTranslate(text, out migrated)) return migrated;
        if (PtBrRuntimePatternCatalog.TryTranslate(text, out migrated)) return migrated;

        var translated = PtBrTranslationCatalog.Translate(text);
        return PtBrRuntimeFallbackCatalog.Translate(translated);
    }

    public static void InitializeApplication()
    {
        ConfigureCulture();
        if (Interlocked.Exchange(ref _globalHandlersRegistered, 1) != 0) return;

        PtBrLiveAuditService.StartSession();

        TryLocalization(() => EventManager.RegisterClassHandler(
            typeof(FrameworkElement),
            FrameworkElement.LoadedEvent,
            new RoutedEventHandler(OnAnyElementLoaded),
            true), "registro global de FrameworkElement");

        TryLocalization(() => EventManager.RegisterClassHandler(
            typeof(FrameworkContentElement),
            FrameworkContentElement.LoadedEvent,
            new RoutedEventHandler(OnAnyElementLoaded),
            true), "registro global de FrameworkContentElement");
    }

    public static void Attach(Window window)
    {
        TryLocalization(InitializeApplication, "inicialização da localização");

        TryLocalization(() =>
        {
            if (!AttachedWindows.TryGetValue(window, out _))
            {
                AttachedWindows.Add(window, Marker);
                window.Loaded += OnWindowLoaded;
                window.Activated += OnWindowActivated;
                window.ContextMenuOpening += OnContextMenuOpening;
                window.ToolTipOpening += OnToolTipOpening;
            }
        }, "eventos da janela");

        TryLocalization(() => Observe(window), "observação da janela");
        TryLocalization(() => TranslateWindow(window), "tradução inicial da janela");
    }

    public static void TranslateWindow(Window window)
    {
        TranslateTree(window);
        if (window.DataContext is ViewModels.MainWindowViewModel vm)
            TryLocalization(
                () => window.SetCurrentValue(Window.TitleProperty, vm.IsElevated ? "Optimize — Administrador" : "Optimize"),
                "título da janela");
    }

    private static void OnWindowLoaded(object sender, RoutedEventArgs e)
    {
        if (sender is Window window)
            TryLocalization(() => TranslateWindow(window), "janela carregada");
    }

    private static void OnWindowActivated(object? sender, EventArgs e)
    {
        if (sender is Window window)
            TryLocalization(() => TranslateWindow(window), "janela ativada");
    }

    private static void OnAnyElementLoaded(object sender, RoutedEventArgs e)
    {
        if (sender is not DependencyObject element) return;
        TryLocalization(() => TranslateElement(element), $"Loaded de {element.GetType().Name}");
        TryLocalization(() => Observe(element), $"observação de {element.GetType().Name}");
    }

    private static void OnContextMenuOpening(object sender, ContextMenuEventArgs e)
    {
        if (e.OriginalSource is FrameworkElement { ContextMenu: { } menu })
            TryLocalization(() => TranslateTree(menu), "menu de contexto");
    }

    private static void OnToolTipOpening(object sender, ToolTipEventArgs e)
    {
        if (e.OriginalSource is FrameworkElement { ToolTip: DependencyObject tooltip })
            TryLocalization(() => TranslateTree(tooltip), "dica de ferramenta");
    }

    private static void TranslateTree(DependencyObject root)
    {
        var visited = new HashSet<DependencyObject>(ReferenceEqualityComparer.Instance);
        var pending = new Stack<DependencyObject>();
        pending.Push(root);

        while (pending.Count > 0)
        {
            var current = pending.Pop();
            if (!visited.Add(current)) continue;

            TryLocalization(() => TranslateElement(current), $"elemento {current.GetType().Name}");
            TryLocalization(() => Observe(current), $"observação de {current.GetType().Name}");

            foreach (var child in SafeChildren(current))
                pending.Push(child);
        }
    }

    private static IReadOnlyList<DependencyObject> SafeChildren(DependencyObject root)
    {
        try
        {
            return EnumerateChildren(root).Where(child => child is not null).ToArray();
        }
        catch (Exception ex) when (IsRecoverable(ex))
        {
            Log.Debug(ex, "pt-BR: falha ao percorrer filhos de {ControlType}", root.GetType().FullName);
            return [];
        }
    }

    private static IEnumerable<DependencyObject> EnumerateChildren(DependencyObject root)
    {
        if (root is Visual or Visual3D)
        {
            var count = 0;
            try { count = VisualTreeHelper.GetChildrenCount(root); }
            catch (Exception ex) when (IsRecoverable(ex))
            {
                Log.Debug(ex, "pt-BR: árvore visual indisponível para {ControlType}", root.GetType().FullName);
            }

            for (var i = 0; i < count; i++)
            {
                DependencyObject? child = null;
                try { child = VisualTreeHelper.GetChild(root, i); }
                catch (Exception ex) when (IsRecoverable(ex))
                {
                    Log.Debug(ex, "pt-BR: filho visual indisponível para {ControlType}", root.GetType().FullName);
                }

                if (child is not null) yield return child;
            }
        }

        System.Collections.IEnumerable? logicalChildren = null;
        try { logicalChildren = LogicalTreeHelper.GetChildren(root); }
        catch (Exception ex) when (IsRecoverable(ex))
        {
            Log.Debug(ex, "pt-BR: árvore lógica indisponível para {ControlType}", root.GetType().FullName);
        }

        if (logicalChildren is not null)
        {
            foreach (var child in logicalChildren)
                if (child is DependencyObject dependencyObject)
                    yield return dependencyObject;
        }

        if (root is Popup { Child: { } popupChild }) yield return popupChild;
        if (root is FrameworkElement { ContextMenu: { } contextMenu }) yield return contextMenu;
        if (root is FrameworkElement { ToolTip: DependencyObject toolTip }) yield return toolTip;
        if (root is ContentControl { Content: DependencyObject content }) yield return content;
        if (root is HeaderedContentControl { Header: DependencyObject header }) yield return header;
        if (root is HeaderedItemsControl { Header: DependencyObject itemsHeader }) yield return itemsHeader;

        if (root is ItemsControl itemsControl)
        {
            foreach (var item in itemsControl.Items)
                if (item is DependencyObject dependencyObject)
                    yield return dependencyObject;
        }

        if (root is DataGrid dataGrid)
            foreach (var column in dataGrid.Columns) yield return column;

        if (root is ListView { View: GridView gridView })
            foreach (var column in gridView.Columns) yield return column;

        if (root is TextBlock textBlock)
            foreach (var inline in textBlock.Inlines) yield return inline;

        if (root is Span span)
            foreach (var inline in span.Inlines) yield return inline;

        if (root is Paragraph paragraph)
            foreach (var inline in paragraph.Inlines) yield return inline;

        if (root is RichTextBox { Document: { } richDocument }) yield return richDocument;
        if (root is FlowDocumentScrollViewer { Document: { } viewerDocument }) yield return viewerDocument;
    }

    private static void TranslateElement(DependencyObject element)
    {
        if (element is TextBlock textBlock) TranslateTextBlock(textBlock);
        else if (element is Run run) TranslateRun(run);

        StringPropertyAccessor[] accessors;
        try { accessors = AccessorCache.GetOrAdd(element.GetType(), BuildAccessors); }
        catch (Exception ex) when (IsRecoverable(ex))
        {
            Log.Debug(ex, "pt-BR: propriedades indisponíveis para {ControlType}", element.GetType().FullName);
            accessors = [];
        }

        foreach (var accessor in accessors)
            TryLocalization(() => TranslateProperty(element, accessor), $"{element.GetType().Name}.{accessor.Name}");

        TryLocalization(() => TranslateAutomationText(element), $"automação de {element.GetType().Name}");
    }

    private static void TranslateTextBlock(TextBlock text)
    {
        if (!string.IsNullOrWhiteSpace(text.Text))
        {
            var translated = Translate(text.Text);
            PtBrLiveAuditService.Inspect(text.GetType().Name, "Text", translated);
            if (!string.Equals(text.Text, translated, StringComparison.Ordinal))
                text.SetCurrentValue(TextBlock.TextProperty, translated);
        }

        foreach (var inline in text.Inlines)
        {
            if (inline is not Run run) continue;
            TryLocalization(() => TranslateRun(run), "Run em TextBlock");
            TryLocalization(() => Observe(run), "observação de Run");
        }
    }

    private static void TranslateRun(Run run)
    {
        if (string.IsNullOrWhiteSpace(run.Text)) return;
        var translated = Translate(run.Text);
        PtBrLiveAuditService.Inspect(run.GetType().Name, "Text", translated);
        if (!string.Equals(run.Text, translated, StringComparison.Ordinal))
            run.SetCurrentValue(Run.TextProperty, translated);
    }

    private static void TranslateProperty(DependencyObject element, StringPropertyAccessor accessor)
    {
        string? current;
        try
        {
            current = accessor.DependencyProperty is { } dependencyProperty
                ? element.GetValue(dependencyProperty) as string
                : accessor.Property?.GetValue(element) as string;
        }
        catch (Exception ex) when (IsRecoverable(ex))
        {
            Log.Debug(ex, "pt-BR: leitura recusada em {ControlType}.{Property}", element.GetType().FullName, accessor.Name);
            return;
        }

        if (string.IsNullOrWhiteSpace(current) || !ShouldTranslateProperty(element, accessor.Name)) return;

        var translated = Translate(current);
        PtBrLiveAuditService.Inspect(element.GetType().Name, accessor.Name, translated);
        if (string.Equals(current, translated, StringComparison.Ordinal)) return;

        try
        {
            if (accessor.DependencyProperty is { } dependencyProperty)
                element.SetCurrentValue(dependencyProperty, translated);
            else
                accessor.Property?.SetValue(element, translated);
        }
        catch (Exception ex) when (IsRecoverable(ex))
        {
            Log.Debug(ex, "pt-BR: escrita recusada em {ControlType}.{Property}", element.GetType().FullName, accessor.Name);
        }
    }

    private static bool ShouldTranslateProperty(DependencyObject element, string propertyName)
    {
        if (!string.Equals(propertyName, "Text", StringComparison.Ordinal)) return true;
        if (element is TextBox textBox) return textBox.IsReadOnly;

        var typeName = element.GetType().Name;
        return !typeName.Contains("PasswordBox", StringComparison.OrdinalIgnoreCase)
            && !typeName.Contains("AutoSuggestBox", StringComparison.OrdinalIgnoreCase)
            && !typeName.Contains("NumberBox", StringComparison.OrdinalIgnoreCase);
    }

    private static void TranslateAutomationText(DependencyObject element)
    {
        TranslateAttachedString(element, AutomationProperties.NameProperty, "AutomationName");
        TranslateAttachedString(element, AutomationProperties.HelpTextProperty, "AutomationHelpText");
    }

    private static void TranslateAttachedString(DependencyObject element, DependencyProperty property, string propertyName)
    {
        if (element.GetValue(property) is not string current || string.IsNullOrWhiteSpace(current)) return;
        var translated = Translate(current);
        PtBrLiveAuditService.Inspect(element.GetType().Name, propertyName, translated);
        if (!string.Equals(current, translated, StringComparison.Ordinal))
            element.SetCurrentValue(property, translated);
    }

    private static void Observe(DependencyObject element)
    {
        if (Observed.TryGetValue(element, out _)) return;
        Observed.Add(element, Marker);

        StringPropertyAccessor[] accessors;
        try { accessors = AccessorCache.GetOrAdd(element.GetType(), BuildAccessors); }
        catch (Exception ex) when (IsRecoverable(ex))
        {
            Log.Debug(ex, "pt-BR: não foi possível observar {ControlType}", element.GetType().FullName);
            accessors = [];
        }

        foreach (var accessor in accessors)
            if (accessor.DependencyProperty is { } dependencyProperty)
                AddValueChanged(element, dependencyProperty);

        AddValueChanged(element, AutomationProperties.NameProperty);
        AddValueChanged(element, AutomationProperties.HelpTextProperty);

        switch (element)
        {
            case Popup popup: popup.Opened += OnPopupOpened; break;
            case ContextMenu contextMenu: contextMenu.Opened += OnContextMenuOpened; break;
            case ToolTip toolTip: toolTip.Opened += OnToolTipOpened; break;
            case MenuItem menuItem: menuItem.SubmenuOpened += OnMenuItemSubmenuOpened; break;
            case ComboBox comboBox: comboBox.DropDownOpened += OnComboBoxDropDownOpened; break;
        }
    }

    private static void AddValueChanged(DependencyObject element, DependencyProperty property)
    {
        try
        {
            DependencyPropertyDescriptor.FromProperty(property, element.GetType())
                ?.AddValueChanged(element, OnDynamicPropertyChanged);
        }
        catch (Exception ex) when (IsRecoverable(ex))
        {
            Log.Debug(ex, "pt-BR: observação recusada em {ControlType}", element.GetType().FullName);
        }
    }

    private static void OnDynamicPropertyChanged(object? sender, EventArgs e)
    {
        if (sender is DependencyObject element)
            TryLocalization(() => TranslateElement(element), $"mudança dinâmica em {element.GetType().Name}");
    }

    private static void OnPopupOpened(object? sender, EventArgs e)
    {
        if (sender is Popup popup)
            TryLocalization(() => TranslateTree(popup.Child ?? popup), "Popup aberto");
    }

    private static void OnContextMenuOpened(object? sender, RoutedEventArgs e)
    {
        if (sender is ContextMenu contextMenu)
            TryLocalization(() => TranslateTree(contextMenu), "ContextMenu aberto");
    }

    private static void OnToolTipOpened(object? sender, RoutedEventArgs e)
    {
        if (sender is ToolTip toolTip)
            TryLocalization(() => TranslateTree(toolTip), "ToolTip aberto");
    }

    private static void OnMenuItemSubmenuOpened(object sender, RoutedEventArgs e)
    {
        if (sender is MenuItem menuItem)
            TryLocalization(() => TranslateTree(menuItem), "submenu aberto");
    }

    private static void OnComboBoxDropDownOpened(object? sender, EventArgs e)
    {
        if (sender is ComboBox comboBox)
            TryLocalization(() => TranslateTree(comboBox), "ComboBox aberto");
    }

    private static StringPropertyAccessor[] BuildAccessors(Type type)
    {
        var accessors = new List<StringPropertyAccessor>();
        foreach (var propertyName in UiStringPropertyNames)
        {
            DependencyProperty? dependencyProperty = null;
            try { dependencyProperty = FindDependencyProperty(type, propertyName); }
            catch (Exception ex) when (IsRecoverable(ex))
            {
                Log.Debug(ex, "pt-BR: DP {Property} indisponível em {ControlType}", propertyName, type.FullName);
            }

            if (dependencyProperty is not null)
            {
                accessors.Add(new StringPropertyAccessor(propertyName, dependencyProperty, null));
                continue;
            }

            PropertyInfo? property = null;
            try { property = type.GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public); }
            catch (Exception ex) when (IsRecoverable(ex))
            {
                Log.Debug(ex, "pt-BR: propriedade {Property} indisponível em {ControlType}", propertyName, type.FullName);
            }

            if (property is null || !property.CanRead || !property.CanWrite || property.GetIndexParameters().Length != 0)
                continue;

            if (property.PropertyType == typeof(string) || property.PropertyType == typeof(object))
                accessors.Add(new StringPropertyAccessor(propertyName, null, property));
        }

        return [.. accessors];
    }

    private static DependencyProperty? FindDependencyProperty(Type type, string propertyName)
    {
        for (var current = type; current is not null; current = current.BaseType)
        {
            var field = current.GetField(
                propertyName + "Property",
                BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);

            if (field?.FieldType == typeof(DependencyProperty))
                return field.GetValue(null) as DependencyProperty;
        }

        return null;
    }

    private static void TryLocalization(Action action, string context)
    {
        try { action(); }
        catch (Exception ex) when (IsRecoverable(ex))
        {
            Log.Debug(ex, "pt-BR: falha isolada durante {Context}", context);
        }
    }

    private static bool IsRecoverable(Exception ex)
        => ex is not OutOfMemoryException
            and not StackOverflowException
            and not AccessViolationException;

    private sealed record StringPropertyAccessor(
        string Name,
        DependencyProperty? DependencyProperty,
        PropertyInfo? Property);
}
