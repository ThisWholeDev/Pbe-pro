using PbePro.Models;
using PbePro.Services;

namespace PbePro.Tests;

public class QuizResultTests
{
    private static Question Q(string id, int points, string? category = null) =>
        new() { Id = id, Text = $"T{id}", Answer = "A", Points = points, Category = category };

    private static QuizSession Session() =>
        new(new[] { Q("a", 1, "Science"), Q("b", 2, "Literature"), Q("c", 1) });

    [Fact]
    public void Outcomes_RecordIdPointsCategoryAndCorrectness()
    {
        var session = Session();
        session.Mark(true);
        session.Mark(false);

        var outcomes = session.Outcomes;

        Assert.Equal(2, outcomes.Count);
        Assert.Equal("a", outcomes[0].QuestionId);
        Assert.True(outcomes[0].Correct);
        Assert.Equal("Science", outcomes[0].Category);
        Assert.Equal("b", outcomes[1].QuestionId);
        Assert.False(outcomes[1].Correct);
        Assert.Equal(2, outcomes[1].Points);
    }

    [Fact]
    public void ToResult_CopiesTotalsTimeAndDate()
    {
        var session = Session();
        session.Mark(true);
        var when = new DateTimeOffset(2026, 10, 25, 15, 0, 0, TimeSpan.Zero);

        var result = session.ToResult("Practice 1", when, TimeSpan.FromSeconds(402.4));

        Assert.False(string.IsNullOrWhiteSpace(result.Id));
        Assert.Equal("Practice 1", result.QuizName);
        Assert.Equal(when, result.CompletedAt);
        Assert.Equal(3, result.TotalQuestions);
        Assert.Equal(402, result.DurationSeconds);
        Assert.Single(result.Outcomes);
    }

    [Fact]
    public void ToResult_GivesEachRunItsOwnId()
    {
        var session = Session();
        var now = DateTimeOffset.Now;

        Assert.NotEqual(session.ToResult("x", now, TimeSpan.Zero).Id, session.ToResult("x", now, TimeSpan.Zero).Id);
    }

    [Fact]
    public void DerivedTotals_ComeFromTheOutcomes()
    {
        var result = new QuizResult
        {
            TotalQuestions = 4,
            Outcomes =
            {
                new QuestionOutcome { QuestionId = "a", Correct = true, Points = 1 },
                new QuestionOutcome { QuestionId = "b", Correct = false, Points = 2 },
                new QuestionOutcome { QuestionId = "c", Correct = true, Points = 2 }
            }
        };

        Assert.Equal(3, result.AnsweredCount);
        Assert.Equal(2, result.CorrectCount);
        Assert.Equal(3, result.PointsEarned);
        Assert.Equal(5, result.PointsAnswered);
        Assert.True(result.EndedEarly);
        Assert.Equal(66.67, result.Percent, 2);
    }

    [Fact]
    public void AnsweringEverything_IsNotEndedEarly()
    {
        var result = new QuizResult { TotalQuestions = 1, Outcomes = { new QuestionOutcome { QuestionId = "a" } } };

        Assert.False(result.EndedEarly);
    }
}
