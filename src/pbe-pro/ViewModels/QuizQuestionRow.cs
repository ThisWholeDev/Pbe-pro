using CommunityToolkit.Mvvm.ComponentModel;
using PbePro.Models;
using PbePro.Services;

namespace PbePro.ViewModels;

public partial class QuizQuestionRow : ObservableObject
{
    public QuizQuestionRow(Question model) => Model = model;

    public Question Model { get; }
    public string Id => Model.Id;
    public string Preview => Model.Text;
    public string Answer => Model.Answer;
    public int Points => Model.Points;
    public string? Category => Model.Category;
    public string TimeText => $"{QuestionTiming.GetSeconds(Model.Points)}s";

    [ObservableProperty] private int _number;
    [ObservableProperty] private bool _canMoveUp;
    [ObservableProperty] private bool _canMoveDown;
}
