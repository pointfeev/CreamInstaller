using System;
using System.Collections;
using System.Drawing;
using System.Windows.Forms;

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

    private class TreeNodeSorter : IComparer
    {
        public int Compare(object a, object b) => AppIdComparer.Comparer.Compare((a as TreeNode).Name, (b as TreeNode).Name);
    }

    internal CustomTreeView() : base()
    {
        DrawMode = TreeViewDrawMode.OwnerDrawAll;
        DrawNode += new DrawTreeNodeEventHandler(DrawTreeNode);
        TreeViewNodeSorter = new TreeNodeSorter();
    }

    private void DrawTreeNode(object sender, DrawTreeNodeEventArgs e)
    {
        e.DrawDefault = true;
        TreeNode node = e.Node;
        if (!node.IsVisible)
            return;

        Graphics graphics = e.Graphics;
        Color backColor = BackColor;
        using SolidBrush brush = new(backColor);
        Font font = Font;
        using Font subFont = new(font.FontFamily, font.SizeInPoints, FontStyle.Regular, font.Unit, font.GdiCharSet, font.GdiVerticalFont);

        string subText = node.Name;
        if (string.IsNullOrWhiteSpace(subText) || subText == "ParadoxLauncher"
            || node.Tag is null && ProgramSelection.FromId(subText) is null && ProgramSelection.GetDlcFromId(subText) is null)
            return;

        Size subSize = TextRenderer.MeasureText(graphics, subText, subFont);
        Rectangle bounds = node.Bounds;
        Rectangle subBounds = new(bounds.X + bounds.Width, bounds.Y, subSize.Width, bounds.Height);
        graphics.FillRectangle(brush, subBounds);
        Point location = subBounds.Location;
        Point subLocation = new(location.X - 1, location.Y + 1);
        TextRenderer.DrawText(graphics, subText, subFont, subLocation, Color.Gray);
    }
}