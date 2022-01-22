using System.Windows.Forms;

namespace CreamInstaller
{
    internal class CustomForm : Form
    {
        internal CustomForm() : base() => Icon = Properties.Resources.Icon;

        internal CustomForm(IWin32Window owner) : this() => Owner = owner as Form;

        protected override CreateParams CreateParams // Double buffering for all controls
        {
            get
            {
                CreateParams handleParam = base.CreateParams;
                handleParam.ExStyle |= 0x02000000; // WS_EX_COMPOSITED       
                return handleParam;
            }
        }
    }
}
