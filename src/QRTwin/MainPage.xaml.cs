using QRTwin.Extensions;
using QRTwin.Models;
using QRTwin.ViewModels;

namespace QRTwin;

public partial class MainPage : ContentPage
{
    private const double CollapsedEditorHeight = 44;
    private const double ActionButtonSize = 44;
    private const double InputBarPadding = 12;
    private const double ButtonsBlockSpacing = 8;
    private const double SeparatorBlockHeight = 17;

    private readonly MainViewModel _viewModel;
    private readonly Color _inactiveButtonBackground;
    private readonly Color _inactiveIconColor;
    private readonly Color _activeIconColor;
    private readonly Color _separatorInactiveColor;
    private readonly Color _separatorActiveColor;
    private bool _isUnloaded;

    public MainPage(MainViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;

        _inactiveButtonBackground = (Color)Application.Current.Resources["SurfaceElevated"];
        _inactiveIconColor = (Color)Application.Current.Resources["MutedText"];
        _activeIconColor = Colors.White;
        _separatorInactiveColor = (Color)Application.Current.Resources["Border"];
        _separatorActiveColor = (Color)Application.Current.Resources["Accent"];

        viewModel.PropertyChanged += OnViewModelPropertyChanged;
        viewModel.Generate.PropertyChanged += OnGenerateViewModelPropertyChanged;
        Unloaded += OnUnloaded;
        UpdateTabVisuals(viewModel.SelectedTab);
        UpdateTabPanels(viewModel.SelectedTab);
        UpdateInputBarButtonStates(_viewModel.Generate.InputText.IsNotBlank());
        UpdateInputEditorSeparatorState(isFocused: false);

#if ANDROID
        Platforms.Android.KeyboardInsetsHelper.Attach(RootLayout, GenerateInputBar);
#endif
    }

    private void OnUnloaded(object? sender, EventArgs e)
    {
        _isUnloaded = true;
        _viewModel.PropertyChanged -= OnViewModelPropertyChanged;
        _viewModel.Generate.PropertyChanged -= OnGenerateViewModelPropertyChanged;
        ScanPanel.StopAnimations();
        GeneratePanel.StopAnimations();
    }

    private void OnGenerateViewModelPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.IsProperty(nameof(GenerateViewModel.InputText)))
        {
            UpdateInputBarButtonStates(_viewModel.Generate.InputText.IsNotBlank());
        }
    }

    private void UpdateInputBarButtonStates(bool hasText)
    {
        var activeBrush = (Brush)Application.Current!.Resources["AccentGradientBrush"];

        ImageGenButton.Background = hasText ? activeBrush : _inactiveButtonBackground;
        WandButton.Background = hasText ? activeBrush : _inactiveButtonBackground;
        ImageGenIcon.IconColor = hasText ? _activeIconColor : _inactiveIconColor;
        WandIcon.IconColor = hasText ? _activeIconColor : _inactiveIconColor;
    }

    private void UpdateInputEditorSeparatorState(bool isFocused)
    {
        InputEditorSeparator.Color = isFocused ? _separatorActiveColor : _separatorInactiveColor;
    }

    private void OnGenerateInputEditorFocused(object? sender, FocusEventArgs e)
    {
        if (_isUnloaded)
        {
            return;
        }

        UpdateInputEditorSeparatorState(isFocused: true);
        Dispatcher.Dispatch(ExpandGenerateInputEditor);
    }

    private void OnGenerateInputEditorUnfocused(object? sender, FocusEventArgs e)
    {
        UpdateInputEditorSeparatorState(isFocused: false);
        GenerateInputEditor.HeightRequest = CollapsedEditorHeight;
    }

    private void OnGenerateInputEditorCompleted(object? sender, EventArgs e)
    {
        if (_viewModel.Generate.GenerateCommand.CanExecute(null))
        {
            _viewModel.Generate.GenerateCommand.Execute(null);
        }

        GenerateInputEditor.Unfocus();
    }

    private void ExpandGenerateInputEditor()
    {
        if (_isUnloaded)
        {
            return;
        }

        var maxHeight = CalculateExpandedEditorMaxHeight();
        GenerateInputEditor.HeightRequest = Math.Max(CollapsedEditorHeight, maxHeight);
    }

    private double CalculateExpandedEditorMaxHeight()
    {
        var buttonsBlock = ActionButtonSize + ButtonsBlockSpacing + SeparatorBlockHeight;
        var chrome = InputBarPadding + GenerateInputBar.Padding.Top + GenerateInputBar.Padding.Bottom;

        if (ContentHost.Height <= 0)
        {
            return 120;
        }

        var inputBarTop = ContentHost.Y + ContentHost.Height;
        var contentTop = ContentHost.Y;

#if WINDOWS
        if (GenerateContent.FindByName<Border>("EmptyStateCard") is { IsVisible: true } emptyStateCard
            && emptyStateCard.Height > 0)
        {
            var cardBottom = contentTop + GetOffsetY(emptyStateCard, ContentHost) + emptyStateCard.Height;
            var availableToCard = inputBarTop - cardBottom - 16 - buttonsBlock - chrome;
            if (availableToCard > CollapsedEditorHeight)
            {
                return availableToCard;
            }
        }
#endif

        var available = inputBarTop - contentTop - buttonsBlock - chrome;
        return Math.Max(CollapsedEditorHeight, available);
    }

    private static double GetOffsetY(VisualElement element, VisualElement ancestor)
    {
        var offset = 0d;
        var current = element;

        while (current is not null && current != ancestor)
        {
            offset += current.Y;
            current = current.Parent as VisualElement;
        }

        return offset;
    }

    private void OnImageGenTapped(object? sender, TappedEventArgs e)
    {
        if (!_viewModel.Generate.InputText.IsNotBlank())
        {
            return;
        }

        if (_viewModel.Generate.GenerateImageCommand.CanExecute(null))
        {
            _viewModel.Generate.GenerateImageCommand.Execute(null);
        }
    }

    private void OnWandTapped(object? sender, TappedEventArgs e)
    {
        if (!_viewModel.Generate.InputText.IsNotBlank())
        {
            return;
        }

        _viewModel.Generate.ClearFromUi();
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
        if (_isUnloaded)
        {
            return;
        }

        var (outgoing, incoming, incomingOffset, outgoingOffset) = newTab switch
        {
            AppTab.Scan => (GeneratePanel, ScanPanel, -24, 24),
            AppTab.Generate => (ScanPanel, GeneratePanel, 24, -24),
            _ => (ScanPanel, GeneratePanel, 0, 0)
        };

        incoming.IsVisible = true;
        incoming.Opacity = 0;
        incoming.TranslationX = incomingOffset;

        try
        {
            await Task.WhenAll(
                outgoing.FadeToAsync(0, 160, Easing.CubicOut),
                outgoing.TranslateToAsync(outgoingOffset, 0, 160, Easing.CubicOut));

            if (_isUnloaded)
            {
                return;
            }

            _viewModel.SelectedTab = newTab;

            await Task.WhenAll(
                incoming.FadeToAsync(1, 200, Easing.CubicOut),
                incoming.TranslateToAsync(0, 0, 200, Easing.CubicOut));
        }
        catch (Exception ex) when (ViewLifecycleExtensions.IsShutdownException(ex))
        {
            return;
        }

        if (_isUnloaded)
        {
            return;
        }

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
