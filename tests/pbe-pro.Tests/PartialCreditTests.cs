using PbePro.Models;
using PbePro.Services;

namespace PbePro.Tests;

public class PartialCreditTests
{
    private static Question Q(string id, int points, string? category = null) =>
        new() { Id = id, Text = $"T{id}", Answer = "A", Points = points, Category = category };

    [Fact]
    public void PartialAward_EarnsThosePoints_ButIsNotCorrect()
    {
        var session = new QuizSession(new[] { Q("a", 3), Q("b", 1) });

        session.Award(2);

        Assert.Equal(2, session.PointsEarned);
        Assert.Equal(3, session.PointsAnswered);
        Assert.Equal(0, session.CorrectCount);
        Assert.Equal(1, session.PartialCount);
        Assert.Equal("b", session.Current!.Id);
    }

    [Fact]
    public void AwardingEveryPoint_IsCorrect()
    {
        var session = new QuizSession(new[] { Q("a", 3) });

        session.Award(3);

        Assert.Equal(1, session.CorrectCount);
        Assert.Equal(0, session.PartialCount);
        Assert.Empty(session.Missed);
    }

    [Fact]
    public void AwardingZero_IsIncorrect()
    {
        var session = new QuizSession(new[] { Q("a", 3) });

        session.Award(0);

        Assert.Equal(0, session.PointsEarned);
        Assert.Equal(0, session.PartialCount);
        Assert.Single(session.Missed);
    }

    [Fact]
    public void PartlyRightQuestions_AreIncludedInMissed()
    {
        var session = new QuizSession(new[] { Q("a", 3), Q("b", 3), Q("c", 3) });
        session.Award(2); // partial
        session.Award(3); // full
        session.Award(0); // none

        Assert.Equal(new[] { "a", "c" }, session.Missed.Select(q => q.Id));
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(4)]
    public void Award_OutsideZeroToThePointValue_Throws(int points)
    {
        var session = new QuizSession(new[] { Q("a", 3) });

        Assert.Throws<ArgumentOutOfRangeException>(() => session.Award(points));
        Assert.Equal(0, session.AnsweredCount); // nothing was recorded
    }

    [Fact]
    public void Award_AfterFinishing_Throws()
    {
        var session = new QuizSession(new[] { Q("a", 2) });
        session.Award(1);

        Assert.Throws<InvalidOperationException>(() => session.Award(1));
    }

    [Fact]
    public void Mark_StillMeansFullOrNoCredit()
    {
        var session = new QuizSession(new[] { Q("a", 2), Q("b", 2) });

        session.Mark(true);
        session.Mark(false);

        Assert.Equal(2, session.PointsEarned);
        Assert.Equal(1, session.CorrectCount);
    }

    [Fact]
    public void Undo_RemovesAPartialAward()
    {
        var session = new QuizSession(new[] { Q("a", 3), Q("b", 1) });
        session.Award(2);

        var restored = session.Undo();

        Assert.Equal("a", restored.Id);
        Assert.Equal(0, session.PointsEarned);
        Assert.Equal(0, session.PartialCount);
    }

    [Fact]
    public void Outcomes_RecordPointsAwarded()
    {
        var session = new QuizSession(new[] { Q("a", 3, "Science"), Q("b", 2) });
        session.Award(1);
        session.Award(2);

        var outcomes = session.Outcomes;

        Assert.False(outcomes[0].Correct);
        Assert.Equal(1, outcomes[0].PointsAwarded);
        Assert.True(outcomes[0].IsPartial);
        Assert.True(outcomes[1].Correct);
        Assert.Equal(2, outcomes[1].PointsAwarded);
        Assert.False(outcomes[1].IsPartial);
    }

    // ----- Saved results, including ones from before partial credit existed -----

    [Fact]
    public void EarnedPoints_FallBackToCorrectness_ForOlderResults()
    {
        Assert.Equal(2, new QuestionOutcome { Correct = true, Points = 2 }.EarnedPoints);
        Assert.Equal(0, new QuestionOutcome { Correct = false, Points = 2 }.EarnedPoints);
    }

    [Fact]
    public void EarnedPoints_UseTheAwardedPoints_WhenPresent()
    {
        var outcome = new QuestionOutcome { Correct = false, Points = 3, PointsAwarded = 2 };

        Assert.Equal(2, outcome.EarnedPoints);
        Assert.True(outcome.IsPartial);
    }

    [Fact]
    public void QuizResult_TotalsIncludePartialCredit()
    {
        var result = new QuizResult
        {
            TotalQuestions = 3,
            Outcomes =
            {
                new QuestionOutcome { QuestionId = "a", Correct = true, Points = 3, PointsAwarded = 3 },
                new QuestionOutcome { QuestionId = "b", Correct = false, Points = 3, PointsAwarded = 2 },
                new QuestionOutcome { QuestionId = "c", Correct = false, Points = 1, PointsAwarded = 0 }
            }
        };

        Assert.Equal(5, result.PointsEarned);
        Assert.Equal(7, result.PointsAnswered);
        Assert.Equal(1, result.CorrectCount);
        Assert.Equal(1, result.PartialCount);
    }

    [Fact]
    public void CategoryPoints_CountPartialCredit()
    {
        var stats = PracticeStats.ByCategory(new[]
        {
            new QuestionOutcome { QuestionId = "a", Correct = false, Points = 3, PointsAwarded = 2, Category = "Science" }
        });

        var science = Assert.Single(stats);
        Assert.Equal(2, science.PointsEarned);
        Assert.Equal(3, science.PointsPossible);
    }

    [Fact]
    public void PartlyRightQuestion_CountsAsMissedLastTime()
    {
        var result = new QuizResult
        {
            Id = "r1",
            CompletedAt = DateTimeOffset.Now,
            Outcomes = { new QuestionOutcome { QuestionId = "q1", Correct = false, Points = 3, PointsAwarded = 2 } }
        };

        Assert.Contains("q1", PracticeStats.MissedLastTime(new[] { result }));
    }
}
