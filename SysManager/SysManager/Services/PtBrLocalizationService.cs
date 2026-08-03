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

namespace SysManager.Services;

/// <summary>
/// Applies pt-BR translations to the actual WPF interface, including controls created after
/// startup, popups, context menus, tooltips, dialogs and custom dependency properties.
/// </summary>
public static class PtBrLocalizationService
{
    private static readonly ConditionalWeakTable<DependencyObject, object> Observed = new();
    private static readonly ConditionalWeakTable<Window, object> AttachedWindows = new();
    private static readonly object Marker = new();
    private static readonly ConcurrentDictionary<Type, StringPropertyAccessor[]> AccessorCache = new();
    private static int _globalHandlersRegistered;

    // WPF-UI and the imported application use several different property names for visible text.
    // Reflection keeps this layer independent from one control library while SetCurrentValue keeps
    // bindings intact for dependency properties.
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

    /// <summary>
    /// Installs class-level Loaded handlers. Unlike a handler attached only to MainWindow, these
    /// handlers also receive elements created inside disconnected Popup/ContextMenu trees and
    /// secondary windows.
    /// </summary>
    public static void InitializeApplication()
    {
        ConfigureCulture();
        if (Interlocked.Exchange(ref _globalHandlersRegistered, 1) != 0) return;

        PtBrLiveAuditService.StartSession();

        EventManager.RegisterClassHandler(
            typeof(FrameworkElement),
            FrameworkElement.LoadedEvent,
            new RoutedEventHandler(OnAnyElementLoaded),
            true);

        EventManager.RegisterClassHandler(
            typeof(FrameworkContentElement),
            FrameworkContentElement.LoadedEvent,
            new RoutedEventHandler(OnAnyElementLoaded),
            true);
    }

    public static void Attach(Window window)
    {
        InitializeApplication();

        if (!AttachedWindows.TryGetValue(window, out _))
        {
            AttachedWindows.Add(window, Marker);
            window.Loaded += OnWindowLoaded;
            window.Activated += OnWindowActivated;
            window.ContextMenuOpening += OnContextMenuOpening;
            window.ToolTipOpening += OnToolTipOpening;
        }

        Observe(window);
        TranslateWindow(window);
    }

    public static void TranslateWindow(Window window)
    {
        TranslateTree(window);
        if (window.DataContext is ViewModels.MainWindowViewModel vm)
            window.SetCurrentValue(Window.TitleProperty, vm.IsElevated ? "Optimize — Administrador" : "Optimize");
    }

    private static void OnWindowLoaded(object sender, RoutedEventArgs e)
    {
        if (sender is Window window) TranslateWindow(window);
    }

    private static void OnWindowActivated(object? sender, EventArgs e)
    {
        if (sender is Window window) TranslateWindow(window);
    }

    private static void OnAnyElementLoaded(object sender, RoutedEventArgs e)
    {
        if (sender is not DependencyObject element) return;
        TranslateElement(element);
        Observe(element);
    }

    private static void OnContextMenuOpening(object sender, ContextMenuEventArgs e)
    {
        if (e.OriginalSource is FrameworkElement { ContextMenu: { } menu })
            TranslateTree(menu);
    }

