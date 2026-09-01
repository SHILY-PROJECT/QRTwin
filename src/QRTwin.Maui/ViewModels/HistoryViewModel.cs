namespace QRTwin.Maui.ViewModels;

public partial class HistoryViewModel(IHistoryService historyService) : ObservableObject
{
    public event EventHandler<HistoryEntry>? EntrySelected;

    [ObservableProperty]
    public partial bool IsLoading { get; set; }

    public ObservableCollection<HistoryEntry> Entries { get; } = [];

    public bool HasEntries => Entries.Count > 0;

    public bool ShowEmptyState => !IsLoading && Entries.Count == 0;

    public async Task LoadAsync()
    {
        IsLoading = true;

        try
        {
            var collected = new List<HistoryEntry>();
            await foreach (var entry in historyService.StreamAllAsync().ConfigureAwait(false))
            {
                collected.Add(entry);
            }

            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                Entries.Clear();
                foreach (var entry in collected)
                {
                    Entries.Add(entry);
                }
            });
        }
        finally
        {
            IsLoading = false;
            NotifyEntriesChanged();
        }
    }

    partial void OnIsLoadingChanged(bool value) => NotifyEntriesChanged();

    private void NotifyEntriesChanged()
    {
        OnPropertyChanged(nameof(HasEntries));
        OnPropertyChanged(nameof(ShowEmptyState));
    }

    [RelayCommand]
    private void SelectEntry(HistoryEntry entry) =>
        EntrySelected?.Invoke(this, entry);

    [RelayCommand]
    private async Task DeleteEntryAsync(HistoryEntry entry)
    {
        await historyService.DeleteAsync(entry.Id).ConfigureAwait(false);
        await MainThread.InvokeOnMainThreadAsync(() =>
        {
            Entries.Remove(entry);
            NotifyEntriesChanged();
        });
    }

    [RelayCommand]
    private async Task ClearAllAsync()
    {
        await historyService.ClearAllAsync().ConfigureAwait(false);
        await MainThread.InvokeOnMainThreadAsync(() =>
        {
            Entries.Clear();
            NotifyEntriesChanged();
        });
    }
}
