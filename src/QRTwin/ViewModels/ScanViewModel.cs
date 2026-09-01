using QRTwin.Models;
using QRTwin.Services;

namespace QRTwin.ViewModels;

public partial class ScanViewModel(
    IHistoryService historyService,
    IQrCodeService qrCodeService,
    IPermissionService permissionService) : ObservableObject
{
    public const string SampleQrContent = "QRTwin — Сканируйте и создавайте QR-коды";

    public event EventHandler? HistorySaved;

    [ObservableProperty]
    public partial string ScanResult { get; set; }

    [ObservableProperty]
    public partial bool HasResult { get; set; }

    [ObservableProperty]
    public partial bool IsScanning { get; set; }

    [ObservableProperty]
    public partial bool IsUrl { get; set; }

    [ObservableProperty]
    public partial bool IsActive { get; set; }

    [ObservableProperty]
    public partial bool HasCameraPermission { get; set; }

    [ObservableProperty]
    public partial string ErrorMessage { get; set; }

    [ObservableProperty]
    public partial ImageSource? SampleQrCodeImage { get; set; }

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

    private async Task EnsureSampleQrAsync()
    {
        if (SampleQrCodeImage is not null)
        {
            return;
        }

        SampleQrCodeImage = await qrCodeService
            .GenerateQrCodeAsync(SampleQrContent, QrEncodeOptions.Preview)
            .ConfigureAwait(true);
    }

    [RelayCommand]
    private async Task InitializeAsync()
    {
        ErrorMessage = string.Empty;
        HasCameraPermission = await permissionService.EnsureCameraPermissionAsync().ConfigureAwait(false);

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

        var result = new ScanResult(
            value.TrimmedOrEmpty(),
            qrCodeService.IsUrl(value),
            DateTime.UtcNow);

        ScanResult = result.Content;
        HasResult = true;
        IsUrl = result.IsUrl;

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

        await historyService.AddAsync(HistoryEntryType.Scan, result.Content).ConfigureAwait(false);
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

    public void RestoreFromHistory(string content)
    {
        if (!content.IsNotBlank())
        {
            return;
        }

        var trimmed = content.TrimmedOrEmpty();
        ScanResult = trimmed;
        HasResult = true;
        IsUrl = qrCodeService.IsUrl(trimmed);
        IsScanning = false;
        ErrorMessage = string.Empty;
    }
}
