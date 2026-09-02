using Microsoft.Maui.Controls.Shapes;
using QRTwin.Effects;
using QRTwin.Extensions;
using QRTwin.Models;
using QRTwin.Services;
using QRTwin.ViewModels;

namespace QRTwin;

public partial class MainPage : ContentPage
{
    private const double CollapsedEditorHeight = 44;
    private const double ExpansionAnchorGap = 12;
    private const string AuthorShimmerAnimationName = "AuthorShimmer";
    private static readonly Uri AuthorCreditUrl = new("https://github.com/SHILY-PROJECT");

    private readonly MainViewModel _viewModel;
    private readonly IThemeService _themeService;
    private Color _inactiveButtonBackground = null!;
    private Color _inactiveIconColor = null!;
    private readonly Color _activeIconColor = Colors.White;
    private Color _separatorInactiveColor = null!;
    private Color _separatorActiveColor = null!;
    private bool _isUnloaded;
    private bool _authorShimmerRunning;
    private double? _referenceExpandedEditorHeight;
    private AppTab _displayedTab;
    private bool _isTabAnimating;
    private bool _isPanning;
    private bool _historyOverlayUiVisible;
    private bool _themesOverlayUiVisible;
    private bool _generateInputBarVisible;
    private bool _swipeIsHorizontal;

    public MainPage(MainViewModel viewModel, IThemeService themeService)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
        _themeService = themeService;

        RefreshThemeColors();
        _themeService.ThemeChanged += OnThemeChanged;

        viewModel.PropertyChanged += OnViewModelPropertyChanged;
        viewModel.Generate.PropertyChanged += OnGenerateViewModelPropertyChanged;
        Unloaded += OnUnloaded;
        _displayedTab = viewModel.SelectedTab;
        UpdateTabVisuals(viewModel.SelectedTab);
        SyncTabPositions();
        UpdateInputBarButtonStates(_viewModel.Generate.InputText.IsNotBlank());
        UpdateInputEditorSeparatorState(isFocused: false);
        ContentHost.SizeChanged += OnContentHostSizeChanged;
        UpdateContentHostClip();

        var attachPan = new Action<View>(element =>
        {
            var pan = new PanGestureRecognizer();
            pan.PanUpdated += OnContentPanUpdated;
            element.GestureRecognizers.Add(pan);
        });
        attachPan(ScanPanel);
        attachPan(GeneratePanel);

