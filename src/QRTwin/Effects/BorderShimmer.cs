using QRTwin.Extensions;

namespace QRTwin.Effects;

/// <summary>
/// Theme gradient on <see cref="Border.Stroke"/> only. Palette slides with Fract
/// so the loop is seamless on square and flat bounds alike.
/// </summary>
public static class BorderShimmer
{
    private const int FrameDelayMs = 24;
    private const double SweepSeconds = 2.8;

    public static readonly BindableProperty IsEnabledProperty =
        BindableProperty.CreateAttached(
            "IsEnabled",
            typeof(bool),
            typeof(BorderShimmer),
            false,
            propertyChanged: OnIsEnabledChanged);

    private static readonly BindableProperty BaseStrokeProperty =
        BindableProperty.CreateAttached(
            "BaseStroke",
            typeof(Color),
            typeof(BorderShimmer),
            Colors.Transparent);

    private static readonly BindableProperty BaseStrokeCapturedProperty =
        BindableProperty.CreateAttached(
            "BaseStrokeCaptured",
            typeof(bool),
            typeof(BorderShimmer),
            false);

    public static bool GetIsEnabled(BindableObject view) =>
        (bool)view.GetValue(IsEnabledProperty);

    public static void SetIsEnabled(BindableObject view, bool value) =>
        view.SetValue(IsEnabledProperty, value);

    private static readonly object Gate = new();
    private static readonly List<WeakReference<Border>> Targets = [];
    private static int _loopRunning;
    private static Color[] _cachedColors = [];
    private static int _cachedThemeVersion = -1;
    private static int _themeVersion;

    private static void OnIsEnabledChanged(BindableObject bindable, object oldValue, object newValue)
    {
        if (bindable is not Border border)
        {
            return;
        }

        if (newValue is true)
        {
            Register(border);
            return;
        }

        Unregister(border);
        RestoreBaseStroke(border);
        if (!border.IsSet(GlassEffect.IntensityProperty))
        {
            border.ClearValue(VisualElement.ShadowProperty);
        }
    }

    private static void Register(Border border)
    {
        CaptureBaseStroke(border);
        border.ClearValue(VisualElement.ShadowProperty);

        lock (Gate)
        {
            PruneUnlocked();
            if (!Targets.Any(reference => reference.TryGetTarget(out var existing) && ReferenceEquals(existing, border)))
            {
                Targets.Add(new WeakReference<Border>(border));
            }
        }

        border.Unloaded -= OnBorderUnloaded;
        border.Unloaded += OnBorderUnloaded;
        ApplyFrame(border, CurrentPhase());
        EnsureLoop();
    }

    private static void Unregister(Border border)
    {
        border.Unloaded -= OnBorderUnloaded;
        lock (Gate)
        {
            Targets.RemoveAll(reference =>
                !reference.TryGetTarget(out var existing) || ReferenceEquals(existing, border));
        }
    }

    private static void OnBorderUnloaded(object? sender, EventArgs e)
    {
        if (sender is Border border)
        {
            Unregister(border);
            RestoreBaseStroke(border);
            if (!border.IsSet(GlassEffect.IntensityProperty))
            {
                border.ClearValue(VisualElement.ShadowProperty);
            }
        }
    }

    /// <summary>Call when the app theme changes so palette colors refresh.</summary>
    public static void InvalidateThemeCache() => Interlocked.Increment(ref _themeVersion);

    private static void EnsureLoop()
    {
        if (Interlocked.CompareExchange(ref _loopRunning, 1, 0) != 0)
        {
            return;
        }

        _ = RunLoopAsync();
    }

    private static async Task RunLoopAsync()
    {
        try
        {
            await Task.Yield();

            while (true)
            {
                List<Border> live;
                lock (Gate)
                {
                    PruneUnlocked();
                    if (Targets.Count == 0)
                    {
                        return;
                    }

                    live = [];
                    foreach (var reference in Targets)
                    {
                        if (reference.TryGetTarget(out var border))
                        {
                            live.Add(border);
                        }
                    }
                }

                var phase = CurrentPhase();
                var colors = GetThemeColors();
                try
                {
                    // Stroke must be assigned on the UI thread — background updates
                    // leave some borders stuck on the style default (often near-white).
                    await MainThread.InvokeOnMainThreadAsync(() =>
                    {
                        foreach (var border in live)
                        {
                            ApplyFrame(border, phase, colors);
                        }
                    });
                }
                catch (Exception ex) when (ViewLifecycleExtensions.IsShutdownException(ex))
                {
                    return;
                }

                try
                {
                    await Task.Delay(FrameDelayMs);
                }
                catch (Exception ex) when (ViewLifecycleExtensions.IsShutdownException(ex))
                {
                    return;
                }
            }
        }
        finally
        {
            Interlocked.Exchange(ref _loopRunning, 0);
            lock (Gate)
            {
                PruneUnlocked();
                if (Targets.Count > 0)
                {
                    EnsureLoop();
                }
            }
        }
    }

    private static double CurrentPhase() =>
        Environment.TickCount64 / (SweepSeconds * 1000.0) % 1.0;

    private static void ApplyFrame(Border border, double phase) =>
        ApplyFrame(border, phase, GetThemeColors());

