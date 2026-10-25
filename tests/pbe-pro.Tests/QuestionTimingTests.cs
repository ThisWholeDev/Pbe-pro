using PbePro.Services;

namespace PbePro.Tests;

public class QuestionTimingTests
{
    [Theory]
    [InlineData(1, 30)]
    [InlineData(2, 35)]
    [InlineData(3, 40)]
    [InlineData(5, 50)]
    public void GetSeconds_FollowsThirtyPlusFivePerExtraPoint(int points, int expected)
    {
        Assert.Equal(expected, QuestionTiming.GetSeconds(points));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void GetSeconds_RejectsPointsBelowOne(int points)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => QuestionTiming.GetSeconds(points));
    }

    [Fact]
    public void GetDuration_ReturnsTimeSpanOfSameLength()
    {
        Assert.Equal(TimeSpan.FromSeconds(35), QuestionTiming.GetDuration(2));
    }
}
