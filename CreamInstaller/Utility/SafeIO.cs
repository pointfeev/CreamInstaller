using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using CreamInstaller.Forms;

namespace CreamInstaller.Utility;

internal static class SafeIO
{
    internal static bool FileLocked(this string filePath)
    {
        if (!FileExists(filePath))
            return false;
        try
        {
            File.Open(filePath, FileMode.Open, FileAccess.ReadWrite, FileShare.None).Close();
        }
        catch
        {
            return true;
        }
        return false;
    }

    internal static bool DirectoryExists(this string directoryPath) => Directory.Exists(directoryPath);

    internal static void CreateDirectory(this string directoryPath, bool crucial = false, Form form = null)
    {
        if (directoryPath.DirectoryExists())
            return;
        while (!Program.Canceled)
            try
            {
                _ = Directory.CreateDirectory(directoryPath);
                break;
            }
            catch (Exception e)
            {
                if (!crucial || directoryPath.DirectoryExists() || directoryPath.IOWarn("Failed to create a crucial directory", e, form) is not DialogResult.OK)
                    break;
            }
    }

    internal static void MoveDirectory(this string directoryPath, string newDirectoryPath, bool crucial = false, Form form = null)
    {
        if (!directoryPath.DirectoryExists())
            return;
        while (!Program.Canceled)
            try
            {
                Directory.Move(directoryPath, newDirectoryPath);
                break;
            }
            catch (Exception e)
            {
                if (!crucial || !directoryPath.DirectoryExists() || directoryPath.IOWarn("Failed to move a crucial directory", e, form) is not DialogResult.OK)
                    break;
            }
    }

    internal static void DeleteDirectory(this string directoryPath, bool crucial = false, Form form = null)
    {
        if (!directoryPath.DirectoryExists())
            return;
        while (!Program.Canceled)
            try
            {
                Directory.Delete(directoryPath, true);
                break;
            }
            catch (Exception e)
            {
                if (!crucial || !directoryPath.DirectoryExists()
                             || directoryPath.IOWarn("Failed to delete a crucial directory", e, form) is not DialogResult.OK)
                    break;
            }
    }

    internal static IEnumerable<string> EnumerateDirectory(this string directoryPath, string filePattern, bool subdirectories = false, bool crucial = false,
        Form form = null)
    {
        if (!directoryPath.DirectoryExists())
            return Enumerable.Empty<string>();
        while (!Program.Canceled)
            try
            {
                return subdirectories
                    ? Directory.EnumerateFiles(directoryPath, filePattern, new EnumerationOptions { RecurseSubdirectories = true })
                    : Directory.EnumerateFiles(directoryPath, filePattern);
            }
            catch (Exception e)
            {
                if (!crucial || !directoryPath.DirectoryExists()
                             || directoryPath.IOWarn("Failed to enumerate a crucial directory's files", e, form) is not DialogResult.OK)
                    break;
            }
        return Enumerable.Empty<string>();
    }

    internal static IEnumerable<string> EnumerateSubdirectories(this string directoryPath, string directoryPattern, bool subdirectories = false,
        bool crucial = false, Form form = null)
    {
        if (!directoryPath.DirectoryExists())
            return Enumerable.Empty<string>();
        while (!Program.Canceled)
            try
            {
                return subdirectories
                    ? Directory.EnumerateDirectories(directoryPath, directoryPattern, new EnumerationOptions { RecurseSubdirectories = true })
                    : Directory.EnumerateDirectories(directoryPath, directoryPattern);
            }
            catch (Exception e)
            {
                if (!crucial || !directoryPath.DirectoryExists()
                             || directoryPath.IOWarn("Failed to enumerate a crucial directory's subdirectories", e, form) is not DialogResult.OK)
                    break;
            }
        return Enumerable.Empty<string>();
    }

    internal static bool FileExists(this string filePath) => File.Exists(filePath);

