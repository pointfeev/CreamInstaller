using System.Windows.Forms;

namespace CreamInstaller.Components;

internal class CustomForm : Form
{
    internal CustomForm() : base() => Icon = Properties.Resources.Icon;

    internal CustomForm(IWin32Window owner) : this() => Owner = (owner as Form) ?? ActiveForm;

    protected override CreateParams CreateParams // Double buffering for all controls
    {
        get
        {
            CreateParams handleParam = base.CreateParams;
            handleParam.ExStyle |= 0x02; // WS_EX_COMPOSITED       
            return handleParam;
        }
    }

    internal void InheritLocation(Form fromForm)
    {
        int X = fromForm.Location.X + fromForm.Size.Width / 2 - Size.Width / 2;
        int Y = fromForm.Location.Y + fromForm.Size.Height / 2 - Size.Height / 2;
        Location = new(X, Y);
    }
}
