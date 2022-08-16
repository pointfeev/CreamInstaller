using System;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace CreamInstaller.Utility;

internal static class ExceptionHandler
{
    internal static bool HandleException(this Exception e, Form form = null, string caption = "CreamInstaller encountered an exception", string acceptButtonText = "Retry", string cancelButtonText = "Cancel")
    {
        while (e.InnerException is not null) // we usually don't need the outer exceptions
            e = e.InnerException;
        StringBuilder output = new();
        string[] stackTrace = e.StackTrace?.Split('\n');
        if (stackTrace is not null && stackTrace.Length > 0)
        {
            _ = output.Append("STACK TRACE\n");
            for (int i = 0; i < Math.Min(stackTrace.Length, 3); i++)
            {
                string line = stackTrace[i];
                int atNum = line.IndexOf("at ");
                int inNum = line.IndexOf("in ");
                int ciNum = line.LastIndexOf(@"CreamInstaller\");
                int lineNum = line.LastIndexOf(":line ");
                if (line is not null && atNum != -1)
                    _ = output.Append("\n    " + (inNum != -1 ? line[atNum..(inNum - 1)] : line[atNum..])
                        + (inNum != -1 ? ("\n        "
                            + (ciNum != -1 ? ("in "
                                + (lineNum != -1 ? line[ciNum..lineNum]
                                    + "\n            on " + line[(lineNum + 1)..]
                                : line[ciNum..]))
                            : line[inNum..]))
                        : null));
            }
        }
        string[] messageLines = e.Message?.Split('\n');
        if (messageLines is not null && messageLines.Length > 0)
        {
            if (output.Length > 0)
                _ = output.Append("\n\n");
            _ = output.Append("MESSAGE\n");
            for (int i = 0; i < messageLines.Length; i++)
            {
                string line = messageLines[i];
                if (line is not null)
                    _ = output.Append("\n    " + line);
            }
        }
        using DialogForm dialogForm = new(form ?? Form.ActiveForm);
        return dialogForm.Show(SystemIcons.Error, output.ToString(), acceptButtonText, cancelButtonText, customFormText: caption) == DialogResult.OK;
    }

    internal static void HandleFatalException(this Exception e)
    {
        bool? restart = e?.HandleException(caption: "CreamInstaller encountered a fatal exception", acceptButtonText: "Restart");
        if (restart.HasValue && restart.Value)
            Application.Restart();
        Application.Exit();
    }
}

public class CustomMessageException : Exception
{
    private readonly string message;
    public override string Message => message;

    public override string ToString() => Message;

    public CustomMessageException() => message = "CustomMessageException";

    public CustomMessageException(string message) => this.message = message;

    public CustomMessageException(string message, Exception _) => this.message = message;
}
