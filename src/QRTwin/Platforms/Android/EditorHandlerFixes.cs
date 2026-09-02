#if ANDROID
using Android.Content.Res;
using Android.Graphics;
using Android.Graphics.Drawables;
using AndroidX.AppCompat.Widget;
using Microsoft.Maui.Handlers;
using AColor = Android.Graphics.Color;

namespace QRTwin.Platforms.Android;

internal static class EditorHandlerFixes
{
    private static readonly ColorStateList TransparentTint = ColorStateList.ValueOf(AColor.Transparent);

    public static void HideNativeUnderline()
    {
        EditorHandler.Mapper.AppendToMapping(
            "QRTwin.HideEditorUnderline",
            static (handler, view) =>
            {
                if (handler.PlatformView is not AppCompatEditText editText)
                {
                    return;
                }

                ConfigureEditText(editText);
                editText.FocusChange += (_, _) => ConfigureEditText(editText);
            });
    }

    private static void ConfigureEditText(AppCompatEditText editText)
    {
        editText.SetPadding(0, 0, 0, 0);
        editText.Background = new ColorDrawable(AColor.Transparent);
        editText.SetBackgroundColor(AColor.Transparent);
        editText.BackgroundTintList = TransparentTint;
        editText.BackgroundTintMode = PorterDuff.Mode.SrcIn;
        editText.SetHighlightColor(AColor.Transparent);
    }
}
#endif
