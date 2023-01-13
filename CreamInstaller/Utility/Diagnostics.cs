using System;
using System.Diagnostics;
using System.IO;
using Microsoft.Win32;

namespace CreamInstaller.Utility;

internal static class Diagnostics
{
    private static string notepadPlusPlusPath;

    internal static string NotepadPlusPlusPath
    {
        get
        {
            notepadPlusPlusPath ??= Registry.GetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Notepad++", "", null) as string;
            notepadPlusPlusPath ??= Registry.GetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432NODE\Notepad++", "", null) as string;
            return notepadPlusPlusPath;
        }
    }

    internal static string GetNotepadPath()
    {
        string npp = NotepadPlusPlusPath + @"\notepad++.exe";
        return File.Exists(npp) ? npp : Environment.GetFolderPath(Environment.SpecialFolder.Windows) + @"\notepad.exe";
    }

    internal static void OpenFileInNotepad(string path)
    {
        string npp = NotepadPlusPlusPath + @"\notepad++.exe";
        if (File.Exists(npp))
            OpenFileInNotepadPlusPlus(npp, path);
        else
            OpenFileInWindowsNotepad(path);
    }

    private static void OpenFileInNotepadPlusPlus(string npp, string path) => Process.Start(new ProcessStartInfo { FileName = npp, Arguments = path });

    private static void OpenFileInWindowsNotepad(string path) => Process.Start(new ProcessStartInfo { FileName = "notepad.exe", Arguments = path });

    internal static void OpenDirectoryInFileExplorer(string path) => Process.Start(new ProcessStartInfo { FileName = "explorer.exe", Arguments = path });

    internal static void OpenUrlInInternetBrowser(string url) => Process.Start(new ProcessStartInfo { FileName = url, UseShellExecute = true });

    internal static string BeautifyPath(this string path) => path is null ? null : Path.TrimEndingDirectorySeparator(Path.GetFullPath(path));
}