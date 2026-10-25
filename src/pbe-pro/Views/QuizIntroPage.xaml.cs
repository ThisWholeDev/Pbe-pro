using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using PbePro.Services;
using PbePro.ViewModels;

namespace PbePro.Views;

public partial class QuizIntroPage : Page
{
    private readonly QuizIntroViewModel _viewModel;
    private Window? _window;

    public QuizIntroPage(QuizSession session, string quizName)
    {
        InitializeComponent();
        _viewModel = new QuizIntroViewModel(session, quizName);
        DataContext = _viewModel;

        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (_window is not null)
            return;

        _window = Window.GetWindow(this);
        if (_window is not null)
            _window.PreviewKeyDown += OnWindowKeyDown;
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        if (_window is not null)
            _window.PreviewKeyDown -= OnWindowKeyDown;

        _window = null;
    }

    private void OnWindowKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Handled || e.IsRepeat)
            return;

        switch (e.Key)
        {
            case Key.Space:
            case Key.Enter:
                _viewModel.BeginCommand.Execute(null);
                break;
            case Key.Escape:
                _viewModel.BackCommand.Execute(null);
                break;
            default:
                return;
        }

        e.Handled = true;
    }
}
