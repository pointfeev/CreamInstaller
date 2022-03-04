
using Newtonsoft.Json;

namespace CreamInstaller.Epic.GraphQL;

internal class Request
{
#pragma warning disable IDE0051 // Remove unused private members
#pragma warning disable CA1822 // Mark members as static
#pragma warning disable IDE0052 // Remove unread private members
#pragma warning disable IDE1006 // Naming Styles

    [JsonProperty(PropertyName = "query")]
    private string _gqlQuery => @"query searchOffers($namespace: String!) {
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
    private Variables _variables { get; set; }

    internal Request(string _namespace) => _variables = new Variables(_namespace);

    private class Headers
    {
        [JsonProperty(PropertyName = "Content-Type")]
        private string _contentType => "application/graphql";
    }

    private class Variables
    {
        [JsonProperty(PropertyName = "namespace")]
        private string _namespace { get; set; }

        internal Variables(string _namespace) => this._namespace = _namespace;
    }

#pragma warning restore IDE0051 // Remove unused private members
#pragma warning restore CA1822 // Mark members as static
#pragma warning restore IDE0052 // Remove unread private members
#pragma warning restore IDE1006 // Naming Styles
}
