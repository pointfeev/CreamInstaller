using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CreamInstaller.Components;
using CreamInstaller.Forms;
using CreamInstaller.Utility;

namespace CreamInstaller.Resources;

internal static class SmokeAPI
{
    internal static void GetSmokeApiComponents(this string directory, out string api32, out string api32_o, out string api64, out string api64_o,
        out string old_config, out string config, out string old_log, out string log, out string cache)
    {
        api32 = directory + @"\steam_api.dll";
        api32_o = directory + @"\steam_api_o.dll";
        api64 = directory + @"\steam_api64.dll";
        api64_o = directory + @"\steam_api64_o.dll";
        old_config = directory + @"\SmokeAPI.json";
        config = directory + @"\SmokeAPI.config.json";
        old_log = directory + @"\SmokeAPI.log";
        log = directory + @"\SmokeAPI.log.log";
        cache = directory + @"\SmokeAPI.cache.json";
    }

    internal static void CheckConfig(string directory, ProgramSelection selection, InstallForm installForm = null)
    {
        directory.GetSmokeApiComponents(out _, out _, out _, out _, out string old_config, out string config, out _, out _, out _);
        List<KeyValuePair<string, (DlcType type, string name, string icon)>> overrideDlc = selection.AllDlc.Except(selection.SelectedDlc).ToList();
        foreach (KeyValuePair<string, (string name, SortedList<string, (DlcType type, string name, string icon)> dlc)> pair in selection.ExtraDlc)
            if (selection.ExtraSelectedDlc.TryGetValue(pair.Key,
                    out (string name, SortedList<string, (DlcType type, string name, string icon)> dlc) selectedPair))
                overrideDlc.AddRange(pair.Value.dlc.Except(selectedPair.dlc));
        List<KeyValuePair<string, (DlcType type, string name, string icon)>> injectDlc = new();
        if (selection.AllDlc.Count > 64)
            injectDlc.AddRange(selection.SelectedDlc.Where(pair => pair.Value.type is DlcType.SteamHidden));
        List<KeyValuePair<string, (string name, SortedList<string, (DlcType type, string name, string icon)> injectDlc)>> extraApps = new();
        if (selection.ExtraDlc.Any(e => e.Value.dlc.Count > 64))
            foreach (KeyValuePair<string, (string name, SortedList<string, (DlcType type, string name, string icon)> injectDlc)> pair in selection
                        .ExtraSelectedDlc)
                if (selection.ExtraDlc.First(e => e.Key == pair.Key).Value.dlc.Count > 64)
                {
                    SortedList<string, (DlcType type, string name, string icon)> extraInjectDlc = new(PlatformIdComparer.String);
                    foreach (KeyValuePair<string, (DlcType type, string name, string icon)> extraPair in pair.Value.injectDlc.Where(extraPair
                                 => extraPair.Value.type is DlcType.SteamHidden))
                        extraInjectDlc.Add(extraPair.Key, extraPair.Value);
                    KeyValuePair<string, (string name, SortedList<string, (DlcType type, string name, string icon)> injectDlc)> newExtraPair = new(pair.Key,
                        (pair.Value.name, extraInjectDlc));
                    extraApps.Add(newExtraPair);
                }
        injectDlc = injectDlc.ToList();
        if (File.Exists(old_config))
        {
            File.Delete(old_config);
            installForm?.UpdateUser($"Deleted old configuration: {Path.GetFileName(old_config)}", LogTextBox.Action, false);
        }
        if (selection.ExtraSelectedDlc.Any(p => p.Value.dlc.Any()) || overrideDlc.Any() || injectDlc.Any())
        {
            /*if (installForm is not null)
                installForm.UpdateUser("Generating SmokeAPI configuration for " + selection.Name + $" in directory \"{directory}\" . . . ", LogTextBox.Operation);*/
            File.Create(config).Close();
            StreamWriter writer = new(config, true, Encoding.UTF8);
            WriteConfig(writer, selection.Id, new(extraApps.ToDictionary(pair => pair.Key, pair => pair.Value), PlatformIdComparer.String),
                new(overrideDlc.ToDictionary(pair => pair.Key, pair => pair.Value), PlatformIdComparer.String),
                new(injectDlc.ToDictionary(pair => pair.Key, pair => pair.Value), PlatformIdComparer.String), installForm);
            writer.Flush();
            writer.Close();
        }
        else if (File.Exists(config))
        {
            File.Delete(config);
            installForm?.UpdateUser($"Deleted unnecessary configuration: {Path.GetFileName(config)}", LogTextBox.Action, false);
        }
    }

