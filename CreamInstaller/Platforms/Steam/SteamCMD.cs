using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CreamInstaller.Resources;
using CreamInstaller.Utility;
using Gameloop.Vdf.JsonConverter;
using Gameloop.Vdf.Linq;
#if DEBUG
using CreamInstaller.Forms;
#endif

namespace CreamInstaller.Platforms.Steam;

internal static partial class SteamCMD
{
    private const int ProcessLimit = 20;

    private static readonly string FilePath = DirectoryPath + @"\steamcmd.exe";

    private static readonly ConcurrentDictionary<string, int>
        AttemptCount = new(); // the more app_updates, the longer SteamCMD should wait for app_info_print

    private static readonly int[] Locks = new int[ProcessLimit];

    private static readonly string ArchivePath = DirectoryPath + @"\steamcmd.zip";
    private static readonly string DllPath = DirectoryPath + @"\steamclient.dll";

    private static readonly string AppCachePath = DirectoryPath + @"\appcache";

    private static string DirectoryPath => ProgramData.DirectoryPath;
    internal static string AppInfoPath => ProgramData.AppInfoPath;

    private static string GetArguments(string appId)
        => AttemptCount.TryGetValue(appId, out int attempts)
            ? $@"@ShutdownOnFailedCommand 0 +force_install_dir {DirectoryPath} +login anonymous +app_info_print {appId} "
              + string.Concat(Enumerable.Repeat("+app_update 4 ", attempts)) + "+quit"
            : $"+login anonymous +app_info_print {appId} +quit";

    private static async Task<string> Run(string appId)
        => await Task.Run(() =>
        {
            wait_for_lock:
            if (Program.Canceled)
                return "";
            for (int i = 0; i < Locks.Length; i++)
            {
                if (Program.Canceled)
                    return "";
                if (Interlocked.CompareExchange(ref Locks[i], 1, 0) != 0)
                    continue;
                if (appId != null)
                {
                    _ = AttemptCount.TryGetValue(appId, out int count);
                    AttemptCount[appId] = ++count;
                }

                if (Program.Canceled)
                    return "";
                ProcessStartInfo processStartInfo = new()
                {
                    FileName = FilePath, RedirectStandardOutput = true, RedirectStandardInput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false, Arguments = appId is null ? "+quit" : GetArguments(appId),
                    CreateNoWindow = true,
                    StandardInputEncoding = Encoding.UTF8, StandardOutputEncoding = Encoding.UTF8,
                    StandardErrorEncoding = Encoding.UTF8
                };
                Process process = Process.Start(processStartInfo);
                StringBuilder output = new();
                StringBuilder appInfo = new();
                bool appInfoStarted = false;
                DateTime lastOutput = DateTime.UtcNow;
                while (process != null)
                {
                    if (Program.Canceled)
                    {
                        process.Kill(true);
                        process.Close();
                        break;
                    }

                    int c = process.StandardOutput.Read();
                    if (c != -1)
                    {
                        lastOutput = DateTime.UtcNow;
                        char ch = (char)c;
                        if (ch == '{')
                            appInfoStarted = true;
                        _ = appInfoStarted ? appInfo.Append(ch) : output.Append(ch);
                    }

                    DateTime now = DateTime.UtcNow;
                    TimeSpan timeDiff = now - lastOutput;
                    if (!(timeDiff.TotalSeconds > 0.1))
                        continue;
                    process.Kill(true);
                    process.Close();
                    if (appId != null &&
                        output.ToString().Contains($"No app info for AppID {appId} found, requesting..."))
                    {
                        AttemptCount[appId]++;
                        processStartInfo.Arguments = GetArguments(appId);
                        process = Process.Start(processStartInfo);
                        appInfoStarted = false;
                        _ = output.Clear();
                        _ = appInfo.Clear();
                    }
                    else
                        break;
                }

                _ = Interlocked.Decrement(ref Locks[i]);
                return appInfo.ToString();
            }

            Thread.Sleep(200);
            goto wait_for_lock;
        });

    internal static async Task<bool> Setup(IProgress<int> progress)
    {
        await Cleanup();
        if (!FilePath.FileExists())
        {
            retryDownload:
            HttpClient httpClient = HttpClientManager.HttpClient;
            if (httpClient is null)
                return false;
            while (!Program.Canceled)
                try
                {
                    byte[] file =
                        await httpClient.GetByteArrayAsync(
                            new Uri("https://steamcdn-a.akamaihd.net/client/installer/steamcmd.zip"));
                    _ = file.WriteResource(ArchivePath);
                    ArchivePath.ExtractZip(DirectoryPath);
                    ArchivePath.DeleteFile();
                    break;
                }
                catch (Exception e)
                {
                    if (e.HandleException(caption: Program.Name + " failed to download SteamCMD"))
                        goto retryDownload;
                    return false;
                }
        }

        if (DllPath.FileExists())
            return true;
        FileSystemWatcher watcher = new(DirectoryPath)
            { Filter = "*", IncludeSubdirectories = true, EnableRaisingEvents = true };
        if (DllPath.FileExists())
            progress.Report(-15); // update (not used at the moment)
        else
            progress.Report(-1660); // install
        int cur = 0;
        progress.Report(cur);
        watcher.Changed += (_, _) => progress.Report(++cur);
        _ = await Run(null);
        watcher.Dispose();
        return true;
    }

