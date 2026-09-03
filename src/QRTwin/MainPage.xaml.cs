using Microsoft.Maui.Controls.Shapes;
using QRTwin.Controls;
using QRTwin.Effects;
using QRTwin.Extensions;
using QRTwin.Models;
using QRTwin.Services;
using QRTwin.Themes;
using QRTwin.ViewModels;

namespace QRTwin;

public partial class MainPage : ContentPage
{
    private const double CollapsedEditorHeight = 44;
    private const double ExpansionAnchorGap = 12;
    private const string AuthorShimmerAnimationName = "AuthorShimmer";
    private const string EditorHeightAnimationName = "GenerateInputEditorHeight";
    private const string InputBarAnimationName = "GenerateInputBar";
    private const string InputButtonGlowAnimationName = "InputButtonGlow";
    private const string DuplicateToastAnimationName = "DuplicateQrToast";
    private const double DefaultInputBarInset = 128;
    private static readonly Uri AuthorCreditUrl = new("https://github.com/SHILY-PROJECT");

    private readonly MainViewModel _viewModel;
    private readonly IThemeService _themeService;
    private Color _inactiveIconColor = null!;
    private readonly Color _activeIconColor = Colors.White;
    private Color _separatorInactiveColor = null!;
    private Color _separatorActiveColor = null!;
    private bool _isUnloaded;
    private bool _authorShimmerRunning;
    private AppTab _displayedTab;
    private bool _isTabAnimating;
    private bool _isPanning;
    private bool _historyOverlayUiVisible;
    private bool _themesOverlayUiVisible;
    private int _inputBarAnimationGeneration;
    private int _inputButtonAnimationGeneration;
    private int _editorExpandGeneration;
    private int _duplicateToastGeneration;
    private bool _inputButtonsAreActive;
    private bool _inputEditorIsFocused;
    private bool _swipeIsHorizontal;
    private double _panTotalX;

    public MainPage(MainViewModel viewModel, IThemeService themeService)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
        _themeService = themeService;

        RefreshThemeColors();
        _themeService.ThemeChanged += OnThemeChanged;

        viewModel.PropertyChanged += OnViewModelPropertyChanged;
        viewModel.Generate.PropertyChanged += OnGenerateViewModelPropertyChanged;
        viewModel.Generate.DuplicateGenerateNotified += OnDuplicateGenerateNotified;
        Unloaded += OnUnloaded;
        _displayedTab = viewModel.SelectedTab;
        UpdateTabVisuals(viewModel.SelectedTab);
        UpdateHeaderAndOverlayChrome();
        SyncTabPositions();
        UpdateInputBarButtonStates(_viewModel.Generate.InputText.IsNotBlank(), animate: false);
        UpdateGenerateInputBarActiveChrome(isFocused: false);
        ContentHost.SizeChanged += OnContentHostSizeChanged;
        UpdateContentHostClip();

        var contentPan = new PanGestureRecognizer();
        contentPan.PanUpdated += OnContentPanUpdated;
        ContentAreaGrid.GestureRecognizers.Add(contentPan);

        GenerateInputBar.SizeChanged += OnGenerateInputBarSizeChanged;

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
            UpdateHeaderAndOverlayChrome();
            UpdateInputBarButtonStates(_viewModel.Generate.InputText.IsNotBlank(), animate: false);
            UpdateGenerateInputBarActiveChrome(GenerateInputEditor.IsFocused);

            if (_historyOverlayUiVisible)
            {
                ApplyGlassPanelShadow(HistoryOverlayPanel);
            }

