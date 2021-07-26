using CG.Web.MegaApiClient;
using System.Collections.Generic;

namespace CreamInstaller
{
    public class ProgramSelection
    {
        public string ProgramName;
        public string ProgramDirectory;
        public List<string> SteamApiDllDirectories;
        public INode DownloadNode;

        public ProgramSelection()
        {
            Program.ProgramSelections.Add(this);
        }

        public void Add()
        {
            if (!Program.ProgramSelections.Contains(this))
                Program.ProgramSelections.Add(this);
        }

        public void Remove()
        {
            if (Program.ProgramSelections.Contains(this))
                Program.ProgramSelections.Remove(this);
        }
    }
}
