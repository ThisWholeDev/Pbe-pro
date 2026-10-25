using PbePro.Services;

namespace PbePro.Tests;

public sealed class DataFolderResultsTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "pbe-pro-folder-results-" + Guid.NewGuid());

    public void Dispose()
    {
        if (Directory.Exists(_root))
            Directory.Delete(_root, true);
    }

    [Fact]
    public void ResultsFile_IsPartOfTheDataThatMovesWithTheFolder()
    {
        var from = Path.Combine(_root, "from");
        var to = Path.Combine(_root, "to");
        Directory.CreateDirectory(from);
        File.WriteAllText(Path.Combine(from, ResultsService.FileName), "{}");

        var files = DataFolderService.FilesIn(from);
        DataFolderService.Copy(from, to, files);

        Assert.Contains(ResultsService.FileName, files);
        Assert.True(File.Exists(Path.Combine(to, ResultsService.FileName)));
    }
}
