using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using PbePro.Services;
using PbePro.ViewModels;

namespace PbePro.Views;

public partial class QuizRunPage : Page
{
    private readonly QuizRunViewModel _viewModel;
    private Window? _window;

    public QuizRunPage(QuizSession session, string quizName)
    {
        InitializeComponent();
        _viewModel = new QuizRunViewModel(session, quizName);
        DataContext = _viewModel;

        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (_viewModel.IsEmpty)
        {
            Navigator.GoHome();
            return;
        }

        if (_window is not null)
            return;

        // Listen on the window rather than the page, so keys work whatever has focus.
        _window = Window.GetWindow(this);
        if (_window is not null)
            _window.PreviewKeyDown += OnWindowKeyDown;
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        if (_window is not null)
            _window.PreviewKeyDown -= OnWindowKeyDown;

        _window = null;
        _viewModel.Shutdown();
    }

    private void OnWindowKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Handled || e.IsRepeat)
            return;

        switch (e.Key)
        {
            case Key.Space:
                _viewModel.PrimaryActionCommand.Execute(null);
                break;
            case Key.C:
                _viewModel.MarkCorrectCommand.Execute(null);
                break;
            case Key.X:
                _viewModel.MarkIncorrectCommand.Execute(null);
                break;
            case Key.P:
                _viewModel.TogglePauseCommand.Execute(null);
                break;
            case Key.Z when Keyboard.Modifiers == ModifierKeys.Control:
                _viewModel.UndoCommand.Execute(null);
                break;
            case Key.Escape:
                _viewModel.ExitCommand.Execute(null);
                break;
            default:
                return;
        }

        e.Handled = true;
    }
}
