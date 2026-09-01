using ZXing.Net.Maui;
using ZXing.Net.Maui.Controls;
using QRTwin.Maui.ViewModels;

namespace QRTwin.Maui.Views;

public partial class ScanView : ContentView
{
    private bool _blinkRunning;

    public ScanView()
    {
        InitializeComponent();
        BarcodeReader.Options = new BarcodeReaderOptions
        {
            Formats = BarcodeFormat.QrCode,
            AutoRotate = true,
            Multiple = false
        };
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }

    private void OnLoaded(object? sender, EventArgs e)
    {
        StartSampleQrBlinkAnimation();
    }

    private void OnUnloaded(object? sender, EventArgs e)
    {
        _blinkRunning = false;
    }

    private async void StartSampleQrBlinkAnimation()
    {
        if (_blinkRunning)
        {
            return;
        }

        _blinkRunning = true;

        while (_blinkRunning && SampleQrImage is not null)
        {
            await SampleQrImage.FadeToAsync(0.35, 700, Easing.SinInOut);
            if (!_blinkRunning)
            {
                break;
            }

            await SampleQrImage.FadeToAsync(1, 700, Easing.SinInOut);
        }
    }

    private async void OnBarcodesDetected(object? sender, BarcodeDetectionEventArgs e)
    {
        var result = e.Results?.FirstOrDefault()?.Value;
        if (BindingContext is not ScanViewModel viewModel || string.IsNullOrWhiteSpace(result))
        {
            return;
        }

        await viewModel.ProcessBarcodeCommand.ExecuteAsync(result);
    }
}
