using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PbePro.Services;
using PbePro.Views;

namespace PbePro.ViewModels;

public partial class StartViewModel : ObservableObject
{
    // Each of these will point at its real page as the later steps are built.

    [RelayCommand]
    private void EditQuestions() =>
        Navigator.Navigate(new QuestionEditorPage());

    [RelayCommand]
    private void EditQuizzes() =>
        Navigator.Navigate(new QuizEditorPage());

    [RelayCommand]
    private void StartQuiz() =>
        Navigator.Navigate(new QuizPickerPage());

    [RelayCommand]
    private void OpenHistory() =>
        Navigator.Navigate(new HistoryPage());

    [RelayCommand]
    private void OpenSettings() =>
        Navigator.Navigate(new SettingsPage());
}
