using CG.Web.MegaApiClient;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.IO.Compression;
using System.Reflection;
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

        public static string BackupFileExtension = ".creaminstaller.backup";

        [STAThread]
        private static void Main()
        {
            Mutex mutex = new Mutex(true, "CreamInstaller", out bool createdNew);
            if (createdNew)
            {
                Application.SetHighDpiMode(HighDpiMode.SystemAware);
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                Application.ApplicationExit += new EventHandler(OnApplicationExit);
                Application.Run(new MainForm());
            }
            mutex.Close();
        }

        public static bool IsProgramRunningDialog(Form form, ProgramSelection selection)
        {
            if (selection.IsProgramRunning)
            {
                if (new DialogForm(form).Show(ApplicationName, SystemIcons.Error,
                $"ERROR: {selection.ProgramName} is currently running!" +
                "\n\nPlease close the program/game to continue . . .",
                "Retry", "Cancel") == DialogResult.OK)
                {
                    return IsProgramRunningDialog(form, selection);
                }
            }
            else
            {
                return true;
            }
            return false;
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

        public static void Cleanup(bool cancel = true, bool logout = true)
        {
            Canceled = cancel;
            if (OutputArchive != null || CancellationTokenSource != null || OutputTask != null || OutputFile != null)
            {
                InstallForm?.UpdateUser("Cleaning up . . . ", LogColor.Cleanup);
            }
            if (OutputArchive != null)
            {
                OutputArchive.Dispose();
                OutputArchive = null;
            }
            if (CancellationTokenSource != null)
            {
                CancellationTokenSource.Cancel();
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
            }
            if (CancellationTokenSource != null)
            {
                CancellationTokenSource.Dispose();
                CancellationTokenSource = null;
            }
            if (OutputFile != null && File.Exists(OutputFile))
            {
                try
                {
                    File.Delete(OutputFile);
                }
                catch
                {
                    InstallForm?.UpdateUser($"WARNING: Failed to clean up downloaded archive: {OutputFile}", LogColor.Warning);
                }
                OutputFile = null;
            }
            if (logout && MegaApiClient != null && MegaApiClient.IsLoggedIn)
            {
                InstallForm?.UpdateUser("Logging out of MEGA . . . ", LogColor.Cleanup);
                MegaApiClient.Logout();
            }
        }

        private static void OnApplicationExit(object s, EventArgs e)
        {
            Cleanup();
        }
    }
}
