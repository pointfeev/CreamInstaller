using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using CreamInstaller.Components;

namespace CreamInstaller.Forms;

internal sealed partial class DialogForm : CustomForm
{
    internal DialogForm(IWin32Window owner) : base(owner) => InitializeComponent();

    internal DialogResult Show(Icon descriptionIcon, string descriptionText, string acceptButtonText = "OK", string cancelButtonText = null,
        string customFormText = null, Icon customFormIcon = null)
    {
        descriptionIcon ??= Icon;
        icon.Image = descriptionIcon?.ToBitmap();
        List<LinkLabel.Link> links = new();
        for (int i = 0; i < descriptionText.Length; i++)
            if (descriptionText[i] == '[')
            {
                int textLeft = descriptionText.IndexOf("[", i, StringComparison.Ordinal);
                int textRight = descriptionText.IndexOf("]", textLeft == -1 ? i : textLeft, StringComparison.Ordinal);
                int linkLeft = descriptionText.IndexOf("(", textRight == -1 ? i : textRight, StringComparison.Ordinal);
                int linkRight = descriptionText.IndexOf(")", linkLeft == -1 ? i : linkLeft, StringComparison.Ordinal);
                if (textLeft == -1 || textRight != linkLeft - 1 || linkRight == -1)
                    continue;
                string text = descriptionText[(textLeft + 1)..textRight];
                string link = descriptionText[(linkLeft + 1)..linkRight];
                if (string.IsNullOrWhiteSpace(link))
                    link = text;
                descriptionText = descriptionText.Remove(i, linkRight + 1 - i).Insert(i, text);
                links.Add(new(i, text.Length, link));
            }
        descriptionLabel.Text = descriptionText;
        acceptButton.Text = acceptButtonText;
        if (cancelButtonText is null)
        {
            cancelButton.Enabled = false;
            cancelButton.Visible = false;
        }
        else
            cancelButton.Text = cancelButtonText;
        if (customFormText is not null)
            Text = customFormText;
        else
        {
            OnResize(null, null);
            Resize += OnResize;
        }
        if (customFormIcon is not null)
            Icon = customFormIcon;
        if (!links.Any())
            return ShowDialog();
        foreach (LinkLabel.Link link in links)
            _ = descriptionLabel.Links.Add(link);
        descriptionLabel.LinkClicked += (_, e) =>
        {
            if (e.Link != null)
                Process.Start(new ProcessStartInfo((string)e.Link.LinkData) { UseShellExecute = true });
        };
        return ShowDialog();
    }

    private void OnResize(object s, EventArgs e)
        => Text = TextRenderer.MeasureText(Program.ApplicationName, Font).Width > Size.Width - 100
            ? TextRenderer.MeasureText(Program.ApplicationNameShort, Font).Width > Size.Width - 100 ? Program.Name : Program.ApplicationNameShort
            : Program.ApplicationName;
}