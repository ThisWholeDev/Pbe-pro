using System.IO;
using System.Text.Json;
using PbePro.Models;

namespace PbePro.Services;

/// <summary>Reads and writes results.json, the history of completed quizzes.</summary>
public class ResultsService
{
    public const string FileName = "results.json";

    private readonly string _folder;

    public ResultsService(string folder) => _folder = folder;

    public static ResultsService CreateFromSettings() => new(JsonDataService.CreateFromSettings().Folder);

    private string FilePath => Path.Combine(_folder, FileName);

    public LoadResult<QuizResult> Load()
    {
        var result = new LoadResult<QuizResult>();
        if (!File.Exists(FilePath))
            return result;

        ResultsFile? file;
        try
        {
            file = JsonSerializer.Deserialize<ResultsFile>(File.ReadAllText(FilePath), JsonDataService.SerializerOptions);
        }
        catch (JsonException ex)
        {
            result.Errors.Add($"{FileName} is not valid JSON and was not loaded: {ex.Message}");
            return result;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            result.Errors.Add($"{FileName} could not be read: {ex.Message}");
            return result;
        }

        var seenIds = new HashSet<string>();
        foreach (var entry in file?.Results ?? new List<QuizResult>())
        {
            if (entry is null)
            {
                result.Errors.Add("An empty result entry was skipped.");
                continue;
            }

            if (string.IsNullOrWhiteSpace(entry.Id))
            {
                result.Errors.Add("A saved result has no id and was skipped.");
                continue;
            }

            if (!seenIds.Add(entry.Id))
            {
                result.Errors.Add($"Result id '{entry.Id}' appears more than once; the duplicate was skipped.");
                continue;
            }

            entry.Outcomes ??= new List<QuestionOutcome>();
            if (string.IsNullOrWhiteSpace(entry.QuizName))
                entry.QuizName = "Quiz";

            result.Items.Add(entry);
        }

        return result;
    }

    public void Save(IEnumerable<QuizResult> results)
    {
        Directory.CreateDirectory(_folder);
        File.WriteAllText(
            FilePath,
            JsonSerializer.Serialize(new ResultsFile { Results = results.ToList() }, JsonDataService.SerializerOptions));
    }

    /// <summary>
    /// Adds a result to the history. Returns null on success, or a message if it couldn't be saved.
    /// An unreadable results.json is left alone rather than overwritten.
    /// </summary>
    public string? TryAppend(QuizResult result)
    {
        try
        {
            var loaded = Load();
            if (loaded.HasErrors && loaded.Items.Count == 0 && File.Exists(FilePath))
                return $"{FileName} couldn’t be read, so this result wasn’t saved to your history.";

            var all = loaded.Items;
            all.Add(result);
            Save(all);
            return null;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return $"This result couldn’t be saved to your history: {ex.Message}";
        }
    }

    private sealed class ResultsFile
    {
        public List<QuizResult> Results { get; set; } = new();
    }
}
