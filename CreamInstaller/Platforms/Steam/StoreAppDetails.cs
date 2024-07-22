using System.Collections.Generic;
using Newtonsoft.Json;

namespace CreamInstaller.Platforms.Steam;

public class StoreAppFullGame
{
    [JsonProperty(PropertyName = "appid")] public string AppId { get; set; }

    [JsonProperty(PropertyName = "name")] public string Name { get; set; }
}

public class StoreAppData
{
    [JsonProperty(PropertyName = "type")] public string Type { get; set; }

    [JsonProperty(PropertyName = "name")] public string Name { get; set; }

    [JsonProperty(PropertyName = "steam_appid")]
    public int SteamAppId { get; set; }

    [JsonProperty(PropertyName = "fullgame")]
    public StoreAppFullGame FullGame { get; set; }

    [JsonProperty(PropertyName = "dlc")] public List<int> DLC { get; set; }

    [JsonProperty(PropertyName = "header_image")]
    public string HeaderImage { get; set; }

    [JsonProperty(PropertyName = "website")]
    public string Website { get; set; }

    [JsonProperty(PropertyName = "developers")]
    public List<string> Developers { get; set; }

    [JsonProperty(PropertyName = "publishers")]
    public List<string> Publishers { get; set; }

    [JsonProperty(PropertyName = "packages")]
    public List<int> Packages { get; set; }
}

public class StoreAppDetails
{
    [JsonProperty(PropertyName = "success")]
    public bool Success { get; set; }

    [JsonProperty(PropertyName = "data")] public StoreAppData Data { get; set; }
}