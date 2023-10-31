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

    internal static void CheckConfig(string directory, Selection selection, InstallForm installForm = null)
    {
        directory.GetSmokeApiComponents(out _, out _, out _, out _, out string old_config, out string config, out _, out _, out _);
        HashSet<SelectionDLC> overrideDlc = selection.DLC.Where(dlc => !dlc.Enabled).ToHashSet();
        foreach (SelectionDLC extraDlc in selection.ExtraSelections.SelectMany(extraSelection => extraSelection.DLC.Where(dlc => !dlc.Enabled)))
            _ = overrideDlc.Add(extraDlc);
        HashSet<SelectionDLC> injectDlc = new();
        if (selection.DLC.Count() > 64)
            foreach (SelectionDLC hiddenDlc in selection.DLC.Where(dlc => dlc.Enabled && dlc.Type is DLCType.SteamHidden))
                _ = injectDlc.Add(hiddenDlc);
        List<KeyValuePair<string, (string name, SortedList<string, SelectionDLC> injectDlc)>> extraApps = new();
        foreach (Selection extraSelection in selection.ExtraSelections.Where(extraSelection => extraSelection.DLC.Count() > 64))
        {
            SortedList<string, SelectionDLC> extraInjectDlc = new(PlatformIdComparer.String);
            foreach (SelectionDLC extraDlc in extraSelection.DLC.Where(extraDlc => extraDlc.Enabled && extraDlc.Type is DLCType.SteamHidden))
                extraInjectDlc.Add(extraDlc.Id, extraDlc);
            if (extraInjectDlc.Count > 0)
                extraApps.Add(new(extraSelection.Id, (extraSelection.Name, extraInjectDlc)));
        }
        if (old_config.FileExists())
        {
            old_config.DeleteFile();
            installForm?.UpdateUser($"Deleted old configuration: {Path.GetFileName(old_config)}", LogTextBox.Action, false);
        }
        if (selection.ExtraSelections.Any(extraSelection => extraSelection.DLC.Any()) || overrideDlc.Count > 0 || injectDlc.Count > 0)
        {
            /*if (installForm is not null)
                installForm.UpdateUser("Generating SmokeAPI configuration for " + selection.Name + $" in directory \"{directory}\" . . . ", LogTextBox.Operation);*/
            config.CreateFile(true, installForm)?.Close();
            StreamWriter writer = new(config, true, Encoding.UTF8);
            WriteConfig(writer, selection.Id, new(extraApps.ToDictionary(extraApp => extraApp.Key, extraApp => extraApp.Value), PlatformIdComparer.String),
                new(overrideDlc.ToDictionary(dlc => dlc.Id, dlc => dlc), PlatformIdComparer.String),
                new(injectDlc.ToDictionary(dlc => dlc.Id, dlc => dlc), PlatformIdComparer.String), installForm);
            writer.Flush();
            writer.Close();
        }
        else if (config.FileExists())
        {
            config.DeleteFile();
            installForm?.UpdateUser($"Deleted unnecessary configuration: {Path.GetFileName(config)}", LogTextBox.Action, false);
        }
    }

    private static void WriteConfig(TextWriter writer, string appId, SortedList<string, (string name, SortedList<string, SelectionDLC> injectDlc)> extraApps,
        SortedList<string, SelectionDLC> overrideDlc, SortedList<string, SelectionDLC> injectDlc, InstallForm installForm = null)
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
            KeyValuePair<string, SelectionDLC> lastOverrideDlc = overrideDlc.Last();
            foreach (KeyValuePair<string, SelectionDLC> pair in overrideDlc)
            {
                SelectionDLC selectionDlc = pair.Value;
                writer.WriteLine($"    \"{selectionDlc.Id}\": \"locked\"{(pair.Equals(lastOverrideDlc) ? "" : ",")}");
                installForm?.UpdateUser($"Added locked DLC to SmokeAPI.config.json with appid {selectionDlc.Id} ({selectionDlc.Name})", LogTextBox.Action,
                    false);
            }
            writer.WriteLine("  },");
        }
        else
            writer.WriteLine("  \"override_dlc_status\": {},");
        writer.WriteLine("  \"auto_inject_inventory\": true,");
        writer.WriteLine("  \"extra_inventory_items\": [],");
        if (injectDlc.Count > 0 || extraApps.Count > 0)
        {
            writer.WriteLine("  \"extra_dlcs\": {");
            if (injectDlc.Count > 0)
            {
                writer.WriteLine("    \"" + appId + "\": {");
                writer.WriteLine("      \"dlcs\": {");
                KeyValuePair<string, SelectionDLC> lastInjectDlc = injectDlc.Last();
                foreach (KeyValuePair<string, SelectionDLC> pair in injectDlc)
                {
                    SelectionDLC selectionDlc = pair.Value;
                    writer.WriteLine($"        \"{selectionDlc.Id}\": \"{selectionDlc.Name}\"{(pair.Equals(lastInjectDlc) ? "" : ",")}");
                    installForm?.UpdateUser($"Added extra DLC to SmokeAPI.config.json with appid {selectionDlc.Id} ({selectionDlc.Name})", LogTextBox.Action,
                        false);
                }
                writer.WriteLine("      }");
                writer.WriteLine(extraApps.Count > 0 ? "    }," : "    }");
            }
            if (extraApps.Count > 0)
            {
                KeyValuePair<string, (string name, SortedList<string, SelectionDLC> injectDlc)> lastExtraApp = extraApps.Last();
                foreach (KeyValuePair<string, (string name, SortedList<string, SelectionDLC> injectDlc)> pair in extraApps)
                {
                    string extraAppId = pair.Key;
                    (string _ /*extraAppName*/, SortedList<string, SelectionDLC> extraInjectDlc) = pair.Value;
                    writer.WriteLine("    \"" + extraAppId + "\": {");
                    writer.WriteLine("      \"dlcs\": {");
                    //installForm?.UpdateUser($"Added extra app to SmokeAPI.config.json with appid {extraAppId} ({extraAppName})", LogTextBox.Action, false);
                    KeyValuePair<string, SelectionDLC> lastExtraAppDlc = extraInjectDlc.Last();
                    foreach (KeyValuePair<string, SelectionDLC> extraPair in extraInjectDlc)
                    {
                        SelectionDLC selectionDlc = extraPair.Value;
                        writer.WriteLine($"        \"{selectionDlc.Id}\": \"{selectionDlc.Name}\"{(extraPair.Equals(lastExtraAppDlc) ? "" : ",")}");
                        installForm?.UpdateUser($"Added extra DLC to SmokeAPI.config.json with appid {selectionDlc.Id} ({selectionDlc.Name})",
                            LogTextBox.Action, false);
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
            if (oldConfig.FileExists())
            {
                oldConfig.DeleteFile();
                installForm?.UpdateUser($"Deleted old CreamAPI configuration: {Path.GetFileName(oldConfig)}", LogTextBox.Action, false);
            }
            directory.GetSmokeApiComponents(out string api32, out string api32_o, out string api64, out string api64_o, out string old_config,
                out string config, out string old_log, out string log, out string cache);
            if (api32_o.FileExists())
            {
                if (api32.FileExists())
                {
                    api32.DeleteFile(true);
                    installForm?.UpdateUser($"Deleted SmokeAPI: {Path.GetFileName(api32)}", LogTextBox.Action, false);
                }
                api32_o.MoveFile(api32!);
                installForm?.UpdateUser($"Restored Steamworks: {Path.GetFileName(api32_o)} -> {Path.GetFileName(api32)}", LogTextBox.Action, false);
            }
            if (api64_o.FileExists())
            {
                if (api64.FileExists())
                {
                    api64.DeleteFile(true);
                    installForm?.UpdateUser($"Deleted SmokeAPI: {Path.GetFileName(api64)}", LogTextBox.Action, false);
                }
                api64_o.MoveFile(api64!);
                installForm?.UpdateUser($"Restored Steamworks: {Path.GetFileName(api64_o)} -> {Path.GetFileName(api64)}", LogTextBox.Action, false);
            }
            if (!deleteOthers)
                return;
            if (old_config.FileExists())
            {
                old_config.DeleteFile();
                installForm?.UpdateUser($"Deleted configuration: {Path.GetFileName(old_config)}", LogTextBox.Action, false);
            }
            if (config.FileExists())
            {
                config.DeleteFile();
                installForm?.UpdateUser($"Deleted configuration: {Path.GetFileName(config)}", LogTextBox.Action, false);
            }
            if (cache.FileExists())
            {
                cache.DeleteFile();
                installForm?.UpdateUser($"Deleted cache: {Path.GetFileName(cache)}", LogTextBox.Action, false);
            }
            if (old_log.FileExists())
            {
                old_log.DeleteFile();
                installForm?.UpdateUser($"Deleted log: {Path.GetFileName(old_log)}", LogTextBox.Action, false);
            }
            if (log.FileExists())
            {
                log.DeleteFile();
                installForm?.UpdateUser($"Deleted log: {Path.GetFileName(log)}", LogTextBox.Action, false);
            }
        });

    internal static async Task Install(string directory, Selection selection, InstallForm installForm = null, bool generateConfig = true)
        => await Task.Run(() =>
        {
            directory.GetCreamApiComponents(out _, out _, out _, out _, out string oldConfig);
            if (oldConfig.FileExists())
            {
                oldConfig.DeleteFile();
                installForm?.UpdateUser($"Deleted old CreamAPI configuration: {Path.GetFileName(oldConfig)}", LogTextBox.Action, false);
            }
            directory.GetSmokeApiComponents(out string api32, out string api32_o, out string api64, out string api64_o, out _, out _, out _, out _, out _);
            if (api32.FileExists() && !api32_o.FileExists())
            {
                api32.MoveFile(api32_o!, true);
                installForm?.UpdateUser($"Renamed Steamworks: {Path.GetFileName(api32)} -> {Path.GetFileName(api32_o)}", LogTextBox.Action, false);
            }
            if (api32_o.FileExists())
            {
                "SmokeAPI.steam_api.dll".WriteManifestResource(api32);
                installForm?.UpdateUser($"Wrote SmokeAPI: {Path.GetFileName(api32)}", LogTextBox.Action, false);
            }
            if (api64.FileExists() && !api64_o.FileExists())
            {
                api64.MoveFile(api64_o!, true);
                installForm?.UpdateUser($"Renamed Steamworks: {Path.GetFileName(api64)} -> {Path.GetFileName(api64_o)}", LogTextBox.Action, false);
            }
            if (api64_o.FileExists())
            {
                "SmokeAPI.steam_api64.dll".WriteManifestResource(api64);
                installForm?.UpdateUser($"Wrote SmokeAPI: {Path.GetFileName(api64)}", LogTextBox.Action, false);
            }
            if (generateConfig)
                CheckConfig(directory, selection, installForm);
        });
}