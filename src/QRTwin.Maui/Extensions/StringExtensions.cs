namespace QRTwin.Maui.Extensions;

public static class StringExtensions
{
    extension(string? value)
    {
        public bool IsNotBlank() => !string.IsNullOrWhiteSpace(value);

        public string TrimmedOrEmpty() => value?.Trim() ?? string.Empty;

        public bool IsHttpUrl() =>
            value.IsNotBlank()
            && Uri.TryCreate(value.TrimmedOrEmpty(), UriKind.Absolute, out var uri)
            && uri.Scheme is "http" or "https";
    }
}
