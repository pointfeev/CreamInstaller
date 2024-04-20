using System;
using System.Collections.Concurrent;
using System.Drawing;
using System.Globalization;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
#if DEBUG
using CreamInstaller.Forms;
#endif

namespace CreamInstaller.Utility;

internal static class HttpClientManager
{
    internal static HttpClient HttpClient;

    private static readonly ConcurrentDictionary<string, string> HttpContentCache = new();

    internal static void Setup()
    {
        HttpClient = new();
        HttpClient.DefaultRequestHeaders.UserAgent.Add(new(Program.Name, Program.Version));
        HttpClient.DefaultRequestHeaders.AcceptLanguage.Add(new(CultureInfo.CurrentCulture.ToString()));
    }

    internal static async Task<string> EnsureGet(string url)
    {
        try
        {
            using HttpRequestMessage request = new(HttpMethod.Get, url);
            using HttpResponseMessage response =
                await HttpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
            if (response.StatusCode is HttpStatusCode.NotModified &&
                HttpContentCache.TryGetValue(url, out string content))
                return content;
            _ = response.EnsureSuccessStatusCode();
            content = await response.Content.ReadAsStringAsync();
            HttpContentCache[url] = content;
            return content;
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