using System.Windows.Controls;

namespace PbePro.Services;

/// <summary>
/// Swaps pages inside the main window's Frame. Kept as a simple static helper for now;
/// it can be swapped for an injected service later without touching the views much.
/// </summary>
public static class Navigator
{
    private static Frame? _frame;

    public static void Initialize(Frame frame) => _frame = frame;

    public static void Navigate(Page page)
    {
        if (_frame is null)
            throw new InvalidOperationException("Navigator has not been initialized.");

        _frame.Navigate(page);

        // Keep the back stack empty; every screen has its own explicit Back/Home button.
        while (_frame.CanGoBack)
            _frame.RemoveBackEntry();
    }

    public static void GoHome() => Navigate(new Views.StartPage());
}
