using System.Drawing;
using System.Windows.Forms;

namespace CreamInstaller.Utility;

internal static class LogTextBox
{
    internal static readonly Color Background = Color.DarkSlateGray;
    internal static readonly Color Operation = Color.LightGray;
    internal static readonly Color Action = Color.LightBlue;
    internal static readonly Color Success = Color.LightGreen;
    internal static readonly Color Cleanup = Color.YellowGreen;
    internal static readonly Color Warning = Color.Yellow;
    internal static readonly Color Error = Color.DarkOrange;

    internal static void AppendText(this RichTextBox textBox, string text, Color color, bool scroll = false)
    {
        textBox.SelectionStart = textBox.TextLength;
        textBox.SelectionLength = 0;
        textBox.SelectionColor = color;
        if (scroll)
            textBox.ScrollToCaret();
        textBox.AppendText(text);
        if (scroll)
            textBox.ScrollToCaret();
        textBox.SelectionColor = textBox.ForeColor;
        textBox.Invalidate();
    }
}