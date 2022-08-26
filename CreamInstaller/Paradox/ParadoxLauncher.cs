using CreamInstaller.Resources;
using CreamInstaller.Utility;

using Microsoft.Win32;

using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

using static CreamInstaller.Resources.Resources;

namespace CreamInstaller.Paradox;

internal static class ParadoxLauncher
{
    private static string installPath;
    internal static string InstallPath
    {
        get
        {
            installPath ??= Registry.GetValue(@"HKEY_CURRENT_USER\Software\Paradox Interactive\Paradox Launcher v2", "LauncherInstallation", null) as string;
            installPath ??= Registry.GetValue(@"HKEY_CURRENT_USER\Software\Wow6432Node\Paradox Interactive\Paradox Launcher v2", "LauncherInstallation", null) as string;
            return installPath.BeautifyPath();
        }
    }

    internal static async Task<List<string>> GetExecutableDirectories(string gameDirectory) =>
        await Task.Run(async () => await gameDirectory.GetExecutableDirectories(validFunc: d => !Path.GetFileName(d).Contains("bootstrapper")));

    private static void PopulateDlc(ProgramSelection paradoxLauncher = null)
    {
        paradoxLauncher ??= ProgramSelection.FromPlatformId(Platform.Paradox, "PL");
        if (paradoxLauncher is not null)
        {
            paradoxLauncher.ExtraDlc.Clear();
            paradoxLauncher.ExtraSelectedDlc.Clear();
            foreach (ProgramSelection selection in ProgramSelection.AllEnabled.Where(s => s != paradoxLauncher && s.Publisher == "Paradox Interactive"))
            {
                paradoxLauncher.ExtraDlc.Add(new(selection.Id, selection.Name, selection.AllDlc));
                paradoxLauncher.ExtraSelectedDlc.Add(new(selection.Id, selection.Name, selection.SelectedDlc));
            }
            if (!paradoxLauncher.ExtraDlc.Any())
            {
                foreach (ProgramSelection selection in ProgramSelection.AllSafe.Where(s => s != paradoxLauncher && s.Publisher == "Paradox Interactive"))
                {
                    paradoxLauncher.ExtraDlc.Add(new(selection.Id, selection.Name, selection.AllDlc));
                    paradoxLauncher.ExtraSelectedDlc.Add(new(selection.Id, selection.Name, selection.AllDlc));
                }
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
                    $"WARNING: There are no scanned games with DLC that can be added to the Paradox Launcher!" +
                    "\n\nInstalling DLC unlockers for the Paradox Launcher alone can cause existing configurations to be deleted!",
                    "Ignore", "Cancel", customFormText: "Paradox Launcher") != DialogResult.OK;
            }
        }
        return false;
    }

    public enum RepairResult
    {
        ProgramRunning = -2,
        Failure,
        Unnecessary = 0,
        Success
    }

    internal static async Task<RepairResult> Repair(Form form, ProgramSelection selection)
    {
        InstallForm installForm = form as InstallForm;
        if (!Program.IsProgramRunningDialog(form, selection))
            return form is InstallForm ? throw new CustomMessageException("Repair failed! The launcher is currently running!")
                : RepairResult.ProgramRunning;
        bool smokeConfig = false;
        byte[] steamOriginalSdk32 = null;
        byte[] steamOriginalSdk64 = null;
        bool screamConfig = false;
        byte[] epicOriginalSdk32 = null;
        byte[] epicOriginalSdk64 = null;
        foreach (string directory in selection.DllDirectories)
        {
            directory.GetSmokeApiComponents(out string api32, out _, out string api64, out _, out string config, out _);
            smokeConfig = smokeConfig || File.Exists(config);
            await SmokeAPI.Uninstall(directory, deleteConfig: false);
            if (steamOriginalSdk32 is null && File.Exists(api32) && !api32.IsResourceFile(ResourceIdentifier.Steamworks32))
                steamOriginalSdk32 = File.ReadAllBytes(api32);
            if (steamOriginalSdk64 is null && File.Exists(api64) && !api64.IsResourceFile(ResourceIdentifier.Steamworks64))
                steamOriginalSdk64 = File.ReadAllBytes(api64);

            directory.GetScreamApiComponents(out api32, out _, out api64, out _, out config);
            screamConfig = screamConfig || File.Exists(config);
            await ScreamAPI.Uninstall(directory, deleteConfig: false);
            if (epicOriginalSdk32 is null && File.Exists(api32) && !api32.IsResourceFile(ResourceIdentifier.EpicOnlineServices32))
                epicOriginalSdk32 = File.ReadAllBytes(api32);
            if (epicOriginalSdk64 is null && File.Exists(api64) && !api64.IsResourceFile(ResourceIdentifier.EpicOnlineServices64))
                epicOriginalSdk64 = File.ReadAllBytes(api64);
        }
        using DialogForm dialogForm = new(form);
        if (steamOriginalSdk32 is not null || steamOriginalSdk64 is not null || epicOriginalSdk32 is not null || epicOriginalSdk64 is not null)
        {
            bool neededRepair = false;
            foreach (string directory in selection.DllDirectories)
            {
                directory.GetSmokeApiComponents(out string api32, out _, out string api64, out _, out _, out _);
                if (steamOriginalSdk32 is not null && api32.IsResourceFile(ResourceIdentifier.Steamworks32))
                {
                    steamOriginalSdk32.Write(api32);
                    if (installForm is not null)
                        installForm.UpdateUser("Corrected Steamworks: " + api32, InstallationLog.Action);
                    neededRepair = true;
                }
                if (steamOriginalSdk64 is not null && api64.IsResourceFile(ResourceIdentifier.Steamworks64))
                {
                    steamOriginalSdk64.Write(api64);
                    if (installForm is not null)
                        installForm.UpdateUser("Corrected Steamworks: " + api64, InstallationLog.Action);
                    neededRepair = true;
                }
                if (!selection.Koaloader && smokeConfig)
                    await SmokeAPI.Install(directory, selection, generateConfig: false);

                directory.GetScreamApiComponents(out api32, out _, out api64, out _, out _);
                if (epicOriginalSdk32 is not null && api32.IsResourceFile(ResourceIdentifier.EpicOnlineServices32))
                {
                    epicOriginalSdk32.Write(api32);
                    if (installForm is not null)
                        installForm.UpdateUser("Corrected Epic Online Services: " + api32, InstallationLog.Action);
                    neededRepair = true;
                }
                if (epicOriginalSdk64 is not null && api64.IsResourceFile(ResourceIdentifier.EpicOnlineServices64))
                {
                    epicOriginalSdk64.Write(api64);
                    if (installForm is not null)
                        installForm.UpdateUser("Corrected Epic Online Services: " + api64, InstallationLog.Action);
                    neededRepair = true;
                }
                if (!selection.Koaloader && screamConfig)
                    await ScreamAPI.Install(directory, selection, generateConfig: false);
            }
            if (neededRepair)
            {
                if (installForm is not null)
                    installForm.UpdateUser("Paradox Launcher successfully repaired!", InstallationLog.Success);
                else
                    _ = dialogForm.Show(form.Icon, "Paradox Launcher successfully repaired!", "OK", customFormText: "Paradox Launcher");
                return RepairResult.Success;
            }
            else
            {
                if (installForm is not null)
                    installForm.UpdateUser("Paradox Launcher did not need to be repaired.", InstallationLog.Success);
                else
                    _ = dialogForm.Show(SystemIcons.Information, "Paradox Launcher does not need to be repaired.", "OK", customFormText: "Paradox Launcher");
                return RepairResult.Unnecessary;
            }
        }
        else
        {
            _ = form is InstallForm
                ? throw new CustomMessageException("Repair failed! " +
                    "An original Steamworks and/or Epic Online Services file could not be found. " +
                    "You will likely have to reinstall Paradox Launcher to fix this issue.")
                : dialogForm.Show(SystemIcons.Error, "Paradox Launcher repair failed!"
                    + "\n\nAn original Steamworks and/or Epic Online Services SDK file could not be found."
                    + "\nYou will likely have to reinstall Paradox Launcher to fix this issue.", "OK", customFormText: "Paradox Launcher");
            return RepairResult.Failure;
        }
    }
}
