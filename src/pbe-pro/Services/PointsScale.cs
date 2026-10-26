namespace PbePro.Services;

/// <summary>One button on the marking scale: how many points it awards and how it should look.</summary>
/// <param name="Kind">"None" (0 points), "Partial", or "Full" (every point). The screen colours buttons by this.</param>
public sealed record ScaleOption(int Points, string Caption, string Kind);

public static class PointsScale
{
    /// <summary>
    /// The scale shown for a question worth more than one point: 0 (incorrect), each partial amount, then
    /// full credit. One-point questions return an empty list; they keep the simple Incorrect / Correct pair.
    /// </summary>
    public static List<ScaleOption> Build(int points)
    {
        var options = new List<ScaleOption>();
        if (points <= 1)
            return options;

        options.Add(new ScaleOption(0, "Incorrect (X)", "None"));

        for (var p = 1; p < points; p++)
            options.Add(new ScaleOption(p, "", "Partial"));

        options.Add(new ScaleOption(points, "Correct (C)", "Full"));
        return options;
    }

    /// <summary>The line above the scale, including which keys work (number keys stop at 9).</summary>
    public static string Caption(int points)
    {
        var top = Math.Min(points - 1, 9);
        var keys = top <= 1 ? "1" : $"1–{top}";
        return $"Points awarded (out of {points}). Press {keys}, C or X";
    }
}
