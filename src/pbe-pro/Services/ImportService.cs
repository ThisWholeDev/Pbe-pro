using System.IO;
using System.Text.Json;
using PbePro.Models;

namespace PbePro.Services;

public class ImportResult
{
    public List<Question> Added { get; } = new();
    public List<string> Errors { get; } = new();
    public int DuplicatesSkipped { get; set; }
}

/// <summary>Imports questions from a JSON file without touching the saved question bank.</summary>
public static class ImportService
{
    /// <summary>
    /// Accepts either { "questions": [ ... ] } or a plain [ ... ] list. Entries with problems are reported and
    /// skipped; duplicates (same id or same question text) are counted and skipped.
    /// </summary>
    public static ImportResult ImportFromFile(string path, IReadOnlyCollection<Question> existing)
    {
        var result = new ImportResult();

        string json;
        try
        {
            json = File.ReadAllText(path);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            result.Errors.Add($"The file could not be read: {ex.Message}");
            return result;
        }

        List<Question?>? incoming;
        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            JsonElement list;
            if (root.ValueKind == JsonValueKind.Array)
            {
                list = root;
            }
            else if (root.ValueKind == JsonValueKind.Object
                     && root.TryGetProperty("questions", out var prop)
                     && prop.ValueKind == JsonValueKind.Array)
            {
                list = prop;
            }
            else
            {
                result.Errors.Add("The file must contain a \"questions\" list or a plain list of questions.");
                return result;
            }

            incoming = list.Deserialize<List<Question?>>(JsonDataService.SerializerOptions);
        }
        catch (JsonException ex)
        {
            result.Errors.Add($"The file is not valid question JSON: {ex.Message}");
            return result;
        }

        var knownIds = new HashSet<string>(existing.Select(q => q.Id));
        var knownTexts = new HashSet<string>(
            existing.Select(q => Normalize(q.Text)), StringComparer.OrdinalIgnoreCase);

        var number = 0;
        foreach (var q in incoming ?? new List<Question?>())
        {
            number++;

            if (q is null)
            {
                result.Errors.Add($"Entry {number} is empty and was skipped.");
                continue;
            }

            q.Text = Normalize(q.Text);
            q.Answer = Normalize(q.Answer);
            q.Category = string.IsNullOrWhiteSpace(q.Category) ? null : q.Category.Trim();

            var problems = QuestionValidator.Validate(q);
            if (problems.Count > 0)
            {
                result.Errors.Add($"Entry {number} was skipped. {string.Join(" ", problems)}");
                continue;
            }

            var hasId = !string.IsNullOrWhiteSpace(q.Id);
            if ((hasId && knownIds.Contains(q.Id)) || knownTexts.Contains(q.Text))
            {
                result.DuplicatesSkipped++;
                continue;
            }

            if (!hasId)
                q.Id = Guid.NewGuid().ToString();

            knownIds.Add(q.Id);
            knownTexts.Add(q.Text);
            result.Added.Add(q);
        }

        return result;
    }

    private static string Normalize(string? text) => text?.Trim() ?? "";
}
