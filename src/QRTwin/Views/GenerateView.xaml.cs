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

    public double GetFreeSpaceBelowEmptyState(double hostHeight, Thickness hostPadding)
    {
        if (BindingContext is GenerateViewModel { HasQrCode: true })
        {
            return 0;
        }

        if (EmptyStateCard.IsVisible && EmptyStateCard.Height > 0)
        {
            var cardBottom = StackTopPadding + EmptyStateCard.Y + EmptyStateCard.Height;
            var innerHeight = hostHeight - hostPadding.Top - hostPadding.Bottom;
            return innerHeight - cardBottom;
        }

        var contentHeight = Math.Max(0, hostHeight - hostPadding.Top - hostPadding.Bottom);
        var anchorBottom = StackTopPadding + (contentHeight / 2) + (280 / 2);
        return contentHeight - anchorBottom;
    }

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
