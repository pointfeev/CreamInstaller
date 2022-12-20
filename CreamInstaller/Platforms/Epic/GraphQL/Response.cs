#pragma warning disable CA1819 // Properties should not return arrays

using System;
using Newtonsoft.Json;

namespace CreamInstaller.Platforms.Epic.GraphQL;

public class Response
{
    [JsonProperty(PropertyName = "data")] public ResponseData Data { get; protected set; }
}

public class ResponseData
{
    [JsonProperty(PropertyName = "Catalog")]
    public Catalog Catalog { get; protected set; }
}

public class Catalog
{
    [JsonProperty(PropertyName = "searchStore")]
    public ElementContainer SearchStore { get; protected set; }

    [JsonProperty(PropertyName = "catalogOffers")]
    public ElementContainer CatalogOffers { get; protected set; }
}

public class ElementContainer
{
    [JsonProperty(PropertyName = "elements")]
    public Element[] Elements { get; protected set; }
}

public class Element
{
    [JsonProperty(PropertyName = "id")] public string Id { get; protected set; }

    [JsonProperty(PropertyName = "title")] public string Title { get; protected set; }

    [JsonProperty(PropertyName = "keyImages")]
    public KeyImage[] KeyImages { get; protected set; }

    [JsonProperty(PropertyName = "items")] public Item[] Items { get; protected set; }

    [JsonProperty(PropertyName = "catalogNs")]
    public CatalogNs CatalogNs { get; protected set; }
}

public class Item
{
    [JsonProperty(PropertyName = "id")] public string Id { get; protected set; }

    [JsonProperty(PropertyName = "title")] public string Title { get; protected set; }

    [JsonProperty(PropertyName = "developer")]
    public string Developer { get; protected set; }
}

public class KeyImage
{
    [JsonProperty(PropertyName = "type")] public string Type { get; protected set; }

    [JsonProperty(PropertyName = "url")] public Uri Url { get; protected set; }
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