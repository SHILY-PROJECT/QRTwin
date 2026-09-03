using QRTwin.Extensions;
using QRTwin.ViewModels;

namespace QRTwin.Views;

public partial class GenerateView : ContentView
{
    private const double StackTopPadding = 4;

    private GenerateViewModel? _viewModel;

    public GenerateView()
    {
        InitializeComponent();
        Unloaded += OnUnloaded;
    }

    public double GetFreeSpaceBelowQrCard(double hostHeight, Thickness hostPadding, double hostWidth)
    {
        if (BindingContext is not GenerateViewModel { HasQrCode: true } || !QrCard.IsVisible)
        {
            return 0;
        }

        var qrHeight = MeasureQrCardHeight(hostWidth);
        if (qrHeight <= 0)
        {
            return -1;
        }

        return hostHeight - hostPadding.Top - hostPadding.Bottom - StackTopPadding - qrHeight;
    }

    public bool TryGetEmptyStateBottomIn(VisualElement ancestor, out double bottom)
    {
        bottom = 0;

        if (BindingContext is GenerateViewModel { HasQrCode: true })
        {
            return false;
        }

        var cardHeight = MeasureEmptyStateCardHeight(ancestor.Width > 0 ? ancestor.Width : Width);
        if (cardHeight <= 0)
        {
            return false;
        }

        if (EmptyStateCard.IsVisible
            && EmptyStateCard.Width > 0
            && TryGetOffsetToAncestor(EmptyStateCard, ancestor, out _, out var offsetY))
        {
            bottom = offsetY + (EmptyStateCard.Height > 0 ? EmptyStateCard.Height : cardHeight);
            return true;
        }

        // Fall back before first layout: empty state sits near the top of this view.
        if (!TryGetOffsetToAncestor(this, ancestor, out _, out var viewTop))
        {
            return false;
        }

        bottom = viewTop + StackTopPadding + cardHeight;
        return true;
    }

    public bool TryGetQrCardBottomIn(VisualElement ancestor, out double bottom)
    {
        bottom = 0;

        if (BindingContext is not GenerateViewModel { HasQrCode: true } || !QrCard.IsVisible)
        {
            return false;
        }

        var cardHeight = MeasureQrCardHeight(ancestor.Width > 0 ? ancestor.Width : Width);
        if (cardHeight <= 0)
        {
            return false;
        }

        if (QrCard.Width > 0
            && TryGetOffsetToAncestor(QrCard, ancestor, out _, out var offsetY))
        {
            bottom = offsetY + (QrCard.Height > 0 ? QrCard.Height : cardHeight);
            return true;
        }

        if (!TryGetOffsetToAncestor(this, ancestor, out _, out var viewTop))
        {
            return false;
        }

        bottom = viewTop + StackTopPadding + cardHeight;
        return true;
    }

    private double MeasureEmptyStateCardHeight(double hostWidth)
    {
        if (EmptyStateCard.IsVisible && EmptyStateCard.Height > 0)
        {
            return EmptyStateCard.Height;
        }

        var width = hostWidth > 0 ? hostWidth : Width;
        if (width > 0)
        {
            EmptyStateCard.Measure(width, double.PositiveInfinity);
            if (EmptyStateCard.DesiredSize.Height > 0)
            {
                return EmptyStateCard.DesiredSize.Height;
            }
        }

        return Math.Max(280, EmptyStateCard.MinimumHeightRequest);
    }

    private static bool TryGetOffsetToAncestor(
        VisualElement view,
        VisualElement ancestor,
        out double offsetX,
        out double offsetY)
    {
        offsetX = 0;
        offsetY = 0;

        VisualElement? current = view;
        while (current is not null && !ReferenceEquals(current, ancestor))
        {
            offsetX += current.X + current.TranslationX;
            offsetY += current.Y + current.TranslationY;

            if (current.Parent is ScrollView scrollView)
            {
                offsetY -= scrollView.ScrollY;
                offsetX -= scrollView.ScrollX;
            }

            if (current.Parent is not VisualElement parent)
            {
                return false;
            }

            // Child X/Y are relative to the parent's padded content area.
            var padding = GetElementPadding(parent);
            offsetX += padding.Left;
            offsetY += padding.Top;
            current = parent;
        }

        return ReferenceEquals(current, ancestor);
    }

    private static Thickness GetElementPadding(VisualElement element) =>
        element switch
        {
            Layout layout => layout.Padding,
            Border border => border.Padding,
            ContentView contentView => contentView.Padding,
            ScrollView scrollView => scrollView.Padding,
            _ => Thickness.Zero
        };

    private double MeasureQrCardHeight(double hostWidth)
    {
        if (QrCard.Height > 0)
        {
            return QrCard.Height;
        }

        var width = hostWidth > 0 ? hostWidth : Width;
        if (width <= 0)
        {
            return 0;
        }

        QrCard.Measure(width, double.PositiveInfinity);
        return QrCard.DesiredSize.Height;
    }

    protected override void OnBindingContextChanged()
    {
        base.OnBindingContextChanged();

        if (_viewModel is not null)
        {
            _viewModel.PropertyChanged -= OnViewModelPropertyChanged;
            _viewModel = null;
        }

        if (BindingContext is GenerateViewModel viewModel)
        {
            _viewModel = viewModel;
            viewModel.PropertyChanged += OnViewModelPropertyChanged;
        }
    }

    private void OnUnloaded(object? sender, EventArgs e)
    {
        QrCard.StopAnimations();
    }

    private void OnResetQrClicked(object? sender, EventArgs e)
    {
        if (BindingContext is GenerateViewModel viewModel)
        {
            viewModel.ClearFromUi();
        }
    }

    private async void OnViewModelPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (!e.IsProperty(nameof(GenerateViewModel.HasQrCode)))
        {
            return;
        }

        if (sender is GenerateViewModel { HasQrCode: true })
        {
            QrCard.Opacity = 0;
            QrCard.Scale = 0.85;

            try
            {
                await QrCard.FadeToAsync(1, 300, Easing.CubicOut);
                await QrCard.ScaleToAsync(1, 300, Easing.CubicOut);
            }
            catch (Exception ex) when (ViewLifecycleExtensions.IsShutdownException(ex))
            {
            }

            return;
        }

        QrCard.Opacity = 1;
        QrCard.Scale = 1;
    }
}
