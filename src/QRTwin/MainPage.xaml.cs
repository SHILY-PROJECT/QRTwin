using Microsoft.Maui.Controls.Shapes;
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
    private static readonly Color AuthorAccentColor = Color.FromArgb("#03AFFF");
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
        UpdateTabVisuals(viewModel.SelectedTab);
        UpdateTabPanels(viewModel.SelectedTab);
        UpdateInputBarButtonStates(_viewModel.Generate.InputText.IsNotBlank());
        UpdateInputEditorSeparatorState(isFocused: false);
        ContentHost.SizeChanged += OnContentHostSizeChanged;
        UpdateContentHostClip();
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
        });
    }

    private void RefreshThemeColors()
    {
        _inactiveButtonBackground = (Color)Application.Current!.Resources["SurfaceElevated"];
        _inactiveIconColor = (Color)Application.Current.Resources["MutedText"];
        _separatorInactiveColor = (Color)Application.Current.Resources["Border"];
        _separatorActiveColor = (Color)Application.Current.Resources["Accent"];
    }

    private void OnLoaded(object? sender, EventArgs e) =>
        StartAuthorCreditShimmer();

    private void OnUnloaded(object? sender, EventArgs e)
    {
        _isUnloaded = true;
        _authorShimmerRunning = false;
        AuthorCreditLabel.StopAnimations();
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
                await AnimateAuthorCreditColorAsync(baseColor, AuthorAccentColor, 1200);
                if (!_authorShimmerRunning)
                {
                    break;
                }

                await AnimateAuthorCreditColorAsync(AuthorAccentColor, baseColor, 1200);
            }
            catch (Exception ex) when (ViewLifecycleExtensions.IsShutdownException(ex))
            {
                break;
            }
        }
    }

    private Task AnimateAuthorCreditColorAsync(Color from, Color to, uint duration)
    {
        if (AuthorCreditLabel is null)
        {
            return Task.CompletedTask;
        }

        var completion = new TaskCompletionSource();

        var animation = new Animation(
            progress => AuthorCreditLabel.TextColor = InterpolateColor(from, to, progress),
            0,
            1);

        animation.Commit(
            AuthorCreditLabel,
            AuthorShimmerAnimationName,
            16,
            duration,
            Easing.SinInOut,
            (_, _) => completion.TrySetResult());

        return completion.Task;
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
