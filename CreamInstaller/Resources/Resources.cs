using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Threading.Tasks;
using System.Windows.Forms;
using CreamInstaller.Utility;

namespace CreamInstaller.Resources;

internal static class Resources
{
    private static HashSet<string> embeddedResources;

    private static readonly Dictionary<ResourceIdentifier, HashSet<string>> ResourceMD5s = new()
    {
        [ResourceIdentifier.Koaloader] = Koaloader.ResourceMD5s[ResourceIdentifier.Koaloader],
        [ResourceIdentifier.EpicOnlineServices32] = ScreamAPI.ResourceMD5s[ResourceIdentifier.EpicOnlineServices32],
        [ResourceIdentifier.EpicOnlineServices64] = ScreamAPI.ResourceMD5s[ResourceIdentifier.EpicOnlineServices64],
        [ResourceIdentifier.Steamworks32] = CreamAPI.ResourceMD5s[ResourceIdentifier.Steamworks32]
            .Union(SmokeAPI.ResourceMD5s[ResourceIdentifier.Steamworks32]).ToHashSet(),
        [ResourceIdentifier.Steamworks64] = CreamAPI.ResourceMD5s[ResourceIdentifier.Steamworks64]
            .Union(SmokeAPI.ResourceMD5s[ResourceIdentifier.Steamworks64]).ToHashSet(),
        [ResourceIdentifier.Uplay32] = UplayR1.ResourceMD5s[ResourceIdentifier.Uplay32],
        [ResourceIdentifier.Uplay64] = UplayR1.ResourceMD5s[ResourceIdentifier.Uplay64],
        [ResourceIdentifier.Upc32] = UplayR2.ResourceMD5s[ResourceIdentifier.Upc32],
        [ResourceIdentifier.Upc64] = UplayR2.ResourceMD5s[ResourceIdentifier.Upc64]
    };

    internal static HashSet<string> EmbeddedResources
    {
        get
        {
            if (embeddedResources is not null)
                return embeddedResources;
            string[] names = Assembly.GetExecutingAssembly().GetManifestResourceNames();
            embeddedResources = [];
            foreach (string resourceName in names.Where(n =>
                         n.StartsWith("CreamInstaller.Resources.", StringComparison.Ordinal)))
                _ = embeddedResources.Add(resourceName[25..]);
            return embeddedResources;
        }
    }

    internal static void WriteManifestResource(this string resourceIdentifier, string filePath)
    {
        while (!Program.Canceled)
            try
            {
                using Stream resource = Assembly.GetExecutingAssembly()
                    .GetManifestResourceStream("CreamInstaller.Resources." + resourceIdentifier);
                using FileStream file = new(filePath, FileMode.Create, FileAccess.Write);
                resource?.CopyTo(file);
                break;
            }
            catch (Exception e)
            {
                if (filePath.IOWarn("Failed to write a crucial manifest resource (" + resourceIdentifier + ")", e) is
                    not DialogResult.OK)
                    break;
            }
    }

    internal static bool WriteResource(this byte[] resource, string filePath)
    {
        while (!Program.Canceled)
            try
            {
                using FileStream fileStream = new(filePath, FileMode.Create, FileAccess.Write);
                fileStream.Write(resource);
                return true;
            }
            catch (Exception e)
            {
                if (filePath.IOWarn("Failed to write a crucial resource", e) is not DialogResult.OK)
                    break;
            }

        return false;
    }

    internal static bool TryGetFileBinaryType(this string path, out BinaryType binaryType) =>
        NativeImports.GetBinaryType(path, out binaryType);

    internal static async Task<List<(string directory, BinaryType binaryType)>> GetExecutableDirectories(
        this string rootDirectory, bool filterCommon = false,
        Func<string, bool> validFunc = null)
        => await Task.Run(async ()
            => (await rootDirectory.GetExecutables(filterCommon, validFunc)
                ?? (filterCommon || validFunc is not null ? await rootDirectory.GetExecutables() : null))?.Select(e =>
            {
                e.path = Path.GetDirectoryName(e.path);
                return e;
            }).DistinctBy(e => e.path).ToList() ?? []);

    internal static async Task<List<(string path, BinaryType binaryType)>> GetExecutables(this string rootDirectory,
        bool filterCommon = false,
        Func<string, bool> validFunc = null)
        => await Task.Run(() =>
        {
            List<(string path, BinaryType binaryType)> executables = [];
            if (Program.Canceled || !rootDirectory.DirectoryExists())
                return null;
            foreach (string path in rootDirectory.EnumerateDirectory("*.exe", true))
            {
                if (Program.Canceled)
                    return null;
                if (executables.All(e => e.path != path) && (!filterCommon ||
                                                             !rootDirectory.IsCommonIncorrectExecutable(path))
                                                         && (validFunc is null || validFunc(path)) &&
                                                         path.TryGetFileBinaryType(out BinaryType binaryType) &&
                                                         binaryType is BinaryType.BIT64)
                    executables.Add((path, binaryType));
            }

            foreach (string path in rootDirectory.EnumerateDirectory("*.exe", true))
            {
                if (Program.Canceled)
                    return null;
                if (executables.All(e => e.path != path) && (!filterCommon ||
                                                             !rootDirectory.IsCommonIncorrectExecutable(path))
                                                         && (validFunc is null || validFunc(path)) &&
                                                         path.TryGetFileBinaryType(out BinaryType binaryType) &&
                                                         binaryType is BinaryType.BIT32)
                    executables.Add((path, binaryType));
            }

            return executables.Count > 0 ? executables : null;
        });

