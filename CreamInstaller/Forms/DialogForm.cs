using System.Drawing;
using System.Windows.Forms;

using CreamInstaller.Forms.Components;

namespace CreamInstaller;

internal partial class DialogForm : CustomForm
{
    internal DialogForm(IWin32Window owner) : base(owner) => InitializeComponent();

    internal DialogResult Show(string formName, Icon descriptionIcon, string descriptionText, string acceptButtonText, string cancelButtonText = null, Icon customFormIcon = null)
    {
        if (customFormIcon is not null)
            Icon = customFormIcon;
        icon.Image = descriptionIcon.ToBitmap();
        Text = formName;
        descriptionLabel.Text = descriptionText;
        acceptButton.Text = acceptButtonText;
        if (cancelButtonText is null)
        {
            cancelButton.Enabled = false;
            cancelButton.Visible = false;
        }
        else cancelButton.Text = cancelButtonText;
        return ShowDialog();
    }
}
