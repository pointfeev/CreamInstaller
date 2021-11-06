using System.IO;
using System.Reflection;

namespace CreamInstaller
{
    public static class Resources
    {
        public static void WriteResourceToFile(string resourceName, string filePath)
        {
            using Stream resource = Assembly.GetExecutingAssembly().GetManifestResourceStream("CreamInstaller." + resourceName);
            using FileStream file = new(filePath, FileMode.Create, FileAccess.Write);
            resource.CopyTo(file);
        }
    }
}