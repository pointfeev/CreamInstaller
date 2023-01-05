using System;
using System.Drawing;
using System.Windows.Forms;
using CreamInstaller.Components;
using CreamInstaller.Utility;

namespace CreamInstaller.Forms;

internal partial class DebugForm : CustomForm
{
    internal static DebugForm current;

    private Form attachedForm;

    internal DebugForm()
    {
        InitializeComponent();
        debugTextBox.BackColor = LogTextBox.Background;
    }

    internal static DebugForm Current
    {
        get
        {
            if (current is not null && (current.Disposing || current.IsDisposed))
                current = null;
            return current ??= new();
        }
        set => current = value;
    }

    protected override void WndProc(ref Message message) // make form immovable by user
    {
        if (message.Msg == 0x0112) // WM_SYSCOMMAND
        {
            int command = message.WParam.ToInt32() & 0xFFF0;
            if (command == 0xF010) // SC_MOVE
                return;
        }
        base.WndProc(ref message);
    }

    internal void Attach(Form form)
    {
        if (attachedForm is not null)
        {
            attachedForm.Activated -= OnChange;
            attachedForm.LocationChanged -= OnChange;
            attachedForm.SizeChanged -= OnChange;
            attachedForm.VisibleChanged -= OnChange;
        }
        attachedForm = form;
        attachedForm.Activated += OnChange;
        attachedForm.LocationChanged += OnChange;
        attachedForm.SizeChanged += OnChange;
        attachedForm.VisibleChanged += OnChange;
        UpdateAttachment();
    }

    internal void OnChange(object sender, EventArgs args) => UpdateAttachment();

    internal void UpdateAttachment()
    {
        if (attachedForm is not null && attachedForm.Visible)
        {
            //Size = new(Size.Width, attachedForm.Size.Height);
            Location = new(attachedForm.Right, attachedForm.Top);
            BringToFrontWithoutActivation();
        }
    }

    internal void Log(string text) => Log(text, LogTextBox.Error);

    internal void Log(string text, Color color)
    {
        if (!debugTextBox.Disposing && !debugTextBox.IsDisposed)
            debugTextBox.Invoke(() =>
            {
                if (debugTextBox.Text.Length > 0)
                    debugTextBox.AppendText(Environment.NewLine, color, true);
                debugTextBox.AppendText(text, color, true);
            });
    }
}
