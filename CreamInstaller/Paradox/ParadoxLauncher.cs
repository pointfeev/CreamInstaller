using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

using CreamInstaller.Resources;

using Microsoft.Win32;

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
            foreach (ProgramSelection selection in ProgramSelection.AllUsableEnabled)
            {
                if (selection == paradoxLauncher || selection.Publisher != "Paradox Interactive") continue;
                paradoxLauncher.ExtraDlc.Add(new(selection.Id, selection.Name, selection.SelectedDlc));
            }
            if (!paradoxLauncher.ExtraDlc.Any())
                foreach (ProgramSelection selection in ProgramSelection.AllUsable)
                {
                    if (selection == paradoxLauncher || selection.Publisher != "Paradox Interactive") continue;
                    paradoxLauncher.ExtraDlc.Add(new(selection.Id, selection.Name, selection.AllDlc));
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
                    "\n\nInstalling CreamAPI/ScreamAPI for the Paradox Launcher is pointless, since no DLC will be added to the configuration!",
                    "Ignore", "Cancel") != DialogResult.OK;
            }
        }
        return false;
    }

    internal static async Task Repair(Form form, ProgramSelection selection)
    {
        if (!Program.IsProgramRunningDialog(form, selection)) return;
        byte[] creamConfig = null;
        byte[] steamOriginalSdk32 = null;
        byte[] steamOriginalSdk64 = null;
        byte[] screamConfig = null;
        byte[] epicOriginalSdk32 = null;
        byte[] epicOriginalSdk64 = null;
        foreach (string directory in selection.DllDirectories)
        {
            directory.GetCreamApiComponents(out string sdk32, out string _, out string sdk64, out string _, out string config);
            if (creamConfig is null && File.Exists(config))
                creamConfig = File.ReadAllBytes(config);
            await InstallForm.UninstallCreamAPI(directory);
            if (steamOriginalSdk32 is null && File.Exists(sdk32) && !Properties.Resources.Steamworks32.EqualsFile(sdk32))
                steamOriginalSdk32 = File.ReadAllBytes(sdk32);
            if (steamOriginalSdk64 is null && File.Exists(sdk64) && !Properties.Resources.Steamworks64.EqualsFile(sdk64))
                steamOriginalSdk64 = File.ReadAllBytes(sdk64);
            directory.GetScreamApiComponents(out sdk32, out string _, out sdk64, out string _, out config);
            if (screamConfig is null && File.Exists(config))
                screamConfig = File.ReadAllBytes(config);
            await InstallForm.UninstallScreamAPI(directory);
            if (epicOriginalSdk32 is null && File.Exists(sdk32) && !Properties.Resources.EpicOnlineServices32.EqualsFile(sdk32))
                epicOriginalSdk32 = File.ReadAllBytes(sdk32);
            if (epicOriginalSdk64 is null && File.Exists(sdk64) && !Properties.Resources.EpicOnlineServices64.EqualsFile(sdk64))
                epicOriginalSdk64 = File.ReadAllBytes(sdk64);
        }
        using DialogForm dialogForm = new(form);
        if (steamOriginalSdk32 is not null || steamOriginalSdk64 is not null || epicOriginalSdk32 is not null || epicOriginalSdk64 is not null)
        {
            bool neededRepair = false;
            foreach (string directory in selection.DllDirectories)
            {
                directory.GetCreamApiComponents(out string sdk32, out string _, out string sdk64, out string _, out string config);
                if (steamOriginalSdk32 is not null && Properties.Resources.Steamworks32.EqualsFile(sdk32))
                {
                    steamOriginalSdk32.Write(sdk32);
                    neededRepair = true;
                }
                if (steamOriginalSdk64 is not null && Properties.Resources.Steamworks64.EqualsFile(sdk64))
                {
                    steamOriginalSdk64.Write(sdk64);
                    neededRepair = true;
                }
                if (creamConfig is not null)
                {
                    await InstallForm.InstallCreamAPI(directory, selection);
                    creamConfig.Write(config);
                }

                directory.GetScreamApiComponents(out sdk32, out string _, out sdk64, out string _, out config);
                if (epicOriginalSdk32 is not null && Properties.Resources.EpicOnlineServices32.EqualsFile(sdk32))
                {
                    epicOriginalSdk32.Write(sdk32);
                    neededRepair = true;
                }
                if (epicOriginalSdk64 is not null && Properties.Resources.EpicOnlineServices64.EqualsFile(sdk64))
                {
                    epicOriginalSdk64.Write(sdk64);
                    neededRepair = true;
                }
                if (screamConfig is not null)
                {
                    await InstallForm.InstallScreamAPI(directory, selection);
                    screamConfig.Write(config);
                }
            }
            if (neededRepair)
                dialogForm.Show(form.Icon, "Paradox Launcher successfully repaired!", "OK");
            else
                dialogForm.Show(SystemIcons.Information, "Paradox Launcher does not need to be repaired.", "OK");
        }
        else
            dialogForm.Show(SystemIcons.Error, "Paradox Launcher repair failed!"
                + "\n\nAn original Steamworks/Epic Online Services SDK file could not be found."
                + "\nYou must reinstall Paradox Launcher to fix this issue.", "OK");
    }
}
