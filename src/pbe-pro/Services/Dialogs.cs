using System.Windows;
using Microsoft.Win32;
using WpfMessageBox = Wpf.Ui.Controls.MessageBox;
using WpfMessageBoxResult = Wpf.Ui.Controls.MessageBoxResult;

namespace PbePro.Services;

/// <summary>Themed WPF UI dialogs plus the standard Windows file pickers.</summary>
public static class Dialogs
{
    /// <summary>Two-button confirmation. Returns true only if the confirm button is clicked.</summary>
    public static async Task<bool> ConfirmAsync(
        string title, string message, string confirmText = "Yes", string cancelText = "Cancel", bool dangerous = false)
    {
        var result = await ShowAsync(title, message, confirmText, null, cancelText, dangerous);
        return result == WpfMessageBoxResult.Primary;
    }

    /// <summary>Two choices plus Cancel. True = primary, false = secondary, null = cancelled.</summary>
    public static async Task<bool?> ChooseAsync(
        string title, string message, string primaryText, string secondaryText, string cancelText = "Cancel")
    {
        var result = await ShowAsync(title, message, primaryText, secondaryText, cancelText, false);
        return result switch
        {
            WpfMessageBoxResult.Primary => true,
            WpfMessageBoxResult.Secondary => false,
            _ => null
        };
    }

    public static Task InfoAsync(string title, string message) =>
        ShowAsync(title, message, null, null, "OK", false);

    /// <summary>Asks for a line of text. Returns null if cancelled, otherwise the trimmed text.</summary>
    public static string? Prompt(string title, string message, string initialValue = "", string confirmText = "OK")
    {
        var dialog = new Views.PromptDialog(title, message, initialValue, confirmText);
        if (Application.Current?.MainWindow is { } owner)
            dialog.Owner = owner;

        return dialog.ShowDialog() == true ? dialog.Result : null;
    }

    private static Task<WpfMessageBoxResult> ShowAsync(
        string title, string message, string? primaryText, string? secondaryText, string closeText, bool dangerous)
    {
        var box = new WpfMessageBox
        {
            Title = title,
            Content = new Wpf.Ui.Controls.TextBlock
            {
                Text = message,
                TextWrapping = TextWrapping.Wrap,
                MaxWidth = 440
            },
            CloseButtonText = closeText
        };

        if (primaryText is not null)
        {
            box.PrimaryButtonText = primaryText;
            if (dangerous)
                box.PrimaryButtonAppearance = Wpf.Ui.Controls.ControlAppearance.Danger;
        }

        if (secondaryText is not null)
            box.SecondaryButtonText = secondaryText;

        return box.ShowDialogAsync();
    }

    public static string? PickOpenFile(string title, string filter)
    {
        var dialog = new OpenFileDialog { Title = title, Filter = filter, CheckFileExists = true };
        return dialog.ShowDialog() == true ? dialog.FileName : null;
    }

    public static string? PickSaveFile(string title, string filter, string defaultFileName)
    {
        var dialog = new SaveFileDialog
        {
            Title = title,
            Filter = filter,
            FileName = defaultFileName,
            OverwritePrompt = true
        };
        return dialog.ShowDialog() == true ? dialog.FileName : null;
    }
}