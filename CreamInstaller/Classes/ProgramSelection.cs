using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using Gameloop.Vdf.Linq;

namespace CreamInstaller
{
    internal class ProgramSelection
    {
        internal bool Enabled = false;
        internal bool Usable = true;

        internal string Name;
        internal string RootDirectory;
        internal int SteamAppId;
        internal List<string> SteamApiDllDirectories;

        internal VProperty AppInfo = null;

        internal readonly SortedList<int, string> AllSteamDlc = new();
        internal readonly SortedList<int, string> SelectedSteamDlc = new();
        internal readonly List<Tuple<int, string, SortedList<int, string>>> ExtraSteamAppIdDlc = new();

        internal bool AreSteamApiDllsLocked
        {
            get
            {
                foreach (string directory in SteamApiDllDirectories)
                {
                    string api = directory + @"\steam_api.dll";
                    string api64 = directory + @"\steam_api64.dll";
                    if (api.IsFilePathLocked() || api64.IsFilePathLocked())
                    {
                        return true;
                    }
                }
                return false;
            }
        }

        private void Toggle(KeyValuePair<int, string> dlcApp, bool enabled)
        {
            if (enabled)
            {
                SelectedSteamDlc[dlcApp.Key] = dlcApp.Value;
            }
            else
            {
                SelectedSteamDlc.Remove(dlcApp.Key);
            }
        }

        internal void ToggleDlc(int dlcAppId, bool enabled)
        {
            foreach (KeyValuePair<int, string> dlcApp in AllSteamDlc)
            {
                if (dlcApp.Key == dlcAppId)
                {
                    Toggle(dlcApp, enabled);
                    break;
                }
            }
            Enabled = SelectedSteamDlc.Any();
        }

        internal void ToggleAllDlc(bool enabled)
        {
            if (!enabled)
            {
                SelectedSteamDlc.Clear();
            }
            else
            {
                foreach (KeyValuePair<int, string> dlcApp in AllSteamDlc)
                {
                    Toggle(dlcApp, enabled);
                }
            }

            Enabled = SelectedSteamDlc.Any();
        }

        internal ProgramSelection() => All.Add(this);

        internal void Validate()
        {
            SteamApiDllDirectories.RemoveAll(directory => !Directory.Exists(directory));
            if (!Directory.Exists(RootDirectory) || !SteamApiDllDirectories.Any())
            {
                All.Remove(this);
            }
        }

        internal static void ValidateAll() => All.ForEach(selection => selection.Validate());

        internal static List<ProgramSelection> All => Program.ProgramSelections;

        internal static List<ProgramSelection> AllSafe => All.FindAll(s => s.Usable);

        internal static List<ProgramSelection> AllSafeEnabled => AllSafe.FindAll(s => s.Enabled);

        internal static ProgramSelection FromAppId(int appId) => AllSafe.Find(s => s.SteamAppId == appId);

        internal static KeyValuePair<int, string>? GetDlcFromAppId(int appId)
        {
            foreach (ProgramSelection selection in AllSafe)
            {
                foreach (KeyValuePair<int, string> app in selection.AllSteamDlc)
                {
                    if (app.Key == appId)
                    {
                        return app;
                    }
                }
            }

            return null;
        }
    }
}