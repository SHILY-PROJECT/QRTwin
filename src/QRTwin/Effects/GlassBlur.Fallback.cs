#if !WINDOWS && !ANDROID
namespace QRTwin.Effects;

public static partial class GlassBlur
{
    static partial void ApplyPlatform(VisualElement element, float radius)
    {
    }

    static partial void ClearPlatform(VisualElement element)
    {
    }
}
#endif
