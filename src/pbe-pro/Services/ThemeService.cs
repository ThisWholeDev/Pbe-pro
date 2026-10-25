using System.Windows;
using PbePro.Models;
using Wpf.Ui.Appearance;

namespace PbePro.Services;

public static class ThemeService
{
    private static Window? _window;
    private static bool _watching;

    public static void Initialize(Window window)
    {
        _window = window;
        Apply(SettingsService.Current.Theme);
    }

    public static void Apply(ThemePreference preference)
    {
        if (_window is null)
            return;

        // Only stop following Windows if we started following it earlier.
        if (_watching)
        {
            SystemThemeWatcher.UnWatch(_window);
            _watching = false;
        }

        switch (preference)
        {
            case ThemePreference.Light:
                ApplicationThemeManager.Apply(ApplicationTheme.Light);
                break;

            case ThemePreference.Dark:
                ApplicationThemeManager.Apply(ApplicationTheme.Dark);
                break;

            default:
                ApplicationThemeManager.ApplySystemTheme();
                SystemThemeWatcher.Watch(_window);
                _watching = true;
                break;
        }
    }
}
