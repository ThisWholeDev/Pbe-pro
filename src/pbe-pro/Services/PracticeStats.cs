using PbePro.Models;

namespace PbePro.Services;

public sealed record CategoryStat(string Category, int Answered, int Correct, int PointsEarned, int PointsPossible)
{
    public double Percent => Answered == 0 ? 0 : 100.0 * Correct / Answered;
    public string PercentText => $"{Math.Round(Percent)}%";
    public string CountText => $"{Correct} / {Answered}";
}

public static class PracticeStats
{
    public const string NoCategory = "No category";

    /// <summary>Totals per category (ignoring letter case), weakest category first.</summary>
    public static List<CategoryStat> ByCategory(IEnumerable<QuestionOutcome> outcomes)
    {
        var groups = new Dictionary<string, (string Display, int Answered, int Correct, int Earned, int Possible)>(
            StringComparer.OrdinalIgnoreCase);

        foreach (var outcome in outcomes)
        {
            var name = string.IsNullOrWhiteSpace(outcome.Category) ? NoCategory : outcome.Category.Trim();

            if (!groups.TryGetValue(name, out var g))
                g = (name, 0, 0, 0, 0);

            groups[name] = (
                g.Display,
                g.Answered + 1,
                g.Correct + (outcome.Correct ? 1 : 0),
                g.Earned + (outcome.Correct ? outcome.Points : 0),
                g.Possible + outcome.Points);
        }

        return groups.Values
            .Select(g => new CategoryStat(g.Display, g.Answered, g.Correct, g.Earned, g.Possible))
            .OrderBy(c => c.Percent)
            .ThenBy(c => c.Category, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    /// <summary>
    /// Ids of questions whose most recent attempt was incorrect. Getting one right later removes it.
    /// </summary>
    public static HashSet<string> MissedLastTime(IEnumerable<QuizResult> results)
    {
        var latest = new Dictionary<string, bool>();

        foreach (var result in results.OrderBy(r => r.CompletedAt))
        {
            foreach (var outcome in result.Outcomes)
                latest[outcome.QuestionId] = outcome.Correct;
        }

        return latest.Where(p => !p.Value).Select(p => p.Key).ToHashSet();
    }
}

public static class MissedPractice
{
    /// <summary>
    /// Questions from the bank whose last attempt was incorrect, in bank order.
    /// Questions that have since been deleted are left out.
    /// </summary>
    public static List<Question> Build(IEnumerable<Question> bank, IEnumerable<QuizResult> results)
    {
        var missed = PracticeStats.MissedLastTime(results);
        return bank.Where(q => missed.Contains(q.Id)).ToList();
    }

    public static List<Question> Shuffled(IReadOnlyList<Question> questions) =>
        QuizRandomPicker.Pick(questions, Array.Empty<string>(), questions.Count, Random.Shared).ToList();
}
