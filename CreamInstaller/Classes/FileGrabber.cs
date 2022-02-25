using System.Diagnostics;
using System.IO;

using Microsoft.Win32;

namespace CreamInstaller.Classes;

internal static class FileGrabber
{
    internal static string steamInstallPath = null;
    internal static string SteamInstallPath
    {
        get
        {
            steamInstallPath ??= Registry.GetValue(@"HKEY_LOCAL_MACHINE\Software\Valve\Steam", "InstallPath", null) as string;
            steamInstallPath ??= Registry.GetValue(@"HKEY_LOCAL_MACHINE\Software\Wow6432Node\Valve\Steam", "InstallPath", null) as string;
            return steamInstallPath;
        }
    }

    internal static string paradoxLauncherInstallPath = null;
    internal static string ParadoxLauncherInstallPath
    {
        get
        {
            paradoxLauncherInstallPath ??= Registry.GetValue(@"HKEY_CURRENT_USER\Software\Paradox Interactive\Paradox Launcher v2", "LauncherInstallation", null) as string;
            return paradoxLauncherInstallPath;
        }
    }

    internal static void GetApiComponents(this string directory, out string api, out string api_o, out string api64, out string api64_o, out string cApi)
    {
        api = directory + @"\steam_api.dll";
        api_o = directory + @"\steam_api_o.dll";
        api64 = directory + @"\steam_api64.dll";
        api64_o = directory + @"\steam_api64_o.dll";
        cApi = directory + @"\cream_api.ini";
    }

    internal static bool IsFilePathLocked(this string filePath)
    {
        try { File.Open(filePath, FileMode.Open, FileAccess.ReadWrite, FileShare.None).Close(); }
        catch (FileNotFoundException) { return false; }
        catch (IOException) { return true; }
        return false;
    }

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
