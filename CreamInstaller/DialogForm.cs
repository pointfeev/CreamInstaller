using System;
using System.Drawing;
using System.Windows.Forms;

namespace CreamInstaller
{
    public partial class DialogForm : Form
    {
        public DialogForm()
        {
            InitializeComponent();
        }

        public DialogResult Show(string formName, Icon descriptionIcon, string descriptionText, string acceptButtonText, string cancelButtonText)
        {
            icon.Image = descriptionIcon.ToBitmap();
            Text = formName;
            descriptionLabel.Text = descriptionText;
            acceptButton.Text = acceptButtonText;
            cancelButton.Text = cancelButtonText;
            return ShowDialog();
        }
    }
}
