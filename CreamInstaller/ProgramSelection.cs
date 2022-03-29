using System.Collections.Generic;
using System.IO;
using System.Linq;

using CreamInstaller.Components;

namespace CreamInstaller;

public enum DlcType
{
    Steam = 0,
    EpicCatalogItem = 1,
    EpicEntitlement = 2
}

internal class ProgramSelection
{
    internal bool Enabled;

    internal string Id = "0";
    internal string Name = "Program";

    internal string ProductUrl;
    internal string IconUrl;
    internal string SubIconUrl;

    internal string Publisher;

    internal string RootDirectory;
    internal List<string> DllDirectories;

    internal bool IsSteam;
    internal bool IsEpic;

    internal readonly SortedList<string, (DlcType type, string name, string icon)> AllDlc = new(AppIdComparer.Comparer);
    internal readonly SortedList<string, (DlcType type, string name, string icon)> SelectedDlc = new(AppIdComparer.Comparer);
    internal readonly List<(string id, string name, SortedList<string, (DlcType type, string name, string icon)> dlc)> ExtraDlc = new(); // for Paradox Launcher

    internal bool AreDllsLocked
    {
        get
        {
            foreach (string directory in DllDirectories)
            {
                directory.GetCreamApiComponents(out string sdk32, out string sdk32_o, out string sdk64, out string sdk64_o, out string config);
                if (sdk32.IsFilePathLocked()
                    || sdk32_o.IsFilePathLocked()
                    || sdk64.IsFilePathLocked()
                    || sdk64_o.IsFilePathLocked()
                    || config.IsFilePathLocked())
                    return true;
                directory.GetScreamApiComponents(out sdk32, out sdk32_o, out sdk64, out sdk64_o, out config);
                if (sdk32.IsFilePathLocked()
                    || sdk32_o.IsFilePathLocked()
                    || sdk64.IsFilePathLocked()
                    || sdk64_o.IsFilePathLocked()
                    || config.IsFilePathLocked())
                    return true;
            }
            return false;
        }
    }

    private void Toggle(string dlcAppId, (DlcType type, string name, string icon) dlcApp, bool enabled)
    {
        if (enabled) SelectedDlc[dlcAppId] = dlcApp;
        else SelectedDlc.Remove(dlcAppId);
    }

    internal void ToggleDlc(string dlcId, bool enabled)
    {
        foreach (KeyValuePair<string, (DlcType type, string name, string icon)> pair in AllDlc)
        {
            string appId = pair.Key;
            (DlcType type, string name, string icon) dlcApp = pair.Value;
            if (appId == dlcId)
            {
                Toggle(appId, dlcApp, enabled);
                break;
            }
        }
        Enabled = SelectedDlc.Any() || ExtraDlc.Any();
    }

    internal ProgramSelection() => All.Add(this);

    internal void Validate()
    {
        if (Program.IsGameBlocked(Name, RootDirectory))
        {
            All.Remove(this);
            return;
        }
        if (!Directory.Exists(RootDirectory))
        {
            All.Remove(this);
            return;
        }
        DllDirectories.RemoveAll(directory => !Directory.Exists(directory));
        if (!DllDirectories.Any()) All.Remove(this);
    }

    internal void Validate(List<(string platform, string id, string name)> programsToScan)
    {
        if (programsToScan is null || !programsToScan.Any(p => p.id == Id))
        {
            All.Remove(this);
            return;
        }
        Validate();
    }

    internal static void ValidateAll() => AllSafe.ForEach(selection => selection.Validate());

    internal static void ValidateAll(List<(string platform, string id, string name)> programsToScan) => AllSafe.ForEach(selection => selection.Validate(programsToScan));

    internal static List<ProgramSelection> All = new();

    internal static List<ProgramSelection> AllSafe => All.ToList();

    internal static List<ProgramSelection> AllEnabled => AllSafe.FindAll(s => s.Enabled);

    internal static ProgramSelection FromId(string gameId) => AllSafe.Find(s => s.Id == gameId);

    internal static (string gameId, (DlcType type, string name, string icon) app)? GetDlcFromId(string dlcId)
    {
        foreach (ProgramSelection selection in AllSafe)
            foreach (KeyValuePair<string, (DlcType type, string name, string icon)> pair in selection.AllDlc)
                if (pair.Key == dlcId) return (selection.Id, pair.Value);
        return null;
    }
}
