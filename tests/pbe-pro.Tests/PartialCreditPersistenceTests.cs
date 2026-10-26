using PbePro.Models;
using PbePro.Services;

namespace PbePro.Tests;

public sealed class PartialCreditPersistenceTests : IDisposable
{
    private readonly string _folder = Path.Combine(Path.GetTempPath(), "pbe-pro-partial-" + Guid.NewGuid());

    public void Dispose()
    {
        if (Directory.Exists(_folder))
            Directory.Delete(_folder, true);
    }

    [Fact]
    public void PointsAwarded_SurvivesSavingAndLoading()
    {
        var service = new ResultsService(_folder);
        var result = new QuizResult
        {
            Id = "r1",
            QuizName = "Practice",
            CompletedAt = DateTimeOffset.Now,
            TotalQuestions = 1,
            Outcomes = { new QuestionOutcome { QuestionId = "q1", Correct = false, Points = 3, PointsAwarded = 2 } }
        };

        service.TryAppend(result);
        var loaded = Assert.Single(service.Load().Items);

        Assert.Equal(2, loaded.Outcomes[0].PointsAwarded);
        Assert.Equal(2, loaded.PointsEarned);
    }

    [Fact]
    public void ResultsSavedBeforePartialCredit_StillLoadWithTheRightPoints()
    {
        Directory.CreateDirectory(_folder);
        File.WriteAllText(Path.Combine(_folder, ResultsService.FileName), """
        { "results": [ {
            "id": "old", "quizName": "Old quiz", "completedAt": "2026-10-01T10:00:00+00:00",
            "totalQuestions": 2, "durationSeconds": 60,
            "outcomes": [
              { "questionId": "q1", "correct": true,  "points": 2 },
              { "questionId": "q2", "correct": false, "points": 2 }
            ] } ] }
        """);

        var loaded = Assert.Single(new ResultsService(_folder).Load().Items);

        Assert.Equal(2, loaded.PointsEarned);
        Assert.Equal(4, loaded.PointsAnswered);
        Assert.Equal(0, loaded.PartialCount);
    }
}
