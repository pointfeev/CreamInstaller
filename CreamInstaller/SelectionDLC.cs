using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace CreamInstaller;

public enum DLCType
{
    None = 0, Steam, SteamHidden, EpicCatalogItem,
    EpicEntitlement
}

internal sealed class SelectionDLC : IEquatable<SelectionDLC>
{
    private static readonly ConcurrentDictionary<SelectionDLC, byte> All = new();

    internal readonly string Id;
    internal readonly string Name;
    internal readonly DLCType Type;

    internal bool Enabled;
    internal string Icon;
    internal string Product;
    internal string Publisher;
    private Selection selection;

    private SelectionDLC(DLCType type, string id, string name)
    {
        Type = type;
        Id = id;
        Name = name;
    }

    internal Selection Selection
    {
        get => selection;
        set
        {
            selection = value;
            _ = value is null ? All.TryRemove(this, out _) : All.TryAdd(this, default);
        }
    }

    internal static HashSet<SelectionDLC> AllSafe => All.Keys.ToHashSet();

    public bool Equals(SelectionDLC other) => other is not null && (ReferenceEquals(this, other) || Id == other.Id && Type == other.Type);

    internal static SelectionDLC GetOrCreate(DLCType type, string id, string name) => FromTypeId(type, id) ?? new SelectionDLC(type, id, name);

    internal static SelectionDLC FromTypeId(DLCType Type, string dlcId) => AllSafe.FirstOrDefault(dlc => dlc.Type == Type && dlc.Id == dlcId);

    public override bool Equals(object obj) => ReferenceEquals(this, obj) || obj is SelectionDLC other && Equals(other);

    public override int GetHashCode() => HashCode.Combine(Id, (int)Type);
}