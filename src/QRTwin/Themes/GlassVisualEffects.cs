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

    public (float Radius, float Opacity, float Blur) GetPreset(GlassEffectIntensity intensity) =>
        intensity switch
        {
            GlassEffectIntensity.Strong => (StrongBloomRadius, StrongBloomOpacity, StrongBlurRadius),
            GlassEffectIntensity.Subtle => (SubtleBloomRadius, SubtleBloomOpacity, SubtleBlurRadius),
            _ => (NormalBloomRadius, NormalBloomOpacity, NormalBlurRadius)
        };
}

public enum GlassEffectIntensity
{
    Subtle,
    Normal,
    Strong
}