    private static void WriteConfig(StreamWriter writer, string appId,
        SortedList<string, (string name, SortedList<string, (DlcType type, string name, string icon)> injectDlc)> extraApps,
        SortedList<string, (DlcType type, string name, string icon)> overrideDlc, SortedList<string, (DlcType type, string name, string icon)> injectDlc,
        InstallForm installForm = null)
    {
        writer.WriteLine("{");
        writer.WriteLine("  \"$version\": 2,");
        writer.WriteLine("  \"logging\": false,");
        writer.WriteLine("  \"unlock_family_sharing\": true,");
        writer.WriteLine("  \"default_app_status\": \"unlocked\",");
        writer.WriteLine("  \"override_app_status\": {},");
        if (overrideDlc.Count > 0)
        {
            writer.WriteLine("  \"override_dlc_status\": {");
            KeyValuePair<string, (DlcType type, string name, string icon)> lastOverrideDlc = overrideDlc.Last();
            foreach (KeyValuePair<string, (DlcType type, string name, string icon)> pair in overrideDlc)
            {
                string dlcId = pair.Key;
                (_, string dlcName, _) = pair.Value;
                writer.WriteLine($"    \"{dlcId}\": \"locked\"{(pair.Equals(lastOverrideDlc) ? "" : ",")}");
                installForm?.UpdateUser($"Added locked DLC to SmokeAPI.config.json with appid {dlcId} ({dlcName})", LogTextBox.Action, false);
            }
            writer.WriteLine("  },");
        }
        else
            writer.WriteLine("  \"override_dlc_status\": {},");
        writer.WriteLine("  \"auto_inject_inventory\": true,");
        writer.WriteLine("  \"extra_inventory_items\": {},");
        if (injectDlc.Count > 0 || extraApps.Count > 0)
        {
            writer.WriteLine("  \"extra_dlcs\": {");
            if (injectDlc.Count > 0)
            {
                writer.WriteLine("    \"" + appId + "\": {");
                writer.WriteLine("      \"dlcs\": {");
                KeyValuePair<string, (DlcType type, string name, string icon)> lastInjectDlc = injectDlc.Last();
                foreach (KeyValuePair<string, (DlcType type, string name, string icon)> pair in injectDlc)
                {
                    string dlcId = pair.Key;
                    (_, string dlcName, _) = pair.Value;
                    writer.WriteLine($"        \"{dlcId}\": \"{dlcName}\"{(pair.Equals(lastInjectDlc) ? "" : ",")}");
                    installForm?.UpdateUser($"Added extra DLC to SmokeAPI.config.json with appid {dlcId} ({dlcName})", LogTextBox.Action, false);
                }
                writer.WriteLine("      }");
                writer.WriteLine(extraApps.Count > 0 ? "    }," : "    }");
            }
            if (extraApps.Count > 0)
            {
                KeyValuePair<string, (string name, SortedList<string, (DlcType type, string name, string icon)> injectDlc)> lastExtraApp = extraApps.Last();
                foreach (KeyValuePair<string, (string name, SortedList<string, (DlcType type, string name, string icon)> injectDlc)> pair in extraApps)
                {
                    string extraAppId = pair.Key;
                    (string _ /*extraAppName*/, SortedList<string, (DlcType type, string name, string icon)> extraInjectDlc) = pair.Value;
                    writer.WriteLine("    \"" + extraAppId + "\": {");
                    writer.WriteLine("      \"dlcs\": {");
                    //installForm?.UpdateUser($"Added extra app to SmokeAPI.config.json with appid {extraAppId} ({extraAppName})", LogTextBox.Action, false);
                    KeyValuePair<string, (DlcType type, string name, string icon)> lastExtraAppDlc = extraInjectDlc.Last();
                    foreach (KeyValuePair<string, (DlcType type, string name, string icon)> extraPair in extraInjectDlc)
                    {
                        string dlcId = extraPair.Key;
                        (_, string dlcName, _) = extraPair.Value;
                        writer.WriteLine($"        \"{dlcId}\": \"{dlcName}\"{(extraPair.Equals(lastExtraAppDlc) ? "" : ",")}");
                        installForm?.UpdateUser($"Added extra DLC to SmokeAPI.config.json with appid {dlcId} ({dlcName})", LogTextBox.Action, false);
                    }
                    writer.WriteLine("      }");
                    writer.WriteLine(pair.Equals(lastExtraApp) ? "    }" : "    },");
                }
            }
            writer.WriteLine("  },");
        }
        else
            writer.WriteLine("  \"extra_dlcs\": {},");
        writer.WriteLine("  \"store_config\": null");
        writer.WriteLine("}");
    }

