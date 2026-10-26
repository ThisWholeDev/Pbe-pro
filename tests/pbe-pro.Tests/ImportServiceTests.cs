using PbePro.Models;
using PbePro.Services;

namespace PbePro.Tests;

public sealed class ImportServiceTests : IDisposable
{
    private const string Header = "Id,Question,Answer,Points,Category";

    private readonly string _folder = Path.Combine(Path.GetTempPath(), "pbe-pro-import-" + Guid.NewGuid());

    private string WriteFile(string csv)
    {
        Directory.CreateDirectory(_folder);
        var path = Path.Combine(_folder, Guid.NewGuid() + ".csv");
        File.WriteAllText(path, csv);
        return path;
    }

    private static readonly Question[] NoExisting = Array.Empty<Question>();

    public void Dispose()
    {
        if (Directory.Exists(_folder))
            Directory.Delete(_folder, true);
    }

    [Fact]
    public void Imports_ValidRows()
    {
        var path = WriteFile($"""
        {Header}
        a,Q1,A1,2,Sci
        """);

        var result = ImportService.ImportFromFile(path, NoExisting);

        var q = Assert.Single(result.Added);
        Assert.Equal("a", q.Id);
        Assert.Equal(2, q.Points);
        Assert.Equal("Sci", q.Category);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void Headers_AreMatchedCaseInsensitively_InAnyOrder()
    {
        var path = WriteFile("""
        points,CATEGORY,answer,QUESTION,id
        3,Sci,A1,Q1,a
        """);

        var result = ImportService.ImportFromFile(path, NoExisting);

        var q = Assert.Single(result.Added);
        Assert.Equal("Q1", q.Text);
        Assert.Equal(3, q.Points);
    }

    [Fact]
    public void QuotedFields_MayContainCommas()
    {
        var path = WriteFile($"""
        {Header}
        a,"Name a city, any city",Paris,1,Geo
        """);

        var result = ImportService.ImportFromFile(path, NoExisting);

        Assert.Equal("Name a city, any city", Assert.Single(result.Added).Text);
    }

    [Fact]
    public void MissingId_GetsGeneratedId()
    {
        var path = WriteFile($"""
        {Header}
        ,Q1,A1,1,
        """);

        var result = ImportService.ImportFromFile(path, NoExisting);

        Assert.False(string.IsNullOrWhiteSpace(Assert.Single(result.Added).Id));
    }

    [Fact]
    public void BlankCategory_BecomesNull()
    {
        var path = WriteFile($"""
        {Header}
        a,Q1,A1,1,
        """);

        var result = ImportService.ImportFromFile(path, NoExisting);

        Assert.Null(Assert.Single(result.Added).Category);
    }

    [Fact]
    public void OptionalColumns_CanBeOmittedFromTheFile()
    {
        var path = WriteFile("""
        Question,Answer,Points
        Q1,A1,2
        """);

        var result = ImportService.ImportFromFile(path, NoExisting);

        var q = Assert.Single(result.Added);
        Assert.False(string.IsNullOrWhiteSpace(q.Id));
        Assert.Null(q.Category);
        Assert.Equal(2, q.Points);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void MissingHeader_ReportsError_AndImportsNothing()
    {
        var path = WriteFile("""
        Id,Question,Answer,Category
        a,Q1,A1,Sci
        """);

        var result = ImportService.ImportFromFile(path, NoExisting);

        Assert.Empty(result.Added);
        var error = Assert.Single(result.Errors);
        Assert.Contains("Points", error);
    }

    [Fact]
    public void MissingHeaders_AreAllListed()
    {
        var path = WriteFile("""
        Id,Category
        a,Sci
        """);

        var result = ImportService.ImportFromFile(path, NoExisting);

        var error = Assert.Single(result.Errors);
        Assert.Contains("Question", error);
        Assert.Contains("Answer", error);
        Assert.Contains("Points", error);
    }

    [Fact]
    public void NoHeaderRow_ReportsError()
    {
        // The first row is treated as the header, so the required headers are missing.
        var path = WriteFile("a,Q1,A1,1,Sci");

        var result = ImportService.ImportFromFile(path, NoExisting);

        Assert.Empty(result.Added);
        Assert.Single(result.Errors);
    }

    [Fact]
    public void EmptyFile_ReportsError()
    {
        var path = WriteFile("");

        var result = ImportService.ImportFromFile(path, NoExisting);

        Assert.Empty(result.Added);
        Assert.Single(result.Errors);
    }

    [Fact]
    public void HeaderOnly_ImportsNothing_WithoutErrors()
    {
        var path = WriteFile(Header);

        var result = ImportService.ImportFromFile(path, NoExisting);

        Assert.Empty(result.Added);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void DuplicateId_IsSkipped()
    {
        var existing = new[] { new Question { Id = "a", Text = "Old", Answer = "X", Points = 1 } };
        var path = WriteFile($"""
        {Header}
        a,New,Y,1,
        """);

        var result = ImportService.ImportFromFile(path, existing);

        Assert.Empty(result.Added);
        Assert.Equal(1, result.DuplicatesSkipped);
    }

    [Fact]
    public void DuplicateText_IsSkipped_IgnoringCaseAndWhitespace()
    {
        var existing = new[] { new Question { Id = "a", Text = "Who wrote Hamlet?", Answer = "X", Points = 1 } };
        var path = WriteFile($"""
        {Header}
        b,"  who wrote hamlet? ",Y,1,
        """);

        var result = ImportService.ImportFromFile(path, existing);

        Assert.Empty(result.Added);
        Assert.Equal(1, result.DuplicatesSkipped);
    }

    [Fact]
    public void DuplicateWithinTheFile_IsSkipped()
    {
        var path = WriteFile($"""
        {Header}
        ,Same,A,1,
        ,Same,A,1,
        """);

        var result = ImportService.ImportFromFile(path, NoExisting);

        Assert.Single(result.Added);
        Assert.Equal(1, result.DuplicatesSkipped);
    }

    [Fact]
    public void InvalidEntry_IsReported_AndOthersStillImport()
    {
        var path = WriteFile($"""
        {Header}
        ,Bad,A,-1,
        ,Good,A,1,
        """);

        var result = ImportService.ImportFromFile(path, NoExisting);

        Assert.Equal("Good", Assert.Single(result.Added).Text);
        Assert.Single(result.Errors);
    }

    [Fact]
    public void NonNumericPoints_IsReported_AndOthersStillImport()
    {
        var path = WriteFile($"""
        {Header}
        ,Bad,A,lots,
        ,Good,A,1,
        """);

        var result = ImportService.ImportFromFile(path, NoExisting);

        Assert.Equal("Good", Assert.Single(result.Added).Text);
        Assert.Contains("Points", Assert.Single(result.Errors));
    }

    [Fact]
    public void ShortRow_IsReported_AndOthersStillImport()
    {
        var path = WriteFile($"""
        {Header}
        a,OnlyText
        ,Good,A,1,
        """);

        var result = ImportService.ImportFromFile(path, NoExisting);

        Assert.Equal("Good", Assert.Single(result.Added).Text);
        Assert.Single(result.Errors);
    }

    [Fact]
    public void MissingFile_ReportsError()
    {
        var result = ImportService.ImportFromFile(Path.Combine(_folder, "nope.csv"), NoExisting);

        Assert.Empty(result.Added);
        Assert.Single(result.Errors);
    }
}