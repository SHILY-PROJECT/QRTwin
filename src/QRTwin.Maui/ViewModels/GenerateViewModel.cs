using CommunityToolkit.Maui.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using QRTwin.Maui.Models;
using QRTwin.Maui.Services;

namespace QRTwin.Maui.ViewModels;

public partial class GenerateViewModel : ObservableObject
{
    private readonly IHistoryService _historyService;
    private readonly IQrCodeService _qrCodeService;

    [ObservableProperty]
    private string _inputText = string.Empty;

    [ObservableProperty]
    private ImageSource? _qrCodeImage;

    [ObservableProperty]
    private bool _hasQrCode;

    [ObservableProperty]
    private bool _isGenerating;

    [ObservableProperty]
    private bool _isActive;

    [ObservableProperty]
    private string _errorMessage = string.Empty;

    private string? _tempFilePath;

    public event EventHandler? HistorySaved;

    public GenerateViewModel(IHistoryService historyService, IQrCodeService qrCodeService)
    {
        _historyService = historyService;
        _qrCodeService = qrCodeService;
    }

    [RelayCommand]
    private async Task GenerateAsync()
    {
        if (string.IsNullOrWhiteSpace(InputText))
        {
            ErrorMessage = "Введите текст или ссылку для генерации QR-кода.";
            return;
        }

        ErrorMessage = string.Empty;
        IsGenerating = true;

        try
        {
            var image = await _qrCodeService.GenerateQrCodeAsync(InputText, 640).ConfigureAwait(false);
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

            _tempFilePath = await _qrCodeService.SaveToTempFileAsync(image).ConfigureAwait(false);
            await _historyService.AddAsync(HistoryEntryType.Generate, InputText.Trim()).ConfigureAwait(false);
            HistorySaved?.Invoke(this, EventArgs.Empty);
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Ошибка генерации: {ex.Message}";
        }
        finally
        {
            IsGenerating = false;
        }
    }

    [RelayCommand]
    private async Task ShareAsync()
    {
        if (string.IsNullOrWhiteSpace(_tempFilePath) || !File.Exists(_tempFilePath))
        {
            if (QrCodeImage is not null)
            {
                _tempFilePath = await _qrCodeService.SaveToTempFileAsync(QrCodeImage).ConfigureAwait(false);
            }
            else
            {
                return;
            }
        }

        await Share.Default.RequestAsync(new ShareFileRequest
        {
            Title = "Поделиться QR-кодом",
            File = new ShareFile(_tempFilePath)
        }).ConfigureAwait(false);
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        if (string.IsNullOrWhiteSpace(_tempFilePath) || !File.Exists(_tempFilePath))
        {
            if (QrCodeImage is not null)
            {
                _tempFilePath = await _qrCodeService.SaveToTempFileAsync(QrCodeImage).ConfigureAwait(false);
            }
            else
            {
                return;
            }
        }

        await using var stream = File.OpenRead(_tempFilePath);
        var result = await FileSaver.Default.SaveAsync("qrcode.png", stream).ConfigureAwait(false);

        if (result is null)
        {
            ErrorMessage = "Сохранение отменено.";
        }
    }
}
