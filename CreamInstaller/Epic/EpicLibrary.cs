using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Win32;

namespace CreamInstaller.Epic;

internal static class EpicLibrary
{
    private static string epicManifestsPath;
    internal static string EpicManifestsPath
    {
        get
        {
            epicManifestsPath ??= Registry.GetValue(@"HKEY_CURRENT_USER\Software\Epic Games\EOS", "ModSdkMetadataDir", null) as string;
            epicManifestsPath ??= Registry.GetValue(@"HKEY_CURRENT_USER\Software\Wow6432Node\Epic Games\EOS", "ModSdkMetadataDir", null) as string;
            epicManifestsPath ??= Registry.GetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Epic Games\EpicGamesLauncher", "AppDataPath", null) as string;
            epicManifestsPath ??= Registry.GetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Epic Games\EpicGamesLauncher", "AppDataPath", null) as string;
            if (epicManifestsPath.EndsWith(@"\Data")) epicManifestsPath += @"\Manifests";
            return epicManifestsPath;
        }
    }

    internal static async Task<List<Manifest>> GetGames() => await Task.Run(() =>
    {
        List<Manifest> games = new();
        string manifests = EpicManifestsPath;
        if (!Directory.Exists(manifests)) return games;
        string[] files = Directory.GetFiles(manifests, "*.item");
        foreach (string file in files)
        {
            if (Program.Canceled) return games;
            Thread.Sleep(0);
            string json = File.ReadAllText(file);
            try
            {
                Manifest manifest = JsonSerializer.Deserialize<Manifest>(json);
                if (manifest is not null && manifest.CatalogItemId == manifest.MainGameCatalogItemId)
                    games.Add(manifest);
            }
            catch { };
        }
        return games;
    });

    internal static async Task<List<string>> GetDllDirectoriesFromGameDirectory(string gameDirectory) => await Task.Run(async () =>
    {
        List<string> dllDirectories = new();
        if (Program.Canceled || !Directory.Exists(gameDirectory)) return null;
        gameDirectory.GetScreamApiComponents(out string sdk32, out string sdk32_o, out string sdk64, out string sdk64_o, out string config);
        if (File.Exists(sdk32)
            || File.Exists(sdk32_o)
            || File.Exists(sdk64)
            || File.Exists(sdk64_o)
            || File.Exists(config))
            dllDirectories.Add(gameDirectory);
        string[] directories = Directory.GetDirectories(gameDirectory);
        foreach (string _directory in directories)
        {
            if (Program.Canceled) return null;
            Thread.Sleep(0);
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