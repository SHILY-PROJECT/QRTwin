using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using QRTwin.Maui.Models;
using QRTwin.Maui.Services;

namespace QRTwin.Maui.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly IHistoryService _historyService;

    [ObservableProperty]
    private AppTab _selectedTab = AppTab.Scan;

    [ObservableProperty]
    private bool _isHistoryVisible;

    public ScanViewModel Scan { get; }

    public GenerateViewModel Generate { get; }

    public HistoryViewModel History { get; }

    public MainViewModel(
        ScanViewModel scanViewModel,
        GenerateViewModel generateViewModel,
        HistoryViewModel historyViewModel,
        IHistoryService historyService)
    {
        Scan = scanViewModel;
        Generate = generateViewModel;
        History = historyViewModel;
        _historyService = historyService;

        Scan.HistorySaved += OnHistorySaved;
        Generate.HistorySaved += OnHistorySaved;
        Scan.IsActive = true;
    }

    partial void OnSelectedTabChanged(AppTab value)
    {
        if (value == AppTab.Scan)
        {
            Generate.InputText = string.Empty;
            Scan.IsActive = true;
            Generate.IsActive = false;
        }
        else
        {
            Scan.IsActive = false;
            Generate.IsActive = true;
        }
    }

    [RelayCommand]
    private async Task OpenHistoryAsync()
    {
        await History.LoadAsync().ConfigureAwait(false);
        IsHistoryVisible = true;
    }

    [RelayCommand]
    private void CloseHistory()
    {
        IsHistoryVisible = false;
    }

    private async void OnHistorySaved(object? sender, EventArgs e)
    {
        if (IsHistoryVisible)
        {
            await History.LoadAsync().ConfigureAwait(false);
        }
    }
}
