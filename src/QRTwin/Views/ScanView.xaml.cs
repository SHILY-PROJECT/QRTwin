using ZXing.Net.Maui;
using ZXing.Net.Maui.Controls;
using QRTwin.Extensions;
using QRTwin.ViewModels;

namespace QRTwin.Views;

public partial class ScanView : ContentView
{
    private const double ScanBeamHeight = 56;

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

    private double GetScanAreaHeight()
    {
        if (CameraScanOverlay?.Height is > 0 and var height)
        {
            return height;
        }

        if (ScannerContent?.Height is > 0 and var contentHeight)
        {
            return contentHeight;
        }

        return 280;
    }

    private (double Top, double Bottom) GetScanLinePositions()
    {
        var areaHeight = GetScanAreaHeight();
        var top = -ScanBeamHeight;
        var bottom = Math.Max(top, areaHeight - ScanBeamHeight);
        return (top, bottom);
    }

    private async Task PulseScanLineAsync(uint duration)
    {
        if (ScanLine is null)
        {
            return;
        }

        var half = duration / 2;
        await ScanLine.FadeToAsync(0.72, half, Easing.SinInOut);
        if (!_scanLineRunning)
        {
            return;
        }

        await ScanLine.FadeToAsync(1, half, Easing.SinInOut);
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
            var (top, bottom) = GetScanLinePositions();
            ScanLine.TranslationY = top;
            ScanLine.Opacity = 1;
            const uint duration = 1800;
            await Task.WhenAll(
                ScanLine.TranslateToAsync(0, bottom, duration, Easing.SinInOut),
                PulseScanLineAsync(duration));
            if (!_scanLineRunning)
            {
                break;
            }

            ScanLine.TranslationY = bottom;
            ScanLine.Opacity = 1;
            await Task.WhenAll(
                ScanLine.TranslateToAsync(0, top, duration, Easing.SinInOut),
                PulseScanLineAsync(duration));
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
