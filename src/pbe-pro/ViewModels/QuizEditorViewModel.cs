using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Windows.Data;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PbePro.Models;
using PbePro.Services;
using PbePro.Views;

namespace PbePro.ViewModels;

public partial class QuizEditorViewModel : ObservableObject
{
    private const string AllCategories = "All categories";
    private const string NoCategory = "(No category)";
    private const string AllPoints = "All points";

    private readonly JsonDataService _service = JsonDataService.CreateFromSettings();
    private readonly Dictionary<string, Question> _bank = new();
    private readonly List<string> _loadMessages = new();
    private string? _saveError;
    private string _editingName = "";

    public QuizEditorViewModel()
    {
        var questions = _service.LoadQuestions();
        foreach (var question in questions.Items)
            _bank[question.Id] = question;
        _loadMessages.AddRange(questions.Errors.Select(e => "Questions: " + e));

        var quizzes = _service.LoadQuizzes();
        _loadMessages.AddRange(quizzes.Errors.Select(e => "Quizzes: " + e));

        var dropped = PruneMissingQuestions(quizzes.Items);
        foreach (var quiz in quizzes.Items)
            Quizzes.Add(new QuizListItemViewModel(quiz, _bank));

        if (dropped.Count > 0)
        {
            _loadMessages.AddRange(dropped);
            Save();
        }

        foreach (var question in _bank.Values)
            BankItems.Add(new BankQuestionRow(question));

        BankView = CollectionViewSource.GetDefaultView(BankItems);
        BankView.Filter = MatchesBank;

        SyncOptions();
        UpdateBanner();
        UpdateBankCount();
    }

    public ObservableCollection<QuizListItemViewModel> Quizzes { get; } = new();
    public ObservableCollection<QuizQuestionRow> QuizQuestions { get; } = new();
    public ObservableCollection<BankQuestionRow> BankItems { get; } = new();
    public ICollectionView BankView { get; }

