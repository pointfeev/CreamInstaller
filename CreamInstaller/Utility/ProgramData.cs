using System;
using System.IO;
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
    });
}
