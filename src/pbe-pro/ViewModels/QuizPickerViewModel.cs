using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PbePro.Models;
using PbePro.Services;
using PbePro.Views;

namespace PbePro.ViewModels;

public partial class QuizPickerViewModel : ObservableObject
{
    private readonly Dictionary<string, Question> _bank = new();

    public QuizPickerViewModel()
    {
        var service = JsonDataService.CreateFromSettings();

        foreach (var question in service.LoadQuestions().Items)
            _bank[question.Id] = question;

        foreach (var quiz in service.LoadQuizzes().Items)
            Quizzes.Add(new QuizListItemViewModel(quiz, _bank));

        _missedQuestions = MissedPractice.Build(_bank.Values, ResultsService.CreateFromSettings().Load().Items);
    }

    private readonly List<Question> _missedQuestions;

    public bool HasMissed => _missedQuestions.Count > 0;

    public string MissedText =>
        $"{_missedQuestions.Count} question{(_missedQuestions.Count == 1 ? "" : "s")} you missed last time. " +
        "Get one right and it drops off the list.";

    [RelayCommand]
    private void PracticeMissed()
    {
        if (_missedQuestions.Count == 0)
            return;

        var questions = Shuffle ? MissedPractice.Shuffled(_missedQuestions) : _missedQuestions.ToList();
        Navigator.Navigate(new QuizIntroPage(new QuizSession(questions), "Missed questions"));
    }

    public ObservableCollection<QuizListItemViewModel> Quizzes { get; } = new();

    public bool HasQuizzes => Quizzes.Count > 0;
    public bool HasNoQuizzes => Quizzes.Count == 0;

    [ObservableProperty] private bool _shuffle = SettingsService.Current.ShuffleQuestions;
    [ObservableProperty] private bool _autoStartTimer = SettingsService.Current.AutoStartTimer;

    [ObservableProperty] private bool _hideCategory = SettingsService.Current.HideCategory;

    // Remembered for next time.
    partial void OnHideCategoryChanged(bool value)
    {
        SettingsService.Current.HideCategory = value;
        SettingsService.Save();
    }

    partial void OnShuffleChanged(bool value)
    {
        SettingsService.Current.ShuffleQuestions = value;
        SettingsService.Save();
    }

    partial void OnAutoStartTimerChanged(bool value)
    {
        SettingsService.Current.AutoStartTimer = value;
        SettingsService.Save();
    }

    [RelayCommand]
    private void GoHome() => Navigator.GoHome();

    [RelayCommand]
    private async Task StartAsync(QuizListItemViewModel? item)
    {
        if (item is null)
            return;

        // Questions deleted from the bank since the quiz was built are skipped.
        var questions = item.Model.QuestionIds
            .Distinct()
            .Where(_bank.ContainsKey)
            .Select(id => _bank[id])
            .ToList();

        if (questions.Count == 0)
        {
            await Dialogs.InfoAsync("Nothing to run",
                "This quiz has no questions yet. Add some under Edit Quizzes.");
            return;
        }

        if (Shuffle)
            questions = QuizRandomPicker.Pick(questions, Array.Empty<string>(), questions.Count, Random.Shared).ToList();

        Navigator.Navigate(new QuizIntroPage(new QuizSession(questions), item.Name));
    }
}
