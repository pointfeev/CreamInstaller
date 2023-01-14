using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CreamInstaller.Components;
using CreamInstaller.Forms;
using CreamInstaller.Utility;

namespace CreamInstaller.Resources;

internal static class ScreamAPI
{
    internal static void GetScreamApiComponents(this string directory, out string api32, out string api32_o, out string api64, out string api64_o,
        out string config, out string log)
    {
        api32 = directory + @"\EOSSDK-Win32-Shipping.dll";
        api32_o = directory + @"\EOSSDK-Win32-Shipping_o.dll";
        api64 = directory + @"\EOSSDK-Win64-Shipping.dll";
        api64_o = directory + @"\EOSSDK-Win64-Shipping_o.dll";
        config = directory + @"\ScreamAPI.json";
        log = directory + @"\ScreamAPI.log";
    }

    internal static void CheckConfig(string directory, ProgramSelection selection, InstallForm installForm = null)
    {
        directory.GetScreamApiComponents(out _, out _, out _, out _, out string config, out _);
        IEnumerable<KeyValuePair<string, (DlcType type, string name, string icon)>> overrideCatalogItems
            = selection.AllDlc.Where(pair => pair.Value.type is DlcType.EpicCatalogItem).Except(selection.SelectedDlc);
        foreach (KeyValuePair<string, (string _, SortedList<string, (DlcType type, string name, string icon)> extraDlc)> pair in selection.ExtraSelectedDlc)
            overrideCatalogItems = overrideCatalogItems.Except(pair.Value.extraDlc);
        IEnumerable<KeyValuePair<string, (DlcType type, string name, string icon)>> entitlements
            = selection.SelectedDlc.Where(pair => pair.Value.type == DlcType.EpicEntitlement);
        foreach (KeyValuePair<string, (string _, SortedList<string, (DlcType type, string name, string icon)> dlc)> pair in selection.ExtraSelectedDlc)
            entitlements = entitlements.Concat(pair.Value.dlc.Where(pair => pair.Value.type == DlcType.EpicEntitlement));
        overrideCatalogItems = overrideCatalogItems.ToList();
        entitlements = entitlements.ToList();
        if (overrideCatalogItems.Any() || entitlements.Any())
        {
            /*if (installForm is not null)
                installForm.UpdateUser("Generating ScreamAPI configuration for " + selection.Name + $" in directory \"{directory}\" . . . ", LogTextBox.Operation);*/
            File.Create(config).Close();
            StreamWriter writer = new(config, true, Encoding.UTF8);
            WriteConfig(writer, new(overrideCatalogItems.ToDictionary(pair => pair.Key, pair => pair.Value), PlatformIdComparer.String),
                new(entitlements.ToDictionary(pair => pair.Key, pair => pair.Value), PlatformIdComparer.String), installForm);
            writer.Flush();
            writer.Close();
        }
        else if (File.Exists(config))
        {
            File.Delete(config);
            installForm?.UpdateUser($"Deleted unnecessary configuration: {Path.GetFileName(config)}", LogTextBox.Action, false);
        }
    }

