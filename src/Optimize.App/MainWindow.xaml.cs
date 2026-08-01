using System.Windows;
using Optimize.App.ViewModels;
using Optimize.Core.Services;

namespace Optimize.App;

public partial class MainWindow : Window
{
    private const string MaximizeGlyph = "\uE922";
    private const string RestoreGlyph = "\uE923";

    private readonly MainViewModel _viewModel;

    public MainWindow()
    {
        InitializeComponent();

        _viewModel = new MainViewModel(new SystemInspectionService());
        DataContext = _viewModel;
        Loaded += HandleLoaded;
    }

    private void HandleLoaded(object sender, RoutedEventArgs eventArgs)
    {
        Loaded -= HandleLoaded;

        if (_viewModel.ScanCommand.CanExecute(null))
        {
            _viewModel.ScanCommand.Execute(null);
        }
    }

    private void MinimizeWindow(object sender, RoutedEventArgs eventArgs)
    {
        WindowState = WindowState.Minimized;
    }

    private void ToggleMaximizeWindow(object sender, RoutedEventArgs eventArgs)
    {
        WindowState = WindowState == WindowState.Maximized
            ? WindowState.Normal
            : WindowState.Maximized;
    }

    private void CloseWindow(object sender, RoutedEventArgs eventArgs)
    {
        Close();
    }

    private void HandleWindowStateChanged(object? sender, EventArgs eventArgs)
    {
        if (MaximizeButton is null)
        {
            return;
        }

        MaximizeButton.Content = WindowState == WindowState.Maximized
            ? RestoreGlyph
            : MaximizeGlyph;
    }
}
