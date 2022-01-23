using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

using Gameloop.Vdf.Linq;

namespace CreamInstaller
{
    internal class ProgramSelection
    {
        internal bool Enabled = false;
        internal bool Usable = true;

        internal int SteamAppId = 0;
        internal string Name = "Program";

        internal Image Icon;
        private string iconPath;
        internal string IconPath
        {
            get => iconPath;
            set
            {
                iconPath = value;
                Task.Run(async () => Icon = await Program.GetImageFromUrl(iconPath));
            }
        }
        internal string IconStaticID
        {
            set => IconPath = $"https://cdn.cloudflare.steamstatic.com/steamcommunity/public/images/apps/{SteamAppId}/{value}.jpg";
        }

        internal Image ClientIcon;
        private string clientIconPath;
        internal string ClientIconPath
        {
            get => clientIconPath;
            set
            {
                clientIconPath = value;
                Task.Run(async () => ClientIcon = await Program.GetImageFromUrl(clientIconPath));
            }
        }
        internal string ClientIconStaticID
        {
            set => ClientIconPath = $"https://cdn.cloudflare.steamstatic.com/steamcommunity/public/images/apps/{SteamAppId}/{value}.ico";
        }

        internal string RootDirectory;
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
                    if (api.IsFilePathLocked() || api64.IsFilePathLocked()) return true;
                }
                return false;
            }
        }

        private void Toggle(KeyValuePair<int, string> dlcApp, bool enabled)
        {
            if (enabled) SelectedSteamDlc[dlcApp.Key] = dlcApp.Value;
            else SelectedSteamDlc.Remove(dlcApp.Key);
        }

        internal void ToggleDlc(int dlcAppId, bool enabled)
        {
            foreach (KeyValuePair<int, string> dlcApp in AllSteamDlc)
                if (dlcApp.Key == dlcAppId)
                {
                    Toggle(dlcApp, enabled);
                    break;
                }
            Enabled = SelectedSteamDlc.Any();
        }

        internal void ToggleAllDlc(bool enabled)
        {
            if (!enabled) SelectedSteamDlc.Clear();
            else foreach (KeyValuePair<int, string> dlcApp in AllSteamDlc) Toggle(dlcApp, enabled);
            Enabled = SelectedSteamDlc.Any();
        }

        internal ProgramSelection() => All.Add(this);

        internal void Validate()
        {
            if (Program.BlockProtectedGames && Program.IsGameBlocked(Name, RootDirectory))
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

        internal static KeyValuePair<int, string>? GetDlcFromAppId(int appId)
        {
            foreach (ProgramSelection selection in AllUsable)
                foreach (KeyValuePair<int, string> app in selection.AllSteamDlc)
                    if (app.Key == appId) return app;
            return null;
        }
    }
}