using System.Diagnostics;
using System.Media;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PbePro.Services;
using PbePro.Views;

namespace PbePro.ViewModels;

public enum QuestionPhase
{
    /// <summary>Question is on screen; the timer hasn't been started.</summary>
    Ready,

    /// <summary>The countdown is running.</summary>
    Running,

    /// <summary>The answer is showing; waiting for Correct / Incorrect.</summary>
    AnswerShown
}

public partial class QuizRunViewModel : ObservableObject
{
    private static readonly TimeSpan WarningAt = TimeSpan.FromSeconds(10);
    private static readonly TimeSpan CriticalAt = TimeSpan.FromSeconds(5);

    private readonly QuizSession _session;
    private readonly string _quizName;
    private readonly DispatcherTimer _tick = new() { Interval = TimeSpan.FromMilliseconds(100) };
    private readonly DispatcherTimer _flashTimer = new() { Interval = TimeSpan.FromMilliseconds(1900) };
    private readonly Stopwatch _questionWatch = new();
    private readonly Stopwatch _totalWatch = new();
    private TimeSpan _limit;
    private bool _expiredHandled;

    public QuizRunViewModel(QuizSession session, string quizName)
    {
        _session = session;
        _quizName = quizName;

        _tick.Tick += (_, _) => OnTick();
        _flashTimer.Tick += (_, _) =>
        {
            _flashTimer.Stop();
            IsFlashing = false;
        };

        if (_session.IsFinished)
            return; // The page sends the person home; there is nothing to run.

        _totalWatch.Start();
        LoadCurrent();
    }

    public bool IsEmpty => _session.Total == 0;
    public string QuizName => _quizName;

    public double QuestionFontSize { get; } = TextSizes.QuestionFontSize(SettingsService.Current.TextSize);
    public double AnswerFontSize { get; } = TextSizes.AnswerFontSize(SettingsService.Current.TextSize);

    [ObservableProperty] private string _progressText = "";
    [ObservableProperty] private string _scoreText = "";
    [ObservableProperty] private string _questionText = "";
    [ObservableProperty] private string _answerText = "";
    [ObservableProperty] private string _pointsText = "";
    [ObservableProperty] private string _categoryText = "";
    [ObservableProperty] private string _timerText = "0:00";

    /// <summary>0 = normal, 1 = under 10 seconds, 2 = under 5 seconds or expired.</summary>
    [ObservableProperty] private int _timerLevel;

