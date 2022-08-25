using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using System.Windows.Forms.VisualStyles;

namespace CreamInstaller.Components;

internal class CustomTreeView : TreeView
{
    protected override void WndProc(ref Message m)
    {
        if (m.Msg == 0x203)
            m.Result = IntPtr.Zero;
        else
            base.WndProc(ref m);
    }

    internal CustomTreeView() : base()
    {
        DrawMode = TreeViewDrawMode.OwnerDrawAll;
        DrawNode += new DrawTreeNodeEventHandler(DrawTreeNode);
        TreeViewNodeSorter = PlatformIdComparer.NodeName;
    }

    private readonly Dictionary<TreeNode, Rectangle> selectionBounds = new();
    private readonly Dictionary<ProgramSelection, Rectangle> checkBoxBounds = new();
    private const string koaloaderToggleString = "Koaloader";

    private void DrawTreeNode(object sender, DrawTreeNodeEventArgs e)
    {
        e.DrawDefault = true;
        TreeNode node = e.Node;
        if (!node.IsVisible)
            return;
        bool highlighted = node.IsSelected && SelectedNode == node && ContainsFocus;

        Form form = FindForm();
        if (form is not SelectForm and not SelectDialogForm)
            return;

        string platformId = node.Name;
        Platform platform = (node.Tag as Platform?).GetValueOrDefault(Platform.None);
        if (string.IsNullOrWhiteSpace(platformId) || platform is Platform.None)
            return;

        Graphics graphics = e.Graphics;
        using SolidBrush backBrush = new(BackColor);
        using SolidBrush highlightBrush = new(SystemColors.Highlight);
        Font font = Font;
        Size lastSize;
        Rectangle lastBounds = node.Bounds;
        Rectangle selectionBounds = lastBounds;
        Point lastPoint;

        string tagText = platform.ToString();
        lastSize = TextRenderer.MeasureText(graphics, tagText, font);
        lastBounds = new(lastBounds.X + lastBounds.Width, lastBounds.Y, lastSize.Width, lastBounds.Height);
        selectionBounds = new(selectionBounds.Location, selectionBounds.Size + new Size(lastBounds.Size.Width, 0));
        graphics.FillRectangle(highlighted ? highlightBrush : backBrush, lastBounds);
        lastPoint = new(lastBounds.Location.X - 1, lastBounds.Location.Y + 1);
        TextRenderer.DrawText(graphics, tagText, font, lastPoint, highlighted ? ColorTranslator.FromHtml("#FFFF99") : Enabled ? ColorTranslator.FromHtml("#696900") : ColorTranslator.FromHtml("#AAAA69"));

        if (platform is not Platform.Paradox)
        {
            string subText = platformId.ToString();
            lastSize = TextRenderer.MeasureText(graphics, subText, font);
            lastBounds = new(lastBounds.X + lastBounds.Width - 4, lastBounds.Y, lastSize.Width, lastBounds.Height);
            selectionBounds = new(selectionBounds.Location, selectionBounds.Size + new Size(lastBounds.Size.Width - 4, 0));
            graphics.FillRectangle(highlighted ? highlightBrush : backBrush, lastBounds);
            lastPoint = new(lastBounds.Location.X - 1, lastBounds.Location.Y + 1);
            TextRenderer.DrawText(graphics, subText, font, lastPoint, highlighted ? ColorTranslator.FromHtml("#99FFFF") : Enabled ? ColorTranslator.FromHtml("#006969") : ColorTranslator.FromHtml("#69AAAA"));
        }

        if (form is SelectForm)
        {
            ProgramSelection selection = ProgramSelection.FromPlatformId(platform, platformId);
            if (selection is not null)
            {
                if (lastBounds == node.Bounds)
                {
                    lastSize = new(4, 0);
                    lastBounds = new(lastBounds.X + lastBounds.Width, lastBounds.Y, lastSize.Width, lastBounds.Height);
                    graphics.FillRectangle(highlighted ? highlightBrush : backBrush, lastBounds);
                }

                CheckBoxState checkBoxState = selection.Koaloader ? CheckBoxState.CheckedNormal : CheckBoxState.UncheckedNormal;
                lastSize = CheckBoxRenderer.GetGlyphSize(graphics, checkBoxState);
                lastBounds = new(lastBounds.X + lastBounds.Width, lastBounds.Y, lastSize.Width, lastBounds.Height);
                selectionBounds = new(selectionBounds.Location, selectionBounds.Size + new Size(lastBounds.Size.Width, 0));
                Rectangle checkBoxBounds = lastBounds;
                graphics.FillRectangle(backBrush, lastBounds);
                lastPoint = new(lastBounds.Left, lastBounds.Top + lastBounds.Height / 2 - lastSize.Height / 2 - 1);
                CheckBoxRenderer.DrawCheckBox(graphics, lastPoint, checkBoxState);

                lastSize = TextRenderer.MeasureText(graphics, koaloaderToggleString, font);
                int left = 1;
                lastBounds = new(lastBounds.X + lastBounds.Width, lastBounds.Y, lastSize.Width + left, lastBounds.Height);
                selectionBounds = new(selectionBounds.Location, selectionBounds.Size + new Size(lastBounds.Size.Width + left, 0));
                checkBoxBounds = new(checkBoxBounds.Location, checkBoxBounds.Size + new Size(lastBounds.Size.Width + left, 0));
                graphics.FillRectangle(backBrush, lastBounds);
                lastPoint = new(lastBounds.Location.X - 1 + left, lastBounds.Location.Y + 1);
                TextRenderer.DrawText(graphics, koaloaderToggleString, font, lastPoint, Enabled ? ColorTranslator.FromHtml("#006900") : ColorTranslator.FromHtml("#69AA69"));

                this.checkBoxBounds[selection] = RectangleToClient(checkBoxBounds);
            }
        }

        this.selectionBounds[node] = RectangleToClient(selectionBounds);
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);
        Refresh();
        Point clickPoint = PointToClient(e.Location);
        foreach (KeyValuePair<TreeNode, Rectangle> pair in selectionBounds)
            if (pair.Key.IsVisible && pair.Value.Contains(clickPoint))
            {
                SelectedNode = pair.Key;
                if (e.Button is MouseButtons.Right && FindForm() is SelectForm selectForm)
                    selectForm.OnNodeRightClick(pair.Key, e.Location);
                break;
            }
        if (e.Button is MouseButtons.Left)
        {
            bool invalidate = false;
            foreach (KeyValuePair<ProgramSelection, Rectangle> pair in checkBoxBounds)
                if (pair.Value.Contains(clickPoint))
                {
                    pair.Key.Koaloader = !pair.Key.Koaloader;
                    invalidate = true;
                    break;
                }
            if (invalidate)
            {
                Invalidate();
                if (FindForm() is SelectForm selectForm)
                {
                    CheckBox koaloaderAllCheckBox = selectForm.KoaloaderAllCheckBox();
                    koaloaderAllCheckBox.CheckedChanged -= selectForm.OnKoaloaderAllCheckBoxChanged;
                    koaloaderAllCheckBox.Checked = ProgramSelection.AllSafe.TrueForAll(selection => selection.Koaloader);
                    koaloaderAllCheckBox.CheckedChanged += selectForm.OnKoaloaderAllCheckBoxChanged;
                }
            }
        }
    }
}