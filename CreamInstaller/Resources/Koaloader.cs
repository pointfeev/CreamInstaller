using ABI.System.Collections.Generic;

using CreamInstaller.Components;
using CreamInstaller.Utility;

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Windows.Media.Playback;
using Windows.Networking.Connectivity;

namespace CreamInstaller.Resources;

internal static class Koaloader
{
    internal static void GetKoaloaderComponents(
            this string directory,
            out List<string> proxies,
            out string config
        )
    {
        proxies = new();
        foreach (string proxy in Resources.EmbeddedResources)
        {
            string proxyPath = proxy[(proxy.IndexOf('.') + 1)..];
            proxyPath = proxyPath[(proxyPath.IndexOf('.') + 1)..];
            proxies.Add(directory + @"\" + proxyPath);
        }
        config = directory + @"\Koaloader.json";
    }

    internal static readonly List<(string unlocker, string dll)> AutoLoadDlls = new()
    {
        ("SmokeAPI", "SmokeAPI32.dll"), ("SmokeAPI", "SmokeAPI64.dll"),
        ("ScreamAPI", "ScreamAPI32.dll"), ("ScreamAPI", "ScreamAPI64.dll"),
        ("Uplay R2 Unlocker", "UplayR2Unlocker32.dll"), ("Uplay R2 Unlocker", "UplayR2Unlocker64.dll"),
        ("Uplay R2 Unlocker", "UplayR2Unlocker32.dll"), ("Uplay R2 Unlocker", "UplayR2Unlocker64.dll")
    };

    internal static void WriteConfig(StreamWriter writer, SortedList<string, string> targets, SortedList<string, string> modules, InstallForm installForm = null)
    {
        writer.WriteLine("{");
        writer.WriteLine("  \"logging\": false,");
        writer.WriteLine("  \"enabled\": true,");
        writer.WriteLine("  \"auto_load\": " + (modules.Any() ? "false" : "true") + ",");
        if (targets.Any())
        {
            writer.WriteLine("  \"targets\": [");
            System.Collections.Generic.KeyValuePair<string, string> lastTarget = targets.Last();
            foreach (System.Collections.Generic.KeyValuePair<string, string> pair in targets)
            {
                string path = pair.Value;
                writer.WriteLine($"      \"{path}\"{(pair.Equals(lastTarget) ? "" : ",")}");
                if (installForm is not null)
                    installForm.UpdateUser($"Added target to Koaloader.json with path {path}", InstallationLog.Action, info: false);
            }
            writer.WriteLine("  ]");
        }
        else
            writer.WriteLine("  \"targets\": []");
        if (modules.Any())
        {
            writer.WriteLine("  \"modules\": [");
            System.Collections.Generic.KeyValuePair<string, string> lastModule = modules.Last();
            foreach (System.Collections.Generic.KeyValuePair<string, string> pair in modules)
            {
                string path = pair.Value;
                writer.WriteLine("    {");
                writer.WriteLine($"      \"path\": \"" + path + "\",");
                writer.WriteLine($"      \"required\": true");
                writer.WriteLine("    }" + (pair.Equals(lastModule) ? "" : ","));
                if (installForm is not null)
                    installForm.UpdateUser($"Added module to Koaloader.json with path {path}", InstallationLog.Action, info: false);
            }
            writer.WriteLine("  ]");
        }
        else
            writer.WriteLine("  \"modules\": []");
        writer.WriteLine("}");
    }

    internal static async Task Uninstall(string directory, ProgramSelection selection, InstallForm installForm = null, bool deleteConfig = true) => await Task.Run(() =>
    {
        directory.GetKoaloaderComponents(out List<string> proxies, out string config);
        foreach (string proxy in proxies)
        {
            if (File.Exists(proxy) && proxy.IsResourceFile(Resources.ResourceIdentifier.Koaloader))
            {
                File.Delete(proxy);
                if (installForm is not null)
                    installForm.UpdateUser($"Deleted Koaloader: {Path.GetFileName(proxy)}", InstallationLog.Action, info: false);
            }
        }
        foreach ((string unlocker, string dll) in AutoLoadDlls)
        {
            string path = directory + @"\" + dll;
            if (File.Exists(path))
            {
                File.Delete(path);
                if (installForm is not null)
                    installForm.UpdateUser($"Deleted {unlocker}: {Path.GetFileName(path)}", InstallationLog.Action, info: false);
            }
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
        directory.GetKoaloaderComponents(out List<string> proxies, out string config);
        string path = directory + @"\version.dll";
        "Koaloader.version_64.version.dll".Write(path);
        if (installForm is not null)
            installForm.UpdateUser($"Wrote Koaloader: {Path.GetFileName(path)}", InstallationLog.Action, info: false);
        if (selection.Platform is Platform.Steam or Platform.Paradox)
        {
            bool did32 = false, did64 = false;
            foreach (string dllDirectory in selection.DllDirectories)
            {
                dllDirectory.GetSmokeApiComponents(out string api32, out _, out string api64, out _, out _, out _);
                if (!did32 && File.Exists(api32))
                {
                    did32 = true;
                    path = directory + @"\SmokeAPI32.dll";
                    "SmokeAPI.steam_api.dll".Write(path);
                    if (installForm is not null)
                        installForm.UpdateUser($"Wrote SmokeAPI: {Path.GetFileName(path)}", InstallationLog.Action, info: false);
                }
                if (!did64 && File.Exists(api64))
                {
                    did64 = true;
                    path = directory + @"\SmokeAPI64.dll";
                    "SmokeAPI.steam_api64.dll".Write(path);
                    if (installForm is not null)
                        installForm.UpdateUser($"Wrote SmokeAPI: {Path.GetFileName(path)}", InstallationLog.Action, info: false);
                }
                if (did32 && did64)
                    break;
            }
        }
        if (selection.Platform is Platform.Epic or Platform.Paradox)
        {
            bool did32 = false, did64 = false;
            foreach (string dllDirectory in selection.DllDirectories)
            {
                dllDirectory.GetScreamApiComponents(out string api32, out _, out string api64, out _, out _);
                if (!did32 && File.Exists(api32))
                {
                    did32 = true;
                    path = directory + @"\ScreamAPI32.dll";
                    "ScreamAPI.EOSSDK-Win32-Shipping.dll".Write(path);
                    if (installForm is not null)
                        installForm.UpdateUser($"Wrote ScreamAPI: {Path.GetFileName(path)}", InstallationLog.Action, info: false);
                }
                if (!did64 && File.Exists(api64))
                {
                    did64 = true;
                    path = directory + @"\ScreamAPI64.dll";
                    "ScreamAPI.EOSSDK-Win64-Shipping.dll".Write(path);
                    if (installForm is not null)
                        installForm.UpdateUser($"Wrote ScreamAPI: {Path.GetFileName(path)}", InstallationLog.Action, info: false);
                }
                if (did32 && did64)
                    break;
            }
        }
        if (selection.Platform is Platform.Ubisoft)
        {
            bool did32r1 = false, did64r1 = false, did32r2 = false, did64r2 = false;
            foreach (string dllDirectory in selection.DllDirectories)
            {
                dllDirectory.GetUplayR1Components(out string api32, out _, out string api64, out _, out _);
                if (!did32r1 && File.Exists(api32))
                {
                    did32r1 = true;
                    path = directory + @"\UplayR1Unlocker32.dll";
                    "UplayR1.uplay_r1_loader.dll".Write(path);
                    if (installForm is not null)
                        installForm.UpdateUser($"Wrote Uplay R1 Unlocker: {Path.GetFileName(path)}", InstallationLog.Action, info: false);
                }
                if (!did64r1 && File.Exists(api64))
                {
                    did64r1 = true;
                    path = directory + @"\UplayR1Unlocker64.dll";
                    "UplayR1.uplay_r1_loader64.dll".Write(path);
                    if (installForm is not null)
                        installForm.UpdateUser($"Wrote Uplay R1 Unlocker: {Path.GetFileName(path)}", InstallationLog.Action, info: false);
                }
                dllDirectory.GetUplayR2Components(out string old_api32, out string old_api64, out api32, out _, out api64, out _, out _);
                if (!did32r2 && (File.Exists(old_api32) || File.Exists(old_api32)))
                {
                    did32r2 = true;
                    path = directory + @"\UplayR2Unlocker32.dll";
                    "UplayR2.upc_r2_loader.dll".Write(path);
                    if (installForm is not null)
                        installForm.UpdateUser($"Wrote Uplay R2 Unlocker: {Path.GetFileName(path)}", InstallationLog.Action, info: false);
                }
                if (!did64r2 && (File.Exists(old_api64) || File.Exists(api64)))
                {
                    did64r2 = true;
                    path = directory + @"\UplayR2Unlocker64.dll";
                    "UplayR2.upc_r2_loader64.dll".Write(path);
                    if (installForm is not null)
                        installForm.UpdateUser($"Wrote Uplay R2 Unlocker: {Path.GetFileName(path)}", InstallationLog.Action, info: false);
                }
                if (did32r1 && did64r1 && did32r2 && did64r2)
                    break;
            }
        }
        if (generateConfig)
        {
            SortedList<string, string> targets = new(PlatformIdComparer.String);
            SortedList<string, string> modules = new(PlatformIdComparer.String);
            if (targets.Any() || modules.Any())
            {
                if (installForm is not null)
                    installForm.UpdateUser("Generating Koaloader configuration for " + selection.Name + $" in directory \"{directory}\" . . . ", InstallationLog.Operation);
                File.Create(config).Close();
                StreamWriter writer = new(config, true, Encoding.UTF8);
                WriteConfig(writer, targets, modules, installForm);
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
