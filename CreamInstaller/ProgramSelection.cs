using CG.Web.MegaApiClient;
using System.Collections.Generic;

namespace CreamInstaller
{
    public class ProgramSelection
    {
        public bool Enabled = true;

        public string ProgramName;
        public string ProgramDirectory;
        public List<string> SteamApiDllDirectories;
        public INode DownloadNode;

        public bool IsProgramRunning
        {
            get
            {
                foreach (string directory in SteamApiDllDirectories)
                {
                    string file = directory + "\\steam_api64.dll";
                    if (file.IsFilePathLocked())
                    {
                        return true;
                    }
                }
                return false;
            }
        }

        public ProgramSelection()
        {
            Program.ProgramSelections.Add(this);
        }

        public void Toggle(bool Enabled)
        {
            this.Enabled = Enabled;
        }
    }
}
