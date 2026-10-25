using CommunityToolkit.Mvvm.ComponentModel;
using PbePro.Models;
using PbePro.Services;

namespace PbePro.ViewModels;

public class QuizListItemViewModel : ObservableObject
{
    private readonly IReadOnlyDictionary<string, Question> _bank;

    public QuizListItemViewModel(Quiz model, IReadOnlyDictionary<string, Question> bank)
    {
        Model = model;
        _bank = bank;
    }

    public Quiz Model { get; }

    public string Id => Model.Id;

    public string Name => Model.Name;

    public string Details => Summary.DisplayText;

    public QuizSummary Summary =>
        QuizSummary.From(Model.QuestionIds
            .Where(_bank.ContainsKey)
            .Select(id => _bank[id]));

    public void NotifyChanged()
    {
        OnPropertyChanged(nameof(Name));
        OnPropertyChanged(nameof(Details));
        OnPropertyChanged(nameof(Summary));
    }
}
