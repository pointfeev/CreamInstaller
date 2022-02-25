using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Gameloop.Vdf.Linq;

using Microsoft.Win32;

namespace CreamInstaller.Classes;

internal static class SteamLibrary
{
    internal static string paradoxLauncherInstallPath = null;
    internal static string ParadoxLauncherInstallPath
    {
        get
        {
            paradoxLauncherInstallPath ??= Registry.GetValue(@"HKEY_CURRENT_USER\Software\Paradox Interactive\Paradox Launcher v2", "LauncherInstallation", null) as string;
            return paradoxLauncherInstallPath;
        }
    }

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

    internal static async Task<List<string>> GetLibraryDirectories() => await Task.Run(() =>
    {
        List<string> gameDirectories = new();
        if (Program.Canceled) return gameDirectories;
        string steamInstallPath = SteamInstallPath;
        if (steamInstallPath != null && Directory.Exists(steamInstallPath))
        {
            string libraryFolder = steamInstallPath + @"\steamapps";
            if (Directory.Exists(libraryFolder))
            {
                gameDirectories.Add(libraryFolder);
                string libraryFolders = libraryFolder + @"\libraryfolders.vdf";
                if (File.Exists(libraryFolders) && ValveDataFile.TryDeserialize(File.ReadAllText(libraryFolders, Encoding.UTF8), out VProperty result))
                {
                    foreach (VProperty property in result.Value)
                        if (int.TryParse(property.Key, out int _))
                        {
                            string path = property.Value.GetChild("path")?.ToString();
                            if (string.IsNullOrWhiteSpace(path)) continue;
                            path += @"\steamapps";
                            if (Directory.Exists(path) && !gameDirectories.Contains(path)) gameDirectories.Add(path);
                        }
                }
            }
        }
        return gameDirectories;
    });

    internal static async Task<List<Tuple<int, string, string, int, string>>> GetGamesFromLibraryDirectory(string libraryDirectory) => await Task.Run(() =>
    {
        List<Tuple<int, string, string, int, string>> games = new();
        if (Program.Canceled || !Directory.Exists(libraryDirectory)) return null;
        string[] files = Directory.GetFiles(libraryDirectory);
        foreach (string file in files)
        {
            if (Program.Canceled) return null;
            if (Path.GetExtension(file) == ".acf" && ValveDataFile.TryDeserialize(File.ReadAllText(file, Encoding.UTF8), out VProperty result))
            {
                string appId = result.Value.GetChild("appid")?.ToString();
                string installdir = result.Value.GetChild("installdir")?.ToString();
                string name = result.Value.GetChild("name")?.ToString();
                string buildId = result.Value.GetChild("buildid")?.ToString();
                if (string.IsNullOrWhiteSpace(appId)
                    || string.IsNullOrWhiteSpace(installdir)
                    || string.IsNullOrWhiteSpace(name)
                    || string.IsNullOrWhiteSpace(buildId))
                    continue;
                string branch = result.Value.GetChild("UserConfig")?.GetChild("betakey")?.ToString();
                if (string.IsNullOrWhiteSpace(branch)) branch = "public";
                string gameDirectory = libraryDirectory + @"\common\" + installdir;
                if (!int.TryParse(appId, out int appIdInt)) continue;
                if (!int.TryParse(buildId, out int buildIdInt)) continue;
                games.Add(new(appIdInt, name, branch, buildIdInt, gameDirectory));
            }
        }
        return !games.Any() ? null : games;
    });

    internal static async Task<List<string>> GetDllDirectoriesFromGameDirectory(string gameDirectory) => await Task.Run(async () =>
    {
        List<string> dllDirectories = new();
        if (Program.Canceled || !Directory.Exists(gameDirectory)) return null;
        gameDirectory.GetApiComponents(out string api, out string api_o, out string api64, out string api64_o, out string cApi);
        if (File.Exists(api)
            || File.Exists(api_o)
            || File.Exists(api64)
            || File.Exists(api64_o)
            || File.Exists(cApi))
            dllDirectories.Add(gameDirectory);
        string[] directories = Directory.GetDirectories(gameDirectory);
        foreach (string _directory in directories)
        {
            if (Program.Canceled) return null;
            try
            {
                List<string> moreDllDirectories = await GetDllDirectoriesFromGameDirectory(_directory);
                if (moreDllDirectories is not null) dllDirectories.AddRange(moreDllDirectories);
            }
            catch { }
        }
        return !dllDirectories.Any() ? null : dllDirectories;
    });
}
