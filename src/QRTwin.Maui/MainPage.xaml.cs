using QRTwin.Maui.Models;
using QRTwin.Maui.ViewModels;

namespace QRTwin.Maui;

public partial class MainPage : ContentPage
{
    private MainViewModel? _viewModel;

    public MainPage(MainViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
        UpdateTabVisuals(viewModel.SelectedTab);
    }

    private async void OnScanTabTapped(object? sender, TappedEventArgs e)
    {
        if (_viewModel is null || _viewModel.SelectedTab == AppTab.Scan)
        {
            return;
        }

        await AnimateTabChangeAsync(AppTab.Scan);
    }

    private async void OnGenerateTabTapped(object? sender, TappedEventArgs e)
    {
        if (_viewModel is null || _viewModel.SelectedTab == AppTab.Generate)
        {
            return;
        }

        await AnimateTabChangeAsync(AppTab.Generate);
    }

    private async Task AnimateTabChangeAsync(AppTab newTab)
    {
        if (_viewModel is null)
        {
            return;
        }

        var outgoing = (View)(newTab == AppTab.Scan ? GenerateContent : ScanContent);
        var incoming = (View)(newTab == AppTab.Scan ? ScanContent : GenerateContent);

        outgoing.IsVisible = true;
        incoming.IsVisible = true;
        incoming.Opacity = 0;
        incoming.TranslationX = newTab == AppTab.Scan ? -30 : 30;

        await Task.WhenAll(
            outgoing.FadeToAsync(0, 180, Easing.CubicOut),
            outgoing.TranslateToAsync(newTab == AppTab.Scan ? 30 : -30, 0, 180, Easing.CubicOut));

        outgoing.IsVisible = false;
        outgoing.Opacity = 1;
        outgoing.TranslationX = 0;

        _viewModel.SelectedTab = newTab;
        UpdateTabVisuals(newTab);

        await Task.WhenAll(
            incoming.FadeToAsync(1, 220, Easing.CubicOut),
            incoming.TranslateToAsync(0, 0, 220, Easing.CubicOut));
    }

    private void UpdateTabVisuals(AppTab selectedTab)
    {
        var accent = (Color)Application.Current!.Resources["Accent"];
        var secondary = (Color)Application.Current.Resources["SecondaryText"];
        var surfaceElevated = (Color)Application.Current.Resources["SurfaceElevated"];

        var isScan = selectedTab == AppTab.Scan;

        ScanTab.BackgroundColor = isScan ? surfaceElevated : Colors.Transparent;
        GenerateTab.BackgroundColor = isScan ? Colors.Transparent : surfaceElevated;

        ScanTabIcon.IconColor = isScan ? accent : secondary;
        GenerateTabIcon.IconColor = isScan ? secondary : accent;

        ScanTabLabel.TextColor = isScan ? accent : secondary;
        GenerateTabLabel.TextColor = isScan ? secondary : accent;
    }
}
