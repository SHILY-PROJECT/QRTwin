#if WINDOWS
namespace QRTwin.Effects;

public static partial class GlassBlur
{
    static partial void ApplyPlatform(VisualElement element, float radius)
    {
        // Per-element backdrop blur is not reliably available for MAUI Border on WinUI.
        // Glass theme uses cyan bloom (Shadow) plus semi-transparent fills on Windows.
    }

    static partial void ClearPlatform(VisualElement element)
    {
    }
}
#endif
