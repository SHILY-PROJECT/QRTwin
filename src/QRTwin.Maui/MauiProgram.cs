using SkiaSharp.Views.Maui.Controls.Hosting;
using CommunityToolkit.Maui;
using Microsoft.Extensions.Logging;
using QRTwin.Maui.Extensions;
using ZXing.Net.Maui;
using ZXing.Net.Maui.Controls;
using SQLitePCL;

namespace QRTwin.Maui;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        Batteries_V2.Init();
        raw.FreezeProvider();

        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .UseMauiCommunityToolkit()
            .UseSkiaSharp()
            .UseBarcodeReader()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            })
            .AddQRTwinServices();

#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}
