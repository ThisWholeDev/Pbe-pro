using CommunityToolkit.Mvvm.ComponentModel;
using PbePro.Models;
using PbePro.Services;

namespace PbePro.ViewModels;

/// <summary>Wraps a Question so the list and the edit panel update as it is edited.</summary>
public class QuestionItemViewModel : ObservableObject
{
    public QuestionItemViewModel(Question model) => Model = model;

    public Question Model { get; }

    public string Id => Model.Id;

    public string Text
    {
        get => Model.Text;
        set
        {
            value ??= "";
            if (Model.Text == value) return;
            Model.Text = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(Preview));
            OnPropertyChanged(nameof(IsIncomplete));
        }
    }

    public string Answer
    {
        get => Model.Answer;
        set
        {
            value ??= "";
            if (Model.Answer == value) return;
            Model.Answer = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(IsIncomplete));
        }
    }

    public int Points
    {
        get => Model.Points;
        set
        {
            if (value < 1) value = 1;

            if (Model.Points != value)
            {
                Model.Points = value;
                OnPropertyChanged(nameof(TimeHint));
            }

            // Raised even when unchanged so a rejected entry (like 0) snaps back in the text box.
            OnPropertyChanged();
        }
    }

    public string? Category
    {
        get => Model.Category;
        set
        {
            var cleaned = string.IsNullOrWhiteSpace(value) ? null : value.Trim();
            if (Model.Category == cleaned) return;
            Model.Category = cleaned;
            OnPropertyChanged();
        }
    }

    public string Preview => string.IsNullOrWhiteSpace(Text) ? "(New question)" : Text;

    public bool IsIncomplete => QuestionValidator.Validate(Model).Count > 0;

    public string TimeHint =>
        $"{Points} point{(Points == 1 ? "" : "s")} = {QuestionTiming.GetSeconds(Points)} seconds to answer";
}
