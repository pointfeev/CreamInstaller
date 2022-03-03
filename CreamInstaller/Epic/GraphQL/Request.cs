
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
        catalogOffers(
            namespace: $namespace
            params: {
                count: 1000,
            }
        ) {
            elements {
                id
                title
                offerType
                items {
                    id
                }
                keyImages {
                    type
                    url
                }
                catalogNs {
                    mappings(pageType: ""productHome"") {
                        pageSlug
                    }
                }
            }
        }
        searchStore(category: ""games/edition/base"", namespace: $namespace) {
            elements {
                id
                title
                offerType
                items {
                    id
                }
                keyImages {
                    type
                    url
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
        [JsonProperty(PropertyName = "category")]
        private string _category => "games/edition/base|bundles/games|editors|software/edition/base";

        [JsonProperty(PropertyName = "count")]
        private int _count => 1000;

        [JsonProperty(PropertyName = "keywords")]
        private string _keywords => "";

        [JsonProperty(PropertyName = "namespace")]
        private string _namespace { get; set; }

        internal Variables(string _namespace) => this._namespace = _namespace;
    }

#pragma warning restore IDE0051 // Remove unused private members
#pragma warning restore CA1822 // Mark members as static
#pragma warning restore IDE0052 // Remove unread private members
#pragma warning restore IDE1006 // Naming Styles
}
