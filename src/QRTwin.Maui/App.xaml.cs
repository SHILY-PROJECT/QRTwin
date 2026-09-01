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
        return new Window(mainPage);
    }
}
