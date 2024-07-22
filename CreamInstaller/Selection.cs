using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using CreamInstaller.Forms;
using CreamInstaller.Resources;
using CreamInstaller.Utility;
using static CreamInstaller.Resources.Resources;

namespace CreamInstaller;

public enum Platform
{
    None = 0,
    Paradox,
    Steam,
    Epic,
    Ubisoft
}

internal sealed class Selection : IEquatable<Selection>
{
    internal const string DefaultProxy = "winmm";

    internal static readonly ConcurrentDictionary<Selection, byte> All = new();

    internal readonly HashSet<string> DllDirectories;
    internal readonly List<(string directory, BinaryType binaryType)> ExecutableDirectories;
    internal readonly HashSet<Selection> ExtraSelections = [];
    internal readonly string Id;
    internal readonly string Name;
    internal readonly Platform Platform;
    internal readonly string RootDirectory;
    internal readonly TreeNode TreeNode;
    internal string Icon;
    internal bool UseProxy;
    internal string Proxy;
    internal string Product;
    internal string Publisher;
    internal string SubIcon;
    internal string Website;

    internal IEnumerable<string> GetAvailableProxies()
    {
        if (!Program.UseSmokeAPI && Platform is Platform.Steam or Platform.Paradox)
            return CreamAPI.ProxyDLLs;
        return EmbeddedResources.Where(r => r.StartsWith("Koaloader", StringComparison.Ordinal)).Select(p =>
        {
            p.GetProxyInfoFromIdentifier(out string proxyName, out _);
            return proxyName;
        }).ToHashSet();
    }

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
        TreeNode = new() { Tag = Platform, Name = Id, Text = Name };
        SelectForm selectForm = SelectForm.Current;
        if (selectForm is null)
            return;
        Enabled = selectForm.allCheckBox.Checked;
        UseProxy = selectForm.proxyAllCheckBox.Checked;
    }

    internal static IEnumerable<Selection> AllEnabled => All.Keys.Where(s => s.Enabled);

    internal bool Enabled
    {
        get => TreeNode.Checked;
        set => TreeNode.Checked = value;
    }

    internal IEnumerable<SelectionDLC> DLC => SelectionDLC.All.Keys.Where(dlc => Equals(dlc.Selection, this));

    public bool Equals(Selection other) => other is not null &&
                                           (ReferenceEquals(this, other) ||
                                            Id == other.Id && Platform == other.Platform);

    internal static Selection GetOrCreate(Platform platform, string id, string name, string rootDirectory,
        HashSet<string> dllDirectories,
        List<(string directory, BinaryType binaryType)> executableDirectories)
        => FromId(platform, id) ??
           new Selection(platform, id, name, rootDirectory, dllDirectories, executableDirectories);

    internal void Remove()
    {
        _ = All.TryRemove(this, out _);
        TreeNode.Remove();
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
        foreach (Selection selection in All.Keys.ToHashSet())
            selection.Validate(programsToScan);
    }

    internal static Selection FromId(Platform platform, string gameId) =>
        All.Keys.FirstOrDefault(s => s.Platform == platform && s.Id == gameId);

    public override bool Equals(object obj) => ReferenceEquals(this, obj) || obj is Selection other && Equals(other);

    public override int GetHashCode() => HashCode.Combine(Id, (int)Platform);
}