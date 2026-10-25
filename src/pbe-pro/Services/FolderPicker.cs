using System.IO;
using Microsoft.Win32;

namespace PbePro.Services;

public static class FolderPicker
{
    /// <summary>Shows the standard "choose a folder" dialog. Returns null if cancelled.</summary>
    public static string? Pick(string title, string? initialDirectory = null)
    {
        var dialog = new OpenFolderDialog { Title = title };
        if (!string.IsNullOrEmpty(initialDirectory) && Directory.Exists(initialDirectory))
            dialog.InitialDirectory = initialDirectory;

        return dialog.ShowDialog() == true ? dialog.FolderName : null;
    }
}
