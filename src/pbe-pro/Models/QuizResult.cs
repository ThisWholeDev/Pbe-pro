using System.Text.Json.Serialization;

namespace PbePro.Models;

/// <summary>How one question went in one run of a quiz. Points and category are copied so history survives edits.</summary>
public class QuestionOutcome
{
    public string QuestionId { get; set; } = "";
    public bool Correct { get; set; }
    public int Points { get; set; } = 1;
    public string? Category { get; set; }

    /// <summary>
    /// Points the coach awarded for this question, from 0 up to <see cref="Points"/>.
    /// Missing in results saved before partial credit existed; then it is worked out from <see cref="Correct"/>.
    /// </summary>
    public int? PointsAwarded { get; set; }

    [JsonIgnore] public int EarnedPoints => PointsAwarded ?? (Correct ? Points : 0);

    /// <summary>Some points, but not all of them.</summary>
    [JsonIgnore] public bool IsPartial => !Correct && EarnedPoints > 0;
}

/// <summary>A finished (or ended-early) run through a quiz, saved to results.json.</summary>
public class QuizResult
{
    public string Id { get; set; } = "";
    public string QuizName { get; set; } = "";
    public DateTimeOffset CompletedAt { get; set; }

    /// <summary>Questions in the quiz, including any not answered if it was ended early.</summary>
    public int TotalQuestions { get; set; }

    public int DurationSeconds { get; set; }
    public List<QuestionOutcome> Outcomes { get; set; } = new();

    [JsonIgnore] public int AnsweredCount => Outcomes.Count;
    [JsonIgnore] public int CorrectCount => Outcomes.Count(o => o.Correct);
    [JsonIgnore] public int PointsEarned => Outcomes.Sum(o => o.EarnedPoints);
    [JsonIgnore] public int PartialCount => Outcomes.Count(o => o.IsPartial);
    [JsonIgnore] public int PointsAnswered => Outcomes.Sum(o => o.Points);
    [JsonIgnore] public bool EndedEarly => AnsweredCount < TotalQuestions;
    [JsonIgnore] public double Percent => AnsweredCount == 0 ? 0 : 100.0 * CorrectCount / AnsweredCount;
}
