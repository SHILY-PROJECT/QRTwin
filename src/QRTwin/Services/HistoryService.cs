using QRTwin.Models;
using SQLite;

namespace QRTwin.Services;

public sealed class HistoryService() : IHistoryService
{
    private readonly Lazy<Task<SQLiteAsyncConnection>> _connection = new(CreateConnectionAsync);

    public async Task<IReadOnlyList<HistoryEntry>> GetAllAsync() =>
        await (await GetConnectionAsync().ConfigureAwait(false))
            .Table<HistoryEntry>()
            .OrderByDescending(e => e.CreatedAt)
            .ToListAsync()
            .ConfigureAwait(false);

    public async IAsyncEnumerable<HistoryEntry> StreamAllAsync(
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var entries = await GetAllAsync().ConfigureAwait(false);

        foreach (var entry in entries)
        {
            cancellationToken.ThrowIfCancellationRequested();
            yield return entry;
        }
    }

    public async Task AddAsync(HistoryEntryType entryType, string content)
    {
        if (!content.IsNotBlank())
        {
            return;
        }

        var dto = entryType switch
        {
            HistoryEntryType.Scan => HistoryEntryDto.FromScan(content.TrimmedOrEmpty()),
            HistoryEntryType.Generate => HistoryEntryDto.FromGenerate(content.TrimmedOrEmpty()),
            _ => throw new ArgumentOutOfRangeException(nameof(entryType), entryType, null)
        };

        var entry = dto.ToEntity();

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
