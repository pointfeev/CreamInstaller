using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CreamInstaller.Forms;
using CreamInstaller.Utility;
using Newtonsoft.Json;

namespace CreamInstaller.Platforms.Steam;

internal static partial class SteamCMD
{
    private const int CooldownGame = 600;
    private const int CooldownDlc = 1200;

    private static async Task<CmdAppData> QueryWebAPI(string appId, bool isDlc = false, int attempts = 0)
    {
        while (!Program.Canceled)
        {
            attempts++;
            string cacheFile = ProgramData.AppInfoPath + @$"\{appId}.cmd.json";
            bool cachedExists = cacheFile.FileExists();
            if (!cachedExists || ProgramData.CheckCooldown(appId + ".cmd", isDlc ? CooldownDlc : CooldownGame))
            {
                string response =
                    await HttpClientManager.EnsureGet($"https://api.steamcmd.net/v1/info/{appId}");
                if (response is not null)
                {
                    try
                    {
                        CmdAppDetails appDetails = JsonConvert.DeserializeObject<CmdAppDetails>(response);
                        if (appDetails is not null && appDetails.Status == "success")
                        {
                            if (appDetails.Data.Values.Count != 0)
                            {
                                CmdAppData data = appDetails.Data.Values.First();
                                try
                                {
                                    cacheFile.WriteFile(JsonConvert.SerializeObject(data, Formatting.Indented));
                                }
                                catch
#if DEBUG
                                    (Exception e)
                                {
                                    DebugForm.Current.Log("SteamCMD web API query failed on attempt #" + attempts +
                                                          " for " + appId + (isDlc ? " (DLC)" : "")
                                                          + ": Unsuccessful serialization (" + e.Message + ")");
                                }
#else
                                {
                                    // ignored
                                }
#endif
                                return data;
                            }
#if DEBUG
                            else
                                DebugForm.Current.Log(
                                    "SteamCMD web API query failed on attempt #" + attempts + " for " + appId +
                                    (isDlc ? " (DLC)" : "")
                                    + ": No data",
                                    LogTextBox.Warning);
#endif
                        }
#if DEBUG
                        else
                            DebugForm.Current.Log(
                                "SteamCMD web API query failed on attempt #" + attempts + " for " + appId +
                                (isDlc ? " (DLC)" : "")
                                + ": Status not success (" + appDetails?.Status + ")",
                                LogTextBox.Warning);
#endif
                    }
                    catch
#if DEBUG
                        (Exception e)
                    {
                        DebugForm.Current.Log("SteamCMD web API query failed on attempt #" + attempts + " for " +
                                              appId + (isDlc ? " (DLC)" : "")
                                              + ": Unsuccessful deserialization (" + e.Message + ")");
                    }
#else
                    {
                        // ignored
                    }
#endif
                }
#if DEBUG
                else
                    DebugForm.Current.Log(
                        "SteamCMD web API query failed on attempt #" + attempts + " for " + appId +
                        (isDlc ? " (DLC)" : "") +
                        ": Response null",
                        LogTextBox.Warning);
#endif
            }

            if (cachedExists)
                try
                {
                    return JsonConvert.DeserializeObject<CmdAppData>(cacheFile.ReadFile());
                }
                catch
                {
                    cacheFile.DeleteFile();
                }

            if (isDlc)
                break;
            if (attempts > 10)
            {
#if DEBUG
                DebugForm.Current.Log("Failed to query SteamCMD web API after 10 tries: " + appId);
#endif
                break;
            }

            Thread.Sleep(1000);
        }

        return null;
    }
}