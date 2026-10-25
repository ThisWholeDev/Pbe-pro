using System.Collections.ObjectModel;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PbePro.Models;
using PbePro.Services;
using PbePro.Views;

namespace PbePro.ViewModels;

public partial class HistoryViewModel : ObservableObject
{
    private readonly ResultsService _service = ResultsService.CreateFromSettings();
    private readonly List<QuizResult> _results;
    private readonly List<Question> _bank;
    private List<Question> _missed = new();

    public HistoryViewModel()
    {
        var loaded = _service.Load();
        _results = loaded.Items;
        _bank = JsonDataService.CreateFromSettings().LoadQuestions().Items;

        if (loaded.HasErrors)
        {
            NoticeText = "Some saved results couldn't be read and were skipped:" + Environment.NewLine +
                         string.Join(Environment.NewLine, loaded.Errors.Take(5).Select(e => "• " + e));
        }

        Rebuild();
    }

    public ObservableCollection<HistoryItemViewModel> Items { get; } = new();
    public ObservableCollection<CategoryStat> Categories { get; } = new();

    [ObservableProperty] private string _summaryText = "";
    [ObservableProperty] private string _practiceText = "";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasNotice))]
    private string _noticeText = "";

    public bool HasNotice => NoticeText.Length > 0;
    public bool HasItems => Items.Count > 0;
    public bool HasNoItems => Items.Count == 0;
    public bool HasMissed => _missed.Count > 0;

    [RelayCommand]
    private void GoHome() => Navigator.GoHome();

    [RelayCommand]
    private void PracticeMissed()
    {
        if (_missed.Count == 0)
            return;

        var questions = SettingsService.Current.ShuffleQuestions
            ? MissedPractice.Shuffled(_missed)
            : _missed.ToList();

        Navigator.Navigate(new QuizIntroPage(new QuizSession(questions), "Missed questions"));
    }

    [RelayCommand]
    private async Task DeleteResultAsync(HistoryItemViewModel? item)
    {
        if (item is null)
            return;

        var confirmed = await Dialogs.ConfirmAsync(
            "Delete this result?",
            $"“{item.Title}”\n{item.When}\n\nThis can't be undone.",
            "Delete", "Cancel", dangerous: true);
        if (!confirmed)
            return;

        _results.Remove(item.Result);
        Save();
        Rebuild();
    }

    [RelayCommand]
    private async Task ClearHistoryAsync()
    {
        if (_results.Count == 0)
            return;

        var confirmed = await Dialogs.ConfirmAsync(
            "Clear all history?",
            $"This removes all {_results.Count} saved results.\n\nYour questions and quizzes aren't affected.",
            "Clear history", "Cancel", dangerous: true);
        if (!confirmed)
            return;

        _results.Clear();
        Save();
        Rebuild();
    }

    private void Save()
    {
        try
        {
            _service.Save(_results);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            NoticeText = $"Your changes couldn't be saved: {ex.Message}";
        }
    }

    private void Rebuild()
    {
        Items.Clear();
        foreach (var result in _results.OrderByDescending(r => r.CompletedAt))
            Items.Add(new HistoryItemViewModel(result));

        Categories.Clear();
        foreach (var stat in PracticeStats.ByCategory(_results.SelectMany(r => r.Outcomes)))
            Categories.Add(stat);

        var answered = _results.Sum(r => r.AnsweredCount);
        var correct = _results.Sum(r => r.CorrectCount);
        SummaryText = _results.Count == 0
            ? ""
            : $"{_results.Count} quiz{(_results.Count == 1 ? "" : "zes")} taken · " +
              $"{Math.Round(100.0 * correct / Math.Max(answered, 1))}% correct overall";

        _missed = MissedPractice.Build(_bank, _results);
        PracticeText = $"Practice missed questions ({_missed.Count})";

        OnPropertyChanged(nameof(HasItems));
        OnPropertyChanged(nameof(HasNoItems));
        OnPropertyChanged(nameof(HasMissed));
    }
}
