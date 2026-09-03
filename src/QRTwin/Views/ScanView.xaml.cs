using ZXing.Net.Maui;
using ZXing.Net.Maui.Controls;
using QRTwin.Extensions;
using QRTwin.ViewModels;

namespace QRTwin.Views;

public partial class ScanView : ContentView
{
    private const double ScanBeamHeight = 56;
    private const double MinFrameSize = 160;
    private const double MaxFrameSize = 520;
    private const double MinQrSize = 100;
    private const double MaxQrSize = 280;
    /// <summary>Keeps inner stroke from visually touching the outer card stroke.</summary>
    private const double FrameStrokeGap = 4;

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
        ScannerHost.SizeChanged += OnScannerHostSizeChanged;
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
        UpdateAdaptiveSizes();
        StartSampleQrBlinkAnimation();
        StartScanLineAnimation();
    }

    private void OnUnloaded(object? sender, EventArgs e)
    {
        _blinkRunning = false;
        _scanLineRunning = false;
        SampleQrImage.StopAnimations();
        SampleScanLine.StopAnimations();
        CameraScanLine.StopAnimations();
    }

    private void OnScannerHostSizeChanged(object? sender, EventArgs e) => UpdateAdaptiveSizes();

    /// <summary>
    /// Keeps the scanner frame square and inset inside the host so inner/outer
    /// strokes never collide across DPI, window resize, and device sizes.
    /// </summary>
    private void UpdateAdaptiveSizes()
    {
        if (ScannerHost.Width <= 0 || ScannerHost.Height <= 0)
        {
            return;
        }

        var pad = ScannerHost.Padding;
        var availableWidth = ScannerHost.Width - pad.Left - pad.Right - FrameStrokeGap;
        var availableHeight = ScannerHost.Height - pad.Top - pad.Bottom - FrameStrokeGap;
        if (availableWidth < 1 || availableHeight < 1)
        {
            return;
        }

        // Fit inside the padded host — never force MinFrameSize above available space
        // (that was clipping the frame into the outer card stroke).
        var frame = Math.Min(Math.Min(availableWidth, availableHeight), MaxFrameSize);
        if (frame < 1)
        {
            return;
        }

        if (Math.Abs(ScannerFrame.WidthRequest - frame) > 0.5
            || Math.Abs(ScannerFrame.HeightRequest - frame) > 0.5)
        {
            ScannerFrame.WidthRequest = frame;
            ScannerFrame.HeightRequest = frame;
        }

        var qrCeiling = Math.Min(MaxQrSize, frame * 0.7);
        var qr = Math.Clamp(frame * 0.52, Math.Min(MinQrSize, qrCeiling), qrCeiling);
        if (Math.Abs(QrScanArea.WidthRequest - qr) > 0.5
            || Math.Abs(QrScanArea.HeightRequest - qr) > 0.5)
        {
            QrScanArea.WidthRequest = qr;
            QrScanArea.HeightRequest = qr;
        }
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
            try
            {
                await SampleQrImage.FadeToAsync(0.45, 700, Easing.SinInOut);
                if (!_blinkRunning)
                {
                    break;
                }

                await SampleQrImage.FadeToAsync(1, 700, Easing.SinInOut);
            }
            catch (Exception ex) when (ViewLifecycleExtensions.IsShutdownException(ex))
            {
                break;
            }
        }
    }

    private BoxView GetActiveScanLine()
    {
        if (BindingContext is ScanViewModel { ShowSamplePreview: true })
        {
            return SampleScanLine;
        }

        return CameraScanLine;
    }

    private double GetActiveScanAreaHeight()
    {
        if (BindingContext is ScanViewModel { ShowSamplePreview: true } && QrScanArea.Height > 0)
        {
            return QrScanArea.Height;
        }

        if (CameraScanOverlay.Height > 0)
        {
            return CameraScanOverlay.Height;
        }

        if (ScannerContent.Height > 0)
        {
            return ScannerContent.Height;
        }

        return MinFrameSize;
    }

    private (double Top, double Bottom) GetScanLinePositions()
    {
        var areaHeight = GetActiveScanAreaHeight();
        var top = 0d;
        var bottom = Math.Max(top, areaHeight - ScanBeamHeight);
        return (top, bottom);
    }

    private async Task PulseScanLineAsync(BoxView scanLine, uint duration)
    {
        var half = duration / 2;
        try
        {
            await scanLine.FadeToAsync(0.72, half, Easing.SinInOut);
            if (!_scanLineRunning)
            {
                return;
            }

            await scanLine.FadeToAsync(1, half, Easing.SinInOut);
        }
        catch (Exception ex) when (ViewLifecycleExtensions.IsShutdownException(ex))
        {
        }
    }

    private async void StartScanLineAnimation()
    {
        if (_scanLineRunning)
        {
            return;
        }

        _scanLineRunning = true;

        while (_scanLineRunning)
        {
            try
            {
                var scanLine = GetActiveScanLine();
                if (!scanLine.IsVisible && scanLine.Opacity <= 0)
                {
                    await Task.Delay(200);
                    continue;
                }

                var (top, bottom) = GetScanLinePositions();
                scanLine.TranslationY = top;
                scanLine.Opacity = 1;
                const uint duration = 1800;
                await Task.WhenAll(
                    scanLine.TranslateToAsync(0, bottom, duration, Easing.SinInOut),
                    PulseScanLineAsync(scanLine, duration));
                if (!_scanLineRunning)
                {
                    break;
                }

                scanLine.TranslationY = bottom;
                scanLine.Opacity = 1;
                await Task.WhenAll(
                    scanLine.TranslateToAsync(0, top, duration, Easing.SinInOut),
                    PulseScanLineAsync(scanLine, duration));
            }
            catch (Exception ex) when (ViewLifecycleExtensions.IsShutdownException(ex))
            {
                break;
            }
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
