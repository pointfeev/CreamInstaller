using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Threading.Tasks;

using CreamInstaller.Utility;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace CreamInstaller.Steam;

internal static class SteamStore
{
    internal static async Task<List<string>> ParseDlcAppIds(AppData appData) => await Task.Run(() =>
    {
        List<string> dlcIds = new();
        if (appData.dlc is null) return dlcIds;
        foreach (int appId in appData.dlc)
            dlcIds.Add(appId.ToString());
        return dlcIds;
    });

    internal static async Task<AppData> QueryStoreAPI(string appId)
    {
        if (Program.Canceled) return null;
        string response = await HttpClientManager.EnsureGet($"https://store.steampowered.com/api/appdetails?appids={appId}");
        string cacheFile = ProgramData.AppInfoPath + @$"\{appId}.json";
        if (response is not null)
        {
            IDictionary<string, JToken> apps = (dynamic)JsonConvert.DeserializeObject(response);
            foreach (KeyValuePair<string, JToken> app in apps)
            {
                try
                {
                    AppData data = JsonConvert.DeserializeObject<AppDetails>(app.Value.ToString()).data;
                    try
                    {
                        File.WriteAllText(cacheFile, JsonConvert.SerializeObject(data, Formatting.Indented));
                    }
                    catch { }
                    return data;
                }
                catch (Exception e)
                {
                    new DialogForm(null).Show(SystemIcons.Error, "Unsuccessful deserialization of query for appid " + appId + ":\n\n" + e.ToString(), "FUCK");
                }
            }
        }
        if (Directory.Exists(Directory.GetDirectoryRoot(cacheFile)) && File.Exists(cacheFile))
        {
            try
            {
                return JsonConvert.DeserializeObject<AppData>(File.ReadAllText(cacheFile));
            }
            catch { }
        }
        return null;
    }
}
