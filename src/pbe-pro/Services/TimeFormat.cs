namespace PbePro.Services;

public static class TimeFormat
{
    /// <summary>
    /// Countdown text such as 0:30. Rounds up, so the display only reads 0:00 once time has truly run out.
    /// </summary>
    public static string Countdown(TimeSpan remaining)
    {
        if (remaining < TimeSpan.Zero)
            remaining = TimeSpan.Zero;

        var total = (int)Math.Ceiling(remaining.TotalSeconds);
        return $"{total / 60}:{total % 60:00}";
    }

    /// <summary>Elapsed time such as 6:42, or 1:02:05 once it passes an hour. Rounds down.</summary>
    public static string Elapsed(TimeSpan elapsed)
    {
        var total = (int)Math.Max(0, elapsed.TotalSeconds);
        var hours = total / 3600;
        var minutes = total % 3600 / 60;
        var seconds = total % 60;

        return hours > 0
            ? $"{hours}:{minutes:00}:{seconds:00}"
            : $"{minutes}:{seconds:00}";
    }
}
