using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CreamInstaller.Components;
using CreamInstaller.Forms;
using CreamInstaller.Utility;
using static CreamInstaller.Resources.Resources;

namespace CreamInstaller.Resources;

internal static class Koaloader
{
    internal static readonly List<(string unlocker, string dll)> AutoLoadDLLs = new()
    {
        ("Koaloader", "Unlocker.dll"), ("Koaloader", "Unlocker32.dll"), ("Koaloader", "Unlocker64.dll"), ("Lyptus", "Lyptus.dll"),
        ("Lyptus", "Lyptus32.dll"), ("Lyptus", "Lyptus64.dll"), ("SmokeAPI", "SmokeAPI.dll"), ("SmokeAPI", "SmokeAPI32.dll"),
        ("SmokeAPI", "SmokeAPI64.dll"), ("ScreamAPI", "ScreamAPI.dll"), ("ScreamAPI", "ScreamAPI32.dll"), ("ScreamAPI", "ScreamAPI64.dll"),
        ("Uplay R1 Unlocker", "UplayR1Unlocker.dll"), ("Uplay R1 Unlocker", "UplayR1Unlocker32.dll"), ("Uplay R1 Unlocker", "UplayR1Unlocker64.dll"),
        ("Uplay R2 Unlocker", "UplayR2Unlocker.dll"), ("Uplay R2 Unlocker", "UplayR2Unlocker32.dll"), ("Uplay R2 Unlocker", "UplayR2Unlocker64.dll")
    };

    internal static IEnumerable<string> GetKoaloaderProxies(this string directory)
        => from resource in EmbeddedResources
           select resource[(resource.IndexOf('.') + 1)..]
           into resource
           select resource[(resource.IndexOf('.') + 1)..]
           into resource
           select directory + @"\" + resource;

    internal static void GetKoaloaderComponents(this string directory, out string old_config, out string config)
    {
        old_config = directory + @"\Koaloader.json";
        config = directory + @"\Koaloader.config.json";
    }

    private static void WriteProxy(this string path, string proxyName, BinaryType binaryType)
    {
        foreach (string resourceIdentifier in EmbeddedResources.FindAll(r => r.StartsWith("Koaloader")))
        {
            resourceIdentifier.GetProxyInfoFromIdentifier(out string _proxyName, out BinaryType _binaryType);
            if (_proxyName != proxyName || _binaryType != binaryType)
                continue;
            resourceIdentifier.Write(path);
            break;
        }
    }

    internal static void GetProxyInfoFromIdentifier(this string resourceIdentifier, out string proxyName, out BinaryType binaryType)
    {
        string baseIdentifier = resourceIdentifier[(resourceIdentifier.IndexOf('.') + 1)..];
        baseIdentifier = baseIdentifier[..baseIdentifier.IndexOf('.')];
        proxyName = baseIdentifier[..baseIdentifier.LastIndexOf('_')];
        string bitness = baseIdentifier[(baseIdentifier.LastIndexOf('_') + 1)..];
        binaryType = bitness switch { "32" => BinaryType.BIT32, "64" => BinaryType.BIT64, _ => BinaryType.Unknown };
    }

    private static void CheckConfig(string directory, InstallForm installForm = null)
    {
        directory.GetKoaloaderComponents(out string old_config, out string config);
        if (File.Exists(old_config))
        {
            if (!File.Exists(config))
            {
                File.Move(old_config, config!);
                installForm?.UpdateUser($"Converted old configuration: {Path.GetFileName(old_config)} -> {Path.GetFileName(config)}", LogTextBox.Action, false);
            }
            else
            {
                File.Delete(old_config);
                installForm?.UpdateUser($"Deleted old configuration: {Path.GetFileName(old_config)}", LogTextBox.Action, false);
            }
        }
        SortedList<string, string> targets = new(PlatformIdComparer.String);
        SortedList<string, string> modules = new(PlatformIdComparer.String);
        if (targets.Any() || modules.Any())
        {
            /*if (installForm is not null)
                installForm.UpdateUser("Generating Koaloader configuration for " + selection.Name + $" in directory \"{directory}\" . . . ", LogTextBox.Operation);*/
            File.Create(config).Close();
            StreamWriter writer = new(config, true, Encoding.UTF8);
            WriteConfig(writer, targets, modules, installForm);
            writer.Flush();
            writer.Close();
        }
        else if (File.Exists(config))
        {
            File.Delete(config);
            installForm?.UpdateUser($"Deleted unnecessary configuration: {Path.GetFileName(config)}", LogTextBox.Action, false);
        }
    }

