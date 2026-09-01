namespace QRTwin.Maui;

public partial class App : Application
{
    public App()
    {
        InitializeComponent();
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        var services = IPlatformApplication.Current?.Services
                       ?? throw new InvalidOperationException("Сервисы приложения недоступны.");

        var mainPage = services.GetRequiredService<MainPage>();
        var window = new Window(mainPage)
        {
            Title = "QRTwin"
        };

#if WINDOWS
        Platforms.Windows.WindowGeometryPersistence.Attach(window);
#endif

        return window;
    }
}
