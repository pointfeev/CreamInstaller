using System.Drawing;
using System.Windows.Forms;

namespace CreamInstaller
{
    public static class LogColor
    {
        public static Color Background => Color.DarkSlateGray;
        public static Color Operation => Color.LightGray;
        public static Color Resource => Color.LightBlue;
        public static Color Success => Color.LightGreen;
        public static Color Cleanup => Color.YellowGreen;
        public static Color Warning => Color.Yellow;
        public static Color Error => Color.DarkOrange;

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