    private static bool IsCommonIncorrectExecutable(this string rootDirectory, string path)
    {
        string subPath = path[rootDirectory.Length..].ToUpperInvariant();
        return subPath.Contains("SETUP") || subPath.Contains("REDIST") || subPath.Contains("SUPPORT")
               || subPath.Contains("CRASH") && (subPath.Contains("PAD") || subPath.Contains("REPORT")) ||
               subPath.Contains("HELPER") || subPath.Contains("CEFPROCESS")
               || subPath.Contains("ZFGAMEBROWSER") || subPath.Contains("MONO") || subPath.Contains("PLUGINS") ||
               subPath.Contains("MODDING")
               || subPath.Contains("MOD") && subPath.Contains("MANAGER") || subPath.Contains("BATTLEYE") ||
               subPath.Contains("ANTICHEAT");
    }

    internal static async Task<HashSet<string>> GetDllDirectoriesFromGameDirectory(this string gameDirectory,
        Platform platform)
        => await Task.Run(() =>
        {
            HashSet<string> dllDirectories = [];
            if (Program.Canceled || !gameDirectory.DirectoryExists())
                return null;
            foreach (string directory in gameDirectory.EnumerateSubdirectories("*", true).Append(gameDirectory))
            {
                if (Program.Canceled)
                    return null;
                string subDirectory = directory.ResolvePath();
                if (subDirectory is null || dllDirectories.Contains(subDirectory))
                    continue;
                bool koaloaderInstalled = Koaloader.AutoLoadDLLs
                    .Select(pair => (pair.unlocker, path: directory + @"\" + pair.dll))
                    .Any(pair => pair.path.FileExists() && pair.path.IsResourceFile());
                if (platform is Platform.Steam or Platform.Paradox)
                {
                    subDirectory.GetSmokeApiComponents(out string api, out string api_o, out string api64,
                        out string api64_o, out string old_config,
                        out string config, out string old_log, out string log, out string cache);
                    if (api.FileExists() || api_o.FileExists() || api64.FileExists() || api64_o.FileExists()
                        || (old_config.FileExists() || config.FileExists() || old_log.FileExists() ||
                            log.FileExists() || cache.FileExists())
                        && !koaloaderInstalled)
                        _ = dllDirectories.Add(subDirectory);
                }

                if (platform is Platform.Epic or Platform.Paradox)
                {
                    subDirectory.GetScreamApiComponents(out string api32, out string api32_o, out string api64,
                        out string api64_o, out string config,
                        out string log);
                    if (api32.FileExists() || api32_o.FileExists() || api64.FileExists() || api64_o.FileExists()
                        || (config.FileExists() || log.FileExists()) && !koaloaderInstalled)
                        _ = dllDirectories.Add(subDirectory);
                }

                if (platform is Platform.Ubisoft)
                {
                    subDirectory.GetUplayR1Components(out string api32, out string api32_o, out string api64,
                        out string api64_o, out string config,
                        out string log);
                    if (api32.FileExists() || api32_o.FileExists() || api64.FileExists() || api64_o.FileExists()
                        || (config.FileExists() || log.FileExists()) && !koaloaderInstalled)
                        _ = dllDirectories.Add(subDirectory);
                    subDirectory.GetUplayR2Components(out string old_api32, out string old_api64, out api32,
                        out api32_o, out api64, out api64_o, out config,
                        out log);
                    if (old_api32.FileExists() || old_api64.FileExists() || api32.FileExists() ||
                        api32_o.FileExists() || api64.FileExists()
                        || api64_o.FileExists() || (config.FileExists() || log.FileExists()) && !koaloaderInstalled)
                        _ = dllDirectories.Add(subDirectory);
                }
            }

            return dllDirectories.Count > 0 ? dllDirectories : null;
        });

#pragma warning disable CA5351
    private static string ComputeMD5(this string filePath)
        => filePath.FileExists() && filePath.ReadFileBytes(true) is { } bytes
            ? BitConverter.ToString(MD5.HashData(bytes)).Replace("-", "").ToUpperInvariant()
            : null;
#pragma warning restore CA5351

    internal static bool IsResourceFile(this string filePath, ResourceIdentifier identifier)
        => filePath.ComputeMD5() is { } hash && ResourceMD5s[identifier].Contains(hash);

    internal static bool IsResourceFile(this string filePath) => filePath.ComputeMD5() is { } hash &&
                                                                 ResourceMD5s.Values.Any(
                                                                     hashes => hashes.Contains(hash));

    internal enum BinaryType
    {
        Unknown = -1,
        BIT32 = 0,
        BIT64 = 6
    }

    internal enum ResourceIdentifier
    {
        Koaloader,
        Steamworks32,
        Steamworks64,
        EpicOnlineServices32,
        EpicOnlineServices64,
        Uplay32,
        Uplay64,
        Upc32,
        Upc64
    }
}