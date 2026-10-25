namespace PbePro.Services;

/// <summary>Time limit per question: 30 seconds for 1 point, plus 5 seconds for each extra point.</summary>
public static class QuestionTiming
{
    public const int BaseSeconds = 25;
    public const int SecondsPerPoint = 5;

    public static int GetSeconds(int points)
    {
        if (points < 1)
            throw new ArgumentOutOfRangeException(nameof(points), "Points must be at least 1.");

        return BaseSeconds + SecondsPerPoint * points;
    }

    public static TimeSpan GetDuration(int points) => TimeSpan.FromSeconds(GetSeconds(points));
}
