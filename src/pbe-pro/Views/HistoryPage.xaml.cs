using System.Windows.Controls;
using PbePro.ViewModels;

namespace PbePro.Views;

public partial class HistoryPage : Page
{
    public HistoryPage()
    {
        InitializeComponent();
        DataContext = new HistoryViewModel();
    }
}
