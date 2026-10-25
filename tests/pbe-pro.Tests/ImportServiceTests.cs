using PbePro.Models;
using PbePro.Services;

namespace PbePro.Tests;

public sealed class ImportServiceTests : IDisposable
{
    private readonly string _folder = Path.Combine(Path.GetTempPath(), "pbe-pro-import-" + Guid.NewGuid());

    private string WriteFile(string json)
    {
        Directory.CreateDirectory(_folder);
        var path = Path.Combine(_folder, Guid.NewGuid() + ".json");
        File.WriteAllText(path, json);
        return path;
    }

    private static readonly Question[] NoExisting = Array.Empty<Question>();

    public void Dispose()
    {
        if (Directory.Exists(_folder))
            Directory.Delete(_folder, true);
    }

    [Fact]
    public void Imports_ObjectWithQuestionsList()
    {
        var path = WriteFile("""
        { "questions": [ { "id": "a", "text": "Q1", "answer": "A1", "points": 2, "category": "Sci" } ] }
        """);

        var result = ImportService.ImportFromFile(path, NoExisting);

        var q = Assert.Single(result.Added);
        Assert.Equal(2, q.Points);
        Assert.Equal("Sci", q.Category);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void Imports_PlainArray()
    {
        var path = WriteFile("""[ { "id": "a", "text": "Q1", "answer": "A1", "points": 1 } ]""");

        var result = ImportService.ImportFromFile(path, NoExisting);

        Assert.Single(result.Added);
    }

    [Fact]
    public void MissingId_GetsGeneratedId()
    {
        var path = WriteFile("""[ { "text": "Q1", "answer": "A1", "points": 1 } ]""");

        var result = ImportService.ImportFromFile(path, NoExisting);

        Assert.False(string.IsNullOrWhiteSpace(Assert.Single(result.Added).Id));
    }

    [Fact]
    public void DuplicateId_IsSkipped()
    {
        var existing = new[] { new Question { Id = "a", Text = "Old", Answer = "X", Points = 1 } };
        var path = WriteFile("""[ { "id": "a", "text": "New", "answer": "Y", "points": 1 } ]""");

        var result = ImportService.ImportFromFile(path, existing);

        Assert.Empty(result.Added);
        Assert.Equal(1, result.DuplicatesSkipped);
    }

    [Fact]
    public void DuplicateText_IsSkipped_IgnoringCaseAndWhitespace()
    {
        var existing = new[] { new Question { Id = "a", Text = "Who wrote Hamlet?", Answer = "X", Points = 1 } };
        var path = WriteFile("""[ { "id": "b", "text": "  who wrote hamlet? ", "answer": "Y", "points": 1 } ]""");

        var result = ImportService.ImportFromFile(path, existing);

        Assert.Empty(result.Added);
        Assert.Equal(1, result.DuplicatesSkipped);
    }

    [Fact]
    public void DuplicateWithinTheFile_IsSkipped()
    {
        var path = WriteFile("""
        [ { "text": "Same", "answer": "A", "points": 1 },
          { "text": "Same", "answer": "A", "points": 1 } ]
        """);

        var result = ImportService.ImportFromFile(path, NoExisting);

        Assert.Single(result.Added);
        Assert.Equal(1, result.DuplicatesSkipped);
    }

    [Fact]
    public void InvalidEntry_IsReported_AndOthersStillImport()
    {
        var path = WriteFile("""
        [ { "text": "Bad", "answer": "A", "points": -1 },
          { "text": "Good", "answer": "A", "points": 1 } ]
        """);

        var result = ImportService.ImportFromFile(path, NoExisting);

        Assert.Equal("Good", Assert.Single(result.Added).Text);
        Assert.Single(result.Errors);
    }

    [Fact]
    public void MalformedJson_ReportsError()
    {
        var path = WriteFile("{ nope");

        var result = ImportService.ImportFromFile(path, NoExisting);

        Assert.Empty(result.Added);
        Assert.Single(result.Errors);
    }

    [Fact]
    public void UnexpectedShape_ReportsError()
    {
        var path = WriteFile("42");

        var result = ImportService.ImportFromFile(path, NoExisting);

        Assert.Empty(result.Added);
        Assert.Single(result.Errors);
    }

    [Fact]
    public void MissingFile_ReportsError()
    {
        var result = ImportService.ImportFromFile(Path.Combine(_folder, "nope.json"), NoExisting);

        Assert.Empty(result.Added);
        Assert.Single(result.Errors);
    }
}
