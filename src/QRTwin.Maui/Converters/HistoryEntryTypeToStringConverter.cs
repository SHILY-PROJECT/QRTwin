using System.Globalization;
using QRTwin.Maui.Models;

namespace QRTwin.Maui.Converters;

public sealed class HistoryEntryTypeToStringConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is HistoryEntryType type
            ? type switch
            {
                HistoryEntryType.Scan => "Сканирование",
                HistoryEntryType.Generate => "Генерация",
                _ => string.Empty
            }
            : string.Empty;

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
