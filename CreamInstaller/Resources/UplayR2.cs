using CreamInstaller.Components;
using CreamInstaller.Utility;

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CreamInstaller.Resources;

internal static class UplayR2
{
    internal static void GetUplayR2Components(
            this string directory,
            out string old_sdk32, out string old_sdk64,
            out string sdk32, out string sdk32_o,
            out string sdk64, out string sdk64_o,
            out string config)
    {
        old_sdk32 = directory + @"\uplay_r2_loader.dll";
        old_sdk64 = directory + @"\uplay_r2_loader64.dll";
        sdk32 = directory + @"\upc_r2_loader.dll";
        sdk32_o = directory + @"\upc_r2_loader_o.dll";
        sdk64 = directory + @"\upc_r2_loader64.dll";
        sdk64_o = directory + @"\upc_r2_loader64_o.dll";
        config = directory + @"\UplayR2Unlocker.jsonc";
    }

    internal static void WriteConfig(StreamWriter writer, SortedList<string, (DlcType type, string name, string icon)> blacklistDlc, InstallForm installForm = null)
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
                if (installForm is not null)
                    installForm.UpdateUser($"Added blacklist DLC to UplayR2Unlocker.jsonc with appid {dlcId} ({dlcName})", InstallationLog.Action, info: false);
            }
            writer.WriteLine("  ],");
        }
        else
            writer.WriteLine("  \"blacklist\": [],");
        writer.WriteLine("}");
    }

    internal static async Task Uninstall(string directory, InstallForm installForm = null, bool deleteConfig = true) => await Task.Run(() =>
    {
        directory.GetUplayR2Components(out string old_sdk32, out string old_sdk64, out string sdk32, out string sdk32_o, out string sdk64, out string sdk64_o, out string config);
        if (File.Exists(sdk32_o))
        {
            string sdk = File.Exists(old_sdk32) ? old_sdk32 : sdk32;
            if (File.Exists(sdk))
            {
                File.Delete(sdk);
                if (installForm is not null)
                    installForm.UpdateUser($"Deleted Uplay R2 Unlocker: {Path.GetFileName(sdk)}", InstallationLog.Action, info: false);
            }
            File.Move(sdk32_o, sdk);
            if (installForm is not null)
                installForm.UpdateUser($"Restored Uplay R2: {Path.GetFileName(sdk32_o)} -> {Path.GetFileName(sdk)}", InstallationLog.Action, info: false);
        }
        if (File.Exists(sdk64_o))
        {
            string sdk = File.Exists(old_sdk64) ? old_sdk64 : sdk64;
            if (File.Exists(sdk))
            {
                File.Delete(sdk);
                if (installForm is not null)
                    installForm.UpdateUser($"Deleted Uplay R2 Unlocker: {Path.GetFileName(sdk)}", InstallationLog.Action, info: false);
            }
            File.Move(sdk64_o, sdk);
            if (installForm is not null)
                installForm.UpdateUser($"Restored Uplay R2: {Path.GetFileName(sdk64_o)} -> {Path.GetFileName(sdk)}", InstallationLog.Action, info: false);
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
        directory.GetUplayR2Components(out string old_sdk32, out string old_sdk64, out string sdk32, out string sdk32_o, out string sdk64, out string sdk64_o, out string config);
        string sdk = File.Exists(old_sdk32) ? old_sdk32 : sdk32;
        if (File.Exists(sdk) && !File.Exists(sdk32_o))
        {
            File.Move(sdk, sdk32_o);
            if (installForm is not null)
                installForm.UpdateUser($"Renamed Uplay R2: {Path.GetFileName(sdk)} -> {Path.GetFileName(sdk32_o)}", InstallationLog.Action, info: false);
        }
        if (File.Exists(sdk32_o))
        {
            Properties.Resources.Upc32.Write(sdk);
            if (installForm is not null)
                installForm.UpdateUser($"Wrote Uplay R2 Unlocker: {Path.GetFileName(sdk)}", InstallationLog.Action, info: false);
        }
        sdk = File.Exists(old_sdk64) ? old_sdk64 : sdk64;
        if (File.Exists(sdk) && !File.Exists(sdk64_o))
        {
            File.Move(sdk, sdk64_o);
            if (installForm is not null)
                installForm.UpdateUser($"Renamed Uplay R2: {Path.GetFileName(sdk)} -> {Path.GetFileName(sdk64_o)}", InstallationLog.Action, info: false);
        }
        if (File.Exists(sdk64_o))
        {
            Properties.Resources.Upc64.Write(sdk);
            if (installForm is not null)
                installForm.UpdateUser($"Wrote Uplay R2 Unlocker: {Path.GetFileName(sdk)}", InstallationLog.Action, info: false);
        }
        if (generateConfig)
        {
            IEnumerable<KeyValuePair<string, (DlcType type, string name, string icon)>> blacklistDlc = selection.AllDlc.Except(selection.SelectedDlc);
            foreach ((string id, string name, SortedList<string, (DlcType type, string name, string icon)> extraDlc) in selection.ExtraSelectedDlc)
                blacklistDlc = blacklistDlc.Except(extraDlc);
            if (blacklistDlc.Any())
            {
                if (installForm is not null)
                    installForm.UpdateUser("Generating Uplay R2 Unlocker configuration for " + selection.Name + $" in directory \"{directory}\" . . . ", InstallationLog.Operation);
                File.Create(config).Close();
                StreamWriter writer = new(config, true, Encoding.UTF8);
                WriteConfig(writer, new(blacklistDlc.ToDictionary(pair => pair.Key, pair => pair.Value), AppIdComparer.Comparer), installForm);
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
