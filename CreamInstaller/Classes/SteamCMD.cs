using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

using CreamInstaller.Resources;

using Gameloop.Vdf.Linq;

namespace CreamInstaller.Classes;

internal static class SteamCMD
{
    internal static readonly int ProcessLimit = 20;
    internal static readonly Version MinimumAppInfoVersion = Version.Parse("2.3.3.0");

    internal static readonly string DirectoryPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"\CreamInstaller";
    internal static readonly string FilePath = DirectoryPath + @"\steamcmd.exe";

    private static readonly int[] locks = new int[ProcessLimit];
    internal static async Task<string> Run(string command) => await Task.Run(() =>
    {
    wait_for_lock:
        if (Program.Canceled) return "";
        for (int i = 0; i < locks.Length; i++)
        {
            if (Program.Canceled) return "";
            if (Interlocked.CompareExchange(ref locks[i], 1, 0) == 0)
            {
                if (Program.Canceled) return "";
                List<string> logs = new();
                ProcessStartInfo processStartInfo = new()
                {
                    FileName = FilePath,
                    RedirectStandardOutput = true,
                    RedirectStandardInput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    Arguments = command,
                    CreateNoWindow = true,
                    StandardInputEncoding = Encoding.UTF8,
                    StandardOutputEncoding = Encoding.UTF8,
                    StandardErrorEncoding = Encoding.UTF8
                };
                using Process process = Process.Start(processStartInfo);
                process.OutputDataReceived += (object sender, DataReceivedEventArgs e) => logs.Add(e.Data);
                process.BeginOutputReadLine();
                process.ErrorDataReceived += (object sender, DataReceivedEventArgs e) => logs.Add(e.Data);
                process.BeginErrorReadLine();
                process.WaitForExit();
                Interlocked.Decrement(ref locks[i]);
                return string.Join("\r\n", logs);
            }
            Thread.Sleep(0);
        }
        Thread.Sleep(200);
        goto wait_for_lock;
    });

    internal static readonly string ArchivePath = DirectoryPath + @"\steamcmd.zip";
    internal static readonly string DllPath = DirectoryPath + @"\steamclient.dll";
    internal static readonly string AppInfoPath = DirectoryPath + @"\appinfo";
    internal static readonly string AppInfoVersionPath = AppInfoPath + @"\version.txt";

    internal static async Task Setup(IProgress<int> progress = null)
    {
        await Cleanup();
        if (!Directory.Exists(DirectoryPath)) Directory.CreateDirectory(DirectoryPath);
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
        if (!File.Exists(AppInfoVersionPath) || !Version.TryParse(File.ReadAllText(AppInfoVersionPath, Encoding.UTF8), out Version version) || version < MinimumAppInfoVersion)
        {
            if (Directory.Exists(AppInfoPath)) Directory.Delete(AppInfoPath, true);
            Directory.CreateDirectory(AppInfoPath);
            File.WriteAllText(AppInfoVersionPath, Application.ProductVersion, Encoding.UTF8);
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
            await Run($@"+quit");
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

    internal static async Task<VProperty> GetAppInfo(int appId, string branch = "public", int buildId = 0)
    {
        if (Program.Canceled) return null;
        string output;
        string appUpdateFile = $@"{AppInfoPath}\{appId}.vdf";
    restart:
        if (Program.Canceled) return null;
        if (File.Exists(appUpdateFile)) output = File.ReadAllText(appUpdateFile, Encoding.UTF8);
        else
        {
            // we add app_update 4 to allow the app_info_print to finish
            output = await Run($@"@ShutdownOnFailedCommand 0 +force_install_dir {DirectoryPath} +login anonymous +app_info_print {appId} +app_update 4 +quit");
            int openBracket = output.IndexOf("{");
            int closeBracket = output.LastIndexOf("}");
            if (openBracket != -1 && closeBracket != -1)
            {
                output = $"\"{appId}\"\n" + output[openBracket..(1 + closeBracket)];
                File.WriteAllText(appUpdateFile, output, Encoding.UTF8);
            }
        }
        if (Program.Canceled || output is null) return null;
        if (!ValveDataFile.TryDeserialize(output, out VProperty appInfo))
        {
            File.Delete(appUpdateFile);
            //new DialogForm(null).Show("GetAppInfo", SystemIcons.Information, "Deserialize exception:\n\n" + output, "OK");
            goto restart;
        }
        if (appInfo.Value is VValue)
        {
            //new DialogForm(null).Show("GetAppInfo", SystemIcons.Information, "VValue exception:\n\n" + output, "OK");
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
                List<int> dlcAppIds = await ParseDlcAppIds(appInfo);
                foreach (int id in dlcAppIds)
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

    internal static async Task<List<int>> ParseDlcAppIds(VProperty appInfo) => await Task.Run(() =>
    {
        List<int> dlcIds = new();
#pragma warning disable IDE0150 // Prefer 'null' check over type check
        if (Program.Canceled || appInfo is not VProperty) return dlcIds;
#pragma warning restore IDE0150 // Prefer 'null' check over type check
        VToken extended = appInfo.Value.GetChild("extended");
        if (extended is not null)
            foreach (VProperty property in extended)
                if (property.Key == "listofdlc")
                    foreach (string id in property.Value.ToString().Split(","))
                        if (int.TryParse(id, out int appId) && !dlcIds.Contains(appId)) dlcIds.Add(appId);
        VToken depots = appInfo.Value.GetChild("depots");
        if (depots is not null) foreach (VProperty property in depots)
                if (int.TryParse(property.Key, out int _)
                    && int.TryParse(property.Value.GetChild("dlcappid")?.ToString(), out int appid)
                    && !dlcIds.Contains(appid))
                    dlcIds.Add(appid);
        return dlcIds;
    });

    internal static async Task Kill()
    {
        List<Task> tasks = new();
        foreach (Process process in Process.GetProcessesByName("steamcmd"))
        {
            tasks.Add(Task.Run(() =>
            {
                try
                {
                    process.Kill(true);
                    process.WaitForExit();
                }
                catch { }
            }));
        }
        foreach (Task task in tasks) await task;
    }

    internal static void Dispose()
    {
        Kill().Wait();
        if (Directory.Exists(DirectoryPath))
            Directory.Delete(DirectoryPath, true);
    }
}
