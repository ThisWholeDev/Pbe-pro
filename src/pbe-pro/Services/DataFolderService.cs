using System.IO;

namespace PbePro.Services;

/// <summary>Helpers for moving the app's data between folders.</summary>
public static class DataFolderService
{
    public static readonly string[] DataFileNames =
    {
        JsonDataService.QuestionsFileName,
        JsonDataService.QuizzesFileName,
        ResultsService.FileName
    };

    /// <summary>The data files (questions.json, quizzes.json) that exist in a folder.</summary>
    public static List<string> FilesIn(string folder) =>
        DataFileNames.Where(name => File.Exists(Path.Combine(folder, name))).ToList();

    /// <summary>Copies the named files into the target folder, creating it if needed. Never overwrites.</summary>
    public static void Copy(string fromFolder, string toFolder, IEnumerable<string> fileNames)
    {
        Directory.CreateDirectory(toFolder);

        foreach (var name in fileNames)
            File.Copy(Path.Combine(fromFolder, name), Path.Combine(toFolder, name), overwrite: false);
    }

    public static bool SameFolder(string a, string b) =>
        string.Equals(Normalize(a), Normalize(b), StringComparison.OrdinalIgnoreCase);

    private static string Normalize(string path) => Path.TrimEndingDirectorySeparator(Path.GetFullPath(path));
}
