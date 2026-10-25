using PbePro.Models;

namespace PbePro.Services;

/// <summary>
/// The state of one run through a quiz: which question is current and how each earlier one was marked.
/// Questions are answered in order, so the number answered is also the index of the current question.
/// </summary>
public sealed class QuizSession
{
    private readonly List<Question> _questions;
    private readonly List<bool> _marks = new();

    public QuizSession(IEnumerable<Question> questions) => _questions = questions.ToList();

    public IReadOnlyList<Question> Questions => _questions;

    public int Total => _questions.Count;
    public int AnsweredCount => _marks.Count;
    public int CorrectCount => _marks.Count(m => m);

    public bool IsFinished => _marks.Count >= _questions.Count;
    public Question? Current => IsFinished ? null : _questions[_marks.Count];

    /// <summary>1-based position of the current question (stays at the last question once finished).</summary>
    public int CurrentNumber => Math.Min(_marks.Count + 1, Math.Max(Total, 1));

    public bool CanUndo => _marks.Count > 0;

    public int PointsEarned => _marks.Select((correct, i) => correct ? _questions[i].Points : 0).Sum();
    public int PointsAnswered => _questions.Take(_marks.Count).Sum(q => q.Points);
    public int PointsTotal => _questions.Sum(q => q.Points);

    /// <summary>Questions marked incorrect, in quiz order.</summary>
    public IReadOnlyList<Question> Missed =>
        _questions.Where((_, i) => i < _marks.Count && !_marks[i]).ToList();

    /// <summary>One entry per answered question, in quiz order.</summary>
    public IReadOnlyList<QuestionOutcome> Outcomes =>
        _marks.Select((correct, i) => new QuestionOutcome
        {
            QuestionId = _questions[i].Id,
            Correct = correct,
            Points = _questions[i].Points,
            Category = _questions[i].Category
        }).ToList();

    public void Mark(bool correct)
    {
        if (IsFinished)
            throw new InvalidOperationException("The quiz is already finished.");

        _marks.Add(correct);
    }

    /// <summary>Takes back the last mark and returns the question that is current again.</summary>
    public Question Undo()
    {
        if (!CanUndo)
            throw new InvalidOperationException("There is nothing to undo.");

        _marks.RemoveAt(_marks.Count - 1);
        return _questions[_marks.Count];
    }

    /// <summary>Packages what happened so far as a result that can be saved to the history.</summary>
    public QuizResult ToResult(string quizName, DateTimeOffset completedAt, TimeSpan elapsed) => new()
    {
        Id = Guid.NewGuid().ToString(),
        QuizName = quizName,
        CompletedAt = completedAt,
        TotalQuestions = Total,
        DurationSeconds = (int)Math.Round(elapsed.TotalSeconds),
        Outcomes = Outcomes.ToList()
    };
}
