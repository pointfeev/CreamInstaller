using Gameloop.Vdf.Linq;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CreamInstaller
{
    public class ProgramSelection
    {
        public bool Enabled = false;

        public string Name;
        public string RootDirectory;
        public int SteamAppId;
        public List<string> SteamApiDllDirectories;

        public VProperty AppInfo = null;

        public readonly SortedList<int, string> AllSteamDlc = new();
        public readonly SortedList<int, string> SelectedSteamDlc = new();
        public readonly List<Tuple<int, string, SortedList<int, string>>> ExtraSteamAppIdDlc = new();

        public bool AreSteamApiDllsLocked
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

        public void ToggleDlc(int dlcAppId, bool enabled)
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

        public void ToggleAllDlc(bool enabled)
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

        public ProgramSelection()
        {
            All.Add(this);
        }

        public static List<ProgramSelection> All => Program.ProgramSelections;

        public static List<ProgramSelection> AllSafe => All.ToList();

        public static List<ProgramSelection> AllSafeEnabled => AllSafe.FindAll(s => s.Enabled);

        public static ProgramSelection FromAppId(int appId)
        {
            return AllSafe.Find(s => s.SteamAppId == appId);
        }

        public static KeyValuePair<int, string>? GetDlcFromAppId(int appId)
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