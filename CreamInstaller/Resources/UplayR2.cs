using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CreamInstaller.Components;
using CreamInstaller.Forms;
using CreamInstaller.Utility;

namespace CreamInstaller.Resources;

internal static class UplayR2
{
    internal static void GetUplayR2Components(this string directory, out string old_api32, out string old_api64, out string api32, out string api32_o,
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

    internal static void CheckConfig(string directory, ProgramSelection selection, InstallForm installForm = null)
    {
        directory.GetUplayR2Components(out _, out _, out _, out _, out _, out _, out string config, out _);
        IEnumerable<KeyValuePair<string, (DlcType type, string name, string icon)>> blacklistDlc = selection.AllDlc.Except(selection.SelectedDlc);
        foreach (KeyValuePair<string, (string _, SortedList<string, (DlcType type, string name, string icon)> extraDlc)> pair in selection.ExtraSelectedDlc)
            blacklistDlc = blacklistDlc.Except(pair.Value.extraDlc);
        blacklistDlc = blacklistDlc.ToList();
        if (blacklistDlc.Any())
        {
            /*if (installForm is not null)
                installForm.UpdateUser("Generating Uplay R2 Unlocker configuration for " + selection.Name + $" in directory \"{directory}\" . . . ", LogTextBox.Operation);*/
            File.Create(config).Close();
            StreamWriter writer = new(config, true, Encoding.UTF8);
            WriteConfig(writer, new(blacklistDlc.ToDictionary(pair => pair.Key, pair => pair.Value), PlatformIdComparer.String), installForm);
            writer.Flush();
            writer.Close();
        }
        else if (File.Exists(config))
        {
            File.Delete(config);
            installForm?.UpdateUser($"Deleted unnecessary configuration: {Path.GetFileName(config)}", LogTextBox.Action, false);
        }
    }

    private static void WriteConfig(StreamWriter writer, SortedList<string, (DlcType type, string name, string icon)> blacklistDlc,
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
            KeyValuePair<string, (DlcType type, string name, string icon)> lastBlacklistDlc = blacklistDlc.Last();
            foreach (KeyValuePair<string, (DlcType type, string name, string icon)> pair in blacklistDlc)
            {
                string dlcId = pair.Key;
                (_, string dlcName, _) = pair.Value;
                writer.WriteLine($"    {dlcId}{(pair.Equals(lastBlacklistDlc) ? "" : ",")}");
                installForm?.UpdateUser($"Added blacklist DLC to UplayR2Unlocker.jsonc with appid {dlcId} ({dlcName})", LogTextBox.Action, false);
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
            directory.GetUplayR2Components(out string old_api32, out string old_api64, out string api32, out string api32_o, out string api64,
                out string api64_o, out string config, out string log);
            if (File.Exists(api32_o))
            {
                string api = File.Exists(old_api32) ? old_api32 : api32;
                if (File.Exists(api))
                {
                    File.Delete(api);
                    installForm?.UpdateUser($"Deleted Uplay R2 Unlocker: {Path.GetFileName(api)}", LogTextBox.Action, false);
                }
                File.Move(api32_o, api!);
                installForm?.UpdateUser($"Restored Uplay R2: {Path.GetFileName(api32_o)} -> {Path.GetFileName(api)}", LogTextBox.Action, false);
            }
            if (File.Exists(api64_o))
            {
                string api = File.Exists(old_api64) ? old_api64 : api64;
                if (File.Exists(api))
                {
                    File.Delete(api);
                    installForm?.UpdateUser($"Deleted Uplay R2 Unlocker: {Path.GetFileName(api)}", LogTextBox.Action, false);
                }
                File.Move(api64_o, api!);
                installForm?.UpdateUser($"Restored Uplay R2: {Path.GetFileName(api64_o)} -> {Path.GetFileName(api)}", LogTextBox.Action, false);
            }
            if (!deleteOthers)
                return;
            if (File.Exists(config))
            {
                File.Delete(config);
                installForm?.UpdateUser($"Deleted configuration: {Path.GetFileName(config)}", LogTextBox.Action, false);
            }
            if (File.Exists(log))
            {
                File.Delete(log);
                installForm?.UpdateUser($"Deleted log: {Path.GetFileName(log)}", LogTextBox.Action, false);
            }
        });

    internal static async Task Install(string directory, ProgramSelection selection, InstallForm installForm = null, bool generateConfig = true)
        => await Task.Run(() =>
        {
            directory.GetUplayR2Components(out string old_api32, out string old_api64, out string api32, out string api32_o, out string api64,
                out string api64_o, out _, out _);
            string api = File.Exists(old_api32) ? old_api32 : api32;
            if (File.Exists(api) && !File.Exists(api32_o))
            {
                File.Move(api, api32_o!);
                installForm?.UpdateUser($"Renamed Uplay R2: {Path.GetFileName(api)} -> {Path.GetFileName(api32_o)}", LogTextBox.Action, false);
            }
            if (File.Exists(api32_o))
            {
                "UplayR2.upc_r2_loader.dll".Write(api);
                installForm?.UpdateUser($"Wrote Uplay R2 Unlocker: {Path.GetFileName(api)}", LogTextBox.Action, false);
            }
            api = File.Exists(old_api64) ? old_api64 : api64;
            if (File.Exists(api) && !File.Exists(api64_o))
            {
                File.Move(api, api64_o!);
                installForm?.UpdateUser($"Renamed Uplay R2: {Path.GetFileName(api)} -> {Path.GetFileName(api64_o)}", LogTextBox.Action, false);
            }
            if (File.Exists(api64_o))
            {
                "UplayR2.upc_r2_loader64.dll".Write(api);
                installForm?.UpdateUser($"Wrote Uplay R2 Unlocker: {Path.GetFileName(api)}", LogTextBox.Action, false);
            }
            if (generateConfig)
                CheckConfig(directory, selection, installForm);
        });
}