using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;
using CreamInstaller.Forms;
using CreamInstaller.Platforms.Steam;
using CreamInstaller.Utility;

namespace CreamInstaller;

internal static class Program
{
    internal static readonly string Name = Application.CompanyName;
    private static readonly string Description = Application.ProductName;
    internal static readonly string Version = Application.ProductVersion;

    internal const string RepositoryOwner = "pointfeev";
    internal static readonly string RepositoryName = Name;
    internal static readonly string RepositoryPackage = Name + ".zip";
#if DEBUG
    internal static readonly string ApplicationName = Name + " v" + Version + "-debug: " + Description;
    internal static readonly string ApplicationNameShort = Name + " v" + Version + "-debug";
    internal static readonly string ApplicationExecutable = Name + "-debug.exe"; // should be the same as in .csproj
#else
    internal static readonly string ApplicationName = Name + " v" + Version + ": " + Description;
    internal static readonly string ApplicationNameShort = Name + " v" + Version;
    internal static readonly string ApplicationExecutable = Name + ".exe"; // should be the same as in .csproj
#endif

    internal static readonly Assembly EntryAssembly = Assembly.GetEntryAssembly();
    private static readonly Process CurrentProcess = Process.GetCurrentProcess();
    internal static readonly string CurrentProcessFilePath = CurrentProcess.MainModule?.FileName;

    internal static bool BlockProtectedGames = true;
    internal static readonly string[] ProtectedGames = { "PAYDAY 2" };
    internal static readonly string[] ProtectedGameDirectories = { @"\EasyAntiCheat", @"\BattlEye" };
    internal static readonly string[] ProtectedGameDirectoryExceptions = Array.Empty<string>();

    internal static bool IsGameBlocked(string name, string directory = null)
    {
        if (!BlockProtectedGames)
            return false;
        if (ProtectedGames.Contains(name))
            return true;
        if (directory is null || ProtectedGameDirectoryExceptions.Contains(name))
            return false;
        return ProtectedGameDirectories.Any(path => Directory.Exists(directory + path));
    }

    internal static bool IsProgramRunningDialog(Form form, ProgramSelection selection)
    {
        while (true)
        {
            if (selection.AreDllsLocked)
            {
                using DialogForm dialogForm = new(form);
                if (dialogForm.Show(SystemIcons.Error,
                        $"ERROR: {selection.Name} is currently running!" + "\n\nPlease close the program/game to continue . . . ", "Retry", "Cancel")
                 == DialogResult.OK)
                    continue;
            }
            else
                return true;
            return false;
        }
    }

    [STAThread]
    private static void Main()
    {
        using Mutex mutex = new(true, Name, out bool createdNew);
        if (createdNew)
        {
            _ = Application.SetHighDpiMode(HighDpiMode.SystemAware);
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.ApplicationExit += OnApplicationExit;
            Application.ThreadException += (_, e) => e.Exception.HandleFatalException();
            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
            AppDomain.CurrentDomain.UnhandledException += (_, e) => (e.ExceptionObject as Exception)?.HandleFatalException();
        retry:
            try
            {
                HttpClientManager.Setup();
                using MainForm form = new();
#if DEBUG
                DebugForm.Current.Attach(form);
#endif
                Application.Run(form);
            }
            catch (Exception e)
            {
                if (e.HandleException())
                    goto retry;
                Application.Exit();
                return;
            }
        }
        mutex.Close();
    }

    internal static bool Canceled;

    internal static async void Cleanup(bool cancel = true)
    {
        Canceled = cancel;
        await SteamCMD.Cleanup();
    }

    private static void OnApplicationExit(object s, EventArgs e)
    {
        Cleanup();
        HttpClientManager.Dispose();
    }
}