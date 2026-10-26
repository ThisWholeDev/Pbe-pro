using PbePro.Services;

namespace PbePro.Tests;

public class PointsScaleTests
{
    [Theory]
    [InlineData(1)]
    [InlineData(0)]
    public void OnePointQuestions_HaveNoScale(int points)
    {
        Assert.Empty(PointsScale.Build(points));
    }

    [Fact]
    public void ThreePoints_RunsFromIncorrectThroughPartialToCorrect()
    {
        var scale = PointsScale.Build(3);

        Assert.Equal(new[] { 0, 1, 2, 3 }, scale.Select(o => o.Points));
        Assert.Equal(new[] { "None", "Partial", "Partial", "Full" }, scale.Select(o => o.Kind));
        Assert.Equal("Incorrect (X)", scale[0].Caption);
        Assert.Equal("Correct (C)", scale[^1].Caption);
        Assert.Equal("", scale[1].Caption);
    }

    [Fact]
    public void TwoPoints_HasASinglePartialStep()
    {
        var scale = PointsScale.Build(2);

        Assert.Equal(new[] { "None", "Partial", "Full" }, scale.Select(o => o.Kind));
    }

    [Fact]
    public void FivePoints_HasFourPartialSteps()
    {
        var scale = PointsScale.Build(5);

        Assert.Equal(6, scale.Count);
        Assert.Equal(4, scale.Count(o => o.Kind == "Partial"));
        Assert.Equal(5, scale[^1].Points);
    }

    [Theory]
    [InlineData(2, "Points awarded (out of 2). Press 1, C or X")]
    [InlineData(3, "Points awarded (out of 3). Press 1–2, C or X")]
    [InlineData(5, "Points awarded (out of 5). Press 1–4, C or X")]
    public void Caption_ExplainsTheScaleAndTheKeys(int points, string expected)
    {
        Assert.Equal(expected, PointsScale.Caption(points));
    }

    [Fact]
    public void Caption_StopsAtNine_BecauseThereAreNoKeysPastIt()
    {
        Assert.Equal("Points awarded (out of 15). Press 1–9, C or X", PointsScale.Caption(15));
    }
}
