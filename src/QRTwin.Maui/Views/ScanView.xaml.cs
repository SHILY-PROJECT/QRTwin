using ZXing.Net.Maui;
using ZXing.Net.Maui.Controls;
using QRTwin.Maui.ViewModels;

namespace QRTwin.Maui.Views;

public partial class ScanView : ContentView
{
    private bool _animationRunning;

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
        StartScanLineAnimation();
    }

    private void OnUnloaded(object? sender, EventArgs e)
    {
        _animationRunning = false;
    }

    private async void StartScanLineAnimation()
    {
        if (_animationRunning)
        {
            return;
        }

        _animationRunning = true;

        while (_animationRunning && ScanLine is not null)
        {
            ScanLine.TranslationY = 0;
            var travel = BarcodeReader?.Height > 0 ? BarcodeReader.Height - 40 : 240;
            await ScanLine.TranslateToAsync(0, travel, 1800, Easing.SinInOut);
            await ScanLine.TranslateToAsync(0, 0, 1800, Easing.SinInOut);
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
