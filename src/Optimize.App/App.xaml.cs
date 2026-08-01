using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Threading;

namespace Optimize.App;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        DispatcherUnhandledException += HandleDispatcherException;
        AppDomain.CurrentDomain.UnhandledException += HandleDomainException;
        TaskScheduler.UnobservedTaskException += HandleTaskException;

        try
        {
            var window = new MainWindow();
            MainWindow = window;
            window.Show();
        }
        catch (Exception exception)
        {
            ShowFatalError(exception);
            Shutdown(-1);
        }
    }

    private void HandleDispatcherException(object sender, DispatcherUnhandledExceptionEventArgs eventArgs)
    {
        ShowFatalError(eventArgs.Exception);
        eventArgs.Handled = true;
        Shutdown(-1);
    }

    private void HandleDomainException(object? sender, UnhandledExceptionEventArgs eventArgs)
    {
        if (eventArgs.ExceptionObject is Exception exception)
        {
            WriteCrashLog(exception);
        }
    }

    private void HandleTaskException(object? sender, UnobservedTaskExceptionEventArgs eventArgs)
    {
        WriteCrashLog(eventArgs.Exception);
        eventArgs.SetObserved();
    }

    private static void ShowFatalError(Exception exception)
    {
        string logPath = WriteCrashLog(exception);

        MessageBox.Show(
            $"O Optimize encontrou um erro ao iniciar.\n\nDetalhes: {exception.Message}\n\nUm relatório foi salvo em:\n{logPath}",
            "Optimize — erro de inicialização",
            MessageBoxButton.OK,
            MessageBoxImage.Error);
    }

    private static string WriteCrashLog(Exception exception)
    {
        string directory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Optimize");
        Directory.CreateDirectory(directory);

        string path = Path.Combine(directory, "crash.log");
        var content = new StringBuilder()
            .AppendLine($"Data: {DateTime.Now:yyyy-MM-dd HH:mm:ss}")
            .AppendLine($"Windows: {Environment.OSVersion}")
            .AppendLine($"Processo 64 bits: {Environment.Is64BitProcess}")
            .AppendLine()
            .AppendLine(exception.ToString())
            .AppendLine(new string('-', 80))
            .ToString();

        File.AppendAllText(path, content, Encoding.UTF8);
        return path;
    }
}