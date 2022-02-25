using System.Diagnostics;

namespace CreamInstaller.Classes;

internal static class Diagnostics
{
    internal static void OpenFileInNotepad(string path) => Process.Start(new ProcessStartInfo
    {
        FileName = "notepad.exe",
        Arguments = path
    });

    internal static void OpenDirectoryInFileExplorer(string path) => Process.Start(new ProcessStartInfo
    {
        FileName = "explorer.exe",
        Arguments = path
    });

    internal static void OpenUrlInInternetBrowser(string url) => Process.Start(new ProcessStartInfo
    {
        FileName = url,
        UseShellExecute = true
    });
}
