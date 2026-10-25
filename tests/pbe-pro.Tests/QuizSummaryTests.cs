using PbePro.Models;
using PbePro.Services;

namespace PbePro.Tests;

public class QuizSummaryTests
{
    [Fact]
    public void EmptyQuiz_IsZeroes()
    {
        var summary = QuizSummary.From(Array.Empty<Question>());

        Assert.Equal(0, summary.QuestionCount);
        Assert.Equal(0, summary.TotalPoints);
        Assert.Equal(0, summary.RunTimeSeconds);
        Assert.Equal("0:00", summary.RunTimeText);
        Assert.Equal("0 questions · 0 pts · 0:00", summary.DisplayText);
    }

    [Fact]
    public void TenOnePointers_RunForFiveMinutes()
    {
        var questions = Enumerable.Range(1, 10)
            .Select(i => new Question { Id = $"q{i}", Text = "T", Answer = "A", Points = 1 });

        var summary = QuizSummary.From(questions);

        Assert.Equal(10, summary.QuestionCount);
        Assert.Equal(10, summary.TotalPoints);
        Assert.Equal(300, summary.RunTimeSeconds);
        Assert.Equal("5:00", summary.RunTimeText);
        Assert.Equal("10 questions · 10 pts · 5:00", summary.DisplayText);
    }

    [Fact]
    public void MixedPoints_SumsTimeLimits()
    {
        var questions = new[]
        {
            new Question { Id = "q1", Text = "T", Answer = "A", Points = 1 }, // 30s
            new Question { Id = "q2", Text = "T", Answer = "A", Points = 2 }, // 35s
            new Question { Id = "q3", Text = "T", Answer = "A", Points = 5 }  // 50s
        };

        var summary = QuizSummary.From(questions);

        Assert.Equal(3, summary.QuestionCount);
        Assert.Equal(8, summary.TotalPoints);
        Assert.Equal(115, summary.RunTimeSeconds);
        Assert.Equal("1:55", summary.RunTimeText);
    }

    [Fact]
    public void SingularWording_ForOneQuestion()
    {
        var summary = QuizSummary.From(new[]
        {
            new Question { Id = "q1", Text = "T", Answer = "A", Points = 1 }
        });

        Assert.Equal("1 question · 1 pt · 0:30", summary.DisplayText);
    }
}
