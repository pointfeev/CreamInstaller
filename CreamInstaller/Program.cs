using System;
using System.Diagnostics;
using System.Linq;
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

    internal static readonly string Version = Application.ProductVersion[
        ..(Application.ProductVersion.IndexOf('+') is var index && index != -1
            ? index
            : Application.ProductVersion.Length)];

    internal const string RepositoryOwner = "pointfeev";
    internal static readonly string RepositoryName = Name;
    internal static readonly string RepositoryPackage = Name + ".zip";
    internal static readonly string RepositoryExecutable = Name + ".exe";
#if DEBUG
    internal static readonly string ApplicationName = Name + " v" + Version + "-debug: " + Description;
    internal static readonly string ApplicationNameShort = Name + " v" + Version + "-debug";
#else
    internal static readonly string ApplicationName = Name + " v" + Version + ": " + Description;
    internal static readonly string ApplicationNameShort = Name + " v" + Version;
#endif

    private static readonly Process CurrentProcess = Process.GetCurrentProcess();
    internal static readonly string CurrentProcessFilePath = CurrentProcess.MainModule?.FileName;
    internal static readonly int CurrentProcessId = CurrentProcess.Id;

    // this may forever be false, but who knows, maybe acidicoala makes it once again better than CreamAPI some day
    internal const bool UseSmokeAPI = false;

    internal static bool BlockProtectedGames = true;
    internal static readonly string[] ProtectedGames = ["PAYDAY 2"];
    internal static readonly string[] ProtectedGameDirectories = [@"\EasyAntiCheat", @"\BattlEye"];
    internal static readonly string[] ProtectedGameDirectoryExceptions = [];

    internal static bool IsGameBlocked(string name, string directory = null)
        => BlockProtectedGames && (ProtectedGames.Contains(name) || directory is not null &&
            !ProtectedGameDirectoryExceptions.Contains(name)
            && ProtectedGameDirectories.Any(path => (directory + path).DirectoryExists()));

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
            AppDomain.CurrentDomain.UnhandledException +=
                (_, e) => (e.ExceptionObject as Exception)?.HandleFatalException();
            retry:
            try
            {
                HttpClientManager.Setup();
                using UpdateForm form = new();
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