            if (_themesOverlayUiVisible)
            {
                ApplyGlassPanelShadow(ThemesOverlayPanel);
            }
        });
    }

    private void RefreshThemeColors()
    {
        _inactiveIconColor = (Color)Application.Current.Resources["MutedText"];
        _separatorInactiveColor = (Color)Application.Current.Resources["BorderLight"];
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
        if (_displayedTab is AppTab.Generate)
        {
            _ = SetGenerateInputBarVisibleAsync(true);
            return;
        }

        if (GenerateInputBar.IsVisible)
        {
            _ = SetGenerateInputBarVisibleAsync(false);
            return;
        }

        HideGenerateInputBarImmediate();
    }

    private void HideGenerateInputBarImmediate()
    {
        GenerateInputBar.AbortAnimation(InputBarAnimationName);
        GenerateInputBar.IsVisible = false;
        GenerateInputBar.InputTransparent = true;
        GenerateInputBar.Opacity = 0;
        GenerateInputBar.TranslationY = 0;
        GenerateInputEditor.HeightRequest = CollapsedEditorHeight;
        UpdateGenerateContentInset(false);
        UpdateInputBarButtonStates(_viewModel.Generate.InputText.IsNotBlank(), animate: false);
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
        _viewModel.Generate.DuplicateGenerateNotified -= OnDuplicateGenerateNotified;
        ScanPanel.StopAnimations();
        GeneratePanel.StopAnimations();
        GenerateInputEditor.AbortAnimation(EditorHeightAnimationName);
        GenerateInputBar.AbortAnimation(InputBarAnimationName);
        DuplicateQrToast.AbortAnimation(DuplicateToastAnimationName);
    }

    private void OnGenerateInputBarSizeChanged(object? sender, EventArgs e)
    {
        if (_displayedTab is AppTab.Generate && GenerateInputBar.IsVisible)
        {
            UpdateGenerateContentInset(true);
        }
    }

    private double GetGenerateInputBarInset()
    {
        if (!GenerateInputBar.IsVisible)
        {
            return 0;
        }

        if (GenerateInputBar.Height > 0)
        {
            return GenerateInputBar.Height + GenerateInputBar.Margin.VerticalThickness + 4;
        }

        return DefaultInputBarInset;
    }

    private void UpdateGenerateContentInset(bool reserveSpace)
    {
        var bottomInset = reserveSpace ? GetGenerateInputBarInset() : 0;

        GeneratePanel.Padding = bottomInset > 0
            ? new Thickness(0, 0, 0, bottomInset)
            : Thickness.Zero;
    }

    private void OnContentHostSizeChanged(object? sender, EventArgs e)
    {
        UpdateContentHostClip();

        if (!_isTabAnimating && !_isPanning)
        {
            SyncTabPositions();
        }

        if (_displayedTab is AppTab.Generate
            && GenerateInputBar.IsVisible
            && GenerateInputEditor.HeightRequest > CollapsedEditorHeight + 8
            && (GenerateInputEditor.IsFocused || _viewModel.Generate.InputText.IsNotBlank()))
        {
            // Keep filling free space while editing or while text is still present.
            _ = ExpandGenerateInputEditorAsync(requireFocus: _viewModel.Generate.HasQrCode);
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
            var hasText = _viewModel.Generate.InputText.IsNotBlank();
            UpdateInputBarButtonStates(hasText);

            if (!hasText
                && !GenerateInputEditor.IsFocused
                && _displayedTab is AppTab.Generate
                && GenerateInputBar.IsVisible)
            {
                _ = CollapseGenerateInputEditorAsync();
            }
        }

        if (!e.IsProperty(nameof(GenerateViewModel.HasQrCode)))
        {
            return;
        }

        if (_viewModel.Generate.HasQrCode)
        {
            _ = CollapseGenerateInputEditorAsync();
            return;
        }

        if (_displayedTab is not AppTab.Generate || !GenerateInputBar.IsVisible)
        {
            return;
        }

        // Empty inactive field stays collapsed; expand only when there is text to show.
        if (_viewModel.Generate.InputText.IsNotBlank())
        {
            _ = ExpandGenerateInputEditorAsync(requireFocus: false);
        }
        else if (!GenerateInputEditor.IsFocused)
        {
            _ = CollapseGenerateInputEditorAsync();
        }
    }

    private void UpdateInputBarButtonStates(bool hasText, bool animate = true)
    {
        if (!animate || _isUnloaded)
        {
            ApplyInputBarButtonStatesImmediate(hasText);
            return;
        }

        if (hasText == _inputButtonsAreActive)
        {
            return;
        }

        _ = AnimateInputBarButtonStatesAsync(hasText);
    }

    private void ApplyInputBarButtonStatesImmediate(bool hasText)
    {
        _inputButtonAnimationGeneration++;
        ImageGenButtonGlow.AbortAnimation(InputButtonGlowAnimationName);
        WandButtonGlow.AbortAnimation(InputButtonGlowAnimationName);
        ImageGenButton.AbortAnimation($"{InputButtonGlowAnimationName}_Stroke_{ImageGenButton.GetHashCode()}");
        WandButton.AbortAnimation($"{InputButtonGlowAnimationName}_Stroke_{WandButton.GetHashCode()}");

        _inputButtonsAreActive = hasText;

        if (hasText)
        {
            SetActiveInputButtonFinalState(ImageGenButton, ImageGenButtonGlow, ImageGenIcon);
            SetActiveInputButtonFinalState(WandButton, WandButtonGlow, WandIcon);
            return;
        }

        SetInactiveInputButtonFinalState(ImageGenButton, ImageGenButtonGlow, ImageGenIcon);
        SetInactiveInputButtonFinalState(WandButton, WandButtonGlow, WandIcon);
    }

    private async Task AnimateInputBarButtonStatesAsync(bool hasText)
    {
        if (_isUnloaded)
        {
            return;
        }

        var generation = ++_inputButtonAnimationGeneration;
        var duration = ViewAnimationExtensions.StandardDuration;
        var fromIcon = ImageGenIcon.IconColor;
        var toIcon = hasText ? _activeIconColor : _inactiveIconColor;
        var targetGlow = hasText ? 1.0 : 0.0;
        var easing = hasText ? ViewAnimationExtensions.EnterEase : ViewAnimationExtensions.ExitEase;

        ImageGenButtonGlow.AbortAnimation(InputButtonGlowAnimationName);
        WandButtonGlow.AbortAnimation(InputButtonGlowAnimationName);
        ImageGenButton.AbortAnimation($"{InputButtonGlowAnimationName}_Stroke_{ImageGenButton.GetHashCode()}");
        WandButton.AbortAnimation($"{InputButtonGlowAnimationName}_Stroke_{WandButton.GetHashCode()}");

        try
        {
            await Task.WhenAll(
                ImageGenButtonGlow.FadeToAsync(targetGlow, duration, easing),
                WandButtonGlow.FadeToAsync(targetGlow, duration, easing),
                ImageGenIcon.AnimateIconColorAsync(fromIcon, toIcon, duration, easing),
                WandIcon.AnimateIconColorAsync(fromIcon, toIcon, duration, easing),
                AnimateInputButtonStrokeAsync(ImageGenButton, hasText, duration, easing),
                AnimateInputButtonStrokeAsync(WandButton, hasText, duration, easing));
        }
        catch (Exception ex) when (ViewLifecycleExtensions.IsShutdownException(ex))
        {
            return;
        }

        if (_isUnloaded || generation != _inputButtonAnimationGeneration)
        {
            return;
        }

        ApplyInputBarButtonStatesImmediate(hasText);
    }

    private static Color GetBorderStrokeColor(Border button) =>
        button.Stroke is SolidColorBrush solid ? solid.Color : Colors.Transparent;

    private Task AnimateInputButtonStrokeAsync(Border button, bool active, uint duration, Easing easing)
    {
        var accent = (Color)Application.Current!.Resources["Accent"];
        var fromThickness = button.StrokeThickness;
        // Stroke only when the field is focused and empty; glowing buttons have no outline.
        var showOutline = _inputEditorIsFocused && !active;
        var toThickness = showOutline ? 1.5 : 0.0;
        var fromColor = GetBorderStrokeColor(button);
        var toColor = showOutline ? accent : Colors.Transparent;
        var animationName = $"{InputButtonGlowAnimationName}_Stroke_{button.GetHashCode()}";
        button.AbortAnimation(animationName);

        var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var animation = new Animation(progress =>
        {
            button.Stroke = ViewAnimationExtensions.InterpolateColor(fromColor, toColor, progress);
            button.StrokeThickness = fromThickness + ((toThickness - fromThickness) * progress);
        });

        animation.Commit(
            button,
            animationName,
            length: duration,
            easing: easing,
            finished: (_, _) => tcs.TrySetResult());

        return tcs.Task;
    }

    private void SetActiveInputButtonFinalState(Border button, Border glow, SvgIconView icon)
    {
        button.Background = null;
        button.BackgroundColor = Colors.Transparent;
        ApplyInputButtonStroke(button, isContentActive: true);
        ClearInputButtonChrome(button);
        glow.Background = (Brush)Application.Current!.Resources["AccentGradientBrush"];
        glow.Opacity = 1;
        GlassEffect.SetIntensity(glow, GlassEffectIntensity.Normal);
        icon.IconColor = _activeIconColor;
    }

    private void SetInactiveInputButtonFinalState(Border button, Border glow, SvgIconView icon)
    {
        button.Background = null;
        button.BackgroundColor = Colors.Transparent;
        ApplyInputButtonStroke(button, isContentActive: false);
        ClearInputButtonChrome(button);
        glow.Opacity = 0;
        if (glow.IsSet(GlassEffect.IntensityProperty))
        {
            glow.ClearValue(GlassEffect.IntensityProperty);
        }

        GlassBlur.Clear(glow);
        icon.IconColor = _inactiveIconColor;
    }

    private void ApplyInputButtonStroke(Border button, bool isContentActive)
    {
        // Has text → glow only, no outline.
        // Empty + focused → accent outline, no glow.
        // Empty + unfocused → invisible chrome, icons only.
        if (isContentActive || !_inputEditorIsFocused)
        {
            button.Stroke = Colors.Transparent;
            button.StrokeThickness = 0;
            return;
        }

        button.Stroke = (Color)Application.Current!.Resources["Accent"];
        button.StrokeThickness = 1.5;
    }

    private static void ClearInputButtonChrome(Border button)
    {
        if (button.IsSet(GlassEffect.IntensityProperty))
        {
            button.ClearValue(GlassEffect.IntensityProperty);
        }

        button.ClearValue(VisualElement.ShadowProperty);
        GlassBlur.Clear(button);
    }

    private void UpdateInputEditorSeparatorState(bool isFocused)
    {
        InputEditorSeparator.Color = isFocused ? _separatorActiveColor : _separatorInactiveColor;
    }

    private void UpdateGenerateInputBarActiveChrome(bool isFocused)
    {
        _inputEditorIsFocused = isFocused;
        UpdateInputEditorSeparatorState(isFocused);

        var accent = (Color)Application.Current!.Resources["Accent"];
        var borderLight = (Color)Application.Current.Resources["BorderLight"];

        if (isFocused)
        {
            GenerateInputBar.Stroke = accent;
            GenerateInputBar.StrokeThickness = 1.5;
        }
        else
        {
            GenerateInputBar.Stroke = borderLight;
            GenerateInputBar.StrokeThickness = 1;
        }

        ApplyInputBarButtonStatesImmediate(_inputButtonsAreActive);
    }

    private void OnGenerateInputEditorFocused(object? sender, FocusEventArgs e)
    {
        if (_isUnloaded)
        {
            return;
        }

        UpdateGenerateInputBarActiveChrome(isFocused: true);
        _ = ExpandGenerateInputEditorAsync(requireFocus: true);
    }

    private async Task ExpandGenerateInputEditorAsync(bool requireFocus = true)
    {
        if (_isUnloaded || _displayedTab is not AppTab.Generate)
        {
            return;
        }

        var generation = ++_editorExpandGeneration;

        if (_viewModel.Generate.HasQrCode
            && GenerateContent.FindByName<ScrollView>("GenerateScrollView") is { } scrollView
            && GenerateContent.FindByName<Border>("QrCard") is { } qrCard)
        {
            try
            {
                await scrollView.ScrollToAsync(qrCard, ScrollToPosition.MakeVisible, animated: true);
            }
            catch (Exception ex) when (ViewLifecycleExtensions.IsShutdownException(ex))
            {
                return;
            }
        }

        double targetHeight = CollapsedEditorHeight;
        for (var attempt = 0; attempt < 8; attempt++)
        {
            if (_isUnloaded
                || generation != _editorExpandGeneration
                || (requireFocus && !GenerateInputEditor.IsFocused))
            {
                return;
            }

            if (TryCalculateExpandedEditorMaxHeight(out targetHeight)
                && targetHeight > CollapsedEditorHeight + 8)
            {
                break;
            }

            await Task.Delay(attempt switch
            {
                0 => 16,
                1 => 32,
                2 => 48,
                _ => 64
            });
        }

        if (_isUnloaded
            || generation != _editorExpandGeneration
            || (requireFocus && !GenerateInputEditor.IsFocused))
        {
            return;
        }

        if (targetHeight <= CollapsedEditorHeight + 8)
        {
            return;
        }

        await GenerateInputEditor.AnimateHeightRequestAsync(
            targetHeight,
            ViewAnimationExtensions.EditorExpandDuration,
            ViewAnimationExtensions.StandardEase,
            EditorHeightAnimationName);

        UpdateGenerateContentInset(true);

        // Layout settles after the bar grows — correct residual gap on a second pass.
        await Task.Delay(48);
        if (_isUnloaded
            || generation != _editorExpandGeneration
            || (requireFocus && !GenerateInputEditor.IsFocused))
        {
            return;
        }

        if (TryCalculateExpandedEditorMaxHeight(out var correctedHeight)
            && Math.Abs(correctedHeight - GenerateInputEditor.HeightRequest) > 4)
        {
            await GenerateInputEditor.AnimateHeightRequestAsync(
                correctedHeight,
                ViewAnimationExtensions.StandardDuration,
                ViewAnimationExtensions.StandardEase,
                EditorHeightAnimationName);
            UpdateGenerateContentInset(true);
        }
    }

    private async void OnGenerateInputEditorUnfocused(object? sender, FocusEventArgs e)
    {
        UpdateGenerateInputBarActiveChrome(isFocused: false);

        // Collapse when inactive and empty, or when a QR result owns the space.
        if (_viewModel.Generate.HasQrCode || !_viewModel.Generate.InputText.IsNotBlank())
        {
            await CollapseGenerateInputEditorAsync();
        }
    }

    private async Task CollapseGenerateInputEditorAsync()
    {
        if (_isUnloaded)
        {
            return;
        }

        _editorExpandGeneration++;
        UpdateGenerateInputBarActiveChrome(isFocused: false);

        if (GenerateInputEditor.IsFocused)
        {
            GenerateInputEditor.Unfocus();
        }

        await GenerateInputEditor.AnimateHeightRequestAsync(
            CollapsedEditorHeight,
            ViewAnimationExtensions.EditorExpandDuration,
            ViewAnimationExtensions.StandardEase,
            EditorHeightAnimationName);

        UpdateGenerateContentInset(true);
    }

    private void CollapseGenerateInputEditor()
    {
        _ = CollapseGenerateInputEditorAsync();
    }

    private void OnGenerateInputEditorCompleted(object? sender, EventArgs e)
    {
        // Generation is only via the image button — Completed also fires on some
        // platforms when tapping outside the editor, which must not generate.
        GenerateInputEditor.Unfocus();
    }

    /// <summary>
    /// Grows the editor so the input bar top sits just below the empty-state / QR card.
    /// Uses live layout bounds so repeated expand/collapse cycles stay correct.
    /// </summary>
    private bool TryCalculateExpandedEditorMaxHeight(out double maxHeight)
    {
        maxHeight = CollapsedEditorHeight;

        if (ContentAreaGrid.Height <= 0 || GenerateInputBar.Height <= 0)
        {
            return false;
        }

        // End-aligned bar must have a real Y once laid out.
        if (GenerateInputBar.Y <= 0 && ContentAreaGrid.Height > GenerateInputBar.Height + 24)
        {
            return false;
        }

        double anchorBottom;
        if (_viewModel.Generate.HasQrCode)
        {
            if (!GenerateContent.TryGetQrCardBottomIn(ContentAreaGrid, out anchorBottom))
            {
                return false;
            }
        }
        else if (!GenerateContent.TryGetEmptyStateBottomIn(ContentAreaGrid, out anchorBottom))
        {
            return false;
        }

        var barTop = GenerateInputBar.Y;
        var gapAboveBar = barTop - anchorBottom;
        if (double.IsNaN(gapAboveBar) || double.IsInfinity(gapAboveBar))
        {
            return false;
        }

        var currentEditorHeight = GenerateInputEditor.HeightRequest > 0
            ? GenerateInputEditor.HeightRequest
            : CollapsedEditorHeight;
        if (GenerateInputEditor.Height > 0)
        {
            currentEditorHeight = Math.Max(currentEditorHeight, GenerateInputEditor.Height);
        }

        // Positive gap → grow editor; negative → shrink so the small anchor gap remains.
        maxHeight = Math.Max(CollapsedEditorHeight, currentEditorHeight + gapAboveBar - ExpansionAnchorGap);
        return true;
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

    private void OnDuplicateGenerateNotified(object? sender, EventArgs e)
    {
        if (_isUnloaded)
        {
            return;
        }

        MainThread.BeginInvokeOnMainThread(() => _ = ShowDuplicateQrToastAsync());
    }

    private async Task ShowDuplicateQrToastAsync()
    {
        if (_isUnloaded)
        {
            return;
        }

        var generation = ++_duplicateToastGeneration;
        DuplicateQrToast.AbortAnimation(DuplicateToastAnimationName);
        DuplicateQrToast.IsVisible = true;
        DuplicateQrToast.Opacity = 0;
        DuplicateQrToast.TranslationY = -12;

        try
        {
            await DuplicateQrToast.FadeSlideToAsync(
                1,
                0,
                ViewAnimationExtensions.StandardDuration,
                ViewAnimationExtensions.EnterEase);

            if (_isUnloaded || generation != _duplicateToastGeneration)
            {
                return;
            }

            await Task.Delay(2000);

            if (_isUnloaded || generation != _duplicateToastGeneration)
            {
                return;
            }

            await DuplicateQrToast.FadeSlideToAsync(
                0,
                -12,
                ViewAnimationExtensions.StandardDuration,
                ViewAnimationExtensions.ExitEase);
        }
        catch (Exception ex) when (ViewLifecycleExtensions.IsShutdownException(ex))
        {
            return;
        }

        if (_isUnloaded || generation != _duplicateToastGeneration)
        {
            return;
        }

        DuplicateQrToast.IsVisible = false;
        DuplicateQrToast.Opacity = 0;
        DuplicateQrToast.TranslationY = -12;
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

    private async Task AnimateTabChangeAsync(AppTab newTab, bool fromCurrentPosition = false)
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
            await SetGenerateInputBarVisibleAsync(newTab is AppTab.Generate);
            _isTabAnimating = false;
            return;
        }

        PreparePanelsForTransition();

        if (!fromCurrentPosition)
        {
            SyncTabPositions();
        }

        UpdateTabVisuals(newTab);

        var targetScanX = newTab is AppTab.Scan ? 0 : -width;
        var targetGenerateX = newTab is AppTab.Scan ? width : 0;

        _viewModel.SelectedTab = newTab;
        var inputBarTask = SetGenerateInputBarVisibleAsync(newTab is AppTab.Generate);

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
            SyncTabPositions();
            return;
        }

        if (_isUnloaded)
        {
            _isTabAnimating = false;
            return;
        }

        _displayedTab = newTab;
        UpdateGenerateContentInset(_displayedTab is AppTab.Generate);
        _isTabAnimating = false;
        SyncTabPositions();
    }

    private void PreparePanelsForTransition()
    {
        var width = ContentHost.Width;
        if (width <= 0)
        {
            return;
        }

        // Mount both panels before translating. On Android the camera SurfaceView can keep
        // stealing hits if the inactive panel stays in the tree after a swipe.
        ScanPanel.IsVisible = true;
        GeneratePanel.IsVisible = true;
        ScanPanel.InputTransparent = false;
        GeneratePanel.InputTransparent = false;
        ScanPanel.Opacity = 1;
        GeneratePanel.Opacity = 1;

        if (Math.Abs(ScanPanel.TranslationX) < 0.5 && Math.Abs(GeneratePanel.TranslationX) < 0.5)
        {
            SyncTabPositions();
        }
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
            ScanPanel.InputTransparent = _displayedTab is not AppTab.Scan;
            GeneratePanel.InputTransparent = _displayedTab is not AppTab.Generate;
            return;
        }

        var transitioning = _isPanning || _isTabAnimating || Math.Abs(panOffset) > 0.5;

        if (transitioning)
        {
            ScanPanel.IsVisible = true;
            GeneratePanel.IsVisible = true;
            ScanPanel.InputTransparent = false;
            GeneratePanel.InputTransparent = false;

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
            return;
        }

        // Settled: hide the inactive panel so Android camera/native views cannot intercept input.
        switch (_displayedTab)
        {
            case AppTab.Scan:
                ScanPanel.TranslationX = 0;
                GeneratePanel.TranslationX = width;
                ScanPanel.IsVisible = true;
                GeneratePanel.IsVisible = false;
                ScanPanel.InputTransparent = false;
                GeneratePanel.InputTransparent = true;
                break;
            case AppTab.Generate:
                ScanPanel.TranslationX = -width;
                GeneratePanel.TranslationX = 0;
                ScanPanel.IsVisible = false;
                GeneratePanel.IsVisible = true;
                ScanPanel.InputTransparent = true;
                GeneratePanel.InputTransparent = false;
                break;
        }

        ScanPanel.Opacity = 1;
        GeneratePanel.Opacity = 1;
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
                _panTotalX = 0;
                PreparePanelsForTransition();
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
                _panTotalX = delta;

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
                _panTotalX = delta;
                SyncTabPositions(delta);
                break;
            }

            case GestureStatus.Canceled:
                _isPanning = false;
                _swipeIsHorizontal = false;
                _panTotalX = 0;
                _ = SnapTabPositionAsync();
                break;

            case GestureStatus.Completed:
            {
                _isPanning = false;

                if (!_swipeIsHorizontal)
                {
                    _swipeIsHorizontal = false;
                    _panTotalX = 0;
                    SyncTabPositions();
                    return;
                }

                _swipeIsHorizontal = false;
                var threshold = Math.Max(56, width * 0.18);
                var totalX = _panTotalX;
                _panTotalX = 0;
                var targetTab = _displayedTab;

                if (_displayedTab is AppTab.Scan && totalX <= -threshold)
                {
                    targetTab = AppTab.Generate;
                }
                else if (_displayedTab is AppTab.Generate && totalX >= threshold)
                {
                    targetTab = AppTab.Scan;
                }

                if (targetTab != _displayedTab)
                {
                    _ = AnimateTabChangeAsync(targetTab, fromCurrentPosition: true);
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

        _isTabAnimating = true;
        PreparePanelsForTransition();

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

        _isTabAnimating = false;
        SyncTabPositions();
    }

    private async Task SetGenerateInputBarVisibleAsync(bool show)
    {
        if (_isUnloaded)
        {
            return;
        }

        var generation = ++_inputBarAnimationGeneration;
        GenerateInputBar.AbortAnimation(InputBarAnimationName);

        if (show)
        {
            if (GenerateInputBar.IsVisible && GenerateInputBar.Opacity > 0.95 && GenerateInputBar.TranslationY < 1)
            {
                UpdateGenerateContentInset(true);
                UpdateInputBarButtonStates(_viewModel.Generate.InputText.IsNotBlank(), animate: false);

                if (!_viewModel.Generate.HasQrCode
                    && _viewModel.Generate.InputText.IsNotBlank()
                    && GenerateInputEditor.HeightRequest <= CollapsedEditorHeight + 8)
                {
                    _ = ExpandGenerateInputEditorAsync(requireFocus: false);
                }

                return;
            }

            if (GenerateInputEditor.HeightRequest > CollapsedEditorHeight + 8)
            {
                GenerateInputEditor.HeightRequest = CollapsedEditorHeight;
            }

            GenerateInputBar.IsVisible = true;
            GenerateInputBar.InputTransparent = false;
            GenerateInputBar.Opacity = 0;
            GenerateInputBar.TranslationY = 32;
            UpdateGenerateContentInset(true);

            try
            {
                await GenerateInputBar.FadeSlideToAsync(
                    1,
                    0,
                    ViewAnimationExtensions.TabDuration,
                    ViewAnimationExtensions.EnterEase);
            }
            catch (Exception ex) when (ViewLifecycleExtensions.IsShutdownException(ex))
            {
            }

            if (_isUnloaded || generation != _inputBarAnimationGeneration)
            {
                return;
            }

            GenerateInputBar.Opacity = 1;
            GenerateInputBar.TranslationY = 0;
            GenerateInputBar.IsVisible = true;
            GenerateInputBar.InputTransparent = false;
            UpdateGenerateContentInset(true);
            UpdateInputBarButtonStates(_viewModel.Generate.InputText.IsNotBlank(), animate: false);

            if (!_viewModel.Generate.HasQrCode && _viewModel.Generate.InputText.IsNotBlank())
            {
                _ = ExpandGenerateInputEditorAsync(requireFocus: false);
            }

            return;
        }

        if (!GenerateInputBar.IsVisible && GenerateInputBar.Opacity < 0.05)
        {
            HideGenerateInputBarImmediate();
            return;
        }

        GenerateInputEditor.Unfocus();

        try
        {
            await GenerateInputBar.FadeSlideToAsync(
                0,
                32,
                ViewAnimationExtensions.TabDuration,
                ViewAnimationExtensions.ExitEase);
        }
        catch (Exception ex) when (ViewLifecycleExtensions.IsShutdownException(ex))
        {
        }

        if (_isUnloaded || generation != _inputBarAnimationGeneration)
        {
            return;
        }

        HideGenerateInputBarImmediate();
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
                ApplyGlassPanelShadow(HistoryOverlayPanel);
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
                ApplyGlassPanelShadow(ThemesOverlayPanel);
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
        var tabActiveBg = (Color)Application.Current!.Resources["TabActiveBackground"];
        var tabActiveText = (Color)Application.Current.Resources["TabActiveText"];
        var tabActiveIcon = (Color)Application.Current.Resources["TabActiveIcon"];
        var tabInactiveText = (Color)Application.Current.Resources["TabInactiveText"];
        var tabInactiveIcon = (Color)Application.Current.Resources["TabInactiveIcon"];
        var tabActiveBorder = Application.Current.Resources.TryGetValue("TabActiveBorder", out var borderValue)
                              && borderValue is Color borderColor
            ? borderColor
            : Colors.Transparent;

        var scanActive = selectedTab is AppTab.Scan;
        var generateActive = selectedTab is AppTab.Generate;
        Brush? tabActiveBrush = Application.Current.Resources.TryGetValue("TabActiveBackgroundBrush", out var brushValue)
                                  && brushValue is Brush brush
            ? brush
            : null;

        ApplyTabBackground(ScanTab, scanActive, tabActiveBrush, tabActiveBg);
        ApplyTabBackground(GenerateTab, generateActive, tabActiveBrush, tabActiveBg);

        ScanTab.Stroke = scanActive ? tabActiveBorder : Colors.Transparent;
        GenerateTab.Stroke = generateActive ? tabActiveBorder : Colors.Transparent;
        ScanTab.StrokeThickness = scanActive && tabActiveBorder != Colors.Transparent ? 1 : 0;
        GenerateTab.StrokeThickness = generateActive && tabActiveBorder != Colors.Transparent ? 1 : 0;

        ScanTabIcon.IconColor = scanActive ? tabActiveIcon : tabInactiveIcon;
        GenerateTabIcon.IconColor = generateActive ? tabActiveIcon : tabInactiveIcon;
        ScanTabLabel.TextColor = scanActive ? tabActiveText : tabInactiveText;
        GenerateTabLabel.TextColor = generateActive ? tabActiveText : tabInactiveText;

        ApplyTabGlassEffect(ScanTab, scanActive);
        ApplyTabGlassEffect(GenerateTab, generateActive);
    }

    private void UpdateHeaderAndOverlayChrome()
    {
        var resources = Application.Current!.Resources;
        var isGlass = IsGlassThemeEnabled();

        HistoryOverlayPanel.Style = (Style)resources[isGlass ? "GlassPanelCard" : "GlassOverlayCard"];
        ThemesOverlayPanel.Style = (Style)resources[isGlass ? "GlassPanelCard" : "GlassOverlayCard"];

        ThemesHeaderButton.Style = (Style)resources[isGlass ? "GlassHeaderIconButton" : "IconButton"];
        HistoryHeaderButton.Style = (Style)resources[isGlass ? "GlassHeaderIconButton" : "AccentIconButton"];

        // Glass header chrome uses frosted fills only; drop any leftover Intensity from other styles.
        if (isGlass)
        {
            if (ThemesHeaderButton.IsSet(GlassEffect.IntensityProperty))
            {
                ThemesHeaderButton.ClearValue(GlassEffect.IntensityProperty);
            }

            if (HistoryHeaderButton.IsSet(GlassEffect.IntensityProperty))
            {
                HistoryHeaderButton.ClearValue(GlassEffect.IntensityProperty);
            }
        }

        var accent = (Color)resources["Accent"];
        ThemesHeaderIcon.IconColor = accent;
        HistoryHeaderIcon.IconColor = isGlass ? accent : Colors.White;

        if (!isGlass)
        {
            ClearGlassPanelChrome(HistoryOverlayPanel, ThemesOverlayPanel, ThemesHeaderButton, HistoryHeaderButton);
            return;
        }

        ApplyGlassPanelShadow(HistoryOverlayPanel);
        ApplyGlassPanelShadow(ThemesOverlayPanel);
    }

    private static void ClearGlassPanelChrome(params Border[] borders)
    {
        foreach (var border in borders)
        {
            border.ClearValue(VisualElement.ShadowProperty);
            if (border.IsSet(GlassEffect.IntensityProperty))
            {
                border.ClearValue(GlassEffect.IntensityProperty);
            }

            GlassBlur.Clear(border);
        }
    }

    private static void ApplyGlassPanelShadow(Border panel)
    {
        if (Application.Current?.Resources.TryGetValue("GlassVisualEffects", out var value) != true
            || value is not GlassVisualEffects effects)
        {
            return;
        }

        panel.Shadow = new Shadow
        {
            Brush = new SolidColorBrush(effects.DropShadowColor),
            Radius = effects.DropShadowRadius,
            Offset = new Point(0, effects.DropShadowOffsetY),
            Opacity = effects.DropShadowOpacity * 0.75f,
        };
    }

    private static bool IsGlassThemeEnabled() =>
        Application.Current?.Resources.TryGetValue("GlassVisualEffects", out var value) == true
        && value is GlassVisualEffects { IsEnabled: true };

    private static void ApplyTabBackground(Border tab, bool active, Brush? activeBrush, Color activeColor)
    {
        if (active && activeBrush is not null)
        {
            tab.Background = activeBrush;
            tab.BackgroundColor = Colors.Transparent;
            return;
        }

        tab.Background = null;
        tab.BackgroundColor = active ? activeColor : Colors.Transparent;
    }

    private void ApplyTabGlassEffect(Border tab, bool active)
    {
        if (Application.Current?.Resources.TryGetValue("GlassVisualEffects", out var effectsValue) != true
            || effectsValue is not GlassVisualEffects { IsEnabled: true })
        {
            return;
        }

        if (active)
        {
            GlassEffect.SetIntensity(tab, GlassEffectIntensity.Subtle);
            return;
        }

        if (tab.IsSet(GlassEffect.IntensityProperty))
        {
            tab.ClearValue(GlassEffect.IntensityProperty);
        }

        tab.ClearValue(VisualElement.ShadowProperty);
        GlassBlur.Clear(tab);
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