    private static void OnToolTipOpening(object sender, ToolTipEventArgs e)
    {
        if (e.OriginalSource is FrameworkElement { ToolTip: DependencyObject tooltip })
            TranslateTree(tooltip);
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

            TranslateElement(current);
            Observe(current);

            foreach (var child in EnumerateChildren(current))
                pending.Push(child);
        }
    }

    private static IEnumerable<DependencyObject> EnumerateChildren(DependencyObject root)
    {
        if (root is Visual or Visual3D)
        {
            int count;
            try { count = VisualTreeHelper.GetChildrenCount(root); }
            catch (InvalidOperationException) { count = 0; }

            for (var i = 0; i < count; i++)
                yield return VisualTreeHelper.GetChild(root, i);
        }

        System.Collections.IEnumerable? logicalChildren = null;
        try { logicalChildren = LogicalTreeHelper.GetChildren(root); }
        catch (InvalidOperationException) { /* Some disconnected objects have no logical tree. */ }

        if (logicalChildren is not null)
        {
            foreach (var child in logicalChildren)
                if (child is DependencyObject dependencyObject)
                    yield return dependencyObject;
        }

        switch (root)
        {
            case Popup { Child: { } popupChild }:
                yield return popupChild;
                break;
            case FrameworkElement { ContextMenu: { } contextMenu }:
                yield return contextMenu;
                break;
        }

        if (root is FrameworkElement { ToolTip: DependencyObject toolTip })
            yield return toolTip;

        if (root is ContentControl { Content: DependencyObject content })
            yield return content;

        if (root is HeaderedContentControl { Header: DependencyObject header })
            yield return header;

        if (root is HeaderedItemsControl { Header: DependencyObject itemsHeader })
            yield return itemsHeader;

        if (root is ItemsControl itemsControl)
        {
            foreach (var item in itemsControl.Items)
                if (item is DependencyObject dependencyObject)
                    yield return dependencyObject;
        }

        if (root is DataGrid dataGrid)
        {
            foreach (var column in dataGrid.Columns)
                yield return column;
        }

        if (root is ListView { View: GridView gridView })
        {
            foreach (var column in gridView.Columns)
                yield return column;
        }

        if (root is TextBlock textBlock)
        {
            foreach (var inline in textBlock.Inlines)
                yield return inline;
        }

        if (root is Span span)
        {
            foreach (var inline in span.Inlines)
                yield return inline;
        }

        if (root is Paragraph paragraph)
        {
            foreach (var inline in paragraph.Inlines)
                yield return inline;
        }

        if (root is RichTextBox { Document: { } richDocument })
            yield return richDocument;

        if (root is FlowDocumentScrollViewer { Document: { } viewerDocument })
            yield return viewerDocument;
    }

    private static void TranslateElement(DependencyObject element)
    {
        if (element is TextBlock textBlock)
            TranslateTextBlock(textBlock);
        else if (element is Run run)
            TranslateRun(run);

        foreach (var accessor in AccessorCache.GetOrAdd(element.GetType(), BuildAccessors))
            TranslateProperty(element, accessor);

        TranslateAutomationText(element);
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
            TranslateRun(run);
            Observe(run);
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
        catch (TargetInvocationException)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(current) || !ShouldTranslateProperty(element, accessor.Name))
            return;

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
        catch (Exception ex) when (ex is InvalidOperationException or TargetInvocationException or ArgumentException)
        {
            // Read-only template state or a control that rejects late mutation. The live audit
            // still records the string so it can be fixed at the source instead.
        }
    }

    private static bool ShouldTranslateProperty(DependencyObject element, string propertyName)
    {
        if (!string.Equals(propertyName, "Text", StringComparison.Ordinal)) return true;

        // Never rewrite user input. Read-only output fields can still be localized.
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

        foreach (var accessor in AccessorCache.GetOrAdd(element.GetType(), BuildAccessors))
        {
            if (accessor.DependencyProperty is not { } dependencyProperty) continue;
            AddValueChanged(element, dependencyProperty);
        }

        AddValueChanged(element, AutomationProperties.NameProperty);
        AddValueChanged(element, AutomationProperties.HelpTextProperty);

        switch (element)
        {
            case Popup popup:
                popup.Opened += OnPopupOpened;
                break;
            case ContextMenu contextMenu:
                contextMenu.Opened += OnContextMenuOpened;
                break;
            case ToolTip toolTip:
                toolTip.Opened += OnToolTipOpened;
                break;
            case MenuItem menuItem:
                menuItem.SubmenuOpened += OnMenuItemSubmenuOpened;
                break;
            case ComboBox comboBox:
                comboBox.DropDownOpened += OnComboBoxDropDownOpened;
                break;
        }
    }

    private static void AddValueChanged(DependencyObject element, DependencyProperty property)
    {
        try
        {
            DependencyPropertyDescriptor.FromProperty(property, element.GetType())
                ?.AddValueChanged(element, OnDynamicPropertyChanged);
        }
        catch (ArgumentException)
        {
            // The property is not registered for this derived type.
        }
    }

    private static void OnDynamicPropertyChanged(object? sender, EventArgs e)
    {
        if (sender is DependencyObject element)
            TranslateElement(element);
    }

    private static void OnPopupOpened(object? sender, EventArgs e)
    {
        if (sender is Popup popup)
            TranslateTree(popup.Child ?? popup);
    }

    private static void OnContextMenuOpened(object? sender, RoutedEventArgs e)
    {
        if (sender is ContextMenu contextMenu)
            TranslateTree(contextMenu);
    }

    private static void OnToolTipOpened(object? sender, RoutedEventArgs e)
    {
        if (sender is ToolTip toolTip)
            TranslateTree(toolTip);
    }

    private static void OnMenuItemSubmenuOpened(object sender, RoutedEventArgs e)
    {
        if (sender is MenuItem menuItem)
            TranslateTree(menuItem);
    }

    private static void OnComboBoxDropDownOpened(object? sender, EventArgs e)
    {
        if (sender is ComboBox comboBox)
            TranslateTree(comboBox);
    }

    private static StringPropertyAccessor[] BuildAccessors(Type type)
    {
        var accessors = new List<StringPropertyAccessor>();
        foreach (var propertyName in UiStringPropertyNames)
        {
            var dependencyProperty = FindDependencyProperty(type, propertyName);
            if (dependencyProperty is not null)
            {
                accessors.Add(new StringPropertyAccessor(propertyName, dependencyProperty, null));
                continue;
            }

            var property = type.GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public);
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

    private sealed record StringPropertyAccessor(
        string Name,
        DependencyProperty? DependencyProperty,
        PropertyInfo? Property);
}
