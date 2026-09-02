using QRTwin.Models;

namespace QRTwin.ViewModels;

public sealed class HistoryEntryItem(HistoryEntry entry, int slideDirection)
{
    public HistoryEntry Entry { get; } = entry;

    /// <summary>-1 flies left, +1 flies right.</summary>
    public int SlideDirection { get; } = slideDirection;
}
