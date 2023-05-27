using System.Collections.Generic;
using System.Linq;
using CreamInstaller.Resources;
using CreamInstaller.Utility;
using static CreamInstaller.Resources.Resources;

namespace CreamInstaller;

public enum Platform
{
    None = 0, Paradox, Steam,
    Epic, Ubisoft
}

internal sealed class Selection
{
    internal const string DefaultKoaloaderProxy = "version";

    internal static readonly HashSet<Selection> All = new();
    internal readonly HashSet<Selection> ExtraSelections = new();

    internal HashSet<string> DllDirectories;
    internal bool Enabled;
    internal HashSet<(string directory, BinaryType binaryType)> ExecutableDirectories;
    internal string Icon;
    internal string Id = "0";
    internal bool Koaloader;
    internal string KoaloaderProxy;
    internal string Name = "Program";
    internal Platform Platform;
    internal string Product;
    internal string Publisher;
    internal string RootDirectory;
    internal string SubIcon;
    internal string Website;

    internal Selection() => All.Add(this);

    internal IEnumerable<SelectionDLC> DLC => SelectionDLC.AllSafe.Where(dlc => dlc.Selection == this);

    internal bool AreDllsLocked
    {
        get
        {
            foreach (string directory in DllDirectories)
            {
                if (Platform is Platform.Steam or Platform.Paradox)
                {
                    directory.GetCreamApiComponents(out string api32, out string api32_o, out string api64, out string api64_o, out string config);
                    if (api32.FileLocked() || api32_o.FileLocked() || api64.FileLocked() || api64_o.FileLocked() || config.FileLocked())
                        return true;
                    directory.GetSmokeApiComponents(out api32, out api32_o, out api64, out api64_o, out string old_config, out config, out string old_log,
                        out string log, out string cache);
                    if (api32.FileLocked() || api32_o.FileLocked() || api64.FileLocked() || api64_o.FileLocked() || old_config.FileLocked()
                     || config.FileLocked() || old_log.FileLocked() || log.FileLocked() || cache.FileLocked())
                        return true;
                }
                if (Platform is Platform.Epic or Platform.Paradox)
                {
                    directory.GetScreamApiComponents(out string api32, out string api32_o, out string api64, out string api64_o, out string config,
                        out string log);
                    if (api32.FileLocked() || api32_o.FileLocked() || api64.FileLocked() || api64_o.FileLocked() || config.FileLocked() || log.FileLocked())
                        return true;
                }
                if (Platform is Platform.Ubisoft)
                {
                    directory.GetUplayR1Components(out string api32, out string api32_o, out string api64, out string api64_o, out string config,
                        out string log);
                    if (api32.FileLocked() || api32_o.FileLocked() || api64.FileLocked() || api64_o.FileLocked() || config.FileLocked() || log.FileLocked())
                        return true;
                    directory.GetUplayR2Components(out string old_api32, out string old_api64, out api32, out api32_o, out api64, out api64_o, out config,
                        out log);
                    if (old_api32.FileLocked() || old_api64.FileLocked() || api32.FileLocked() || api32_o.FileLocked() || api64.FileLocked()
                     || api64_o.FileLocked() || config.FileLocked() || log.FileLocked())
                        return true;
                }
            }
            return false;
        }
    }

    internal static List<Selection> AllSafe => All.ToList();

    internal static List<Selection> AllEnabled => AllSafe.FindAll(s => s.Enabled);

    private void Remove()
    {
        _ = All.Remove(this);
        foreach (SelectionDLC dlc in DLC)
            dlc.Selection = null;
    }

    private void Validate(List<(Platform platform, string id, string name)> programsToScan)
    {
        if (programsToScan is null || !programsToScan.Any(p => p.platform == Platform && p.id == Id))
        {
            Remove();
            return;
        }
        if (Program.IsGameBlocked(Name, RootDirectory))
        {
            Remove();
            return;
        }
        if (!RootDirectory.DirectoryExists())
        {
            Remove();
            return;
        }
        _ = DllDirectories.RemoveWhere(directory => !directory.DirectoryExists());
        if (DllDirectories.Count < 1)
            Remove();
    }

    internal static void ValidateAll(List<(Platform platform, string id, string name)> programsToScan)
        => AllSafe.ForEach(selection => selection.Validate(programsToScan));

    internal static Selection FromPlatformId(Platform platform, string gameId) => AllSafe.Find(s => s.Platform == platform && s.Id == gameId);
}