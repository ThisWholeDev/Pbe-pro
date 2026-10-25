using CommunityToolkit.Mvvm.ComponentModel;
using PbePro.Models;

namespace PbePro.ViewModels;

public partial class BankQuestionRow : ObservableObject
{
    public BankQuestionRow(Question model) => Model = model;

    public Question Model { get; }
    public string Id => Model.Id;
    public string Preview => Model.Text;
    public string Answer => Model.Answer;
    public int Points => Model.Points;
    public string? Category => Model.Category;

    [ObservableProperty] private bool _isInQuiz;
}
