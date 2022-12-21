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

namespace CreamInstaller.Platforms.Steam;

internal static class SteamCMD
{
    internal const int ProcessLimit = 20;

    internal static readonly string FilePath = DirectoryPath + @"\steamcmd.exe";

    private static readonly ConcurrentDictionary<string, int>
        AttemptCount = new(); // the more app_updates, the longer SteamCMD should wait for app_info_print

    private static readonly int[] locks = new int[ProcessLimit];

    internal static readonly string ArchivePath = DirectoryPath + @"\steamcmd.zip";
    internal static readonly string DllPath = DirectoryPath + @"\steamclient.dll";

    internal static readonly string AppCachePath = DirectoryPath + @"\appcache";
    internal static readonly string ConfigPath = DirectoryPath + @"\config";
    internal static readonly string DumpsPath = DirectoryPath + @"\dumps";
    internal static readonly string LogsPath = DirectoryPath + @"\logs";
    internal static readonly string SteamAppsPath = DirectoryPath + @"\steamapps";

    internal static string DirectoryPath => ProgramData.DirectoryPath;
    internal static string AppInfoPath => ProgramData.AppInfoPath;

    private static string GetArguments(string appId) => AttemptCount.TryGetValue(appId, out int attempts)
        ? $@"@ShutdownOnFailedCommand 0 +force_install_dir {DirectoryPath} +login anonymous +app_info_print {appId} "
        + string.Concat(Enumerable.Repeat("+app_update 4 ", attempts)) + "+quit"
        : $"+login anonymous +app_info_print {appId} +quit";

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
                    AttemptCount.TryGetValue(appId, out int count);
                    AttemptCount[appId] = ++count;
                }
                if (Program.Canceled) return "";
                ProcessStartInfo processStartInfo = new()
                {
                    FileName = FilePath, RedirectStandardOutput = true, RedirectStandardInput = true,
                    RedirectStandardError = true, UseShellExecute = false,
                    Arguments = appId is null ? "+quit" : GetArguments(appId), CreateNoWindow = true,
                    StandardInputEncoding = Encoding.UTF8, StandardOutputEncoding = Encoding.UTF8,
                    StandardErrorEncoding = Encoding.UTF8
                };
                Process process = Process.Start(processStartInfo);
                StringBuilder output = new();
                StringBuilder appInfo = new();
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
                    int c = process.StandardOutput.Read();
                    if (c != -1)
                    {
                        lastOutput = DateTime.UtcNow;
                        char ch = (char)c;
                        if (ch == '{') appInfoStarted = true;
                        _ = appInfoStarted ? appInfo.Append(ch) : output.Append(ch);
                    }
                    DateTime now = DateTime.UtcNow;
                    TimeSpan timeDiff = now - lastOutput;
                    if (timeDiff.TotalSeconds > 0.1)
                    {
                        process.Kill(true);
                        process.Close();
                        if (output.ToString().Contains($"No app info for AppID {appId} found, requesting..."))
                        {
                            AttemptCount[appId]++;
                            processStartInfo.Arguments = GetArguments(appId);
                            process = Process.Start(processStartInfo);
                            appInfoStarted = false;
                            _ = output.Clear();
                            _ = appInfo.Clear();
                        }
                        else
                        {
                            break;
                        }
                    }
                }
                _ = Interlocked.Decrement(ref locks[i]);
                return appInfo.ToString();
            }
            Thread.Sleep(200);
        }
        Thread.Sleep(200);
        goto wait_for_lock;
    });

    internal static async Task Setup(IProgress<int> progress)
    {
        await Cleanup();
        if (!File.Exists(FilePath))
        {
            HttpClient httpClient = HttpClientManager.HttpClient;
            if (httpClient is null) return;
            byte[] file = await httpClient.GetByteArrayAsync(
                new Uri("https://steamcdn-a.akamaihd.net/client/installer/steamcmd.zip"));
            file.Write(ArchivePath);
            ZipFile.ExtractToDirectory(ArchivePath, DirectoryPath);
            File.Delete(ArchivePath);
        }
        if (!File.Exists(DllPath))
        {
            FileSystemWatcher watcher = new(DirectoryPath)
            {
                Filter = "*", IncludeSubdirectories = true, EnableRaisingEvents = true
            };
            if (File.Exists(DllPath)) progress.Report(-15); // update (not used at the moment)
            else progress.Report(-1660); // install
            int cur = 0;
            progress.Report(cur);
            watcher.Changed += (sender, e) => progress.Report(++cur);
            _ = await Run(null);
            watcher.Dispose();
        }
    }

    internal static async Task Cleanup() => await Task.Run(async () =>
    {
        if (!Directory.Exists(DirectoryPath)) return;
        await Kill();
        try
        {
            if (Directory.Exists(ConfigPath))
                foreach (string file in Directory.EnumerateFiles(ConfigPath, "*.tmp"))
                    File.Delete(file);
            foreach (string file in Directory.EnumerateFiles(DirectoryPath, "*.old"))
                File.Delete(file);
            foreach (string file in Directory.EnumerateFiles(DirectoryPath, "*.delete"))
                File.Delete(file);
            foreach (string file in Directory.EnumerateFiles(DirectoryPath, "*.crash"))
                File.Delete(file);
            foreach (string file in Directory.EnumerateFiles(DirectoryPath, "*.ntfs_transaction_failed"))
                File.Delete(file);
            if (Directory.Exists(AppCachePath))
                Directory.Delete(AppCachePath,
                                 true); // this is definitely needed, so SteamCMD gets the latest information for us
            if (Directory.Exists(DumpsPath))
                Directory.Delete(DumpsPath, true);
            if (Directory.Exists(LogsPath))
                Directory.Delete(LogsPath, true);
            if (Directory.Exists(SteamAppsPath))
                Directory.Delete(SteamAppsPath, true); // this is just a useless folder created from +app_update 4
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
        if (File.Exists(appUpdateFile))
        {
            try
            {
                output = await File.ReadAllTextAsync(appUpdateFile, Encoding.UTF8);
            }
            catch
            {
                goto restart;
            }
        }
        else
        {
            output = await Run(appId) ?? "";
            int openBracket = output.IndexOf("{", StringComparison.Ordinal);
            int closeBracket = output.LastIndexOf("}", StringComparison.Ordinal);
            if (openBracket != -1 && closeBracket != -1 && closeBracket > openBracket)
            {
                output = $"\"{appId}\"\n" + output[openBracket..(1 + closeBracket)];
                output = output.Replace("ERROR! Failed to install app '4' (Invalid platform)", "");
                try
                {
                    await File.WriteAllTextAsync(appUpdateFile, output, Encoding.UTF8);
                }
                catch
                {
                    goto restart;
                }
            }
            else
            {
                goto restart;
            }
        }
        if (Program.Canceled) return null;
        if (!ValveDataFile.TryDeserialize(output, out VProperty appInfo) || appInfo.Value is VValue)
        {
            File.Delete(appUpdateFile);
            goto restart;
        }
        if (appInfo is null || appInfo.Value?.Children()?.ToList()?.Count == 0) return appInfo;
        VToken type = appInfo.Value?.GetChild("common")?.GetChild("type");
        if (type is not null && type.ToString() != "Game")
            return appInfo;
        string buildid = appInfo.Value?.GetChild("depots")?.GetChild("branches")?.GetChild(branch)
                               ?.GetChild("buildid")?.ToString();
        if (buildid is null && type is not null) return appInfo;
        if (type is not null && (!int.TryParse(buildid, out int gamebuildId) || gamebuildId >= buildId))
            return appInfo;
        List<string> dlcAppIds = await ParseDlcAppIds(appInfo);
        foreach (string dlcAppUpdateFile in dlcAppIds.Select(id => $@"{AppInfoPath}\{id}.vdf"))
            if (File.Exists(dlcAppUpdateFile))
                File.Delete(dlcAppUpdateFile);
        if (File.Exists(appUpdateFile)) File.Delete(appUpdateFile);
        goto restart;
    }

    internal static async Task<List<string>> ParseDlcAppIds(VProperty appInfo) => await Task.Run(() =>
    {
        List<string> dlcIds = new();
        if (Program.Canceled || appInfo is null) return dlcIds;
        VToken extended = appInfo.Value.GetChild("extended");
        if (extended is not null)
            foreach (VToken vToken in extended.Where(p => p is VProperty { Key: "listofdlc" }))
            {
                VProperty property = (VProperty)vToken;
                foreach (string id in property.Value.ToString().Split(","))
                    if (int.TryParse(id, out int appId) && appId > 0 && !dlcIds.Contains("" + appId))
                        dlcIds.Add("" + appId);
            }
        VToken depots = appInfo.Value.GetChild("depots");
        if (depots is null)
            return dlcIds;
        foreach (VToken vToken in depots.Where(p => p is VProperty property && int.TryParse(property.Key, out int _)))
        {
            VProperty property = (VProperty)vToken;
            if (int.TryParse(property.Value.GetChild("dlcappid")?.ToString(), out int appId) && appId > 0
             && !dlcIds.Contains("" + appId))
                dlcIds.Add("" + appId);
        }
        return dlcIds;
    });

    private static async Task Kill()
    {
        List<Task> tasks = Process.GetProcessesByName("steamcmd")
                                  .Select(process => Task.Run(() =>
                                   {
                                       try
                                       {
                                           process.Kill(true);
                                           process.WaitForExit();
                                           process.Close();
                                       }
                                       catch { }
                                   }))
                                  .ToList();
        foreach (Task task in tasks) await task;
    }

    internal static void Dispose()
    {
        Kill().Wait();
        if (Directory.Exists(DirectoryPath))
            Directory.Delete(DirectoryPath, true);
    }
}