    internal static async Task Cleanup()
        => await Task.Run(async () =>
        {
            if (!DirectoryPath.DirectoryExists())
                return;
            await Kill();
            try
            {
                AppCachePath.DeleteDirectory();
            }
            catch
            {
                // ignored
            }
        });

    internal static async Task<CmdAppData> GetAppInfo(string appId, string branch = "public", int buildId = 0)
    {
        CmdAppData data = await QueryWebAPI(appId);
        if (data is not null)
            return data;

        int attempts = 0;
        while (!Program.Canceled)
        {
            attempts++;
            if (attempts > 10)
            {
#if DEBUG
                DebugForm.Current.Log("Failed to query SteamCMD after 10 tries: " + appId + " (" + branch + ")",
                    LogTextBox.Warning);
#endif
                break;
            }

            string appUpdateFile = $@"{AppInfoPath}\{appId}.vdf";
            string output = appUpdateFile.ReadFile();
            if (output is null)
            {
                output = await Run(appId) ?? "";
                int openBracket = output.IndexOf('{');
                int closeBracket = output.LastIndexOf('}');
                if (openBracket != -1 && closeBracket != -1 && closeBracket > openBracket)
                {
                    output = $"\"{appId}\"\n" + output[openBracket..(1 + closeBracket)];
                    output = output.Replace("ERROR! Failed to install app '4' (Invalid platform)", "");
                    appUpdateFile.WriteFile(output);
                }
                else
                {
#if DEBUG
                    DebugForm.Current.Log(
                        "SteamCMD query failed on attempt #" + attempts + " for " + appId + " (" + branch +
                        "): Bad output",
                        LogTextBox.Warning);
#endif
                    continue;
                }
            }

            if (!ValveDataFile.TryDeserialize(output, out VProperty appInfo) || appInfo.Value is VValue)
            {
                appUpdateFile.DeleteFile();
#if DEBUG
                DebugForm.Current.Log(
                    "SteamCMD query failed on attempt #" + attempts + " for " + appId + " (" + branch +
                    "): Deserialization failed",
                    LogTextBox.Warning);
#endif
                continue;
            }

            CmdAppData appData;
            try
            {
                if (appInfo.ToJson().Value.ToObject<CmdAppData>() is not { } cmdAppData)
                {
                    appUpdateFile.DeleteFile();
#if DEBUG
                    DebugForm.Current.Log(
                        "SteamCMD query failed on attempt #" + attempts + " for " + appId + " (" + branch +
                        "): VDF-JSON conversion failed",
                        LogTextBox.Warning);
#endif
                    continue;
                }

                appData = cmdAppData;
            }
            catch
#if DEBUG
                (Exception e)
#endif
            {
                appUpdateFile.DeleteFile();
#if DEBUG
                DebugForm.Current.Log(
                    "SteamCMD query failed on attempt #" + attempts + " for " + appId + " (" + branch +
                    "): VDF-JSON conversion failed (" + e.Message + ")",
                    LogTextBox.Warning);
#endif
                continue;
            }

            string type = appData.Common?.Type;
            if (type is not null && type != "Game")
                return appData;
            if (appData.Depots is null || !appData.Depots.TryGetValue("branches", out dynamic appBranch))
                return appData;
            string buildid = appBranch?[branch]?.buildid;
            if (buildid is null && type is not null)
                return appData;
            if (type is not null && (!int.TryParse(buildid, out int gamebuildId) || gamebuildId >= buildId))
                return appData;
            HashSet<string> dlcAppIds = await ParseDlcAppIds(appData);
            foreach (string dlcAppUpdateFile in dlcAppIds.Select(id => $@"{AppInfoPath}\{id}.vdf"))
                dlcAppUpdateFile.DeleteFile();
            appUpdateFile.DeleteFile();
#if DEBUG
            DebugForm.Current.Log(
                "SteamCMD query skipped on attempt #" + attempts + " for " + appId + " (" + branch +
                "): Outdated cache", LogTextBox.Warning);
#endif
        }

        return null;
    }

    internal static async Task<HashSet<string>> ParseDlcAppIds(CmdAppData appData)
        => await Task.Run(() =>
        {
            HashSet<string> dlcIds = [];
            if (Program.Canceled || appData is null)
                return dlcIds;

            CmdAppExtended extended = appData.Extended;
            if (extended?.Dlc != null)
                foreach (string id in extended.Dlc.Split(","))
                    if (int.TryParse(id, out int appId) && appId > 0)
                        _ = dlcIds.Add("" + appId);

            Dictionary<string, dynamic> depots = appData.Depots;
            if (depots is null)
                return dlcIds;

            foreach ((_, dynamic depot) in depots.Where(p => int.TryParse(p.Key, out _)))
            {
                string dlcAppId = depot.dlcappid;
                if (dlcAppId is not null && int.TryParse(dlcAppId, out int appId) && appId > 0)
                    _ = dlcIds.Add("" + appId);
            }

            return dlcIds;
        });

    private static async Task Kill()
    {
        List<Task> tasks = Process.GetProcessesByName("steamcmd").Select(process => Task.Run(() =>
        {
            try
            {
                process.Kill(true);
                process.WaitForExit();
                process.Close();
            }
            catch
            {
                // ignored
            }
        })).ToList();
        foreach (Task task in tasks)
            await task;
    }
}