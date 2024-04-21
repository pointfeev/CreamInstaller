using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CreamInstaller.Components;
using CreamInstaller.Forms;
using CreamInstaller.Utility;
using static CreamInstaller.Resources.Resources;

namespace CreamInstaller.Resources;

internal static class UplayR2
{
    internal static void GetUplayR2Components(this string directory, out string old_api32, out string old_api64,
        out string api32, out string api32_o,
        out string api64, out string api64_o, out string config, out string log)
    {
        old_api32 = directory + @"\uplay_r2_loader.dll";
        old_api64 = directory + @"\uplay_r2_loader64.dll";
        api32 = directory + @"\upc_r2_loader.dll";
        api32_o = directory + @"\upc_r2_loader_o.dll";
        api64 = directory + @"\upc_r2_loader64.dll";
        api64_o = directory + @"\upc_r2_loader64_o.dll";
        config = directory + @"\UplayR2Unlocker.jsonc";
        log = directory + @"\UplayR2Unlocker.log";
    }

    internal static void CheckConfig(string directory, Selection selection, InstallForm installForm = null)
    {
        directory.GetUplayR2Components(out _, out _, out _, out _, out _, out _, out string config, out _);
        HashSet<SelectionDLC> blacklistDlc = selection.DLC.Where(dlc => !dlc.Enabled).ToHashSet();
        foreach (SelectionDLC extraDlc in selection.ExtraSelections.SelectMany(extraSelection =>
                     extraSelection.DLC.Where(dlc => !dlc.Enabled)))
            _ = blacklistDlc.Add(extraDlc);
        if (blacklistDlc.Count > 0)
        {
            /*if (installForm is not null)
                installForm.UpdateUser("Generating Uplay R2 Unlocker configuration for " + selection.Name + $" in directory \"{directory}\" . . . ", LogTextBox.Operation);*/
            config.CreateFile(true, installForm)?.Close();
            StreamWriter writer = new(config, true, Encoding.UTF8);
            WriteConfig(writer, new(blacklistDlc.ToDictionary(dlc => dlc.Id, dlc => dlc), PlatformIdComparer.String),
                installForm);
            writer.Flush();
            writer.Close();
        }
        else if (config.FileExists())
        {
            config.DeleteFile();
            installForm?.UpdateUser($"Deleted unnecessary configuration: {Path.GetFileName(config)}", LogTextBox.Action,
                false);
        }
    }

    private static void WriteConfig(StreamWriter writer, SortedList<string, SelectionDLC> blacklistDlc,
        InstallForm installForm = null)
    {
        writer.WriteLine("{");
        writer.WriteLine("  \"logging\": false,");
        writer.WriteLine("  \"lang\": \"default\",");
        writer.WriteLine("  \"auto_fetch\": true,");
        writer.WriteLine("  \"dlcs\": [],");
        writer.WriteLine("  \"items\": [],");
        if (blacklistDlc.Count > 0)
        {
            writer.WriteLine("  \"blacklist\": [");
            KeyValuePair<string, SelectionDLC> lastBlacklistDlc = blacklistDlc.Last();
            foreach (KeyValuePair<string, SelectionDLC> pair in blacklistDlc)
            {
                SelectionDLC selectionDlc = pair.Value;
                writer.WriteLine($"    {selectionDlc.Id}{(pair.Equals(lastBlacklistDlc) ? "" : ",")}");
                installForm?.UpdateUser(
                    $"Added blacklist DLC to UplayR2Unlocker.jsonc with appid {selectionDlc.Id} ({selectionDlc.Name})",
                    LogTextBox.Action,
                    false);
            }

            writer.WriteLine("  ],");
        }
        else
            writer.WriteLine("  \"blacklist\": [],");

        writer.WriteLine("}");
    }

