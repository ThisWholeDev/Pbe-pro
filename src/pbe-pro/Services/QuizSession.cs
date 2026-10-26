using PbePro.Models;

namespace PbePro.Services;

/// <summary>
/// The state of one run through a quiz: which question is current and how many points each earlier one earned.
/// Questions are answered in order, so the number answered is also the index of the current question.
/// A question earns 0 points (incorrect), all of its points (correct), or something in between (partial credit).
/// </summary>
public sealed class QuizSession
{
    private readonly List<Question> _questions;
    private readonly List<int> _awarded = new();

    public QuizSession(IEnumerable<Question> questions) => _questions = questions.ToList();

    public IReadOnlyList<Question> Questions => _questions;

    public int Total => _questions.Count;
    public int AnsweredCount => _awarded.Count;

    /// <summary>Questions that earned every point.</summary>
    public int CorrectCount => _awarded.Where((points, i) => points >= _questions[i].Points).Count();

    /// <summary>Questions that earned some points, but not all.</summary>
    public int PartialCount => _awarded.Where((points, i) => points > 0 && points < _questions[i].Points).Count();

    public bool IsFinished => _awarded.Count >= _questions.Count;
    public Question? Current => IsFinished ? null : _questions[_awarded.Count];

    /// <summary>1-based position of the current question (stays at the last question once finished).</summary>
    public int CurrentNumber => Math.Min(_awarded.Count + 1, Math.Max(Total, 1));

    public bool CanUndo => _awarded.Count > 0;

    public int PointsEarned => _awarded.Sum();
    public int PointsAnswered => _questions.Take(_awarded.Count).Sum(q => q.Points);
    public int PointsTotal => _questions.Sum(q => q.Points);

    /// <summary>Questions that didn't earn full credit (including partly right ones), in quiz order.</summary>
    public IReadOnlyList<Question> Missed =>
        _questions.Where((q, i) => i < _awarded.Count && _awarded[i] < q.Points).ToList();

    /// <summary>One entry per answered question, in quiz order.</summary>
    public IReadOnlyList<QuestionOutcome> Outcomes =>
        _awarded.Select((points, i) => new QuestionOutcome
        {
            QuestionId = _questions[i].Id,
            Correct = points >= _questions[i].Points,
            Points = _questions[i].Points,
            PointsAwarded = points,
            Category = _questions[i].Category
        }).ToList();

    /// <summary>Full credit when correct, none when not.</summary>
    public void Mark(bool correct)
    {
        if (IsFinished)
            throw new InvalidOperationException("The quiz is already finished.");

        Award(correct ? _questions[_awarded.Count].Points : 0);
    }

    /// <summary>Gives the current question 0 up to all of its points, then moves on.</summary>
    public void Award(int points)
    {
        if (IsFinished)
            throw new InvalidOperationException("The quiz is already finished.");

        var max = _questions[_awarded.Count].Points;
        if (points < 0 || points > max)
            throw new ArgumentOutOfRangeException(nameof(points), $"Points must be between 0 and {max}.");

        _awarded.Add(points);
    }

    /// <summary>Takes back the last award and returns the question that is current again.</summary>
    public Question Undo()
    {
        if (!CanUndo)
            throw new InvalidOperationException("There is nothing to undo.");

        _awarded.RemoveAt(_awarded.Count - 1);
        return _questions[_awarded.Count];
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
