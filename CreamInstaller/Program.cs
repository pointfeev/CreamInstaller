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

using CreamInstaller.Classes;

using Microsoft.Win32;

namespace CreamInstaller;

internal static class Program
{
    internal static readonly string ApplicationName = Application.CompanyName + " v" + Application.ProductVersion + ": " + Application.ProductName;
    internal static readonly Assembly EntryAssembly = Assembly.GetEntryAssembly();
    internal static readonly Process CurrentProcess = Process.GetCurrentProcess();
    internal static readonly string CurrentProcessFilePath = CurrentProcess.MainModule.FileName;
    internal static readonly string CurrentProcessDirectory = CurrentProcessFilePath[..CurrentProcessFilePath.LastIndexOf("\\")];
    internal static readonly string BackupFileExtension = ".creaminstaller.backup";

    internal static bool BlockProtectedGames = true;
    internal static readonly string[] ProtectedGameNames = { "PAYDAY 2", "Call to Arms" }; // non-functioning CreamAPI or DLL detections
    internal static readonly string[] ProtectedGameDirectories = { @"\EasyAntiCheat", @"\BattlEye" }; // DLL detections
    internal static readonly string[] ProtectedGameDirectoryExceptions = { "Arma 3" }; // Arma 3's BattlEye doesn't detect DLL changes?

    internal static string steamInstallPath = null;
    internal static string SteamInstallPath
    {
        get
        {
            steamInstallPath ??= Registry.GetValue(@"HKEY_LOCAL_MACHINE\Software\Valve\Steam", "InstallPath", null) as string;
            steamInstallPath ??= Registry.GetValue(@"HKEY_LOCAL_MACHINE\Software\Wow6432Node\Valve\Steam", "InstallPath", null) as string;
            return steamInstallPath;
        }
    }

    internal static string paradoxLauncherInstallPath = null;
    internal static string ParadoxLauncherInstallPath
    {
        get
        {
            paradoxLauncherInstallPath ??= Registry.GetValue(@"HKEY_CURRENT_USER\Software\Paradox Interactive\Paradox Launcher v2", "LauncherInstallation", null) as string;
            return paradoxLauncherInstallPath;
        }
    }

    internal static void GetApiComponents(this string directory, out string api, out string api_o, out string api64, out string api64_o, out string cApi)
    {
        api = directory + @"\steam_api.dll";
        api_o = directory + @"\steam_api_o.dll";
        api64 = directory + @"\steam_api64.dll";
        api64_o = directory + @"\steam_api64_o.dll";
        cApi = directory + @"\cream_api.ini";
    }

    internal static bool IsGameBlocked(string name, string directory)
    {
        if (!BlockProtectedGames) return false;
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

    internal static Icon ToIcon(this Image image)
    {
        Bitmap dialogIconBitmap = new(image, new Size(image.Width, image.Height));
        return Icon.FromHandle(dialogIconBitmap.GetHicon());
    }

    private static readonly string SteamAppImagesPath = "https://cdn.cloudflare.steamstatic.com/steamcommunity/public/images/apps/";
    internal static async Task<Image> GetSteamIcon(int steamAppId, string iconStaticId) => await GetImageFromUrl(SteamAppImagesPath + $"/{steamAppId}/{iconStaticId}.jpg");
    internal static async Task<Image> GetSteamClientIcon(int steamAppId, string clientIconStaticId) => await GetImageFromUrl(SteamAppImagesPath + $"/{steamAppId}/{clientIconStaticId}.ico");

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

    internal static void OpenFileInNotepad(string path) => Process.Start(new ProcessStartInfo
    {
        FileName = "notepad.exe",
        Arguments = path
    });

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

    internal static Image GetNotepadImage() => GetFileIconImage(Environment.GetFolderPath(Environment.SpecialFolder.Windows) + @"\notepad.exe");

    internal static Image GetCommandPromptImage() => GetFileIconImage(Environment.SystemDirectory + @"\cmd.exe");

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

    internal static void Invoke(this Control control, MethodInvoker methodInvoker) => control.Invoke(methodInvoker);

    internal static SelectForm SelectForm;
    internal static InstallForm InstallForm;

    internal static void InheritLocation(this Form form, Form fromForm)
    {
        int X = fromForm.Location.X + fromForm.Size.Width / 2 - form.Size.Width / 2;
        int Y = fromForm.Location.Y + fromForm.Size.Height / 2 - form.Size.Height / 2;
        form.Location = new(X, Y);
    }

    internal static List<ProgramSelection> ProgramSelections = new();

    internal static bool Canceled = false;
    internal static async void Cleanup(bool cancel = true)
    {
        Canceled = cancel;
        await SteamCMD.Cleanup();
    }

    private static void OnApplicationExit(object s, EventArgs e) => Cleanup();
}
