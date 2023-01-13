using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CreamInstaller.Utility;
using Microsoft.Win32;
using static CreamInstaller.Resources.Resources;

namespace CreamInstaller.Platforms.Ubisoft;

internal static class UbisoftLibrary
{
    private static RegistryKey installsKey;

    internal static RegistryKey InstallsKey
    {
        get
        {
            installsKey = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Ubisoft\Launcher\Installs");
            installsKey ??= Registry.LocalMachine.OpenSubKey(@"SOFTWARE\WOW6432Node\Ubisoft\Launcher\Installs");
            return installsKey;
        }
    }

    internal static async Task<List<(string directory, BinaryType binaryType)>> GetExecutableDirectories(string gameDirectory)
        => await Task.Run(async () => await gameDirectory.GetExecutableDirectories(true));

    internal static async Task<List<(string gameId, string name, string gameDirectory)>> GetGames()
        => await Task.Run(() =>
        {
            List<(string gameId, string name, string gameDirectory)> games = new();
            RegistryKey installsKey = InstallsKey;
            if (installsKey is null)
                return games;
            foreach (string gameId in installsKey.GetSubKeyNames())
            {
                RegistryKey installKey = installsKey.OpenSubKey(gameId);
                string installDir = installKey?.GetValue("InstallDir")?.ToString()?.BeautifyPath();
                if (installDir is not null && !games.Any(g => g.gameId == gameId && g.gameDirectory == installDir))
                    games.Add((gameId, new DirectoryInfo(installDir).Name, installDir));
            }
            return games;
        });
}