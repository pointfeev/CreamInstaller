using CreamInstaller.Resources;

using Newtonsoft.Json;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CreamInstaller.Utility;

internal static class ProgramData
{
    internal static readonly string DirectoryPathOld = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"\CreamInstaller";
    internal static readonly string DirectoryPath = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData) + @"\CreamInstaller";

    internal static readonly string AppInfoPath = DirectoryPath + @"\appinfo";
    internal static readonly string AppInfoVersionPath = AppInfoPath + @"\version.txt";

    internal static readonly Version MinimumAppInfoVersion = Version.Parse("3.2.0.0");

    internal static readonly string CooldownPath = DirectoryPath + @"\cooldown";

    internal static readonly string OldChoicesPath = DirectoryPath + @"\choices.txt";
    internal static readonly string ChoicesPath = DirectoryPath + @"\choices.json";

    internal static readonly string KoaloaderProxyChoicesPath = DirectoryPath + @"\proxies.json";

    internal static async Task Setup() => await Task.Run(() =>
    {
        if (Directory.Exists(DirectoryPathOld))
        {
            if (Directory.Exists(DirectoryPath)) Directory.Delete(DirectoryPath, true);
            Directory.Move(DirectoryPathOld, DirectoryPath);
        }
        if (!Directory.Exists(DirectoryPath)) _ = Directory.CreateDirectory(DirectoryPath);
        if (!File.Exists(AppInfoVersionPath) || !Version.TryParse(File.ReadAllText(AppInfoVersionPath, Encoding.UTF8), out Version version) || version < MinimumAppInfoVersion)
        {
            if (Directory.Exists(AppInfoPath)) Directory.Delete(AppInfoPath, true);
            _ = Directory.CreateDirectory(AppInfoPath);
            File.WriteAllText(AppInfoVersionPath, Program.Version, Encoding.UTF8);
        }
        if (!Directory.Exists(CooldownPath))
            _ = Directory.CreateDirectory(CooldownPath);
        if (File.Exists(OldChoicesPath))
            File.Delete(OldChoicesPath);
    });

    internal static bool CheckCooldown(string identifier, int cooldown)
    {
        DateTime now = DateTime.UtcNow;
        DateTime lastCheck = GetCooldown(identifier) ?? now;
        bool cooldownOver = (now - lastCheck).TotalSeconds > cooldown;
        if (cooldownOver || now == lastCheck)
            SetCooldown(identifier, now);
        return cooldownOver;
    }
    private static DateTime? GetCooldown(string identifier)
    {
        if (Directory.Exists(CooldownPath))
        {
            string cooldownFile = CooldownPath + @$"\{identifier}.txt";
            if (File.Exists(cooldownFile))
            {
                try
                {
                    if (DateTime.TryParse(File.ReadAllText(cooldownFile), out DateTime cooldown))
                        return cooldown;
                }
                catch { }
            }
        }
        return null;
    }
    private static void SetCooldown(string identifier, DateTime time)
    {
        if (!Directory.Exists(CooldownPath))
            _ = Directory.CreateDirectory(CooldownPath);
        string cooldownFile = CooldownPath + @$"\{identifier}.txt";
        try
        {
            File.WriteAllText(cooldownFile, time.ToString());
        }
        catch { }
    }

    internal static List<(Platform platform, string id)> ReadChoices()
    {
        if (!File.Exists(ChoicesPath)) return null;
        try
        {
            return JsonConvert.DeserializeObject(File.ReadAllText(ChoicesPath),
                typeof(List<(Platform platform, string id)>)) as List<(Platform platform, string id)>;
        }
        catch
        {
            return new();
        }
    }
    internal static void WriteChoices(List<(Platform platform, string id)> choices)
    {
        try
        {
            File.WriteAllText(ChoicesPath, JsonConvert.SerializeObject(choices));
        }
        catch { }
    }

    internal static List<(Platform platform, string id, string proxy)> ReadKoaloaderProxyChoices()
    {
        if (!File.Exists(KoaloaderProxyChoicesPath)) return null;
        try
        {
            return JsonConvert.DeserializeObject(File.ReadAllText(KoaloaderProxyChoicesPath),
                typeof(List<(Platform platform, string id, string proxy)>)) as List<(Platform platform, string id, string proxy)>;
        }
        catch
        {
            return new();
        }
    }

    internal static void WriteKoaloaderProxyChoices(List<(Platform platform, string id, string proxy)> choices)
    {
        try
        {
            File.WriteAllText(KoaloaderProxyChoicesPath, JsonConvert.SerializeObject(choices));
        }
        catch { }
    }

    internal static void UpdateKoaloaderProxyChoices()
    {
        string defaultProxy = "version";
        List<(Platform platform, string id, string proxy)> choices = ReadKoaloaderProxyChoices() ?? new();
        foreach ((Platform platform, string id, string proxy) choice in choices.ToList())
            if (ProgramSelection.FromPlatformId(choice.platform, choice.id) is ProgramSelection selection)
            {
                string proxy = choice.proxy;
                if (proxy.Contains('.')) // convert pre-v4.1.0.0 choices
                    proxy.GetProxyInfoFromIdentifier(out proxy, out _);
                if (selection.KoaloaderProxy is null)
                    selection.KoaloaderProxy = proxy;
                else if (selection.KoaloaderProxy != proxy && choices.Remove(choice))
                    choices.Add((selection.Platform, selection.Id, selection.KoaloaderProxy));
            }
        foreach (ProgramSelection selection in ProgramSelection.AllSafe)
            if (selection.KoaloaderProxy is null)
            {
                selection.KoaloaderProxy = defaultProxy;
                choices.Add((selection.Platform, selection.Id, selection.KoaloaderProxy));
            }
        if (choices.Any())
            WriteKoaloaderProxyChoices(choices);
    }

    internal static void ResetKoaloaderProxyChoices()
    {
        if (File.Exists(KoaloaderProxyChoicesPath))
            File.Delete(KoaloaderProxyChoicesPath);
        UpdateKoaloaderProxyChoices();
    }
}
