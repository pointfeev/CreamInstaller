using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using Newtonsoft.Json;

namespace CreamInstaller.Utility;

internal static class ProgramData
{
    private static readonly string DirectoryPathOld =
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"\CreamInstaller";

    internal static readonly string DirectoryPath =
        Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData) + @"\CreamInstaller";

    internal static readonly string AppInfoPath = DirectoryPath + @"\appinfo";
    private static readonly string AppInfoVersionPath = AppInfoPath + @"\version.txt";

    private static readonly Version MinimumAppInfoVersion = Version.Parse("4.7.0.0");

    internal static readonly string CooldownPath = DirectoryPath + @"\cooldown";

    private static readonly string OldProgramChoicesPath = DirectoryPath + @"\choices.txt";
    private static readonly string ProgramChoicesPath = DirectoryPath + @"\choices.json";
    private static readonly string DlcChoicesPath = DirectoryPath + @"\dlc.json";
    private static readonly string KoaloaderProxyChoicesPath = DirectoryPath + @"\proxies.json";

    internal static async Task Setup(Form form = null)
        => await Task.Run(() =>
        {
            if (DirectoryPathOld.DirectoryExists())
            {
                DirectoryPath.DeleteDirectory();
                DirectoryPathOld.MoveDirectory(DirectoryPath, true, form);
            }

            DirectoryPath.CreateDirectory();
            if (!AppInfoVersionPath.FileExists() ||
                !Version.TryParse(AppInfoVersionPath.ReadFile(), out Version version) ||
                version < MinimumAppInfoVersion)
            {
                AppInfoPath.DeleteDirectory();
                AppInfoPath.CreateDirectory();
                AppInfoVersionPath.WriteFile(Program.Version);
            }

            CooldownPath.CreateDirectory();
            if (OldProgramChoicesPath.FileExists())
                OldProgramChoicesPath.DeleteFile();
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
        if (!CooldownPath.DirectoryExists())
            return null;
        string cooldownFile = CooldownPath + @$"\{identifier}.txt";
        if (!cooldownFile.FileExists())
            return null;
        try
        {
            if (DateTime.TryParse(cooldownFile.ReadFile(), out DateTime cooldown))
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
        CooldownPath.CreateDirectory();
        string cooldownFile = CooldownPath + @$"\{identifier}.txt";
        try
        {
            cooldownFile.WriteFile(time.ToString(CultureInfo.InvariantCulture));
        }
        catch
        {
            // ignored
        }
    }

    internal static IEnumerable<(Platform platform, string id)> ReadProgramChoices()
    {
        if (ProgramChoicesPath.FileExists())
            try
            {
                if (JsonConvert.DeserializeObject(ProgramChoicesPath.ReadFile(),
                        typeof(List<(Platform platform, string id)>)) is
                    List<(Platform platform, string id)> choices)
                    return choices;
            }
            catch
            {
                // ignored
            }

        return [];
    }

    internal static void WriteProgramChoices(IEnumerable<(Platform platform, string id)> choices)
    {
        try
        {
            if (choices is null || !choices.Any())
                ProgramChoicesPath.DeleteFile();
            else
                ProgramChoicesPath.WriteFile(JsonConvert.SerializeObject(choices));
        }
        catch
        {
            // ignored
        }
    }

    internal static IEnumerable<(Platform platform, string gameId, string dlcId)> ReadDlcChoices()
    {
        if (DlcChoicesPath.FileExists())
            try
            {
                if (JsonConvert.DeserializeObject(DlcChoicesPath.ReadFile(),
                        typeof(IEnumerable<(Platform platform, string gameId, string dlcId)>)) is
                    IEnumerable<(Platform platform, string gameId, string dlcId)> choices)
                    return choices;
            }
            catch
            {
                // ignored
            }

        return [];
    }

    internal static void WriteDlcChoices(List<(Platform platform, string gameId, string dlcId)> choices)
    {
        try
        {
            if (choices is null || choices.Count == 0)
                DlcChoicesPath.DeleteFile();
            else
                DlcChoicesPath.WriteFile(JsonConvert.SerializeObject(choices));
        }
        catch
        {
            // ignored
        }
    }

    internal static IEnumerable<(Platform platform, string id, string proxy, bool enabled)> ReadProxyChoices()
    {
        if (KoaloaderProxyChoicesPath.FileExists())
            try
            {
                if (JsonConvert.DeserializeObject(KoaloaderProxyChoicesPath.ReadFile(),
                        typeof(IEnumerable<(Platform platform, string id, string proxy, bool enabled)>)) is
                    IEnumerable<(Platform platform, string id, string proxy, bool enabled)> choices)
                    return choices;
            }
            catch
            {
                // ignored
            }

        return [];
    }

    internal static void WriteProxyChoices(
        IEnumerable<(Platform platform, string id, string proxy, bool enabled)> choices)
    {
        try
        {
            if (choices is null || !choices.Any())
                KoaloaderProxyChoicesPath.DeleteFile();
            else
                KoaloaderProxyChoicesPath.WriteFile(JsonConvert.SerializeObject(choices));
        }
        catch
        {
            // ignored
        }
    }
}