using System.Windows;
using System.Windows.Input;
using PbePro.Services;
using PbePro.Views;
using Wpf.Ui.Controls;

namespace PbePro;

public partial class MainWindow : FluentWindow
{
    public MainWindow()
    {
        InitializeComponent();

        // Registered first, so F11 and the full-screen Esc are handled before any page sees the key.
        PreviewKeyDown += OnPreviewKeyDown;

        // Starts on the user's Windows theme unless they've chosen Light or Dark in Settings.
        SettingsService.Load();
        ThemeService.Initialize(this);
        FullScreenService.Initialize(this);

        Navigator.Initialize(RootFrame);
        Navigator.Navigate(new StartPage());

        Loaded += OnLoaded;
    }

    /// <summary>Called by the full-screen service: the title bar is hidden while in full screen.</summary>
    public void SetTitleBarVisible(bool visible) =>
        AppTitleBar.Visibility = visible ? Visibility.Visible : Visibility.Collapsed;

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        Loaded -= OnLoaded;

        if (SettingsService.Current.StartFullScreen)
            FullScreenService.Enter();
    }

    private void OnPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.IsRepeat)
            return;

        if (e.Key == Key.F11)
        {
            FullScreenService.Toggle();
            e.Handled = true;
        }
        else if (e.Key == Key.Escape && FullScreenService.IsFullScreen)
        {
            // The first Esc only leaves full screen. Pages see Esc only when we're not in it.
            FullScreenService.Exit();
            e.Handled = true;
        }
    }
}
