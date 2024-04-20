using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CreamInstaller.Utility;
using Microsoft.Win32;

namespace CreamInstaller.Platforms.Ubisoft;

internal static class UbisoftLibrary
{
    private static RegistryKey installsKey;

    private static RegistryKey InstallsKey
    {
        get
        {
            installsKey = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\WOW6432Node\Ubisoft\Launcher\Installs");
            return installsKey;
        }
    }

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
                string installDir = installKey?.GetValue("InstallDir")?.ToString()?.ResolvePath();
                if (installDir is not null && games.All(g => g.gameId != gameId))
                    games.Add((gameId, new DirectoryInfo(installDir).Name, installDir));
            }

            return games;
        });
}