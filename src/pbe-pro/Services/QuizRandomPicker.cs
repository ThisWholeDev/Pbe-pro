using PbePro.Models;

namespace PbePro.Services;

/// <summary>Chooses random questions from the bank, optionally limited by category and points.</summary>
public static class QuizRandomPicker
{
    /// <summary>
    /// Questions matching an optional category and points value.
    /// A null category or points means any; an empty category means "no category".
    /// </summary>
    public static IReadOnlyList<Question> Filter(
        IEnumerable<Question> bank, string? category, int? points)
    {
        IEnumerable<Question> query = bank;

        if (category is not null)
        {
            query = category.Length == 0
                ? query.Where(q => string.IsNullOrWhiteSpace(q.Category))
                : query.Where(q => string.Equals(q.Category, category, StringComparison.OrdinalIgnoreCase));
        }

        if (points is not null)
            query = query.Where(q => q.Points == points.Value);

        return query.ToList();
    }

    /// <summary>
    /// Picks up to <paramref name="count"/> questions from <paramref name="pool"/>,
    /// skipping any whose id is in <paramref name="excludeIds"/>.
    /// </summary>
    public static IReadOnlyList<Question> Pick(
        IReadOnlyList<Question> pool,
        IReadOnlyCollection<string> excludeIds,
        int count,
        Random random)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(count);

        var candidates = pool.Where(q => !excludeIds.Contains(q.Id)).ToList();
        Shuffle(candidates, random);

        if (count > candidates.Count)
            count = candidates.Count;

        return candidates.GetRange(0, count);
    }

    private static void Shuffle<T>(IList<T> list, Random random)
    {
        for (var i = list.Count - 1; i > 0; i--)
        {
            var j = random.Next(i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }
}
