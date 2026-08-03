// Optimize · missions panel actions
// Based on SysManager (MIT) — original license preserved in repository.

using System.Windows;
using System.Windows.Controls;
using SysManager.Models;
using SysManager.ViewModels;

namespace SysManager.Views;

public partial class OptimizeMissionsPanel : UserControl
{
    public OptimizeMissionsPanel()
    {
        InitializeComponent();
    }

    private void MissionAction_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { DataContext: OptimizationMission mission }) return;

        if (mission.Id == "diagnostic-admin" && DataContext is DashboardViewModel dashboard)
        {
            if (dashboard.RelaunchAsAdminCommand.CanExecute(null))
                dashboard.RelaunchAsAdminCommand.Execute(null);
            return;
        }

        if (Window.GetWindow(this)?.DataContext is MainWindowViewModel shell)
            shell.NavigateTo(mission.TargetNavId);
    }
}
