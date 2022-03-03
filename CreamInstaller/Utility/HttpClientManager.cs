using System;
using System.Drawing;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

using HtmlAgilityPack;

namespace CreamInstaller.Utility;

internal static class HttpClientManager
{
    internal static HttpClient HttpClient;
    internal static void Setup()
    {
        HttpClient = new();
        HttpClient.DefaultRequestHeaders.Add("user-agent", $"CreamInstaller-{Environment.MachineName}_{Environment.UserDomainName}_{Environment.UserName}");
    }

    internal static async Task<HtmlDocument> Get(string url)
    {
        try
        {
            using HttpRequestMessage request = new(HttpMethod.Get, url);
            using HttpResponseMessage response = await HttpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
            response.EnsureSuccessStatusCode();
            using Stream stream = await response.Content.ReadAsStreamAsync();
            using StreamReader reader = new(stream, Encoding.UTF8);
            HtmlDocument document = new();
            document.LoadHtml(reader.ReadToEnd());
            return document;
        }
        catch { return null; }
    }

    internal static async Task<HtmlNodeCollection> GetDocumentNodes(string url, string xpath) => (await Get(url))?.DocumentNode?.SelectNodes(xpath);

    internal static async Task<Image> GetImageFromUrl(string url)
    {
        try
        {
            return new Bitmap(await HttpClient.GetStreamAsync(url));
        }
        catch { }
        return null;
    }

    internal static void Dispose() => HttpClient.Dispose();
}
