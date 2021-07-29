using CG.Web.MegaApiClient;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace CreamInstaller
{
    public static class Program
    {
        public static string ApplicationName = "CreamInstaller v" + Application.ProductVersion + ": CreamAPI Downloader & Installer";

        public static Assembly EntryAssembly = Assembly.GetEntryAssembly();
        public static Process CurrentProcess = Process.GetCurrentProcess();
        public static string CurrentProcessFilePath = CurrentProcess.MainModule.FileName;
        public static string CurrentProcessDirectory = CurrentProcessFilePath.Substring(0, CurrentProcessFilePath.LastIndexOf("\\"));

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool AllocConsole();

        [STAThread]
        static void Main()
        {
            Console.WriteLine(CurrentProcessDirectory);
            DirectoryInfo directoryInfo = new DirectoryInfo(CurrentProcessDirectory);
            FileInfo[] files = directoryInfo.GetFiles("CreamInstaller*.exe");
            if (files.Length > 0)
            {
                foreach (FileInfo file in files)
                {
                    if (file.FullName != CurrentProcessFilePath)
                    {
                        try
                        {
                            File.Delete(file.FullName);
                        }
                        catch { }
                    }

                }
            }

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

        public static void UpdateProgressInstantly(ProgressBar progressBar, int progress)
        {
            progressBar.Maximum++;
            progressBar.Value = progress + 1;
            progressBar.Value = progress;
            progressBar.Maximum--;
        }

        public static void Cleanup(bool cancel = true, bool logout = true)
        {
            Canceled = cancel;
            if (OutputArchive != null || CancellationTokenSource != null || OutputTask != null || OutputFile != null)
            {
                InstallForm?.UpdateProgress(0);
                InstallForm?.UpdateUser("Cleaning up . . . ");
            }
            if (OutputArchive != null)
            {
                OutputArchive.Dispose();
                OutputArchive = null;
                InstallForm?.UpdateProgress(25);
            }
            if (CancellationTokenSource != null)
            {
                CancellationTokenSource.Cancel();
                InstallForm?.UpdateProgress(40);
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
                InstallForm?.UpdateProgress(50);
            }
            if (CancellationTokenSource != null)
            {
                CancellationTokenSource.Dispose();
                CancellationTokenSource = null;
                InstallForm?.UpdateProgress(75);
            }
            if (OutputFile != null && File.Exists(OutputFile))
            {
                try
                {
                    File.Delete(OutputFile);
                }
                catch (UnauthorizedAccessException)
                {
                    InstallForm?.UpdateUser($"WARNING: Couldn't clean up downloaded archive ({OutputFile})");
                }
                OutputFile = null;
            }
            InstallForm?.UpdateProgress(100);
            if (logout && MegaApiClient != null && MegaApiClient.IsLoggedIn)
            {
                InstallForm?.UpdateProgress(0);
                InstallForm?.UpdateUser("Logging out of MEGA . . . ");
                MegaApiClient.Logout();
                InstallForm?.UpdateProgress(100);
            }
        }

        private static void OnApplicationExit(object s, EventArgs e)
        {
            Cleanup();
        }
    }
}
