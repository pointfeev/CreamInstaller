using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace CreamInstaller.Components;

internal class CustomForm : Form
{
    internal CustomForm() : base()
    {
        Icon = Properties.Resources.Icon;
        KeyPreview = true;
        KeyPress += OnKeyPress;
        ResizeRedraw = true;
    }

    internal CustomForm(IWin32Window owner) : this()
    {
        if (owner is Form form)
        {
            Owner = form;
            InheritLocation(form);
            SizeChanged += (s, e) => InheritLocation(form);
            form.Activated += OnActivation;
            FormClosing += (s, e) => form.Activated -= OnActivation;
        }
    }

    internal void OnActivation(object sender, EventArgs args) => Activate();

    public static readonly IntPtr HWND_NOTOPMOST = new(-2);
    public static readonly IntPtr HWND_TOPMOST = new(-1);
    public const short SWP_NOACTIVATE = 0x0010;
    public const short SWP_SHOWWINDOW = 0x0040;
    public const short SWP_NOMOVE = 0x0002;
    public const short SWP_NOSIZE = 0x0001;

    [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    internal static extern void SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int x, int y, int cx, int cy, uint uFlags);

    internal void BringToFrontWithoutActivation()
    {
        bool topMost = TopMost;
        SetWindowPos(Handle, HWND_TOPMOST, 0, 0, 0, 0, SWP_NOACTIVATE | SWP_SHOWWINDOW | SWP_NOMOVE | SWP_NOSIZE);
        if (!topMost)
            SetWindowPos(Handle, HWND_NOTOPMOST, 0, 0, 0, 0, SWP_NOACTIVATE | SWP_SHOWWINDOW | SWP_NOMOVE | SWP_NOSIZE);
    }

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
        if (fromForm is null)
            return;
        int X = fromForm.Location.X + fromForm.Size.Width / 2 - Size.Width / 2;
        int Y = fromForm.Location.Y + fromForm.Size.Height / 2 - Size.Height / 2;
        Location = new(X, Y);
    }

    private void OnKeyPress(object s, KeyPressEventArgs e)
    {
        if (e.KeyChar != 'S') return; // Shift + S
        UpdateBounds();
        Rectangle bounds = Bounds;
        using Bitmap bitmap = new(Size.Width - 14, Size.Height - 7);
        using Graphics graphics = Graphics.FromImage(bitmap);
        graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
        using EncoderParameters encoding = new(1);
        using EncoderParameter encoderParam = new(Encoder.Quality, 100L);
        encoding.Param[0] = encoderParam;
        graphics.CopyFromScreen(new(bounds.Left + 7, bounds.Top), Point.Empty, new(Size.Width - 14, Size.Height - 7));
        Clipboard.SetImage(bitmap);
        e.Handled = true;
    }
}
