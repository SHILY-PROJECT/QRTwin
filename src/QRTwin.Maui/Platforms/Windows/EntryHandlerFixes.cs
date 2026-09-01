#if WINDOWS
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Platform;

namespace QRTwin.Maui.Platforms.Windows;

internal static class EntryHandlerFixes
{
    public static void DisableNativeClearButton()
    {
        EntryHandler.Mapper.AppendToMapping(
            "QRTwin.DisableClearButton",
            static (handler, view) =>
            {
                if (view is not Microsoft.Maui.Controls.Entry entry)
                {
                    return;
                }

                EntryHandler.MapClearButtonVisibility(handler, entry);
            });
    }
}
#endif
