using System;
using System.Diagnostics;
using System.IO;
using Microsoft.Win32;

namespace CreamInstaller.Utility;

internal static class Diagnostics
{
    private static string nppPath;

    private static string NppPath
    {
        get
        {
            nppPath ??= Registry.GetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Notepad++", "", null) as string;
            nppPath ??= Registry.GetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432NODE\Notepad++", "", null) as string;
            return nppPath;
        }
    }

    internal static string GetNotepadPath()
    {
        string npp = NppPath + @"\notepad++.exe";
        return npp.Exists() ? npp : Environment.GetFolderPath(Environment.SpecialFolder.Windows) + @"\notepad.exe";
    }

    internal static void OpenFileInNotepad(string path)
    {
        string npp = NppPath + @"\notepad++.exe";
        if (npp.Exists())
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