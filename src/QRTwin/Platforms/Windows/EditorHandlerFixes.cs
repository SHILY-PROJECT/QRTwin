#if WINDOWS
using Microsoft.Maui.Handlers;
using Microsoft.UI.Xaml.Controls;
using WinSolidColorBrush = Microsoft.UI.Xaml.Media.SolidColorBrush;
using WinColors = Microsoft.UI.Colors;

namespace QRTwin.Platforms.Windows;

internal static class EditorHandlerFixes
{
    public static void HideNativeBorder()
    {
        EditorHandler.Mapper.AppendToMapping(
            "QRTwin.HideEditorBorder",
            static (handler, view) =>
            {
                if (handler.PlatformView is not TextBox textBox)
                {
                    return;
                }

                ConfigureTextBox(textBox);
                textBox.GotFocus += (_, _) => ConfigureTextBox(textBox);
                textBox.LostFocus += (_, _) => ConfigureTextBox(textBox);
            });
    }

    private static void ConfigureTextBox(TextBox textBox)
    {
        textBox.UseSystemFocusVisuals = false;
        textBox.Padding = new Microsoft.UI.Xaml.Thickness(0);
        textBox.BorderThickness = new Microsoft.UI.Xaml.Thickness(0);
        textBox.BorderBrush = new WinSolidColorBrush(WinColors.Transparent);
        textBox.Background = new WinSolidColorBrush(WinColors.Transparent);

        ApplyTransparentTextControlResources(textBox.Resources);
    }

    private static void ApplyTransparentTextControlResources(Microsoft.UI.Xaml.ResourceDictionary resources)
    {
        var transparentBrush = new WinSolidColorBrush(WinColors.Transparent);
        var zeroThickness = new Microsoft.UI.Xaml.Thickness(0);

        resources["TextControlBorderBrush"] = transparentBrush;
        resources["TextControlBorderBrushPointerOver"] = transparentBrush;
        resources["TextControlBorderBrushFocused"] = transparentBrush;
        resources["TextControlBorderBrushDisabled"] = transparentBrush;
        resources["TextControlBackground"] = transparentBrush;
        resources["TextControlBackgroundPointerOver"] = transparentBrush;
        resources["TextControlBackgroundFocused"] = transparentBrush;
        resources["TextControlBackgroundDisabled"] = transparentBrush;
        resources["TextControlBorderThemeThickness"] = zeroThickness;
        resources["TextControlBorderThemeThicknessFocused"] = zeroThickness;
        resources["TextControlBorderThemeThicknessPointerOver"] = zeroThickness;
        resources["TextControlBorderThemeThicknessDisabled"] = zeroThickness;
    }
}
#endif
