using System.Diagnostics;
using System.Net.Http;
using System.Reflection;
using System.Text.Json;

namespace PbePro.Services;

public static class UpdateChecker
{
    private const string Owner = "ThisWholeDev";
    private const string Repo = "Pbe-pro";

    private static readonly HttpClient Http = CreateClient();

    private static Task<UpdateInfo?>? _cached;

    /// <summary>True once the user has closed the update banner this session.</summary>
    public static bool Dismissed { get; set; }

    public record UpdateInfo(Version LatestVersion, string TagName, string Url, string Notes);

    /// <summary>
    /// Runs the GitHub check once per app session and returns the same result
    /// on later calls, so navigating back to the start page doesn't re-hit the API.
    /// </summary>
    public static Task<UpdateInfo?> GetCachedAsync() => _cached ??= CheckForUpdateAsync();

    public static async Task<UpdateInfo?> CheckForUpdateAsync()
    {
        try
        {
            var url = $"https://api.github.com/repos/{Owner}/{Repo}/releases/latest";
            var json = await Http.GetStringAsync(url).ConfigureAwait(false);

            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            var tag = root.GetProperty("tag_name").GetString() ?? "";
            var htmlUrl = root.GetProperty("html_url").GetString() ?? "";
            var notes = root.TryGetProperty("body", out var body) ? body.GetString() ?? "" : "";

            if (!TryParseTag(tag, out var latest))
                return null;

            var current = Normalize(Assembly.GetExecutingAssembly().GetName().Version ?? new Version(0, 0, 0));

            return latest > current
                ? new UpdateInfo(latest, tag, htmlUrl, notes)
                : null;
        }
        catch (Exception ex)
        {
            // No internet, rate limited, repo has no releases, etc. Fail silently for the user.
            Debug.WriteLine($"Update check failed: {ex.Message}");
            return null;
        }
    }

    private static HttpClient CreateClient()
    {
        var client = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
        // GitHub's API rejects requests without a User-Agent.
        client.DefaultRequestHeaders.UserAgent.ParseAdd($"{Repo}-UpdateChecker");
        client.DefaultRequestHeaders.Accept.ParseAdd("application/vnd.github+json");
        return client;
    }

    /// <summary>Parses tags like "v1.2.3" or "1.2.3-beta" into a 3-part Version.</summary>
    private static bool TryParseTag(string tag, out Version version)
    {
        var text = tag.Trim().TrimStart('v', 'V');

        // Drop any pre-release/build suffix: "1.2.3-beta.1" -> "1.2.3"
        var suffixIndex = text.IndexOfAny(['-', '+']);
        if (suffixIndex >= 0)
            text = text[..suffixIndex];

        if (Version.TryParse(text, out var parsed))
        {
            version = Normalize(parsed);
            return true;
        }

        version = new Version(0, 0, 0);
        return false;
    }

    /// <summary>
    /// Reduces a version to Major.Minor.Build so that 1.0.0 (a tag) and
    /// 1.0.0.0 (the assembly version) compare as equal.
    /// </summary>
    private static Version Normalize(Version v) =>
        new(v.Major, Math.Max(v.Minor, 0), Math.Max(v.Build, 0));
}