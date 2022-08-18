using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;

namespace CreamInstaller.Resources;

internal static class Resources
{
    internal static void GetCreamApiComponents(
            this string directory,
            out string api32, out string api32_o,
            out string api64, out string api64_o,
            out string config)
    {
        api32 = directory + @"\steam_api.dll";
        api32_o = directory + @"\steam_api_o.dll";
        api64 = directory + @"\steam_api64.dll";
        api64_o = directory + @"\steam_api64_o.dll";
        config = directory + @"\cream_api.ini";
    }

    public enum ResourceIdentifier
    {
        Steamworks32 = 0,
        Steamworks64 = 1,
        EpicOnlineServices32 = 2,
        EpicOnlineServices64 = 3,
        Uplay32 = 4,
        Uplay64 = 5,
        Upc32 = 6,
        Upc64 = 7,
    }

    internal static readonly Dictionary<ResourceIdentifier, IReadOnlyList<string>> ResourceMD5s = new()
    {
        {
            ResourceIdentifier.EpicOnlineServices32,
            new List<string>()
            {
                "069A57B1834A960193D2AD6B96926D70", // ScreamAPI v3.0.0
                "E2FB3A4A9583FDC215832E5F935E4440"  // ScreamAPI v3.0.1
            }
        },
        {
            ResourceIdentifier.EpicOnlineServices64,
            new List<string>()
            {
                "0D62E57139F1A64F807A9934946A9474", // ScreamAPI v3.0.0
                "3875C7B735EE80C23239CC4749FDCBE6"  // ScreamAPI v3.0.1
            }
        },
        {
            ResourceIdentifier.Steamworks32,
            new List<string>()
            {
                "02594110FE56B2945955D46670B9A094", // CreamAPI v4.5.0.0 Hotfix
                "B2434578957CBE38BDCE0A671C1262FC", // SmokeAPI v1.0.0
                "973AB1632B747D4BF3B2666F32E34327", // SmokeAPI v1.0.1
                "C7E41F569FC6A347D67D2BFB2BD10F25", // SmokeAPI v1.0.2
                "F9E7D5B248B86D1C2F2F2905A9F37755"  // SmokeAPI v1.0.3
            }
        },
        {
            ResourceIdentifier.Steamworks64,
            new List<string>()
            {
                "30091B91923D9583A54A93ED1145554B", // CreamAPI v4.5.0.0 Hotfix
                "08713035CAD6F52548FF324D0487B88D", // SmokeAPI v1.0.0
                "D077737B9979D32458AC938A2978FA3C", // SmokeAPI v1.0.1
                "49122A2E2E51CBB0AE5E1D59B280E4CD", // SmokeAPI v1.0.2
                "13F3E9476116F7670E21365A400357AC"  // SmokeAPI v1.0.3
            }
        },
        {
            ResourceIdentifier.Uplay32,
            new List<string>()
            {
                "1977967B2549A38EC2DB39D4C8ED499B" // Uplay R1 Unlocker v2.0.0
            }
        },
        {
            ResourceIdentifier.Uplay64,
            new List<string>()
            {
                "333FEDD9DC2B299419B37ED1624FF8DB" // Uplay R1 Unlocker v2.0.0
            }
        },
        {
            ResourceIdentifier.Upc32,
            new List<string>()
            {
                "C14368BC4EE19FDE8DBAC07E31C67AE4", // Uplay R2 Unlocker v3.0.0
                "DED3A3EA1876E3110D7D87B9A22946B0"  // Uplay R2 Unlocker v3.0.1
            }
        },
        {
            ResourceIdentifier.Upc64,
            new List<string>()
            {
                "7D9A4C12972BAABCB6C181920CC0F19B", // Uplay R2 Unlocker v3.0.0
                "D7FDBFE0FC8D7600FEB8EC0A97713184"  // Uplay R2 Unlocker v3.0.1
            }
        }
    };

    internal static string ComputeMD5(this string filePath)
    {
        if (!File.Exists(filePath)) return null;
#pragma warning disable CA5351 // Do Not Use Broken Cryptographic Algorithms
        using MD5 md5 = MD5.Create();
#pragma warning restore CA5351 // Do Not Use Broken Cryptographic Algorithms
        using FileStream stream = File.OpenRead(filePath);
        byte[] hash = md5.ComputeHash(stream);
        return BitConverter.ToString(hash).Replace("-", "").ToUpperInvariant();
    }

    internal static bool IsResourceFile(this string filePath, ResourceIdentifier identifier) => filePath.ComputeMD5() is string hash && ResourceMD5s[identifier].Contains(hash);

    internal static bool IsResourceFile(this string filePath) => filePath.ComputeMD5() is string hash && ResourceMD5s.Values.Any(hashes => hashes.Contains(hash));

    internal static bool IsFilePathLocked(this string filePath)
    {
        try
        {
            File.Open(filePath, FileMode.Open, FileAccess.ReadWrite, FileShare.None).Close();
        }
        catch (FileNotFoundException)
        {
            return false;
        }
        catch (IOException)
        {
            return true;
        }
        return false;
    }

    internal static void Write(this byte[] resource, string filePath)
    {
        using FileStream file = new(filePath, FileMode.Create, FileAccess.Write);
        file.Write(resource);
    }
}
