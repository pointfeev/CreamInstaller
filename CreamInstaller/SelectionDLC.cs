using System.Collections.Generic;
using System.Linq;

namespace CreamInstaller;

public enum DLCType
{
    Steam, SteamHidden, EpicCatalogItem,
    EpicEntitlement
}

internal sealed class SelectionDLC
{
    private static readonly HashSet<SelectionDLC> All = new();

    internal bool Enabled;
    internal string Icon;
    internal string Id;
    internal string Name;
    internal string Product;
    internal string Publisher;
    private Selection selection;
    internal DLCType Type;

    internal Selection Selection
    {
        get => selection;
        set
        {
            selection = value;
            _ = value is null ? All.Remove(this) : All.Add(this);
        }
    }

    internal static List<SelectionDLC> AllSafe => All.ToList();

    internal static SelectionDLC FromPlatformId(Platform platform, string dlcId) => AllSafe.Find(dlc => dlc.Selection.Platform == platform && dlc.Id == dlcId);
}