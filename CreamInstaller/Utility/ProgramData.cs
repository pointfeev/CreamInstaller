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

    internal static readonly string ChoicesPath = DirectoryPath + @"\choices.txt";

    internal static async Task Setup() => await Task.Run(() =>
    {
        if (Directory.Exists(DirectoryPathOld))
        {
            if (Directory.Exists(DirectoryPath)) Directory.Delete(DirectoryPath, true);
            Directory.Move(DirectoryPathOld, DirectoryPath);
        }
        if (!Directory.Exists(DirectoryPath)) Directory.CreateDirectory(DirectoryPath);
        if (!File.Exists(AppInfoVersionPath) || !Version.TryParse(File.ReadAllText(AppInfoVersionPath, Encoding.UTF8), out Version version) || version < MinimumAppInfoVersion)
        {
            if (Directory.Exists(AppInfoPath)) Directory.Delete(AppInfoPath, true);
            Directory.CreateDirectory(AppInfoPath);
            File.WriteAllText(AppInfoVersionPath, Application.ProductVersion, Encoding.UTF8);
        }
        if (!Directory.Exists(CooldownPath))
            Directory.CreateDirectory(CooldownPath);
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
            Directory.CreateDirectory(CooldownPath);
        string cooldownFile = CooldownPath + @$"\{identifier}.txt";
        try
        {
            File.WriteAllText(cooldownFile, time.ToString());
        }
        catch { }
    }

    internal static List<string> ReadChoices()
    {
        if (!File.Exists(ChoicesPath)) return new();
        try
        {
            return File.ReadAllLines(ChoicesPath).ToList();
        }
        catch
        {
            return new();
        }
    }
    internal static void WriteChoices(List<string> choices)
    {
        try
        {
            File.WriteAllLines(ChoicesPath, choices.ToArray());
        }
        catch { }
    }
}
