using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CreamInstaller.Utility;
using Gameloop.Vdf.Linq;
using Microsoft.Win32;

namespace CreamInstaller.Platforms.Steam;

internal static class SteamLibrary
{
    private static string installPath;

    internal static string InstallPath
    {
        get
        {
            installPath ??= Registry.GetValue(@"HKEY_CURRENT_USER\Software\Valve\Steam", "SteamPath", null) as string;
            installPath ??=
                Registry.GetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Valve\Steam", "InstallPath", null) as string;
            return installPath.ResolvePath();
        }
    }

    internal static async Task<List<(string appId, string name, string branch, int buildId, string gameDirectory)>>
        GetGames()
        => await Task.Run(async () =>
        {
            List<(string appId, string name, string branch, int buildId, string gameDirectory)> games = new();
            HashSet<string> gameLibraryDirectories = await GetLibraryDirectories();
            foreach (string libraryDirectory in gameLibraryDirectories)
            {
                if (Program.Canceled)
                    return games;
                foreach ((string appId, string name, string branch, int buildId, string gameDirectory) game in (await
                             GetGamesFromLibraryDirectory(
                                 libraryDirectory)).Where(game => games.All(_game => _game.appId != game.appId)))
                    games.Add(game);
            }

            return games;
        });

    private static async Task<List<(string appId, string name, string branch, int buildId, string gameDirectory)>>
        GetGamesFromLibraryDirectory(string libraryDirectory)
        => await Task.Run(() =>
        {
            List<(string appId, string name, string branch, int buildId, string gameDirectory)> games = new();
            if (Program.Canceled || !libraryDirectory.DirectoryExists())
                return games;
            foreach (string file in libraryDirectory.EnumerateDirectory("*.acf"))
            {
                if (Program.Canceled)
                    return games;
                if (!ValveDataFile.TryDeserialize(file.ReadFile(), out VProperty result))
                    continue;
                string appId = result.Value.GetChild("appid")?.ToString();
                string installdir = result.Value.GetChild("installdir")?.ToString();
                string name = result.Value.GetChild("name")?.ToString();
                string buildId = result.Value.GetChild("buildid")?.ToString();
                if (string.IsNullOrWhiteSpace(appId) || string.IsNullOrWhiteSpace(installdir) ||
                    string.IsNullOrWhiteSpace(name)
                    || string.IsNullOrWhiteSpace(buildId))
                    continue;
                string gameDirectory = (libraryDirectory + @"\common\" + installdir).ResolvePath();
                if (gameDirectory is null || !int.TryParse(appId, out int _) ||
                    !int.TryParse(buildId, out int buildIdInt) || games.Any(g => g.appId == appId))
                    continue;
                VToken userConfig = result.Value.GetChild("UserConfig");
                string branch = userConfig?.GetChild("BetaKey")?.ToString();
                branch ??= userConfig?.GetChild("betakey")?.ToString();
                if (branch is null)
                {
                    VToken mountedConfig = result.Value.GetChild("MountedConfig");
                    branch = mountedConfig?.GetChild("BetaKey")?.ToString();
                    branch ??= mountedConfig?.GetChild("betakey")?.ToString();
                }

                if (string.IsNullOrWhiteSpace(branch))
                    branch = "public";
                games.Add((appId, name, branch, buildIdInt, gameDirectory));
            }

            return games;
        });

    private static async Task<HashSet<string>> GetLibraryDirectories()
        => await Task.Run(() =>
        {
            HashSet<string> libraryDirectories = new();
            if (Program.Canceled)
                return libraryDirectories;
            string steamInstallPath = InstallPath;
            if (steamInstallPath == null || !steamInstallPath.DirectoryExists())
                return libraryDirectories;
            string libraryFolder = steamInstallPath + @"\steamapps";
            if (!libraryFolder.DirectoryExists())
                return libraryDirectories;
            _ = libraryDirectories.Add(libraryFolder);
            string libraryFolders = libraryFolder + @"\libraryfolders.vdf";
            if (!libraryFolders.FileExists() ||
                !ValveDataFile.TryDeserialize(libraryFolders.ReadFile(), out VProperty result))
                return libraryDirectories;
            foreach (VToken vToken in result.Value.Where(p =>
                         p is VProperty property && int.TryParse(property.Key, out int _)))
            {
                VProperty property = (VProperty)vToken;
                string path = property.Value.GetChild("path")?.ToString();
                if (string.IsNullOrWhiteSpace(path))
                    continue;
                path += @"\steamapps";
                if (path.DirectoryExists())
                    _ = libraryDirectories.Add(path);
            }

            return libraryDirectories;
        });
}