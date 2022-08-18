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
            out string sdk32, out string sdk32_o,
            out string sdk64, out string sdk64_o,
            out string config
        )
    {
        sdk32 = directory + @"\uplay_r1_loader.dll";
        sdk32_o = directory + @"\uplay_r1_loader_o.dll";
        sdk64 = directory + @"\uplay_r1_loader64.dll";
        sdk64_o = directory + @"\uplay_r1_loader64_o.dll";
        config = directory + @"\UplayR1Unlocker.jsonc";
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
                    installForm.UpdateUser($"Added blacklist DLC to UplayR1Unlocker.jsonc with appid {dlcId} ({dlcName})", InstallationLog.Action, info: false);
            }
            writer.WriteLine("  ],");
        }
        else
            writer.WriteLine("  \"blacklist\": [],");
        writer.WriteLine("}");
    }

    internal static async Task Uninstall(string directory, InstallForm installForm = null, bool deleteConfig = true) => await Task.Run(() =>
    {
        directory.GetUplayR1Components(out string sdk32, out string sdk32_o, out string sdk64, out string sdk64_o, out string config);
        if (File.Exists(sdk32_o))
        {
            if (File.Exists(sdk32))
            {
                File.Delete(sdk32);
                if (installForm is not null)
                    installForm.UpdateUser($"Deleted Uplay R1 Unlocker: {Path.GetFileName(sdk32)}", InstallationLog.Action, info: false);
            }
            File.Move(sdk32_o, sdk32);
            if (installForm is not null)
                installForm.UpdateUser($"Restored Uplay R1: {Path.GetFileName(sdk32_o)} -> {Path.GetFileName(sdk32)}", InstallationLog.Action, info: false);
        }
        if (File.Exists(sdk64_o))
        {
            if (File.Exists(sdk64))
            {
                File.Delete(sdk64);
                if (installForm is not null)
                    installForm.UpdateUser($"Deleted Uplay R1 Unlocker: {Path.GetFileName(sdk64)}", InstallationLog.Action, info: false);
            }
            File.Move(sdk64_o, sdk64);
            if (installForm is not null)
                installForm.UpdateUser($"Restored Uplay R1: {Path.GetFileName(sdk64_o)} -> {Path.GetFileName(sdk64)}", InstallationLog.Action, info: false);
        }
        if (deleteConfig && File.Exists(config))
        {
            File.Delete(config);
            if (installForm is not null)
                installForm.UpdateUser($"Deleted configuration: {Path.GetFileName(config)}", InstallationLog.Action, info: false);
        }
    });

    internal static async Task Install(string directory, ProgramSelection selection, InstallForm installForm = null, bool generateConfig = true) => await Task.Run(() =>
    {
        directory.GetUplayR1Components(out string sdk32, out string sdk32_o, out string sdk64, out string sdk64_o, out string config);
        if (File.Exists(sdk32) && !File.Exists(sdk32_o))
        {
            File.Move(sdk32, sdk32_o);
            if (installForm is not null)
                installForm.UpdateUser($"Renamed Uplay R1: {Path.GetFileName(sdk32)} -> {Path.GetFileName(sdk32_o)}", InstallationLog.Action, info: false);
        }
        if (File.Exists(sdk32_o))
        {
            Properties.Resources.Uplay32.Write(sdk32);
            if (installForm is not null)
                installForm.UpdateUser($"Wrote Uplay R1 Unlocker: {Path.GetFileName(sdk32)}", InstallationLog.Action, info: false);
        }
        if (File.Exists(sdk64) && !File.Exists(sdk64_o))
        {
            File.Move(sdk64, sdk64_o);
            if (installForm is not null)
                installForm.UpdateUser($"Renamed Uplay R1: {Path.GetFileName(sdk64)} -> {Path.GetFileName(sdk64_o)}", InstallationLog.Action, info: false);
        }
        if (File.Exists(sdk64_o))
        {
            Properties.Resources.Uplay64.Write(sdk64);
            if (installForm is not null)
                installForm.UpdateUser($"Wrote Uplay R1 Unlocker: {Path.GetFileName(sdk64)}", InstallationLog.Action, info: false);
        }
        if (generateConfig)
        {
            IEnumerable<KeyValuePair<string, (DlcType type, string name, string icon)>> blacklistDlc = selection.AllDlc.Except(selection.SelectedDlc);
            foreach ((string id, string name, SortedList<string, (DlcType type, string name, string icon)> extraDlc) in selection.ExtraSelectedDlc)
                blacklistDlc = blacklistDlc.Except(extraDlc);
            if (blacklistDlc.Any())
            {
                if (installForm is not null)
                    installForm.UpdateUser("Generating Uplay R1 Unlocker configuration for " + selection.Name + $" in directory \"{directory}\" . . . ", InstallationLog.Operation);
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
                    installForm.UpdateUser($"Deleted unnecessary configuration: {Path.GetFileName(config)}", InstallationLog.Action, info: false);
            }
        }
    });
}
