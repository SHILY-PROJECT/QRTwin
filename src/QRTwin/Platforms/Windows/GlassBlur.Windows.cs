#if WINDOWS
namespace QRTwin.Effects;

public static partial class GlassBlur
{
    static partial void ApplyPlatform(VisualElement element, float radius)
    {
        // WinUI does not expose reliable per-Border backdrop blur in MAUI.
        // Glass panels use opaque frosted fills from the theme palette instead.
    }

    static partial void ClearPlatform(VisualElement element)
    {
    }
}
#endif
