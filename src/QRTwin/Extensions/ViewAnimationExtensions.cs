namespace QRTwin.Extensions;

public static class ViewAnimationExtensions
{
    public const uint StandardDuration = 320;
    public const uint TabDuration = 300;
    public const uint OverlayDuration = 340;
    public const uint EditorExpandDuration = 420;
    public const uint HistoryRemoveDuration = 360;

    public static readonly Easing StandardEase = Easing.CubicInOut;
    public static readonly Easing EnterEase = Easing.CubicOut;
    public static readonly Easing ExitEase = Easing.CubicIn;

    public static Task AnimateHeightRequestAsync(
        this VisualElement element,
        double targetHeight,
        uint duration = EditorExpandDuration,
        Easing? easing = null,
        string? animationName = null)
    {
        animationName ??= $"HeightRequest_{element.GetHashCode()}";
        element.AbortAnimation(animationName);

        var startHeight = element.HeightRequest;
        if (double.IsNaN(startHeight) || startHeight < 0)
        {
            startHeight = element.Height;
        }

        if (Math.Abs(startHeight - targetHeight) < 0.5)
        {
            element.HeightRequest = targetHeight;
            return Task.CompletedTask;
        }

        var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var animation = new Animation(
            value => element.HeightRequest = value,
            startHeight,
            targetHeight,
            easing: easing ?? StandardEase);

        animation.Commit(
            element,
            animationName,
            length: duration,
            easing: easing ?? StandardEase,
            finished: (_, cancelled) =>
            {
                if (!cancelled)
                {
                    element.HeightRequest = targetHeight;
                }

                tcs.TrySetResult();
            });

        return tcs.Task;
    }

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

    public static Color InterpolateColor(Color from, Color to, double progress) =>
        Color.FromRgba(
            from.Red + ((to.Red - from.Red) * progress),
            from.Green + ((to.Green - from.Green) * progress),
            from.Blue + ((to.Blue - from.Blue) * progress),
            from.Alpha + ((to.Alpha - from.Alpha) * progress));

    public static Task AnimateIconColorAsync(
        this Controls.SvgIconView icon,
        Color from,
        Color to,
        uint duration = StandardDuration,
        Easing? easing = null,
        string? animationName = null)
    {
        animationName ??= $"IconColor_{icon.GetHashCode()}";
        icon.AbortAnimation(animationName);

        if (from == to)
        {
            icon.IconColor = to;
            return Task.CompletedTask;
        }

        var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var animation = new Animation(
            progress => icon.IconColor = InterpolateColor(from, to, progress),
            easing: easing ?? StandardEase);

        animation.Commit(
            icon,
            animationName,
            length: duration,
            easing: easing ?? StandardEase,
            finished: (_, _) => tcs.TrySetResult());

        return tcs.Task;
    }
}