    [ObservableProperty] private bool _isFlashing;
    [ObservableProperty] private bool _canUndo;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsReady))]
    [NotifyPropertyChangedFor(nameof(IsAnswerShown))]
    [NotifyPropertyChangedFor(nameof(ShowAnswerVisible))]
    [NotifyPropertyChangedFor(nameof(ShowAnswerButtonText))]
    private QuestionPhase _phase = QuestionPhase.Ready;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsNotPaused))]
    [NotifyPropertyChangedFor(nameof(PauseButtonText))]
    private bool _isPaused;

    public bool IsReady => Phase == QuestionPhase.Ready;
    public bool IsAnswerShown => Phase == QuestionPhase.AnswerShown;
    public bool ShowAnswerVisible => Phase != QuestionPhase.AnswerShown;
    public bool IsNotPaused => !IsPaused;
    public bool HasCategory => CategoryText.Length > 0;
    public string PauseButtonText => IsPaused ? "Resume (P)" : "Pause (P)";

    /// <summary>Space only shows the answer once the timer is running; before that it starts the timer.</summary>
    public string ShowAnswerButtonText => Phase == QuestionPhase.Running ? "Show answer (Space)" : "Show answer";

    partial void OnCategoryTextChanged(string value) => OnPropertyChanged(nameof(HasCategory));

    // ----- Commands -----

    /// <summary>The Space key: start the timer, then show the answer.</summary>
    [RelayCommand]
    private void PrimaryAction()
    {
        if (IsPaused)
            return;

        if (Phase == QuestionPhase.Ready)
            StartTimer();
        else if (Phase == QuestionPhase.Running)
            ShowAnswer();
    }

    [RelayCommand]
    private void StartTimer()
    {
        if (IsPaused || Phase != QuestionPhase.Ready)
            return;

        _questionWatch.Restart();
        _tick.Start();
        Phase = QuestionPhase.Running;
        UpdateTimerDisplay();
    }

    [RelayCommand]
    private void ShowAnswer()
    {
        if (IsPaused || Phase == QuestionPhase.AnswerShown)
            return;

        _questionWatch.Stop();
        _tick.Stop();
        Phase = QuestionPhase.AnswerShown;
        UpdateTimerDisplay();
    }

    [RelayCommand]
    private void MarkCorrect() => Mark(true);

    [RelayCommand]
    private void MarkIncorrect() => Mark(false);

    [RelayCommand]
    private void TogglePause()
    {
        if (_session.IsFinished)
            return;

        if (!IsPaused)
        {
            IsPaused = true;
            _totalWatch.Stop();
            if (Phase == QuestionPhase.Running)
            {
                _questionWatch.Stop();
                _tick.Stop();
            }
        }
        else
        {
            IsPaused = false;
            _totalWatch.Start();
            if (Phase == QuestionPhase.Running)
            {
                _questionWatch.Start();
                _tick.Start();
            }
        }
    }

    /// <summary>Goes back to the previous question with its answer showing, so it can be marked again.</summary>
    [RelayCommand]
    private void Undo()
    {
        if (IsPaused || !_session.CanUndo)
            return;

        _session.Undo();
        LoadCurrent(autoStart: false);
        Phase = QuestionPhase.AnswerShown;
    }

    [RelayCommand]
    private void ToggleFullScreen() => FullScreenService.Toggle();

    [RelayCommand]
    private async Task ExitAsync()
    {
        // Hold the clock while the dialog is open.
        var wasPaused = IsPaused;
        if (!wasPaused)
            TogglePause();

        if (_session.AnsweredCount == 0)
        {
            var leave = await Dialogs.ConfirmAsync(
                "Leave the quiz?", "No questions have been answered yet.", "Leave", "Stay", dangerous: true);
            if (!leave)
            {
                if (!wasPaused)
                    TogglePause();
                return;
            }

            Shutdown();
            Navigator.GoHome();
            return;
        }

        var choice = await Dialogs.ChooseAsync(
            "End the quiz?",
            $"{_session.AnsweredCount} of {_session.Total} questions have been answered.",
            "Show results", "Leave without results", "Keep going");

        if (choice is null)
        {
            if (!wasPaused)
                TogglePause();
            return;
        }

        if (choice == true)
        {
            Finish();
        }
        else
        {
            Shutdown();
            Navigator.GoHome();
        }
    }

    /// <summary>Stops every timer. Safe to call more than once.</summary>
    public void Shutdown()
    {
        _tick.Stop();
        _flashTimer.Stop();
        _questionWatch.Stop();
        _totalWatch.Stop();
    }

    // ----- Internals -----

    private void Mark(bool correct)
    {
        if (IsPaused || Phase != QuestionPhase.AnswerShown)
            return;

        _session.Mark(correct);

        if (_session.IsFinished)
            Finish();
        else
            LoadCurrent();
    }

    private void Finish()
    {
        Shutdown();
        var elapsed = _totalWatch.Elapsed;

        // Save to the history. If that fails, the results page says so instead of hiding it.
        string? historyNote = null;
        if (_session.AnsweredCount > 0)
        {
            historyNote = ResultsService.CreateFromSettings()
                .TryAppend(_session.ToResult(_quizName, DateTimeOffset.Now, elapsed));
        }

        Navigator.Navigate(new ResultsPage(_session, elapsed, _quizName, historyNote));
    }

    private void LoadCurrent(bool autoStart = true)
    {
        if (_session.Current is not { } question)
            return;

        _tick.Stop();
        _questionWatch.Reset();
        _expiredHandled = false;
        _limit = QuestionTiming.GetDuration(question.Points);

        QuestionText = question.Text;
        AnswerText = question.Answer;
        PointsText = $"{question.Points} point{(question.Points == 1 ? "" : "s")}";
        CategoryText = SettingsService.Current.HideCategory ? "" : question.Category ?? "";

        ProgressText = $"Question {_session.CurrentNumber} of {_session.Total}";
        ScoreText = $"Correct: {_session.CorrectCount} / {_session.AnsweredCount} answered";
        CanUndo = _session.CanUndo;

        Phase = QuestionPhase.Ready;
        UpdateTimerDisplay();

        if (autoStart && SettingsService.Current.AutoStartTimer)
            StartTimer();
    }

    private void OnTick()
    {
        UpdateTimerDisplay();

        if (!_expiredHandled && _limit - _questionWatch.Elapsed <= TimeSpan.Zero)
        {
            _expiredHandled = true;
            NotifyExpired();
        }
    }

    private void UpdateTimerDisplay()
    {
        var remaining = _limit - _questionWatch.Elapsed;
        TimerText = TimeFormat.Countdown(remaining);
        TimerLevel = remaining <= CriticalAt ? 2 : remaining <= WarningAt ? 1 : 0;
    }

    /// <summary>Time ran out. The coach decides what happens next; the timer just stays at 0:00.</summary>
    private void NotifyExpired()
    {
        var settings = SettingsService.Current;

        if (settings.SoundOnExpire)
            SystemSounds.Exclamation.Play();

        if (settings.FlashOnExpire)
        {
            _flashTimer.Stop();
            IsFlashing = true;
            _flashTimer.Start();
        }
    }
}
