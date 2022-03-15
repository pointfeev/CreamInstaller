using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;

using CreamInstaller.Steam;
using CreamInstaller.Utility;

namespace CreamInstaller;

internal static class Program
{
    internal static readonly string ApplicationName = Application.CompanyName + " v" + Application.ProductVersion + ": " + Application.ProductName;
    internal static readonly string ApplicationNameShort = Application.CompanyName + " v" + Application.ProductVersion;

    internal static readonly Assembly EntryAssembly = Assembly.GetEntryAssembly();
    internal static readonly Process CurrentProcess = Process.GetCurrentProcess();
    internal static readonly string CurrentProcessFilePath = CurrentProcess.MainModule.FileName;

    internal static bool BlockProtectedGames = true;
    internal static readonly string[] ProtectedGameNames = { "PAYDAY 2", "Call to Arms" }; // non-functioning CreamAPI/ScreamAPI or DLL detections
    internal static readonly string[] ProtectedGameDirectories = { @"\EasyAntiCheat", @"\BattlEye" }; // DLL detections
    internal static readonly string[] ProtectedGameDirectoryExceptions = { "Arma 3" }; // Arma 3's BattlEye doesn't detect DLL changes?

    internal static bool IsGameBlocked(string name, string directory = null)
    {
        if (!BlockProtectedGames) return false;
        if (ProtectedGameNames.Contains(name)) return true;
        if (directory is not null && !ProtectedGameDirectoryExceptions.Contains(name))
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
        if (selection.AreDllsLocked)
        {
            using DialogForm dialogForm = new(form);
            if (dialogForm.Show(SystemIcons.Error,
            $"ERROR: {selection.Name} is currently running!" +
            "\n\nPlease close the program/game to continue . . . ",
            "Retry", "Cancel") == DialogResult.OK)
                return IsProgramRunningDialog(form, selection);
        }
        else return true;
        return false;
    }

    internal static void GetCreamApiComponents(this string directory, out string sdk32, out string sdk32_o, out string sdk64, out string sdk64_o, out string config)
    {
        sdk32 = directory + @"\steam_api.dll";
        sdk32_o = directory + @"\steam_api_o.dll";
        sdk64 = directory + @"\steam_api64.dll";
        sdk64_o = directory + @"\steam_api64_o.dll";
        config = directory + @"\cream_api.ini";
    }

    internal static void GetScreamApiComponents(this string directory, out string sdk32, out string sdk32_o, out string sdk64, out string sdk64_o, out string config)
    {
        sdk32 = directory + @"\EOSSDK-Win32-Shipping.dll";
        sdk32_o = directory + @"\EOSSDK-Win32-Shipping_o.dll";
        sdk64 = directory + @"\EOSSDK-Win64-Shipping.dll";
        sdk64_o = directory + @"\EOSSDK-Win64-Shipping_o.dll";
        config = directory + @"\ScreamAPI.json";
    }

    [STAThread]
    private static void Main()
    {
        using Mutex mutex = new(true, "CreamInstaller", out bool createdNew);
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
                using MainForm form = new();
                Application.Run(form);
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
