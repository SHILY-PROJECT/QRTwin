using QRTwin.Models;
using QRTwin.Services;
using QRTwin.Views;

namespace QRTwin.ViewModels;

public partial class HistoryViewModel(IHistoryService historyService) : ObservableObject
{
    private const int ClearAllStaggerMs = 45;

    public event EventHandler<HistoryEntry>? EntrySelected;

    [ObservableProperty]
    public partial bool IsLoading { get; set; }

    public ObservableCollection<HistoryEntryItem> EntryItems { get; } = [];

    public bool HasEntries => EntryItems.Count > 0;

    public bool ShowEmptyState => !IsLoading && EntryItems.Count == 0;

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
                EntryItems.Clear();
                for (var index = 0; index < collected.Count; index++)
                {
                    EntryItems.Add(CreateItem(collected[index], index));
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
    private void SelectEntry(HistoryEntryItem item) =>
        EntrySelected?.Invoke(this, item.Entry);

    [RelayCommand]
    private async Task DeleteEntryAsync(HistoryEntryItem item)
    {
        await MainThread.InvokeOnMainThreadAsync(() => HistoryEntryCard.AnimateOutIfPresentAsync(item));
        await historyService.DeleteAsync(item.Entry.Id).ConfigureAwait(false);
        await MainThread.InvokeOnMainThreadAsync(() =>
        {
            EntryItems.Remove(item);
            NotifyEntriesChanged();
        });
    }

    [RelayCommand]
    private async Task ClearAllAsync()
    {
        var snapshot = EntryItems.ToArray();
        if (snapshot.Length == 0)
        {
            return;
        }

        var animationTasks = snapshot.Select(async (item, index) =>
        {
            await Task.Delay(index * ClearAllStaggerMs).ConfigureAwait(false);
            await MainThread.InvokeOnMainThreadAsync(() => HistoryEntryCard.AnimateOutIfPresentAsync(item));
        });

        await Task.WhenAll(animationTasks).ConfigureAwait(false);
        await historyService.ClearAllAsync().ConfigureAwait(false);
        await MainThread.InvokeOnMainThreadAsync(() =>
        {
            EntryItems.Clear();
            NotifyEntriesChanged();
        });
    }

    private static HistoryEntryItem CreateItem(HistoryEntry entry, int index) =>
        new(entry, index % 2 == 0 ? 1 : -1);
}
