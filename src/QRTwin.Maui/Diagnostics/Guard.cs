namespace QRTwin.Maui.Diagnostics;

public static class Guard
{
    public static T NotNull<T>(
        T? value,
        [CallerArgumentExpression(nameof(value))] string? expression = null)
        where T : class =>
        value ?? throw new ArgumentNullException(expression);

    public static string NotBlank(
        string? value,
        [CallerArgumentExpression(nameof(value))] string? expression = null) =>
        value.IsNotBlank()
            ? value.TrimmedOrEmpty()
            : throw new ArgumentException("Значение не может быть пустым.", expression);
}
