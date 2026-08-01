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

        // Missions are the primary Optimize experience: after the header/admin context,
        // show what this specific PC should do before exposing raw metrics/tools.
        if (Content is not ScrollViewer { Content: Grid dashboardGrid }) return;

        const int missionRow = 2;
        dashboardGrid.RowDefinitions.Insert(missionRow, new RowDefinition { Height = GridLength.Auto });

        foreach (UIElement child in dashboardGrid.Children)
        {
            var row = Grid.GetRow(child);
            if (row >= missionRow)
                Grid.SetRow(child, row + 1);
        }

        var missions = new OptimizeMissionsPanel();
        Grid.SetRow(missions, missionRow);
        dashboardGrid.Children.Add(missions);
    }
}
