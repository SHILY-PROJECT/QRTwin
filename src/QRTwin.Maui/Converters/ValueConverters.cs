namespace QRTwin.Maui.Converters;

public sealed class AppTabToBoolConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is AppTab tab && parameter is string tabName
            ? tab.ToString().Equals(tabName, StringComparison.OrdinalIgnoreCase)
            : false;

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        parameter is string tabName && value is true
            ? Enum.Parse<AppTab>(tabName)
            : Binding.DoNothing;
}

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

public sealed class InvertedBoolConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is bool b ? !b : value;

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is bool b ? !b : value;
}

public sealed class IsStringNotNullOrEmptyConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is string s && s.IsNotBlank();

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

public sealed class DateTimeToLocalStringConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is DateTime dateTime
            ? dateTime.ToLocalTime().ToString("dd.MM.yyyy HH:mm", culture)
            : string.Empty;

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

public sealed class BoolToOpacityConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is true ? 1d : 0d;

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
