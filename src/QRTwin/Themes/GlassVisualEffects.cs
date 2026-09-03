namespace QRTwin.Themes;

public sealed class GlassVisualEffects
{
    public static GlassVisualEffects Disabled { get; } = new() { IsEnabled = false };

    public bool IsEnabled { get; init; }

    public Color BloomColor { get; init; } = Colors.Transparent;

    public float SubtleBloomRadius { get; init; } = 8;

    public float NormalBloomRadius { get; init; } = 16;

    public float StrongBloomRadius { get; init; } = 24;

    public float SubtleBloomOpacity { get; init; } = 0.3f;

    public float NormalBloomOpacity { get; init; } = 0.45f;

    public float StrongBloomOpacity { get; init; } = 0.58f;

    public float SubtleBlurRadius { get; init; } = 8;

    public float NormalBlurRadius { get; init; } = 14;

    public float StrongBlurRadius { get; init; } = 20;

    public float ElevatedBlurRadius { get; init; } = 22;

    public Color DropShadowColor { get; init; } = Color.FromArgb("#59000000");

    public float DropShadowRadius { get; init; } = 14;

    public float DropShadowOffsetY { get; init; } = 8;

    public float DropShadowOpacity { get; init; } = 0.32f;

    public (float Radius, float Opacity, float Blur) GetPreset(GlassEffectIntensity intensity) =>
        intensity switch
        {
            GlassEffectIntensity.Strong => (StrongBloomRadius, StrongBloomOpacity, StrongBlurRadius),
            GlassEffectIntensity.Subtle => (SubtleBloomRadius, SubtleBloomOpacity, SubtleBlurRadius),
            GlassEffectIntensity.Elevated => (0, 0, ElevatedBlurRadius),
            _ => (NormalBloomRadius, NormalBloomOpacity, NormalBlurRadius)
        };
}

public enum GlassEffectIntensity
{
    Subtle,
    Normal,
    Strong,
    Elevated
}
