using System.Windows.Controls;
using PbePro.ViewModels;

namespace PbePro.Views;

public partial class StartPage : Page
{
    private readonly StartViewModel _viewModel = new();

    public StartPage()
    {
        InitializeComponent();
        DataContext = _viewModel;
        Loaded += async (_, _) => await _viewModel.InitializeAsync();
    }
}
