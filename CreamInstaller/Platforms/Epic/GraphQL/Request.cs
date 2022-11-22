#pragma warning disable IDE0051 // Remove unused private members
#pragma warning disable IDE0052 // Remove unread private members
#pragma warning disable CA1812 // Avoid uninstantiated internal classes
#pragma warning disable CA1822 // Mark members as static

using Newtonsoft.Json;

namespace CreamInstaller.Platforms.Epic.GraphQL;

internal class Request
{
    [JsonProperty(PropertyName = "query")]
    private string Query => @"query searchOffers($namespace: String!) {
    Catalog {
        searchStore(category: ""*"", namespace: $namespace){
            elements {
                id
                title
                developer
                items {
                    id
                }
                catalogNs {
                    mappings(pageType: ""productHome"") {
                        pageSlug
                    }
                }
            }
        }
        catalogOffers(
            namespace: $namespace
            params: {
                count: 1000,
            }
        ) {
            elements {
                id
                title
                keyImages {
                    type
                    url
                }
                items {
                    id
                    title
                    developer
                }
                catalogNs {
                    mappings(pageType: ""productHome"") {
                        pageSlug
                    }
                }
            }
        }
    }
}";

    [JsonProperty(PropertyName = "variables")]
    private Variables Vars { get; set; }

    internal Request(string @namespace) => Vars = new Variables(@namespace);

    private class Headers
    {
        [JsonProperty(PropertyName = "Content-Type")]
        private string ContentType => "application/graphql";

    }

    private class Variables
    {
        [JsonProperty(PropertyName = "namespace")]
        private string Namespace { get; set; }

        internal Variables(string @namespace) => Namespace = @namespace;
    }
}