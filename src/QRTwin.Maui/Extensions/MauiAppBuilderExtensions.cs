using QRTwin.Maui.Services;
using QRTwin.Maui.ViewModels;
using QRTwin.Maui.Views;

namespace QRTwin.Maui.Extensions;

public static class MauiAppBuilderExtensions
{
    extension(MauiAppBuilder builder)
    {
        public MauiAppBuilder AddQRTwinServices()
        {
            builder.Services.AddSingleton<IHistoryService, HistoryService>();
            builder.Services.AddSingleton<IQrCodeService, QrCodeService>();
            builder.Services.AddSingleton<IPermissionService, PermissionService>();

            builder.Services.AddSingleton<ScanViewModel>();
            builder.Services.AddSingleton<GenerateViewModel>();
            builder.Services.AddSingleton<HistoryViewModel>();
            builder.Services.AddSingleton<MainViewModel>(sp =>
            {
                var mainViewModel = new MainViewModel(
                    sp.GetRequiredService<ScanViewModel>(),
                    sp.GetRequiredService<GenerateViewModel>(),
                    sp.GetRequiredService<HistoryViewModel>());
                mainViewModel.Initialize();
                return mainViewModel;
            });

            builder.Services.AddSingleton<MainPage>();
            builder.Services.AddTransient<ScanView>();
            builder.Services.AddTransient<GenerateView>();
            builder.Services.AddTransient<HistoryView>();

            return builder;
        }
    }
}
