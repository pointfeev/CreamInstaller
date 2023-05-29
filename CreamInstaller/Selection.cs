using System;
using System.Collections.Concurrent;
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

internal sealed class Selection : IEquatable<Selection>
{
    internal const string DefaultKoaloaderProxy = "version";

    internal static readonly ConcurrentDictionary<Selection, byte> All = new();

    internal readonly HashSet<string> DllDirectories;
    internal readonly List<(string directory, BinaryType binaryType)> ExecutableDirectories;
    internal readonly HashSet<Selection> ExtraSelections = new();
    internal readonly string Id;
    internal readonly string Name;
    internal readonly Platform Platform;
    internal readonly string RootDirectory;

    internal bool Enabled;
    internal string Icon;
    internal bool Koaloader;
    internal string KoaloaderProxy;
    internal string Product;
    internal string Publisher;
    internal string SubIcon;
    internal string Website;

    private Selection(Platform platform, string id, string name, string rootDirectory, HashSet<string> dllDirectories,
        List<(string directory, BinaryType binaryType)> executableDirectories)
    {
        Platform = platform;
        Id = id;
        Name = name;
        RootDirectory = rootDirectory;
        DllDirectories = dllDirectories;
        ExecutableDirectories = executableDirectories;
        _ = All.TryAdd(this, default);
    }

    internal IEnumerable<SelectionDLC> DLC => SelectionDLC.AllSafe.Where(dlc => dlc.Selection.Equals(this));

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

    internal static HashSet<Selection> AllSafe => All.Keys.ToHashSet();

    internal static HashSet<Selection> AllEnabled => All.Keys.Where(s => s.Enabled).ToHashSet();

    public bool Equals(Selection other)
        => other is not null && (ReferenceEquals(this, other) || Id == other.Id && Platform == other.Platform && RootDirectory == other.RootDirectory);

    internal static Selection GetOrCreate(Platform platform, string id, string name, string rootDirectory, HashSet<string> dllDirectories,
        List<(string directory, BinaryType binaryType)> executableDirectories)
        => FromPlatformId(platform, id) ?? new Selection(platform, id, name, rootDirectory, dllDirectories, executableDirectories);

    private void Remove()
    {
        _ = All.TryRemove(this, out _);
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
    {
        foreach (Selection selection in AllSafe)
            selection.Validate(programsToScan);
    }

    internal static Selection FromPlatformId(Platform platform, string gameId) => AllSafe.FirstOrDefault(s => s.Platform == platform && s.Id == gameId);

    public override bool Equals(object obj) => ReferenceEquals(this, obj) || obj is Selection other && Equals(other);

    public override int GetHashCode() => HashCode.Combine(Id, (int)Platform, RootDirectory);
}