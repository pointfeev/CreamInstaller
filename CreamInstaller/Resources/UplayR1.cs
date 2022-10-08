using CreamInstaller.Components;
using CreamInstaller.Utility;

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CreamInstaller.Resources;

internal static class UplayR1
{
    internal static void GetUplayR1Components(
            this string directory,
            out string api32, out string api32_o,
            out string api64, out string api64_o,
            out string config
        )
    {
        api32 = directory + @"\uplay_r1_loader.dll";
        api32_o = directory + @"\uplay_r1_loader_o.dll";
        api64 = directory + @"\uplay_r1_loader64.dll";
        api64_o = directory + @"\uplay_r1_loader64_o.dll";
        config = directory + @"\UplayR1Unlocker.jsonc";
    }

    internal static void CheckConfig(string directory, ProgramSelection selection, InstallForm installForm = null)
    {
        directory.GetUplayR1Components(out _, out _, out _, out _, out string config);
        IEnumerable<KeyValuePair<string, (DlcType type, string name, string icon)>> blacklistDlc = selection.AllDlc.Except(selection.SelectedDlc);
        foreach ((string id, string name, SortedList<string, (DlcType type, string name, string icon)> extraDlc) in selection.ExtraSelectedDlc)
            blacklistDlc = blacklistDlc.Except(extraDlc);
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
            if (installForm is not null)
                installForm.UpdateUser($"Deleted unnecessary configuration: {Path.GetFileName(config)}", LogTextBox.Action, info: false);
        }
    }

    internal static void WriteConfig(StreamWriter writer, SortedList<string, (DlcType type, string name, string icon)> blacklistDlc, InstallForm installForm = null)
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
                if (installForm is not null)
                    installForm.UpdateUser($"Added blacklist DLC to UplayR1Unlocker.jsonc with appid {dlcId} ({dlcName})", LogTextBox.Action, info: false);
            }
            writer.WriteLine("  ],");
        }
        else
            writer.WriteLine("  \"blacklist\": [],");
        writer.WriteLine("}");
    }

    internal static async Task Uninstall(string directory, InstallForm installForm = null, bool deleteConfig = true) => await Task.Run(() =>
    {
        directory.GetUplayR1Components(out string api32, out string api32_o, out string api64, out string api64_o, out string config);
        if (File.Exists(api32_o))
        {
            if (File.Exists(api32))
            {
                File.Delete(api32);
                if (installForm is not null)
                    installForm.UpdateUser($"Deleted Uplay R1 Unlocker: {Path.GetFileName(api32)}", LogTextBox.Action, info: false);
            }
            File.Move(api32_o, api32);
            if (installForm is not null)
                installForm.UpdateUser($"Restored Uplay R1: {Path.GetFileName(api32_o)} -> {Path.GetFileName(api32)}", LogTextBox.Action, info: false);
        }
        if (File.Exists(api64_o))
        {
            if (File.Exists(api64))
            {
                File.Delete(api64);
                if (installForm is not null)
                    installForm.UpdateUser($"Deleted Uplay R1 Unlocker: {Path.GetFileName(api64)}", LogTextBox.Action, info: false);
            }
            File.Move(api64_o, api64);
            if (installForm is not null)
                installForm.UpdateUser($"Restored Uplay R1: {Path.GetFileName(api64_o)} -> {Path.GetFileName(api64)}", LogTextBox.Action, info: false);
        }
        if (deleteConfig && File.Exists(config))
        {
            File.Delete(config);
            if (installForm is not null)
                installForm.UpdateUser($"Deleted configuration: {Path.GetFileName(config)}", LogTextBox.Action, info: false);
        }
    });

    internal static async Task Install(string directory, ProgramSelection selection, InstallForm installForm = null, bool generateConfig = true) => await Task.Run(() =>
    {
        directory.GetUplayR1Components(out string api32, out string api32_o, out string api64, out string api64_o, out string config);
        if (File.Exists(api32) && !File.Exists(api32_o))
        {
            File.Move(api32, api32_o);
            if (installForm is not null)
                installForm.UpdateUser($"Renamed Uplay R1: {Path.GetFileName(api32)} -> {Path.GetFileName(api32_o)}", LogTextBox.Action, info: false);
        }
        if (File.Exists(api32_o))
        {
            "UplayR1.uplay_r1_loader.dll".Write(api32);
            if (installForm is not null)
                installForm.UpdateUser($"Wrote Uplay R1 Unlocker: {Path.GetFileName(api32)}", LogTextBox.Action, info: false);
        }
        if (File.Exists(api64) && !File.Exists(api64_o))
        {
            File.Move(api64, api64_o);
            if (installForm is not null)
                installForm.UpdateUser($"Renamed Uplay R1: {Path.GetFileName(api64)} -> {Path.GetFileName(api64_o)}", LogTextBox.Action, info: false);
        }
        if (File.Exists(api64_o))
        {
            "UplayR1.uplay_r1_loader64.dll".Write(api64);
            if (installForm is not null)
                installForm.UpdateUser($"Wrote Uplay R1 Unlocker: {Path.GetFileName(api64)}", LogTextBox.Action, info: false);
        }
        if (generateConfig)
            CheckConfig(directory, selection, installForm);
    });
}
