namespace QRTwin.Maui.Models;

public sealed record HistoryEntryDto(
    HistoryEntryType EntryType,
    string Content,
    DateTime CreatedAt)
{
    public static HistoryEntryDto FromScan(string content) =>
        new(HistoryEntryType.Scan, content, DateTime.UtcNow);

    public static HistoryEntryDto FromGenerate(string content) =>
        new(HistoryEntryType.Generate, content, DateTime.UtcNow);

    public HistoryEntry ToEntity() => new()
    {
        EntryType = EntryType,
        Content = Content,
        CreatedAt = CreatedAt
    };
}
