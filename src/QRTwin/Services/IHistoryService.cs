using QRTwin.Models;

namespace QRTwin.Services;

public interface IHistoryService
{
    Task<IReadOnlyList<HistoryEntry>> GetAllAsync();

    IAsyncEnumerable<HistoryEntry> StreamAllAsync(CancellationToken cancellationToken = default);

    Task AddAsync(HistoryEntryType entryType, string content);

    Task DeleteAsync(int id);

    Task ClearAllAsync();
}
