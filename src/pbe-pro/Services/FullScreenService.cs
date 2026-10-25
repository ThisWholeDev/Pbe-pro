using System.Windows;

namespace PbePro.Services;

/// <summary>Switches the main window in and out of full screen, remembering how it was before.</summary>
public static class FullScreenService
{
    private static MainWindow? _window;
    private static WindowState _previousState;
    private static WindowStyle _previousStyle;
    private static ResizeMode _previousResizeMode;

    public static bool IsFullScreen { get; private set; }

    public static void Initialize(MainWindow window) => _window = window;

    public static void Toggle()
    {
        if (IsFullScreen)
            Exit();
        else
            Enter();
    }

    public static void Enter()
    {
        if (_window is null || IsFullScreen)
            return;

        _previousState = _window.WindowState;
        _previousStyle = _window.WindowStyle;
        _previousResizeMode = _window.ResizeMode;

        _window.SetTitleBarVisible(false);

        // Passing through Normal makes Windows recalculate the maximised bounds, so the taskbar is covered.
        _window.WindowState = WindowState.Normal;
        _window.WindowStyle = WindowStyle.None;
        _window.ResizeMode = ResizeMode.NoResize;
        _window.WindowState = WindowState.Maximized;

        IsFullScreen = true;
    }

    public static void Exit()
    {
        if (_window is null || !IsFullScreen)
            return;

        _window.WindowState = WindowState.Normal;
        _window.WindowStyle = _previousStyle;
        _window.ResizeMode = _previousResizeMode;
        _window.SetTitleBarVisible(true);
        _window.WindowState = _previousState;

        IsFullScreen = false;
    }
}
