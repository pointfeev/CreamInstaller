using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace CreamInstaller.Utility;

internal static class ProgramData
{
    private static readonly string DirectoryPathOld = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"\CreamInstaller";

    internal static readonly string DirectoryPath = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData) + @"\CreamInstaller";

    internal static readonly string AppInfoPath = DirectoryPath + @"\appinfo";
    private static readonly string AppInfoVersionPath = AppInfoPath + @"\version.txt";

    private static readonly Version MinimumAppInfoVersion = Version.Parse("3.2.0.0");

    internal static readonly string CooldownPath = DirectoryPath + @"\cooldown";

    private static readonly string OldProgramChoicesPath = DirectoryPath + @"\choices.txt";
    private static readonly string ProgramChoicesPath = DirectoryPath + @"\choices.json";
    private static readonly string DlcChoicesPath = DirectoryPath + @"\dlc.json";
    private static readonly string KoaloaderProxyChoicesPath = DirectoryPath + @"\proxies.json";

    internal static async Task Setup()
        => await Task.Run(() =>
        {
            if (Directory.Exists(DirectoryPathOld))
            {
                if (Directory.Exists(DirectoryPath))
                    Directory.Delete(DirectoryPath, true);
                Directory.Move(DirectoryPathOld, DirectoryPath);
            }
            if (!Directory.Exists(DirectoryPath))
                _ = Directory.CreateDirectory(DirectoryPath);
            if (!File.Exists(AppInfoVersionPath) || !Version.TryParse(File.ReadAllText(AppInfoVersionPath, Encoding.UTF8), out Version version)
                                                 || version < MinimumAppInfoVersion)
            {
                if (Directory.Exists(AppInfoPath))
                    Directory.Delete(AppInfoPath, true);
                _ = Directory.CreateDirectory(AppInfoPath);
                File.WriteAllText(AppInfoVersionPath, Program.Version, Encoding.UTF8);
            }
            if (!Directory.Exists(CooldownPath))
                _ = Directory.CreateDirectory(CooldownPath);
            if (File.Exists(OldProgramChoicesPath))
                File.Delete(OldProgramChoicesPath);
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
        if (!Directory.Exists(CooldownPath))
            return null;
        string cooldownFile = CooldownPath + @$"\{identifier}.txt";
        if (!File.Exists(cooldownFile))
            return null;
        try
        {
            if (DateTime.TryParse(File.ReadAllText(cooldownFile), out DateTime cooldown))
                return cooldown;
        }
        catch
        {
            // ignored
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
            File.WriteAllText(cooldownFile, time.ToString(CultureInfo.InvariantCulture));
        }
        catch
        {
            // ignored
        }
    }

    internal static IEnumerable<(Platform platform, string id)> ReadProgramChoices()
    {
        if (!File.Exists(ProgramChoicesPath))
            return Enumerable.Empty<(Platform platform, string id)>();
        try
        {
            return JsonConvert.DeserializeObject(File.ReadAllText(ProgramChoicesPath), typeof(List<(Platform platform, string id)>)) as
                List<(Platform platform, string id)>;
        }
        catch
        {
            return Enumerable.Empty<(Platform platform, string id)>();
        }
    }

    internal static void WriteProgramChoices(IEnumerable<(Platform platform, string id)> choices)
    {
        try
        {
            if (choices is null || !choices.Any())
                File.Delete(ProgramChoicesPath);
            else
                File.WriteAllText(ProgramChoicesPath, JsonConvert.SerializeObject(choices));
        }
        catch
        {
            // ignored
        }
    }

    internal static IEnumerable<(Platform platform, string gameId, string dlcId)> ReadDlcChoices()
    {
        if (!File.Exists(DlcChoicesPath))
            return Enumerable.Empty<(Platform platform, string gameId, string dlcId)>();
        try
        {
            return JsonConvert.DeserializeObject(File.ReadAllText(DlcChoicesPath), typeof(IEnumerable<(Platform platform, string gameId, string dlcId)>)) as
                IEnumerable<(Platform platform, string gameId, string dlcId)>;
        }
        catch
        {
            return Enumerable.Empty<(Platform platform, string gameId, string dlcId)>();
        }
    }

    internal static void WriteDlcChoices(List<(Platform platform, string gameId, string dlcId)> choices)
    {
        try
        {
            if (choices is null || !choices.Any())
                File.Delete(DlcChoicesPath);
            else
                File.WriteAllText(DlcChoicesPath, JsonConvert.SerializeObject(choices));
        }
        catch
        {
            // ignored
        }
    }

    internal static IEnumerable<(Platform platform, string id, string proxy, bool enabled)> ReadKoaloaderChoices()
    {
        if (!File.Exists(KoaloaderProxyChoicesPath))
            return Enumerable.Empty<(Platform platform, string id, string proxy, bool enabled)>();
        try
        {
            return JsonConvert.DeserializeObject(File.ReadAllText(KoaloaderProxyChoicesPath),
                    typeof(IEnumerable<(Platform platform, string id, string proxy, bool enabled)>)) as
                IEnumerable<(Platform platform, string id, string proxy, bool enabled)>;
        }
        catch
        {
            return Enumerable.Empty<(Platform platform, string id, string proxy, bool enabled)>();
        }
    }

    internal static void WriteKoaloaderProxyChoices(IEnumerable<(Platform platform, string id, string proxy, bool enabled)> choices)
    {
        try
        {
            if (choices is null || !choices.Any())
                File.Delete(KoaloaderProxyChoicesPath);
            else
                File.WriteAllText(KoaloaderProxyChoicesPath, JsonConvert.SerializeObject(choices));
        }
        catch
        {
            // ignored
        }
    }
}