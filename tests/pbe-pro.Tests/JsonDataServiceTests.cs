using PbePro.Models;
using PbePro.Services;

namespace PbePro.Tests;

public sealed class JsonDataServiceTests : IDisposable
{
    private readonly string _folder = Path.Combine(Path.GetTempPath(), "pbe-pro-tests-" + Guid.NewGuid());

    private JsonDataService Create() => new(_folder);

    private void WriteRaw(string fileName, string content)
    {
        Directory.CreateDirectory(_folder);
        File.WriteAllText(Path.Combine(_folder, fileName), content);
    }

    public void Dispose()
    {
        if (Directory.Exists(_folder))
            Directory.Delete(_folder, true);
    }

    [Fact]
    public void LoadQuestions_WhenNoFile_ReturnsEmptyWithoutErrors()
    {
        var result = Create().LoadQuestions();

        Assert.Empty(result.Items);
        Assert.False(result.HasErrors);
    }

    [Fact]
    public void QuestionsRoundTrip()
    {
        var service = Create();
        service.SaveQuestions(new[]
        {
            new Question { Id = "q1", Text = "Who wrote Hamlet?", Answer = "Shakespeare", Points = 2, Category = "Lit" },
            new Question { Id = "q2", Text = "2+2?", Answer = "4", Points = 1 }
        });

        var result = service.LoadQuestions();

        Assert.False(result.HasErrors);
        Assert.Equal(2, result.Items.Count);
        Assert.Equal("Shakespeare", result.Items[0].Answer);
        Assert.Equal(2, result.Items[0].Points);
        Assert.Equal("Lit", result.Items[0].Category);
    }

    [Fact]
    public void SavedFiles_UseCamelCaseNames()
    {
        var service = Create();
        service.SaveQuestions(new[] { new Question { Id = "q1", Text = "T", Answer = "A", Points = 1 } });
        service.SaveQuizzes(new[] { new Quiz { Id = "z1", Name = "N", QuestionIds = { "q1" } } });

        var questions = File.ReadAllText(Path.Combine(_folder, JsonDataService.QuestionsFileName));
        var quizzes = File.ReadAllText(Path.Combine(_folder, JsonDataService.QuizzesFileName));

        Assert.Contains("\"questions\"", questions);
        Assert.Contains("\"points\"", questions);
        Assert.Contains("\"quizzes\"", quizzes);
        Assert.Contains("\"questionIds\"", quizzes);
    }

    [Fact]
    public void LoadQuestions_MalformedJson_ReportsErrorInsteadOfThrowing()
    {
        WriteRaw(JsonDataService.QuestionsFileName, "{ this is not json");

        var result = Create().LoadQuestions();

        Assert.Empty(result.Items);
        var error = Assert.Single(result.Errors);
        Assert.Contains("not valid JSON", error);
    }

    [Fact]
    public void LoadQuestions_SkipsInvalidEntries_AndKeepsGoodOnes()
    {
        WriteRaw(JsonDataService.QuestionsFileName, """
        {
          "questions": [
            { "id": "q1", "text": "A", "answer": "B", "points": 1 },
            { "id": "q1", "text": "C", "answer": "D", "points": 1 },
            { "id": "q2", "text": "E", "answer": "F", "points": 0 },
            { "text": "G", "answer": "H", "points": 1 }
          ]
        }
        """);

        var result = Create().LoadQuestions();

        var kept = Assert.Single(result.Items);
        Assert.Equal("q1", kept.Id);
        Assert.Equal(3, result.Errors.Count); // duplicate id, bad points, missing id
    }

    [Fact]
    public void QuizzesRoundTrip_PreservingQuestionOrder()
    {
        var service = Create();
        service.SaveQuizzes(new[]
        {
            new Quiz { Id = "z1", Name = "Practice 1", QuestionIds = { "q3", "q1", "q2" } }
        });

        var result = service.LoadQuizzes();

        var quiz = Assert.Single(result.Items);
        Assert.Equal("Practice 1", quiz.Name);
        Assert.Equal(new[] { "q3", "q1", "q2" }, quiz.QuestionIds);
    }

    [Fact]
    public void LoadQuizzes_SkipsQuizWithoutName()
    {
        WriteRaw(JsonDataService.QuizzesFileName, """
        { "quizzes": [ { "id": "z1", "name": "", "questionIds": [] },
                       { "id": "z2", "name": "Good", "questionIds": ["q1"] } ] }
        """);

        var result = Create().LoadQuizzes();

        Assert.Equal("z2", Assert.Single(result.Items).Id);
        Assert.Single(result.Errors);
    }

    [Fact]
    public void LoadQuizzes_MissingQuestionIds_BecomesEmptyList()
    {
        WriteRaw(JsonDataService.QuizzesFileName, """
        { "quizzes": [ { "id": "z1", "name": "No questions yet" } ] }
        """);

        var result = Create().LoadQuizzes();

        Assert.Empty(Assert.Single(result.Items).QuestionIds);
    }
}
