using CreamInstaller.Resources;

using Newtonsoft.Json;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace CreamInstaller.Utility;

internal static class ProgramData
{
    internal static readonly string DirectoryPathOld = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"\CreamInstaller";
    internal static readonly string DirectoryPath = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData) + @"\CreamInstaller";

    internal static readonly string AppInfoPath = DirectoryPath + @"\appinfo";
    internal static readonly string AppInfoVersionPath = AppInfoPath + @"\version.txt";

    internal static readonly Version MinimumAppInfoVersion = Version.Parse("3.2.0.0");

    internal static readonly string CooldownPath = DirectoryPath + @"\cooldown";

    internal static readonly string OldProgramChoicesPath = DirectoryPath + @"\choices.txt";
    internal static readonly string ProgramChoicesPath = DirectoryPath + @"\choices.json";
    internal static readonly string DlcChoicesPath = DirectoryPath + @"\dlc.json";
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

    internal static List<(Platform platform, string id)> ReadProgramChoices()
    {
        if (!File.Exists(ProgramChoicesPath)) return null;
        try
        {
            return JsonConvert.DeserializeObject(File.ReadAllText(ProgramChoicesPath),
                typeof(List<(Platform platform, string id)>)) as List<(Platform platform, string id)>;
        }
        catch
        {
            return new();
        }
    }
    internal static void WriteProgramChoices(List<(Platform platform, string id)> choices)
    {
        try
        {
            if (choices is null || !choices.Any())
                File.Delete(ProgramChoicesPath);
            else
                File.WriteAllText(ProgramChoicesPath, JsonConvert.SerializeObject(choices));
        }
        catch { }
    }

    internal static List<(Platform platform, string gameId, string dlcId)> ReadDlcChoices()
    {
        if (!File.Exists(DlcChoicesPath)) return null;
        try
        {
            return JsonConvert.DeserializeObject(File.ReadAllText(DlcChoicesPath),
                typeof(List<(Platform platform, string gameId, string dlcId)>)) as List<(Platform platform, string gameId, string dlcId)>;
        }
        catch
        {
            return new();
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
            if (choices is null || !choices.Any())
                File.Delete(KoaloaderProxyChoicesPath);
            else
                File.WriteAllText(KoaloaderProxyChoicesPath, JsonConvert.SerializeObject(choices));
        }
        catch { }
    }

    internal static void UpdateKoaloaderProxyChoices(bool initial = false)
    {
        List<(Platform platform, string id, string proxy)> choices = ReadKoaloaderProxyChoices() ?? new();
        if (!initial)
            foreach (ProgramSelection selection in ProgramSelection.AllSafe)
            {
                _ = choices.RemoveAll(c => c.platform == selection.Platform && c.id == selection.Id);
                if (selection.KoaloaderProxy is not null and not ProgramSelection.DefaultKoaloaderProxy)
                    choices.Add((selection.Platform, selection.Id, selection.KoaloaderProxy));
            }
        foreach ((Platform platform, string id, string proxy) choice in choices.ToList())
        {
            string proxy = choice.proxy;
            if (proxy is not null && proxy.Contains('.')) // convert pre-v4.1.0.0 choices
                proxy.GetProxyInfoFromIdentifier(out proxy, out _);
            if (choice.proxy != proxy && choices.Remove(choice)) // convert pre-v4.1.0.0 choices
                choices.Add((choice.platform, choice.id, proxy));
            if (proxy is null or ProgramSelection.DefaultKoaloaderProxy)
                _ = choices.RemoveAll(c => c.platform == choice.platform && c.id == choice.id);
            else if (ProgramSelection.FromPlatformId(choice.platform, choice.id) is ProgramSelection selection)
                selection.KoaloaderProxy = proxy;
        }
        WriteKoaloaderProxyChoices(choices);
        foreach (Form form in Application.OpenForms)
            if (form is SelectForm selectForm)
                selectForm.OnKoaloaderProxiesChanged();
    }

    internal static void ResetKoaloaderProxyChoices()
    {
        foreach (ProgramSelection selection in ProgramSelection.AllSafe)
            selection.KoaloaderProxy = null;
        UpdateKoaloaderProxyChoices();
    }
}
