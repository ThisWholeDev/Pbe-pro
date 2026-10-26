using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PbePro.Services;
using PbePro.Views;

namespace PbePro.ViewModels;

public sealed record MissedQuestionItem(string Text, string Answer, int Points, string? Category, string PartialText);

public partial class ResultsViewModel : ObservableObject
{
    private readonly QuizSession _session;
    private readonly string _quizName;

    public ResultsViewModel(QuizSession session, TimeSpan elapsed, string quizName, string? historyNote = null)
    {
        _session = session;
        _quizName = quizName;

        CorrectText = $"{session.CorrectCount} / {session.AnsweredCount}";
        PercentText = session.AnsweredCount == 0
            ? "—"
            : $"{Math.Round(100.0 * session.CorrectCount / session.AnsweredCount)}%";
        PointsText = $"{session.PointsEarned} / {session.PointsAnswered}";
        TimeText = TimeFormat.Elapsed(elapsed);

        EndedEarlyText = session.AnsweredCount < session.Total
            ? $"You ended the quiz after {session.AnsweredCount} of {session.Total} questions."
            : "";

        HistoryNote = historyNote ?? "";

        // Anything short of full credit is listed, with a note when part of the points were earned.
        var outcomes = session.Outcomes;
        for (var i = 0; i < outcomes.Count; i++)
        {
            var outcome = outcomes[i];
            if (outcome.Correct)
                continue;

            var question = session.Questions[i];
            MissedItems.Add(new MissedQuestionItem(
                question.Text, question.Answer, question.Points, question.Category,
                outcome.EarnedPoints > 0
                    ? $"Partial credit: {outcome.EarnedPoints} of {question.Points} points"
                    : ""));
        }

        CorrectCaption = session.PartialCount > 0 ? $"correct · {session.PartialCount} partial" : "correct";

        CategoryRows = PracticeStats.ByCategory(session.Outcomes);

        // A breakdown is only worth showing if it says something: several groups, or one real category.
        HasCategoryBreakdown = CategoryRows.Count > 1
                               || (CategoryRows.Count == 1 && CategoryRows[0].Category != PracticeStats.NoCategory);
    }

    public string QuizName => _quizName;
    public string CorrectText { get; }
    public string CorrectCaption { get; }
    public string PercentText { get; }
    public string PointsText { get; }
    public string TimeText { get; }
    public string EndedEarlyText { get; }
    public string HistoryNote { get; }

    public List<MissedQuestionItem> MissedItems { get; } = new();
    public List<CategoryStat> CategoryRows { get; }

    public bool HasEndedEarlyText => EndedEarlyText.Length > 0;
    public bool HasHistoryNote => HistoryNote.Length > 0;
    public bool HasCategoryBreakdown { get; }
    public bool HasMissed => MissedItems.Count > 0;
    public bool HasNoMissed => MissedItems.Count == 0;
    public string MissedHeading => $"Missed questions ({MissedItems.Count})";

    [RelayCommand]
    private void RetryMissed()
    {
        if (!HasMissed)
            return;

        var name = _quizName.EndsWith(" (missed)") ? _quizName : _quizName + " (missed)";
        Navigator.Navigate(new QuizIntroPage(new QuizSession(_session.Missed), name));
    }

    [RelayCommand]
    private void RunAgain() =>
        Navigator.Navigate(new QuizIntroPage(new QuizSession(_session.Questions), _quizName));

    [RelayCommand]
    private void ChooseAnotherQuiz() => Navigator.Navigate(new QuizPickerPage());

    [RelayCommand]
    private void ViewHistory() => Navigator.Navigate(new HistoryPage());

    [RelayCommand]
    private void GoHome() => Navigator.GoHome();
}
