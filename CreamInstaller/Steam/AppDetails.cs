#pragma warning disable IDE1006 // Naming Styles
#pragma warning disable CA1002 // Do not expose generic lists
#pragma warning disable CA1707 // Identifiers should not contain underscores
#pragma warning disable CA2227 // Collection properties should be read only

using System.Collections.Generic;

namespace CreamInstaller.Steam;

public class Category
{
    public int id { get; set; }

    public string description { get; set; }
}

public class Genre
{
    public string id { get; set; }

    public string description { get; set; }
}

public class AppData
{
    public string type { get; set; }

    public string name { get; set; }

    public int steam_appid { get; set; }

    public List<int> dlc { get; set; }

    public string header_image { get; set; }

    public string website { get; set; }

    public List<string> developers { get; set; }

    public List<string> publishers { get; set; }

    public List<int> packages { get; set; }

    public List<Category> categories { get; set; }

    public List<Genre> genres { get; set; }
}

public class AppDetails
{
    public bool success { get; set; }

    public AppData data { get; set; }
}