using PbePro.Models;
using PbePro.Services;

namespace PbePro.Tests;

public class QuestionValidatorTests
{
    private static Question Valid() => new() { Id = "q1", Text = "Who?", Answer = "Me", Points = 1 };

    [Fact]
    public void ValidQuestion_HasNoProblems() => Assert.Empty(QuestionValidator.Validate(Valid()));

    [Fact]
    public void BlankText_IsReported()
    {
        var q = Valid();
        q.Text = "  ";
        Assert.Single(QuestionValidator.Validate(q));
    }

    [Fact]
    public void BlankAnswer_IsReported()
    {
        var q = Valid();
        q.Answer = "";
        Assert.Single(QuestionValidator.Validate(q));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-3)]
    public void PointsBelowOne_AreReported(int points)
    {
        var q = Valid();
        q.Points = points;
        Assert.Single(QuestionValidator.Validate(q));
    }

    [Fact]
    public void ThreePoints_IsAllowed()
    {
        var q = Valid();
        q.Points = 3;
        Assert.Empty(QuestionValidator.Validate(q));
    }
}
