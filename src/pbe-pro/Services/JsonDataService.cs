using System.IO;
using System.Text.Json;
using PbePro.Models;

namespace PbePro.Services;

/// <summary>Reads and writes questions.json and quizzes.json in the data folder.</summary>
public class JsonDataService
{
    public const string QuestionsFileName = "questions.json";
    public const string QuizzesFileName = "quizzes.json";

    internal static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public string Folder { get; }

    public JsonDataService(string folder) => Folder = folder;

    public static string DefaultFolder =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "PBE Pro");

    /// <summary>Uses the data folder from settings, or the default Documents folder.</summary>
    public static JsonDataService CreateFromSettings() =>
        new(string.IsNullOrWhiteSpace(SettingsService.Current.DataFolder)
            ? DefaultFolder
            : SettingsService.Current.DataFolder);

    // ----- Questions -----

    public LoadResult<Question> LoadQuestions()
    {
        var result = new LoadResult<Question>();
        var file = ReadFile<QuestionFile>(QuestionsFileName, result.Errors);
        var seenIds = new HashSet<string>();

        foreach (var q in file?.Questions ?? new List<Question>())
        {
            if (q is null)
            {
                result.Errors.Add("An empty question entry was skipped.");
                continue;
            }

            if (string.IsNullOrWhiteSpace(q.Id))
            {
                result.Errors.Add($"A question has no id and was skipped: \"{Preview(q.Text)}\"");
                continue;
            }

            var problems = QuestionValidator.Validate(q);
            if (problems.Count > 0)
            {
                result.Errors.Add($"Question '{q.Id}' was skipped. {string.Join(" ", problems)}");
                continue;
            }

            if (!seenIds.Add(q.Id))
            {
                result.Errors.Add($"Question id '{q.Id}' appears more than once; the duplicate was skipped.");
                continue;
            }

            result.Items.Add(q);
        }

        return result;
    }

    public void SaveQuestions(IEnumerable<Question> questions) =>
        WriteFile(QuestionsFileName, new QuestionFile { Questions = questions.ToList() });

    /// <summary>Writes questions to any path in the same { "questions": [...] } format the importer reads.</summary>
    public static void ExportQuestions(string path, IEnumerable<Question> questions)
    {
        var directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(directory))
            Directory.CreateDirectory(directory);

        File.WriteAllText(path,
            JsonSerializer.Serialize(new QuestionFile { Questions = questions.ToList() }, SerializerOptions));
    }

    // ----- Quizzes -----

    public LoadResult<Quiz> LoadQuizzes()
    {
        var result = new LoadResult<Quiz>();
        var file = ReadFile<QuizFile>(QuizzesFileName, result.Errors);
        var seenIds = new HashSet<string>();

        foreach (var quiz in file?.Quizzes ?? new List<Quiz>())
        {
            if (quiz is null)
            {
                result.Errors.Add("An empty quiz entry was skipped.");
                continue;
            }

            if (string.IsNullOrWhiteSpace(quiz.Id))
            {
                result.Errors.Add($"A quiz has no id and was skipped: \"{Preview(quiz.Name)}\"");
                continue;
            }

            if (string.IsNullOrWhiteSpace(quiz.Name))
            {
                result.Errors.Add($"Quiz '{quiz.Id}' was skipped because it has no name.");
                continue;
            }

            if (!seenIds.Add(quiz.Id))
            {
                result.Errors.Add($"Quiz id '{quiz.Id}' appears more than once; the duplicate was skipped.");
                continue;
            }

            quiz.QuestionIds ??= new List<string>();
            result.Items.Add(quiz);
        }

        return result;
    }

    public void SaveQuizzes(IEnumerable<Quiz> quizzes) =>
        WriteFile(QuizzesFileName, new QuizFile { Quizzes = quizzes.ToList() });

    // ----- Helpers -----

    private T? ReadFile<T>(string fileName, List<string> errors) where T : class
    {
        var path = Path.Combine(Folder, fileName);
        if (!File.Exists(path))
            return null; // First run: nothing saved yet.

        try
        {
            return JsonSerializer.Deserialize<T>(File.ReadAllText(path), SerializerOptions);
        }
        catch (JsonException ex)
        {
            errors.Add($"{fileName} is not valid JSON and was not loaded: {ex.Message}");
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            errors.Add($"{fileName} could not be read: {ex.Message}");
        }

        return null;
    }

    private void WriteFile<T>(string fileName, T content)
    {
        Directory.CreateDirectory(Folder);
        File.WriteAllText(Path.Combine(Folder, fileName), JsonSerializer.Serialize(content, SerializerOptions));
    }

    private static string Preview(string? text)
    {
        text = text?.Trim() ?? "";
        return text.Length <= 40 ? text : text[..40] + "...";
    }

    private sealed class QuestionFile
    {
        public List<Question> Questions { get; set; } = new();
    }

    private sealed class QuizFile
    {
        public List<Quiz> Quizzes { get; set; } = new();
    }
}
