using PbePro.Models;
using PbePro.Services;

namespace PbePro.Tests;

public class TextSizesTests
{
    [Fact]
    public void QuestionSizes_GrowFromSmallToExtraLarge()
    {
        var sizes = new[]
        {
            TextSizes.QuestionFontSize(QuestionTextSize.Small),
            TextSizes.QuestionFontSize(QuestionTextSize.Medium),
            TextSizes.QuestionFontSize(QuestionTextSize.Large),
            TextSizes.QuestionFontSize(QuestionTextSize.ExtraLarge)
        };

        Assert.Equal(sizes.OrderBy(s => s), sizes);
        Assert.Equal(4, sizes.Distinct().Count());
    }

    [Fact]
    public void Large_IsTheDefaultSize()
    {
        Assert.Equal(QuestionTextSize.Large, new AppSettings().TextSize);
        Assert.Equal(40, TextSizes.QuestionFontSize(QuestionTextSize.Large));
    }

    [Theory]
    [InlineData(QuestionTextSize.Small)]
    [InlineData(QuestionTextSize.Medium)]
    [InlineData(QuestionTextSize.Large)]
    [InlineData(QuestionTextSize.ExtraLarge)]
    public void Answer_IsSmallerThanQuestion(QuestionTextSize size)
    {
        Assert.True(TextSizes.AnswerFontSize(size) < TextSizes.QuestionFontSize(size));
    }

    [Fact]
    public void TimerAutoStarts_ByDefault()
    {
        var settings = new AppSettings();

        Assert.True(settings.AutoStartTimer);
        Assert.False(settings.ShuffleQuestions);
        Assert.False(settings.StartFullScreen);
    }
}
