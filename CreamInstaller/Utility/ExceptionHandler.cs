using System;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using CreamInstaller.Forms;

namespace CreamInstaller.Utility;

internal static class ExceptionHandler
{
    internal static bool HandleException(this Exception e, Form form = null, string caption = null, string acceptButtonText = "Retry",
        string cancelButtonText = "Cancel")
    {
        caption ??= Program.Name + " encountered an exception";
        StringBuilder output = new();
        int stackDepth = 0;
        while (e is not null)
        {
            if (stackDepth > 10)
                break;
            if (output.Length > 0)
                _ = output.Append("\n\n");
            string[] stackTrace = e.StackTrace?.Split('\n');
            if (stackTrace is not null && stackTrace.Length > 0)
            {
                _ = output.Append(e.GetType() + (": " + e.Message));
                for (int i = 0; i < stackTrace.Length; i++)
                {
                    string line = stackTrace[i];
                    int atNum = line.IndexOf("at ", StringComparison.Ordinal);
                    int inNum = line.IndexOf("in ", StringComparison.Ordinal);
                    int ciNum = line.LastIndexOf(@"CreamInstaller\", StringComparison.Ordinal);
                    int lineNum = line.LastIndexOf(":line ", StringComparison.Ordinal);
                    if (atNum != -1)
                        _ = output.Append("\n    " + (inNum != -1 ? line[atNum..(inNum - 1)] : line[atNum..]) + (inNum != -1
                            ? "\n        " + (ciNum != -1
                                ? "in " + (lineNum != -1 ? line[ciNum..lineNum] + "\n            on " + line[(lineNum + 1)..] : line[ciNum..])
                                : line[inNum..])
                            : null));
                }
            }
            e = e.InnerException;
            stackDepth++;
        }
        using DialogForm dialogForm = new(form ?? Form.ActiveForm);
        return dialogForm.Show(SystemIcons.Error, output.ToString(), acceptButtonText, cancelButtonText, caption) == DialogResult.OK;
    }

    internal static void HandleFatalException(this Exception e)
    {
        bool? restart = e?.HandleException(caption: Program.Name + " encountered a fatal exception", acceptButtonText: "Restart");
        if (restart.HasValue && restart.Value)
            Application.Restart();
        Application.Exit();
    }
}

public class CustomMessageException : Exception
{
    public CustomMessageException() => Message = "CustomMessageException";

    public CustomMessageException(string message) : base(message) => Message = message;

    public CustomMessageException(string message, Exception e) : base(message, e) => Message = message;
    public override string Message { get; }

    public override string ToString() => Message;
}