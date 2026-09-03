using QRTwin.Models;
using QRTwin.Services;

namespace QRTwin.ViewModels;

public partial class MainViewModel(
    ScanViewModel scan,
    GenerateViewModel generate,
    HistoryViewModel history,
    ThemesViewModel themes,
    IThemeService themeService) : ObservableObject
{
    public ScanViewModel Scan { get; } = scan;

    public GenerateViewModel Generate { get; } = generate;

    public HistoryViewModel History { get; } = history;

    public ThemesViewModel Themes { get; } = themes;

    [ObservableProperty]
    public partial AppTab SelectedTab { get; set; }

    [ObservableProperty]
    public partial bool IsHistoryVisible { get; set; }

    [ObservableProperty]
    public partial bool IsThemesVisible { get; set; }

    public void Initialize()
    {
        Scan.HistorySaved += OnHistorySaved;
        Generate.HistorySaved += OnHistorySaved;
        History.EntrySelected += OnHistoryEntrySelected;
        themeService.ThemeChanged += OnThemeChanged;
        Scan.IsActive = true;
        SelectedTab = AppTab.Scan;
        Themes.Refresh();
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
        await MainThread.InvokeOnMainThreadAsync(() => IsThemesVisible = false);
        await History.LoadAsync().ConfigureAwait(false);
        await MainThread.InvokeOnMainThreadAsync(() =>
        {
            // Toggle guarantees PropertyChanged even if a prior open failed mid-animation.
            IsHistoryVisible = false;
            IsHistoryVisible = true;
        });
    }

    [RelayCommand]
    private void CloseHistory() => IsHistoryVisible = false;

    [RelayCommand]
    private void OpenThemes()
    {
        IsHistoryVisible = false;
        Themes.Refresh();
        IsThemesVisible = true;
    }

    [RelayCommand]
    private void CloseThemes() => IsThemesVisible = false;

    private void OnThemeChanged(object? sender, EventArgs e) => Themes.Refresh();

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
