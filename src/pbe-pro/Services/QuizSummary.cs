using PbePro.Models;

namespace PbePro.Services;

/// <summary>Question count, total points, and estimated run time for a quiz.</summary>
public readonly record struct QuizSummary(int QuestionCount, int TotalPoints, int RunTimeSeconds)
{
    public static QuizSummary From(IEnumerable<Question> questions)
    {
        var count = 0;
        var points = 0;
        var seconds = 0;

        foreach (var question in questions)
        {
            count++;
            points += question.Points;
            seconds += QuestionTiming.GetSeconds(question.Points);
        }

        return new QuizSummary(count, points, seconds);
    }

    public TimeSpan RunTime => TimeSpan.FromSeconds(RunTimeSeconds);

    /// <summary>Minutes and seconds, such as 5:00 or 0:30.</summary>
    public string RunTimeText
    {
        get
        {
            var minutes = RunTimeSeconds / 60;
            var seconds = RunTimeSeconds % 60;
            return $"{minutes}:{seconds:D2}";
        }
    }

    public string DisplayText =>
        $"{QuestionCount} question{(QuestionCount == 1 ? "" : "s")} · " +
        $"{TotalPoints} pt{(TotalPoints == 1 ? "" : "s")} · {RunTimeText}";
}
