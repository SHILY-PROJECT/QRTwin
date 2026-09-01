using QRTwin.Maui.Extensions;
using QRTwin.Maui.Models;
using SQLite;

namespace QRTwin.Maui.Services;

public sealed class HistoryService() : IHistoryService
{
    private readonly Lazy<Task<SQLiteAsyncConnection>> _connection = new(CreateConnectionAsync);

    public async Task<IReadOnlyList<HistoryEntry>> GetAllAsync() =>
        await (await GetConnectionAsync().ConfigureAwait(false))
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

        await (await GetConnectionAsync().ConfigureAwait(false))
            .InsertAsync(entry)
            .ConfigureAwait(false);
    }

    public async Task DeleteAsync(int id) =>
        await (await GetConnectionAsync().ConfigureAwait(false))
            .DeleteAsync<HistoryEntry>(id)
            .ConfigureAwait(false);

    public async Task ClearAllAsync() =>
        await (await GetConnectionAsync().ConfigureAwait(false))
            .DeleteAllAsync<HistoryEntry>()
            .ConfigureAwait(false);

    private Task<SQLiteAsyncConnection> GetConnectionAsync() => _connection.Value;

    private static async Task<SQLiteAsyncConnection> CreateConnectionAsync()
    {
        var databasePath = Path.Combine(FileSystem.AppDataDirectory, "qrtwin_history.db3");
        var connection = new SQLiteAsyncConnection(databasePath);
        await connection.CreateTableAsync<HistoryEntry>().ConfigureAwait(false);
        return connection;
    }
}
