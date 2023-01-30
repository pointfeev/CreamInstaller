using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
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

    internal static async Task Setup(Form form = null)
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
            if (!AppInfoVersionPath.Exists(form: form) || !Version.TryParse(AppInfoVersionPath.Read(), out Version version) || version < MinimumAppInfoVersion)
            {
                if (Directory.Exists(AppInfoPath))
                    Directory.Delete(AppInfoPath, true);
                _ = Directory.CreateDirectory(AppInfoPath);
                AppInfoVersionPath.Write(Program.Version);
            }
            if (!Directory.Exists(CooldownPath))
                _ = Directory.CreateDirectory(CooldownPath);
            if (OldProgramChoicesPath.Exists(form: form))
                OldProgramChoicesPath.Delete();
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
        if (!cooldownFile.Exists())
            return null;
        try
        {
            if (DateTime.TryParse(cooldownFile.Read(), out DateTime cooldown))
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
            cooldownFile.Write(time.ToString(CultureInfo.InvariantCulture));
        }
        catch
        {
            // ignored
        }
    }

    internal static IEnumerable<(Platform platform, string id)> ReadProgramChoices()
    {
        if (ProgramChoicesPath.Exists())
            try
            {
                if (JsonConvert.DeserializeObject(ProgramChoicesPath.Read(), typeof(List<(Platform platform, string id)>)) is
                    List<(Platform platform, string id)> choices)
                    return choices;
            }
            catch
            {
                // ignored
            }
        return Enumerable.Empty<(Platform platform, string id)>();
    }

    internal static void WriteProgramChoices(IEnumerable<(Platform platform, string id)> choices)
    {
        try
        {
            if (choices is null || !choices.Any())
                ProgramChoicesPath.Delete();
            else
                ProgramChoicesPath.Write(JsonConvert.SerializeObject(choices));
        }
        catch
        {
            // ignored
        }
    }

    internal static IEnumerable<(Platform platform, string gameId, string dlcId)> ReadDlcChoices()
    {
        if (DlcChoicesPath.Exists())
            try
            {
                if (JsonConvert.DeserializeObject(DlcChoicesPath.Read(), typeof(IEnumerable<(Platform platform, string gameId, string dlcId)>)) is
                    IEnumerable<(Platform platform, string gameId, string dlcId)> choices)
                    return choices;
            }
            catch
            {
                // ignored
            }
        return Enumerable.Empty<(Platform platform, string gameId, string dlcId)>();
    }

    internal static void WriteDlcChoices(List<(Platform platform, string gameId, string dlcId)> choices)
    {
        try
        {
            if (choices is null || !choices.Any())
                DlcChoicesPath.Delete();
            else
                DlcChoicesPath.Write(JsonConvert.SerializeObject(choices));
        }
        catch
        {
            // ignored
        }
    }

    internal static IEnumerable<(Platform platform, string id, string proxy, bool enabled)> ReadKoaloaderChoices()
    {
        if (KoaloaderProxyChoicesPath.Exists())
            try
            {
                if (JsonConvert.DeserializeObject(KoaloaderProxyChoicesPath.Read(),
                        typeof(IEnumerable<(Platform platform, string id, string proxy, bool enabled)>)) is
                    IEnumerable<(Platform platform, string id, string proxy, bool enabled)> choices)
                    return choices;
            }
            catch
            {
                // ignored
            }
        return Enumerable.Empty<(Platform platform, string id, string proxy, bool enabled)>();
    }

    internal static void WriteKoaloaderProxyChoices(IEnumerable<(Platform platform, string id, string proxy, bool enabled)> choices)
    {
        try
        {
            if (choices is null || !choices.Any())
                KoaloaderProxyChoicesPath.Delete();
            else
                KoaloaderProxyChoicesPath.Write(JsonConvert.SerializeObject(choices));
        }
        catch
        {
            // ignored
        }
    }
}