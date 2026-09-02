namespace QRTwin;

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
            Title = "QRTwin"
        };

#if WINDOWS
        Platforms.Windows.WindowGeometryPersistence.Attach(window);
#endif

        return window;
    }
}
