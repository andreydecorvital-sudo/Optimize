// SysManager · DialogService — WPF MessageBox implementation of IDialogService
// Author: laurentiu021 · https://github.com/laurentiu021/SystemManager
// License: MIT

using System.Windows;

namespace SysManager.Services;

/// <summary>
/// Production implementation of <see cref="IDialogService"/> using WPF MessageBox.
/// All user-facing dialog text passes through the Optimize pt-BR catalog so inherited
/// confirmations cannot bypass localization.
/// </summary>
public sealed class DialogService : IDialogService
{
    /// <summary>Shared singleton instance for ViewModels without DI.</summary>
    private static volatile IDialogService _instance = new DialogService();
    public static IDialogService Instance
    {
        get => _instance;
        set => _instance = value ?? throw new ArgumentNullException(nameof(value));
    }

    /// <inheritdoc/>
    public bool Confirm(string message, string title)
    {
        if (Application.Current == null) return false;
        var localizedMessage = PtBrLocalizationService.Translate(message);
        var localizedTitle = PtBrLocalizationService.Translate(title);
        var result = MessageBox.Show(localizedMessage, localizedTitle, MessageBoxButton.YesNo, MessageBoxImage.Question);
        return result == MessageBoxResult.Yes;
    }
}
