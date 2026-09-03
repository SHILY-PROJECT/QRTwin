#if ANDROID
using AView = Android.Views.View;

namespace QRTwin.Effects;

public static partial class GlassBlur
{
    static partial void ApplyPlatform(VisualElement element, float radius)
    {
        // Android RenderEffect blur is content blur (children become unreadable), not backdrop
        // blur. Match Windows: rely on frosted SurfaceGlass fills + bloom shadows instead.
        ClearPlatform(element);
    }

    static partial void ClearPlatform(VisualElement element)
    {
        if (element.Handler?.PlatformView is not AView nativeView)
        {
            return;
        }

        if (OperatingSystem.IsAndroidVersionAtLeast(31))
        {
            nativeView.SetRenderEffect(null);
        }
    }
}
#endif
