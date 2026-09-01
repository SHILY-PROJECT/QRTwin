using QRTwin.Maui.Models;

namespace QRTwin.Maui.Services;

public interface IHistoryService
{
    Task<IReadOnlyList<HistoryEntry>> GetAllAsync();

    Task AddAsync(HistoryEntryType entryType, string content);

    Task DeleteAsync(int id);

    Task ClearAllAsync();
}
