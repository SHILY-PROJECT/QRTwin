using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using QRTwin.Maui.Extensions;
using QRTwin.Maui.Models;
using QRTwin.Maui.Services;

namespace QRTwin.Maui.ViewModels;

public partial class ScanViewModel : ObservableObject
{
    private readonly IHistoryService _historyService;
    private readonly IQrCodeService _qrCodeService;
    private readonly IPermissionService _permissionService;

    public const string SampleQrContent = "QRTwin — Сканируйте и создавайте QR-коды";

    public event EventHandler? HistorySaved;

    public ScanViewModel(
        IHistoryService historyService,
        IQrCodeService qrCodeService,
        IPermissionService permissionService)
    {
        _historyService = historyService;
        _qrCodeService = qrCodeService;
        _permissionService = permissionService;
    }

    [ObservableProperty]
    private string _scanResult = string.Empty;

    [ObservableProperty]
    private bool _hasResult;

    [ObservableProperty]
    private bool _isScanning = true;

    [ObservableProperty]
    private bool _isUrl;

    [ObservableProperty]
    private bool _isActive;

    [ObservableProperty]
    private bool _hasCameraPermission;

    [ObservableProperty]
    private string _errorMessage = string.Empty;

    [ObservableProperty]
    private ImageSource? _sampleQrCodeImage;

    partial void OnIsActiveChanged(bool value)
    {
        switch (value)
        {
            case true:
                _ = InitializeAsync();
                break;
            case false:
                IsScanning = false;
                break;
        }
    }

    private async Task EnsureSampleQrAsync()
    {
        if (SampleQrCodeImage is not null)
        {
            return;
        }

        SampleQrCodeImage = await _qrCodeService.GenerateQrCodeAsync(SampleQrContent, 280)
            .ConfigureAwait(true);
    }

    [RelayCommand]
    private async Task InitializeAsync()
    {
        ErrorMessage = string.Empty;
        HasCameraPermission = await _permissionService.EnsureCameraPermissionAsync().ConfigureAwait(false);

        if (!HasCameraPermission)
        {
            ErrorMessage = ErrorMessage.IsNotBlank()
                ? ErrorMessage
                : "Для сканирования необходим доступ к камере.";
            return;
        }

        await EnsureSampleQrAsync().ConfigureAwait(false);
        ResetScan();
    }

    [RelayCommand]
    private async Task ProcessBarcodeAsync(string? value)
    {
        if (!IsActive || !IsScanning || !value.IsNotBlank())
        {
            return;
        }

        IsScanning = false;
        ScanResult = value.TrimmedOrEmpty();
        HasResult = true;
        IsUrl = _qrCodeService.IsUrl(ScanResult);

        await MainThread.InvokeOnMainThreadAsync(() =>
        {
            try
            {
                HapticFeedback.Default.Perform(HapticFeedbackType.Click);
            }
            catch
            {
                // Haptic feedback may be unavailable on some platforms.
            }
        });

        await _historyService.AddAsync(HistoryEntryType.Scan, ScanResult).ConfigureAwait(false);
        HistorySaved?.Invoke(this, EventArgs.Empty);
    }

    [RelayCommand]
    private async Task CopyResultAsync()
    {
        if (!ScanResult.IsNotBlank())
        {
            return;
        }

        await Clipboard.Default.SetTextAsync(ScanResult).ConfigureAwait(false);
    }

    [RelayCommand]
    private async Task OpenUrlAsync()
    {
        if (!IsUrl)
        {
            return;
        }

        await Launcher.Default.OpenAsync(new Uri(ScanResult)).ConfigureAwait(false);
    }

    [RelayCommand]
    private void ResetScan()
    {
        ScanResult = string.Empty;
        HasResult = false;
        IsUrl = false;
        IsScanning = HasCameraPermission;
    }
}
