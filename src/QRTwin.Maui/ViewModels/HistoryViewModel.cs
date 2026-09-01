using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using QRTwin.Maui.Models;
using QRTwin.Maui.Services;

namespace QRTwin.Maui.ViewModels;

public partial class HistoryViewModel(IHistoryService historyService) : ObservableObject
{
    private readonly IHistoryService _historyService = historyService;

    [ObservableProperty]
    private bool _isLoading;

    public ObservableCollection<HistoryEntry> Entries { get; } = [];

    public async Task LoadAsync()
    {
        IsLoading = true;

        try
        {
            var entries = await _historyService.GetAllAsync().ConfigureAwait(false);
            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                Entries.Clear();
                foreach (var entry in entries)
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
        await _historyService.DeleteAsync(entry.Id).ConfigureAwait(false);
        await MainThread.InvokeOnMainThreadAsync(() => Entries.Remove(entry));
    }

    [RelayCommand]
    private async Task ClearAllAsync()
    {
        await _historyService.ClearAllAsync().ConfigureAwait(false);
        await MainThread.InvokeOnMainThreadAsync(() => Entries.Clear());
    }
}
