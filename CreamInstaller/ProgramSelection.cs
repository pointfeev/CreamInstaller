using System.Collections.Generic;

namespace CreamInstaller
{
    public class ProgramSelection
    {
        public bool Enabled = true;

        public string Identifier;
        public string DisplayName;
        public string RootDirectory;
        public int SteamAppId;
        public List<string> SteamApiDllDirectories;

        public bool IsProgramRunning
        {
            get
            {
                foreach (string directory in SteamApiDllDirectories)
                {
                    string file = directory + "\\steam_api64.dll";
                    if (file.IsFilePathLocked()) return true;
                }
                return false;
            }
        }

        public ProgramSelection() => Program.ProgramSelections.Add(this);

        public void Toggle(bool Enabled) => this.Enabled = Enabled;
    }
}