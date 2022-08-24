using CreamInstaller.Resources;
using CreamInstaller.Utility;

using Gameloop.Vdf.Linq;

using Microsoft.Win32;

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CreamInstaller.Steam;

internal static class SteamLibrary
{
    private static string installPath;
    internal static string InstallPath
    {
        get
        {
            installPath ??= Registry.GetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Valve\Steam", "InstallPath", null) as string;
            installPath ??= Registry.GetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Valve\Steam", "InstallPath", null) as string;
            return installPath.BeautifyPath();
        }
    }

    internal static async Task<List<(string appId, string name, string branch, int buildId, string gameDirectory)>> GetGames() => await Task.Run(async () =>
    {
        List<(string appId, string name, string branch, int buildId, string gameDirectory)> games = new();
        List<string> gameLibraryDirectories = await GetLibraryDirectories();
        foreach (string libraryDirectory in gameLibraryDirectories)
        {
            if (Program.Canceled) return games;
            List<(string appId, string name, string branch, int buildId, string gameDirectory)> directoryGames = await GetGamesFromLibraryDirectory(libraryDirectory);
            if (directoryGames is not null)
                foreach ((string appId, string name, string branch, int buildId, string gameDirectory) game in directoryGames
                    .Where(game => !games.Any(_game => _game.appId == game.appId)))
                    games.Add(game);
        }
        return games;
    });

    internal static async Task<List<string>> GetDllDirectoriesFromGameDirectory(string gameDirectory) => await Task.Run(async () =>
    {
        List<string> dllDirectories = new();
        if (Program.Canceled || !Directory.Exists(gameDirectory)) return null;
        gameDirectory.GetSmokeApiComponents(out string api, out string api_o, out string api64, out string api64_o, out string config, out string cache);
        if (File.Exists(api)
            || File.Exists(api_o)
            || File.Exists(api64)
            || File.Exists(api64_o)
            || File.Exists(config)
            || File.Exists(cache))
            dllDirectories.Add(gameDirectory.BeautifyPath());
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

    internal static async Task<List<(string appId, string name, string branch, int buildId, string gameDirectory)>> GetGamesFromLibraryDirectory(string libraryDirectory) => await Task.Run(() =>
    {
        List<(string appId, string name, string branch, int buildId, string gameDirectory)> games = new();
        if (Program.Canceled || !Directory.Exists(libraryDirectory)) return null;
        string[] files = Directory.GetFiles(libraryDirectory, "*.acf");
        foreach (string file in files)
        {
            if (Program.Canceled) return null;
            if (ValveDataFile.TryDeserialize(File.ReadAllText(file, Encoding.UTF8), out VProperty result))
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
                games.Add((appId, name, branch, buildIdInt, gameDirectory.BeautifyPath()));
            }
        }
        return !games.Any() ? null : games;
    });

    internal static async Task<List<string>> GetLibraryDirectories() => await Task.Run(() =>
    {
        List<string> gameDirectories = new();
        if (Program.Canceled) return gameDirectories;
        string steamInstallPath = InstallPath;
        if (steamInstallPath != null && Directory.Exists(steamInstallPath))
        {
            string libraryFolder = steamInstallPath + @"\steamapps";
            if (Directory.Exists(libraryFolder))
            {
                gameDirectories.Add(libraryFolder);
                string libraryFolders = libraryFolder + @"\libraryfolders.vdf";
                if (File.Exists(libraryFolders) && ValveDataFile.TryDeserialize(File.ReadAllText(libraryFolders, Encoding.UTF8), out VProperty result))
#pragma warning disable IDE0220 // Add explicit cast
                    foreach (VProperty property in result.Value.Where(p => p is VProperty && int.TryParse((p as VProperty).Key, out int _)))
                    {
                        string path = property.Value.GetChild("path")?.ToString();
                        if (string.IsNullOrWhiteSpace(path)) continue;
                        path += @"\steamapps";
                        if (Directory.Exists(path) && !gameDirectories.Contains(path)) gameDirectories.Add(path);
                    }
#pragma warning restore IDE0220 // Add explicit cast
            }
        }
        return gameDirectories;
    });
}
