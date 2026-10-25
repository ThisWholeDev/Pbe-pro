using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Windows.Data;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PbePro.Models;
using PbePro.Services;

namespace PbePro.ViewModels;

public partial class QuestionEditorViewModel : ObservableObject
{
    private const string AllCategories = "All categories";
    private const string NoCategory = "(No category)";
    private const string AllPoints = "All points";

    private readonly JsonDataService _service = JsonDataService.CreateFromSettings();
    private readonly List<Quiz> _quizzes;
    private readonly List<string> _loadMessages = [];
    private string? _saveError;

    public QuestionEditorViewModel()
    {
        var loaded = _service.LoadQuestions();
        foreach (var question in loaded.Items)
            AddItem(question);
        _loadMessages.AddRange(loaded.Errors);

        // Used only to warn before deleting a question that quizzes rely on.
        _quizzes = _service.LoadQuizzes().Items;

        QuestionsView = CollectionViewSource.GetDefaultView(Items);
        QuestionsView.Filter = Matches;

        SyncOptions();
        UpdateBanner();
        UpdateCount();
        SelectedQuestion = Items.FirstOrDefault();
    }

    /// <summary>Raised after a new question is added so the page can focus the text box.</summary>
    public event Action? QuestionAdded;

    public ObservableCollection<QuestionItemViewModel> Items { get; } = new();
    public ICollectionView QuestionsView { get; }