    private static void WriteConfig(StreamWriter writer, SortedList<string, string> targets, SortedList<string, string> modules, InstallForm installForm = null)
    {
        writer.WriteLine("{");
        writer.WriteLine("  \"logging\": false,");
        writer.WriteLine("  \"enabled\": true,");
        writer.WriteLine("  \"auto_load\": " + (modules.Any() ? "false" : "true") + ",");
        if (targets.Any())
        {
            writer.WriteLine("  \"targets\": [");
            KeyValuePair<string, string> lastTarget = targets.Last();
            foreach (KeyValuePair<string, string> pair in targets)
            {
                string path = pair.Value;
                writer.WriteLine($"      \"{path}\"{(pair.Equals(lastTarget) ? "" : ",")}");
                installForm?.UpdateUser($"Added target to Koaloader.json with path {path}", LogTextBox.Action, false);
            }
            writer.WriteLine("  ]");
        }
        else
            writer.WriteLine("  \"targets\": []");
        if (modules.Any())
        {
            writer.WriteLine("  \"modules\": [");
            KeyValuePair<string, string> lastModule = modules.Last();
            foreach (KeyValuePair<string, string> pair in modules)
            {
                string path = pair.Value;
                writer.WriteLine("    {");
                writer.WriteLine("      \"path\": \"" + path + "\",");
                writer.WriteLine("      \"required\": true");
                writer.WriteLine("    }" + (pair.Equals(lastModule) ? "" : ","));
                installForm?.UpdateUser($"Added module to Koaloader.json with path {path}", LogTextBox.Action, false);
            }
            writer.WriteLine("  ]");
        }
        else
            writer.WriteLine("  \"modules\": []");
        writer.WriteLine("}");
    }

