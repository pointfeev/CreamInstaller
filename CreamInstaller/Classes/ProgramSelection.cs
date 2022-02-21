using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using Gameloop.Vdf.Linq;

namespace CreamInstaller.Classes;

internal class ProgramSelection
{
    internal bool Enabled = false;
    internal bool Usable = true;

    internal int SteamAppId = 0;
    internal string Name = "Program";

    internal string IconStaticID = null;
    internal string ClientIconStaticID = null;

    internal string RootDirectory;
    internal List<string> SteamApiDllDirectories;

    internal VProperty AppInfo = null;

    internal readonly SortedList<int, (string name, string iconStaticId)> AllSteamDlc = new();
    internal readonly SortedList<int, (string name, string iconStaticId)> SelectedSteamDlc = new();
    internal readonly List<Tuple<int, string, SortedList<int, (string name, string iconStaticId)>>> ExtraSteamAppIdDlc = new();

    internal bool AreSteamApiDllsLocked
    {
        get
        {
            foreach (string directory in SteamApiDllDirectories)
            {
                directory.GetApiComponents(out string api, out string api_o, out string api64, out string api64_o, out string cApi);
                if (api.IsFilePathLocked()
                    || api_o.IsFilePathLocked()
                    || api64.IsFilePathLocked()
                    || api64_o.IsFilePathLocked()
                    || cApi.IsFilePathLocked())
                    return true;
            }
            return false;
        }
    }

    private void Toggle(int dlcAppId, (string name, string iconStaticId) dlcApp, bool enabled)
    {
        if (enabled) SelectedSteamDlc[dlcAppId] = dlcApp;
        else SelectedSteamDlc.Remove(dlcAppId);
    }

    internal void ToggleDlc(int dlcAppId, bool enabled)
    {
        foreach (KeyValuePair<int, (string name, string iconStaticId)> pair in AllSteamDlc)
        {
            int appId = pair.Key;
            (string name, string iconStaticId) dlcApp = pair.Value;
            if (appId == dlcAppId)
            {
                Toggle(appId, dlcApp, enabled);
                break;
            }
        }
        Enabled = SelectedSteamDlc.Any();
    }

    internal void ToggleAllDlc(bool enabled)
    {
        if (!enabled) SelectedSteamDlc.Clear();
        else foreach (KeyValuePair<int, (string name, string iconStaticId)> pair in AllSteamDlc)
            {
                int appId = pair.Key;
                (string name, string iconStaticId) dlcApp = pair.Value;
                Toggle(appId, dlcApp, enabled);
            }
        Enabled = SelectedSteamDlc.Any();
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
        SteamApiDllDirectories.RemoveAll(directory => !Directory.Exists(directory));
        if (!SteamApiDllDirectories.Any()) All.Remove(this);
    }

    internal static void ValidateAll() => AllSafe.ForEach(selection => selection.Validate());

    internal static List<ProgramSelection> All => Program.ProgramSelections;

    internal static List<ProgramSelection> AllSafe => All.ToList();

    internal static List<ProgramSelection> AllUsable => All.FindAll(s => s.Usable);

    internal static List<ProgramSelection> AllUsableEnabled => AllUsable.FindAll(s => s.Enabled);

    internal static ProgramSelection FromAppId(int appId) => AllUsable.Find(s => s.SteamAppId == appId);

    internal static (int gameAppId, (string name, string iconStaticId) app)? GetDlcFromAppId(int appId)
    {
        foreach (ProgramSelection selection in AllUsable)
            foreach (KeyValuePair<int, (string name, string iconStaticId)> pair in selection.AllSteamDlc)
                if (pair.Key == appId) return (selection.SteamAppId, pair.Value);
        return null;
    }
}
