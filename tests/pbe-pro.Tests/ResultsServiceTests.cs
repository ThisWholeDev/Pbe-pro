using PbePro.Models;
using PbePro.Services;

namespace PbePro.Tests;

public sealed class ResultsServiceTests : IDisposable
{
    private readonly string _folder = Path.Combine(Path.GetTempPath(), "pbe-pro-results-" + Guid.NewGuid());

    private ResultsService Create() => new(_folder);
    private string FilePath => Path.Combine(_folder, ResultsService.FileName);

    private static QuizResult Result(string name = "Practice 1") => new()
    {
        Id = Guid.NewGuid().ToString(),
        QuizName = name,
        CompletedAt = new DateTimeOffset(2026, 10, 25, 15, 30, 0, TimeSpan.Zero),
        TotalQuestions = 2,
        DurationSeconds = 95,
        Outcomes =
        {
            new QuestionOutcome { QuestionId = "q1", Correct = true, Points = 1, Category = "Science" },
            new QuestionOutcome { QuestionId = "q2", Correct = false, Points = 2 }
        }
    };

    public void Dispose()
    {
        if (Directory.Exists(_folder))
            Directory.Delete(_folder, true);
    }

    [Fact]
    public void Load_WithNoFile_ReturnsEmptyWithoutErrors()
    {
        var loaded = Create().Load();

        Assert.Empty(loaded.Items);
        Assert.False(loaded.HasErrors);
    }

    [Fact]
    public void TryAppend_ThenLoad_RoundTripsEverything()
    {
        var service = Create();
        var original = Result();

        Assert.Null(service.TryAppend(original));
        var loaded = service.Load();

        var result = Assert.Single(loaded.Items);
        Assert.Equal(original.Id, result.Id);
        Assert.Equal("Practice 1", result.QuizName);
        Assert.Equal(original.CompletedAt, result.CompletedAt);
        Assert.Equal(2, result.TotalQuestions);
        Assert.Equal(95, result.DurationSeconds);
        Assert.Equal(2, result.Outcomes.Count);
        Assert.Equal("Science", result.Outcomes[0].Category);
        Assert.False(result.Outcomes[1].Correct);
    }

    [Fact]
    public void TryAppend_KeepsEarlierResults()
    {
        var service = Create();

        service.TryAppend(Result("First"));
        service.TryAppend(Result("Second"));

        Assert.Equal(new[] { "First", "Second" }, service.Load().Items.Select(r => r.QuizName));
    }

    [Fact]
    public void SavedFile_UsesCamelCase_AndDoesNotStoreDerivedTotals()
    {
        Create().TryAppend(Result());

        var text = File.ReadAllText(FilePath);

        Assert.Contains("\"results\"", text);
        Assert.Contains("\"quizName\"", text);
        Assert.Contains("\"outcomes\"", text);
        Assert.DoesNotContain("\"correctCount\"", text);
        Assert.DoesNotContain("\"percent\"", text);
    }

    [Fact]
    public void TryAppend_WhenTheFileIsCorrupt_LeavesItAloneAndSaysSo()
    {
        Directory.CreateDirectory(_folder);
        File.WriteAllText(FilePath, "{ not json");

        var message = Create().TryAppend(Result());

        Assert.NotNull(message);
        Assert.Equal("{ not json", File.ReadAllText(FilePath));
    }

    [Fact]
    public void Load_SkipsEntriesWithoutAnId_AndKeepsTheRest()
    {
        Directory.CreateDirectory(_folder);
        File.WriteAllText(FilePath, """
        { "results": [
            { "id": "r1", "quizName": "A", "completedAt": "2026-10-25T10:00:00+00:00",
              "totalQuestions": 1, "durationSeconds": 30,
              "outcomes": [ { "questionId": "q1", "correct": true, "points": 1 } ] },
            { "id": "", "quizName": "B" }
        ] }
        """);

        var loaded = Create().Load();

        Assert.Equal("r1", Assert.Single(loaded.Items).Id);
        Assert.Single(loaded.Errors);
    }

    [Fact]
    public void Load_WithMalformedJson_ReportsAnErrorInsteadOfThrowing()
    {
        Directory.CreateDirectory(_folder);
        File.WriteAllText(FilePath, "oops");

        var loaded = Create().Load();

        Assert.Empty(loaded.Items);
        Assert.Single(loaded.Errors);
    }
}
