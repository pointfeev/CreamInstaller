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

        public List<Tuple<int, string>> AllSteamDlc = new();
        public List<Tuple<int, string>> SelectedSteamDlc = new();
        public List<Tuple<int, string, List<Tuple<int, string>>>> ExtraSteamAppIdDlc = new();

        public bool AreSteamApiDllsLocked
        {
            get
            {
                foreach (string directory in SteamApiDllDirectories)
                {
                    string api = directory + @"\steam_api.dll";
                    string api64 = directory + @"\steam_api64.dll";
                    if (api.IsFilePathLocked() || api64.IsFilePathLocked()) return true;
                }
                return false;
            }
        }

        private void Toggle(Tuple<int, string> dlcApp, bool enabled)
        {
            if (enabled)
            {
                if (!SelectedSteamDlc.Contains(dlcApp)) SelectedSteamDlc.Add(dlcApp);
            }
            else SelectedSteamDlc.Remove(dlcApp);
        }

        public void ToggleDlc(string dlcName, bool enabled)
        {
            foreach (Tuple<int, string> dlcApp in AllSteamDlc)
            {
                if (dlcApp.Item2 == dlcName)
                {
                    Toggle(dlcApp, enabled);
                    break;
                }
            }
            Enabled = SelectedSteamDlc.Any();
        }

        public void ToggleAllDlc(bool enabled)
        {
            if (!enabled) SelectedSteamDlc.Clear();
            else foreach (Tuple<int, string> dlcApp in AllSteamDlc) Toggle(dlcApp, enabled);
            Enabled = SelectedSteamDlc.Any();
        }

        public ProgramSelection() => All.Add(this);

        public static List<ProgramSelection> All => Program.ProgramSelections;

        public static List<ProgramSelection> AllSafe => All.ToList();

        public static List<ProgramSelection> AllSafeEnabled => AllSafe.FindAll(s => s.Enabled);

        public static ProgramSelection FromName(string displayName) => AllSafe.Find(s => s.Name == displayName);

        public static Tuple<int, string> GetDlc(string displayName)
        {
            foreach (ProgramSelection selection in AllSafe)
            {
                foreach (Tuple<int, string> app in selection.AllSteamDlc)
                {
                    if (app.Item2 == displayName)
                    {
                        return app;
                    }
                }
            }
            return null;
        }
    }
}