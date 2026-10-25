using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using PbePro.ViewModels;

namespace PbePro.Views;

public partial class QuizEditorPage : Page
{
    public QuizEditorPage()
    {
        InitializeComponent();
        DataContext = new QuizEditorViewModel();
    }

    private void OnQuizDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (FindParent<Button>(e.OriginalSource as DependencyObject) is not null)
            return;

        if (sender is ListBox { SelectedItem: QuizListItemViewModel item } &&
            DataContext is QuizEditorViewModel viewModel)
        {
            viewModel.OpenQuizCommand.Execute(item);
        }
    }

    private static T? FindParent<T>(DependencyObject? current) where T : DependencyObject
    {
        while (current is not null)
        {
            if (current is T match)
                return match;
            current = VisualTreeHelper.GetParent(current);
        }

        return null;
    }
}
