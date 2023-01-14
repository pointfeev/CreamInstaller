using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using CreamInstaller.Forms;
using CreamInstaller.Resources;
using CreamInstaller.Utility;
using Microsoft.Win32;
using static CreamInstaller.Resources.Resources;

namespace CreamInstaller.Platforms.Paradox;

internal static class ParadoxLauncher
{
    public enum RepairResult
    {
        ProgramRunning = -2, Failure, Unnecessary = 0,
        Success
    }

    private static string installPath;

    internal static string InstallPath
    {
        get
        {
            installPath ??= Registry.GetValue(@"HKEY_CURRENT_USER\Software\Paradox Interactive\Paradox Launcher v2", "LauncherInstallation", null) as string;
            installPath
                ??= Registry.GetValue(@"HKEY_CURRENT_USER\Software\Wow6432Node\Paradox Interactive\Paradox Launcher v2", "LauncherInstallation",
                    null) as string;
            return installPath.BeautifyPath();
        }
    }

    internal static async Task<List<(string directory, BinaryType binaryType)>> GetExecutableDirectories(string gameDirectory)
        => await Task.Run(async () => await gameDirectory.GetExecutableDirectories(validFunc: path => !Path.GetFileName(path).Contains("bootstrapper")));

    private static void PopulateDlc(ProgramSelection paradoxLauncher = null)
    {
        paradoxLauncher ??= ProgramSelection.FromPlatformId(Platform.Paradox, "PL");
        if (paradoxLauncher is not null)
        {
            paradoxLauncher.ExtraDlc.Clear();
            paradoxLauncher.ExtraSelectedDlc.Clear();
            foreach (ProgramSelection selection in ProgramSelection.AllEnabled.Where(s => s != paradoxLauncher && s.Publisher == "Paradox Interactive"))
            {
                paradoxLauncher.ExtraDlc.Add(selection.Id, (selection.Name, selection.AllDlc));
                paradoxLauncher.ExtraSelectedDlc.Add(selection.Id, (selection.Name, selection.SelectedDlc));
            }
            if (!paradoxLauncher.ExtraDlc.Any())
                foreach (ProgramSelection selection in ProgramSelection.AllSafe.Where(s => s != paradoxLauncher && s.Publisher == "Paradox Interactive"))
                {
                    paradoxLauncher.ExtraDlc.Add(selection.Id, (selection.Name, selection.AllDlc));
                    paradoxLauncher.ExtraSelectedDlc.Add(selection.Id, (selection.Name, selection.AllDlc));
                }
        }
    }

    internal static bool DlcDialog(Form form)
    {
        ProgramSelection paradoxLauncher = ProgramSelection.FromPlatformId(Platform.Paradox, "PL");
        if (paradoxLauncher is not null && paradoxLauncher.Enabled)
        {
            PopulateDlc(paradoxLauncher);
            if (!paradoxLauncher.ExtraDlc.Any())
            {
                using DialogForm dialogForm = new(form);
                return dialogForm.Show(SystemIcons.Warning,
                    "WARNING: There are no scanned games with DLC that can be added to the Paradox Launcher!"
                  + "\n\nInstalling DLC unlockers for the Paradox Launcher alone can cause existing configurations to be deleted!", "Ignore", "Cancel",
                    "Paradox Launcher") != DialogResult.OK;
            }
        }
        return false;
    }

