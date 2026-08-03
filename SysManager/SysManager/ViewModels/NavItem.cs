// SysManager · NavItem
// Author: laurentiu021 · https://github.com/laurentiu021/SystemManager
// License: MIT

using System.ComponentModel;
using System.Windows.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using SysManager.Services;

namespace SysManager.ViewModels;

/// <summary>
/// A single entry in the left nav. Both the <see cref="View"/> AND the underlying
/// ViewModel (<see cref="Content"/>) are materialised lazily on first access, so the
/// tab view-models are not all built at startup.
/// </summary>
public sealed partial class NavItem : ObservableObject, IDisposable
{
    private UserControl? _view;
    private object? _content;
    private Func<object>? _contentFactory;
    private string _label = string.Empty;

    public required string Id { get; init; }

    public required string Label
    {
        get => PtBrLocalizationService.Translate(_label);
        init => _label = value;
    }

    public required string Glyph { get; init; }
    public required Type ViewType { get; init; }

    public object Content
    {
        get
        {
            if (_content is not null) return _content;

            _content = _contentFactory?.Invoke()
                ?? throw new InvalidOperationException(
                    $"NavItem '{Id}' has neither an eager Content nor a ContentFactory set.");
            WireBusy(_content);
            return _content;
        }
        init
        {
            _content = value;
        }
    }

    public Func<object>? ContentFactory
    {
        private get => _contentFactory;
        init => _contentFactory = value;
    }

    public bool IsContentCreated => _content is not null;

    public bool IsInDevelopment { get; init; }

    [ObservableProperty] private bool _isBusy;

    public NavItem WireBusy()
    {
        if (_content is not null) WireBusy(_content);
        return this;
    }

    private void WireBusy(object content)
    {
        if (content is ViewModelBase vm)
        {
            vm.PropertyChanged -= OnViewModelPropertyChanged;
            vm.PropertyChanged += OnViewModelPropertyChanged;
            IsBusy = vm.IsBusy;
        }
    }

    public void Dispose()
    {
        if (_content is null) return;
        if (_content is ViewModelBase vm)
            vm.PropertyChanged -= OnViewModelPropertyChanged;
        (_content as IDisposable)?.Dispose();
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ViewModelBase.IsBusy) && sender is ViewModelBase vm)
            IsBusy = vm.IsBusy;
    }

    public UserControl View
    {
        get
        {
            if (_view is not null) return _view;
            _view = (UserControl)Activator.CreateInstance(ViewType)!;
            _view.DataContext = Content;
            return _view;
        }
    }
}
