// Optimize · live pt-BR UI audit
// Original project: laurentiu021/SystemManager · MIT License

using System.Diagnostics;
using System.IO;
using FlaUI.Core;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Definitions;
using FlaUI.Core.Tools;
using FlaUI.UIA3;
using SysManager.Services;

namespace SysManager.UITests;

/// <summary>
/// Launches the real application, visits every navigable tab and verifies the rendered interface.
/// In addition to the English detector, this test writes a raw census of EVERY text surfaced by
/// Windows UI Automation per page. The census intentionally performs no language classification;
/// it exists so localization can be reviewed from the real rendered product instead of heuristics.
/// </summary>
public sealed class PtBrSurfaceUiTests
{
    private static readonly Dictionary<string, string> NavigationLabelOverrides = new(StringComparer.Ordinal)
    {
        ["nav-privacy-monitor"] = "Câmera/Microfone/Localização",
        ["nav-app-alerts"] = "Alertas de aplicativos",
        ["nav-dns-hosts"] = "DNS e hosts",
        ["nav-privacy-settings"] = "Privacidade e telemetria",
        ["nav-app-blocker"] = "Bloqueador de aplicativos",
        ["nav-profile-export"] = "Exportar/Importar perfil",
    };

    [Fact]
    public void EveryNavigableSurface_HasNoDetectedEnglishUiText()
    {
        var auditPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Optimize",
            "ptbr-live-audit.log");

        var repositoryRoot = GetRepositoryRoot();
        var inventoryDirectory = Path.Combine(repositoryRoot, "artifacts", "test-results");
        var inventoryPath = Path.Combine(inventoryDirectory, "ptbr-ui-inventory.txt");
        Directory.CreateDirectory(inventoryDirectory);

        try { if (File.Exists(auditPath)) File.Delete(auditPath); }
        catch (IOException) { /* A previous crashed test may still be releasing the handle. */ }
        try { if (File.Exists(inventoryPath)) File.Delete(inventoryPath); }
        catch (IOException) { /* Best effort; inventory must never stop the application test. */ }

        using var automation = new UIA3Automation();
        using var app = Application.Launch(new ProcessStartInfo
        {
            FileName = FindExecutable(),
            UseShellExecute = false,
            CreateNoWindow = false
        });

        var window = app.GetMainWindow(automation, TimeSpan.FromSeconds(60))
            ?? throw new InvalidOperationException("A janela principal do Optimize não apareceu no tempo esperado.");

        var treeReady = Retry.WhileFalse(
            () => window.FindAllDescendants().Length >= 20,
            TimeSpan.FromSeconds(25),
            TimeSpan.FromMilliseconds(250)).Success;

        Assert.True(treeReady, "A árvore visual do Optimize não terminou de carregar.\n" + DescribeTree(window));
        ExpandAllNavGroups(window);

        WriteInventoryHeader(inventoryPath);
        CaptureSurfaceInventory(window, "nav-dashboard", "Visão geral", inventoryPath);

        foreach (var row in AllTabsSmokeUiTests.AllTabs())
        {
            var navId = (string)row[0];
            var expectedHeader = (string)row[1];
            var visibleLabel = NavigationLabelOverrides.GetValueOrDefault(
                navId,
                PtBrLocalizationService.Translate(expectedHeader));

            var item = Retry.WhileNull(
                () => FindNavigationItem(window, navId, visibleLabel, expectedHeader),
                TimeSpan.FromSeconds(20),
                TimeSpan.FromMilliseconds(250)).Result;

            if (item is null)
            {
                ExpandAllNavGroups(window);
                item = Retry.WhileNull(
                    () => FindNavigationItem(window, navId, visibleLabel, expectedHeader),
                    TimeSpan.FromSeconds(10),
                    TimeSpan.FromMilliseconds(250)).Result;
            }

            Assert.True(
                item is not null,
                $"Item de navegação '{navId}' / '{visibleLabel}' não encontrado.\n" + DescribeTree(window));

            item!.Click();
            Thread.Sleep(450);
            CaptureSurfaceInventory(window, navId, visibleLabel, inventoryPath);
        }

        // Open common disconnected surfaces so Popup/ContextMenu/ToolTip translation is exercised.
        OpenFirstAvailableComboBox(window);
        OpenFirstAvailableMenu(window);
        Thread.Sleep(700);
        CaptureSurfaceInventory(window, "disconnected-surfaces", "Pop-ups e menus", inventoryPath);

