using System.IO;

namespace CreamInstaller
{
    internal static class FileResourceExtensions
    {
        internal static void Write(this byte[] resource, string filePath)
        {
            using FileStream file = new(filePath, FileMode.Create, FileAccess.Write);
            file.Write(resource);
        }
    }
}