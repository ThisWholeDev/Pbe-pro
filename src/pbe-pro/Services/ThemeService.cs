using System.Windows;
using PbePro.Models;
using Wpf.Ui.Appearance;

namespace PbePro.Services;

public static class ThemeService
{
    private static Window? _window;
    private static Window? _watchedWindow;

    public static void Initialize(Window window)
    {
        _window = window;

        // Applying the theme before the window has loaded can't hook system theme
        // changes yet, so wait until it has.
        if (window.IsLoaded)
            Apply(SettingsService.Current.Theme);
        else
            window.Loaded += (_, _) => Apply(SettingsService.Current.Theme);
    }

    public static void Apply(ThemePreference preference)
    {
        if (_window is null)
            return;

        // Stop following Windows, but only on the window we actually started watching.
        if (_watchedWindow is not null)
        {
            if (_watchedWindow.IsLoaded)
                SystemThemeWatcher.UnWatch(_watchedWindow);
            _watchedWindow = null;
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
                _watchedWindow = _window;
                break;
        }
    }
}
