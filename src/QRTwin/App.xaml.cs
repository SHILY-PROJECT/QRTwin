namespace QRTwin;

using QRTwin.Services;

public partial class App : Application
{
    public App(IThemeService themeService)
    {
        InitializeComponent();
        themeService.Initialize();
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        if (IPlatformApplication.Current?.Services is not { } services)
        {
            throw new InvalidOperationException("Сервисы приложения недоступны.");
        }

        var window = new Window(services.GetRequiredService<MainPage>())
        {
            Title = "QRTwin - Сканируйте и создавайте QR-коды"
        };

#if WINDOWS
        Platforms.Windows.WindowGeometryPersistence.Attach(window);
        Platforms.Windows.GlassWindowBackdrop.Attach(window, services.GetRequiredService<IThemeService>());
#endif

        return window;
    }
}
