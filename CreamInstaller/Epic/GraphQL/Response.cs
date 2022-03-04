
using Newtonsoft.Json;

namespace CreamInstaller.Epic.GraphQL;

public class Response
{
    [JsonProperty(PropertyName = "data")]
    public Data Data { get; protected set; }
}

public class Data
{
    [JsonProperty(PropertyName = "Catalog")]
    public Catalog Catalog { get; protected set; }
}

public class Catalog
{
    [JsonProperty(PropertyName = "catalogOffers")]
    public CatalogOffers CatalogOffers { get; protected set; }
}

public class CatalogOffers
{
    [JsonProperty(PropertyName = "elements")]
    public Element[] Elements { get; protected set; }
}

public class Element
{
    [JsonProperty(PropertyName = "title")]
    public string Title { get; protected set; }

    [JsonProperty(PropertyName = "keyImages")]
    public KeyImage[] KeyImages { get; protected set; }

    [JsonProperty(PropertyName = "items")]
    public Item[] Items { get; protected set; }

    [JsonProperty(PropertyName = "catalogNs")]
    public CatalogNs CatalogNs { get; protected set; }
}

public class Item
{
    [JsonProperty(PropertyName = "id")]
    public string Id { get; protected set; }

    [JsonProperty(PropertyName = "developer")]
    public string Developer { get; protected set; }
}

public class KeyImage
{
    [JsonProperty(PropertyName = "type")]
    public string Type { get; protected set; }

    [JsonProperty(PropertyName = "url")]
    public string Url { get; protected set; }
}

public class CatalogNs
{
    [JsonProperty(PropertyName = "mappings")]
    public Mapping[] Mappings { get; protected set; }
}

public class Mapping
{
    [JsonProperty(PropertyName = "pageSlug")]
    public string PageSlug { get; protected set; }
}