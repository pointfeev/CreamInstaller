using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace CreamInstaller
{
    internal static class Program
    {
        internal static readonly string ApplicationName = Application.CompanyName + " v" + Application.ProductVersion + ": " + Application.ProductName;
        internal static readonly Assembly EntryAssembly = Assembly.GetEntryAssembly();
        internal static readonly Process CurrentProcess = Process.GetCurrentProcess();
        internal static readonly string CurrentProcessFilePath = CurrentProcess.MainModule.FileName;
        internal static readonly string CurrentProcessDirectory = CurrentProcessFilePath.Substring(0, CurrentProcessFilePath.LastIndexOf("\\"));
        internal static readonly string BackupFileExtension = ".creaminstaller.backup";

        internal static bool BlockProtectedGames = true;
        internal static readonly string[] ProtectedGameNames = { "PAYDAY 2", "Call to Arms" }; // non-functioning CreamAPI or DLL detections
        internal static readonly string[] ProtectedGameDirectories = { @"\EasyAntiCheat", @"\BattlEye" }; // DLL detections
        internal static readonly string[] ProtectedGameDirectoryExceptions = { "Arma 3" }; // Arma 3's BattlEye doesn't detect DLL changes?

        internal static bool IsGameBlocked(string name, string directory)
        {
            if (ProtectedGameNames.Contains(name)) return true;
            if (!ProtectedGameDirectoryExceptions.Contains(name))
                foreach (string path in ProtectedGameDirectories)
                    if (Directory.Exists(directory + path)) return true;
            return false;
        }

        [STAThread]
        private static void Main()
        {
            Mutex mutex = new(true, "CreamInstaller", out bool createdNew);
            if (createdNew)
            {
                Application.SetHighDpiMode(HighDpiMode.SystemAware);
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                Application.ApplicationExit += new(OnApplicationExit);
            retry:
                try
                {
                    Application.Run(new MainForm());
                }
                catch (Exception e)
                {
                    if (ExceptionHandler.OutputException(e)) goto retry;
                    Application.Exit();
                    return;
                }
            }
            mutex.Close();
        }

        internal static async Task<Image> GetImageFromUrl(string url)
        {
            try
            {
                HttpClient httpClient = new();
                httpClient.DefaultRequestHeaders.Add("user-agent", "CreamInstaller");
                return new Bitmap(await httpClient.GetStreamAsync(url));
            }
            catch { }
            return null;
        }

        internal static void OpenDirectoryInFileExplorer(string path) => Process.Start(new ProcessStartInfo
        {
            FileName = "explorer.exe",
            Arguments = path
        });

        internal static void OpenUrlInInternetBrowser(string url) => Process.Start(new ProcessStartInfo
        {
            FileName = url,
            UseShellExecute = true
        });

        internal static Image GetFileIconImage(string path) => File.Exists(path) ? Icon.ExtractAssociatedIcon(path).ToBitmap() : null;

        internal static Image GetFileExplorerImage() => GetFileIconImage(Environment.GetFolderPath(Environment.SpecialFolder.Windows) + @"\explorer.exe");

        internal static bool IsProgramRunningDialog(Form form, ProgramSelection selection)
        {
            if (selection.AreSteamApiDllsLocked)
            {
                if (new DialogForm(form).Show(ApplicationName, SystemIcons.Error,
                $"ERROR: {selection.Name} is currently running!" +
                "\n\nPlease close the program/game to continue . . . ",
                "Retry", "Cancel") == DialogResult.OK)
                    return IsProgramRunningDialog(form, selection);
            }
            else return true;
            return false;
        }

        internal static bool IsFilePathLocked(this string filePath)
        {
            try { File.Open(filePath, FileMode.Open, FileAccess.ReadWrite, FileShare.None).Close(); }
            catch (FileNotFoundException) { return false; }
            catch (IOException) { return true; }
            return false;
        }

        internal static SelectForm SelectForm;
        internal static InstallForm InstallForm;

        internal static List<ProgramSelection> ProgramSelections = new();

        internal static bool Canceled = false;
        internal static async void Cleanup(bool cancel = true)
        {
            Canceled = cancel;
            await SteamCMD.Kill();
        }

        private static void OnApplicationExit(object s, EventArgs e) => Cleanup();

        internal static void InheritLocation(this Form form, Form fromForm)
        {
            int X = fromForm.Location.X + fromForm.Size.Width / 2 - form.Size.Width / 2;
            int Y = fromForm.Location.Y + fromForm.Size.Height / 2 - form.Size.Height / 2;
            form.Location = new(X, Y);
        }
    }
}