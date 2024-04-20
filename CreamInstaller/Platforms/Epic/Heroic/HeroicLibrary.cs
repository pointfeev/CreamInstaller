using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CreamInstaller.Utility;
using Newtonsoft.Json.Linq;

namespace CreamInstaller.Platforms.Epic.Heroic;

internal static class HeroicLibrary
{
    internal static readonly string HeroicLibraryPath
        = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) +
          @"\heroic\store_cache\legendary_library.json";

    internal static async Task GetGames(List<Manifest> games)
        => await Task.Run(() =>
        {
            string libraryPath = HeroicLibraryPath;
            if (!libraryPath.FileExists())
                return;
            string libraryJson = libraryPath.ReadFile();
            try
            {
                JObject library = JObject.Parse(libraryJson);
                if (!library.TryGetValue("library", out JToken libraryToken) || libraryToken is not JArray libraryArray)
                    return;
                foreach (JToken token in libraryArray)
                    try
                    {
                        HeroicAppData appData = token.ToObject<HeroicAppData>();
                        if (appData is null || string.IsNullOrWhiteSpace(appData.Install.InstallPath =
                                appData.Install.InstallPath.ResolvePath()))
                            continue;
                        Manifest manifest = new()
                        {
                            DisplayName = appData.Title, CatalogNamespace = appData.Namespace,
                            InstallLocation = appData.Install.InstallPath
                        };
                        if (games.All(g => g.CatalogNamespace != manifest.CatalogNamespace))
                            games.Add(manifest);
                    }
                    catch
                    {
                        // ignored
                    }
            }
            catch
            {
                // ignored
            }
        });
}