using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

using Gameloop.Vdf;
using Gameloop.Vdf.Linq;

namespace CreamInstaller
{
    internal static class SteamCMD
    {
        internal static readonly string DirectoryPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"\CreamInstaller";
        internal static readonly string FilePath = DirectoryPath + @"\steamcmd.exe";
        internal static readonly string ArchivePath = DirectoryPath + @"\steamcmd.zip";
        internal static readonly string DllPath = DirectoryPath + @"\steamclient.dll";
        internal static readonly string AppCachePath = DirectoryPath + @"\appcache";
        internal static readonly string AppCacheAppInfoPath = AppCachePath + @"\appinfo.vdf";
        internal static readonly string AppInfoPath = DirectoryPath + @"\appinfo";

        internal static readonly Version MinimumAppInfoVersion = Version.Parse("2.0.3.2");
        internal static readonly string AppInfoVersionPath = AppInfoPath + @"\version.txt";

        internal static async Task<string> Run(string command) => await Task.Run(() =>
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
            using (Process process = Process.Start(processStartInfo))
            {
                process.OutputDataReceived += (object sender, DataReceivedEventArgs e) => logs.Add(e.Data);
                process.BeginOutputReadLine();
                process.ErrorDataReceived += (object sender, DataReceivedEventArgs e) => logs.Add(e.Data);
                process.BeginErrorReadLine();
                process.WaitForExit();
            }
            return string.Join("\r\n", logs);
        });

        internal static async Task Setup()
        {
            await Kill();
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
            if (File.Exists(AppCacheAppInfoPath)) File.Delete(AppCacheAppInfoPath);
            if (!File.Exists(AppInfoVersionPath) || !Version.TryParse(File.ReadAllText(AppInfoVersionPath, Encoding.UTF8), out Version version) || version < MinimumAppInfoVersion)
            {
                if (Directory.Exists(AppInfoPath)) Directory.Delete(AppInfoPath, true);
                Directory.CreateDirectory(AppInfoPath);
                File.WriteAllText(AppInfoVersionPath, Application.ProductVersion, Encoding.UTF8);
            }
            if (!File.Exists(DllPath)) await Run($@"+quit");
        }

        internal static async Task<VProperty> GetAppInfo(int appId, string branch = "public", int buildId = 0)
        {
            VProperty appInfo = null;
            if (Program.Canceled) return null;
            string output;
            string appUpdatePath = $@"{AppInfoPath}\{appId}";
            string appUpdateFile = $@"{appUpdatePath}\appinfo.txt";
        restart:
            if (Program.Canceled) return null;
            if (Directory.Exists(appUpdatePath) && File.Exists(appUpdateFile))
                output = File.ReadAllText(appUpdateFile, Encoding.UTF8);
            else
            {
                await Run($@"+@ShutdownOnFailedCommand 0 +login anonymous +app_info_print {appId} +force_install_dir {appUpdatePath} +app_update 4 +quit");
                output = await Run($@"+@ShutdownOnFailedCommand 0 +login anonymous +app_info_print {appId} +quit");
                int openBracket = output.IndexOf("{");
                int closeBracket = output.LastIndexOf("}");
                if (openBracket != -1 && closeBracket != -1)
                {
                    output = $"\"{appId}\"\n" + output[openBracket..(1 + closeBracket)];
                    File.WriteAllText(appUpdateFile, output, Encoding.UTF8);
                }
            }
            if (Program.Canceled || output is null) return null;
            try { appInfo = VdfConvert.Deserialize(output); }
            catch
            {
                if (Directory.Exists(appUpdatePath))
                {
                    Directory.Delete(appUpdatePath, true);
                    goto restart;
                }
            }
            if (appInfo.Value is VValue) goto restart;
            if (appInfo is null || (appInfo.Value is not VValue && appInfo.Value.Children().ToList().Count == 0))
                return appInfo;
            VToken type = appInfo.Value is VValue ? null : appInfo.Value?["common"]?["type"];
            if (type is null || type.ToString() == "Game")
            {
                string buildid = appInfo.Value is VValue ? null : appInfo.Value["depots"]?["branches"]?[branch]?["buildid"]?.ToString();
                if (buildid is null && type is not null) return appInfo;
                if (type is null || int.Parse(buildid) < buildId)
                {
                    List<int> dlcAppIds = await ParseDlcAppIds(appInfo);
                    foreach (int id in dlcAppIds)
                    {
                        string dlcAppUpdatePath = $@"{AppInfoPath}\{id}";
                        if (Directory.Exists(dlcAppUpdatePath)) Directory.Delete(dlcAppUpdatePath, true);
                    }
                    if (Directory.Exists(appUpdatePath)) Directory.Delete(appUpdatePath, true);
                    goto restart;
                }
            }
            return appInfo;
        }

        internal static async Task<List<int>> ParseDlcAppIds(VProperty appInfo) => await Task.Run(() =>
        {
            List<int> dlcIds = new();
            if (appInfo is not VProperty) return dlcIds;
            if (appInfo.Value["extended"] is not null)
                foreach (VProperty property in appInfo.Value["extended"])
                    if (property.Key.ToString() == "listofdlc")
                        foreach (string id in property.Value.ToString().Split(","))
                            if (!dlcIds.Contains(int.Parse(id)))
                                dlcIds.Add(int.Parse(id));
            if (appInfo.Value["depots"] is not null)
                foreach (VProperty _property in appInfo.Value["depots"])
                    if (int.TryParse(_property.Key.ToString(), out int _))
                        if (int.TryParse(_property.Value?["dlcappid"]?.ToString(), out int appid) && !dlcIds.Contains(appid))
                            dlcIds.Add(appid);
            return dlcIds;
        });

        internal static async Task Kill()
        {
            List<Task> tasks = new();
            foreach (Process process in Process.GetProcessesByName("steamcmd"))
            {
                process.Kill();
                tasks.Add(Task.Run(() => process.WaitForExit()));
            }
            foreach (Task task in tasks) await task;
        }

        internal static void Dispose()
        {
            Kill().Wait();
            if (Directory.Exists(DirectoryPath))
            {
                Directory.Delete(DirectoryPath, true);
            }
        }
    }
}