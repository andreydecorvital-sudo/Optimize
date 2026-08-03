// Optimize · live pt-BR UI audit
// Original project: laurentiu021/SystemManager · MIT License

using System.Diagnostics;
using FlaUI.Core;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Definitions;
using FlaUI.Core.Tools;
using FlaUI.UIA3;

namespace SysManager.UITests;

/// <summary>
/// Launches the real application, visits every navigable tab and then reads the audit produced
/// by PtBrLiveAuditService. This verifies the rendered interface rather than only source literals.
/// </summary>
public sealed class PtBrSurfaceUiTests
{
    [Fact]
    public void EveryNavigableSurface_HasNoDetectedEnglishUiText()
    {
        var auditPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Optimize",
            "ptbr-live-audit.log");

        try { if (File.Exists(auditPath)) File.Delete(auditPath); }
        catch (IOException) { /* A previous crashed test may still be releasing the handle. */ }

        using var automation = new UIA3Automation();
        using var app = Application.Launch(new ProcessStartInfo
        {
            FileName = FindExecutable(),
            UseShellExecute = false,
            CreateNoWindow = false
        });

        var window = app.GetMainWindow(automation, TimeSpan.FromSeconds(45))
            ?? throw new InvalidOperationException("A janela principal do Optimize não apareceu no tempo esperado.");

        ExpandAllNavGroups(window);

        foreach (var row in AllTabsSmokeUiTests.AllTabs())
        {
            var navId = (string)row[0];
            var item = window.FindFirstDescendant(cf => cf.ByAutomationId(navId));
            if (item is null)
            {
                ExpandAllNavGroups(window);
                item = Retry.WhileNull(
                    () => window.FindFirstDescendant(cf => cf.ByAutomationId(navId)),
                    TimeSpan.FromSeconds(5)).Result;
            }

            Assert.True(item is not null, $"Item de navegação '{navId}' não encontrado.");
            item!.Click();
            Thread.Sleep(220);
        }

        // Open common disconnected surfaces so Popup/ContextMenu/ToolTip translation is exercised.
        OpenFirstAvailableComboBox(window);
        OpenFirstAvailableMenu(window);
        Thread.Sleep(500);

        var findings = ReadAuditFindings(auditPath);
        Assert.True(
            findings.Count == 0,
            "A interface executada ainda contém possíveis textos em inglês:\n" +
            string.Join("\n", findings.Take(150)));
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

        Thread.Sleep(400);
    }

    private static void OpenFirstAvailableComboBox(Window window)
    {
        var combo = window.FindFirstDescendant(cf => cf.ByControlType(ControlType.ComboBox));
        if (combo is null) return;
        try
        {
            combo.AsComboBox().Expand();
            Thread.Sleep(200);
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
            Thread.Sleep(200);
        }
        catch (Exception)
        {
            // Optional surface; tab traversal remains the required part of the audit.
        }
    }

    private static List<string> ReadAuditFindings(string auditPath)
    {
        if (!File.Exists(auditPath))
            return ["O arquivo de auditoria em tempo real não foi criado."];

        return File.ReadAllLines(auditPath)
            .Where(line => line.StartsWith('[', StringComparison.Ordinal))
            .Distinct(StringComparer.Ordinal)
            .ToList();
    }

    private static string FindExecutable()
    {
        var repositoryRoot = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory, "..", "..", "..", ".."));

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
