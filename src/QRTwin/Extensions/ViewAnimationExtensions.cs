namespace QRTwin.Extensions;

public static class ViewAnimationExtensions
{
    public const uint StandardDuration = 320;
    public const uint TabDuration = 300;
    public const uint OverlayDuration = 340;

    public static readonly Easing StandardEase = Easing.CubicInOut;
    public static readonly Easing EnterEase = Easing.CubicOut;
    public static readonly Easing ExitEase = Easing.CubicIn;

    public static Task FadeSlideToAsync(
        this VisualElement element,
        double opacity,
        double translationY,
        uint duration = StandardDuration,
        Easing? easing = null) =>
        Task.WhenAll(
            element.FadeToAsync(opacity, duration, easing ?? StandardEase),
            element.TranslateToAsync(element.TranslationX, translationY, duration, easing ?? StandardEase));

    public static Task FadeSlideXToAsync(
        this VisualElement element,
        double opacity,
        double translationX,
        uint duration = StandardDuration,
        Easing? easing = null) =>
        Task.WhenAll(
            element.FadeToAsync(opacity, duration, easing ?? StandardEase),
            element.TranslateToAsync(translationX, element.TranslationY, duration, easing ?? StandardEase));
}
