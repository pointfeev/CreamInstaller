using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using CreamInstaller.Utility;
using Microsoft.Win32;
using static CreamInstaller.Resources.Resources;

namespace CreamInstaller.Platforms.Epic;

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
            if (epicManifestsPath is not null && epicManifestsPath.EndsWith(@"\Data"))
                epicManifestsPath += @"\Manifests";
            return epicManifestsPath.BeautifyPath();
        }
    }

    internal static async Task<List<(string directory, BinaryType binaryType)>> GetExecutableDirectories(string gameDirectory)
        => await Task.Run(async () => await gameDirectory.GetExecutableDirectories(true));

    internal static async Task<List<Manifest>> GetGames()
        => await Task.Run(() =>
        {
            List<Manifest> games = new();
            string manifests = EpicManifestsPath;
            if (!Directory.Exists(manifests))
                return games;
            foreach (string file in Directory.EnumerateFiles(manifests, "*.item"))
            {
                if (Program.Canceled)
                    return games;
                string json = File.ReadAllText(file);
                try
                {
                    Manifest manifest = JsonSerializer.Deserialize<Manifest>(json);
                    if (manifest is not null && manifest.CatalogItemId == manifest.MainGameCatalogItemId && !games.Any(g
                            => g.CatalogItemId == manifest.CatalogItemId && g.InstallLocation == manifest.InstallLocation))
                        games.Add(manifest);
                }
                catch
                {
                    // ignored
                }
            }
            return games;
        });
}