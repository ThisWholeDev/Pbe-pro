using PbePro.Models;
using PbePro.Services;

namespace PbePro.ViewModels;

public class HistoryItemViewModel
{
    public HistoryItemViewModel(QuizResult result) => Result = result;

    public QuizResult Result { get; }

    public string Title => Result.QuizName;

    public string When => Result.CompletedAt.ToLocalTime().ToString("MMM d, yyyy  h:mm tt");

    public string Details
    {
        get
        {
            var percent = Result.AnsweredCount == 0 ? "—" : $"{Math.Round(Result.Percent)}%";
            var partial = Result.PartialCount > 0 ? $", {Result.PartialCount} partial" : "";
            var text = $"{Result.CorrectCount} / {Result.AnsweredCount} correct ({percent}){partial} · " +
                       $"{Result.PointsEarned} / {Result.PointsAnswered} pts · " +
                       TimeFormat.Elapsed(TimeSpan.FromSeconds(Result.DurationSeconds));

            return Result.EndedEarly ? text + " · ended early" : text;
        }
    }
}
