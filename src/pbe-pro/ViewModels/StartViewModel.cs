using System.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PbePro.Services;
using PbePro.Views;

namespace PbePro.ViewModels;

public partial class StartViewModel : ObservableObject
{
    [ObservableProperty]
    private bool _isUpdateAvailable;

    [ObservableProperty]
    private string _updateMessage = "";

    private string _updateUrl = "";

    public async Task InitializeAsync()
    {
        if (UpdateChecker.Dismissed) return;

        var update = await UpdateChecker.GetCachedAsync();
        if (update is null) return;

        _updateUrl = update.Url;
        UpdateMessage = $"Version {update.TagName} is ready to download.";
        IsUpdateAvailable = true;
    }

    // Fires when the user closes the banner; don't show it again this session.
    partial void OnIsUpdateAvailableChanged(bool value)
    {
        if (!value)
            UpdateChecker.Dismissed = true;
    }

    [RelayCommand]
    private void OpenUpdatePage()
    {
        if (!string.IsNullOrEmpty(_updateUrl))
            Process.Start(new ProcessStartInfo(_updateUrl) { UseShellExecute = true });
    }

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
