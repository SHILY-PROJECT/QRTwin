using System.Globalization;

namespace QRTwin.Maui.Converters;

public sealed class DateTimeToLocalStringConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is DateTime dateTime
            ? dateTime.ToLocalTime().ToString("dd.MM.yyyy HH:mm", culture)
            : string.Empty;

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
