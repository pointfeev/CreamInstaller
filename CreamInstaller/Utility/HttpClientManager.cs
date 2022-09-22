using HtmlAgilityPack;

using System;
using System.Drawing;
using System.Net.Http;
using System.Threading.Tasks;

namespace CreamInstaller.Utility;

internal static class HttpClientManager
{
    internal static HttpClient HttpClient;
    internal static void Setup()
    {
        HttpClient = new();
        HttpClient.DefaultRequestHeaders.Add("User-Agent", $"CI{Program.Version.Replace(".", "")}");
    }

    internal static async Task<string> EnsureGet(string url)
    {
        try
        {
            using HttpRequestMessage request = new(HttpMethod.Get, url);
            using HttpResponseMessage response = await HttpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
            _ = response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsStringAsync();
        }
        catch
        {
            return null;
        }
    }

    internal static HtmlAgilityPack.HtmlDocument ToHtmlDocument(this string html)
    {
        HtmlAgilityPack.HtmlDocument document = new();
        document.LoadHtml(html);
        return document;
    }

    internal static async Task<HtmlNodeCollection> GetDocumentNodes(string url, string xpath) => (await EnsureGet(url))?.ToHtmlDocument()?.DocumentNode?.SelectNodes(xpath);

    internal static HtmlNodeCollection GetDocumentNodes(this HtmlAgilityPack.HtmlDocument htmlDocument, string xpath) => htmlDocument.DocumentNode?.SelectNodes(xpath);

    internal static async Task<Image> GetImageFromUrl(string url)
    {
        try
        {
            return new Bitmap(await HttpClient.GetStreamAsync(new Uri(url)));
        }
        catch
        {
            return null;
        }
    }

    internal static void Dispose() => HttpClient?.Dispose();
}
