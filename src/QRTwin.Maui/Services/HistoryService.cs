using QRTwin.Maui.Extensions;
using QRTwin.Maui.Models;
using SQLite;

namespace QRTwin.Maui.Services;

public sealed class HistoryService() : IHistoryService
{
    private readonly Lazy<SQLiteAsyncConnection> _connection = new(CreateConnection);

    public async Task<IReadOnlyList<HistoryEntry>> GetAllAsync() =>
        await _connection.Value
            .Table<HistoryEntry>()
            .OrderByDescending(e => e.CreatedAt)
            .ToListAsync()
            .ConfigureAwait(false);

    public async Task AddAsync(HistoryEntryType entryType, string content)
    {
        if (!content.IsNotBlank())
        {
            return;
        }

        var entry = new HistoryEntry
        {
            EntryType = entryType,
            Content = content.TrimmedOrEmpty(),
            CreatedAt = DateTime.UtcNow
        };

        await _connection.Value.InsertAsync(entry).ConfigureAwait(false);
    }

    public Task DeleteAsync(int id) =>
        _connection.Value.DeleteAsync<HistoryEntry>(id);

    public Task ClearAllAsync() =>
        _connection.Value.DeleteAllAsync<HistoryEntry>();

    private static SQLiteAsyncConnection CreateConnection()
    {
        var databasePath = Path.Combine(FileSystem.AppDataDirectory, "qrtwin_history.db3");
        var connection = new SQLiteAsyncConnection(databasePath);
        connection.CreateTableAsync<HistoryEntry>().GetAwaiter().GetResult();
        return connection;
    }
}