    private static void WriteConfig(StreamWriter writer, SortedList<string, (DlcType type, string name, string icon)> overrideCatalogItems,
        SortedList<string, (DlcType type, string name, string icon)> entitlements, InstallForm installForm = null)
    {
        writer.WriteLine("{");
        writer.WriteLine("  \"version\": 2,");
        writer.WriteLine("  \"logging\": false,");
        writer.WriteLine("  \"eos_logging\": false,");
        writer.WriteLine("  \"block_metrics\": false,");
        writer.WriteLine("  \"catalog_items\": {");
        writer.WriteLine("    \"unlock_all\": true,");
        if (overrideCatalogItems.Any())
        {
            writer.WriteLine("    \"override\": [");
            KeyValuePair<string, (DlcType type, string name, string icon)> lastOverrideCatalogItem = overrideCatalogItems.Last();
            foreach (KeyValuePair<string, (DlcType type, string name, string icon)> pair in overrideCatalogItems)
            {
                string id = pair.Key;
                (_, string name, _) = pair.Value;
                writer.WriteLine($"      \"{id}\"{(pair.Equals(lastOverrideCatalogItem) ? "" : ",")}");
                installForm?.UpdateUser($"Added override catalog item to ScreamAPI.json with id {id} ({name})", LogTextBox.Action, false);
            }
            writer.WriteLine("    ]");
        }
        else
            writer.WriteLine("    \"override\": []");
        writer.WriteLine("  },");
        writer.WriteLine("  \"entitlements\": {");
        writer.WriteLine("    \"unlock_all\": true,");
        writer.WriteLine("    \"auto_inject\": true,");
        if (entitlements.Any())
        {
            writer.WriteLine("    \"inject\": [");
            KeyValuePair<string, (DlcType type, string name, string icon)> lastEntitlement = entitlements.Last();
            foreach (KeyValuePair<string, (DlcType type, string name, string icon)> pair in entitlements)
            {
                string id = pair.Key;
                (_, string name, _) = pair.Value;
                writer.WriteLine($"      \"{id}\"{(pair.Equals(lastEntitlement) ? "" : ",")}");
                installForm?.UpdateUser($"Added entitlement to ScreamAPI.json with id {id} ({name})", LogTextBox.Action, false);
            }
            writer.WriteLine("    ]");
        }
        else
            writer.WriteLine("    \"inject\": []");
        writer.WriteLine("  }");
        writer.WriteLine("}");
    }

    internal static async Task Uninstall(string directory, InstallForm installForm = null, bool deleteOthers = true)
        => await Task.Run(() =>
        {
            directory.GetScreamApiComponents(out string api32, out string api32_o, out string api64, out string api64_o, out string config, out string log);
            if (File.Exists(api32_o))
            {
                if (File.Exists(api32))
                {
                    File.Delete(api32);
                    installForm?.UpdateUser($"Deleted ScreamAPI: {Path.GetFileName(api32)}", LogTextBox.Action, false);
                }
                File.Move(api32_o, api32!);
                installForm?.UpdateUser($"Restored EOS: {Path.GetFileName(api32_o)} -> {Path.GetFileName(api32)}", LogTextBox.Action, false);
            }
            if (File.Exists(api64_o))
            {
                if (File.Exists(api64))
                {
                    File.Delete(api64);
                    installForm?.UpdateUser($"Deleted ScreamAPI: {Path.GetFileName(api64)}", LogTextBox.Action, false);
                }
                File.Move(api64_o, api64!);
                installForm?.UpdateUser($"Restored EOS: {Path.GetFileName(api64_o)} -> {Path.GetFileName(api64)}", LogTextBox.Action, false);
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
            directory.GetScreamApiComponents(out string api32, out string api32_o, out string api64, out string api64_o, out _, out _);
            if (File.Exists(api32) && !File.Exists(api32_o))
            {
                File.Move(api32, api32_o!);
                installForm?.UpdateUser($"Renamed EOS: {Path.GetFileName(api32)} -> {Path.GetFileName(api32_o)}", LogTextBox.Action, false);
            }
            if (File.Exists(api32_o))
            {
                "ScreamAPI.EOSSDK-Win32-Shipping.dll".Write(api32);
                installForm?.UpdateUser($"Wrote ScreamAPI: {Path.GetFileName(api32)}", LogTextBox.Action, false);
            }
            if (File.Exists(api64) && !File.Exists(api64_o))
            {
                File.Move(api64, api64_o!);
                installForm?.UpdateUser($"Renamed EOS: {Path.GetFileName(api64)} -> {Path.GetFileName(api64_o)}", LogTextBox.Action, false);
            }
            if (File.Exists(api64_o))
            {
                "ScreamAPI.EOSSDK-Win64-Shipping.dll".Write(api64);
                installForm?.UpdateUser($"Wrote ScreamAPI: {Path.GetFileName(api64)}", LogTextBox.Action, false);
            }
            if (generateConfig)
                CheckConfig(directory, selection, installForm);
        });
}