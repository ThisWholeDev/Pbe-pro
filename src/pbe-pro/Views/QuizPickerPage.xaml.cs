using System.Windows.Controls;
using PbePro.ViewModels;

namespace PbePro.Views;

public partial class QuizPickerPage : Page
{
    public QuizPickerPage()
    {
        InitializeComponent();
        DataContext = new QuizPickerViewModel();
    }
}
