namespace QRTwin.Extensions;

public static class ImageSourceExtensions
{
    extension(ImageSource imageSource)
    {
        public async Task SaveToFileAsync(string filePath, CancellationToken cancellationToken = default)
        {
            switch (imageSource)
            {
                case StreamImageSource streamImageSource:
                {
                    await using var stream = await streamImageSource.Stream(cancellationToken)
                        ?? throw new InvalidOperationException("Не удалось получить поток изображения.");
                    await using var fileStream = File.Create(filePath);
                    await stream.CopyToAsync(fileStream, cancellationToken).ConfigureAwait(false);
                    break;
                }
                default:
                    throw new NotSupportedException(
                        $"Сохранение поддерживается только для {nameof(StreamImageSource)}.");
            }
        }
    }
}
