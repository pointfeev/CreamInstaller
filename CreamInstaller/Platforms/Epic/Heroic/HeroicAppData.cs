using Newtonsoft.Json;

namespace CreamInstaller.Platforms.Epic.Heroic;

public class HeroicInstall
{
    [JsonProperty("install_path")] public string InstallPath { get; set; }
}

public class HeroicAppData
{
    [JsonProperty("install")] public HeroicInstall Install { get; set; }

    [JsonProperty("namespace")] public string Namespace { get; set; }

    [JsonProperty("title")] public string Title { get; set; }
}