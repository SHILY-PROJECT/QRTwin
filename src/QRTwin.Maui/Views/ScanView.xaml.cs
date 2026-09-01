using ZXing.Net.Maui;
using ZXing.Net.Maui.Controls;
using QRTwin.Maui.ViewModels;

namespace QRTwin.Maui.Views;

public partial class ScanView : ContentView
{
    private bool _blinkRunning;
    private bool _scanLineRunning;

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
        StartScanLineAnimation();
    }

    private void OnUnloaded(object? sender, EventArgs e)
    {
        _blinkRunning = false;
        _scanLineRunning = false;
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
            await SampleQrImage.FadeToAsync(0.45, 700, Easing.SinInOut);
            if (!_blinkRunning)
            {
                break;
            }

            await SampleQrImage.FadeToAsync(1, 700, Easing.SinInOut);
        }
    }

    private async void StartScanLineAnimation()
    {
        if (_scanLineRunning)
        {
            return;
        }

        _scanLineRunning = true;

        while (_scanLineRunning && ScanLine is not null)
        {
            ScanLine.TranslationY = 0;
            var travel = QrScanArea?.Height > 0 ? QrScanArea.Height - 28 : 192;
            await ScanLine.TranslateToAsync(0, travel, 1800, Easing.SinInOut);
            if (!_scanLineRunning)
            {
                break;
            }

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
