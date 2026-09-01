using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using QRTwin.Maui.Models;

namespace QRTwin.Maui.ViewModels;

public partial class MainViewModel : ObservableObject
{
    public ScanViewModel Scan { get; }

    public GenerateViewModel Generate { get; }

    public HistoryViewModel History { get; }

    public MainViewModel(
        ScanViewModel scanViewModel,
        GenerateViewModel generateViewModel,
        HistoryViewModel historyViewModel)
    {
        Scan = scanViewModel;
        Generate = generateViewModel;
        History = historyViewModel;

        Scan.HistorySaved += OnHistorySaved;
        Generate.HistorySaved += OnHistorySaved;
        Scan.IsActive = true;
        SelectedTab = AppTab.Scan;
    }

    [ObservableProperty]
    public partial AppTab SelectedTab { get; set; }

    [ObservableProperty]
    public partial bool IsHistoryVisible { get; set; }

    partial void OnSelectedTabChanged(AppTab value)
    {
        switch (value)
        {
            case AppTab.Scan:
                Generate.InputText = string.Empty;
                Scan.IsActive = true;
                Generate.IsActive = false;
                break;
            case AppTab.Generate:
                Scan.IsActive = false;
                Generate.IsActive = true;
                break;
        }
    }

    [RelayCommand]
    private async Task OpenHistoryAsync()
    {
        await History.LoadAsync().ConfigureAwait(false);
        IsHistoryVisible = true;
    }

    [RelayCommand]
    private void CloseHistory() => IsHistoryVisible = false;

    private async void OnHistorySaved(object? sender, EventArgs e)
    {
        if (IsHistoryVisible)
        {
            await History.LoadAsync().ConfigureAwait(false);
        }
    }
}
