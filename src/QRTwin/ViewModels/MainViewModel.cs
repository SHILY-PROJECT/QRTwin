using QRTwin.Models;

namespace QRTwin.ViewModels;

public partial class MainViewModel(
    ScanViewModel scan,
    GenerateViewModel generate,
    HistoryViewModel history) : ObservableObject
{
    public ScanViewModel Scan { get; } = scan;

    public GenerateViewModel Generate { get; } = generate;

    public HistoryViewModel History { get; } = history;

    [ObservableProperty]
    public partial AppTab SelectedTab { get; set; }

    [ObservableProperty]
    public partial bool IsHistoryVisible { get; set; }

    public void Initialize()
    {
        Scan.HistorySaved += OnHistorySaved;
        Generate.HistorySaved += OnHistorySaved;
        History.EntrySelected += OnHistoryEntrySelected;
        Scan.IsActive = true;
        SelectedTab = AppTab.Scan;
    }

    partial void OnSelectedTabChanged(AppTab value)
    {
        switch (value)
        {
            case AppTab.Scan:
                generate.ClearFromUi();
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

    private async void OnHistoryEntrySelected(object? sender, HistoryEntry entry)
    {
        IsHistoryVisible = false;

        switch (entry.EntryType)
        {
            case HistoryEntryType.Scan:
                SelectedTab = AppTab.Scan;
                Scan.RestoreFromHistory(entry.Content);
                break;
            case HistoryEntryType.Generate:
                SelectedTab = AppTab.Generate;
                await Generate.RestoreFromHistoryAsync(entry.Content).ConfigureAwait(false);
                break;
        }
    }
}
