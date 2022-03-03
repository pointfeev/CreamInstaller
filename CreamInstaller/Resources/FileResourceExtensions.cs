using System.IO;

namespace CreamInstaller.Resources;

internal static class FileResourceExtensions
{
    internal static void Write(this byte[] resource, string filePath)
    {
        using FileStream file = new(filePath, FileMode.Create, FileAccess.Write);
        file.Write(resource);
    }

    internal static bool EqualsFile(this byte[] resource, string filePath)
    {
        byte[] file = File.ReadAllBytes(filePath);
        if (resource.Length != file.Length)
            return false;
        for (int i = 0; i < resource.Length; i++)
            if (resource[i] != file[i])
                return false;
        return true;
    }
}
