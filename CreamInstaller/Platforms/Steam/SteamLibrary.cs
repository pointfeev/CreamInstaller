using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CreamInstaller.Utility;
using Gameloop.Vdf.Linq;
using Microsoft.Win32;
using static CreamInstaller.Resources.Resources;

namespace CreamInstaller.Platforms.Steam;

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

    internal static async Task<List<(string directory, BinaryType binaryType)>> GetExecutableDirectories(string gameDirectory)
        => await Task.Run(async () => await gameDirectory.GetExecutableDirectories(true));

    internal static async Task<List<(string appId, string name, string branch, int buildId, string gameDirectory)>> GetGames()
        => await Task.Run(async () =>
        {
            List<(string appId, string name, string branch, int buildId, string gameDirectory)> games = new();
            List<string> gameLibraryDirectories = await GetLibraryDirectories();
            foreach (string libraryDirectory in gameLibraryDirectories)
            {
                if (Program.Canceled)
                    return games;
                foreach ((string appId, string name, string branch, int buildId, string gameDirectory) game in
                         (await GetGamesFromLibraryDirectory(libraryDirectory)).Where(game
                             => !games.Any(_game => _game.appId == game.appId && _game.gameDirectory == game.gameDirectory)))
                    games.Add(game);
            }
            return games;
        });

    private static async Task<List<(string appId, string name, string branch, int buildId, string gameDirectory)>>
        GetGamesFromLibraryDirectory(string libraryDirectory)
        => await Task.Run(() =>
        {
            List<(string appId, string name, string branch, int buildId, string gameDirectory)> games = new();
            if (Program.Canceled || !Directory.Exists(libraryDirectory))
                return games;
            foreach (string file in Directory.EnumerateFiles(libraryDirectory, "*.acf"))
            {
                if (Program.Canceled)
                    return games;
                if (!ValveDataFile.TryDeserialize(File.ReadAllText(file, Encoding.UTF8), out VProperty result))
                    continue;
                string appId = result.Value.GetChild("appid")?.ToString();
                string installdir = result.Value.GetChild("installdir")?.ToString();
                string name = result.Value.GetChild("name")?.ToString();
                string buildId = result.Value.GetChild("buildid")?.ToString();
                if (string.IsNullOrWhiteSpace(appId) || string.IsNullOrWhiteSpace(installdir) || string.IsNullOrWhiteSpace(name)
                 || string.IsNullOrWhiteSpace(buildId))
                    continue;
                string gameDirectory = (libraryDirectory + @"\common\" + installdir).BeautifyPath();
                if (games.Any(g => g.appId == appId && g.gameDirectory == gameDirectory))
                    continue;
                if (!int.TryParse(appId, out int _))
                    continue;
                if (!int.TryParse(buildId, out int buildIdInt))
                    continue;
                string branch = result.Value.GetChild("UserConfig")?.GetChild("betakey")?.ToString();
                if (string.IsNullOrWhiteSpace(branch))
                    branch = "public";
                games.Add((appId, name, branch, buildIdInt, gameDirectory));
            }
            return games;
        });

    private static async Task<List<string>> GetLibraryDirectories()
        => await Task.Run(() =>
        {
            List<string> gameDirectories = new();
            if (Program.Canceled)
                return gameDirectories;
            string steamInstallPath = InstallPath;
            if (steamInstallPath == null || !Directory.Exists(steamInstallPath))
                return gameDirectories;
            string libraryFolder = steamInstallPath + @"\steamapps";
            if (!Directory.Exists(libraryFolder))
                return gameDirectories;
            gameDirectories.Add(libraryFolder);
            string libraryFolders = libraryFolder + @"\libraryfolders.vdf";
            if (!File.Exists(libraryFolders) || !ValveDataFile.TryDeserialize(File.ReadAllText(libraryFolders, Encoding.UTF8), out VProperty result))
                return gameDirectories;
            foreach (VToken vToken in result.Value.Where(p => p is VProperty property && int.TryParse(property.Key, out int _)))
            {
                VProperty property = (VProperty)vToken;
                string path = property.Value.GetChild("path")?.ToString();
                if (string.IsNullOrWhiteSpace(path))
                    continue;
                path += @"\steamapps";
                if (Directory.Exists(path) && !gameDirectories.Contains(path))
                    gameDirectories.Add(path);
            }
            return gameDirectories;
        });
}