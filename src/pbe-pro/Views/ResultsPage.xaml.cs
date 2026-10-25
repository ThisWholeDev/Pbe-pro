using System.Windows.Controls;
using PbePro.Services;
using PbePro.ViewModels;

namespace PbePro.Views;

public partial class ResultsPage : Page
{
    public ResultsPage(QuizSession session, TimeSpan elapsed, string quizName, string? historyNote = null)
    {
        InitializeComponent();
        DataContext = new ResultsViewModel(session, elapsed, quizName, historyNote);
    }
}
