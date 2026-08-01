using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Optimize.Core.Models;
using Optimize.Core.Services;

namespace Optimize.App.ViewModels;

public sealed class MainViewModel : INotifyPropertyChanged
{
    private readonly ISystemInspectionService _inspectionService;
    private string _statusText = "Pronto para analisar este computador.";
    private string _healthLabel = "Não analisado";
    private string _systemSummary = "Execute o primeiro diagnóstico.";
    private string _processor = "Aguardando leitura";
    private string _graphicsAdapter = "Aguardando leitura";
    private string _memorySummary = "Aguardando leitura";
    private string _processSummary = "Aguardando leitura";
    private string _startupSummary = "Aguardando leitura";
    private string _uptimeSummary = "Aguardando leitura";
    private string _lastScanText = "Nenhuma análise executada";
    private int _score;
    private bool _isScanning;

    public MainViewModel(ISystemInspectionService inspectionService)
    {
        _inspectionService = inspectionService;
        ScanCommand = new AsyncRelayCommand(ScanAsync, () => !IsScanning);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public AsyncRelayCommand ScanCommand { get; }

    public ObservableCollection<DriveSnapshot> Drives { get; } = new();

    public ObservableCollection<Recommendation> Recommendations { get; } = new();

    public string StatusText
    {
        get => _statusText;
        private set => SetField(ref _statusText, value);
    }

    public string HealthLabel
    {
        get => _healthLabel;
        private set => SetField(ref _healthLabel, value);
    }

    public string SystemSummary
    {
        get => _systemSummary;
        private set => SetField(ref _systemSummary, value);
    }

    public string Processor
    {
        get => _processor;
        private set => SetField(ref _processor, value);
    }

    public string GraphicsAdapter
    {
        get => _graphicsAdapter;
        private set => SetField(ref _graphicsAdapter, value);
    }

    public string MemorySummary
    {
        get => _memorySummary;
        private set => SetField(ref _memorySummary, value);
    }

    public string ProcessSummary
    {
        get => _processSummary;
        private set => SetField(ref _processSummary, value);
    }

    public string StartupSummary
    {
        get => _startupSummary;
        private set => SetField(ref _startupSummary, value);
    }

    public string UptimeSummary
    {
        get => _uptimeSummary;
        private set => SetField(ref _uptimeSummary, value);
    }

    public string LastScanText
    {
        get => _lastScanText;
        private set => SetField(ref _lastScanText, value);
    }

    public int Score
    {
        get => _score;
        private set => SetField(ref _score, value);
    }

    public bool IsScanning
    {
        get => _isScanning;
        private set
        {
            if (SetField(ref _isScanning, value))
            {
                OnPropertyChanged(nameof(ScanButtonText));
                ScanCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public string ScanButtonText => IsScanning ? "Analisando..." : "Escanear computador";

    private async Task ScanAsync()
    {
        IsScanning = true;
        StatusText = "Coletando informações do Windows sem alterar configurações...";

        try
        {
            SystemSnapshot snapshot = await _inspectionService.InspectAsync().ConfigureAwait(true);
            ApplySnapshot(snapshot);
            StatusText = $"Análise concluída. {snapshot.Recommendations.Count} ponto(s) para revisar.";
        }
        catch (Exception exception)
        {
            StatusText = $"Não foi possível concluir a análise: {exception.Message}";
        }
        finally
        {
            IsScanning = false;
        }
    }

    private void ApplySnapshot(SystemSnapshot snapshot)
    {
        Score = snapshot.Score;
        HealthLabel = snapshot.HealthLabel;
        SystemSummary = $"{snapshot.OperatingSystem} · {snapshot.OperatingSystemVersion}";
        Processor = $"{snapshot.Processor} · {snapshot.LogicalProcessorCount} processadores lógicos";
        GraphicsAdapter = snapshot.GraphicsAdapter;

        double usedMemory = Math.Max(0, snapshot.TotalMemoryGb - snapshot.AvailableMemoryGb);
        MemorySummary = $"{usedMemory:0.0} GB em uso de {snapshot.TotalMemoryGb:0.0} GB · {snapshot.MemoryUsagePercent:0}%";
        ProcessSummary = $"{snapshot.RunningProcessCount} processos em execução";
        StartupSummary = $"{snapshot.StartupItemCount} entradas básicas de inicialização";
        UptimeSummary = $"{snapshot.Uptime.Days} dias e {snapshot.Uptime.Hours} horas desde a última inicialização";
        LastScanText = $"Última análise: {snapshot.CapturedAt:dd/MM/yyyy HH:mm}";

        Drives.Clear();
        foreach (DriveSnapshot drive in snapshot.Drives)
        {
            Drives.Add(drive);
        }

        Recommendations.Clear();
        foreach (Recommendation recommendation in snapshot.Recommendations)
        {
            Recommendations.Add(recommendation);
        }
    }

    private bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
        {
            return false;
        }

        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
