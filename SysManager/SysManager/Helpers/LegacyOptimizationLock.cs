// Optimize · temporary fail-closed guard for inherited optimization screens
// Original project: laurentiu021/SystemManager · MIT License

using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using SysManager.ViewModels;

namespace SysManager.Helpers;

/// <summary>
/// During the migration, any inherited optimization screen that has not yet been routed
/// through OptimizationCompatibilityService is read-only. This helper makes that state
/// explicit instead of leaving generic tweak buttons usable by accident.
/// </summary>
public static class LegacyOptimizationLock
{
    public static void Apply(UserControl root, string reason)
    {
        DisableActionButtons(root);
        if (root.DataContext is ViewModelBase vm)
            vm.StatusMessage = reason;
    }

    private static void DisableActionButtons(DependencyObject root)
    {
        var count = VisualTreeHelper.GetChildrenCount(root);
        for (var i = 0; i < count; i++)
        {
            var child = VisualTreeHelper.GetChild(root, i);
            if (child is ButtonBase button)
            {
                button.IsEnabled = false;
                button.ToolTip = "Bloqueado até esta ação passar pela validação de compatibilidade do Optimize.";
            }
            DisableActionButtons(child);
        }
    }
}
