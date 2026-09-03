using QRTwin.Services;
using QRTwin.Themes;

namespace QRTwin.Effects;

public static class GlassEffect
{
    public static readonly BindableProperty IntensityProperty =
        BindableProperty.CreateAttached(
            "Intensity",
            typeof(GlassEffectIntensity),
            typeof(GlassEffect),
            GlassEffectIntensity.Normal,
            propertyChanged: OnIntensityChanged);

    private static readonly HashSet<Border> Hooked = [];

    public static GlassEffectIntensity GetIntensity(BindableObject view) =>
        (GlassEffectIntensity)view.GetValue(IntensityProperty);

    public static void SetIntensity(BindableObject view, GlassEffectIntensity value) =>
        view.SetValue(IntensityProperty, value);

    private static void OnIntensityChanged(BindableObject bindable, object oldValue, object newValue)
    {
        if (bindable is not Border border)
        {
            return;
        }

        EnsureHooked(border);
        ApplyEffects(border);
    }

    private static void EnsureHooked(Border border)
    {
        if (!Hooked.Add(border))
        {
            return;
        }

        border.HandlerChanged += (_, _) => ApplyEffects(border);

        if (Application.Current?.Handler?.MauiContext?.Services.GetService(typeof(IThemeService)) is IThemeService themeService)
        {
            themeService.ThemeChanged += (_, _) => ApplyEffects(border);
        }
    }

    internal static void ApplyEffects(Border border)
    {
        if (GetEffects() is not { IsEnabled: true } effects)
        {
            ClearEffects(border);
            return;
        }

        var (bloomRadius, bloomOpacity, blurRadius) = effects.GetPreset(GetIntensity(border));

        if (GetIntensity(border) is GlassEffectIntensity.Elevated)
        {
            border.Shadow = new Shadow
            {
                Brush = new SolidColorBrush(effects.DropShadowColor),
                Radius = effects.DropShadowRadius,
                Offset = new Point(0, effects.DropShadowOffsetY),
                Opacity = effects.DropShadowOpacity
            };
        }
        else
        {
            border.Shadow = new Shadow
            {
                Brush = effects.BloomColor,
                Radius = bloomRadius,
                Offset = new Point(0, 0),
                Opacity = bloomOpacity
            };
        }

        GlassBlur.Apply(border, blurRadius);
    }

    public static void Refresh(Border border)
    {
        if (!border.IsSet(IntensityProperty))
        {
            return;
        }

        ApplyEffects(border);
    }

    private static GlassVisualEffects? GetEffects()
    {
        if (Application.Current?.Resources.TryGetValue("GlassVisualEffects", out var value) != true)
        {
            return null;
        }

        return value as GlassVisualEffects;
    }

    private static void ClearEffects(Border border)
    {
        border.ClearValue(VisualElement.ShadowProperty);
        GlassBlur.Clear(border);
    }

    public static void RefreshVisualTree(Element element)
    {
        if (element is Border border && border.IsSet(IntensityProperty))
        {
            ApplyEffects(border);
        }

        switch (element)
        {
            case Layout layout:
                foreach (var child in layout.Children)
                {
                    if (child is Element childElement)
                    {
                        RefreshVisualTree(childElement);
                    }
                }

                break;
            case ContentView { Content: Element contentViewRoot }:
                RefreshVisualTree(contentViewRoot);
                break;
            case ScrollView { Content: Element scrollContent }:
                RefreshVisualTree(scrollContent);
                break;
            case Border { Content: Element borderContent }:
                RefreshVisualTree(borderContent);
                break;
        }
    }
}
