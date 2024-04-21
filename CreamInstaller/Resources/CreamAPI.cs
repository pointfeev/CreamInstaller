using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using CreamInstaller.Forms;
using static CreamInstaller.Resources.Resources;

namespace CreamInstaller.Resources;

internal static class CreamAPI
{
    internal static void GetCreamApiComponents(this string directory, out string api32, out string api32_o,
        out string api64, out string api64_o,
        out string config)
    {
        api32 = directory + @"\steam_api.dll";
        api32_o = directory + @"\steam_api_o.dll";
        api64 = directory + @"\steam_api64.dll";
        api64_o = directory + @"\steam_api64_o.dll";
        config = directory + @"\cream_api.ini";
        // TODO: account for log builds?
    }

    internal static void CheckConfig(string directory, Selection selection, InstallForm installForm = null)
    {
        // TODO
    }

    private static void WriteConfig(StreamWriter writer, string appId,
        SortedList<string, (string name, SortedList<string, SelectionDLC> injectDlc)> extraApps,
        SortedList<string, SelectionDLC> overrideDlc, SortedList<string, SelectionDLC> injectDlc,
        InstallForm installForm = null)
    {
        // TODO
    }

    internal static async Task Uninstall(string directory, InstallForm installForm = null, bool deleteOthers = true)
        => await Task.Run(() =>
        {
            // TODO
        });

    internal static async Task Install(string directory, Selection selection, InstallForm installForm = null,
        bool generateConfig = true)
        => await Task.Run(() =>
        {
            // TODO
        });

    // TODO: add all CreamAPI versions' MD5s
    internal static readonly Dictionary<ResourceIdentifier, HashSet<string>> ResourceMD5s = new()
    {
        [ResourceIdentifier.Steamworks32] =
        [
            "02594110FE56B2945955D46670B9A094" // CreamAPI v4.5.0.0 Hotfix
        ],
        [ResourceIdentifier.Steamworks64] =
        [
            "30091B91923D9583A54A93ED1145554B" // CreamAPI v4.5.0.0 Hotfix
        ]
    };
}