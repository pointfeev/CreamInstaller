using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Threading;
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

    private static readonly ConcurrentDictionary<string, DateTime> lastQueries = new();
    internal static async Task<AppData> QueryStoreAPI(string appId, int cooldown = 10)
    {
        if (Program.Canceled) return null;
        Thread.Sleep(0);
        string cacheFile = ProgramData.AppInfoPath + @$"\{appId}.json";
        DateTime now = DateTime.UtcNow;
        if (!lastQueries.TryGetValue(appId, out DateTime lastQuery) || (now - lastQuery).TotalSeconds > cooldown)
        {
            lastQueries[appId] = now;
            string response = await HttpClientManager.EnsureGet($"https://store.steampowered.com/api/appdetails?appids={appId}");
            if (response is not null)
            {
                IDictionary<string, JToken> apps = (dynamic)JsonConvert.DeserializeObject(response);
                foreach (KeyValuePair<string, JToken> app in apps)
                {
                    Thread.Sleep(0);
                    try
                    {
                        AppData data = JsonConvert.DeserializeObject<AppDetails>(app.Value.ToString()).data;
                        if (data is not null)
                        {
                            try
                            {
                                File.WriteAllText(cacheFile, JsonConvert.SerializeObject(data, Formatting.Indented));
                            }
                            catch //(Exception e)
                            {
                                //using DialogForm dialogForm = new(null);
                                //dialogForm.Show(SystemIcons.Error, "Unsuccessful serialization of query for appid " + appId + ":\n\n" + e.ToString(), "FUCK");
                            }
                            return data;
                        }
                    }
                    catch //(Exception e)
                    {
                        //using DialogForm dialogForm = new(null);
                        //dialogForm.Show(SystemIcons.Error, "Unsuccessful deserialization of query for appid " + appId + ":\n\n" + e.ToString(), "FUCK");
                    }
                }
            }
        }
        if (Directory.Exists(Directory.GetDirectoryRoot(cacheFile)) && File.Exists(cacheFile))
        {
            try
            {
                return JsonConvert.DeserializeObject<AppData>(File.ReadAllText(cacheFile));
            }
            catch
            {
                File.Delete(cacheFile);
            }
        }
        return null;
    }
}
