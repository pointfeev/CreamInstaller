using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CreamInstaller.Components;
using CreamInstaller.Forms;
using CreamInstaller.Utility;

namespace CreamInstaller.Resources;

internal static class UplayR1
{
    internal static void GetUplayR1Components(this string directory, out string api32, out string api32_o, out string api64, out string api64_o,
        out string config, out string log)
    {
        api32 = directory + @"\uplay_r1_loader.dll";
        api32_o = directory + @"\uplay_r1_loader_o.dll";
        api64 = directory + @"\uplay_r1_loader64.dll";
        api64_o = directory + @"\uplay_r1_loader64_o.dll";
        config = directory + @"\UplayR1Unlocker.jsonc";
        log = directory + @"\UplayR1Unlocker.log";
    }

    internal static void CheckConfig(string directory, ProgramSelection selection, InstallForm installForm = null)
    {
        directory.GetUplayR1Components(out _, out _, out _, out _, out string config, out _);
        IEnumerable<KeyValuePair<string, (DlcType type, string name, string icon)>> blacklistDlc = selection.AllDlc.Except(selection.SelectedDlc);
        foreach (KeyValuePair<string, (string _, SortedList<string, (DlcType type, string name, string icon)> extraDlc)> pair in selection.ExtraSelectedDlc)
            blacklistDlc = blacklistDlc.Except(pair.Value.extraDlc);
        blacklistDlc = blacklistDlc.ToList();
        if (blacklistDlc.Any())
        {
            /*if (installForm is not null)
                installForm.UpdateUser("Generating Uplay R1 Unlocker configuration for " + selection.Name + $" in directory \"{directory}\" . . . ", LogTextBox.Operation);*/
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
        writer.WriteLine("  \"hook_loader\": false,");
        if (blacklistDlc.Count > 0)
        {
            writer.WriteLine("  \"blacklist\": [");
            KeyValuePair<string, (DlcType type, string name, string icon)> lastBlacklistDlc = blacklistDlc.Last();
            foreach (KeyValuePair<string, (DlcType type, string name, string icon)> pair in blacklistDlc)
            {
                string dlcId = pair.Key;
                (_, string dlcName, _) = pair.Value;
                writer.WriteLine($"    {dlcId}{(pair.Equals(lastBlacklistDlc) ? "" : ",")}");
                installForm?.UpdateUser($"Added blacklist DLC to UplayR1Unlocker.jsonc with appid {dlcId} ({dlcName})", LogTextBox.Action, false);
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
            directory.GetUplayR1Components(out string api32, out string api32_o, out string api64, out string api64_o, out string config, out string log);
            if (File.Exists(api32_o))
            {
                if (File.Exists(api32))
                {
                    File.Delete(api32);
                    installForm?.UpdateUser($"Deleted Uplay R1 Unlocker: {Path.GetFileName(api32)}", LogTextBox.Action, false);
                }
                File.Move(api32_o, api32!);
                installForm?.UpdateUser($"Restored Uplay R1: {Path.GetFileName(api32_o)} -> {Path.GetFileName(api32)}", LogTextBox.Action, false);
            }
            if (File.Exists(api64_o))
            {
                if (File.Exists(api64))
                {
                    File.Delete(api64);
                    installForm?.UpdateUser($"Deleted Uplay R1 Unlocker: {Path.GetFileName(api64)}", LogTextBox.Action, false);
                }
                File.Move(api64_o, api64!);
                installForm?.UpdateUser($"Restored Uplay R1: {Path.GetFileName(api64_o)} -> {Path.GetFileName(api64)}", LogTextBox.Action, false);
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
            directory.GetUplayR1Components(out string api32, out string api32_o, out string api64, out string api64_o, out _, out _);
            if (File.Exists(api32) && !File.Exists(api32_o))
            {
                File.Move(api32, api32_o!);
                installForm?.UpdateUser($"Renamed Uplay R1: {Path.GetFileName(api32)} -> {Path.GetFileName(api32_o)}", LogTextBox.Action, false);
            }
            if (File.Exists(api32_o))
            {
                "UplayR1.uplay_r1_loader.dll".Write(api32);
                installForm?.UpdateUser($"Wrote Uplay R1 Unlocker: {Path.GetFileName(api32)}", LogTextBox.Action, false);
            }
            if (File.Exists(api64) && !File.Exists(api64_o))
            {
                File.Move(api64, api64_o!);
                installForm?.UpdateUser($"Renamed Uplay R1: {Path.GetFileName(api64)} -> {Path.GetFileName(api64_o)}", LogTextBox.Action, false);
            }
            if (File.Exists(api64_o))
            {
                "UplayR1.uplay_r1_loader64.dll".Write(api64);
                installForm?.UpdateUser($"Wrote Uplay R1 Unlocker: {Path.GetFileName(api64)}", LogTextBox.Action, false);
            }
            if (generateConfig)
                CheckConfig(directory, selection, installForm);
        });
}