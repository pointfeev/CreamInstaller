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

internal static class UplayR1
{
    internal static void GetUplayR1Components(this string directory, out string api32, out string api32_o,
        out string api64, out string api64_o,
        out string config, out string log)
    {
        api32 = directory + @"\uplay_r1_loader.dll";
        api32_o = directory + @"\uplay_r1_loader_o.dll";
        api64 = directory + @"\uplay_r1_loader64.dll";
        api64_o = directory + @"\uplay_r1_loader64_o.dll";
        config = directory + @"\UplayR1Unlocker.jsonc";
        log = directory + @"\UplayR1Unlocker.log";
    }

    internal static void CheckConfig(string directory, Selection selection, InstallForm installForm = null)
    {
        directory.GetUplayR1Components(out _, out _, out _, out _, out string config, out _);
        HashSet<SelectionDLC> blacklistDlc = selection.DLC.Where(dlc => !dlc.Enabled).ToHashSet();
        foreach (SelectionDLC extraDlc in selection.ExtraSelections.SelectMany(extraSelection =>
                     extraSelection.DLC.Where(dlc => !dlc.Enabled)))
            _ = blacklistDlc.Add(extraDlc);
        if (blacklistDlc.Count > 0)
        {
            /*if (installForm is not null)
                installForm.UpdateUser("Generating Uplay R1 Unlocker configuration for " + selection.Name + $" in directory \"{directory}\" . . . ", LogTextBox.Operation);*/
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
        writer.WriteLine("  \"hook_loader\": false,");
        if (blacklistDlc.Count > 0)
        {
            writer.WriteLine("  \"blacklist\": [");
            KeyValuePair<string, SelectionDLC> lastBlacklistDlc = blacklistDlc.Last();
            foreach (KeyValuePair<string, SelectionDLC> pair in blacklistDlc)
            {
                SelectionDLC selectionDlc = pair.Value;
                writer.WriteLine($"    {selectionDlc.Id}{(pair.Equals(lastBlacklistDlc) ? "" : ",")}");
                installForm?.UpdateUser(
                    $"Added blacklist DLC to UplayR1Unlocker.jsonc with appid {selectionDlc.Id} ({selectionDlc.Name})",
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
            directory.GetUplayR1Components(out string api32, out string api32_o, out string api64, out string api64_o,
                out string config, out string log);
            if (api32_o.FileExists())
            {
                if (api32.FileExists())
                {
                    api32.DeleteFile(true);
                    installForm?.UpdateUser($"Deleted Uplay R1 Unlocker: {Path.GetFileName(api32)}", LogTextBox.Action,
                        false);
                }

                api32_o.MoveFile(api32!);
                installForm?.UpdateUser($"Restored Uplay R1: {Path.GetFileName(api32_o)} -> {Path.GetFileName(api32)}",
                    LogTextBox.Action, false);
            }

            if (api64_o.FileExists())
            {
                if (api64.FileExists())
                {
                    api64.DeleteFile(true);
                    installForm?.UpdateUser($"Deleted Uplay R1 Unlocker: {Path.GetFileName(api64)}", LogTextBox.Action,
                        false);
                }

                api64_o.MoveFile(api64!);
                installForm?.UpdateUser($"Restored Uplay R1: {Path.GetFileName(api64_o)} -> {Path.GetFileName(api64)}",
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
            directory.GetUplayR1Components(out string api32, out string api32_o, out string api64, out string api64_o,
                out _, out _);
            if (api32.FileExists() && !api32_o.FileExists())
            {
                api32.MoveFile(api32_o!, true);
                installForm?.UpdateUser($"Renamed Uplay R1: {Path.GetFileName(api32)} -> {Path.GetFileName(api32_o)}",
                    LogTextBox.Action, false);
            }

            if (api32_o.FileExists())
            {
                "UplayR1.uplay_r1_loader.dll".WriteManifestResource(api32);
                installForm?.UpdateUser($"Wrote Uplay R1 Unlocker: {Path.GetFileName(api32)}", LogTextBox.Action,
                    false);
            }

            if (api64.FileExists() && !api64_o.FileExists())
            {
                api64.MoveFile(api64_o!, true);
                installForm?.UpdateUser($"Renamed Uplay R1: {Path.GetFileName(api64)} -> {Path.GetFileName(api64_o)}",
                    LogTextBox.Action, false);
            }

            if (api64_o.FileExists())
            {
                "UplayR1.uplay_r1_loader64.dll".WriteManifestResource(api64);
                installForm?.UpdateUser($"Wrote Uplay R1 Unlocker: {Path.GetFileName(api64)}", LogTextBox.Action,
                    false);
            }

            if (generateConfig)
                CheckConfig(directory, selection, installForm);
        });

    internal static readonly Dictionary<ResourceIdentifier, HashSet<string>> ResourceMD5s = new()
    {
        [ResourceIdentifier.Uplay32] =
        [
            "1977967B2549A38EC2DB39D4C8ED499B" // Uplay R1 Unlocker v2.0.0
        ],
        [ResourceIdentifier.Uplay64] =
        [
            "333FEDD9DC2B299419B37ED1624FF8DB" // Uplay R1 Unlocker v2.0.0
        ]
    };
}