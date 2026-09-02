using QRTwin.Services;
using QRTwin.ViewModels;
using QRTwin.Views;

namespace QRTwin.Extensions;

public static class MauiAppBuilderExtensions
{
    extension(MauiAppBuilder builder)
    {
        public MauiAppBuilder AddQRTwinServices()
        {
            builder.Services.AddSingleton<IHistoryService, HistoryService>();
            builder.Services.AddSingleton<IQrCodeService, QrCodeService>();
            builder.Services.AddSingleton<IPermissionService, PermissionService>();
            builder.Services.AddSingleton<IThemeService, ThemeService>();

            builder.Services.AddSingleton<ScanViewModel>();
            builder.Services.AddSingleton<GenerateViewModel>();
            builder.Services.AddSingleton<HistoryViewModel>();
            builder.Services.AddSingleton<ThemesViewModel>();
            builder.Services.AddSingleton<MainViewModel>(sp =>
            {
                var mainViewModel = new MainViewModel(
                    sp.GetRequiredService<ScanViewModel>(),
                    sp.GetRequiredService<GenerateViewModel>(),
                    sp.GetRequiredService<HistoryViewModel>(),
                    sp.GetRequiredService<ThemesViewModel>(),
                    sp.GetRequiredService<IThemeService>());
                mainViewModel.Initialize();
                return mainViewModel;
            });

            builder.Services.AddSingleton<MainPage>();
            builder.Services.AddTransient<ScanView>();
            builder.Services.AddTransient<GenerateView>();
            builder.Services.AddTransient<HistoryView>();
            builder.Services.AddTransient<ThemesView>();

            return builder;
        }
    }
}