    internal static FileStream CreateFile(this string filePath, bool crucial = false, Form form = null)
    {
        while (!Program.Canceled)
            try
            {
                return File.Create(filePath);
            }
            catch (Exception e)
            {
                if (!crucial || filePath.IOWarn("Failed to create a crucial file", e, form) is not DialogResult.OK)
                    break;
            }
        return null;
    }

    internal static void MoveFile(this string filePath, string newFilePath, bool crucial = false, Form form = null)
    {
        if (!filePath.FileExists())
            return;
        while (!Program.Canceled)
            try
            {
                File.Move(filePath, newFilePath);
                break;
            }
            catch (Exception e)
            {
                if (!crucial || !filePath.FileExists() || filePath.IOWarn("Failed to move a crucial file", e, form) is not DialogResult.OK)
                    break;
            }
    }

    internal static void DeleteFile(this string filePath, bool crucial = false, Form form = null)
    {
        if (!filePath.FileExists())
            return;
        while (!Program.Canceled)
            try
            {
                File.Delete(filePath);
                break;
            }
            catch (Exception e)
            {
                if (!crucial || !filePath.FileExists() || filePath.IOWarn("Failed to delete a crucial file", e, form) is not DialogResult.OK)
                    break;
            }
    }

    internal static string ReadFile(this string filePath, bool crucial = false, Form form = null)
    {
        if (!filePath.FileExists())
            return null;
        while (!Program.Canceled)
            try
            {
                return File.ReadAllText(filePath, Encoding.UTF8);
            }
            catch (Exception e)
            {
                if (!crucial || !filePath.FileExists() || filePath.IOWarn("Failed to read a crucial file", e, form) is not DialogResult.OK)
                    break;
            }
        return null;
    }

    internal static byte[] ReadFileBytes(this string filePath, bool crucial = false, Form form = null)
    {
        if (!filePath.FileExists())
            return null;
        while (!Program.Canceled)
            try
            {
                return File.ReadAllBytes(filePath);
            }
            catch (Exception e)
            {
                if (!crucial || !filePath.FileExists() || filePath.IOWarn("Failed to read a crucial file", e, form) is not DialogResult.OK)
                    break;
            }
        return null;
    }

    internal static void WriteFile(this string filePath, string text, bool crucial = false, Form form = null)
    {
        while (!Program.Canceled)
            try
            {
                File.WriteAllText(filePath, text, Encoding.UTF8);
                break;
            }
            catch (Exception e)
            {
                if (!crucial || filePath.IOWarn("Failed to write a crucial file", e, form) is not DialogResult.OK)
                    break;
            }
    }

    internal static void ExtractZip(this string archivePath, string destinationPath, bool crucial = false, Form form = null)
    {
        if (!archivePath.FileExists())
            return;
        while (!Program.Canceled)
            try
            {
                ZipFile.ExtractToDirectory(archivePath, destinationPath);
                break;
            }
            catch (Exception e)
            {
                if (!crucial || !archivePath.FileExists() || archivePath.IOWarn("Failed to extract a crucial zip file", e, form) is not DialogResult.OK)
                    break;
            }
    }

    internal static DialogResult IOWarn(this string filePath, string message, Exception e, Form form = null)
    {
        form ??= Form.ActiveForm;
        if (form is null || !form.InvokeRequired)
            return filePath.IOWarnInternal(message, e, form);
        return form.Invoke(() => filePath.IOWarnInternal(message, e, form));
    }

    private static DialogResult IOWarnInternal(this string filePath, string message, Exception e, Form form = null)
    {
        using DialogForm dialogForm = new(form);
        string description = message + ": " + filePath.BeautifyPath() + "\n\n";
        if (e is IOException && (e.HResult & 0x0000FFFF) == 225) // virus or potentially unwanted software
            description += "Please resolve your anti-virus and press retry to continue . . . ";
        else
            description += e.FormatException();
        return dialogForm.Show(SystemIcons.Warning, description, "Retry", "OK");
    }
}