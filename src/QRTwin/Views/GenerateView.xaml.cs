using QRTwin.Extensions;
using QRTwin.ViewModels;

namespace QRTwin.Views;

public partial class GenerateView : ContentView
{
    private GenerateViewModel? _viewModel;

    public GenerateView()
    {
        InitializeComponent();
        Unloaded += OnUnloaded;
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
