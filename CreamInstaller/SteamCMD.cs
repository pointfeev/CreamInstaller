using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
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

        public static bool GetAppInfo(int steamAppId, out Dictionary<string, string> appInfo)
        {
            appInfo = new();
            if (Program.Canceled) return false;
            string output;
            string appUpdatePath = $@"{AppInfoCachePath}\{steamAppId}";
            string appUpdateFile = $@"{appUpdatePath}\appinfo.txt";
            //if (Directory.Exists(appUpdatePath) && File.Exists(appUpdateFile)) output = File.ReadAllText(appUpdateFile);
            //else
            //{
            Run($@"+@ShutdownOnFailedCommand 0 +login anonymous +app_info_print {steamAppId} +force_install_dir {appUpdatePath} +app_update 4 +quit", out _);
            Run($@"+@ShutdownOnFailedCommand 0 +login anonymous +app_info_print {steamAppId} +quit", out output);
            File.WriteAllText(appUpdateFile, output);
            //}
            if (Program.Canceled || output is null) return false;
            foreach (string s in output.Split(new string[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries))
            {
                if (Program.Canceled) return false;
                int first = s.IndexOf("\"");
                int second = 1 + first + s.Substring(first + 1).IndexOf("\"");
                int third = 1 + second + s.Substring(second + 1).IndexOf("\"");
                int fourth = 1 + third + s.Substring(third + 1).IndexOf("\"");
                if (first > -1 && second > 0 && third > 0 && fourth > 0)
                {
                    string a = s.Substring(first + 1, Math.Max(second - first - 1, 0));
                    string b = s.Substring(third + 1, Math.Max(fourth - third - 1, 0));
                    if (string.IsNullOrWhiteSpace(a) || string.IsNullOrWhiteSpace(b)) continue;
                    if (!appInfo.TryGetValue(a, out _)) appInfo.Add(a, b);
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