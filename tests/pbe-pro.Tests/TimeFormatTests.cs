using PbePro.Services;

namespace PbePro.Tests;

public class TimeFormatTests
{
    [Theory]
    [InlineData(30.0, "0:30")]
    [InlineData(29.1, "0:30")]
    [InlineData(0.2, "0:01")]
    [InlineData(0.0, "0:00")]
    [InlineData(-5.0, "0:00")]
    [InlineData(125.0, "2:05")]
    public void Countdown_RoundsUp_AndNeverGoesNegative(double seconds, string expected)
    {
        Assert.Equal(expected, TimeFormat.Countdown(TimeSpan.FromSeconds(seconds)));
    }

    [Theory]
    [InlineData(0.0, "0:00")]
    [InlineData(59.9, "0:59")]
    [InlineData(402.0, "6:42")]
    [InlineData(3725.0, "1:02:05")]
    public void Elapsed_RoundsDown_AndAddsHoursWhenNeeded(double seconds, string expected)
    {
        Assert.Equal(expected, TimeFormat.Elapsed(TimeSpan.FromSeconds(seconds)));
    }
}
