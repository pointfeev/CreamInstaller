using System;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace CreamInstaller.Utility;

internal static class ExceptionHandler
{
    internal static bool HandleException(this Exception e, Form form = null, string caption = null, string acceptButtonText = "Retry", string cancelButtonText = "Cancel")
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
                _ = output.Append(e.GetType() + (e.Message is not null ? (": " + e.Message) : ""));
                for (int i = 0; i < stackTrace.Length; i++)
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
            e = e.InnerException;
            stackDepth++;
        }
        using DialogForm dialogForm = new(form ?? Form.ActiveForm);
        return dialogForm.Show(SystemIcons.Error, output.ToString(), acceptButtonText, cancelButtonText, customFormText: caption) == DialogResult.OK;
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
    private readonly string message;
    public override string Message => message;

    public override string ToString() => Message;

    public CustomMessageException() : base() => message = "CustomMessageException";

    public CustomMessageException(string message) : base(message) => this.message = message;

    public CustomMessageException(string message, Exception e) : base(message, e) => this.message = message;
}
