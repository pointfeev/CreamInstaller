using System.Drawing;
using System.Windows.Forms;

namespace CreamInstaller.Utility;

internal static class InstallationLog
{
    internal static readonly Color Background = Color.DarkSlateGray;
    internal static readonly Color Operation = Color.LightGray;
    internal static readonly Color Resource = Color.LightBlue;
    internal static readonly Color Success = Color.LightGreen;
    internal static readonly Color Cleanup = Color.YellowGreen;
    internal static readonly Color Warning = Color.Yellow;
    internal static readonly Color Error = Color.DarkOrange;

    internal static void AppendText(this RichTextBox logTextBox, string text, Color color)
    {
        logTextBox.SelectionStart = logTextBox.TextLength;
        logTextBox.SelectionLength = 0;
        logTextBox.SelectionColor = color;
        logTextBox.AppendText(text);
        logTextBox.SelectionColor = logTextBox.ForeColor;
    }
}
