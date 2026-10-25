using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PbePro.Services;
using PbePro.Views;

namespace PbePro.ViewModels;

/// <summary>The "ready to begin?" screen shown before the first question.</summary>
public partial class QuizIntroViewModel : ObservableObject
{
    private readonly QuizSession _session;

    public QuizIntroViewModel(QuizSession session, string quizName)
    {
        _session = session;
        QuizName = quizName;

        var summary = QuizSummary.From(session.Questions);
        QuestionCountText = summary.QuestionCount.ToString();
        QuestionCaption = summary.QuestionCount == 1 ? "question" : "questions";
        PointsText = summary.TotalPoints.ToString();
        PointsCaption = summary.TotalPoints == 1 ? "point" : "points";
        TimeText = summary.RunTimeText;

        TimerModeText = SettingsService.Current.AutoStartTimer
            ? "The timer starts automatically on each question."
            : "You start the timer on each question.";
    }

    public string QuizName { get; }
    public string QuestionCountText { get; }
    public string QuestionCaption { get; }
    public string PointsText { get; }
    public string PointsCaption { get; }
    public string TimeText { get; }
    public string TimerModeText { get; }

    [RelayCommand]
    private void Begin() => Navigator.Navigate(new QuizRunPage(_session, QuizName));

    [RelayCommand]
    private void Back() => Navigator.Navigate(new QuizPickerPage());

    [RelayCommand]
    private void ToggleFullScreen() => FullScreenService.Toggle();
}
