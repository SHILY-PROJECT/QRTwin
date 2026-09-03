using QRTwin.Models;
using QRTwin.Services;
using QRTwin.Themes;

namespace QRTwin.ViewModels;

public partial class ThemeOptionViewModel : ObservableObject
{
    public ThemeOptionViewModel(AppThemeDescriptor descriptor, bool isSelected)
    {
        Id = descriptor.Id;
        Name = descriptor.DisplayName;
        Description = descriptor.Description;
        PreviewBrush = descriptor.CreatePalette().PreviewBrush;
        IsSelected = isSelected;
    }

    public AppThemeId Id { get; }

    public string Name { get; }

    public string Description { get; }

    public Brush PreviewBrush { get; }

    [ObservableProperty]
    public partial bool IsSelected { get; set; }
}

public partial class ThemesViewModel(IThemeService themeService) : ObservableObject
{
    public ObservableCollection<ThemeOptionViewModel> Themes { get; } = [];

    public void Refresh()
    {
        Themes.Clear();
        foreach (var descriptor in AppThemeCatalog.All)
        {
            Themes.Add(new ThemeOptionViewModel(descriptor, descriptor.Id == themeService.CurrentThemeId));
        }
    }

    [RelayCommand]
    private void SelectTheme(ThemeOptionViewModel option)
    {
        themeService.SetTheme(option.Id);
        foreach (var theme in Themes)
        {
            theme.IsSelected = theme.Id == option.Id;
        }
    }
}
