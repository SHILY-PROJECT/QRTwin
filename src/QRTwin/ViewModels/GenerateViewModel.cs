using CommunityToolkit.Maui.Storage;
using QRTwin.Models;
using QRTwin.Services;

namespace QRTwin.ViewModels;

public partial class GenerateViewModel(
    IHistoryService historyService,
    IQrCodeService qrCodeService) : ObservableObject
{
    private const int GenerateBlockMs = 400;

    private string? _tempFilePath;
    private long _generateBlockedUntilTicks;

    public event EventHandler? HistorySaved;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(GenerateCommand))]
    public partial string InputText { get; set; }

    [ObservableProperty]
    public partial ImageSource? QrCodeImage { get; set; }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(GenerateCommand))]
    public partial bool HasQrCode { get; set; }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(GenerateCommand))]
    public partial bool IsGenerating { get; set; }

    [ObservableProperty]
    public partial bool IsActive { get; set; }

    [ObservableProperty]
    public partial string ErrorMessage { get; set; }

    private bool CanGenerate() =>
        InputText.IsNotBlank()
        && !IsGenerating
        && Environment.TickCount64 >= _generateBlockedUntilTicks;

    public void ClearFromUi()
    {
        _generateBlockedUntilTicks = Environment.TickCount64 + GenerateBlockMs;
        InputText = string.Empty;
        ClearResult();
        GenerateCommand.NotifyCanExecuteChanged();
    }

    partial void OnInputTextChanged(string value)
    {
        if (!value.IsNotBlank())
        {
            ClearResult();
        }
    }

    public async Task RestoreFromHistoryAsync(string content)
    {
        if (!content.IsNotBlank())
        {
            return;
        }

        _generateBlockedUntilTicks = 0;
        InputText = content.TrimmedOrEmpty();
        await GenerateAsync(saveToHistory: false).ConfigureAwait(false);
    }

    [RelayCommand(CanExecute = nameof(CanGenerate))]
    private Task GenerateAsync() => GenerateAsync(saveToHistory: true);

    private async Task GenerateAsync(bool saveToHistory)
    {
        if (!CanGenerate())
        {
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

            if (saveToHistory)
            {
                await historyService.AddAsync(HistoryEntryType.Generate, InputText.TrimmedOrEmpty()).ConfigureAwait(false);
                HistorySaved?.Invoke(this, EventArgs.Empty);
            }
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

    private void ClearResult()
    {
        QrCodeImage = null;
        HasQrCode = false;
        ErrorMessage = string.Empty;
        DeleteTempFile();
    }

    private void DeleteTempFile()
    {
        if (_tempFilePath is not { } path)
        {
            return;
        }

        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
        catch
        {
            // Temp file cleanup is best-effort.
        }

        _tempFilePath = null;
    }
}
