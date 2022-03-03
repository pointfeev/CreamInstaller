using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using Gameloop.Vdf.Linq;

namespace CreamInstaller;

internal class ProgramSelection
{
    internal bool Enabled = false;
    internal bool Usable = true;

    internal string Id = "0";
    internal string Name = "Program";

    internal string ProductUrl = null;

    internal string IconUrl = null;
    internal string ClientIconUrl = null;

    internal string RootDirectory = null;
    internal List<string> DllDirectories = null;

    internal bool IsSteam = false;
    internal VProperty AppInfo = null;

    internal readonly SortedList<string, (string name, string icon)> AllDlc = new();
    internal readonly SortedList<string, (string name, string icon)> SelectedDlc = new();
    internal readonly List<Tuple<string, string, SortedList<string, (string name, string icon)>>> ExtraDlc = new();

    internal bool AreDllsLocked
    {
        get
        {
            foreach (string directory in DllDirectories)
            {
                directory.GetCreamApiComponents(out string api, out string api_o, out string api64, out string api64_o, out string cApi);
                if (api.IsFilePathLocked()
                    || api_o.IsFilePathLocked()
                    || api64.IsFilePathLocked()
                    || api64_o.IsFilePathLocked()
                    || cApi.IsFilePathLocked())
                    return true;
                directory.GetScreamApiComponents(out string sdk, out string sdk_o, out string sdk64, out string sdk64_o, out string sApi);
                if (sdk.IsFilePathLocked()
                    || sdk_o.IsFilePathLocked()
                    || sdk64.IsFilePathLocked()
                    || sdk64_o.IsFilePathLocked()
                    || sApi.IsFilePathLocked())
                    return true;
            }
            return false;
        }
    }

    private void Toggle(string dlcAppId, (string name, string icon) dlcApp, bool enabled)
    {
        if (enabled) SelectedDlc[dlcAppId] = dlcApp;
        else SelectedDlc.Remove(dlcAppId);
    }

    internal void ToggleDlc(string dlcAppId, bool enabled)
    {
        foreach (KeyValuePair<string, (string name, string icon)> pair in AllDlc)
        {
            string appId = pair.Key;
            (string name, string icon) dlcApp = pair.Value;
            if (appId == dlcAppId)
            {
                Toggle(appId, dlcApp, enabled);
                break;
            }
        }
        Enabled = SelectedDlc.Any();
    }

    internal void ToggleAllDlc(bool enabled)
    {
        if (!enabled) SelectedDlc.Clear();
        else foreach (KeyValuePair<string, (string name, string icon)> pair in AllDlc)
            {
                string appId = pair.Key;
                (string name, string icon) dlcApp = pair.Value;
                Toggle(appId, dlcApp, enabled);
            }
        Enabled = SelectedDlc.Any();
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

    internal static void ValidateAll() => AllSafe.ForEach(selection => selection.Validate());

    internal static List<ProgramSelection> All = new();

    internal static List<ProgramSelection> AllSafe => All.ToList();

    internal static List<ProgramSelection> AllUsable => All.FindAll(s => s.Usable);

    internal static List<ProgramSelection> AllUsableEnabled => AllUsable.FindAll(s => s.Enabled);

    internal static ProgramSelection FromId(string id) => AllUsable.Find(s => s.Id == id);

    internal static (string gameAppId, (string name, string icon) app)? GetDlcFromId(string appId)
    {
        foreach (ProgramSelection selection in AllUsable)
            foreach (KeyValuePair<string, (string name, string icon)> pair in selection.AllDlc)
                if (pair.Key == appId) return (selection.Id, pair.Value);
        return null;
    }
}
