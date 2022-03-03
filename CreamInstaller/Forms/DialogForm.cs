using System;
using System.Drawing;
using System.Windows.Forms;

using CreamInstaller.Forms.Components;

namespace CreamInstaller;

internal partial class DialogForm : CustomForm
{
    internal DialogForm(IWin32Window owner) : base(owner) => InitializeComponent();

    internal DialogResult Show(Icon descriptionIcon, string descriptionText, string acceptButtonText, string cancelButtonText = null, Icon customFormIcon = null)
    {
        if (customFormIcon is not null)
            Icon = customFormIcon;
        icon.Image = descriptionIcon.ToBitmap();
        descriptionLabel.Text = descriptionText;
        acceptButton.Text = acceptButtonText;
        if (cancelButtonText is null)
        {
            cancelButton.Enabled = false;
            cancelButton.Visible = false;
        }
        else cancelButton.Text = cancelButtonText;
        OnResize(null, null);
        Resize += OnResize;
        return ShowDialog();
    }

    internal void OnResize(object s, EventArgs e) =>
        Text = TextRenderer.MeasureText(Program.ApplicationName, Font).Width > Size.Width - 100
            ? Program.ApplicationNameShort
            : Program.ApplicationName;
}
