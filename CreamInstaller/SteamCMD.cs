using Gameloop.Vdf;
using Gameloop.Vdf.Linq;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;

namespace CreamInstaller
{
    public static class SteamCMD
    {
        public static string DirectoryPath = Path.GetTempPath() + "CreamInstaller";
        public static string FilePath = DirectoryPath + @"\steamcmd.exe";
        public static string ArchivePath = DirectoryPath + @"\steamcmd.zip";
        public static string DllPath = DirectoryPath + @"\steamclient.dll";
        public static string AppInfoCachePath = DirectoryPath + @"\appinfocache";

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
                CreateNoWindow = true
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
                using (WebClient webClient = new()) webClient.DownloadFile("https://steamcdn-a.akamaihd.net/client/installer/steamcmd.zip", ArchivePath);
                ZipFile.ExtractToDirectory(ArchivePath, DirectoryPath);
                File.Delete(ArchivePath);
            }
            if (!File.Exists(DllPath)) Run($@"+quit", out _);
        }

        public static bool GetAppInfo(int appId, int buildId, out VProperty appInfo)
        {
            appInfo = null;
            if (Program.Canceled) return false;
            string output;
            string appUpdatePath = $@"{AppInfoCachePath}\{appId}";
            string appUpdateFile = $@"{appUpdatePath}\appinfo.txt";
            if (Directory.Exists(appUpdatePath) && File.Exists(appUpdateFile)) output = File.ReadAllText(appUpdateFile);
            else
            {
                Run($@"+@ShutdownOnFailedCommand 0 +login anonymous +app_info_print {appId} +force_install_dir {appUpdatePath} +app_update 4 +quit", out _);
                Run($@"+@ShutdownOnFailedCommand 0 +login anonymous +app_info_print {appId} +quit", out output);
                int openBracket = output.IndexOf("{");
                int closeBracket = output.LastIndexOf("}");
                if (openBracket != -1 && closeBracket != -1)
                {
                    output = $"\"{appId}\"\n" + output.Substring(openBracket, 1 + closeBracket - openBracket);
                    File.WriteAllText(appUpdateFile, output);
                }
            }
            if (Program.Canceled || output is null) return false;
            appInfo = VdfConvert.Deserialize(output);
            if (appInfo?.Value?.Children()?.ToList()?.Count > 0) return true;
            VToken type = null;
            try { type = appInfo?.Value?["common"]?["type"]; } catch { }
            if (type is null || type.ToString() == "Game")
            {
                string buildid = null;
                try { buildid = appInfo.Value["depots"]["public"]["buildid"].ToString(); } catch { }
                if (buildid is null && !(type is null)) return true;
                if (type is null || int.Parse(buildid?.ToString()) < buildId || appInfo.Value["extended"] is null || appInfo.Value["depots"] is null)
                {
                    File.Delete(appUpdateFile);
                    bool success = GetAppInfo(appId, buildId, out appInfo);
                    return success;
                }
            }
            return true;
        }

        public static void Kill()
        {
            foreach (Process process in Process.GetProcessesByName("steamcmd")) process.Kill();
        }

        public static void Dispose()
        {
            Kill();
            if (Directory.Exists(DirectoryPath)) Directory.Delete(DirectoryPath, true);
        }
    }
}