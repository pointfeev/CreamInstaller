using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CreamInstaller.Platforms.Epic.Heroic;
using CreamInstaller.Utility;
using Microsoft.Win32;
using Newtonsoft.Json;

namespace CreamInstaller.Platforms.Epic;

internal static class EpicLibrary
{
    private static string epicManifestsPath;

    internal static string EpicManifestsPath
    {
        get
        {
            epicManifestsPath ??=
                Registry.GetValue(@"HKEY_CURRENT_USER\Software\Epic Games\EOS", "ModSdkMetadataDir", null) as string;
            epicManifestsPath ??=
                Registry.GetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Epic Games\EpicGamesLauncher", "AppDataPath",
                    null) as string;
            if (epicManifestsPath is not null && epicManifestsPath.EndsWith(@"\Data", StringComparison.Ordinal))
                epicManifestsPath += @"\Manifests";
            return epicManifestsPath.ResolvePath();
        }
    }

    internal static async Task<List<Manifest>> GetGames()
        => await Task.Run(async () =>
        {
            List<Manifest> games = new();
            string manifests = EpicManifestsPath;
            if (manifests.DirectoryExists())
                foreach (string item in manifests.EnumerateDirectory("*.item"))
                {
                    if (Program.Canceled)
                        return games;
                    string json = item.ReadFile();
                    try
                    {
                        Manifest manifest = JsonConvert.DeserializeObject<Manifest>(json);
                        if (manifest is not null && (manifest.InstallLocation = manifest.InstallLocation.ResolvePath())
                                                 is not null
                                                 && games.All(g => g.CatalogNamespace != manifest.CatalogNamespace))
                            games.Add(manifest);
                    }
                    catch
                    {
                        // ignored
                    }
                }

            if (Program.Canceled)
                return games;
            await HeroicLibrary.GetGames(games);
            return games;
        });
}