    internal static async Task Uninstall(string directory, InstallForm installForm = null, bool deleteOthers = true)
        => await Task.Run(() =>
        {
            directory.GetCreamApiComponents(out _, out _, out _, out _, out string oldConfig);
            if (File.Exists(oldConfig))
            {
                File.Delete(oldConfig);
                installForm?.UpdateUser($"Deleted old CreamAPI configuration: {Path.GetFileName(oldConfig)}", LogTextBox.Action, false);
            }
            directory.GetSmokeApiComponents(out string api32, out string api32_o, out string api64, out string api64_o, out string old_config,
                out string config, out string old_log, out string log, out string cache);
            if (File.Exists(api32_o))
            {
                if (File.Exists(api32))
                {
                    File.Delete(api32);
                    installForm?.UpdateUser($"Deleted SmokeAPI: {Path.GetFileName(api32)}", LogTextBox.Action, false);
                }
                File.Move(api32_o, api32!);
                installForm?.UpdateUser($"Restored Steamworks: {Path.GetFileName(api32_o)} -> {Path.GetFileName(api32)}", LogTextBox.Action, false);
            }
            if (File.Exists(api64_o))
            {
                if (File.Exists(api64))
                {
                    File.Delete(api64);
                    installForm?.UpdateUser($"Deleted SmokeAPI: {Path.GetFileName(api64)}", LogTextBox.Action, false);
                }
                File.Move(api64_o, api64!);
                installForm?.UpdateUser($"Restored Steamworks: {Path.GetFileName(api64_o)} -> {Path.GetFileName(api64)}", LogTextBox.Action, false);
            }
            if (!deleteOthers)
                return;
            if (File.Exists(old_config))
            {
                File.Delete(old_config);
                installForm?.UpdateUser($"Deleted configuration: {Path.GetFileName(old_config)}", LogTextBox.Action, false);
            }
            if (File.Exists(config))
            {
                File.Delete(config);
                installForm?.UpdateUser($"Deleted configuration: {Path.GetFileName(config)}", LogTextBox.Action, false);
            }
            if (File.Exists(cache))
            {
                File.Delete(cache);
                installForm?.UpdateUser($"Deleted cache: {Path.GetFileName(cache)}", LogTextBox.Action, false);
            }
            if (File.Exists(old_log))
            {
                File.Delete(old_log);
                installForm?.UpdateUser($"Deleted log: {Path.GetFileName(old_log)}", LogTextBox.Action, false);
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
            directory.GetCreamApiComponents(out _, out _, out _, out _, out string oldConfig);
            if (File.Exists(oldConfig))
            {
                File.Delete(oldConfig);
                installForm?.UpdateUser($"Deleted old CreamAPI configuration: {Path.GetFileName(oldConfig)}", LogTextBox.Action, false);
            }
            directory.GetSmokeApiComponents(out string api32, out string api32_o, out string api64, out string api64_o, out _, out _, out _, out _, out _);
            if (File.Exists(api32) && !File.Exists(api32_o))
            {
                File.Move(api32, api32_o!);
                installForm?.UpdateUser($"Renamed Steamworks: {Path.GetFileName(api32)} -> {Path.GetFileName(api32_o)}", LogTextBox.Action, false);
            }
            if (File.Exists(api32_o))
            {
                "SmokeAPI.steam_api.dll".Write(api32);
                installForm?.UpdateUser($"Wrote SmokeAPI: {Path.GetFileName(api32)}", LogTextBox.Action, false);
            }
            if (File.Exists(api64) && !File.Exists(api64_o))
            {
                File.Move(api64, api64_o!);
                installForm?.UpdateUser($"Renamed Steamworks: {Path.GetFileName(api64)} -> {Path.GetFileName(api64_o)}", LogTextBox.Action, false);
            }
            if (File.Exists(api64_o))
            {
                "SmokeAPI.steam_api64.dll".Write(api64);
                installForm?.UpdateUser($"Wrote SmokeAPI: {Path.GetFileName(api64)}", LogTextBox.Action, false);
            }
            if (generateConfig)
                CheckConfig(directory, selection, installForm);
        });
}