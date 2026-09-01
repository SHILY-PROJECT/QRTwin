namespace QRTwin.Maui.Services;

public interface IQrCodeService
{
    Task<ImageSource?> GenerateQrCodeAsync(string content, int size = 512);

    Task<string> SaveToTempFileAsync(ImageSource imageSource, string fileName = "qrcode.png");

    bool IsUrl(string? text);
}