    public ObservableCollection<string> CategorySuggestions { get; } = new();
    public ObservableCollection<string> CategoryOptions { get; } = new();
    public ObservableCollection<string> PointsOptions { get; } = new();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasSelection))]
    private QuestionItemViewModel? _selectedQuestion;

    [ObservableProperty] private string _searchText = "";
    [ObservableProperty] private string? _selectedCategoryFilter = AllCategories;
    [ObservableProperty] private string? _selectedPointsFilter = AllPoints;
    [ObservableProperty] private string _countText = "";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasBanner))]
    private string _bannerText = "";

    public bool HasSelection => SelectedQuestion is not null;
    public bool HasBanner => BannerText.Length > 0;

    partial void OnSearchTextChanged(string value) => RefreshView();
    partial void OnSelectedCategoryFilterChanged(string? value) => RefreshView();
    partial void OnSelectedPointsFilterChanged(string? value) => RefreshView();

    // ----- Commands -----

    [RelayCommand]
    private void GoHome() => Navigator.GoHome();

    [RelayCommand]
    private void AddQuestion()
    {
        // Make sure the new question isn't hidden by an active search or filter.
        SearchText = "";
        SelectedCategoryFilter = AllCategories;
        SelectedPointsFilter = AllPoints;

        // New questions start with the same points and category as the one being edited,
        // which makes entering a run of similar questions quick.
        var question = new Question
        {
            Id = Guid.NewGuid().ToString(),
            Points = SelectedQuestion?.Points ?? 1,
            Category = SelectedQuestion?.Category
        };

        var item = AddItem(question);
        SyncOptions();
        RefreshView();
        SelectedQuestion = item;
        QuestionAdded?.Invoke();
    }

    [RelayCommand]
    private async Task DeleteQuestionAsync()
    {
        if (SelectedQuestion is not { } item)
            return;

        var isBlank = string.IsNullOrWhiteSpace(item.Text) && string.IsNullOrWhiteSpace(item.Answer);
        if (!isBlank)
        {
            var message = $"“{item.Preview}”\n\nThis can't be undone.";

            var usedIn = _quizzes
                .Where(z => z.QuestionIds.Contains(item.Id))
                .Select(z => z.Name)
                .ToList();
            if (usedIn.Count > 0)
                message += $"\n\nIt's used in: {string.Join(", ", usedIn)}. Those quizzes will no longer include it.";

            if (!await Dialogs.ConfirmAsync("Delete this question?", message, "Delete", "Cancel", dangerous: true))
                return;
        }

        var index = Items.IndexOf(item);
        item.PropertyChanged -= OnItemChanged;
        Items.Remove(item);

        Save();
        SyncOptions();
        RefreshView();

        var visible = QuestionsView.Cast<QuestionItemViewModel>().ToList();
        SelectedQuestion = visible.Count == 0 ? null : visible[Math.Min(index, visible.Count - 1)];
    }

    [RelayCommand]
    private async Task ImportAsync()
    {
        var path = Dialogs.PickOpenFile("Import questions", "JSON files (*.json)|*.json|All files (*.*)|*.*");
        if (path is null)
            return;

        var result = ImportService.ImportFromFile(path, Items.Select(i => i.Model).ToList());

        foreach (var question in result.Added)
            AddItem(question);

        if (result.Added.Count > 0)
        {
            Save();
            SyncOptions();
            RefreshView();
        }

        await Dialogs.InfoAsync("Import finished", BuildImportSummary(result));
    }

    [RelayCommand]
    private async Task ExportAsync()
    {
        var all = Items.Where(i => !i.IsIncomplete).Select(i => i.Model).ToList();
        var shown = QuestionsView.Cast<QuestionItemViewModel>()
            .Where(i => !i.IsIncomplete).Select(i => i.Model).ToList();

        var toExport = all;
        if (shown.Count != all.Count)
        {
            var choice = await Dialogs.ChooseAsync("Export questions",
                $"{shown.Count} of {all.Count} questions are currently shown. What would you like to export?",
                "Shown only", "All questions");
            if (choice is null)
                return;
            if (choice == true)
                toExport = shown;
        }

        if (toExport.Count == 0)
        {
            await Dialogs.InfoAsync("Nothing to export", "There are no complete questions to export.");
            return;
        }

        var path = Dialogs.PickSaveFile("Export questions", "JSON files (*.json)|*.json", "questions-export.json");
        if (path is null)
            return;

        try
        {
            JsonDataService.ExportQuestions(path, toExport);
            await Dialogs.InfoAsync("Export finished", $"Exported {toExport.Count} questions to:\n{path}");
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            await Dialogs.InfoAsync("Export failed", ex.Message);
        }
    }

    [RelayCommand]
    private void DismissBanner()
    {
        _loadMessages.Clear();
        _saveError = null;
        UpdateBanner();
    }

    // ----- Internals -----

    private QuestionItemViewModel AddItem(Question question)
    {
        var item = new QuestionItemViewModel(question);
        item.PropertyChanged += OnItemChanged;
        Items.Add(item);
        return item;
    }

    private void OnItemChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(QuestionItemViewModel.Text)
            or nameof(QuestionItemViewModel.Answer)
            or nameof(QuestionItemViewModel.Points)
            or nameof(QuestionItemViewModel.Category))
        {
            Save();
            SyncOptions();
            RefreshView();
        }
    }

    /// <summary>Saves every complete question. Incomplete ones stay on screen but are not written to disk.</summary>
    private void Save()
    {
        try
        {
            _service.SaveQuestions(Items.Where(i => !i.IsIncomplete).Select(i => i.Model));
            _saveError = null;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            _saveError = $"Your changes couldn't be saved: {ex.Message}";
        }

        UpdateBanner();
    }

    private bool Matches(object obj)
    {
        if (obj is not QuestionItemViewModel q)
            return false;

        var search = SearchText.Trim();
        if (search.Length > 0
            && !q.Text.Contains(search, StringComparison.OrdinalIgnoreCase)
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

    private void RefreshView()
    {
        QuestionsView.Refresh();
        UpdateCount();
    }

    private void UpdateCount()
    {
        var shown = QuestionsView.Cast<object>().Count();
        CountText = shown == Items.Count
            ? $"{Items.Count} questions"
            : $"Showing {shown} of {Items.Count} questions";
    }

    private void UpdateBanner()
    {
        var lines = new List<string>();

        if (_loadMessages.Count > 0)
        {
            lines.Add("Some entries in questions.json couldn't be loaded. " +
                      "They will be dropped from the file the next time you change something:");
            lines.AddRange(_loadMessages.Take(8).Select(m => "• " + m));
            if (_loadMessages.Count > 8)
                lines.Add($"…and {_loadMessages.Count - 8} more.");
        }

        if (_saveError is not null)
            lines.Add(_saveError);

        BannerText = string.Join(Environment.NewLine, lines);
    }

    private void SyncOptions()
    {
        var categories = Items
            .Select(i => i.Category)
            .Where(c => !string.IsNullOrWhiteSpace(c))
            .Select(c => c!)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(c => c, StringComparer.OrdinalIgnoreCase)
            .ToList();

        Sync(CategorySuggestions, categories);

        var categoryOptions = new List<string> { AllCategories };
        categoryOptions.AddRange(categories);
        if (Items.Any(i => string.IsNullOrWhiteSpace(i.Category)))
            categoryOptions.Add(NoCategory);
        Sync(CategoryOptions, categoryOptions);

        var pointsOptions = new List<string> { AllPoints };
        pointsOptions.AddRange(Items.Select(i => i.Points).Distinct().OrderBy(p => p).Select(p => p.ToString()));
        Sync(PointsOptions, pointsOptions);

        // Removing the selected option clears the selection; fall back to "All".
        if (SelectedCategoryFilter is null || !CategoryOptions.Contains(SelectedCategoryFilter))
            SelectedCategoryFilter = AllCategories;
        if (SelectedPointsFilter is null || !PointsOptions.Contains(SelectedPointsFilter))
            SelectedPointsFilter = AllPoints;
    }

    /// <summary>Updates a collection in place (no Clear) so combo boxes bound to it keep their text.</summary>
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

    private static string BuildImportSummary(ImportResult result)
    {
        var lines = new List<string> { $"Added {result.Added.Count} question{(result.Added.Count == 1 ? "" : "s")}." };

        if (result.DuplicatesSkipped > 0)
            lines.Add($"Skipped {result.DuplicatesSkipped} duplicate{(result.DuplicatesSkipped == 1 ? "" : "s")}.");

        if (result.Errors.Count > 0)
        {
            lines.Add("");
            lines.Add($"{result.Errors.Count} problem{(result.Errors.Count == 1 ? "" : "s")}:");
            lines.AddRange(result.Errors.Take(10).Select(e => "• " + e));
            if (result.Errors.Count > 10)
                lines.Add($"…and {result.Errors.Count - 10} more.");
        }

        return string.Join(Environment.NewLine, lines);
    }
}