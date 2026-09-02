using QRTwin.Models;
using QRTwin.Themes;

namespace QRTwin.Services;

public sealed class ThemeService : IThemeService
{
    public event EventHandler? ThemeChanged;

    public AppThemeId CurrentThemeId { get; private set; } = AppThemeCatalog.DefaultThemeId;

    public IReadOnlyList<AppThemeId> AvailableThemeIds { get; } =
        AppThemeCatalog.All.Select(theme => theme.Id).ToArray();

    public void Initialize()
    {
        var savedTheme = Preferences.Get(AppThemeCatalog.PreferenceKey, AppThemeCatalog.DefaultThemeId.ToString());
        var themeId = Enum.TryParse<AppThemeId>(savedTheme, out var parsed)
            ? parsed
            : AppThemeCatalog.DefaultThemeId;

        if (!AvailableThemeIds.Contains(themeId))
        {
            themeId = AppThemeCatalog.DefaultThemeId;
        }

        ApplyTheme(themeId, persist: false);
    }

    public void SetTheme(AppThemeId themeId)
    {
        if (!AvailableThemeIds.Contains(themeId) || themeId == CurrentThemeId)
        {
            return;
        }

        ApplyTheme(themeId, persist: true);
    }

    private void ApplyTheme(AppThemeId themeId, bool persist)
    {
        CurrentThemeId = themeId;

        if (persist)
        {
            Preferences.Set(AppThemeCatalog.PreferenceKey, themeId.ToString());
        }

        var resources = Application.Current?.Resources;
        if (resources is null)
        {
            return;
        }

        AppThemeCatalog.GetPalette(themeId).ApplyTo(resources);
        ThemeChanged?.Invoke(this, EventArgs.Empty);
    }
}
