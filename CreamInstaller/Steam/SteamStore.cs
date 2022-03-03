using System.Collections.Generic;
using System.Threading.Tasks;

using CreamInstaller.Utility;

using HtmlAgilityPack;

namespace CreamInstaller.Steam;

internal static class SteamStore
{
    internal static async Task ParseDlcAppIds(string appId, List<string> dlcIds)
    {
        // currently this is only really needed to get DLC that release without changing game buildid (very rare)
        // it also finds things which aren't really connected to the game itself, and thus not needed (usually soundtracks, collections, packs, etc.)
        HtmlNodeCollection nodes = await HttpClientManager.GetDocumentNodes(
                        $"https://store.steampowered.com/dlc/{appId}",
                        "//div[@class='recommendation']/div/a");
        if (nodes is not null)
            foreach (HtmlNode node in nodes)
                if (int.TryParse(node.Attributes?["data-ds-appid"]?.Value, out int dlcAppId) && dlcAppId > 0 && !dlcIds.Contains("" + dlcAppId))
                    dlcIds.Add("" + dlcAppId);
    }
}
