using PbePro.Models;

namespace PbePro.Services;

public static class TextSizes
{
    public static double QuestionFontSize(QuestionTextSize size) => size switch
    {
        QuestionTextSize.Small => 28,
        QuestionTextSize.Medium => 34,
        QuestionTextSize.Large => 40,
        QuestionTextSize.ExtraLarge => 52,
        _ => 40
    };

    /// <summary>The revealed answer is the focus of the screen, so it is larger than the question was.</summary>
    public static double AnswerFontSize(QuestionTextSize size) => Math.Round(QuestionFontSize(size) * 1.2);

    /// <summary>The question shrinks once the answer is showing, to make room for it.</summary>
    public static double RevealedQuestionFontSize(QuestionTextSize size) => Math.Round(QuestionFontSize(size) * 0.7);
}
