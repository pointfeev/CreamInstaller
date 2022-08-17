using CreamInstaller.Resources;

using Microsoft.Win32;

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace CreamInstaller.Ubisoft;

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

    internal static async Task<List<(string gameId, string name, string gameDirectory)>> GetGames() => await Task.Run(() =>
    {
        List<(string gameId, string name, string gameDirectory)> games = new();
        RegistryKey installsKey = InstallsKey;
        if (installsKey is null) return games;
        foreach (string gameId in installsKey.GetSubKeyNames())
        {
            RegistryKey installKey = installsKey.OpenSubKey(gameId);
            string installDir = installKey?.GetValue("InstallDir")?.ToString();
            if (installDir is not null)
                games.Add((gameId, new DirectoryInfo(installDir).Name, Path.GetFullPath(installDir)));
        }
        return games;
    });

    internal static async Task<List<string>> GetDllDirectoriesFromGameDirectory(string gameDirectory) => await Task.Run(async () =>
    {
        List<string> dllDirectories = new();
        if (Program.Canceled || !Directory.Exists(gameDirectory)) return null;
        gameDirectory.GetUplayR1Components(out string sdk32, out string sdk32_o, out string sdk64, out string sdk64_o, out string config);
        if (File.Exists(sdk32)
            || File.Exists(sdk32_o)
            || File.Exists(sdk64)
            || File.Exists(sdk64_o)
            || File.Exists(config))
            dllDirectories.Add(gameDirectory);
        else
        {
            gameDirectory.GetUplayR2Components(out string old_sdk32, out string old_sdk64, out sdk32, out sdk32_o, out sdk64, out sdk64_o, out config);
            if (File.Exists(old_sdk32)
                || File.Exists(old_sdk64)
                || File.Exists(sdk32)
                || File.Exists(sdk32_o)
                || File.Exists(sdk64)
                || File.Exists(sdk64_o)
                || File.Exists(config))
                dllDirectories.Add(gameDirectory);
        }
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
