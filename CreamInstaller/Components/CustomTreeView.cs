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

    internal CustomTreeView() : base()
    {
        DrawMode = TreeViewDrawMode.OwnerDrawAll;
        DrawNode += new DrawTreeNodeEventHandler(DrawTreeNode);
        TreeViewNodeSorter = PlatformIdComparer.TreeNodes;
    }

    private void DrawTreeNode(object sender, DrawTreeNodeEventArgs e)
    {
        e.DrawDefault = true;
        TreeNode node = e.Node;
        if (!node.IsVisible)
            return;

        string subText = node.Name;
        Platform? platform = node.Tag as Platform?;
        string tagText = platform?.ToString();
        if (string.IsNullOrWhiteSpace(subText) || string.IsNullOrWhiteSpace(tagText) || subText == "PL")
            return;

        Graphics graphics = e.Graphics;
        Color backColor = BackColor;
        using SolidBrush brush = new(backColor);
        Font font = Font;
        Rectangle bounds = node.Bounds;

        Size tagSize = TextRenderer.MeasureText(graphics, tagText, font);
        Rectangle tagBounds = new(bounds.X + bounds.Width, bounds.Y, tagSize.Width, bounds.Height);
        graphics.FillRectangle(brush, tagBounds);
        Point tagLocation = new(tagBounds.Location.X - 1, tagBounds.Location.Y + 1);
        TextRenderer.DrawText(graphics, tagText, font, tagLocation, Color.Gray);

        Size subSize = TextRenderer.MeasureText(graphics, subText, font);
        Rectangle subBounds = new(tagBounds.X + tagBounds.Width - 4, bounds.Y, subSize.Width, bounds.Height);
        graphics.FillRectangle(brush, subBounds);
        Point subLocation = new(subBounds.Location.X - 1, subBounds.Location.Y + 1);
        TextRenderer.DrawText(graphics, subText, font, subLocation, Color.LightSlateGray);
    }
}