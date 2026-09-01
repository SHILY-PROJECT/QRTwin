using QRTwin.Maui.ViewModels;

namespace QRTwin.Maui.Views;

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

    private async void OnViewModelPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(GenerateViewModel.HasQrCode))
        {
            return;
        }

        if (sender is GenerateViewModel { HasQrCode: true })
        {
            QrCard.Opacity = 0;
            QrCard.Scale = 0.85;
            await QrCard.FadeToAsync(1, 300, Easing.CubicOut);
            await QrCard.ScaleToAsync(1, 300, Easing.CubicOut);
        }
    }
}
