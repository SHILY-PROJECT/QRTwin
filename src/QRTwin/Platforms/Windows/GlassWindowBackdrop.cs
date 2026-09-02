#if WINDOWS
using Microsoft.Maui.Platform;
using Microsoft.UI.Composition.SystemBackdrops;
using Microsoft.UI.Xaml.Media;
using QRTwin.Models;
using QRTwin.Services;

namespace QRTwin.Platforms.Windows;

internal static class GlassWindowBackdrop
{
    public static void Attach(Window window, IThemeService themeService)
    {
        void ApplyBackdrop()
        {
            if (window.Handler?.PlatformView is not MauiWinUIWindow nativeWindow)
            {
                return;
            }

            nativeWindow.SystemBackdrop = themeService.CurrentThemeId == AppThemeId.Glass
                ? new DesktopAcrylicBackdrop()
                : null;
        }

        window.HandlerChanged += (_, _) => ApplyBackdrop();
        themeService.ThemeChanged += (_, _) => ApplyBackdrop();
        ApplyBackdrop();
    }
}
#endif
