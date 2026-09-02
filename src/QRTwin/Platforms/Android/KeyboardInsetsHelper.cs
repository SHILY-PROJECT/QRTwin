using Android.Views;
using AndroidX.Core.View;
using AView = Android.Views.View;
using MauiView = Microsoft.Maui.Controls.View;

namespace QRTwin.Platforms.Android;

static class KeyboardInsetsHelper
{
    public static void Attach(MauiView rootView, params MauiView[] liftWithKeyboard)
    {
        if (rootView.Handler?.PlatformView is AView nativeRoot)
        {
            Apply(nativeRoot, liftWithKeyboard);
            return;
        }

        rootView.HandlerChanged += OnHandlerChanged;

        void OnHandlerChanged(object? sender, EventArgs e)
        {
            if (rootView.Handler?.PlatformView is not AView native)
            {
                return;
            }

            rootView.HandlerChanged -= OnHandlerChanged;
            Apply(native, liftWithKeyboard);
        }
    }

    private static void Apply(AView nativeRoot, MauiView[] liftWithKeyboard)
    {
        ViewCompat.SetOnApplyWindowInsetsListener(
            nativeRoot,
            new LiftOnKeyboardInsetsListener(liftWithKeyboard));
        ViewCompat.RequestApplyInsets(nativeRoot);
    }

    private sealed class LiftOnKeyboardInsetsListener(MauiView[] views) : Java.Lang.Object, IOnApplyWindowInsetsListener
    {
        private readonly Dictionary<MauiView, Thickness> _baseMargins = views.ToDictionary(v => v, v => v.Margin);

        public WindowInsetsCompat? OnApplyWindowInsets(AView? v, WindowInsetsCompat? insets)
        {
            if (insets is null)
            {
                return insets;
            }

            var density = DeviceDisplay.MainDisplayInfo.Density;
            if (density <= 0)
            {
                density = 1;
            }

            var imeBottom = insets.GetInsets(WindowInsetsCompat.Type.Ime()).Bottom / density;
            var systemBottom = insets.GetInsets(WindowInsetsCompat.Type.SystemBars()).Bottom / density;
            var keyboardLift = Math.Max(0, imeBottom - systemBottom);

            foreach (var view in views)
            {
                var margin = _baseMargins[view];
                view.Margin = new Thickness(
                    margin.Left,
                    margin.Top,
                    margin.Right,
                    margin.Bottom + keyboardLift);
            }

            return insets;
        }
    }
}
