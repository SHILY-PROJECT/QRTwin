using ZXing.Net.Maui;
using ZXing.Net.Maui.Controls;
using QRTwin.Extensions;
using QRTwin.ViewModels;

namespace QRTwin.Views;

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
        BarcodeReader.HandlerChanged += OnBarcodeReaderHandlerChanged;
    }

#if WINDOWS
    private void OnBarcodeReaderHandlerChanged(object? sender, EventArgs e)
    {
        if (BarcodeReader.Handler?.PlatformView is not Microsoft.UI.Xaml.FrameworkElement nativeCamera)
        {
            return;
        }

        nativeCamera.Opacity = 0;
        nativeCamera.Width = 1;
        nativeCamera.Height = 1;

        if (nativeCamera is Microsoft.UI.Xaml.Controls.Control control)
        {
            control.Background = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Transparent);
        }
    }
#else
    private void OnBarcodeReaderHandlerChanged(object? sender, EventArgs e)
    {
    }
#endif

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
        if (_blinkRunning || SampleQrImage is null)
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
        if (_scanLineRunning || ScanLine is null)
        {
            return;
        }

        _scanLineRunning = true;

        while (_scanLineRunning && ScanLine is not null)
        {
            ScanLine.TranslationY = 0;
            var travel = QrScanArea?.Height is > 0 and var height ? height - 28 : 192;
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
        if (e.Results?.FirstOrDefault()?.Value is not { } result
            || BindingContext is not ScanViewModel viewModel
            || !result.IsNotBlank())
        {
            return;
        }

        await viewModel.ProcessBarcodeCommand.ExecuteAsync(result);
    }
}
