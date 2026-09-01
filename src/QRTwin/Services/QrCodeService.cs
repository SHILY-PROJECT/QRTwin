using QRTwin.Diagnostics;
using QRTwin.Models;
using SkiaSharp;
using ZXing;
using ZXing.QrCode;

namespace QRTwin.Services;

public sealed class QrCodeService() : IQrCodeService
{
    private const int PngQuality = 100;

    public async Task<ImageSource?> GenerateQrCodeAsync(string content, QrEncodeOptions options = default)
    {
        if (!content.IsNotBlank())
        {
            return null;
        }

        options = options.Size is 0 ? QrEncodeOptions.Default : options;
        var pngBytes = await Task.Run(() => EncodeQrCodeToPng(content.TrimmedOrEmpty(), options))
            .ConfigureAwait(false);

        return pngBytes is null
            ? null
            : await MainThread.InvokeOnMainThreadAsync(() => ImageSource.FromStream(() => new MemoryStream(pngBytes)));
    }

    private static byte[]? EncodeQrCodeToPng(string content, QrEncodeOptions options)
    {
        var writer = new BarcodeWriterPixelData
        {
            Format = BarcodeFormat.QR_CODE,
            Options = new QrCodeEncodingOptions
            {
                Height = options.Size,
                Width = options.Size,
                Margin = options.Margin,
                CharacterSet = options.CharacterSet
            }
        };

        if (writer.Write(content) is not { } pixelData)
        {
            return null;
        }

        using var bitmap = new SKBitmap(
            pixelData.Width,
            pixelData.Height,
            SKColorType.Bgra8888,
            SKAlphaType.Premul);

        CopyPixelsToBitmap(pixelData.Pixels.AsSpan(), bitmap);

        using var image = SKImage.FromBitmap(bitmap);
        using var data = image.Encode(SKEncodedImageFormat.Png, PngQuality);
        return data.ToArray();
    }

    private static void CopyPixelsToBitmap(ReadOnlySpan<byte> source, SKBitmap bitmap)
    {
        var destination = bitmap.GetPixelSpan();
        if (source.Length != destination.Length)
        {
            throw new InvalidOperationException(
                $"Несовпадение размера пикселей: источник {source.Length}, назначение {destination.Length}.");
        }

        source.CopyTo(destination);
    }

    public async Task<string> SaveToTempFileAsync(ImageSource imageSource, string fileName = "qrcode.png")
    {
        Guard.NotNull(imageSource);
        var tempPath = Path.Combine(FileSystem.CacheDirectory, fileName);
        await imageSource.SaveToFileAsync(tempPath).ConfigureAwait(false);
        return tempPath;
    }

    public bool IsUrl(string? text) => text.IsHttpUrl();
}
