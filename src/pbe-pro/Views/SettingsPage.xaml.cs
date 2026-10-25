using System.Windows;
using System.Windows.Controls;
using PbePro.Services;
using PbePro.ViewModels;

namespace PbePro.Views;

public partial class SettingsPage : Page
{
    private readonly SettingsViewModel _viewModel = new();

    public SettingsPage()
    {
        InitializeComponent();
        DataContext = _viewModel;
        Unloaded += (_, _) => _viewModel.Shutdown();
    }

    private void OnHomeClick(object sender, RoutedEventArgs e) => Navigator.GoHome();
}
