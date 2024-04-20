using System.Drawing;
using System.Linq;
using System.Text;
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
        Failure = -1,
        Unnecessary = 0,
        Success
    }

    private static string installPath;

    internal static string InstallPath
    {
        get
        {
            installPath ??= Registry.GetValue(@"HKEY_CURRENT_USER\Software\Paradox Interactive\Paradox Launcher v2",
                "LauncherInstallation", null) as string;
            return installPath.ResolvePath();
        }
    }

    private static void PopulateDlc(Selection paradoxLauncher = null)
    {
        paradoxLauncher ??= Selection.FromId(Platform.Paradox, "PL");
        if (paradoxLauncher is null)
            return;
        paradoxLauncher.ExtraSelections.Clear();
        foreach (Selection selection in Selection.AllEnabled.Where(s =>
                     !s.Equals(paradoxLauncher) && s.Publisher == "Paradox Interactive"))
            _ = paradoxLauncher.ExtraSelections.Add(selection);
        if (paradoxLauncher.ExtraSelections.Count > 0)
            return;
        foreach (Selection selection in Selection.All.Keys.Where(s =>
                     !s.Equals(paradoxLauncher) && s.Publisher == "Paradox Interactive"))
            _ = paradoxLauncher.ExtraSelections.Add(selection);
    }

    internal static bool DlcDialog(Form form)
    {
        Selection paradoxLauncher = Selection.FromId(Platform.Paradox, "PL");
        if (paradoxLauncher is null || !paradoxLauncher.Enabled)
            return false;
        PopulateDlc(paradoxLauncher);
        if (paradoxLauncher.ExtraSelections.Count > 0)
            return false;
        using DialogForm dialogForm = new(form);
        return dialogForm.Show(SystemIcons.Warning,
            "WARNING: There are no scanned games with DLC that can be added to the Paradox Launcher!"
            + "\n\nInstalling DLC unlockers for the Paradox Launcher alone can cause existing configurations to be deleted!",
            "Ignore", "Cancel",
            "Paradox Launcher") != DialogResult.OK;
    }

    internal static async Task<RepairResult> Repair(Form form, Selection selection)
    {
        InstallForm installForm = form as InstallForm;
        StringBuilder dialogText = null;
        if (installForm is null)
        {
            Program.Canceled = false;
            dialogText = new();
        }

        using DialogForm dialogForm = new(form);
        bool smokeInstalled = false;
        byte[] steamOriginalSdk32 = null;
        byte[] steamOriginalSdk64 = null;
        bool screamInstalled = false;
        byte[] epicOriginalSdk32 = null;
        byte[] epicOriginalSdk64 = null;
        foreach (string directory in selection.DllDirectories.TakeWhile(_ => !Program.Canceled))
        {
            bool koaloaderInstalled = Koaloader.AutoLoadDLLs
                .Select(pair => (pair.unlocker, path: directory + @"\" + pair.dll))
                .Any(pair => pair.path.FileExists() && pair.path.IsResourceFile());
            directory.GetSmokeApiComponents(out string api32, out string api32_o, out string api64, out string api64_o,
                out string old_config,
                out string config, out _, out _, out _);
            smokeInstalled = smokeInstalled || api32_o.FileExists() || api64_o.FileExists()
                             || (old_config.FileExists() || config.FileExists()) && !koaloaderInstalled
                             || api32.FileExists() && api32.IsResourceFile(ResourceIdentifier.Steamworks32)
                             || api64.FileExists() && api64.IsResourceFile(ResourceIdentifier.Steamworks64);
            await SmokeAPI.Uninstall(directory, deleteOthers: false);
            if (steamOriginalSdk32 is null && api32.FileExists() &&
                !api32.IsResourceFile(ResourceIdentifier.Steamworks32))
                steamOriginalSdk32 = api32.ReadFileBytes(true);
            if (steamOriginalSdk64 is null && api64.FileExists() &&
                !api64.IsResourceFile(ResourceIdentifier.Steamworks64))
                steamOriginalSdk64 = api64.ReadFileBytes(true);
            directory.GetScreamApiComponents(out api32, out api32_o, out api64, out api64_o, out config,
                out string log);
            screamInstalled = screamInstalled || api32_o.FileExists() || api64_o.FileExists()
                              || (config.FileExists() || log.FileExists()) && !koaloaderInstalled
                              || api32.FileExists() && api32.IsResourceFile(ResourceIdentifier.EpicOnlineServices32)
                              || api64.FileExists() && api64.IsResourceFile(ResourceIdentifier.EpicOnlineServices64);
            await ScreamAPI.Uninstall(directory, deleteOthers: false);
            if (epicOriginalSdk32 is null && api32.FileExists() &&
                !api32.IsResourceFile(ResourceIdentifier.EpicOnlineServices32))
                epicOriginalSdk32 = api32.ReadFileBytes(true);
            if (epicOriginalSdk64 is null && api64.FileExists() &&
                !api64.IsResourceFile(ResourceIdentifier.EpicOnlineServices64))
                epicOriginalSdk64 = api64.ReadFileBytes(true);
        }

        if (steamOriginalSdk32 is not null || steamOriginalSdk64 is not null || epicOriginalSdk32 is not null ||
            epicOriginalSdk64 is not null)
        {
            bool neededRepair = false;
            foreach (string directory in selection.DllDirectories.TakeWhile(_ => !Program.Canceled))
            {
                directory.GetSmokeApiComponents(out string api32, out _, out string api64, out _, out _, out _, out _,
                    out _, out _);
                if (steamOriginalSdk32 is not null && api32.IsResourceFile(ResourceIdentifier.Steamworks32))
                {
                    steamOriginalSdk32.WriteResource(api32);
                    if (installForm is not null)
                        installForm.UpdateUser("Corrected Steamworks: " + api32, LogTextBox.Action);
                    else
                        dialogText.AppendLine("Corrected Steamworks: " + api32);
                    neededRepair = true;
                }

                if (steamOriginalSdk64 is not null && api64.IsResourceFile(ResourceIdentifier.Steamworks64))
                {
                    steamOriginalSdk64.WriteResource(api64);
                    if (installForm is not null)
                        installForm.UpdateUser("Corrected Steamworks: " + api64, LogTextBox.Action);
                    else
                        dialogText.AppendLine("Corrected Steamworks: " + api64);
                    neededRepair = true;
                }

                if (smokeInstalled)
                    await SmokeAPI.Install(directory, selection, generateConfig: false);
                directory.GetScreamApiComponents(out api32, out _, out api64, out _, out _, out _);
                if (epicOriginalSdk32 is not null && api32.IsResourceFile(ResourceIdentifier.EpicOnlineServices32))
                {
                    epicOriginalSdk32.WriteResource(api32);
                    if (installForm is not null)
                        installForm.UpdateUser("Corrected Epic Online Services: " + api32, LogTextBox.Action);
                    else
                        dialogText.AppendLine("Corrected Epic Online Services: " + api32);
                    neededRepair = true;
                }

                if (epicOriginalSdk64 is not null && api64.IsResourceFile(ResourceIdentifier.EpicOnlineServices64))
                {
                    epicOriginalSdk64.WriteResource(api64);
                    if (installForm is not null)
                        installForm.UpdateUser("Corrected Epic Online Services: " + api64, LogTextBox.Action);
                    else
                        dialogText.AppendLine("Corrected Epic Online Services: " + api64);
                    neededRepair = true;
                }

                if (screamInstalled)
                    await ScreamAPI.Install(directory, selection, generateConfig: false);
            }

            if (!Program.Canceled)
            {
                if (neededRepair)
                {
                    if (installForm is not null)
                        installForm.UpdateUser("Paradox Launcher successfully repaired!", LogTextBox.Action);
                    else
                    {
                        dialogText.AppendLine("\nParadox Launcher successfully repaired!");
                        _ = dialogForm.Show(form.Icon, dialogText.ToString(), customFormText: "Paradox Launcher");
                    }

                    return RepairResult.Success;
                }

                if (installForm is not null)
                    installForm.UpdateUser("Paradox Launcher did not need to be repaired.", LogTextBox.Success);
                else
                    _ = dialogForm.Show(SystemIcons.Information, "Paradox Launcher does not need to be repaired.",
                        customFormText: "Paradox Launcher");
                return RepairResult.Unnecessary;
            }
        }

        if (Program.Canceled)
        {
            _ = form is InstallForm
                ? throw new CustomMessageException("Repair failed! The operation was canceled.")
                : dialogForm.Show(SystemIcons.Error, "Paradox Launcher repair failed! The operation was canceled.",
                    customFormText: "Paradox Launcher");
            return RepairResult.Failure;
        }

        _ = form is InstallForm
            ? throw new CustomMessageException(
                "Repair failed! " + "An original Steamworks and/or Epic Online Services file could not be found. "
                                  + "You will likely have to reinstall Paradox Launcher to fix this issue.")
            : dialogForm.Show(SystemIcons.Error,
                "Paradox Launcher repair failed!" + "\n\nAn original Steamworks and/or Epic Online Services file could not be found."
                                                  + "\nYou will likely have to reinstall Paradox Launcher to fix this issue.",
                customFormText: "Paradox Launcher");
        return RepairResult.Failure;
    }
}