using SkiaSharp.Views.Maui.Controls.Hosting;
using CommunityToolkit.Maui;
using Microsoft.Extensions.Logging;
using QRTwin.Maui.Services;
using QRTwin.Maui.ViewModels;
using QRTwin.Maui.Views;
using ZXing.Net.Maui;
using ZXing.Net.Maui.Controls;
using SQLitePCL;

namespace QRTwin.Maui;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        Batteries_V2.Init();

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
            });

        builder.Services.AddSingleton<IHistoryService, HistoryService>();
        builder.Services.AddSingleton<IQrCodeService, QrCodeService>();
        builder.Services.AddSingleton<IPermissionService, PermissionService>();

        builder.Services.AddSingleton<ScanViewModel>();
        builder.Services.AddSingleton<GenerateViewModel>();
        builder.Services.AddSingleton<HistoryViewModel>();
        builder.Services.AddSingleton<MainViewModel>();

        builder.Services.AddSingleton<MainPage>();
        builder.Services.AddTransient<ScanView>();
        builder.Services.AddTransient<GenerateView>();
        builder.Services.AddTransient<HistoryView>();

#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}
