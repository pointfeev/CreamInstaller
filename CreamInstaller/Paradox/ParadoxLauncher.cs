using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

using CreamInstaller.Resources;
using CreamInstaller.Utility;

using Microsoft.Win32;

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
            return installPath;
        }
    }

    private static void PopulateDlc(ProgramSelection paradoxLauncher = null)
    {
        paradoxLauncher ??= ProgramSelection.FromId("ParadoxLauncher");
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
        ProgramSelection paradoxLauncher = ProgramSelection.FromId("ParadoxLauncher");
        if (paradoxLauncher is not null && paradoxLauncher.Enabled)
        {
            PopulateDlc(paradoxLauncher);
            if (!paradoxLauncher.ExtraDlc.Any())
            {
                using DialogForm dialogForm = new(form);
                return dialogForm.Show(SystemIcons.Warning,
                    $"WARNING: There are no installed games with DLC that can be added to the Paradox Launcher!" +
                    "\n\nInstalling SmokeAPI/ScreamAPI for the Paradox Launcher is pointless, since no DLC will be added to the configuration!",
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
            directory.GetSmokeApiComponents(out string sdk32, out _, out string sdk64, out _, out string config, out _);
            smokeConfig = smokeConfig || File.Exists(config);
            await InstallForm.UninstallSmokeAPI(directory, deleteConfig: false);
            if (steamOriginalSdk32 is null && File.Exists(sdk32) && !sdk32.IsResourceFile(ResourceIdentifier.Steamworks32))
                steamOriginalSdk32 = File.ReadAllBytes(sdk32);
            if (steamOriginalSdk64 is null && File.Exists(sdk64) && !sdk64.IsResourceFile(ResourceIdentifier.Steamworks64))
                steamOriginalSdk64 = File.ReadAllBytes(sdk64);

            directory.GetScreamApiComponents(out sdk32, out _, out sdk64, out _, out config, out _);
            screamConfig = screamConfig || File.Exists(config);
            await InstallForm.UninstallScreamAPI(directory, deleteConfig: false);
            if (epicOriginalSdk32 is null && File.Exists(sdk32) && !sdk32.IsResourceFile(ResourceIdentifier.EpicOnlineServices32))
                epicOriginalSdk32 = File.ReadAllBytes(sdk32);
            if (epicOriginalSdk64 is null && File.Exists(sdk64) && !sdk64.IsResourceFile(ResourceIdentifier.EpicOnlineServices64))
                epicOriginalSdk64 = File.ReadAllBytes(sdk64);
        }
        using DialogForm dialogForm = new(form);
        if (steamOriginalSdk32 is not null || steamOriginalSdk64 is not null || epicOriginalSdk32 is not null || epicOriginalSdk64 is not null)
        {
            bool neededRepair = false;
            foreach (string directory in selection.DllDirectories)
            {
                directory.GetSmokeApiComponents(out string sdk32, out _, out string sdk64, out _, out _, out _);
                if (steamOriginalSdk32 is not null && sdk32.IsResourceFile(ResourceIdentifier.Steamworks32))
                {
                    steamOriginalSdk32.Write(sdk32);
                    if (installForm is not null)
                        installForm.UpdateUser("Corrected Steamworks: " + sdk32, InstallationLog.Action);
                    neededRepair = true;
                }
                if (steamOriginalSdk64 is not null && sdk64.IsResourceFile(ResourceIdentifier.Steamworks64))
                {
                    steamOriginalSdk64.Write(sdk64);
                    if (installForm is not null)
                        installForm.UpdateUser("Corrected Steamworks: " + sdk64, InstallationLog.Action);
                    neededRepair = true;
                }
                if (smokeConfig)
                    await InstallForm.InstallSmokeAPI(directory, selection, generateConfig: false);

                directory.GetScreamApiComponents(out sdk32, out _, out sdk64, out _, out _, out _);
                if (epicOriginalSdk32 is not null && sdk32.IsResourceFile(ResourceIdentifier.EpicOnlineServices32))
                {
                    epicOriginalSdk32.Write(sdk32);
                    if (installForm is not null)
                        installForm.UpdateUser("Corrected Epic Online Services: " + sdk32, InstallationLog.Action);
                    neededRepair = true;
                }
                if (epicOriginalSdk64 is not null && sdk64.IsResourceFile(ResourceIdentifier.EpicOnlineServices64))
                {
                    epicOriginalSdk64.Write(sdk64);
                    if (installForm is not null)
                        installForm.UpdateUser("Corrected Epic Online Services: " + sdk64, InstallationLog.Action);
                    neededRepair = true;
                }
                if (screamConfig)
                    await InstallForm.InstallScreamAPI(directory, selection, generateConfig: false);
            }
            if (neededRepair)
            {
                if (installForm is not null)
                    installForm.UpdateUser("Paradox Launcher successfully repaired!", InstallationLog.Success);
                else
                    dialogForm.Show(form.Icon, "Paradox Launcher successfully repaired!", "OK", customFormText: "Paradox Launcher");
                return RepairResult.Success;
            }
            else
            {
                if (installForm is not null)
                    installForm.UpdateUser("Paradox Launcher did not need to be repaired.", InstallationLog.Success);
                else
                    dialogForm.Show(SystemIcons.Information, "Paradox Launcher does not need to be repaired.", "OK", customFormText: "Paradox Launcher");
                return RepairResult.Unnecessary;
            }
        }
        else
        {
            if (form is InstallForm)
                throw new CustomMessageException("Repair failed! " +
                    "An original Steamworks/Epic Online Services SDK file could not be found. " +
                    "You will likely have to reinstall Paradox Launcher to fix this issue.");
            else
                dialogForm.Show(SystemIcons.Error, "Paradox Launcher repair failed!"
                    + "\n\nAn original Steamworks/Epic Online Services SDK file could not be found."
                    + "\nYou will likely have to reinstall Paradox Launcher to fix this issue.", "OK", customFormText: "Paradox Launcher");
            return RepairResult.Failure;
        }
    }
}
