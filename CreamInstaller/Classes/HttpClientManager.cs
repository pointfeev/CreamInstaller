using System;
using System.Drawing;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

using HtmlAgilityPack;

namespace CreamInstaller.Classes;

internal static class HttpClientManager
{
    private static HttpClient httpClient;
    internal static void Setup()
    {
        httpClient = new();
        httpClient.DefaultRequestHeaders.Add("user-agent", "CreamInstaller");
    }

    internal static async Task<HtmlNodeCollection> GetDocumentNodes(string url, string xpath)
    {
        try
        {
            using HttpRequestMessage request = new(HttpMethod.Get, url);
            using HttpResponseMessage response = await httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
            response.EnsureSuccessStatusCode();
            using Stream stream = await response.Content.ReadAsStreamAsync();
            using StreamReader reader = new(stream, Encoding.UTF8);
            HtmlDocument document = new();
            document.LoadHtml(reader.ReadToEnd());
            return document.DocumentNode.SelectNodes(xpath);
        }
        catch { return null; }
    }

    internal static async Task<Image> GetImageFromUrl(string url)
    {
        try
        {
            return new Bitmap(await httpClient.GetStreamAsync(url));
        }
        catch { }
        return null;
    }

    internal static void Cleanup() => httpClient.Dispose();
}
