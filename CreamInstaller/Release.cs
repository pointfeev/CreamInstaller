using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace CreamInstaller;

public class Release
{
    private string[] changes;

    private Version version;

    [JsonProperty("tag_name", NullValueHandling = NullValueHandling.Ignore)]
    public string TagName { get; set; }

    [JsonProperty("name", NullValueHandling = NullValueHandling.Ignore)]
    public string Name { get; set; }

    [JsonProperty("draft", NullValueHandling = NullValueHandling.Ignore)]
    public bool Draft { get; set; }

    [JsonProperty("prerelease", NullValueHandling = NullValueHandling.Ignore)]
    public bool Prerelease { get; set; }

    [JsonProperty("assets", NullValueHandling = NullValueHandling.Ignore)]
    public List<Asset> Assets { get; } = new();

    [JsonProperty("body", NullValueHandling = NullValueHandling.Ignore)]
    public string Body { get; set; }

    public Version Version => version ??= new(TagName[1..]);

    public string[] Changes => changes ??= Body.Replace("- ", "").Split("\r\n");
}

public class Asset
{
    [JsonProperty("name", NullValueHandling = NullValueHandling.Ignore)]
    public string Name { get; set; }

    [JsonProperty("content_type", NullValueHandling = NullValueHandling.Ignore)]
    public string ContentType { get; set; }

    [JsonProperty("state", NullValueHandling = NullValueHandling.Ignore)]
    public string State { get; set; }

    [JsonProperty("size", NullValueHandling = NullValueHandling.Ignore)]
    public int Size { get; set; }

    [JsonProperty("browser_download_url", NullValueHandling = NullValueHandling.Ignore)]
    public string BrowserDownloadUrl { get; set; }
}