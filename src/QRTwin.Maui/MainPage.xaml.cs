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
        viewModel.PropertyChanged += OnViewModelPropertyChanged;
        UpdateTabVisuals(viewModel.SelectedTab);
        UpdateTabPanels(viewModel.SelectedTab);
    }

    private void OnViewModelPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (sender is MainViewModel vm && e.PropertyName == nameof(MainViewModel.SelectedTab))
        {
            UpdateTabVisuals(vm.SelectedTab);
            UpdateTabPanels(vm.SelectedTab);
        }
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

        var outgoing = newTab == AppTab.Scan ? GeneratePanel : ScanPanel;
        var incoming = newTab == AppTab.Scan ? ScanPanel : GeneratePanel;

        incoming.IsVisible = true;
        incoming.Opacity = 0;
        incoming.TranslationX = newTab == AppTab.Scan ? -24 : 24;

        await Task.WhenAll(
            outgoing.FadeToAsync(0, 160, Easing.CubicOut),
            outgoing.TranslateToAsync(newTab == AppTab.Scan ? 24 : -24, 0, 160, Easing.CubicOut));

        _viewModel.SelectedTab = newTab;

        await Task.WhenAll(
            incoming.FadeToAsync(1, 200, Easing.CubicOut),
            incoming.TranslateToAsync(0, 0, 200, Easing.CubicOut));

        outgoing.Opacity = 1;
        outgoing.TranslationX = 0;
    }

    private void UpdateTabPanels(AppTab selectedTab)
    {
        var isScan = selectedTab == AppTab.Scan;
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

        var isScan = selectedTab == AppTab.Scan;

        ScanTab.BackgroundColor = isScan ? accent : Colors.Transparent;
        GenerateTab.BackgroundColor = isScan ? Colors.Transparent : accent;

        ScanTabIcon.IconColor = isScan ? Colors.White : secondary;
        GenerateTabIcon.IconColor = isScan ? secondary : Colors.White;

        ScanTabLabel.TextColor = isScan ? Colors.White : secondary;
        GenerateTabLabel.TextColor = isScan ? secondary : Colors.White;
    }
}