        Loaded += OnLoaded;

#if ANDROID
        Platforms.Android.KeyboardInsetsHelper.Attach(RootLayout, GenerateInputBar);
#endif
    }

    private void OnThemeChanged(object? sender, EventArgs e)
    {
        if (_isUnloaded)
        {
            return;
        }

        MainThread.BeginInvokeOnMainThread(() =>
        {
            RefreshThemeColors();
            UpdateTabVisuals(_viewModel.SelectedTab);
            UpdateInputBarButtonStates(_viewModel.Generate.InputText.IsNotBlank());
            UpdateInputEditorSeparatorState(GenerateInputEditor.IsFocused);

            if (_historyOverlayUiVisible)
            {
                GlassEffect.RefreshVisualTree(HistoryOverlayPanel);
            }

            if (_themesOverlayUiVisible)
            {
                GlassEffect.RefreshVisualTree(ThemesOverlayPanel);
            }
        });
    }

    private void RefreshThemeColors()
    {
        _inactiveButtonBackground = (Color)Application.Current!.Resources["SurfaceElevated"];
        _inactiveIconColor = (Color)Application.Current.Resources["MutedText"];
        _separatorInactiveColor = (Color)Application.Current.Resources["Border"];
        _separatorActiveColor = (Color)Application.Current.Resources["Accent"];
    }

    private void OnLoaded(object? sender, EventArgs e)
    {
        SyncTabPositions();
        SyncGenerateInputBarState();
        StartAuthorCreditShimmer();
    }

    private void SyncGenerateInputBarState()
    {
        var shouldShow = _displayedTab is AppTab.Generate;
        _generateInputBarVisible = !shouldShow;

        if (shouldShow)
        {
            _ = AnimateGenerateInputBarAsync(true);
        }
        else
        {
            GenerateInputBar.IsVisible = false;
            GenerateInputBar.InputTransparent = true;
            GenerateInputBar.Opacity = 0;
            GenerateInputBar.TranslationY = 0;
        }

        UpdateInputBarButtonStates(_viewModel.Generate.InputText.IsNotBlank());
    }

    private void OnUnloaded(object? sender, EventArgs e)
    {
        _isUnloaded = true;
        _authorShimmerRunning = false;
        AuthorCreditLabel.StopAnimations();
        AuthorCreditLabel.Shadow = CreateAuthorCreditGlow(0);
        _themeService.ThemeChanged -= OnThemeChanged;
        ContentHost.SizeChanged -= OnContentHostSizeChanged;
        _viewModel.PropertyChanged -= OnViewModelPropertyChanged;
        _viewModel.Generate.PropertyChanged -= OnGenerateViewModelPropertyChanged;
        ScanPanel.StopAnimations();
        GeneratePanel.StopAnimations();
    }

    private void OnContentHostSizeChanged(object? sender, EventArgs e)
    {
        _referenceExpandedEditorHeight = null;
        UpdateContentHostClip();

        if (!_isTabAnimating && !_isPanning)
        {
            SyncTabPositions();
        }
    }

    private void UpdateContentHostClip()
    {
        if (ContentHost.Width > 0 && ContentHost.Height > 0)
        {
            ContentHost.Clip = new RectangleGeometry(new Rect(0, 0, ContentHost.Width, ContentHost.Height));
        }
    }

    private void OnGenerateViewModelPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.IsProperty(nameof(GenerateViewModel.InputText)))
        {
            UpdateInputBarButtonStates(_viewModel.Generate.InputText.IsNotBlank());
        }

        if (e.IsProperty(nameof(GenerateViewModel.HasQrCode)) && _viewModel.Generate.HasQrCode)
        {
            CollapseGenerateInputEditor();
        }
    }

    private void UpdateInputBarButtonStates(bool hasText)
    {
        var activeBrush = (Brush)Application.Current!.Resources["AccentGradientBrush"];

        if (hasText)
        {
            ImageGenButton.Background = activeBrush;
            ImageGenButton.BackgroundColor = Colors.Transparent;
            WandButton.Background = activeBrush;
            WandButton.BackgroundColor = Colors.Transparent;
            ImageGenIcon.IconColor = _activeIconColor;
            WandIcon.IconColor = _activeIconColor;
        }
        else
        {
            ImageGenButton.Background = null;
            ImageGenButton.BackgroundColor = _inactiveButtonBackground;
            WandButton.Background = null;
            WandButton.BackgroundColor = _inactiveButtonBackground;
            ImageGenIcon.IconColor = _inactiveIconColor;
            WandIcon.IconColor = _inactiveIconColor;
        }
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
        _ = ExpandGenerateInputEditorAsync();
    }

    private async Task ExpandGenerateInputEditorAsync()
    {
        if (_isUnloaded)
        {
            return;
        }

        if (_viewModel.Generate.HasQrCode
            && GenerateContent.FindByName<ScrollView>("GenerateScrollView") is { } scrollView
            && GenerateContent.FindByName<Border>("QrCard") is { } qrCard)
        {
            try
            {
                await scrollView.ScrollToAsync(qrCard, ScrollToPosition.MakeVisible, animated: false);
            }
            catch (Exception ex) when (ViewLifecycleExtensions.IsShutdownException(ex))
            {
                return;
            }
        }

        for (var attempt = 0; attempt < 4; attempt++)
        {
            if (_isUnloaded || !GenerateInputEditor.IsFocused)
            {
                return;
            }

            ExpandGenerateInputEditor();

            if (GenerateInputEditor.HeightRequest > CollapsedEditorHeight + 8)
            {
                return;
            }

            await Task.Delay(attempt switch
            {
                0 => 16,
                1 => 32,
                _ => 48
            });
        }
    }

    private void OnGenerateInputEditorUnfocused(object? sender, FocusEventArgs e)
    {
        UpdateInputEditorSeparatorState(isFocused: false);
        GenerateInputEditor.HeightRequest = CollapsedEditorHeight;
    }

    private void CollapseGenerateInputEditor()
    {
        UpdateInputEditorSeparatorState(isFocused: false);
        GenerateInputEditor.HeightRequest = CollapsedEditorHeight;

        if (GenerateInputEditor.IsFocused)
        {
            GenerateInputEditor.Unfocus();
        }
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
        if (ContentHost.Height <= 0)
        {
            return CollapsedEditorHeight;
        }

        if (!_viewModel.Generate.HasQrCode && _referenceExpandedEditorHeight is { } cachedHeight)
        {
            return cachedHeight;
        }

        var freeSpace = _viewModel.Generate.HasQrCode
            ? GenerateContent.GetFreeSpaceBelowQrCard(
                ContentHost.Height,
                ContentHost.Padding,
                ContentHost.Width)
            : GenerateContent.GetFreeSpaceBelowEmptyState(
                ContentHost.Height,
                ContentHost.Padding);

        if (freeSpace < 0)
        {
            return CollapsedEditorHeight;
        }

        var maxHeight = CollapsedEditorHeight + Math.Max(0, freeSpace - ExpansionAnchorGap);

        if (!_viewModel.Generate.HasQrCode)
        {
            _referenceExpandedEditorHeight = maxHeight;
        }

        return maxHeight;
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
        if (_isUnloaded)
        {
            return;
        }

        if (sender is MainViewModel vm && e.IsProperty(nameof(MainViewModel.SelectedTab)))
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                UpdateTabVisuals(vm.SelectedTab);

                if (!_isTabAnimating && !_isPanning && vm.SelectedTab != _displayedTab)
                {
                    _ = AnimateTabChangeAsync(vm.SelectedTab);
                }
            });

            return;
        }

        if (sender is MainViewModel && e.IsProperty(nameof(MainViewModel.IsHistoryVisible)))
        {
            MainThread.BeginInvokeOnMainThread(() =>
                _ = SetHistoryOverlayVisibleAsync(_viewModel.IsHistoryVisible));
            return;
        }

        if (sender is MainViewModel && e.IsProperty(nameof(MainViewModel.IsThemesVisible)))
        {
            MainThread.BeginInvokeOnMainThread(() =>
                _ = SetThemesOverlayVisibleAsync(_viewModel.IsThemesVisible));
        }
    }

    private async void OnScanTabTapped(object? sender, TappedEventArgs e)
    {
        if (_displayedTab is AppTab.Scan)
        {
            return;
        }

        await AnimateTabChangeAsync(AppTab.Scan);
    }

    private async void OnGenerateTabTapped(object? sender, TappedEventArgs e)
    {
        if (_displayedTab is AppTab.Generate)
        {
            return;
        }

        await AnimateTabChangeAsync(AppTab.Generate);
    }

    private async Task AnimateTabChangeAsync(AppTab newTab)
    {
        if (_isUnloaded || _isTabAnimating || newTab == _displayedTab)
        {
            return;
        }

        _isTabAnimating = true;
        var width = ContentHost.Width;

        if (width <= 0)
        {
            _viewModel.SelectedTab = newTab;
            _displayedTab = newTab;
            SyncTabPositions();
            await AnimateGenerateInputBarAsync(newTab is AppTab.Generate);
            _isTabAnimating = false;
            return;
        }

        SyncTabPositions();
        UpdateTabVisuals(newTab);

        var targetScanX = newTab is AppTab.Scan ? 0 : -width;
        var targetGenerateX = newTab is AppTab.Scan ? width : 0;

        _viewModel.SelectedTab = newTab;
        var inputBarTask = AnimateGenerateInputBarAsync(newTab is AppTab.Generate);

        try
        {
            await Task.WhenAll(
                ScanPanel.TranslateToAsync(targetScanX, 0, ViewAnimationExtensions.TabDuration, ViewAnimationExtensions.StandardEase),
                GeneratePanel.TranslateToAsync(targetGenerateX, 0, ViewAnimationExtensions.TabDuration, ViewAnimationExtensions.StandardEase),
                inputBarTask);
        }
        catch (Exception ex) when (ViewLifecycleExtensions.IsShutdownException(ex))
        {
            _isTabAnimating = false;
            return;
        }

        if (_isUnloaded)
        {
            _isTabAnimating = false;
            return;
        }

        _displayedTab = newTab;
        _isTabAnimating = false;
    }

    private void SyncTabPositions(double panOffset = 0)
    {
        var width = ContentHost.Width;
        if (width <= 0)
        {
            ScanPanel.IsVisible = _displayedTab is AppTab.Scan;
            GeneratePanel.IsVisible = _displayedTab is AppTab.Generate;
            ScanPanel.TranslationX = 0;
            GeneratePanel.TranslationX = 0;
            ScanPanel.Opacity = 1;
            GeneratePanel.Opacity = 1;
            return;
        }

        switch (_displayedTab)
        {
            case AppTab.Scan:
                ScanPanel.TranslationX = panOffset;
                GeneratePanel.TranslationX = width + panOffset;
                break;
            case AppTab.Generate:
                ScanPanel.TranslationX = -width + panOffset;
                GeneratePanel.TranslationX = panOffset;
                break;
        }

        ScanPanel.Opacity = 1;
        GeneratePanel.Opacity = 1;
        ScanPanel.IsVisible = true;
        GeneratePanel.IsVisible = true;
    }

    private void OnContentPanUpdated(object? sender, PanUpdatedEventArgs e)
    {
        if (_isUnloaded || _isTabAnimating || _viewModel.IsHistoryVisible || _viewModel.IsThemesVisible)
        {
            return;
        }

        var width = ContentHost.Width;
        if (width <= 0)
        {
            return;
        }

        switch (e.StatusType)
        {
            case GestureStatus.Started:
                _isPanning = true;
                _swipeIsHorizontal = false;
                GenerateInputEditor.Unfocus();
                break;

            case GestureStatus.Running:
            {
                if (Math.Abs(e.TotalX) > 10 && Math.Abs(e.TotalX) > Math.Abs(e.TotalY) * 1.15)
                {
                    _swipeIsHorizontal = true;
                }

                if (!_swipeIsHorizontal)
                {
                    return;
                }

                var delta = e.TotalX;

                if (_displayedTab is AppTab.Scan && delta > 0)
                {
                    delta *= 0.2;
                }
                else if (_displayedTab is AppTab.Generate && delta < 0)
                {
                    delta *= 0.2;
                }

                var maxDrag = width * 0.92;
                delta = Math.Clamp(delta, -maxDrag, maxDrag);
                SyncTabPositions(delta);
                break;
            }

            case GestureStatus.Canceled:
                _isPanning = false;
                _swipeIsHorizontal = false;
                _ = SnapTabPositionAsync();
                break;

            case GestureStatus.Completed:
            {
                _isPanning = false;

                if (!_swipeIsHorizontal)
                {
                    _swipeIsHorizontal = false;
                    return;
                }

                _swipeIsHorizontal = false;
                var threshold = Math.Max(56, width * 0.18);
                var targetTab = _displayedTab;

                if (_displayedTab is AppTab.Scan && e.TotalX <= -threshold)
                {
                    targetTab = AppTab.Generate;
                }
                else if (_displayedTab is AppTab.Generate && e.TotalX >= threshold)
                {
                    targetTab = AppTab.Scan;
                }

                if (targetTab != _displayedTab)
                {
                    _ = AnimateTabChangeAsync(targetTab);
                }
                else
                {
                    _ = SnapTabPositionAsync();
                }

                break;
            }
        }
    }

    private async Task SnapTabPositionAsync()
    {
        if (_isUnloaded || _isTabAnimating)
        {
            return;
        }

        var width = ContentHost.Width;
        if (width <= 0)
        {
            SyncTabPositions();
            return;
        }

        var targetScanX = _displayedTab is AppTab.Scan ? 0 : -width;
        var targetGenerateX = _displayedTab is AppTab.Scan ? width : 0;

        try
        {
            await Task.WhenAll(
                ScanPanel.TranslateToAsync(targetScanX, 0, ViewAnimationExtensions.TabDuration, ViewAnimationExtensions.StandardEase),
                GeneratePanel.TranslateToAsync(targetGenerateX, 0, ViewAnimationExtensions.TabDuration, ViewAnimationExtensions.StandardEase));
        }
        catch (Exception ex) when (ViewLifecycleExtensions.IsShutdownException(ex))
        {
        }
    }

    private async Task AnimateGenerateInputBarAsync(bool show)
    {
        if (_isUnloaded || show == _generateInputBarVisible)
        {
            return;
        }

        _generateInputBarVisible = show;

        if (show)
        {
            GenerateInputBar.IsVisible = true;
            GenerateInputBar.InputTransparent = false;
            GenerateInputBar.Opacity = 0;
            GenerateInputBar.TranslationY = 18;

            try
            {
                await GenerateInputBar.FadeSlideToAsync(1, 0, ViewAnimationExtensions.TabDuration, ViewAnimationExtensions.EnterEase);
            }
            catch (Exception ex) when (ViewLifecycleExtensions.IsShutdownException(ex))
            {
            }
            finally
            {
                if (!_isUnloaded && _generateInputBarVisible)
                {
                    GenerateInputBar.Opacity = 1;
                    GenerateInputBar.TranslationY = 0;
                    GenerateInputBar.IsVisible = true;
                }
            }

            UpdateInputBarButtonStates(_viewModel.Generate.InputText.IsNotBlank());
            return;
        }

        try
        {
            await GenerateInputBar.FadeSlideToAsync(0, 18, ViewAnimationExtensions.TabDuration - 40, ViewAnimationExtensions.ExitEase);
        }
        catch (Exception ex) when (ViewLifecycleExtensions.IsShutdownException(ex))
        {
        }

        if (!_isUnloaded && !_generateInputBarVisible)
        {
            GenerateInputBar.IsVisible = false;
            GenerateInputBar.InputTransparent = true;
        }
    }

    private async Task SetHistoryOverlayVisibleAsync(bool visible)
    {
        if (_isUnloaded)
        {
            return;
        }

        if (visible)
        {
            if (_historyOverlayUiVisible && HistoryOverlay.IsVisible && HistoryOverlayPanel.Opacity > 0.95)
            {
                return;
            }

            _historyOverlayUiVisible = true;
            HistoryOverlay.IsVisible = true;
            HistoryOverlay.InputTransparent = false;
            HistoryOverlayScrim.Opacity = 0;
            HistoryOverlayPanel.Opacity = 0;
            HistoryOverlayPanel.TranslationY = 28;

            try
            {
                await Task.WhenAll(
                    HistoryOverlayScrim.FadeToAsync(1, ViewAnimationExtensions.OverlayDuration, ViewAnimationExtensions.EnterEase),
                    HistoryOverlayPanel.FadeSlideToAsync(1, 0, ViewAnimationExtensions.OverlayDuration, ViewAnimationExtensions.EnterEase));
            }
            catch (Exception ex) when (ViewLifecycleExtensions.IsShutdownException(ex))
            {
                return;
            }

            if (!_isUnloaded && _viewModel.IsHistoryVisible)
            {
                GlassEffect.RefreshVisualTree(HistoryOverlayPanel);
            }

            return;
        }

        if (!_historyOverlayUiVisible)
        {
            return;
        }

        try
        {
            await Task.WhenAll(
                HistoryOverlayScrim.FadeToAsync(0, ViewAnimationExtensions.OverlayDuration - 60, ViewAnimationExtensions.ExitEase),
                HistoryOverlayPanel.FadeSlideToAsync(0, 24, ViewAnimationExtensions.OverlayDuration - 60, ViewAnimationExtensions.ExitEase));
        }
        catch (Exception ex) when (ViewLifecycleExtensions.IsShutdownException(ex))
        {
        }

        if (!_isUnloaded && !_viewModel.IsHistoryVisible)
        {
            HistoryOverlay.IsVisible = false;
            HistoryOverlay.InputTransparent = true;
            HistoryOverlayPanel.Opacity = 0;
            _historyOverlayUiVisible = false;
        }
    }

    private async Task SetThemesOverlayVisibleAsync(bool visible)
    {
        if (_isUnloaded)
        {
            return;
        }

        if (visible)
        {
            if (_themesOverlayUiVisible && ThemesOverlay.IsVisible && ThemesOverlayPanel.Opacity > 0.95)
            {
                return;
            }

            _themesOverlayUiVisible = true;
            ThemesOverlay.IsVisible = true;
            ThemesOverlay.InputTransparent = false;
            ThemesOverlayScrim.Opacity = 0;
            ThemesOverlayPanel.Opacity = 0;
            ThemesOverlayPanel.TranslationY = 28;

            try
            {
                await Task.WhenAll(
                    ThemesOverlayScrim.FadeToAsync(1, ViewAnimationExtensions.OverlayDuration, ViewAnimationExtensions.EnterEase),
                    ThemesOverlayPanel.FadeSlideToAsync(1, 0, ViewAnimationExtensions.OverlayDuration, ViewAnimationExtensions.EnterEase));
            }
            catch (Exception ex) when (ViewLifecycleExtensions.IsShutdownException(ex))
            {
                return;
            }

            if (!_isUnloaded && _viewModel.IsThemesVisible)
            {
                GlassEffect.RefreshVisualTree(ThemesOverlayPanel);
            }

            return;
        }

        if (!_themesOverlayUiVisible)
        {
            return;
        }

        try
        {
            await Task.WhenAll(
                ThemesOverlayScrim.FadeToAsync(0, ViewAnimationExtensions.OverlayDuration - 60, ViewAnimationExtensions.ExitEase),
                ThemesOverlayPanel.FadeSlideToAsync(0, 24, ViewAnimationExtensions.OverlayDuration - 60, ViewAnimationExtensions.ExitEase));
        }
        catch (Exception ex) when (ViewLifecycleExtensions.IsShutdownException(ex))
        {
        }

        if (!_isUnloaded && !_viewModel.IsThemesVisible)
        {
            ThemesOverlay.IsVisible = false;
            ThemesOverlay.InputTransparent = true;
            ThemesOverlayPanel.Opacity = 0;
            _themesOverlayUiVisible = false;
        }
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

    private async void StartAuthorCreditShimmer()
    {
        if (_authorShimmerRunning || AuthorCreditLabel is null)
        {
            return;
        }

        _authorShimmerRunning = true;

        while (_authorShimmerRunning && AuthorCreditLabel is not null)
        {
            var baseColor = (Color)Application.Current!.Resources["SecondaryText"];

            try
            {
                await AnimateAuthorCreditVisualAsync(baseColor, Colors.White, 0, 1, 1200);
                if (!_authorShimmerRunning)
                {
                    break;
                }

                await AnimateAuthorCreditVisualAsync(Colors.White, baseColor, 1, 0, 1200);
            }
            catch (Exception ex) when (ViewLifecycleExtensions.IsShutdownException(ex))
            {
                break;
            }
        }
    }

    private Task AnimateAuthorCreditVisualAsync(
        Color from,
        Color to,
        double glowFrom,
        double glowTo,
        uint duration)
    {
        if (AuthorCreditLabel is null)
        {
            return Task.CompletedTask;
        }

        var completion = new TaskCompletionSource();

        var animation = new Animation(progress =>
        {
            AuthorCreditLabel.TextColor = InterpolateColor(from, to, progress);
            var glowStrength = glowFrom + ((glowTo - glowFrom) * progress);
            AuthorCreditLabel.Shadow = CreateAuthorCreditGlow(glowStrength);
        });

        animation.Commit(
            AuthorCreditLabel,
            AuthorShimmerAnimationName,
            16,
            duration,
            Easing.SinInOut,
            (_, _) => completion.TrySetResult());

        return completion.Task;
    }

    private static Shadow CreateAuthorCreditGlow(double strength)
    {
        strength = Math.Clamp(strength, 0, 1);

        return new Shadow
        {
            Brush = Colors.White,
            Radius = (float)(1 + (strength * 14)),
            Offset = new Point(0, 0),
            Opacity = (float)(strength * 0.9)
        };
    }

    private static Color InterpolateColor(Color from, Color to, double progress) =>
        Color.FromRgba(
            from.Red + ((to.Red - from.Red) * progress),
            from.Green + ((to.Green - from.Green) * progress),
            from.Blue + ((to.Blue - from.Blue) * progress),
            from.Alpha + ((to.Alpha - from.Alpha) * progress));

    private async void OnAuthorCreditTapped(object? sender, EventArgs e)
    {
        try
        {
            await Launcher.Default.OpenAsync(AuthorCreditUrl).ConfigureAwait(false);
        }
        catch (Exception ex) when (ViewLifecycleExtensions.IsShutdownException(ex))
        {
        }
        catch
        {
            // Ignore browser launch failures on unsupported platforms.
        }
    }
}
