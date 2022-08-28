using CreamInstaller.Resources;
using CreamInstaller.Utility;

using Microsoft.Win32;

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

using static CreamInstaller.Resources.Resources;

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
            if (epicManifestsPath is not null && epicManifestsPath.EndsWith(@"\Data")) epicManifestsPath += @"\Manifests";
            return epicManifestsPath.BeautifyPath();
        }
    }

    internal static async Task<List<(string directory, BinaryType binaryType)>> GetExecutableDirectories(string gameDirectory) =>
        await Task.Run(async () => await gameDirectory.GetExecutableDirectories(filterCommon: true));

    internal static async Task<List<Manifest>> GetGames() => await Task.Run(() =>
    {
        List<Manifest> games = new();
        string manifests = EpicManifestsPath;
        if (!Directory.Exists(manifests)) return games;
        string[] files = Directory.GetFiles(manifests, "*.item");
        foreach (string file in files)
        {
            if (Program.Canceled) return games;
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
        gameDirectory.GetScreamApiComponents(out string api32, out string api32_o, out string api64, out string api64_o, out string config);
        if (File.Exists(api32)
            || File.Exists(api32_o)
            || File.Exists(api64)
            || File.Exists(api64_o)
            || File.Exists(config))
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
}