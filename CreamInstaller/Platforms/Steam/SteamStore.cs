using CreamInstaller.Utility;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

#if DEBUG
using System;
#endif

namespace CreamInstaller.Platforms.Steam;

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

    private const int COOLDOWN_GAME = 600;
    private const int COOLDOWN_DLC = 1200;

    internal static async Task<AppData> QueryStoreAPI(string appId, bool isDlc = false)
    {
        if (Program.Canceled) return null;
        string cacheFile = ProgramData.AppInfoPath + @$"\{appId}.json";
        bool cachedExists = File.Exists(cacheFile);
        if (!cachedExists || ProgramData.CheckCooldown(appId, isDlc ? COOLDOWN_DLC : COOLDOWN_GAME))
        {
            string response = await HttpClientManager.EnsureGet($"https://store.steampowered.com/api/appdetails?appids={appId}");
            if (response is not null)
            {
                IDictionary<string, JToken> apps = (IDictionary<string, JToken>)JsonConvert.DeserializeObject(response);
                if (apps is not null)
                    foreach (KeyValuePair<string, JToken> app in apps)
                        try
                        {
                            AppDetails appDetails = JsonConvert.DeserializeObject<AppDetails>(app.Value.ToString());
                            if (appDetails is not null)
                            {
                                AppData data = appDetails.data;
                                if (!appDetails.success)
                                {
#if DEBUG
                                    DebugForm.Current.Log($"Query unsuccessful for appid {appId}{(isDlc ? " (DLC)" : "")}: {app.Value.ToString(Formatting.None)}", LogTextBox.Warning);
#endif
                                    if (data is null)
                                        return null;
                                }
                                if (data is not null)
                                {
                                    try
                                    {
                                        File.WriteAllText(cacheFile, JsonConvert.SerializeObject(data, Formatting.Indented));
                                    }
                                    catch
#if DEBUG
                                    (Exception e)
                                    {
                                        DebugForm.Current.Log($"Unsuccessful serialization of query for appid {appId}{(isDlc ? " (DLC)" : "")}: {e.GetType()} ({e.Message})");
                                    }
#else
                                    { }
#endif
                                    return data;
                                }
#if DEBUG
                                else DebugForm.Current.Log($"Response data null for appid {appId}{(isDlc ? " (DLC)" : "")}: {app.Value.ToString(Formatting.None)}");
#endif
                            }
#if DEBUG
                            else DebugForm.Current.Log($"Response details null for appid {appId}{(isDlc ? " (DLC)" : "")}: {app.Value.ToString(Formatting.None)}");
#endif
                        }
                        catch
#if DEBUG
                        (Exception e)
                        {
                            DebugForm.Current.Log($"Unsuccessful deserialization of query for appid {appId}{(isDlc ? " (DLC)" : "")}: {e.GetType()} ({e.Message})");
                        }
#else
                        { }
#endif
#if DEBUG
                else DebugForm.Current.Log("Response deserialization null for appid " + appId);
#endif
            }
#if DEBUG
            else DebugForm.Current.Log("Response null for appid " + appId, LogTextBox.Warning);
#endif
        }
        if (cachedExists)
            try
            {
                return JsonConvert.DeserializeObject<AppData>(File.ReadAllText(cacheFile));
            }
            catch
            {
                File.Delete(cacheFile);
            }
        if (!isDlc)
        {
            Thread.Sleep(1000);
            return await QueryStoreAPI(appId, isDlc);
        }
        return null;
    }
}
