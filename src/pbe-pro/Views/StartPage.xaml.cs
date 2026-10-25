using System.Windows.Controls;
using PbePro.ViewModels;

namespace PbePro.Views;

public partial class StartPage : Page
{
    public StartPage()
    {
        InitializeComponent();
        DataContext = new StartViewModel();
    }
}
