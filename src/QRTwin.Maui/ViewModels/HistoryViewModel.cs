namespace QRTwin.Maui.ViewModels;

public partial class HistoryViewModel(IHistoryService historyService) : ObservableObject
{
    [ObservableProperty]
    public partial bool IsLoading { get; set; }

    public ObservableCollection<HistoryEntry> Entries { get; } = [];

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
        }
    }

    [RelayCommand]
    private async Task DeleteEntryAsync(HistoryEntry entry)
    {
        await historyService.DeleteAsync(entry.Id).ConfigureAwait(false);
        await MainThread.InvokeOnMainThreadAsync(() => Entries.Remove(entry));
    }

    [RelayCommand]
    private async Task ClearAllAsync()
    {
        await historyService.ClearAllAsync().ConfigureAwait(false);
        await MainThread.InvokeOnMainThreadAsync(Entries.Clear);
    }
}