    internal static async Task Uninstall(string directory, InstallForm installForm = null, bool deleteOthers = true)
        => await Task.Run(() =>
        {
            directory.GetUplayR2Components(out string old_api32, out string old_api64, out string api32,
                out string api32_o, out string api64,
                out string api64_o, out string config, out string log);
            if (api32_o.FileExists())
            {
                string api = old_api32.FileExists() ? old_api32 : api32;
                if (api.FileExists())
                {
                    api.DeleteFile(true);
                    installForm?.UpdateUser($"Deleted Uplay R2 Unlocker: {Path.GetFileName(api)}", LogTextBox.Action,
                        false);
                }

                api32_o.MoveFile(api!);
                installForm?.UpdateUser($"Restored Uplay R2: {Path.GetFileName(api32_o)} -> {Path.GetFileName(api)}",
                    LogTextBox.Action, false);
            }

            if (api64_o.FileExists())
            {
                string api = old_api64.FileExists() ? old_api64 : api64;
                if (api.FileExists())
                {
                    api.DeleteFile(true);
                    installForm?.UpdateUser($"Deleted Uplay R2 Unlocker: {Path.GetFileName(api)}", LogTextBox.Action,
                        false);
                }

                api64_o.MoveFile(api!);
                installForm?.UpdateUser($"Restored Uplay R2: {Path.GetFileName(api64_o)} -> {Path.GetFileName(api)}",
                    LogTextBox.Action, false);
            }

            if (!deleteOthers)
                return;
            if (config.FileExists())
            {
                config.DeleteFile();
                installForm?.UpdateUser($"Deleted configuration: {Path.GetFileName(config)}", LogTextBox.Action, false);
            }

            if (!log.FileExists())
                return;
            log.DeleteFile();
            installForm?.UpdateUser($"Deleted log: {Path.GetFileName(log)}", LogTextBox.Action, false);
        });

    internal static async Task Install(string directory, Selection selection, InstallForm installForm = null,
        bool generateConfig = true)
        => await Task.Run(() =>
        {
            directory.GetUplayR2Components(out string old_api32, out string old_api64, out string api32,
                out string api32_o, out string api64,
                out string api64_o, out _, out _);
            string api = old_api32.FileExists() ? old_api32 : api32;
            if (api.FileExists() && !api32_o.FileExists())
            {
                api.MoveFile(api32_o!, true);
                installForm?.UpdateUser($"Renamed Uplay R2: {Path.GetFileName(api)} -> {Path.GetFileName(api32_o)}",
                    LogTextBox.Action, false);
            }

            if (api32_o.FileExists())
            {
                "UplayR2.upc_r2_loader.dll".WriteManifestResource(api);
                installForm?.UpdateUser($"Wrote Uplay R2 Unlocker: {Path.GetFileName(api)}", LogTextBox.Action, false);
            }

            api = old_api64.FileExists() ? old_api64 : api64;
            if (api.FileExists() && !api64_o.FileExists())
            {
                api.MoveFile(api64_o!, true);
                installForm?.UpdateUser($"Renamed Uplay R2: {Path.GetFileName(api)} -> {Path.GetFileName(api64_o)}",
                    LogTextBox.Action, false);
            }

            if (api64_o.FileExists())
            {
                "UplayR2.upc_r2_loader64.dll".WriteManifestResource(api);
                installForm?.UpdateUser($"Wrote Uplay R2 Unlocker: {Path.GetFileName(api)}", LogTextBox.Action, false);
            }

            if (generateConfig)
                CheckConfig(directory, selection, installForm);
        });

    internal static readonly Dictionary<ResourceIdentifier, HashSet<string>> ResourceMD5s = new()
    {
        [ResourceIdentifier.Upc32] =
        [
            "C14368BC4EE19FDE8DBAC07E31C67AE4", // Uplay R2 Unlocker v3.0.0
            "DED3A3EA1876E3110D7D87B9A22946B0" // Uplay R2 Unlocker v3.0.1
        ],
        [ResourceIdentifier.Upc64] =
        [
            "7D9A4C12972BAABCB6C181920CC0F19B", // Uplay R2 Unlocker v3.0.0
            "D7FDBFE0FC8D7600FEB8EC0A97713184" // Uplay R2 Unlocker v3.0.1
        ]
    };
}