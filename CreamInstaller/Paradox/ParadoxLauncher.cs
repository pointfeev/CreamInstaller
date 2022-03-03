using System.Drawing;
using System.Linq;
using System.Windows.Forms;

using Microsoft.Win32;

namespace CreamInstaller.Paradox;

internal static class ParadoxLauncher
{
    private static string installPath = null;
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
                return new DialogForm(form).Show(SystemIcons.Warning,
                    $"WARNING: There are no installed games with DLC that can be added to the Paradox Launcher!" +
                    "\n\nInstalling CreamAPI/ScreamAPI for the Paradox Launcher is pointless, since no DLC will be added to the configuration!",
                    "Ignore", "Cancel") != DialogResult.OK;
            }
        }
        return false;
    }
}
