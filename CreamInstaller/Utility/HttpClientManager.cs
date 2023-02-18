using System;
using System.Drawing;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using HtmlAgilityPack;
#if DEBUG
using CreamInstaller.Forms;
#endif

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
        catch (HttpRequestException e)
        {
            if (e.StatusCode != HttpStatusCode.TooManyRequests)
            {
#if DEBUG
                DebugForm.Current.Log("Get request failed to " + url + ": " + e, LogTextBox.Warning);
#endif
                return null;
            }
#if DEBUG
            DebugForm.Current.Log("Too many requests to " + url, LogTextBox.Error);
#endif
            // do something special?
            return null;
        }
#if DEBUG
        catch (Exception e)
        {
            DebugForm.Current.Log("Get request failed to " + url + ": " + e, LogTextBox.Warning);
            return null;
        }
#else
        catch
        {
            return null;
        }
#endif
    }

    private static HtmlDocument ToHtmlDocument(this string html)
    {
        HtmlDocument document = new();
        document.LoadHtml(html);
        return document;
    }

    internal static async Task<HtmlNodeCollection> GetDocumentNodes(string url, string xpath)
        => (await EnsureGet(url))?.ToHtmlDocument()?.GetDocumentNodes(xpath);

    private static HtmlNodeCollection GetDocumentNodes(this HtmlDocument htmlDocument, string xpath) => htmlDocument.DocumentNode?.SelectNodes(xpath);

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