    public ObservableCollection<string> CategoryOptions { get; } = new();
    public ObservableCollection<string> PointsOptions { get; } = new();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsEditing))]
    [NotifyPropertyChangedFor(nameof(IsListView))]
    [NotifyPropertyChangedFor(nameof(HasQuizQuestions))]
    private QuizListItemViewModel? _editingQuiz;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasQuizQuestions))]
    private string _quizDetails = "";

    [ObservableProperty] private string _searchText = "";
    [ObservableProperty] private string? _selectedCategoryFilter = AllCategories;
    [ObservableProperty] private string? _selectedPointsFilter = AllPoints;
    [ObservableProperty] private string _bankCountText = "";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasBanner))]
    private string _bannerText = "";

    public bool IsEditing => EditingQuiz is not null;
    public bool IsListView => EditingQuiz is null;
    public bool HasQuizzes => Quizzes.Count > 0;
    public bool HasQuizQuestions => QuizQuestions.Count > 0;
    public bool HasBankItems => BankItems.Count > 0;
    public bool HasBanner => BannerText.Length > 0;

    public string EditingName
    {
        get => _editingName;
        set
        {
            var trimmed = (value ?? "").Trim();
            if (trimmed.Length == 0)
            {
                OnPropertyChanged();
                return;
            }

            if (!SetProperty(ref _editingName, trimmed))
                return;

            if (EditingQuiz is { } quiz && quiz.Model.Name != trimmed)
            {
                quiz.Model.Name = trimmed;
                quiz.NotifyChanged();
                Save();
            }
        }
    }

    partial void OnSearchTextChanged(string value) => RefreshBank();
    partial void OnSelectedCategoryFilterChanged(string? value) => RefreshBank();
    partial void OnSelectedPointsFilterChanged(string? value) => RefreshBank();

    // ----- List -----

    [RelayCommand]
    private void GoHome() => Navigator.GoHome();

    [RelayCommand]
    private void BackToList()
    {
        EditingQuiz = null;
        QuizQuestions.Clear();
        UpdateBanner();
        OnPropertyChanged(nameof(HasQuizzes));
    }

    [RelayCommand]
    private void NewQuiz()
    {
        var quiz = new Quiz
        {
            Id = Guid.NewGuid().ToString(),
            Name = UniqueName("Untitled quiz")
        };
        var item = new QuizListItemViewModel(quiz, _bank);
        Quizzes.Add(item);
        OnPropertyChanged(nameof(HasQuizzes));
        Save();
        OpenQuiz(item);
    }

    [RelayCommand]
    private void OpenQuiz(QuizListItemViewModel? item)
    {
        if (item is null)
            return;

        EditingQuiz = item;
        _editingName = item.Name;
        OnPropertyChanged(nameof(EditingName));
        RebuildQuizQuestions();
        MarkBankMembership();
        RefreshBank();
        UpdateQuizDetails();
        UpdateBanner();
    }

    [RelayCommand]
    private void RenameQuiz(QuizListItemViewModel? item)
    {
        if (item is null)
            return;

        var name = Dialogs.Prompt("Rename quiz", "Quiz name", item.Name, "Rename");
        if (name is null)
            return;

        item.Model.Name = name;
        item.NotifyChanged();
        if (EditingQuiz == item)
        {
            _editingName = name;
            OnPropertyChanged(nameof(EditingName));
        }

        Save();
    }

    [RelayCommand]
    private void DuplicateQuiz(QuizListItemViewModel? item)
    {
        if (item is null)
            return;

        var copy = new Quiz
        {
            Id = Guid.NewGuid().ToString(),
            Name = UniqueName("Copy of " + item.Name),
            QuestionIds = item.Model.QuestionIds.ToList()
        };
        Quizzes.Add(new QuizListItemViewModel(copy, _bank));
        OnPropertyChanged(nameof(HasQuizzes));
        Save();
    }

    [RelayCommand]
    private async Task DeleteQuizAsync(QuizListItemViewModel? item)
    {
        if (item is null)
            return;

        if (!await Dialogs.ConfirmAsync(
                "Delete this quiz?",
                $"“{item.Name}”\n\nThis can't be undone. Questions in the bank are not deleted.",
                "Delete", "Cancel", dangerous: true))
            return;

        if (EditingQuiz == item)
            BackToList();

        Quizzes.Remove(item);
        OnPropertyChanged(nameof(HasQuizzes));
        Save();
    }

    [RelayCommand]
    private void DismissBanner()
    {
        _loadMessages.Clear();
        _saveError = null;
        UpdateBanner();
    }

    // ----- Editor -----

    [RelayCommand]
    private void AddToQuiz(BankQuestionRow? row)
    {
        if (row is null || row.IsInQuiz || EditingQuiz is null)
            return;

        EditingQuiz.Model.QuestionIds.Add(row.Id);
        row.IsInQuiz = true;
        QuizQuestions.Add(CreateQuizRow(row.Model));
        RenumberQuiz();
        AfterQuizChanged();
        AddAllShownCommand.NotifyCanExecuteChanged();
    }

    [RelayCommand(CanExecute = nameof(CanAddAllShown))]
    private void AddAllShown()
    {
        if (EditingQuiz is null)
            return;

        var added = false;
        foreach (var row in BankView.Cast<BankQuestionRow>())
        {
            if (row.IsInQuiz)
                continue;

            EditingQuiz.Model.QuestionIds.Add(row.Id);
            row.IsInQuiz = true;
            QuizQuestions.Add(CreateQuizRow(row.Model));
            added = true;
        }

        if (!added)
            return;

        RenumberQuiz();
        AfterQuizChanged();
        AddAllShownCommand.NotifyCanExecuteChanged();
    }

    private bool CanAddAllShown() =>
        EditingQuiz is not null && BankView.Cast<BankQuestionRow>().Any(r => !r.IsInQuiz);

    [RelayCommand]
    private void RemoveFromQuiz(QuizQuestionRow? row)
    {
        if (row is null || EditingQuiz is null)
            return;

        EditingQuiz.Model.QuestionIds.Remove(row.Id);
        QuizQuestions.Remove(row);
        var bankRow = BankItems.FirstOrDefault(b => b.Id == row.Id);
        if (bankRow is not null)
            bankRow.IsInQuiz = false;

        RenumberQuiz();
        AfterQuizChanged();
        AddAllShownCommand.NotifyCanExecuteChanged();
    }

    [RelayCommand]
    private void MoveUp(QuizQuestionRow? row)
    {
        if (row is null || EditingQuiz is null)
            return;

        var index = QuizQuestions.IndexOf(row);
        if (index <= 0)
            return;

        QuizQuestions.Move(index, index - 1);
        var ids = EditingQuiz.Model.QuestionIds;
        (ids[index - 1], ids[index]) = (ids[index], ids[index - 1]);
        RenumberQuiz();
        AfterQuizChanged();
    }

    [RelayCommand]
    private void MoveDown(QuizQuestionRow? row)
    {
        if (row is null || EditingQuiz is null)
            return;

        var index = QuizQuestions.IndexOf(row);
        if (index < 0 || index >= QuizQuestions.Count - 1)
            return;

        QuizQuestions.Move(index, index + 1);
        var ids = EditingQuiz.Model.QuestionIds;
        (ids[index], ids[index + 1]) = (ids[index + 1], ids[index]);
        RenumberQuiz();
        AfterQuizChanged();
    }

    [RelayCommand]
    private async Task BuildRandomlyAsync()
    {
        if (EditingQuiz is null)
            return;

        var request = RandomBuildDialog.Prompt(CategoryOptions.ToList(), PointsOptions.ToList());
        if (request is null)
            return;

        var pool = QuizRandomPicker.Filter(_bank.Values, request.Category, request.Points);
        IReadOnlyCollection<string> exclude = request.Replace
            ? Array.Empty<string>()
            : EditingQuiz.Model.QuestionIds;

        var picked = QuizRandomPicker.Pick(pool, exclude, request.Count, Random.Shared);
        if (picked.Count == 0)
        {
            await Dialogs.InfoAsync("No matching questions",
                "There aren't any questions left that match those filters.");
            return;
        }

        if (request.Replace)
        {
            EditingQuiz.Model.QuestionIds.Clear();
            QuizQuestions.Clear();
            foreach (var row in BankItems)
                row.IsInQuiz = false;
        }

        foreach (var question in picked)
        {
            if (EditingQuiz.Model.QuestionIds.Contains(question.Id))
                continue;

            EditingQuiz.Model.QuestionIds.Add(question.Id);
            QuizQuestions.Add(CreateQuizRow(question));
            var bankRow = BankItems.FirstOrDefault(b => b.Id == question.Id);
            if (bankRow is not null)
                bankRow.IsInQuiz = true;
        }

        RenumberQuiz();
        AfterQuizChanged();
        AddAllShownCommand.NotifyCanExecuteChanged();

        if (picked.Count < request.Count)
        {
            await Dialogs.InfoAsync("Not enough matching questions",
                $"Added {picked.Count} of {request.Count} requested questions.");
        }
    }

    // ----- Internals -----

    private void AfterQuizChanged()
    {
        EditingQuiz?.NotifyChanged();
        UpdateQuizDetails();
        OnPropertyChanged(nameof(HasQuizQuestions));
        Save();
    }

    private void RebuildQuizQuestions()
    {
        QuizQuestions.Clear();
        if (EditingQuiz is null)
            return;

        foreach (var id in EditingQuiz.Model.QuestionIds)
        {
            if (_bank.TryGetValue(id, out var question))
                QuizQuestions.Add(CreateQuizRow(question));
        }

        RenumberQuiz();
        OnPropertyChanged(nameof(HasQuizQuestions));
    }

    private static QuizQuestionRow CreateQuizRow(Question question) => new(question);

    private void RenumberQuiz()
    {
        for (var i = 0; i < QuizQuestions.Count; i++)
        {
            var row = QuizQuestions[i];
            row.Number = i + 1;
            row.CanMoveUp = i > 0;
            row.CanMoveDown = i < QuizQuestions.Count - 1;
        }
    }

    private void MarkBankMembership()
    {
        var ids = EditingQuiz is null
            ? new HashSet<string>()
            : EditingQuiz.Model.QuestionIds.ToHashSet();

        foreach (var row in BankItems)
            row.IsInQuiz = ids.Contains(row.Id);
    }

    private void UpdateQuizDetails()
    {
        QuizDetails = EditingQuiz?.Details ?? "";
    }

    private bool MatchesBank(object obj)
    {
        if (obj is not BankQuestionRow q)
            return false;

        var search = SearchText.Trim();
        if (search.Length > 0
            && !q.Preview.Contains(search, StringComparison.OrdinalIgnoreCase)
            && !q.Answer.Contains(search, StringComparison.OrdinalIgnoreCase)
            && !(q.Category ?? "").Contains(search, StringComparison.OrdinalIgnoreCase))
            return false;

        if (SelectedCategoryFilter is { } category && category != AllCategories)
        {
            var matches = category == NoCategory
                ? string.IsNullOrWhiteSpace(q.Category)
                : string.Equals(q.Category, category, StringComparison.OrdinalIgnoreCase);
            if (!matches)
                return false;
        }

        if (SelectedPointsFilter is { } points && points != AllPoints && q.Points.ToString() != points)
            return false;

        return true;
    }

    private void RefreshBank()
    {
        BankView.Refresh();
        UpdateBankCount();
        AddAllShownCommand.NotifyCanExecuteChanged();
    }

    private void UpdateBankCount()
    {
        var shown = BankView.Cast<object>().Count();
        BankCountText = shown == BankItems.Count
            ? $"{BankItems.Count} in the bank"
            : $"Showing {shown} of {BankItems.Count}";
    }

    private void SyncOptions()
    {
        var categories = _bank.Values
            .Select(q => q.Category)
            .Where(c => !string.IsNullOrWhiteSpace(c))
            .Select(c => c!)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(c => c, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var categoryOptions = new List<string> { AllCategories };
        categoryOptions.AddRange(categories);
        if (_bank.Values.Any(q => string.IsNullOrWhiteSpace(q.Category)))
            categoryOptions.Add(NoCategory);
        Sync(CategoryOptions, categoryOptions);

        var pointsOptions = new List<string> { AllPoints };
        pointsOptions.AddRange(_bank.Values.Select(q => q.Points).Distinct().OrderBy(p => p).Select(p => p.ToString()));
        Sync(PointsOptions, pointsOptions);

        if (SelectedCategoryFilter is null || !CategoryOptions.Contains(SelectedCategoryFilter))
            SelectedCategoryFilter = AllCategories;
        if (SelectedPointsFilter is null || !PointsOptions.Contains(SelectedPointsFilter))
            SelectedPointsFilter = AllPoints;
    }

    private static void Sync(ObservableCollection<string> target, IReadOnlyList<string> desired)
    {
        for (var i = target.Count - 1; i >= 0; i--)
        {
            if (!desired.Contains(target[i]))
                target.RemoveAt(i);
        }

        for (var i = 0; i < desired.Count; i++)
        {
            if (i < target.Count && target[i] == desired[i])
                continue;
            target.Insert(i, desired[i]);
        }
    }

    private string UniqueName(string baseName)
    {
        var names = Quizzes.Select(q => q.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
        if (!names.Contains(baseName))
            return baseName;

        for (var i = 2; ; i++)
        {
            var candidate = $"{baseName} {i}";
            if (!names.Contains(candidate))
                return candidate;
        }
    }

    /// <summary>
    /// Drops ids that are no longer in the bank, and duplicate ids.
    /// Returns a notice per quiz that changed.
    /// </summary>
    private List<string> PruneMissingQuestions(IEnumerable<Quiz> quizzes)
    {
        var notices = new List<string>();

        foreach (var quiz in quizzes)
        {
            var original = quiz.QuestionIds.ToList();
            var seen = new HashSet<string>();
            quiz.QuestionIds.Clear();

            var missing = 0;
            foreach (var id in original)
            {
                if (!_bank.ContainsKey(id))
                {
                    missing++;
                    continue;
                }

                if (seen.Add(id))
                    quiz.QuestionIds.Add(id);
            }

            if (missing > 0)
            {
                notices.Add($"“{quiz.Name}”: removed {missing} question{(missing == 1 ? "" : "s")} " +
                            "that no longer exist in the bank.");
            }
        }

        return notices;
    }

    private void Save()
    {
        try
        {
            _service.SaveQuizzes(Quizzes.Select(q => q.Model));
            _saveError = null;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            _saveError = $"Your changes couldn't be saved: {ex.Message}";
        }

        UpdateBanner();
    }

    private void UpdateBanner()
    {
        var lines = new List<string>();

        if (_loadMessages.Count > 0)
        {
            lines.Add("Some data couldn't be loaded cleanly. " +
                      "Broken entries are dropped from the file the next time you change something:");
            lines.AddRange(_loadMessages.Take(8).Select(m => "• " + m));
            if (_loadMessages.Count > 8)
                lines.Add($"…and {_loadMessages.Count - 8} more.");
        }

        if (_saveError is not null)
            lines.Add(_saveError);

        BannerText = string.Join(Environment.NewLine, lines);
    }
}
