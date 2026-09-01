using QRTwin.ViewModels;

namespace QRTwin.Views;

public partial class GenerateView : ContentView
{
    public GenerateView()
    {
        InitializeComponent();
    }

    protected override void OnBindingContextChanged()
    {
        base.OnBindingContextChanged();

        if (BindingContext is GenerateViewModel viewModel)
        {
            viewModel.PropertyChanged += OnViewModelPropertyChanged;
        }
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
            await QrCard.FadeToAsync(1, 300, Easing.CubicOut);
            await QrCard.ScaleToAsync(1, 300, Easing.CubicOut);
            return;
        }

        QrCard.Opacity = 1;
        QrCard.Scale = 1;
    }
}