    internal static async Task<RepairResult> Repair(Form form, ProgramSelection selection)
    {
        InstallForm installForm = form as InstallForm;
        if (!Program.IsProgramRunningDialog(form, selection))
            return form is InstallForm ? throw new CustomMessageException("Repair failed! The launcher is currently running!") : RepairResult.ProgramRunning;
        bool smokeInstalled = false;
        byte[] steamOriginalSdk32 = null;
        byte[] steamOriginalSdk64 = null;
        bool screamInstalled = false;
        byte[] epicOriginalSdk32 = null;
        byte[] epicOriginalSdk64 = null;
        foreach (string directory in selection.DllDirectories)
        {
            bool koaloaderInstalled = Koaloader.AutoLoadDLLs.Select(pair => (pair.unlocker, path: directory + @"\" + pair.dll))
                                               .Any(pair => File.Exists(pair.path) && pair.path.IsResourceFile());
            directory.GetSmokeApiComponents(out string api32, out string api32_o, out string api64, out string api64_o, out string old_config,
                out string config, out _, out _, out _);
            smokeInstalled = smokeInstalled || File.Exists(api32_o) || File.Exists(api64_o)
                          || (File.Exists(old_config) || File.Exists(config)) && !koaloaderInstalled
                          || File.Exists(api32) && api32.IsResourceFile(ResourceIdentifier.Steamworks32)
                          || File.Exists(api64) && api64.IsResourceFile(ResourceIdentifier.Steamworks64);
            await SmokeAPI.Uninstall(directory, deleteOthers: false);
            if (steamOriginalSdk32 is null && File.Exists(api32) && !api32.IsResourceFile(ResourceIdentifier.Steamworks32))
                steamOriginalSdk32 = await File.ReadAllBytesAsync(api32);
            if (steamOriginalSdk64 is null && File.Exists(api64) && !api64.IsResourceFile(ResourceIdentifier.Steamworks64))
                steamOriginalSdk64 = await File.ReadAllBytesAsync(api64);
            directory.GetScreamApiComponents(out api32, out api32_o, out api64, out api64_o, out config, out string log);
            screamInstalled = screamInstalled || File.Exists(api32_o) || File.Exists(api64_o)
                           || (File.Exists(config) || File.Exists(log)) && !koaloaderInstalled
                           || File.Exists(api32) && api32.IsResourceFile(ResourceIdentifier.EpicOnlineServices32)
                           || File.Exists(api64) && api64.IsResourceFile(ResourceIdentifier.EpicOnlineServices64);
            await ScreamAPI.Uninstall(directory, deleteOthers: false);
            if (epicOriginalSdk32 is null && File.Exists(api32) && !api32.IsResourceFile(ResourceIdentifier.EpicOnlineServices32))
                epicOriginalSdk32 = await File.ReadAllBytesAsync(api32);
            if (epicOriginalSdk64 is null && File.Exists(api64) && !api64.IsResourceFile(ResourceIdentifier.EpicOnlineServices64))
                epicOriginalSdk64 = await File.ReadAllBytesAsync(api64);
        }
        using DialogForm dialogForm = new(form);
        if (steamOriginalSdk32 is not null || steamOriginalSdk64 is not null || epicOriginalSdk32 is not null || epicOriginalSdk64 is not null)
        {
            bool neededRepair = false;
            foreach (string directory in selection.DllDirectories)
            {
                directory.GetSmokeApiComponents(out string api32, out _, out string api64, out _, out _, out _, out _, out _, out _);
                if (steamOriginalSdk32 is not null && api32.IsResourceFile(ResourceIdentifier.Steamworks32))
                {
                    steamOriginalSdk32.Write(api32);
                    installForm?.UpdateUser("Corrected Steamworks: " + api32, LogTextBox.Action);
                    neededRepair = true;
                }
                if (steamOriginalSdk64 is not null && api64.IsResourceFile(ResourceIdentifier.Steamworks64))
                {
                    steamOriginalSdk64.Write(api64);
                    installForm?.UpdateUser("Corrected Steamworks: " + api64, LogTextBox.Action);
                    neededRepair = true;
                }
                if (smokeInstalled)
                    await SmokeAPI.Install(directory, selection, generateConfig: false);
                directory.GetScreamApiComponents(out api32, out _, out api64, out _, out _, out _);
                if (epicOriginalSdk32 is not null && api32.IsResourceFile(ResourceIdentifier.EpicOnlineServices32))
                {
                    epicOriginalSdk32.Write(api32);
                    installForm?.UpdateUser("Corrected Epic Online Services: " + api32, LogTextBox.Action);
                    neededRepair = true;
                }
                if (epicOriginalSdk64 is not null && api64.IsResourceFile(ResourceIdentifier.EpicOnlineServices64))
                {
                    epicOriginalSdk64.Write(api64);
                    installForm?.UpdateUser("Corrected Epic Online Services: " + api64, LogTextBox.Action);
                    neededRepair = true;
                }
                if (screamInstalled)
                    await ScreamAPI.Install(directory, selection, generateConfig: false);
            }
            if (neededRepair)
            {
                if (installForm is not null)
                    installForm.UpdateUser("Paradox Launcher successfully repaired!", LogTextBox.Success);
                else
                    _ = dialogForm.Show(form.Icon, "Paradox Launcher successfully repaired!", customFormText: "Paradox Launcher");
                return RepairResult.Success;
            }
            if (installForm is not null)
                installForm.UpdateUser("Paradox Launcher did not need to be repaired.", LogTextBox.Success);
            else
                _ = dialogForm.Show(SystemIcons.Information, "Paradox Launcher does not need to be repaired.", customFormText: "Paradox Launcher");
            return RepairResult.Unnecessary;
        }
        _ = form is InstallForm
            ? throw new CustomMessageException("Repair failed! " + "An original Steamworks and/or Epic Online Services file could not be found. "
                                                                 + "You will likely have to reinstall Paradox Launcher to fix this issue.")
            : dialogForm.Show(SystemIcons.Error,
                "Paradox Launcher repair failed!" + "\n\nAn original Steamworks and/or Epic Online Services file could not be found."
                                                  + "\nYou will likely have to reinstall Paradox Launcher to fix this issue.",
                customFormText: "Paradox Launcher");
        return RepairResult.Failure;
    }
}