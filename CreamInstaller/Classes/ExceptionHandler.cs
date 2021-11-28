using System;
using System.Windows.Forms;

namespace CreamInstaller
{
    public static class ExceptionHandler
    {
        public static bool OutputException(Exception e)
        {
            while (!(e.InnerException is null)) e = e.InnerException;
            string output = "";
            string[] stackTrace = e.StackTrace?.Split('\n');
            if (!(stackTrace is null) && stackTrace.Length > 0)
            {
                output += "STACK TRACE\n";
                for (int i = 0; i < Math.Min(stackTrace.Length, 3); i++)
                {
                    string line = stackTrace[i];
                    if (!(line is null))
                    {
                        output += "\n    " + line[line.IndexOf("at")..];
                    }
                }
            }
            string[] messageLines = e.Message?.Split('\n');
            if (!(messageLines is null) && messageLines.Length > 0)
            {
                if (output.Length > 0) output += "\n\n";
                output += "MESSAGE\n";
                for (int i = 0; i < messageLines.Length; i++)
                {
                    string line = messageLines[i];
                    if (!(line is null))
                    {
                        output += "\n    " + messageLines[i];
                    }
                }
            }
            return MessageBox.Show(output, caption: "CreamInstaller encountered an exception", buttons: MessageBoxButtons.RetryCancel, icon: MessageBoxIcon.Error) == DialogResult.Retry;
        }
    }

    public class CustomMessageException : Exception
    {
        private string message;
        public override string Message => message ?? "CustomMessageException";

        public override string ToString() => Message;

        public CustomMessageException(string message)
        {
            this.message = message;
        }
    }
}