        var findings = ReadAuditFindings(auditPath);
        Assert.True(
            findings.Count == 0,
            "A interface executada ainda contém possíveis textos em inglês:\n" +
            string.Join("\n", findings.Take(200)));
    }

    private static void WriteInventoryHeader(string inventoryPath)
    {
        File.AppendAllText(
            inventoryPath,
            "OPTIMIZE · CENSO BRUTO DA INTERFACE RENDERIZADA\n" +
            "Gerado por UI Automation. Nenhuma classificação de idioma é aplicada aqui.\n" +
            "Cada seção corresponde a uma página realmente aberta pelo teste.\n" +
            new string('=', 88) + "\n\n");
    }

    private static void CaptureSurfaceInventory(
        Window window,
        string pageId,
        string pageLabel,
        string inventoryPath)
    {
        try
        {
            var rows = window.FindAllDescendants()
                .Select(element => new
                {
                    Type = element.ControlType.ToString(),
                    Id = element.AutomationId?.Trim() ?? string.Empty,
                    Name = element.Name?.Trim() ?? string.Empty,
                })
                .Where(row => !string.IsNullOrWhiteSpace(row.Name))
                .Where(row => row.Name.Length <= 600)
                .DistinctBy(row => $"{row.Type}|{row.Id}|{row.Name}", StringComparer.Ordinal)
                .OrderBy(row => row.Type, StringComparer.Ordinal)
                .ThenBy(row => row.Name, StringComparer.OrdinalIgnoreCase)
                .ToArray();

            var lines = new List<string>
            {
                $"### {pageId} · {pageLabel}",
                $"Elementos textuais expostos: {rows.Length}",
            };

            lines.AddRange(rows.Select(row =>
                $"[{row.Type}] id='{row.Id}' :: {NormalizeInventoryText(row.Name)}"));
            lines.Add(string.Empty);

            File.AppendAllLines(inventoryPath, lines);
        }
        catch (Exception ex)
        {
            File.AppendAllText(
                inventoryPath,
                $"### {pageId} · {pageLabel}\n[FALHA AO INVENTARIAR] {ex.GetType().Name}: {ex.Message}\n\n");
        }
    }

    private static string NormalizeInventoryText(string text)
        => string.Join(" ", text
            .Replace("\r", " ", StringComparison.Ordinal)
            .Replace("\n", " ", StringComparison.Ordinal)
            .Split(' ', StringSplitOptions.RemoveEmptyEntries));

    private static AutomationElement? FindNavigationItem(
        Window window,
        string navId,
        string visibleLabel,
        string originalHeader)
    {
        var byId = window.FindFirstDescendant(cf => cf.ByAutomationId(navId));
        if (byId is not null) return byId;

        // Some templated WPF containers do not surface AutomationId through UIA. Search both
        // the translated label and the inherited source label; the latter is useful precisely
        // when the localization layer has missed a navigation surface.
        return window.FindAllDescendants()
            .FirstOrDefault(element => NameMatches(element.Name, visibleLabel))
            ?? window.FindAllDescendants()
                .FirstOrDefault(element => NameMatches(element.Name, originalHeader));
    }

    private static bool NameMatches(string? candidate, string expected)
    {
        if (string.IsNullOrWhiteSpace(candidate) || string.IsNullOrWhiteSpace(expected)) return false;
        var name = candidate.Trim();
        return string.Equals(name, expected, StringComparison.OrdinalIgnoreCase)
            || name.Contains(expected, StringComparison.OrdinalIgnoreCase);
    }

    private static void ExpandAllNavGroups(Window window)
    {
        foreach (var element in window.FindAllDescendants(cf => cf.ByControlType(ControlType.Group)))
        {
            try
            {
                var pattern = element.Patterns.ExpandCollapse.PatternOrDefault;
                if (pattern is not null && pattern.ExpandCollapseState.Value == ExpandCollapseState.Collapsed)
                    pattern.Expand();
            }
            catch (Exception)
            {
                // Not every Group control is an expandable navigation group.
            }
        }

        Thread.Sleep(600);
    }

    private static void OpenFirstAvailableComboBox(Window window)
    {
        var combo = window.FindFirstDescendant(cf => cf.ByControlType(ControlType.ComboBox));
        if (combo is null) return;
        try
        {
            combo.AsComboBox().Expand();
            Thread.Sleep(250);
            combo.AsComboBox().Collapse();
        }
        catch (Exception)
        {
            // Some custom combo boxes do not expose ExpandCollapse through UIA.
        }
    }

    private static void OpenFirstAvailableMenu(Window window)
    {
        var menu = window.FindFirstDescendant(cf => cf.ByControlType(ControlType.MenuItem));
        if (menu is null) return;
        try
        {
            menu.Click();
            Thread.Sleep(250);
        }
        catch (Exception)
        {
            // Optional surface; tab traversal remains the required part of the audit.
        }
    }

    private static string DescribeTree(Window window)
    {
        try
        {
            return "Árvore de automação (primeiros 180 elementos):\n" + string.Join(
                "\n",
                window.FindAllDescendants()
                    .Take(180)
                    .Select(element =>
                        $"[{element.ControlType}] id='{element.AutomationId}' nome='{element.Name}'"));
        }
        catch (Exception ex)
        {
            return $"Não foi possível descrever a árvore: {ex.Message}";
        }
    }

    private static List<string> ReadAuditFindings(string auditPath)
    {
        if (!File.Exists(auditPath))
            return ["O arquivo de auditoria em tempo real não foi criado."];

        return File.ReadAllLines(auditPath)
            .Where(line => line.StartsWith("[", StringComparison.Ordinal))
            .Distinct(StringComparer.Ordinal)
            .ToList();
    }

    private static string GetRepositoryRoot()
        => Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));

    private static string FindExecutable()
    {
        var repositoryRoot = GetRepositoryRoot();

        foreach (var configuration in new[] { "Release", "Debug" })
        {
            var binDirectory = Path.Combine(repositoryRoot, "SysManager", "bin", configuration);
            if (!Directory.Exists(binDirectory)) continue;

            var candidate = Directory
                .EnumerateDirectories(binDirectory, "net*-windows")
                .Select(targetFramework => Path.Combine(targetFramework, "Optimize.exe"))
                .FirstOrDefault(File.Exists);

            if (candidate is not null) return candidate;
        }

        throw new FileNotFoundException("Optimize.exe não foi encontrado. Compile o aplicativo antes da auditoria de interface.");
    }
}
