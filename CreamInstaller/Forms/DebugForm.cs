using CreamInstaller.Components;
using CreamInstaller.Utility;

using System;
using System.Drawing;
using System.Windows.Forms;

namespace CreamInstaller;

internal partial class DebugForm : CustomForm
{
    internal static DebugForm current;
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

    internal DebugForm()
    {
        InitializeComponent();
        debugTextBox.BackColor = LogTextBox.Background;
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

    private Form attachedForm;

    internal void Attach(Form form)
    {
        if (attachedForm is not null)
        {
            attachedForm.Activated -= OnChange;
            attachedForm.LocationChanged -= OnChange;
            attachedForm.SizeChanged -= OnChange;
        }
        attachedForm = form;
        attachedForm.Activated += OnChange;
        attachedForm.LocationChanged += OnChange;
        attachedForm.SizeChanged += OnChange;
        UpdateAttachment();
    }

    internal void OnChange(object sender, EventArgs args) => UpdateAttachment();

    internal void UpdateAttachment()
    {
        if (attachedForm is null)
            return;
        Size = new(Size.Width, attachedForm.Size.Height);
        Location = new(attachedForm.Right, attachedForm.Top);
        Show();
        BringToFrontWithoutActivation();
    }

    internal void Log(string text) => Log(text, LogTextBox.Error);

    internal void Log(string text, Color color)
    {
        if (!debugTextBox.Disposing && !debugTextBox.IsDisposed)
        {
            debugTextBox.Invoke(() =>
            {
                if (debugTextBox.Text.Length > 0)
                    debugTextBox.AppendText(Environment.NewLine, color, scroll: true);
                debugTextBox.AppendText(text, color, scroll: true);
            });
        }
    }
}
