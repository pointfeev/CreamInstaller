using Gameloop.Vdf;
using Gameloop.Vdf.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Windows.Forms;

namespace CreamInstaller
{
    public static class SteamCMD
    {
        public static readonly string DirectoryPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"\CreamInstaller";
        public static readonly string FilePath = DirectoryPath + @"\steamcmd.exe";
        public static readonly string ArchivePath = DirectoryPath + @"\steamcmd.zip";
        public static readonly string DllPath = DirectoryPath + @"\steamclient.dll";
        public static readonly string AppCachePath = DirectoryPath + @"\appcache";
        public static readonly string AppCacheAppInfoPath = AppCachePath + @"\appinfo.vdf";
        public static readonly string AppInfoPath = DirectoryPath + @"\appinfo";

        public static readonly Version MinimumAppInfoVersion = Version.Parse("2.0.3.2");
        public static readonly string AppInfoVersionPath = AppInfoPath + @"\version.txt";

        public static bool Run(string command, out string output)
        {
            bool success = true;
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
            output = string.Join("\r\n", logs);
            return success;
        }

        public static void Setup()
        {
            Kill();
            if (!File.Exists(FilePath))
            {
                using (HttpClient httpClient = new())
                {
                    byte[] file = httpClient.GetByteArrayAsync("https://steamcdn-a.akamaihd.net/client/installer/steamcmd.zip").Result;
                    file.Write(ArchivePath);
                }

                ZipFile.ExtractToDirectory(ArchivePath, DirectoryPath);
                File.Delete(ArchivePath);
            }
            if (File.Exists(AppCacheAppInfoPath))
            {
                File.Delete(AppCacheAppInfoPath);
            }

            if (!File.Exists(AppInfoVersionPath) || !Version.TryParse(File.ReadAllText(AppInfoVersionPath, Encoding.UTF8), out Version version) || version < MinimumAppInfoVersion)
            {
                if (Directory.Exists(AppInfoPath))
                {
                    Directory.Delete(AppInfoPath, true);
                }

                Directory.CreateDirectory(AppInfoPath);
                File.WriteAllText(AppInfoVersionPath, Application.ProductVersion, Encoding.UTF8);
            }
            if (!File.Exists(DllPath))
            {
                Run($@"+quit", out _);
            }
        }

        public static bool GetAppInfo(int appId, out VProperty appInfo, string branch = "public", int buildId = 0)
        {
            appInfo = null;
            if (Program.Canceled)
            {
                return false;
            }

            string output;
            string appUpdatePath = $@"{AppInfoPath}\{appId}";
            string appUpdateFile = $@"{appUpdatePath}\appinfo.txt";
        restart:
            if (Directory.Exists(appUpdatePath) && File.Exists(appUpdateFile))
            {
                output = File.ReadAllText(appUpdateFile, Encoding.UTF8);
            }
            else
            {
                Run($@"+@ShutdownOnFailedCommand 0 +login anonymous +app_info_print {appId} +force_install_dir {appUpdatePath} +app_update 4 +quit", out _);
                Run($@"+@ShutdownOnFailedCommand 0 +login anonymous +app_info_print {appId} +quit", out output);
                int openBracket = output.IndexOf("{");
                int closeBracket = output.LastIndexOf("}");
                if (openBracket != -1 && closeBracket != -1)
                {
                    output = $"\"{appId}\"\n" + output[openBracket..(1 + closeBracket)];
                    File.WriteAllText(appUpdateFile, output, Encoding.UTF8);
                }
            }
            if (Program.Canceled || output is null)
            {
                return false;
            }

            try { appInfo = VdfConvert.Deserialize(output); }
            catch
            {
                if (Directory.Exists(appUpdatePath))
                {
                    Directory.Delete(appUpdatePath, true);
                    goto restart;
                }
            }
            if (appInfo.Value is VValue)
            {
                goto restart;
            }

            if (appInfo is null || (!(appInfo.Value is VValue) && appInfo.Value.Children().ToList().Count == 0))
            {
                return true;
            }

            VToken type = appInfo.Value is VValue ? null : appInfo.Value?["common"]?["type"];
            if (type is null || type.ToString() == "Game")
            {
                string buildid = appInfo.Value is VValue ? null : appInfo.Value["depots"]?["branches"]?[branch]?["buildid"]?.ToString();
                if (buildid is null && !(type is null))
                {
                    return true;
                }

                if (type is null || int.Parse(buildid) < buildId)
                {
                    foreach (int id in ParseDlcAppIds(appInfo))
                    {
                        string dlcAppUpdatePath = $@"{AppInfoPath}\{id}";
                        if (Directory.Exists(dlcAppUpdatePath))
                        {
                            Directory.Delete(dlcAppUpdatePath, true);
                        }
                    }
                    if (Directory.Exists(appUpdatePath))
                    {
                        Directory.Delete(appUpdatePath, true);
                    }

                    goto restart;
                }
            }
            return true;
        }

        public static List<int> ParseDlcAppIds(VProperty appInfo)
        {
            List<int> dlcIds = new();
            if (!(appInfo is VProperty))
            {
                return dlcIds;
            }

            if (!(appInfo.Value["extended"] is null))
            {
                foreach (VProperty property in appInfo.Value["extended"])
                {
                    if (property.Key.ToString() == "listofdlc")
                    {
                        foreach (string id in property.Value.ToString().Split(","))
                        {
                            if (!dlcIds.Contains(int.Parse(id)))
                            {
                                dlcIds.Add(int.Parse(id));
                            }
                        }
                    }
                }
            }

            if (!(appInfo.Value["depots"] is null))
            {
                foreach (VProperty _property in appInfo.Value["depots"])
                {
                    if (int.TryParse(_property.Key.ToString(), out int _))
                    {
                        if (int.TryParse(_property.Value?["dlcappid"]?.ToString(), out int appid) && !dlcIds.Contains(appid))
                        {
                            dlcIds.Add(appid);
                        }
                    }
                }
            }

            return dlcIds;
        }

        public static void Kill()
        {
            foreach (Process process in Process.GetProcessesByName("steamcmd"))
            {
                process.Kill();
            }
        }

        public static void Dispose()
        {
            Kill();
            if (Directory.Exists(DirectoryPath))
            {
                Directory.Delete(DirectoryPath, true);
            }
        }
    }
}