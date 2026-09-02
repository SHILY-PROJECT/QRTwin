using QRTwin.Models;

namespace QRTwin.Themes;

public sealed record AppThemeDescriptor(
    AppThemeId Id,
    string DisplayName,
    string Description,
    Func<AppThemePalette> CreatePalette);

public static class AppThemeCatalog
{
    public const string PreferenceKey = "AppThemeId";

    public static AppThemeId DefaultThemeId => AppThemeId.Neon;

    public static IReadOnlyList<AppThemeDescriptor> All { get; } =
    [
        new(
            AppThemeId.Neon,
            "Неон",
            "Градиент иконки QRTwin: циан, синий и фиолетовый",
            CreateNeonPalette),
        new(
            AppThemeId.Classic,
            "Классика",
            "Тёмная тема с оранжевыми акцентами",
            CreateClassicPalette)
    ];

    public static AppThemePalette GetPalette(AppThemeId themeId) =>
        All.First(theme => theme.Id == themeId).CreatePalette();

    public static AppThemeDescriptor GetDescriptor(AppThemeId themeId) =>
        All.First(theme => theme.Id == themeId);

    private static AppThemePalette CreateNeonPalette() => new()
    {
        AppBackground = Color.FromArgb("#0B1028"),
        AppBackgroundDeep = Color.FromArgb("#060918"),
        Surface = Color.FromArgb("#CC1A2248"),
        SurfaceElevated = Color.FromArgb("#D9222D5C"),
        SurfaceGlass = Color.FromArgb("#B3182038"),
        PrimaryText = Color.FromArgb("#FFFFFF"),
        SecondaryText = Color.FromArgb("#D9C8D8F0"),
        MutedText = Color.FromArgb("#997A8FA8"),
        Accent = Color.FromArgb("#00D4FF"),
        AccentLight = Color.FromArgb("#5CE1FF"),
        AccentGlow = Color.FromArgb("#4000D4FF"),
        AccentSoft = Color.FromArgb("#2600D4FF"),
        Danger = Color.FromArgb("#FF5C7A"),
        Success = Color.FromArgb("#00E676"),
        ScannerLine = Color.FromArgb("#00FFFF"),
        Border = Color.FromArgb("#CC2A3560"),
        BorderLight = Color.FromArgb("#CC3D4A78"),
        PageBackgroundBrush = new LinearGradientBrush
        {
            StartPoint = new Point(0, 0),
            EndPoint = new Point(1, 1),
            GradientStops =
            [
                new(Color.FromArgb("#0B1028"), 0f),
                new(Color.FromArgb("#121A3A"), 0.45f),
                new(Color.FromArgb("#1A1248"), 1f)
            ]
        },
        BackgroundGlowBrush = new RadialGradientBrush
        {
            Center = new Point(0.5, 0),
            Radius = 1.4,
            GradientStops =
            [
                new(Color.FromArgb("#5500E5FF"), 0f),
                new(Color.FromArgb("#352962FF"), 0.35f),
                new(Color.FromArgb("#186200EA"), 0.65f),
                new(Color.FromArgb("#000B1028"), 1f)
            ]
        },
        AccentGradientBrush = new LinearGradientBrush
        {
            StartPoint = new Point(0, 0),
            EndPoint = new Point(1, 1),
            GradientStops =
            [
                new(Color.FromArgb("#00E5FF"), 0f),
                new(Color.FromArgb("#2962FF"), 0.55f),
                new(Color.FromArgb("#7B1FA2"), 1f)
            ]
        },
        CardGradientBrush = new LinearGradientBrush
        {
            StartPoint = new Point(0, 0),
            EndPoint = new Point(0, 1),
            GradientStops =
            [
                new(Color.FromArgb("#CC243058"), 0f),
                new(Color.FromArgb("#CC182040"), 1f)
            ]
        },
        ScannerBeamBrush = new LinearGradientBrush
        {
            StartPoint = new Point(0, 0),
            EndPoint = new Point(0, 1),
            GradientStops =
            [
                new(Color.FromArgb("#0000FFFF"), 0f),
                new(Color.FromArgb("#3300FFFF"), 0.55f),
                new(Color.FromArgb("#8000FFFF"), 0.85f),
                new(Color.FromArgb("#00FFFF"), 1f)
            ]
        }
    };

    private static AppThemePalette CreateClassicPalette() => new()
    {
        AppBackground = Color.FromArgb("#121212"),
        AppBackgroundDeep = Color.FromArgb("#0A0A0A"),
        Surface = Color.FromArgb("#D91E1E1E"),
        SurfaceElevated = Color.FromArgb("#D9282828"),
        SurfaceGlass = Color.FromArgb("#D91E1E1E"),
        PrimaryText = Color.FromArgb("#FFFFFF"),
        SecondaryText = Color.FromArgb("#D99E9E9E"),
        MutedText = Color.FromArgb("#D9666666"),
        Accent = Color.FromArgb("#FF4D00"),
        AccentLight = Color.FromArgb("#FF6B2C"),
        AccentGlow = Color.FromArgb("#40FF4D00"),
        AccentSoft = Color.FromArgb("#1AFF4D00"),
        Danger = Color.FromArgb("#FF4444"),
        Success = Color.FromArgb("#4CAF50"),
        ScannerLine = Color.FromArgb("#FF4D00"),
        Border = Color.FromArgb("#D92A2A2A"),
        BorderLight = Color.FromArgb("#D93A3A3A"),
        PageBackgroundBrush = new SolidColorBrush(Color.FromArgb("#121212")),
        BackgroundGlowBrush = new RadialGradientBrush
        {
            Center = new Point(0.5, 0),
            Radius = 1.4,
            GradientStops =
            [
                new(Color.FromArgb("#35FF4D00"), 0f),
                new(Color.FromArgb("#18FF4D00"), 0.35f),
                new(Color.FromArgb("#00121212"), 1f)
            ]
        },
        AccentGradientBrush = new LinearGradientBrush
        {
            StartPoint = new Point(0, 0),
            EndPoint = new Point(1, 1),
            GradientStops =
            [
                new(Color.FromArgb("#FF4D00"), 0f),
                new(Color.FromArgb("#FF6B2C"), 1f)
            ]
        },
        CardGradientBrush = new LinearGradientBrush
        {
            StartPoint = new Point(0, 0),
            EndPoint = new Point(0, 1),
            GradientStops =
            [
                new(Color.FromArgb("#D9242424"), 0f),
                new(Color.FromArgb("#D91A1A1A"), 1f)
            ]
        },
        ScannerBeamBrush = new LinearGradientBrush
        {
            StartPoint = new Point(0, 0),
            EndPoint = new Point(0, 1),
            GradientStops =
            [
                new(Color.FromArgb("#00FF4D00"), 0f),
                new(Color.FromArgb("#33FF4D00"), 0.55f),
                new(Color.FromArgb("#80FF4D00"), 0.85f),
                new(Color.FromArgb("#FF4D00"), 1f)
            ]
        }
    };
}
