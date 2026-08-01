using System.Windows;
using Optimize.App.ViewModels;
using Optimize.Core.Services;

namespace Optimize.App;

public partial class MainWindow : Window
{
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
}