    internal static async Task Uninstall(string directory, string rootDirectory = null, InstallForm installForm = null, bool deleteConfig = true)
        => await Task.Run(async () =>
        {
            directory.GetKoaloaderComponents(out string old_config, out string config);
            foreach (string proxyPath in directory.GetKoaloaderProxies()
                                                  .Where(proxyPath => File.Exists(proxyPath) && proxyPath.IsResourceFile(ResourceIdentifier.Koaloader)))
            {
                File.Delete(proxyPath);
                installForm?.UpdateUser($"Deleted Koaloader: {Path.GetFileName(proxyPath)}", LogTextBox.Action, false);
            }
            foreach ((string unlocker, string path) in AutoLoadDLLs.Select(pair => (pair.unlocker, path: directory + @"\" + pair.dll))
                                                                   .Where(pair => File.Exists(pair.path) && pair.path.IsResourceFile()))
            {
                File.Delete(path);
                installForm?.UpdateUser($"Deleted {unlocker}: {Path.GetFileName(path)}", LogTextBox.Action, false);
            }
            if (deleteConfig && File.Exists(old_config))
            {
                File.Delete(old_config);
                installForm?.UpdateUser($"Deleted configuration: {Path.GetFileName(old_config)}", LogTextBox.Action, false);
            }
            if (deleteConfig && File.Exists(config))
            {
                File.Delete(config);
                installForm?.UpdateUser($"Deleted configuration: {Path.GetFileName(config)}", LogTextBox.Action, false);
            }
            await SmokeAPI.Uninstall(directory, installForm, deleteConfig);
            await ScreamAPI.Uninstall(directory, installForm, deleteConfig);
            await UplayR1.Uninstall(directory, installForm, deleteConfig);
            await UplayR2.Uninstall(directory, installForm, deleteConfig);
            if (rootDirectory is not null && directory != rootDirectory)
                await Uninstall(rootDirectory, null, installForm, deleteConfig);
        });

    internal static async Task Install(string directory, BinaryType binaryType, ProgramSelection selection, string rootDirectory = null,
        InstallForm installForm = null, bool generateConfig = true)
        => await Task.Run(() =>
        {
            string proxy = selection.KoaloaderProxy ?? ProgramSelection.DefaultKoaloaderProxy;
            string path = directory + @"\" + proxy + ".dll";
            foreach (string _path in directory.GetKoaloaderProxies().Where(p => p != path && File.Exists(p) && p.IsResourceFile(ResourceIdentifier.Koaloader)))
            {
                File.Delete(_path);
                installForm?.UpdateUser($"Deleted Koaloader: {Path.GetFileName(_path)}", LogTextBox.Action, false);
            }
            if (File.Exists(path) && !path.IsResourceFile(ResourceIdentifier.Koaloader))
                throw new CustomMessageException("A non-Koaloader DLL named " + proxy + ".dll already exists in this directory!");
            path.WriteProxy(proxy, binaryType);
            installForm?.UpdateUser($"Wrote {(binaryType == BinaryType.BIT32 ? "32-bit" : "64-bit")} Koaloader: {Path.GetFileName(path)}", LogTextBox.Action,
                false);
            bool bit32 = false, bit64 = false;
            foreach (string executable in Directory.EnumerateFiles(directory, "*.exe"))
                if (executable.TryGetFileBinaryType(out BinaryType binaryType))
                {
                    switch (binaryType)
                    {
                        case BinaryType.BIT32:
                            bit32 = true;
                            break;
                        case BinaryType.BIT64:
                            bit64 = true;
                            break;
                    }
                    if (bit32 && bit64)
                        break;
                }
            if (selection.Platform is Platform.Steam or Platform.Paradox)
            {
                if (bit32)
                {
                    path = directory + @"\SmokeAPI32.dll";
                    if (rootDirectory is not null && directory != rootDirectory)
                    {
                        if (File.Exists(path))
                        {
                            File.Delete(path);
                            installForm?.UpdateUser($"Deleted SmokeAPI from non-root directory: {Path.GetFileName(path)}", LogTextBox.Action, false);
                        }
                        path = rootDirectory + @"\SmokeAPI32.dll";
                    }
                    "SmokeAPI.steam_api.dll".Write(path);
                    installForm?.UpdateUser(
                        $"Wrote SmokeAPI{(rootDirectory is not null && directory != rootDirectory ? " to root directory" : "")}: {Path.GetFileName(path)}",
                        LogTextBox.Action, false);
                }
                if (bit64)
                {
                    path = directory + @"\SmokeAPI64.dll";
                    if (rootDirectory is not null && directory != rootDirectory)
                    {
                        if (File.Exists(path))
                        {
                            File.Delete(path);
                            installForm?.UpdateUser($"Deleted SmokeAPI from non-root directory: {Path.GetFileName(path)}", LogTextBox.Action, false);
                        }
                        path = rootDirectory + @"\SmokeAPI64.dll";
                    }
                    "SmokeAPI.steam_api64.dll".Write(path);
                    installForm?.UpdateUser(
                        $"Wrote SmokeAPI{(rootDirectory is not null && directory != rootDirectory ? " to root directory" : "")}: {Path.GetFileName(path)}",
                        LogTextBox.Action, false);
                }
                SmokeAPI.CheckConfig(rootDirectory ?? directory, selection, installForm);
            }
            switch (selection.Platform)
            {
                case Platform.Epic or Platform.Paradox:
                {
                    if (bit32)
                    {
                        path = directory + @"\ScreamAPI32.dll";
                        if (rootDirectory is not null && directory != rootDirectory)
                        {
                            if (File.Exists(path))
                            {
                                File.Delete(path);
                                installForm?.UpdateUser($"Deleted ScreamAPI from non-root directory: {Path.GetFileName(path)}", LogTextBox.Action, false);
                            }
                            path = rootDirectory + @"\ScreamAPI32.dll";
                        }
                        "ScreamAPI.EOSSDK-Win32-Shipping.dll".Write(path);
                        installForm?.UpdateUser(
                            $"Wrote ScreamAPI{(rootDirectory is not null && directory != rootDirectory ? " to root directory" : "")}: {Path.GetFileName(path)}",
                            LogTextBox.Action, false);
                    }
                    if (bit64)
                    {
                        path = directory + @"\ScreamAPI64.dll";
                        if (rootDirectory is not null && directory != rootDirectory)
                        {
                            if (File.Exists(path))
                            {
                                File.Delete(path);
                                installForm?.UpdateUser($"Deleted ScreamAPI from non-root directory: {Path.GetFileName(path)}", LogTextBox.Action, false);
                            }
                            path = rootDirectory + @"\ScreamAPI64.dll";
                        }
                        "ScreamAPI.EOSSDK-Win64-Shipping.dll".Write(path);
                        installForm?.UpdateUser(
                            $"Wrote ScreamAPI{(rootDirectory is not null && directory != rootDirectory ? " to root directory" : "")}: {Path.GetFileName(path)}",
                            LogTextBox.Action, false);
                    }
                    ScreamAPI.CheckConfig(rootDirectory ?? directory, selection, installForm);
                    break;
                }
                case Platform.Ubisoft:
                {
                    if (bit32)
                    {
                        path = directory + @"\UplayR1Unlocker32.dll";
                        if (rootDirectory is not null && directory != rootDirectory)
                        {
                            if (File.Exists(path))
                            {
                                File.Delete(path);
                                installForm?.UpdateUser($"Deleted Uplay R1 Unlocker from non-root directory: {Path.GetFileName(path)}", LogTextBox.Action,
                                    false);
                            }
                            path = rootDirectory + @"\UplayR1Unlocker32.dll";
                        }
                        "UplayR1.uplay_r1_loader.dll".Write(path);
                        installForm?.UpdateUser(
                            $"Wrote Uplay R1 Unlocker{(rootDirectory is not null && directory != rootDirectory ? " to root directory" : "")}: {Path.GetFileName(path)}",
                            LogTextBox.Action, false);
                    }
                    if (bit64)
                    {
                        path = directory + @"\UplayR1Unlocker64.dll";
                        if (rootDirectory is not null && directory != rootDirectory)
                        {
                            if (File.Exists(path))
                            {
                                File.Delete(path);
                                installForm?.UpdateUser($"Deleted Uplay R1 Unlocker from non-root directory: {Path.GetFileName(path)}", LogTextBox.Action,
                                    false);
                            }
                            path = rootDirectory + @"\UplayR1Unlocker64.dll";
                        }
                        "UplayR1.uplay_r1_loader64.dll".Write(path);
                        installForm?.UpdateUser(
                            $"Wrote Uplay R1 Unlocker{(rootDirectory is not null && directory != rootDirectory ? " to root directory" : "")}: {Path.GetFileName(path)}",
                            LogTextBox.Action, false);
                    }
                    UplayR1.CheckConfig(rootDirectory ?? directory, selection, installForm);
                    if (bit32)
                    {
                        path = directory + @"\UplayR2Unlocker32.dll";
                        if (rootDirectory is not null && directory != rootDirectory)
                        {
                            if (File.Exists(path))
                            {
                                File.Delete(path);
                                installForm?.UpdateUser($"Deleted Uplay R2 Unlocker from non-root directory: {Path.GetFileName(path)}", LogTextBox.Action,
                                    false);
                            }
                            path = rootDirectory + @"\UplayR2Unlocker32.dll";
                        }
                        "UplayR2.upc_r2_loader.dll".Write(path);
                        installForm?.UpdateUser(
                            $"Wrote Uplay R2 Unlocker{(rootDirectory is not null && directory != rootDirectory ? " to root directory" : "")}: {Path.GetFileName(path)}",
                            LogTextBox.Action, false);
                    }
                    if (bit64)
                    {
                        path = directory + @"\UplayR2Unlocker64.dll";
                        if (rootDirectory is not null && directory != rootDirectory)
                        {
                            if (File.Exists(path))
                            {
                                File.Delete(path);
                                installForm?.UpdateUser($"Deleted Uplay R2 Unlocker from non-root directory: {Path.GetFileName(path)}", LogTextBox.Action,
                                    false);
                            }
                            path = rootDirectory + @"\UplayR2Unlocker64.dll";
                        }
                        "UplayR2.upc_r2_loader64.dll".Write(path);
                        installForm?.UpdateUser(
                            $"Wrote Uplay R2 Unlocker{(rootDirectory is not null && directory != rootDirectory ? " to root directory" : "")}: {Path.GetFileName(path)}",
                            LogTextBox.Action, false);
                    }
                    UplayR2.CheckConfig(rootDirectory ?? directory, selection, installForm);
                    break;
                }
            }
            if (generateConfig)
                CheckConfig(directory, installForm);
        });
}