using QRTwin.Maui.Extensions;
using SkiaSharp;
using ZXing;
using ZXing.QrCode;

namespace QRTwin.Maui.Services;

public sealed class QrCodeService() : IQrCodeService
{
    public async Task<ImageSource?> GenerateQrCodeAsync(string content, int size = 512)
    {
        if (!content.IsNotBlank()) return null;
        
        var pngBytes = await Task.Run(() => EncodeQrCodeToPng(content.TrimmedOrEmpty(), size)).ConfigureAwait(false);

        return pngBytes is null ? null : await MainThread.InvokeOnMainThreadAsync(() => ImageSource.FromStream(() => new MemoryStream(pngBytes)));
    }

    private static byte[]? EncodeQrCodeToPng(string content, int size)
    {
        var writer = new BarcodeWriterPixelData
        {
            Format = BarcodeFormat.QR_CODE,
            Options = new QrCodeEncodingOptions
            {
                Height = size,
                Width = size,
                Margin = 2,
                CharacterSet = "UTF-8"
            }
        };

        if (writer.Write(content) is not { } pixelData)
        {
            return null;
        }

        using var bitmap = new SKBitmap(pixelData.Width, pixelData.Height, SKColorType.Bgra8888, SKAlphaType.Premul);
        System.Runtime.InteropServices.Marshal.Copy(
            pixelData.Pixels,
            0,
            bitmap.GetPixels(),
            pixelData.Pixels.Length);

        using var image = SKImage.FromBitmap(bitmap);
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);
        return data.ToArray();
    }

    public async Task<string> SaveToTempFileAsync(ImageSource imageSource, string fileName = "qrcode.png")
    {
        var tempPath = Path.Combine(FileSystem.CacheDirectory, fileName);
        await imageSource.SaveToFileAsync(tempPath).ConfigureAwait(false);
        return tempPath;
    }

    public bool IsUrl(string? text) => text.IsHttpUrl();
}
