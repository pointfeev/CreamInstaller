using CreamInstaller.Components;
using CreamInstaller.Utility;

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CreamInstaller.Resources;

internal static class SmokeAPI
{
    internal static void GetSmokeApiComponents(
            this string directory,
            out string api32, out string api32_o,
            out string api64, out string api64_o,
            out string config,
            out string cache)
    {
        api32 = directory + @"\steam_api.dll";
        api32_o = directory + @"\steam_api_o.dll";
        api64 = directory + @"\steam_api64.dll";
        api64_o = directory + @"\steam_api64_o.dll";
        config = directory + @"\SmokeAPI.json";
        cache = directory + @"\SmokeAPI.cache.json";
    }

    internal static void CheckConfig(string directory, ProgramSelection selection, InstallForm installForm = null)
    {
        directory.GetSmokeApiComponents(out _, out _, out _, out _, out string config, out _);
        IEnumerable<KeyValuePair<string, (DlcType type, string name, string icon)>> overrideDlc = selection.AllDlc.Except(selection.SelectedDlc);
        foreach ((string id, string name, SortedList<string, (DlcType type, string name, string icon)> extraDlc) in selection.ExtraSelectedDlc)
            overrideDlc = overrideDlc.Except(extraDlc);
        IEnumerable<KeyValuePair<string, (DlcType type, string name, string icon)>> injectDlc = new List<KeyValuePair<string, (DlcType type, string name, string icon)>>();
        if (selection.AllDlc.Count > 64 || selection.ExtraDlc.Any(e => e.dlc.Count > 64))
        {
            injectDlc = injectDlc.Concat(selection.SelectedDlc.Where(pair => pair.Value.type is DlcType.SteamHidden));
            foreach ((string id, string name, SortedList<string, (DlcType type, string name, string icon)> extraDlc) in selection.ExtraSelectedDlc)
                if (selection.ExtraDlc.Where(e => e.id == id).Single().dlc.Count > 64)
                    injectDlc = injectDlc.Concat(extraDlc.Where(pair => pair.Value.type is DlcType.SteamHidden));
        }
        if (overrideDlc.Any() || injectDlc.Any())
        {
            /*if (installForm is not null)
                installForm.UpdateUser("Generating SmokeAPI configuration for " + selection.Name + $" in directory \"{directory}\" . . . ", LogTextBox.Operation);*/
            File.Create(config).Close();
            StreamWriter writer = new(config, true, Encoding.UTF8);
            WriteConfig(writer,
                new(overrideDlc.ToDictionary(pair => pair.Key, pair => pair.Value), PlatformIdComparer.String),
                new(injectDlc.ToDictionary(pair => pair.Key, pair => pair.Value), PlatformIdComparer.String),
                installForm);
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

    internal static void WriteConfig(StreamWriter writer, SortedList<string, (DlcType type, string name, string icon)> overrideDlc, SortedList<string, (DlcType type, string name, string icon)> injectDlc, InstallForm installForm = null)
    {
        writer.WriteLine("{");
        writer.WriteLine("  \"$version\": 1,");
        writer.WriteLine("  \"logging\": false,");
        writer.WriteLine("  \"hook_steamclient\": true,");
        writer.WriteLine("  \"unlock_all\": true,");
        if (overrideDlc.Count > 0)
        {
            writer.WriteLine("  \"override\": [");
            KeyValuePair<string, (DlcType type, string name, string icon)> lastOverrideDlc = overrideDlc.Last();
            foreach (KeyValuePair<string, (DlcType type, string name, string icon)> pair in overrideDlc)
            {
                string dlcId = pair.Key;
                (_, string dlcName, _) = pair.Value;
                writer.WriteLine($"    {dlcId}{(pair.Equals(lastOverrideDlc) ? "" : ",")}");
                if (installForm is not null)
                    installForm.UpdateUser($"Added override DLC to SmokeAPI.json with appid {dlcId} ({dlcName})", LogTextBox.Action, info: false);
            }
            writer.WriteLine("  ],");
        }
        else
            writer.WriteLine("  \"override\": [],");
        if (injectDlc.Count > 0)
        {
            writer.WriteLine("  \"dlc_ids\": [");
            KeyValuePair<string, (DlcType type, string name, string icon)> lastInjectDlc = injectDlc.Last();
            foreach (KeyValuePair<string, (DlcType type, string name, string icon)> pair in injectDlc)
            {
                string dlcId = pair.Key;
                (_, string dlcName, _) = pair.Value;
                writer.WriteLine($"    {dlcId}{(pair.Equals(lastInjectDlc) ? "" : ",")}");
                if (installForm is not null)
                    installForm.UpdateUser($"Added inject DLC to SmokeAPI.json with appid {dlcId} ({dlcName})", LogTextBox.Action, info: false);
            }
            writer.WriteLine("  ],");
        }
        else
            writer.WriteLine("  \"dlc_ids\": [],");
        writer.WriteLine("  \"auto_inject_inventory\": true,");
        writer.WriteLine("  \"inventory_items\": []");
        writer.WriteLine("}");
    }

    internal static async Task Uninstall(string directory, InstallForm installForm = null, bool deleteConfig = true) => await Task.Run(() =>
    {
        directory.GetCreamApiComponents(out _, out _, out _, out _, out string oldConfig);
        if (File.Exists(oldConfig))
        {
            File.Delete(oldConfig);
            if (installForm is not null)
                installForm.UpdateUser($"Deleted old CreamAPI configuration: {Path.GetFileName(oldConfig)}", LogTextBox.Action, info: false);
        }
        directory.GetSmokeApiComponents(out string api32, out string api32_o, out string api64, out string api64_o, out string config, out string cache);
        if (File.Exists(api32_o))
        {
            if (File.Exists(api32))
            {
                File.Delete(api32);
                if (installForm is not null)
                    installForm.UpdateUser($"Deleted SmokeAPI: {Path.GetFileName(api32)}", LogTextBox.Action, info: false);
            }
            File.Move(api32_o, api32);
            if (installForm is not null)
                installForm.UpdateUser($"Restored Steamworks: {Path.GetFileName(api32_o)} -> {Path.GetFileName(api32)}", LogTextBox.Action, info: false);
        }
        if (File.Exists(api64_o))
        {
            if (File.Exists(api64))
            {
                File.Delete(api64);
                if (installForm is not null)
                    installForm.UpdateUser($"Deleted SmokeAPI: {Path.GetFileName(api64)}", LogTextBox.Action, info: false);
            }
            File.Move(api64_o, api64);
            if (installForm is not null)
                installForm.UpdateUser($"Restored Steamworks: {Path.GetFileName(api64_o)} -> {Path.GetFileName(api64)}", LogTextBox.Action, info: false);
        }
        if (deleteConfig && File.Exists(config))
        {
            File.Delete(config);
            if (installForm is not null)
                installForm.UpdateUser($"Deleted configuration: {Path.GetFileName(config)}", LogTextBox.Action, info: false);
        }
        if (deleteConfig && File.Exists(cache))
        {
            File.Delete(cache);
            if (installForm is not null)
                installForm.UpdateUser($"Deleted cache: {Path.GetFileName(cache)}", LogTextBox.Action, info: false);
        }
    });

    internal static async Task Install(string directory, ProgramSelection selection, InstallForm installForm = null, bool generateConfig = true) => await Task.Run(() =>
    {
        directory.GetCreamApiComponents(out _, out _, out _, out _, out string oldConfig);
        if (File.Exists(oldConfig))
        {
            File.Delete(oldConfig);
            if (installForm is not null)
                installForm.UpdateUser($"Deleted old CreamAPI configuration: {Path.GetFileName(oldConfig)}", LogTextBox.Action, info: false);
        }
        directory.GetSmokeApiComponents(out string api32, out string api32_o, out string api64, out string api64_o, out string config, out _);
        if (File.Exists(api32) && !File.Exists(api32_o))
        {
            File.Move(api32, api32_o);
            if (installForm is not null)
                installForm.UpdateUser($"Renamed Steamworks: {Path.GetFileName(api32)} -> {Path.GetFileName(api32_o)}", LogTextBox.Action, info: false);
        }
        if (File.Exists(api32_o))
        {
            "SmokeAPI.steam_api.dll".Write(api32);
            if (installForm is not null)
                installForm.UpdateUser($"Wrote SmokeAPI: {Path.GetFileName(api32)}", LogTextBox.Action, info: false);
        }
        if (File.Exists(api64) && !File.Exists(api64_o))
        {
            File.Move(api64, api64_o);
            if (installForm is not null)
                installForm.UpdateUser($"Renamed Steamworks: {Path.GetFileName(api64)} -> {Path.GetFileName(api64_o)}", LogTextBox.Action, info: false);
        }
        if (File.Exists(api64_o))
        {
            "SmokeAPI.steam_api64.dll".Write(api64);
            if (installForm is not null)
                installForm.UpdateUser($"Wrote SmokeAPI: {Path.GetFileName(api64)}", LogTextBox.Action, info: false);
        }
        if (generateConfig)
            CheckConfig(directory, selection, installForm);
    });
}
