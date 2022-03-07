using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using CreamInstaller.Resources;
using CreamInstaller.Utility;

using Gameloop.Vdf.Linq;

namespace CreamInstaller.Steam;

internal static class SteamCMD
{
    internal static readonly int ProcessLimit = 20;

    internal static string DirectoryPath => ProgramData.DirectoryPath;
    internal static string AppInfoPath => ProgramData.AppInfoPath;

    internal static readonly string FilePath = DirectoryPath + @"\steamcmd.exe";

    private static readonly ConcurrentDictionary<string, int> AttemptCount = new(); // the more app_updates, the longer SteamCMD should wait for app_info_print
    private static string GetArguments(string appId) => AttemptCount.TryGetValue(appId, out int attempts)
        ? $@"@ShutdownOnFailedCommand 0 +force_install_dir {DirectoryPath} +login anonymous +app_info_print {appId} " + string.Concat(Enumerable.Repeat("+app_update 4 ", attempts)) + "+quit"
        : $"+login anonymous +app_info_print {appId} +quit";

    private static readonly int[] locks = new int[ProcessLimit];
    internal static async Task<string> Run(string appId) => await Task.Run(() =>
    {
    wait_for_lock:
        if (Program.Canceled) return "";
        for (int i = 0; i < locks.Length; i++)
        {
            if (Program.Canceled) return "";
            if (Interlocked.CompareExchange(ref locks[i], 1, 0) == 0)
            {
                if (appId is not null)
                {
                    if (AttemptCount.ContainsKey(appId))
                        AttemptCount[appId]++;
                    else
                        AttemptCount[appId] = 0;
                }
                if (Program.Canceled) return "";
                List<string> logs = new();
                ProcessStartInfo processStartInfo = new()
                {
                    FileName = FilePath,
                    RedirectStandardOutput = true,
                    RedirectStandardInput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    Arguments = appId is null ? "+quit" : GetArguments(appId),
                    CreateNoWindow = true,
                    StandardInputEncoding = Encoding.UTF8,
                    StandardOutputEncoding = Encoding.UTF8,
                    StandardErrorEncoding = Encoding.UTF8
                };
                Process process = Process.Start(processStartInfo);
                string output = "";
                string appInfo = "";
                bool appInfoStarted = false;
                DateTime lastOutput = DateTime.UtcNow;
                while (true)
                {
                    if (Program.Canceled)
                    {
                        process.Kill(true);
                        process.Close();
                        break;
                    }
                    Thread.Sleep(0);
                    int c = process.StandardOutput.Read();
                    if (c != -1)
                    {
                        lastOutput = DateTime.UtcNow;
                        char ch = (char)c;
                        if (ch == '{') appInfoStarted = true;
                        if (appInfoStarted) appInfo += ch;
                        else output += ch;
                    }
                    DateTime now = DateTime.UtcNow;
                    TimeSpan timeDiff = now - lastOutput;
                    if (timeDiff.TotalSeconds > 0.1)
                    {
                        process.Kill(true);
                        process.Close();
                        if (output.Contains($"No app info for AppID {appId} found, requesting..."))
                        {
                            AttemptCount[appId]++;
                            processStartInfo.Arguments = GetArguments(appId);
                            process = Process.Start(processStartInfo);
                            appInfoStarted = false;
                            output = "";
                            appInfo = "";
                        }
                        else break;
                    }
                }
                Interlocked.Decrement(ref locks[i]);
                return appInfo;
            }
            Thread.Sleep(0);
        }
        Thread.Sleep(200);
        goto wait_for_lock;
    });

    internal static readonly string ArchivePath = DirectoryPath + @"\steamcmd.zip";
    internal static readonly string DllPath = DirectoryPath + @"\steamclient.dll";

    internal static async Task Setup(IProgress<int> progress = null)
    {
        await Cleanup();
        if (!File.Exists(FilePath))
        {
            using (HttpClient httpClient = new())
            {
                byte[] file = await httpClient.GetByteArrayAsync("https://steamcdn-a.akamaihd.net/client/installer/steamcmd.zip");
                file.Write(ArchivePath);
            }
            ZipFile.ExtractToDirectory(ArchivePath, DirectoryPath);
            File.Delete(ArchivePath);
        }
        if (!File.Exists(DllPath))
        {
            FileSystemWatcher watcher = new(DirectoryPath);
            watcher.Filter = "*";
            watcher.IncludeSubdirectories = true;
            watcher.EnableRaisingEvents = true;
            if (File.Exists(DllPath)) progress.Report(-15); // update (not used at the moment)
            else progress.Report(-1660); // install
            int cur = 0;
            progress.Report(cur);
            watcher.Changed += (sender, e) => progress.Report(++cur);
            await Run(null);
            watcher.Dispose();
        }
    }

    internal static readonly string AppCachePath = DirectoryPath + @"\appcache";
    internal static readonly string ConfigPath = DirectoryPath + @"\config";
    internal static readonly string DumpsPath = DirectoryPath + @"\dumps";
    internal static readonly string LogsPath = DirectoryPath + @"\logs";
    internal static readonly string SteamAppsPath = DirectoryPath + @"\steamapps";
    internal static readonly string UserDataPath = DirectoryPath + @"\userdata";

