using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;

namespace CreamInstaller.Resources;

internal static class Resources
{
    internal static void Write(this byte[] resource, string filePath)
    {
        using FileStream file = new(filePath, FileMode.Create, FileAccess.Write);
        file.Write(resource);
    }

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

    internal static void GetCreamApiComponents(this string directory, out string sdk32, out string sdk32_o, out string sdk64, out string sdk64_o, out string config)
    {
        sdk32 = directory + @"\steam_api.dll";
        sdk32_o = directory + @"\steam_api_o.dll";
        sdk64 = directory + @"\steam_api64.dll";
        sdk64_o = directory + @"\steam_api64_o.dll";
        config = directory + @"\cream_api.ini";
    }

    internal static void GetSmokeApiComponents(this string directory, out string sdk32, out string sdk32_o, out string sdk64, out string sdk64_o, out string config)
    {
        sdk32 = directory + @"\steam_api.dll";
        sdk32_o = directory + @"\steam_api_o.dll";
        sdk64 = directory + @"\steam_api64.dll";
        sdk64_o = directory + @"\steam_api64_o.dll";
        config = directory + @"\SmokeAPI.json";
    }

    internal static void GetScreamApiComponents(this string directory, out string sdk32, out string sdk32_o, out string sdk64, out string sdk64_o, out string config)
    {
        sdk32 = directory + @"\EOSSDK-Win32-Shipping.dll";
        sdk32_o = directory + @"\EOSSDK-Win32-Shipping_o.dll";
        sdk64 = directory + @"\EOSSDK-Win64-Shipping.dll";
        sdk64_o = directory + @"\EOSSDK-Win64-Shipping_o.dll";
        config = directory + @"\ScreamAPI.json";
    }

    public enum ResourceIdentifier
    {
        Steamworks32 = 0,
        Steamworks64 = 1,
        EpicOnlineServices32 = 2,
        EpicOnlineServices64 = 3
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
        }
    };

    internal static string ComputeMD5(this string filePath)
    {
#pragma warning disable CA5351 // Do Not Use Broken Cryptographic Algorithms
        using MD5 md5 = MD5.Create();
#pragma warning restore CA5351 // Do Not Use Broken Cryptographic Algorithms
        using FileStream stream = File.OpenRead(filePath);
        byte[] hash = md5.ComputeHash(stream);
        return BitConverter.ToString(hash).Replace("-", "").ToUpperInvariant();
    }

    internal static bool IsResourceFile(this string filePath, ResourceIdentifier identifier) => ResourceMD5s[identifier].Contains(filePath.ComputeMD5());

    internal static bool IsResourceFile(this string filePath)
    {
        string hash = filePath.ComputeMD5();
        foreach (IReadOnlyList<string> md5s in ResourceMD5s.Values)
            if (md5s.Contains(hash))
                return true;
        return false;
    }
}
