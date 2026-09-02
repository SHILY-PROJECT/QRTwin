namespace QRTwin.Themes;

public sealed class AppThemePalette
{
    public required Color AppBackground { get; init; }

    public required Color AppBackgroundDeep { get; init; }

    public required Color Surface { get; init; }

    public required Color SurfaceElevated { get; init; }

    public required Color SurfaceGlass { get; init; }

    public required Color PrimaryText { get; init; }

    public required Color SecondaryText { get; init; }

    public required Color MutedText { get; init; }

    public required Color Accent { get; init; }

    public required Color AccentLight { get; init; }

    public required Color AccentGlow { get; init; }

    public required Color AccentSoft { get; init; }

    public required Color Danger { get; init; }

    public required Color Success { get; init; }

    public required Color ScannerLine { get; init; }

    public required Color Border { get; init; }

    public required Color BorderLight { get; init; }

    public required Brush PageBackgroundBrush { get; init; }

    public required Brush BackgroundGlowBrush { get; init; }

    public required Brush BackgroundGlowSecondaryBrush { get; init; }

    public required Brush AccentGradientBrush { get; init; }

    public required Brush CardGradientBrush { get; init; }

    public required Brush OverlayScrimBrush { get; init; }

    public required Brush OverlayPanelBrush { get; init; }

    public required Brush OverlayItemBrush { get; init; }

    public required Brush ScannerBeamBrush { get; init; }

    public GlassVisualEffects VisualEffects { get; init; } = GlassVisualEffects.Disabled;

    public Brush PreviewBrush => AccentGradientBrush;

    public void ApplyTo(ResourceDictionary resources)
    {
        SetColor(resources, "AppBackground", AppBackground);
        SetColor(resources, "AppBackgroundDeep", AppBackgroundDeep);
        SetColor(resources, "Surface", Surface);
        SetColor(resources, "SurfaceElevated", SurfaceElevated);
        SetColor(resources, "SurfaceGlass", SurfaceGlass);
        SetColor(resources, "PrimaryText", PrimaryText);
        SetColor(resources, "SecondaryText", SecondaryText);
        SetColor(resources, "MutedText", MutedText);
        SetColor(resources, "Accent", Accent);
        SetColor(resources, "AccentLight", AccentLight);
        SetColor(resources, "AccentGlow", AccentGlow);
        SetColor(resources, "AccentSoft", AccentSoft);
        SetColor(resources, "Danger", Danger);
        SetColor(resources, "Success", Success);
        SetColor(resources, "ScannerLine", ScannerLine);
        SetColor(resources, "Border", Border);
        SetColor(resources, "BorderLight", BorderLight);

        SetBrush(resources, "PageBackgroundBrush", PageBackgroundBrush);
        SetBrush(resources, "BackgroundGlowBrush", BackgroundGlowBrush);
        SetBrush(resources, "BackgroundGlowSecondaryBrush", BackgroundGlowSecondaryBrush);
        SetBrush(resources, "AccentGradientBrush", AccentGradientBrush);
        SetBrush(resources, "CardGradientBrush", CardGradientBrush);
        SetBrush(resources, "OverlayScrimBrush", OverlayScrimBrush);
        SetBrush(resources, "OverlayPanelBrush", OverlayPanelBrush);
        SetBrush(resources, "OverlayItemBrush", OverlayItemBrush);
        SetBrush(resources, "ScannerBeamBrush", ScannerBeamBrush);

        resources["GlassVisualEffects"] = VisualEffects;

        SetBrush(resources, "AppBackgroundBrush", new SolidColorBrush(AppBackground));
        SetBrush(resources, "SurfaceBrush", new SolidColorBrush(Surface));
        SetBrush(resources, "AccentBrush", new SolidColorBrush(Accent));
        SetBrush(resources, "BorderBrush", new SolidColorBrush(Border));
    }

    private static void SetColor(ResourceDictionary resources, string key, Color color) =>
        resources[key] = color;

    private static void SetBrush(ResourceDictionary resources, string key, Brush brush) =>
        resources[key] = brush;
}
