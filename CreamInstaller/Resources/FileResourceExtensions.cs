using System.IO;

namespace CreamInstaller
{
    public static class FileResourceExtensions
    {
        public static void Write(this byte[] resource, string filePath)
        {
            using FileStream file = new(filePath, FileMode.Create, FileAccess.Write);
            file.Write(resource);
        }
    }
}