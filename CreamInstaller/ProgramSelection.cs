using CreamInstaller.Components;
using CreamInstaller.Resources;

using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace CreamInstaller;

public enum Platform
{
    None = 0,
    Paradox,
    Steam,
    Epic,
    Ubisoft
}

public enum DlcType
{
    Steam,
    SteamHidden,
    EpicCatalogItem,
    EpicEntitlement
}

internal class ProgramSelection
{
    internal bool Enabled;

    internal Platform Platform;
    internal string Id = "0";
    internal string Name = "Program";

    internal string ProductUrl;
    internal string IconUrl;
    internal string SubIconUrl;

    internal string Publisher;

    internal string WebsiteUrl;

    internal string RootDirectory;
    internal List<string> DllDirectories;

    internal readonly SortedList<string, (DlcType type, string name, string icon)> AllDlc = new(PlatformIdComparer.String);
    internal readonly SortedList<string, (DlcType type, string name, string icon)> SelectedDlc = new(PlatformIdComparer.String);

    internal readonly List<(string id, string name, SortedList<string, (DlcType type, string name, string icon)> dlc)> ExtraDlc = new();         // for Paradox Launcher
    internal readonly List<(string id, string name, SortedList<string, (DlcType type, string name, string icon)> dlc)> ExtraSelectedDlc = new(); // for Paradox Launcher

    internal bool AreDllsLocked
    {
        get
        {
            foreach (string directory in DllDirectories)
            {
                if (Platform is Platform.Steam or Platform.Paradox)
                {
                    directory.GetCreamApiComponents(out string sdk32, out string sdk32_o, out string sdk64, out string sdk64_o, out string config);
                    if (sdk32.IsFilePathLocked()
                        || sdk32_o.IsFilePathLocked()
                        || sdk64.IsFilePathLocked()
                        || sdk64_o.IsFilePathLocked()
                        || config.IsFilePathLocked())
                        return true;
                    directory.GetSmokeApiComponents(out sdk32, out sdk32_o, out sdk64, out sdk64_o, out config, out string cache);
                    if (sdk32.IsFilePathLocked()
                        || sdk32_o.IsFilePathLocked()
                        || sdk64.IsFilePathLocked()
                        || sdk64_o.IsFilePathLocked()
                        || config.IsFilePathLocked()
                        || cache.IsFilePathLocked())
                        return true;
                }
                if (Platform is Platform.Epic or Platform.Paradox)
                {
                    directory.GetScreamApiComponents(out string sdk32, out string sdk32_o, out string sdk64, out string sdk64_o, out string config);
                    if (sdk32.IsFilePathLocked()
                        || sdk32_o.IsFilePathLocked()
                        || sdk64.IsFilePathLocked()
                        || sdk64_o.IsFilePathLocked()
                        || config.IsFilePathLocked())
                        return true;
                }
                if (Platform is Platform.Ubisoft)
                {
                    directory.GetUplayR1Components(out string sdk32, out string sdk32_o, out string sdk64, out string sdk64_o, out string config);
                    if (sdk32.IsFilePathLocked()
                        || sdk32_o.IsFilePathLocked()
                        || sdk64.IsFilePathLocked()
                        || sdk64_o.IsFilePathLocked()
                        || config.IsFilePathLocked())
                        return true;
                    directory.GetUplayR2Components(out string old_sdk32, out string old_sdk64, out sdk32, out sdk32_o, out sdk64, out sdk64_o, out config);
                    if (old_sdk32.IsFilePathLocked()
                        || old_sdk64.IsFilePathLocked()
                        || sdk32.IsFilePathLocked()
                        || sdk32_o.IsFilePathLocked()
                        || sdk64.IsFilePathLocked()
                        || sdk64_o.IsFilePathLocked()
                        || config.IsFilePathLocked())
                        return true;
                }
            }
            return false;
        }
    }

    private void Toggle(string dlcAppId, (DlcType type, string name, string icon) dlcApp, bool enabled)
    {
        if (enabled) SelectedDlc[dlcAppId] = dlcApp;
        else _ = SelectedDlc.Remove(dlcAppId);
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
        Enabled = SelectedDlc.Any() || ExtraSelectedDlc.Any();
    }

    internal ProgramSelection() => All.Add(this);

    internal void Validate()
    {
        if (Program.IsGameBlocked(Name, RootDirectory))
        {
            _ = All.Remove(this);
            return;
        }
        if (!Directory.Exists(RootDirectory))
        {
            _ = All.Remove(this);
            return;
        }
        _ = DllDirectories.RemoveAll(directory => !Directory.Exists(directory));
        if (!DllDirectories.Any()) _ = All.Remove(this);
    }

    internal void Validate(List<(Platform platform, string id, string name)> programsToScan)
    {
        if (programsToScan is null || !programsToScan.Any(p => p.platform == Platform && p.id == Id))
        {
            _ = All.Remove(this);
            return;
        }
        Validate();
    }

    internal static void ValidateAll() => AllSafe.ForEach(selection => selection.Validate());

    internal static void ValidateAll(List<(Platform platform, string id, string name)> programsToScan) => AllSafe.ForEach(selection => selection.Validate(programsToScan));

    internal static readonly List<ProgramSelection> All = new();

    internal static List<ProgramSelection> AllSafe => All.ToList();

    internal static List<ProgramSelection> AllEnabled => AllSafe.FindAll(s => s.Enabled);

    internal static ProgramSelection FromPlatformId(Platform platform, string gameId) => AllSafe.Find(s => s.Platform == platform && s.Id == gameId);

    internal static (string gameId, (DlcType type, string name, string icon) app)? GetDlcFromPlatformId(Platform platform, string dlcId)
    {
        foreach (ProgramSelection selection in AllSafe.Where(s => s.Platform == platform))
            foreach (KeyValuePair<string, (DlcType type, string name, string icon)> pair in selection.AllDlc.Where(p => p.Key == dlcId))
                return (selection.Id, pair.Value);
        return null;
    }
}
