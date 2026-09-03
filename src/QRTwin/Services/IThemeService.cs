using QRTwin.Models;

namespace QRTwin.Services;

public interface IThemeService
{
    event EventHandler? ThemeChanged;

    AppThemeId CurrentThemeId { get; }

    IReadOnlyList<AppThemeId> AvailableThemeIds { get; }

    void Initialize();

    void SetTheme(AppThemeId themeId);
}
