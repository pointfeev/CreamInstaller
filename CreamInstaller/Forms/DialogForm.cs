using System.Drawing;
using System.Windows.Forms;

namespace CreamInstaller
{
    public partial class DialogForm : Form
    {
        public DialogForm(IWin32Window owner)
        {
            Owner = owner as Form;
            InitializeComponent();
            Icon = Properties.Resources.Icon;
        }

        public DialogResult Show(string formName, Icon descriptionIcon, string descriptionText, string acceptButtonText, string cancelButtonText = null)
        {
            icon.Image = descriptionIcon.ToBitmap();
            Text = formName;
            descriptionLabel.Text = descriptionText;
            acceptButton.Text = acceptButtonText;
            if (cancelButtonText is null)
            {
                cancelButton.Enabled = false;
                cancelButton.Visible = false;
            }
            else
            {
                cancelButton.Text = cancelButtonText;
            }
            return ShowDialog();
        }
    }
}