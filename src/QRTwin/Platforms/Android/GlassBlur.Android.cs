#if ANDROID
using Android.Graphics;
using Android.Views;
using Android.Widget;
using AView = Android.Views.View;

namespace QRTwin.Effects;

public static partial class GlassBlur
{
    static partial void ApplyPlatform(VisualElement element, float radius)
    {
        if (element.Handler?.PlatformView is not AView nativeView)
        {
            return;
        }

        if (!OperatingSystem.IsAndroidVersionAtLeast(31))
        {
            return;
        }

        var blurRadius = Math.Clamp(radius, 0f, 32f);
        nativeView.SetRenderEffect(
            RenderEffect.CreateBlurEffect(blurRadius, blurRadius, Shader.TileMode.Clamp!));
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
