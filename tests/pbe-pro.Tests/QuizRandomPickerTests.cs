using PbePro.Models;
using PbePro.Services;

namespace PbePro.Tests;

public class QuizRandomPickerTests
{
    private static List<Question> Bank() => new()
    {
        new() { Id = "s1", Text = "S1", Answer = "A", Points = 1, Category = "Science" },
        new() { Id = "s2", Text = "S2", Answer = "A", Points = 2, Category = "Science" },
        new() { Id = "s3", Text = "S3", Answer = "A", Points = 1, Category = "Science" },
        new() { Id = "h1", Text = "H1", Answer = "A", Points = 1, Category = "History" },
        new() { Id = "n1", Text = "N1", Answer = "A", Points = 1 }
    };

    [Fact]
    public void Filter_ByCategory_IsCaseInsensitive()
    {
        var result = QuizRandomPicker.Filter(Bank(), "science", null);

        Assert.Equal(new[] { "s1", "s2", "s3" }, result.Select(q => q.Id));
    }

    [Fact]
    public void Filter_EmptyCategory_MeansUncategorized()
    {
        var result = QuizRandomPicker.Filter(Bank(), "", null);

        Assert.Equal("n1", Assert.Single(result).Id);
    }

    [Fact]
    public void Filter_ByPoints()
    {
        var result = QuizRandomPicker.Filter(Bank(), "Science", 1);

        Assert.Equal(new[] { "s1", "s3" }, result.Select(q => q.Id));
    }

    [Fact]
    public void Filter_Nulls_ReturnTheWholeBank()
    {
        var bank = Bank();
        var result = QuizRandomPicker.Filter(bank, null, null);

        Assert.Equal(bank.Count, result.Count);
    }

    [Fact]
    public void Pick_SkipsQuestionsAlreadyInTheQuiz()
    {
        var picked = QuizRandomPicker.Pick(Bank(), new HashSet<string> { "s1", "h1", "n1" }, 10, new Random(1));

        Assert.Equal(2, picked.Count);
        Assert.All(picked, q => Assert.DoesNotContain(q.Id, new[] { "s1", "h1", "n1" }));
    }

    [Fact]
    public void Pick_WhenCountExceedsPool_ReturnsAllRemaining()
    {
        var picked = QuizRandomPicker.Pick(Bank(), Array.Empty<string>(), 100, new Random(1));

        Assert.Equal(5, picked.Count);
    }

    [Fact]
    public void Pick_ZeroCount_ReturnsEmpty()
    {
        Assert.Empty(QuizRandomPicker.Pick(Bank(), Array.Empty<string>(), 0, new Random(1)));
    }

    [Fact]
    public void Pick_IsDeterministicForASeed()
    {
        var pool = Bank();
        var a = QuizRandomPicker.Pick(pool, Array.Empty<string>(), 3, new Random(42));
        var b = QuizRandomPicker.Pick(pool, Array.Empty<string>(), 3, new Random(42));

        Assert.Equal(a.Select(q => q.Id), b.Select(q => q.Id));
    }

    [Fact]
    public void Pick_RejectsNegativeCount()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => QuizRandomPicker.Pick(Bank(), Array.Empty<string>(), -1, new Random(1)));
    }
}
