// SysManager · PerformanceView — performance mode UI
// Author: laurentiu021 · https://github.com/laurentiu021/SystemManager
// License: MIT

using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using SysManager.ViewModels;

namespace SysManager.Views;

public partial class PerformanceView : UserControl
{
    private bool _legacyActionsLocked;

    public PerformanceView()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (_legacyActionsLocked) return;
        _legacyActionsLocked = true;

        // Optimize safety rule: the upstream Performance page contains generic system tweaks.
        // Until every individual action is routed through OptimizationCompatibilityService,
        // fail closed instead of exposing hardware-agnostic buttons. Read-only metrics/state
        // remain visible; the hardware-aware Gaming Profile and Optimize Missions are the safe
        // paths for applying performance changes during this migration.
        DisableLegacyActionButtons(this);

        if (DataContext is PerformanceViewModel vm)
        {
            vm.StatusMessage =
                "Alterações genéricas desta tela estão temporariamente bloqueadas pelo Optimize. " +
                "Use as Missões do Optimize ou o Perfil para jogos; eles validam o hardware antes de aplicar mudanças.";
        }
    }

    private static void DisableLegacyActionButtons(DependencyObject root)
    {
        var count = VisualTreeHelper.GetChildrenCount(root);
        for (var i = 0; i < count; i++)
        {
            var child = VisualTreeHelper.GetChild(root, i);
            if (child is ButtonBase button)
            {
                button.IsEnabled = false;
                button.ToolTip =
                    "Bloqueado até esta ação receber validação específica para o hardware deste PC.";
            }

            DisableLegacyActionButtons(child);
        }
    }
}
