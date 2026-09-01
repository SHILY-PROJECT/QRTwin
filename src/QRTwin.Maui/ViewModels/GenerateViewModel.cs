using CommunityToolkit.Maui.Storage;

namespace QRTwin.Maui.ViewModels;

public partial class GenerateViewModel(
    IHistoryService historyService,
    IQrCodeService qrCodeService) : ObservableObject
{
    private string? _tempFilePath;

    public event EventHandler? HistorySaved;

    [ObservableProperty]
    public partial string InputText { get; set; }

    [ObservableProperty]
    public partial ImageSource? QrCodeImage { get; set; }

    [ObservableProperty]
    public partial bool HasQrCode { get; set; }

    [ObservableProperty]
    public partial bool IsGenerating { get; set; }

    [ObservableProperty]
    public partial bool IsActive { get; set; }

    [ObservableProperty]
    public partial string ErrorMessage { get; set; }

    [RelayCommand]
    private async Task GenerateAsync()
    {
        if (!InputText.IsNotBlank())
        {
            ErrorMessage = "Введите текст или ссылку для генерации QR-кода.";
            return;
        }

        ErrorMessage = string.Empty;
        IsGenerating = true;

        try
        {
            var image = await qrCodeService
                .GenerateQrCodeAsync(InputText, QrEncodeOptions.Presentation)
                .ConfigureAwait(false);

            if (image is null)
            {
                ErrorMessage = "Не удалось сгенерировать QR-код.";
                return;
            }

            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                QrCodeImage = image;
                HasQrCode = true;
            });

            _tempFilePath = await qrCodeService.SaveToTempFileAsync(image).ConfigureAwait(false);
            await historyService.AddAsync(HistoryEntryType.Generate, InputText.TrimmedOrEmpty()).ConfigureAwait(false);
            HistorySaved?.Invoke(this, EventArgs.Empty);
        }
        catch (Exception ex)
        {
            ErrorMessage = $"""
                Ошибка генерации:
                {ex.Message}
                """;
        }
        finally
        {
            IsGenerating = false;
        }
    }

    [RelayCommand]
    private async Task ShareAsync()
    {
        if (await EnsureTempFilePathAsync() is not { } path)
        {
            return;
        }

        await Share.Default.RequestAsync(new ShareFileRequest
        {
            Title = "Поделиться QR-кодом",
            File = new ShareFile(path)
        }).ConfigureAwait(false);
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        if (await EnsureTempFilePathAsync() is not { } path)
        {
            return;
        }

        await using var stream = File.OpenRead(path);
        var result = await FileSaver.Default.SaveAsync("qrcode.png", stream).ConfigureAwait(false);

        ErrorMessage = result is null ? "Сохранение отменено." : string.Empty;
    }

    private async Task<string?> EnsureTempFilePathAsync()
    {
        if (_tempFilePath is { } existingPath && File.Exists(existingPath))
        {
            return existingPath;
        }

        if (QrCodeImage is not { } image)
        {
            return null;
        }

        return _tempFilePath = await qrCodeService.SaveToTempFileAsync(image).ConfigureAwait(false);
    }
}