    private static void ApplyFrame(Border border, double phase, Color[] colors)
    {
        if (!GetIsEnabled(border) || border.Handler is null || colors.Length == 0)
        {
            return;
        }

        // Some styles leave thickness 0 (e.g. filled accent buttons) — force a visible outline.
        if (border.StrokeThickness <= 0)
        {
            border.StrokeThickness = 1.5;
        }

        var (start, end) = ResolveAxis(border);

        // Short opaque gradient — many stops + off-thread updates caused white strokes on WinUI.
        var brush = new LinearGradientBrush
        {
            StartPoint = start,
            EndPoint = end,
            GradientStops =
            [
                new(SamplePalette(colors, -phase), 0f),
                new(SamplePalette(colors, 0.33 - phase), 0.33f),
                new(SamplePalette(colors, 0.66 - phase), 0.66f),
                new(SamplePalette(colors, 1.0 - phase), 1f)
            ]
        };

        border.Stroke = brush;
    }

    private static (Point Start, Point End) ResolveAxis(Border border)
    {
        var width = border.Width > 1 ? border.Width : border.WidthRequest;
        var height = border.Height > 1 ? border.Height : border.HeightRequest;

        if (height > 1 && width > 1 && height > width * 1.25)
        {
            return (new Point(0.5, 0), new Point(0.5, 1));
        }

        if (width > 1 && height > 1 && Math.Abs(width - height) / Math.Max(width, height) < 0.2)
        {
            return (new Point(0, 0), new Point(1, 1));
        }

        return (new Point(0, 0.5), new Point(1, 0.5));
    }

    private static Color SamplePalette(Color[] colors, double u)
    {
        u -= Math.Floor(u);
        var scaled = u * colors.Length;
        var index = (int)Math.Floor(scaled);
        var frac = scaled - index;
        var from = colors[index % colors.Length];
        var to = colors[(index + 1) % colors.Length];
        var mixed = ViewAnimationExtensions.InterpolateColor(from, to, frac);
        // Force full opacity so thin strokes never wash out to white on glass themes.
        return mixed.Alpha >= 0.99f ? mixed : Color.FromRgb(mixed.Red, mixed.Green, mixed.Blue);
    }

    private static Color[] GetThemeColors()
    {
        var version = Volatile.Read(ref _themeVersion);
        if (version == _cachedThemeVersion && _cachedColors.Length > 0)
        {
            return _cachedColors;
        }

        var resources = Application.Current?.Resources;
        if (resources is null)
        {
            return _cachedColors.Length > 0 ? _cachedColors : [Color.FromArgb("#FF4D00"), Color.FromArgb("#FF6B2C")];
        }

        var list = new List<Color>();

        if (resources.TryGetValue("Accent", out var accentObj) && accentObj is Color accent)
        {
            list.Add(accent);
        }

        if (resources.TryGetValue("AccentGradientBrush", out var gradientObj)
            && gradientObj is LinearGradientBrush themeBrush)
        {
            foreach (var stop in themeBrush.GradientStops)
            {
                if (!IsNearWhite(stop.Color) && !ContainsSimilar(list, stop.Color))
                {
                    list.Add(stop.Color);
                }
            }
        }

        if (resources.TryGetValue("AccentLight", out var lightObj) && lightObj is Color light
            && !IsNearWhite(light) && !ContainsSimilar(list, light))
        {
            list.Add(light);
        }

        if (list.Count == 0)
        {
            list.Add(Color.FromArgb("#FF4D00"));
            list.Add(Color.FromArgb("#FF6B2C"));
        }
        else if (list.Count == 1)
        {
            list.Add(Lighten(list[0], 0.28));
        }

        _cachedColors = [.. list];
        _cachedThemeVersion = version;
        return _cachedColors;
    }

    private static bool IsNearWhite(Color color) =>
        color.Alpha > 0.8
        && color.Red > 0.88
        && color.Green > 0.88
        && color.Blue > 0.88;

    private static bool ContainsSimilar(List<Color> colors, Color candidate) =>
        colors.Any(existing =>
            Math.Abs(existing.Red - candidate.Red) < 0.04
            && Math.Abs(existing.Green - candidate.Green) < 0.04
            && Math.Abs(existing.Blue - candidate.Blue) < 0.04);

    private static Color Lighten(Color color, double amount) =>
        Color.FromRgba(
            color.Red + ((1 - color.Red) * amount),
            color.Green + ((1 - color.Green) * amount),
            color.Blue + ((1 - color.Blue) * amount),
            color.Alpha);

    private static void CaptureBaseStroke(Border border)
    {
        if ((bool)border.GetValue(BaseStrokeCapturedProperty))
        {
            return;
        }

        var color = border.Stroke is SolidColorBrush solid
            ? solid.Color
            : Colors.Transparent;

        border.SetValue(BaseStrokeProperty, color);
        border.SetValue(BaseStrokeCapturedProperty, true);
    }

    private static void RestoreBaseStroke(Border border)
    {
        if (!(bool)border.GetValue(BaseStrokeCapturedProperty))
        {
            return;
        }

        var baseColor = (Color)border.GetValue(BaseStrokeProperty);
        border.Stroke = baseColor.Alpha <= 0.01 ? Colors.Transparent : baseColor;
        border.ClearValue(BaseStrokeProperty);
        border.ClearValue(BaseStrokeCapturedProperty);
    }

    private static void PruneUnlocked() =>
        Targets.RemoveAll(reference => !reference.TryGetTarget(out _));
}
