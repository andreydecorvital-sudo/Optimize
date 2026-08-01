// SysManager · DashboardView.xaml
// Author: laurentiu021 · https://github.com/laurentiu021/SystemManager
// License: MIT

using System.Windows;
using System.Windows.Controls;

namespace SysManager.Views;

public partial class DashboardView : UserControl
{
    private bool _missionsInjected;

    public DashboardView()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (_missionsInjected) return;
        _missionsInjected = true;

        // Keep the large upstream XAML intact while Optimize is being progressively reshaped.
        // The missions panel is appended to the Dashboard grid and inherits the same DataContext.
        if (Content is not ScrollViewer { Content: Grid dashboardGrid }) return;

        var rowIndex = dashboardGrid.RowDefinitions.Count;
        dashboardGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        var missions = new OptimizeMissionsPanel();
        Grid.SetRow(missions, rowIndex);
        dashboardGrid.Children.Add(missions);
    }
}
