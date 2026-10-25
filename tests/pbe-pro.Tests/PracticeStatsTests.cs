using PbePro.Models;
using PbePro.Services;

namespace PbePro.Tests;

public class PracticeStatsTests
{
    private static QuestionOutcome O(string id, bool correct, int points = 1, string? category = null) =>
        new() { QuestionId = id, Correct = correct, Points = points, Category = category };

    private static QuizResult R(DateTimeOffset when, params QuestionOutcome[] outcomes) =>
        new() { Id = Guid.NewGuid().ToString(), QuizName = "Q", CompletedAt = when, Outcomes = outcomes.ToList() };

    private static readonly DateTimeOffset Earlier = new(2026, 10, 1, 9, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset Later = new(2026, 10, 8, 9, 0, 0, TimeSpan.Zero);

    // ----- By category -----

    [Fact]
    public void ByCategory_GroupsIgnoringCase_AndTotalsPoints()
    {
        var stats = PracticeStats.ByCategory(new[]
        {
            O("1", true, 1, "Science"),
            O("2", false, 2, "science"),
            O("3", true, 1, "Literature")
        });

        var science = stats.Single(s => s.Category == "Science");
        Assert.Equal(2, science.Answered);
        Assert.Equal(1, science.Correct);
        Assert.Equal(1, science.PointsEarned);
        Assert.Equal(3, science.PointsPossible);
        Assert.Equal("1 / 2", science.CountText);
        Assert.Equal("50%", science.PercentText);
    }

    [Fact]
    public void ByCategory_PutsUncategorisedQuestionsInTheirOwnGroup()
    {
        var stats = PracticeStats.ByCategory(new[] { O("1", false), O("2", false, 1, "  ") });

        var group = Assert.Single(stats);
        Assert.Equal(PracticeStats.NoCategory, group.Category);
        Assert.Equal(2, group.Answered);
    }

    [Fact]
    public void ByCategory_ListsWeakestFirst()
    {
        var stats = PracticeStats.ByCategory(new[]
        {
            O("1", true, 1, "Strong"),
            O("2", false, 1, "Weak"),
            O("3", true, 1, "Middle"),
            O("4", false, 1, "Middle")
        });

        Assert.Equal(new[] { "Weak", "Middle", "Strong" }, stats.Select(s => s.Category));
    }

    [Fact]
    public void ByCategory_WithNothing_ReturnsEmpty()
    {
        Assert.Empty(PracticeStats.ByCategory(Array.Empty<QuestionOutcome>()));
    }

    // ----- Missed last time -----

    [Fact]
    public void MissedLastTime_LatestAttemptWins_RegardlessOfListOrder()
    {
        var older = R(Earlier, O("q1", false), O("q2", true));
        var newer = R(Later, O("q1", true), O("q2", false));

        // Deliberately newest first.
        var missed = PracticeStats.MissedLastTime(new[] { newer, older });

        Assert.Equal(new[] { "q2" }, missed);
    }

    [Fact]
    public void MissedLastTime_ForgetsAQuestionOnceItIsAnsweredCorrectly()
    {
        var missed = PracticeStats.MissedLastTime(new[]
        {
            R(Earlier, O("q1", false)),
            R(Later, O("q1", true))
        });

        Assert.Empty(missed);
    }

    [Fact]
    public void MissedLastTime_WithNoHistory_IsEmpty()
    {
        Assert.Empty(PracticeStats.MissedLastTime(Array.Empty<QuizResult>()));
    }

    // ----- Practice session questions -----

    [Fact]
    public void MissedPractice_ReturnsBankQuestionsInBankOrder_AndSkipsDeletedOnes()
    {
        var bank = new[]
        {
            new Question { Id = "q1", Text = "1", Answer = "a" },
            new Question { Id = "q2", Text = "2", Answer = "a" },
            new Question { Id = "q3", Text = "3", Answer = "a" }
        };
        var history = new[]
        {
            R(Earlier, O("q3", false), O("q2", false), O("q9", false), O("q1", true))
        };

        var practice = MissedPractice.Build(bank, history);

        Assert.Equal(new[] { "q2", "q3" }, practice.Select(q => q.Id));
    }
}
