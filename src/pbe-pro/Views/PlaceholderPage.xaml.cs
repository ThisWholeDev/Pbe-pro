using System.Windows;
using System.Windows.Controls;
using PbePro.Services;

namespace PbePro.Views;

/// <summary>Temporary stand-in for screens that are built in later steps.</summary>
public partial class PlaceholderPage : Page
{
    public PlaceholderPage(string title, string message)
    {
        InitializeComponent();
        TitleText.Text = title;
        MessageText.Text = message;
    }

    private void OnHomeClick(object sender, RoutedEventArgs e) => Navigator.GoHome();
}
