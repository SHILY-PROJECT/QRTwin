using System.Runtime.CompilerServices;
using QRTwin.Extensions;
using QRTwin.ViewModels;

namespace QRTwin.Views;

public partial class HistoryEntryCard : Border
{
    private static readonly ConditionalWeakTable<HistoryEntryItem, HistoryEntryCard> LiveCards = new();

    public HistoryEntryCard()
    {
        InitializeComponent();
    }

    public HistoryEntryItem? Item { get; private set; }

    protected override void OnBindingContextChanged()
    {
        base.OnBindingContextChanged();
        Item = BindingContext as HistoryEntryItem;
        if (Item is not null)
        {
            LiveCards.AddOrUpdate(Item, this);
        }
    }

    public static Task AnimateOutIfPresentAsync(HistoryEntryItem item)
    {
        if (!LiveCards.TryGetValue(item, out var card))
        {
            return Task.CompletedTask;
        }

        return card.AnimateOutAsync();
    }

    private Task AnimateOutAsync()
    {
        var direction = Item?.SlideDirection ?? 1;
        var offset = direction * 220;
        InputTransparent = true;

        return this.FadeSlideXToAsync(
            0,
            offset,
            ViewAnimationExtensions.HistoryRemoveDuration,
            ViewAnimationExtensions.ExitEase);
    }
}
