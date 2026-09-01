using SQLite;

namespace QRTwin.Models;

[Table("HistoryEntries")]
public class HistoryEntry
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    public HistoryEntryType EntryType { get; set; }

    public string Content { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }
}
