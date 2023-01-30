using System.Drawing;
using System.IO;
using System.Text;
using System.Windows.Forms;
using CreamInstaller.Forms;

namespace CreamInstaller.Utility;

internal static class SafeIO
{
    internal static bool Exists(this string filePath, bool crucial = false, Form form = null)
    {
        while (!Program.Canceled)
        {
            bool exists = File.Exists(filePath);
            if (exists || !crucial || filePath.Warn("Failed to find a crucial file", form) is not DialogResult.OK)
                return exists;
        }
        return false;
    }

    internal static void Create(this string filePath, bool crucial = false, Form form = null)
    {
        while (!Program.Canceled)
            try
            {
                File.Create(filePath).Close();
                break;
            }
            catch
            {
                if (!crucial || filePath.Warn("Failed to create a crucial file", form) is not DialogResult.OK)
                    break;
            }
    }

    internal static void Move(this string filePath, string newFilePath, bool crucial = false, Form form = null)
    {
        while (!Program.Canceled)
            try
            {
                File.Move(filePath, newFilePath);
                break;
            }
            catch
            {
                if (!crucial || !filePath.Exists(true) || filePath.Warn("Failed to move a crucial file", form) is not DialogResult.OK)
                    break;
            }
    }

    internal static void Delete(this string filePath, bool crucial = false, Form form = null)
    {
        if (!filePath.Exists(form: form))
            return;
        while (!Program.Canceled)
            try
            {
                File.Delete(filePath);
                break;
            }
            catch
            {
                if (!crucial || filePath.Warn("Failed to delete a crucial file", form) is not DialogResult.OK)
                    break;
            }
    }

    internal static string Read(this string filePath, bool crucial = false, Form form = null)
    {
        while (!Program.Canceled)
            try
            {
                return File.ReadAllText(filePath, Encoding.UTF8);
            }
            catch
            {
                if (!crucial || !filePath.Exists(true) || filePath.Warn("Failed to read a crucial file", form) is not DialogResult.OK)
                    break;
            }
        return null;
    }

    internal static byte[] ReadBytes(this string filePath, bool crucial = false, Form form = null)
    {
        while (!Program.Canceled)
            try
            {
                return File.ReadAllBytes(filePath);
            }
            catch
            {
                if (!crucial || !filePath.Exists(true) || filePath.Warn("Failed to read a crucial file", form) is not DialogResult.OK)
                    break;
            }
        return null;
    }

    internal static void Write(this string filePath, string text, bool crucial = false, Form form = null)
    {
        while (!Program.Canceled)
            try
            {
                File.WriteAllText(filePath, text, Encoding.UTF8);
                break;
            }
            catch
            {
                if (!crucial || filePath.Warn("Failed to write a crucial file", form) is not DialogResult.OK)
                    break;
            }
    }

    private static DialogResult Warn(this string filePath, string message, Form form = null)
    {
        using DialogForm dialogForm = new(form ?? Form.ActiveForm);
        return dialogForm.Show(SystemIcons.Warning, message + ": " + filePath.BeautifyPath(), "Retry", "OK");
    }
}