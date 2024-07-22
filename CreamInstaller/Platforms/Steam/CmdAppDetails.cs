using System.Collections.Generic;
using Newtonsoft.Json;

namespace CreamInstaller.Platforms.Steam;

public class CmdAppCommon
{
    [JsonProperty(PropertyName = "type")] public string Type { get; set; }

    [JsonProperty(PropertyName = "name")] public string Name { get; set; }

    [JsonProperty(PropertyName = "icon")] public string Icon { get; set; }

    [JsonProperty(PropertyName = "clienticon")]
    public string ClientIcon { get; set; }

    [JsonProperty(PropertyName = "logo_small")]
    public string LogoSmall { get; set; }

    [JsonProperty(PropertyName = "logo")] public string Logo { set; get; }

    [JsonProperty(PropertyName = "parent")]
    public string Parent { set; get; }
}

public class CmdAppExtended
{
    [JsonProperty(PropertyName = "listofdlc")]
    public string Dlc { get; set; }

    [JsonProperty(PropertyName = "publisher")]
    public string Publisher { get; set; }
}

public class CmdAppData
{
    [JsonProperty(PropertyName = "common")]
    public CmdAppCommon Common { get; set; }

    [JsonProperty(PropertyName = "depots")]
    public Dictionary<string, dynamic> Depots { get; set; }

    [JsonProperty(PropertyName = "extended")]
    public CmdAppExtended Extended { get; set; }
}

public class CmdAppDetails
{
    [JsonProperty(PropertyName = "status")]
    public string Status { get; set; }

    [JsonProperty(PropertyName = "data")] public Dictionary<string, CmdAppData> Data { get; set; }
}