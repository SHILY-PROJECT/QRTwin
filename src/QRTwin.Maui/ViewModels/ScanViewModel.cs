using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using QRTwin.Maui.Models;
using QRTwin.Maui.Services;

namespace QRTwin.Maui.ViewModels;

public partial class ScanViewModel : ObservableObject
{
    private readonly IHistoryService _historyService;
    private readonly IQrCodeService _qrCodeService;
    private readonly IPermissionService _permissionService;

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

    partial void OnIsActiveChanged(bool value)
    {
        if (value)
        {
            _ = InitializeAsync();
        }
        else
        {
            IsScanning = false;
        }
    }

    [RelayCommand]
    private async Task InitializeAsync()
    {
        ErrorMessage = string.Empty;
        HasCameraPermission = await _permissionService.EnsureCameraPermissionAsync().ConfigureAwait(false);

        if (!HasCameraPermission)
        {
            ErrorMessage = "Нет доступа к камере. Разрешите использование камеры в настройках.";
            return;
        }

        ResetScan();
    }

    [RelayCommand]
    private async Task ProcessBarcodeAsync(string? value)
    {
        if (!IsActive || !IsScanning || string.IsNullOrWhiteSpace(value))
        {
            return;
        }

        IsScanning = false;
        ScanResult = value.Trim();
        HasResult = true;
        IsUrl = _qrCodeService.IsUrl(ScanResult);

        await MainThread.InvokeOnMainThreadAsync(async () =>
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
        if (string.IsNullOrWhiteSpace(ScanResult))
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
