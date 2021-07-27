using CG.Web.MegaApiClient;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace CreamInstaller
{
    public static class Program
    {
        public static string ApplicationName = "CreamInstaller v" + Application.ProductVersion + ": CreamAPI Downloader & Installer";

        [STAThread]
        static void Main()
        {
            MegaApiClient = new MegaApiClient();
            MegaApiClient.Login();

            Application.SetHighDpiMode(HighDpiMode.SystemAware);
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.ApplicationExit += new EventHandler(OnApplicationExit);
            Application.Run(new MainForm());
        }

        public static bool IsFilePathLocked(this string filePath)
        {
            bool Locked = false;
            try
            {
                File.Open(filePath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None).Close();
            }
            catch (IOException)
            {
                Locked = true;
            }
            return Locked;
        }

        public static SelectForm SelectForm;
        public static InstallForm InstallForm;

        public static List<ProgramSelection> ProgramSelections = new();

        public static bool Canceled = false;
        public static MegaApiClient MegaApiClient;
        public static ZipArchive OutputArchive;
        public static CancellationTokenSource CancellationTokenSource;
        public static Task OutputTask;
        public static string OutputFile;

        private static void UpdateProgress(int progress)
        {
            if (InstallForm != null)
            {
                InstallForm.UpdateProgress(progress);
            }
        }
        private static void UpdateUser(string text)
        {
            if (InstallForm != null)
            {
                InstallForm.UpdateUser(text);
            }
        }

        public static void Cleanup(bool cancel = true, bool logout = true)
        {
            Canceled = cancel;
            if (OutputArchive != null || CancellationTokenSource != null || OutputTask != null || OutputFile != null)
            {
                UpdateProgress(0);
                UpdateUser("Cleaning up . . . ");
            }
            if (OutputArchive != null)
            {
                OutputArchive.Dispose();
                OutputArchive = null;
                UpdateProgress(25);
            }
            if (CancellationTokenSource != null)
            {
                CancellationTokenSource.Cancel();
                UpdateProgress(40);
            }
            if (OutputTask != null)
            {
                try
                {
                    OutputTask.Wait();
                }
                catch (AggregateException) { }
                OutputTask.Dispose();
                OutputTask = null;
                UpdateProgress(50);
            }
            if (CancellationTokenSource != null)
            {
                CancellationTokenSource.Dispose();
                CancellationTokenSource = null;
                UpdateProgress(75);
            }
            if (OutputFile != null && File.Exists(OutputFile))
            {
                try
                {
                    File.Delete(OutputFile);
                }
                catch (UnauthorizedAccessException)
                {
                    UpdateUser($"WARNING: Couldn't clean up downloaded archive ({OutputFile})");
                }
                OutputFile = null;
            }
            UpdateProgress(100);
            if (logout && MegaApiClient != null && MegaApiClient.IsLoggedIn)
            {
                UpdateProgress(0);
                UpdateUser("Logging out of MEGA . . . ");
                MegaApiClient.Logout();
                UpdateProgress(100);
            }
        }

        private static void OnApplicationExit(object s, EventArgs e)
        {
            Cleanup();
        }
    }
}
