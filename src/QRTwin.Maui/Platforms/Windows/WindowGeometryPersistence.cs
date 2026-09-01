using Microsoft.Maui.Platform;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using WinRT.Interop;
using Windows.Graphics;

namespace QRTwin.Maui.Platforms.Windows;

public static class WindowGeometryPersistence
{
    private const string KeyHasSaved = "window_geometry_saved";
    private const string KeyX = "window_x";
    private const string KeyY = "window_y";
    private const string KeyWidth = "window_width";
    private const string KeyHeight = "window_height";

    private const int DefaultWidth = 440;
    private const int DefaultHeight = 820;
    private const int MinWidth = 360;
    private const int MinHeight = 560;

    public static void Attach(Window window)
    {
        window.HandlerChanged += OnHandlerChanged;

        void OnHandlerChanged(object? sender, EventArgs e)
        {
            if (window.Handler?.PlatformView is not MauiWinUIWindow nativeWindow)
            {
                return;
            }

            window.HandlerChanged -= OnHandlerChanged;

            var appWindow = GetAppWindow(nativeWindow);
            if (appWindow is null)
            {
                return;
            }

            Restore(appWindow);

            nativeWindow.Closed += (_, _) => Save(appWindow);
            window.Destroying += (_, _) => Save(appWindow);
        }
    }

    private static AppWindow? GetAppWindow(MauiWinUIWindow nativeWindow)
    {
        var hwnd = WindowNative.GetWindowHandle(nativeWindow);
        var windowId = Win32Interop.GetWindowIdFromWindow(hwnd);
        return AppWindow.GetFromWindowId(windowId);
    }

    private static void Restore(AppWindow appWindow)
    {
        if (!Preferences.Get(KeyHasSaved, false))
        {
            appWindow.Resize(new SizeInt32(DefaultWidth, DefaultHeight));
            CenterOnPrimaryDisplay(appWindow, DefaultWidth, DefaultHeight);
            return;
        }

        var width = Math.Clamp(Preferences.Get(KeyWidth, DefaultWidth), MinWidth, 4000);
        var height = Math.Clamp(Preferences.Get(KeyHeight, DefaultHeight), MinHeight, 4000);
        var x = Preferences.Get(KeyX, 0);
        var y = Preferences.Get(KeyY, 0);

        var rect = ClampToVisibleArea(new RectInt32(x, y, width, height));
        appWindow.MoveAndResize(rect);
    }

    private static void Save(AppWindow appWindow)
    {
        if (appWindow.Presenter is not OverlappedPresenter)
        {
            return;
        }

        var position = appWindow.Position;
        var size = appWindow.Size;

        if (size.Width < MinWidth || size.Height < MinHeight)
        {
            return;
        }

        Preferences.Set(KeyX, position.X);
        Preferences.Set(KeyY, position.Y);
        Preferences.Set(KeyWidth, size.Width);
        Preferences.Set(KeyHeight, size.Height);
        Preferences.Set(KeyHasSaved, true);
    }

    private static void CenterOnPrimaryDisplay(AppWindow appWindow, int width, int height)
    {
        var displayArea = DisplayArea.GetFromPoint(new PointInt32(0, 0), DisplayAreaFallback.Primary);
        var workArea = displayArea.WorkArea;
        var x = workArea.X + (workArea.Width - width) / 2;
        var y = workArea.Y + (workArea.Height - height) / 2;
        appWindow.MoveAndResize(new RectInt32(x, y, width, height));
    }

    private static RectInt32 ClampToVisibleArea(RectInt32 rect)
    {
        var displayArea = DisplayArea.GetFromRect(rect, DisplayAreaFallback.Nearest);
        var work = displayArea.WorkArea;

        rect.Width = Math.Clamp(rect.Width, MinWidth, work.Width);
        rect.Height = Math.Clamp(rect.Height, MinHeight, work.Height);

        if (rect.X < work.X)
        {
            rect.X = work.X;
        }

        if (rect.Y < work.Y)
        {
            rect.Y = work.Y;
        }

        if (rect.X + rect.Width > work.X + work.Width)
        {
            rect.X = Math.Max(work.X, work.X + work.Width - rect.Width);
        }

        if (rect.Y + rect.Height > work.Y + work.Height)
        {
            rect.Y = Math.Max(work.Y, work.Y + work.Height - rect.Height);
        }

        return rect;
    }
}
