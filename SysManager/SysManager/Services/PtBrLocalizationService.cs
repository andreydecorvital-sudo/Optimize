// Optimize / SysManager · pt-BR localization runtime
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
/// Applies pt-BR translations to both static and dynamically-bound WPF text.
/// </summary>
public static class PtBrLocalizationService
{
    private static readonly ConditionalWeakTable<DependencyObject, object> Observed = new();
    private static readonly object Marker = new();

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
        if (PtBrMigrationCatalog.TryTranslate(text, out var migrated))
            return migrated;
        if (PtBrMigrationCatalog2.TryTranslate(text, out migrated))
            return migrated;
        if (PtBrMigrationCatalog3.TryTranslate(text, out migrated))
            return migrated;
        return PtBrTranslationCatalog.Translate(text);
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
            if (inline is not Run run) continue;
            TranslateRun(run);
            Observe(run);
        }
    }

    private static void TranslateRun(Run run)
    {
        if (string.IsNullOrWhiteSpace(run.Text)) return;
        var translated = Translate(run.Text);
        if (!string.Equals(run.Text, translated, StringComparison.Ordinal))
            run.SetCurrentValue(Run.TextProperty, translated);
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
        Observed.Add(element, Marker);

        switch (element)
        {
            case Window window:
                DependencyPropertyDescriptor.FromProperty(Window.TitleProperty, typeof(Window))
                    ?.AddValueChanged(window, DynamicTitleChanged);
                break;
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
        }
    }

    private static void DynamicTextChanged(object? sender, EventArgs e)
    {
        if (sender is TextBlock text) TranslateTextBlock(text);
    }

    private static void DynamicRunChanged(object? sender, EventArgs e)
    {
        if (sender is Run run) TranslateRun(run);
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
}
