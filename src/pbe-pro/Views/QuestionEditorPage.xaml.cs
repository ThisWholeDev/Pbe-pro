using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Threading;
using PbePro.ViewModels;

namespace PbePro.Views;

public partial class QuestionEditorPage : Page
{
    private readonly QuestionEditorViewModel _viewModel = new();

    public QuestionEditorPage()
    {
        InitializeComponent();
        DataContext = _viewModel;

        _viewModel.QuestionAdded += () =>
            Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() =>
            {
                if (_viewModel.SelectedQuestion is { } selected)
                    QuestionList.ScrollIntoView(selected);
                QuestionTextBox.Focus();
            }));
    }

    /// <summary>Enter in the answer box saves it and starts the next question, for fast entry.</summary>
    private void OnAnswerKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter)
            return;

        e.Handled = true;

        if (sender is TextBox box)
            BindingOperations.GetBindingExpression(box, TextBox.TextProperty)?.UpdateSource();

        // Don't pile up blank questions if the current one isn't finished.
        if (_viewModel.SelectedQuestion?.IsIncomplete == true)
            return;

        _viewModel.AddQuestionCommand.Execute(null);
    }
}
