using QRTwin.Models;

namespace QRTwin.Services;

public interface IQrCodeService
{
    Task<ImageSource?> GenerateQrCodeAsync(string content, QrEncodeOptions options = default);

    Task<string> SaveToTempFileAsync(ImageSource imageSource, string fileName = "qrcode.png");

    bool IsUrl(string? text);
}
