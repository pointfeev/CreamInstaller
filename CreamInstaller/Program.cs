using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;

using CreamInstaller.Classes;

namespace CreamInstaller;

internal static class Program
{
    internal static readonly string ApplicationName = Application.CompanyName + " v" + Application.ProductVersion + ": " + Application.ProductName;

    internal static readonly Assembly EntryAssembly = Assembly.GetEntryAssembly();
    internal static readonly Process CurrentProcess = Process.GetCurrentProcess();
    internal static readonly string CurrentProcessFilePath = CurrentProcess.MainModule.FileName;

    internal static bool BlockProtectedGames = true;
    internal static readonly string[] ProtectedGameNames = { "PAYDAY 2", "Call to Arms" }; // non-functioning CreamAPI or DLL detections
    internal static readonly string[] ProtectedGameDirectories = { @"\EasyAntiCheat", @"\BattlEye" }; // DLL detections
    internal static readonly string[] ProtectedGameDirectoryExceptions = { "Arma 3" }; // Arma 3's BattlEye doesn't detect DLL changes?

    internal static bool IsGameBlocked(string name, string directory)
    {
        if (!BlockProtectedGames) return false;
        if (ProtectedGameNames.Contains(name)) return true;
        if (!ProtectedGameDirectoryExceptions.Contains(name))
            foreach (string path in ProtectedGameDirectories)
                if (Directory.Exists(directory + path)) return true;
        return false;
    }

    internal static bool IsFilePathLocked(this string filePath)
    {
        try { File.Open(filePath, FileMode.Open, FileAccess.ReadWrite, FileShare.None).Close(); }
        catch (FileNotFoundException) { return false; }
        catch (IOException) { return true; }
        return false;
    }

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

    internal static void GetApiComponents(this string directory, out string api, out string api_o, out string api64, out string api64_o, out string cApi)
    {
        api = directory + @"\steam_api.dll";
        api_o = directory + @"\steam_api_o.dll";
        api64 = directory + @"\steam_api64.dll";
        api64_o = directory + @"\steam_api64_o.dll";
        cApi = directory + @"\cream_api.ini";
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
                HttpClientManager.Setup();
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

    internal static void Invoke(this Control control, MethodInvoker methodInvoker) => control.Invoke(methodInvoker);

    internal static bool Canceled = false;
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
