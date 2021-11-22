using System.Drawing;
using System.Windows.Forms;

namespace CreamInstaller
{
    public static class InstallationLog
    {
        public static readonly Color Background = Color.DarkSlateGray;
        public static readonly Color Operation = Color.LightGray;
        public static readonly Color Resource = Color.LightBlue;
        public static readonly Color Success = Color.LightGreen;
        public static readonly Color Cleanup = Color.YellowGreen;
        public static readonly Color Warning = Color.Yellow;
        public static readonly Color Error = Color.DarkOrange;

        public static void AppendText(this RichTextBox logTextBox, string text, Color color)
        {
            logTextBox.SelectionStart = logTextBox.TextLength;
            logTextBox.SelectionLength = 0;
            logTextBox.SelectionColor = color;
            logTextBox.AppendText(text);
            logTextBox.SelectionColor = logTextBox.ForeColor;
        }
    }
}