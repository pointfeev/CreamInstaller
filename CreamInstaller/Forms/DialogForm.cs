using CreamInstaller.Components;

using System;
using System.Drawing;
using System.Windows.Forms;

namespace CreamInstaller;

internal partial class DialogForm : CustomForm
{
    internal DialogForm(IWin32Window owner) : base(owner)
    {
        InitializeComponent();
        TopLevel = true;
    }

    internal DialogResult Show(Icon descriptionIcon, string descriptionText, string acceptButtonText = "OK", string cancelButtonText = null, string customFormText = null, Icon customFormIcon = null)
    {
        descriptionIcon ??= Icon;
        icon.Image = descriptionIcon.ToBitmap();
        descriptionLabel.Text = descriptionText;
        acceptButton.Text = acceptButtonText;
        if (cancelButtonText is null)
        {
            cancelButton.Enabled = false;
            cancelButton.Visible = false;
        }
        else cancelButton.Text = cancelButtonText;
        if (customFormText is not null)
            Text = customFormText;
        else
        {
            OnResize(null, null);
            Resize += OnResize;
        }
        if (customFormIcon is not null)
            Icon = customFormIcon;
        return ShowDialog();
    }

    private void OnResize(object s, EventArgs e) =>
        Text = TextRenderer.MeasureText(Program.ApplicationName, Font).Width > Size.Width - 100
            ? Program.ApplicationNameShort
            : Program.ApplicationName;
}
