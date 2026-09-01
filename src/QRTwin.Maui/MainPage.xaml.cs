namespace QRTwin.Maui;

public partial class MainPage : ContentPage
{
    private readonly MainViewModel _viewModel;

    public MainPage(MainViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
        viewModel.PropertyChanged += OnViewModelPropertyChanged;
        UpdateTabVisuals(viewModel.SelectedTab);
        UpdateTabPanels(viewModel.SelectedTab);
    }

    private void OnViewModelPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (sender is MainViewModel vm && e.IsProperty(nameof(MainViewModel.SelectedTab)))
        {
            UpdateTabVisuals(vm.SelectedTab);
            UpdateTabPanels(vm.SelectedTab);
        }
    }

    private async void OnScanTabTapped(object? sender, TappedEventArgs e)
    {
        if (_viewModel.SelectedTab is AppTab.Scan)
        {
            return;
        }

        await AnimateTabChangeAsync(AppTab.Scan);
    }

    private async void OnGenerateTabTapped(object? sender, TappedEventArgs e)
    {
        if (_viewModel.SelectedTab is AppTab.Generate)
        {
            return;
        }

        await AnimateTabChangeAsync(AppTab.Generate);
    }

    private async Task AnimateTabChangeAsync(AppTab newTab)
    {
        var (outgoing, incoming, incomingOffset, outgoingOffset) = newTab switch
        {
            AppTab.Scan => (GeneratePanel, ScanPanel, -24, 24),
            AppTab.Generate => (ScanPanel, GeneratePanel, 24, -24),
            _ => (ScanPanel, GeneratePanel, 0, 0)
        };

        incoming.IsVisible = true;
        incoming.Opacity = 0;
        incoming.TranslationX = incomingOffset;

        await Task.WhenAll(
            outgoing.FadeToAsync(0, 160, Easing.CubicOut),
            outgoing.TranslateToAsync(outgoingOffset, 0, 160, Easing.CubicOut));

        _viewModel.SelectedTab = newTab;

        await Task.WhenAll(
            incoming.FadeToAsync(1, 200, Easing.CubicOut),
            incoming.TranslateToAsync(0, 0, 200, Easing.CubicOut));

        outgoing.Opacity = 1;
        outgoing.TranslationX = 0;
    }

    private void UpdateTabPanels(AppTab selectedTab)
    {
        var isScan = selectedTab is AppTab.Scan;
        ScanPanel.IsVisible = isScan;
        GeneratePanel.IsVisible = !isScan;
        ScanPanel.Opacity = 1;
        GeneratePanel.Opacity = 1;
        ScanPanel.TranslationX = 0;
        GeneratePanel.TranslationX = 0;
    }

    private void UpdateTabVisuals(AppTab selectedTab)
    {
        var accent = (Color)Application.Current!.Resources["Accent"];
        var secondary = (Color)Application.Current.Resources["SecondaryText"];

        var (scanTabBg, generateTabBg, scanIconColor, generateIconColor, scanLabelColor, generateLabelColor) =
            selectedTab switch
            {
                AppTab.Scan => (accent, Colors.Transparent, Colors.White, secondary, Colors.White, secondary),
                AppTab.Generate => (Colors.Transparent, accent, secondary, Colors.White, secondary, Colors.White),
                _ => (Colors.Transparent, Colors.Transparent, secondary, secondary, secondary, secondary)
            };

        ScanTab.BackgroundColor = scanTabBg;
        GenerateTab.BackgroundColor = generateTabBg;
        ScanTabIcon.IconColor = scanIconColor;
        GenerateTabIcon.IconColor = generateIconColor;
        ScanTabLabel.TextColor = scanLabelColor;
        GenerateTabLabel.TextColor = generateLabelColor;
    }
}
