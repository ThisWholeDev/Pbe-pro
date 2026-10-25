using PbePro.Services;

namespace PbePro.Tests;

public sealed class DataFolderServiceTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "pbe-pro-folders-" + Guid.NewGuid());

    private string Folder(string name)
    {
        var path = Path.Combine(_root, name);
        Directory.CreateDirectory(path);
        return path;
    }

    public void Dispose()
    {
        if (Directory.Exists(_root))
            Directory.Delete(_root, true);
    }

    [Fact]
    public void FilesIn_ListsOnlyTheDataFilesThatExist()
    {
        var folder = Folder("a");
        File.WriteAllText(Path.Combine(folder, "questions.json"), "{}");
        File.WriteAllText(Path.Combine(folder, "notes.txt"), "x");

        Assert.Equal(new[] { "questions.json" }, DataFolderService.FilesIn(folder));
    }

    [Fact]
    public void FilesIn_MissingFolder_ReturnsEmpty()
    {
        Assert.Empty(DataFolderService.FilesIn(Path.Combine(_root, "does-not-exist")));
    }

    [Fact]
    public void Copy_CopiesFilesIntoANewFolder()
    {
        var from = Folder("from");
        File.WriteAllText(Path.Combine(from, "questions.json"), "hello");
        var to = Path.Combine(_root, "to"); // doesn't exist yet

        DataFolderService.Copy(from, to, new[] { "questions.json" });

        Assert.Equal("hello", File.ReadAllText(Path.Combine(to, "questions.json")));
        Assert.True(File.Exists(Path.Combine(from, "questions.json"))); // original stays
    }

    [Fact]
    public void Copy_NeverOverwritesAnExistingFile()
    {
        var from = Folder("from");
        var to = Folder("to");
        File.WriteAllText(Path.Combine(from, "questions.json"), "new");
        File.WriteAllText(Path.Combine(to, "questions.json"), "old");

        Assert.Throws<IOException>(() => DataFolderService.Copy(from, to, new[] { "questions.json" }));
        Assert.Equal("old", File.ReadAllText(Path.Combine(to, "questions.json")));
    }

    [Fact]
    public void SameFolder_IgnoresCaseAndTrailingSeparator()
    {
        var folder = Folder("same");

        Assert.True(DataFolderService.SameFolder(folder, folder.ToUpperInvariant() + Path.DirectorySeparatorChar));
        Assert.False(DataFolderService.SameFolder(folder, Folder("other")));
    }
}
