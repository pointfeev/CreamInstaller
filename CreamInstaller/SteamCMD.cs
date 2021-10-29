using SteamCmdFluentApi;
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
        public static string FilePath = DirectoryPath + "\\steamcmd.exe";
        public static string ArchivePath = DirectoryPath + "\\steamcmd.zip";
        public static string DllPath = DirectoryPath + "\\steamclient.dll";

        private static SteamCmd current = null;

        public static SteamCmd Current
        {
            get
            {
                if (current is null) current = SteamCmd.WithExecutable(FilePath);
                return current;
            }
        }

        public static void Setup()
        {
            Kill();
            if (!Directory.Exists(DirectoryPath)) Directory.CreateDirectory(DirectoryPath);
            if (!File.Exists(FilePath))
            {
                using (WebClient webClient = new WebClient()) webClient.DownloadFile("https://steamcdn-a.akamaihd.net/client/installer/steamcmd.zip", ArchivePath);
                ZipFile.ExtractToDirectory(ArchivePath, DirectoryPath);
                File.Delete(ArchivePath);
            }
            if (!File.Exists(DllPath)) Current.TryToExecuteCommand($@"+login anonymous +app_info_update 1 +quit", out _);
        }

        public static bool GetAppInfo(int steamAppId, out Dictionary<string, string> appInfo)
        {
            appInfo = new();
            if (Program.Canceled) return false;
            // need to find a way to request app info, currently it won't work from the command line like below
            bool success = Current.TryToExecuteCommand($@"+login anonymous +app_info_update 1 +app_info_request {steamAppId} +app_info_print {steamAppId} +quit", out string output);
            if (!success) return false;
            foreach (string s in output.Split(new string[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries))
            {
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
            foreach (Process process in Process.GetProcessesByName("steamcmd")) { process.Kill(); process.WaitForExit(); }
        }

        public static void Dispose()
        {
            Kill();
            if (Directory.Exists(DirectoryPath)) Directory.Delete(DirectoryPath, true);
        }
    }
}