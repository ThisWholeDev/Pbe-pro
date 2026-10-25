using System.Diagnostics;
using System.IO;
using System.Media;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PbePro.Models;
using PbePro.Services;

namespace PbePro.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
    private readonly DispatcherTimer _flashTimer = new() { Interval = TimeSpan.FromMilliseconds(1900) };

    public SettingsViewModel()
    {
        _flashTimer.Tick += (_, _) =>
        {
            _flashTimer.Stop();
            IsFlashing = false;
        };

        RefreshFolder();
    }

    /// <summary>Stops the test-flash timer when leaving the page.</summary>
    public void Shutdown() => _flashTimer.Stop();

    // ----- Theme -----

    [ObservableProperty]
    private ThemePreference _theme = SettingsService.Current.Theme;

    public bool IsSystemTheme
    {
        get => Theme == ThemePreference.System;
        set { if (value) Theme = ThemePreference.System; }
    }

    public bool IsLightTheme
    {
        get => Theme == ThemePreference.Light;
        set { if (value) Theme = ThemePreference.Light; }
    }

    public bool IsDarkTheme
    {
        get => Theme == ThemePreference.Dark;
        set { if (value) Theme = ThemePreference.Dark; }
    }

    partial void OnThemeChanged(ThemePreference value)
    {
        OnPropertyChanged(nameof(IsSystemTheme));
        OnPropertyChanged(nameof(IsLightTheme));
        OnPropertyChanged(nameof(IsDarkTheme));

        Persist(s => s.Theme = value);
        ThemeService.Apply(value);
    }

    // ----- When time runs out -----

    [ObservableProperty] private bool _flashOnExpire = SettingsService.Current.FlashOnExpire;
    [ObservableProperty] private bool _soundOnExpire = SettingsService.Current.SoundOnExpire;
    [ObservableProperty] private bool _isFlashing;

    partial void OnFlashOnExpireChanged(bool value) => Persist(s => s.FlashOnExpire = value);
    partial void OnSoundOnExpireChanged(bool value) => Persist(s => s.SoundOnExpire = value);

    [RelayCommand]
    private void TestSound() => SystemSounds.Exclamation.Play();

    [RelayCommand]
    private void TestFlash()
    {
        _flashTimer.Stop();
        IsFlashing = false;
        IsFlashing = true;
        _flashTimer.Start();
    }

    // ----- Display -----

    [ObservableProperty] private int _textSizeIndex = (int)SettingsService.Current.TextSize;
    [ObservableProperty] private bool _startFullScreen = SettingsService.Current.StartFullScreen;

    public double PreviewFontSize =>
        TextSizes.QuestionFontSize((QuestionTextSize)Math.Clamp(TextSizeIndex, 0, 3));

    partial void OnTextSizeIndexChanged(int value)
    {
        OnPropertyChanged(nameof(PreviewFontSize));

        if (value is >= 0 and <= 3)
            Persist(s => s.TextSize = (QuestionTextSize)value);
    }

    partial void OnStartFullScreenChanged(bool value) => Persist(s => s.StartFullScreen = value);

    // ----- Data folder -----

    [ObservableProperty] private string _dataFolderPath = "";
    [ObservableProperty] private bool _isCustomFolder;

    [RelayCommand]
    private async Task ChangeFolderAsync()
    {
        var picked = FolderPicker.Pick("Choose the data folder", DataFolderPath);
        if (picked is not null)
            await SwitchFolderAsync(picked);
    }

    [RelayCommand]
    private Task UseDefaultFolderAsync() => SwitchFolderAsync(JsonDataService.DefaultFolder);

    [RelayCommand]
    private async Task OpenFolderAsync()
    {
        try
        {
            Directory.CreateDirectory(DataFolderPath);
            Process.Start(new ProcessStartInfo(DataFolderPath) { UseShellExecute = true });
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException
                                       or System.ComponentModel.Win32Exception)
        {
            await Dialogs.InfoAsync("Couldn’t open the folder", ex.Message);
        }
    }

    private async Task SwitchFolderAsync(string target)
    {
        if (DataFolderService.SameFolder(target, DataFolderPath))
            return;

        var here = DataFolderService.FilesIn(DataFolderPath);
        var there = DataFolderService.FilesIn(target);
        var message = "";

        if (there.Count > 0)
        {
            message = "That folder already has question or quiz files, so they’ll be used as they are. " +
                      "Nothing was copied or overwritten.";
        }
        else if (here.Count > 0)
        {
            var choice = await Dialogs.ChooseAsync(
                "Copy your data?",
                "Copy your current questions and quizzes into the new folder?\n\n" +
                "The originals stay where they are either way.",
                "Copy files", "Start empty");
            if (choice is null)
                return;

            if (choice == true)
            {
                try
                {
                    DataFolderService.Copy(DataFolderPath, target, here);
                    message = $"Copied {here.Count} file{(here.Count == 1 ? "" : "s")} to the new folder.";
                }
                catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
                {
                    await Dialogs.InfoAsync("Couldn’t copy the files", ex.Message);
                    return;
                }
            }
            else
            {
                message = "The new folder is empty, so you’ll start with no questions or quizzes.";
            }
        }

        Persist(s => s.DataFolder = DataFolderService.SameFolder(target, JsonDataService.DefaultFolder) ? null : target);
        RefreshFolder();

        if (message.Length > 0)
            await Dialogs.InfoAsync("Data folder changed", message);
    }

    private void RefreshFolder()
    {
        DataFolderPath = JsonDataService.CreateFromSettings().Folder;
        IsCustomFolder = !string.IsNullOrWhiteSpace(SettingsService.Current.DataFolder);
    }

    private static void Persist(Action<AppSettings> change)
    {
        change(SettingsService.Current);
        SettingsService.Save();
    }
}