    internal static async Task Cleanup() => await Task.Run(async () =>
    {
        if (!Directory.Exists(DirectoryPath)) return;
        await Kill();
        try
        {
            string[] oldFiles = Directory.GetFiles(DirectoryPath, "*.old");
            foreach (string file in oldFiles) File.Delete(file);
        }
        catch { }
        try
        {
            string[] deleteFiles = Directory.GetFiles(DirectoryPath, "*.delete");
            foreach (string file in deleteFiles) File.Delete(file);
        }
        catch { }
        try
        {
            string[] crashFiles = Directory.GetFiles(DirectoryPath, "*.crash");
            foreach (string file in crashFiles) File.Delete(file);
        }
        catch { }
        try
        {
            if (Directory.Exists(AppCachePath)) Directory.Delete(AppCachePath, true);
        }
        catch { }
        try
        {
            if (Directory.Exists(ConfigPath)) Directory.Delete(ConfigPath, true);
        }
        catch { }
        try
        {
            if (Directory.Exists(DumpsPath)) Directory.Delete(DumpsPath, true);
        }
        catch { }
        try
        {
            if (Directory.Exists(LogsPath)) Directory.Delete(LogsPath, true);
        }
        catch { }
        try
        {
            if (Directory.Exists(SteamAppsPath)) Directory.Delete(SteamAppsPath, true);
        }
        catch { }
        try
        {
            if (Directory.Exists(UserDataPath)) Directory.Delete(UserDataPath, true);
        }
        catch { }
    });

    internal static async Task<VProperty> GetAppInfo(string appId, string branch = "public", int buildId = 0)
    {
        if (Program.Canceled) return null;
        string output;
        string appUpdateFile = $@"{AppInfoPath}\{appId}.vdf";
    restart:
        if (Program.Canceled) return null;
        if (File.Exists(appUpdateFile)) output = File.ReadAllText(appUpdateFile, Encoding.UTF8);
        else
        {
            output = await Run(appId);
            int openBracket = output.IndexOf("{");
            int closeBracket = output.LastIndexOf("}");
            if (openBracket != -1 && closeBracket != -1)
            {
                output = $"\"{appId}\"\n" + output[openBracket..(1 + closeBracket)];
                output = output.Replace("ERROR! Failed to install app '4' (Invalid platform)", "");
                File.WriteAllText(appUpdateFile, output, Encoding.UTF8);
            }
            else goto restart;
        }
        if (Program.Canceled || output is null) return null;
        if (!ValveDataFile.TryDeserialize(output, out VProperty appInfo) || appInfo.Value is VValue)
        {
            File.Delete(appUpdateFile);
            goto restart;
        }
        if (appInfo is null || appInfo.Value?.Children()?.ToList()?.Count == 0) return appInfo;
        VToken type = appInfo.Value?.GetChild("common")?.GetChild("type");
        if (type is null || type.ToString() == "Game")
        {
            string buildid = appInfo.Value?.GetChild("depots")?.GetChild("branches")?.GetChild(branch)?.GetChild("buildid")?.ToString();
            if (buildid is null && type is not null) return appInfo;
            if (type is null || int.TryParse(buildid, out int gamebuildId) && gamebuildId < buildId)
            {
                List<string> dlcAppIds = await ParseDlcAppIds(appInfo);
                foreach (string id in dlcAppIds)
                {
                    string dlcAppUpdateFile = $@"{AppInfoPath}\{id}.vdf";
                    if (File.Exists(dlcAppUpdateFile)) File.Delete(dlcAppUpdateFile);
                }
                if (File.Exists(appUpdateFile)) File.Delete(appUpdateFile);
                goto restart;
            }
        }
        return appInfo;
    }

    internal static async Task<List<string>> ParseDlcAppIds(VProperty appInfo) => await Task.Run(() =>
    {
        List<string> dlcIds = new();
#pragma warning disable IDE0150 // Prefer 'null' check over type check
        if (Program.Canceled || appInfo is not VProperty) return dlcIds;
#pragma warning restore IDE0150 // Prefer 'null' check over type check
        VToken extended = appInfo.Value.GetChild("extended");
        if (extended is not null)
            foreach (VProperty property in extended)
                if (property.Key == "listofdlc")
                    foreach (string id in property.Value.ToString().Split(","))
                        if (int.TryParse(id, out int appId)
                            && !dlcIds.Contains("" + appId))
                            dlcIds.Add("" + appId);
        VToken depots = appInfo.Value.GetChild("depots");
        if (depots is not null) foreach (VProperty property in depots)
                if (int.TryParse(property.Key, out int _)
                    && int.TryParse(property.Value.GetChild("dlcappid")?.ToString(), out int appid)
                    && !dlcIds.Contains("" + appid))
                    dlcIds.Add("" + appid);
        return dlcIds;
    });

    internal static async Task Kill()
    {
        List<Task> tasks = new();
        foreach (Process process in Process.GetProcessesByName("steamcmd"))
            tasks.Add(Task.Run(() =>
            {
                try
                {
                    process.Kill(true);
                    process.WaitForExit();
                    process.Close();
                }
                catch { }
            }));
        foreach (Task task in tasks) await task;
    }

    internal static void Dispose()
    {
        Kill().Wait();
        if (Directory.Exists(DirectoryPath))
            Directory.Delete(DirectoryPath, true);
    }
}
