using SkiaSharp;
using ZXing;
using ZXing.QrCode;

namespace QRTwin.Maui.Services;

public sealed class QrCodeService : IQrCodeService
{
    public Task<ImageSource?> GenerateQrCodeAsync(string content, int size = 512)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            return Task.FromResult<ImageSource?>(null);
        }

        return Task.Run<ImageSource?>(() =>
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

            var pixelData = writer.Write(content.Trim());
            if (pixelData is null)
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
            using var stream = new MemoryStream(data.ToArray());
            return ImageSource.FromStream(() => new MemoryStream(data.ToArray()));
        });
    }

    public async Task<string> SaveToTempFileAsync(ImageSource imageSource, string fileName = "qrcode.png")
    {
        var tempPath = Path.Combine(FileSystem.CacheDirectory, fileName);

        if (imageSource is StreamImageSource streamImageSource)
        {
            await using var stream = await streamImageSource.Stream(CancellationToken.None);
            if (stream is null)
            {
                throw new InvalidOperationException("Не удалось получить поток изображения.");
            }

            await using var fileStream = File.Create(tempPath);
            await stream.CopyToAsync(fileStream).ConfigureAwait(false);
            return tempPath;
        }

        throw new NotSupportedException("Поддерживается только сохранение из StreamImageSource.");
    }

    public bool IsUrl(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return false;
        }

        return Uri.TryCreate(text.Trim(), UriKind.Absolute, out var uri)
               && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
    }
}
