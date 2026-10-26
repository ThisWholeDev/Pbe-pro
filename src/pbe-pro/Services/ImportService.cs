using System.Globalization;
using System.IO;
using CsvHelper;
using CsvHelper.Configuration;
using PbePro.Models;

namespace PbePro.Services;

public class ImportResult
{
    public List<Question> Added { get; } = new();
    public List<string> Errors { get; } = new();
    public int DuplicatesSkipped { get; set; }
}

/// <summary>Imports questions from a CSV file without touching the saved question bank.</summary>
public static class ImportService
{
    /// <summary>These headers must be present (any order, any casing).</summary>
    public static readonly string[] RequiredHeaders = { "Question", "Answer", "Points" };

    /// <summary>These columns may be left out of the file entirely, or left blank in any row.</summary>
    public static readonly string[] OptionalHeaders = { "Id", "Category" };

    /// <summary>All columns, in the order the exporter writes them.</summary>
    public static readonly string[] AllHeaders = { "Id", "Question", "Answer", "Points", "Category" };

    private sealed record RawRow(string? Id, string? Text, string? Answer, string? Points, string? Category);

    private static string NormalizeHeader(string? header) => (header ?? "").Trim().ToLowerInvariant();

    /// <summary>
    /// Reads a CSV with a mandatory header row (Question, Answer, Points); Id and Category are optional. Rows with problems are
    /// reported and skipped; duplicates (same id or same question text) are counted and skipped.
    /// </summary>
    public static ImportResult ImportFromFile(string path, IReadOnlyCollection<Question> existing)
    {
        var result = new ImportResult();

        var config = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = true,
            PrepareHeaderForMatch = args => NormalizeHeader(args.Header),
            MissingFieldFound = null, // short rows yield null fields, which validation then reports
            HeaderValidated = null,   // headers are checked below so every missing one is reported at once
        };

        var rows = new List<RawRow>();
        try
        {
            using var reader = new StreamReader(path);
            using var csv = new CsvReader(reader, config);

            if (!csv.Read() || !csv.ReadHeader() || csv.HeaderRecord is null)
            {
                result.Errors.Add("The file is empty. It must start with a header row containing: "
                                  + string.Join(", ", RequiredHeaders) + ".");
                return result;
            }

            var present = new HashSet<string>(csv.HeaderRecord.Select(NormalizeHeader));
            var missing = RequiredHeaders.Where(h => !present.Contains(NormalizeHeader(h))).ToList();
            if (missing.Count > 0)
            {
                result.Errors.Add($"The file is missing required header(s): {string.Join(", ", missing)}. "
                                  + $"Required headers: {string.Join(", ", RequiredHeaders)}. "
                                  + $"Optional headers: {string.Join(", ", OptionalHeaders)}.");
                return result;
            }

            // Optional columns that are not in the file read as null.
            string? Field(string name) => present.Contains(NormalizeHeader(name)) ? csv.GetField(name) : null;

            while (csv.Read())
            {
                rows.Add(new RawRow(
                    Field("Id"),
                    Field("Question"),
                    Field("Answer"),
                    Field("Points"),
                    Field("Category")));
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            result.Errors.Add($"The file could not be read: {ex.Message}");
            return result;
        }
        catch (CsvHelperException ex)
        {
            result.Errors.Add($"The file is not valid question CSV: {ex.Message}");
            return result;
        }

        var knownIds = new HashSet<string>(existing.Select(q => q.Id));
        var knownTexts = new HashSet<string>(
            existing.Select(q => Normalize(q.Text)), StringComparer.OrdinalIgnoreCase);

        var number = 0;
        foreach (var row in rows)
        {
            number++;

            if (!int.TryParse(Normalize(row.Points), NumberStyles.Integer, CultureInfo.InvariantCulture, out var points))
            {
                result.Errors.Add($"Entry {number} was skipped. Points must be a whole number.");
                continue;
            }

            var q = new Question
            {
                Id = Normalize(row.Id),
                Text = Normalize(row.Text),
                Answer = Normalize(row.Answer),
                Points = points,
                Category = string.IsNullOrWhiteSpace(row.Category) ? null : row.Category.Trim(),
            };

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
