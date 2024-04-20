using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Windows.Forms;
using CreamInstaller.Forms;
using CreamInstaller.Utility;

namespace CreamInstaller.Components;

internal class CustomForm : Form
{
    internal CustomForm()
    {
        Icon = Properties.Resources.Icon;
        KeyPreview = true;
        KeyPress += OnKeyPress;
        ResizeRedraw = true;
        HelpButton = true;
        HelpButtonClicked += OnHelpButtonClicked;
    }

    internal CustomForm(IWin32Window owner) : this()
    {
        if (owner is not Form form)
            return;
        Owner = form;
        InheritLocation(form);
        SizeChanged += (_, _) => InheritLocation(form);
        form.Activated += OnActivation;
        FormClosing += (_, _) => form.Activated -= OnActivation;
        TopLevel = true;
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

    private void OnHelpButtonClicked(object sender, EventArgs args)
    {
        using DialogForm helpDialog = new(this);
        helpDialog.HelpButton = false;
        const string acidicoala = "https://github.com/acidicoala";
        string repository = $"https://github.com/{Program.RepositoryOwner}/{Program.RepositoryName}";
        _ = helpDialog.Show(SystemIcons.Information,
            "Automatically finds all installed Steam, Epic and Ubisoft games with their respective DLC-related DLL locations on the user's computer,\n"
            + "parses SteamCMD, Steam Store and Epic Games Store for user-selected games' DLCs, then provides a very simple graphical interface\n"
            + "utilizing the gathered information for the maintenance of DLC unlockers.\n\n"
            + $"The program utilizes the latest versions of [Koaloader]({acidicoala}/Koaloader), [SmokeAPI]({acidicoala}/SmokeAPI), [ScreamAPI]({acidicoala}/ScreamAPI), [Uplay R1 Unlocker]({acidicoala}/UplayR1Unlocker) and [Uplay R2 Unlocker]({acidicoala}/UplayR2Unlocker), all by\n"
            + $"the wonderful [acidicoala]({acidicoala}), and all downloaded and embedded into the program itself; no further downloads necessary on your part!\n\n"
            + "USAGE:\n" + "    1. Choose which programs and/or games the program should scan for DLC.\n"
            + "            The program automatically gathers all installed games from Steam, Epic and Ubisoft directories.\n"
            + "    2. Wait for the program to download and install SteamCMD (if you chose a Steam game).\n"
            + "    3. Wait for the program to gather and cache the chosen games' information && DLCs.\n"
            + "             May take some time on the first run; depends on how many DLCs the games you chose have.\n"
            + "    4. CAREFULLY select which games' DLCs you wish to unlock.\n"
            + "            Obviously none of the DLC unlockers are tested for every single game!\n"
            + "    5. Choose whether or not to install with Koaloader, and if so then also pick the proxy DLL to use.\n"
            + "            If the default \'version.dll\' doesn't work, then see [here](https://cs.rin.ru/forum/viewtopic.php?p=2552172#p2552172) to find one that does.\n"
            + "    6. Click the \"Generate and Install\" button.\n" +
            "    7. Click the \"OK\" button to close the program.\n"
            + "    8. If any of the DLC unlockers cause problems with any of the games you installed them on, simply go back\n"
            + "        to step 5 and select what games you wish you revert changes to, and instead click the \"Uninstall\" button this time.\n\n"
            + "NOTE: This program does not automatically download nor install actual DLC files for you; as the title of the program states, this program\n"
            + "is only a DLC Unlocker installer. Should the game you wish to unlock DLC for not already come with the DLCs installed, as is the case with\n"
            + "a good majority of games, you must find, download and install those to the game yourself. This process includes manually installing new\n"
            + "DLCs and manually updating the previously manually installed DLCs after game updates.\n\n"
            + $"For reliable and quick assistance, all bugs, crashes and other issues should be referred to the [GitHub Issues]({repository}/issues) page!\n\n"
            + $"HOWEVER: Please read the [FAQ entry]({repository}#faq--common-issues) and/or [template issue]({repository}/issues/new/choose) corresponding to your problem should one exist! Also, note that the [GitHub Issues]({repository}/issues)\n"
            + "page is not your personal assistance hotline, rather it is for genuine bugs/crashes/issues with the program itself. If you post an issue which\n"
            + "has already been explained within the FAQ, template issues, and/or within this text in general, I will just close it and you will be ignored.\n\n"
            + "SteamCMD installation and appinfo cache can be found at [C:\\ProgramData\\CreamInstaller]().\n"
            + $"The program automatically and very quickly updates from [GitHub]({repository}) by choice of the user through a dialog on startup.\n"
            + $"The program source and other information can be found on [GitHub]({repository}).");
    }

    private void OnActivation(object sender, EventArgs args) => Activate();

    internal void BringToFrontWithoutActivation()
    {
        bool topMost = TopMost;
        NativeImports.SetWindowPos(Handle, NativeImports.HWND_TOPMOST, 0, 0, 0, 0,
            NativeImports.SWP_NOACTIVATE | NativeImports.SWP_SHOWWINDOW | NativeImports.SWP_NOMOVE |
            NativeImports.SWP_NOSIZE);
        if (!topMost)
            NativeImports.SetWindowPos(Handle, NativeImports.HWND_NOTOPMOST, 0, 0, 0, 0,
                NativeImports.SWP_NOACTIVATE | NativeImports.SWP_SHOWWINDOW | NativeImports.SWP_NOMOVE |
                NativeImports.SWP_NOSIZE);
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
        if (e.KeyChar != 'S')
            return; // Shift + S
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