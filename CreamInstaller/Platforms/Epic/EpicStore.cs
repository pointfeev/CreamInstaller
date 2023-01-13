using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;
using CreamInstaller.Platforms.Epic.GraphQL;
using CreamInstaller.Utility;
using Newtonsoft.Json;

namespace CreamInstaller.Platforms.Epic;

internal static class EpicStore
{
    //private const int CooldownCatalogItem = 600;

    /* need a method to query catalog items
    internal static async Task QueryCatalogItems(Manifest manifest)
    {
    }*/

    private const int CooldownEntitlement = 600;

    internal static async Task<List<(string id, string name, string product, string icon, string developer)>> QueryEntitlements(string categoryNamespace)
    {
        List<(string id, string name, string product, string icon, string developer)> dlcIds = new();
        string cacheFile = ProgramData.AppInfoPath + @$"\{categoryNamespace}.json";
        bool cachedExists = File.Exists(cacheFile);
        Response response = null;
        if (!cachedExists || ProgramData.CheckCooldown(categoryNamespace, CooldownEntitlement))
        {
            response = await QueryGraphQL(categoryNamespace);
            try
            {
                await File.WriteAllTextAsync(cacheFile, JsonConvert.SerializeObject(response, Formatting.Indented));
            }
            catch
            {
                // ignored
            }
        }
        else
            try
            {
                response = JsonConvert.DeserializeObject<Response>(await File.ReadAllTextAsync(cacheFile));
            }
            catch
            {
                File.Delete(cacheFile);
            }
        if (response is null)
            return dlcIds;
        List<Element> searchStore = new(response.Data.Catalog.SearchStore.Elements);
        foreach (Element element in searchStore)
        {
            string title = element.Title;
            string product = element.CatalogNs is not null && element.CatalogNs.Mappings.Any() ? element.CatalogNs.Mappings.First().PageSlug : null;
            string icon = null;
            for (int i = 0; i < element.KeyImages?.Length; i++)
            {
                KeyImage keyImage = element.KeyImages[i];
                if (keyImage.Type != "DieselStoreFront")
                    continue;
                icon = keyImage.Url.ToString();
                break;
            }
            foreach (Item item in element.Items)
                dlcIds.Populate(item.Id, title, product, icon, null, element.Items.Length == 1);
        }
        List<Element> catalogOffers = new(response.Data.Catalog.CatalogOffers.Elements);
        foreach (Element element in catalogOffers)
        {
            string title = element.Title;
            string product = element.CatalogNs is not null && element.CatalogNs.Mappings.Any() ? element.CatalogNs.Mappings.First().PageSlug : null;
            string icon = null;
            for (int i = 0; i < element.KeyImages?.Length; i++)
            {
                KeyImage keyImage = element.KeyImages[i];
                if (keyImage.Type != "Thumbnail")
                    continue;
                icon = keyImage.Url.ToString();
                break;
            }
            foreach (Item item in element.Items)
                dlcIds.Populate(item.Id, title, product, icon, item.Developer, element.Items.Length == 1);
        }
        return dlcIds;
    }

    private static void Populate(this List<(string id, string name, string product, string icon, string developer)> dlcIds, string id, string title,
        string product, string icon, string developer, bool canOverwrite = false)
    {
        if (id == null)
            return;
        bool found = false;
        for (int i = 0; i < dlcIds.Count; i++)
        {
            (string id, string name, string product, string icon, string developer) app = dlcIds[i];
            if (app.id != id)
                continue;
            found = true;
            dlcIds[i] = canOverwrite
                ? (app.id, title ?? app.name, product ?? app.product, icon ?? app.icon, developer ?? app.developer)
                : (app.id, app.name ?? title, app.product ?? product, app.icon ?? icon, app.developer ?? developer);
            break;
        }
        if (!found)
            dlcIds.Add((id, title, product, icon, developer));
    }

    private static async Task<Response> QueryGraphQL(string categoryNamespace)
    {
        try
        {
            string encoded = HttpUtility.UrlEncode(categoryNamespace);
            Request request = new(encoded);
            string payload = JsonConvert.SerializeObject(request);
            using HttpContent content = new StringContent(payload);
            content.Headers.ContentType = new("application/json");
            HttpClient client = HttpClientManager.HttpClient;
            if (client is null)
                return null;
            HttpResponseMessage httpResponse = await client.PostAsync(new Uri("https://graphql.epicgames.com/graphql"), content);
            _ = httpResponse.EnsureSuccessStatusCode();
            string response = await httpResponse.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<Response>(response);
        }
        catch
        {
            return null;
        }
    }
}