using System;
using System.Drawing;
using System.Windows.Forms;
using System.Windows.Forms.VisualStyles;

namespace CreamInstaller
{
    public class CustomTreeView : TreeView
    {
        protected override void WndProc(ref Message m)
        {
            if (m.Msg == 0x203)
            {
                m.Result = IntPtr.Zero;
            }
            else
            {
                base.WndProc(ref m);
            }
        }

        public CustomTreeView() : base()
        {
            //DrawMode = TreeViewDrawMode.OwnerDrawAll;
            //DrawNode += new DrawTreeNodeEventHandler(DrawTreeNode);

            closedGlyphRenderer = new(VisualStyleElement.TreeView.Glyph.Closed);
            openedGlyphRenderer = new(VisualStyleElement.TreeView.Glyph.Opened);
        }

        private readonly VisualStyleRenderer closedGlyphRenderer;
        private readonly VisualStyleRenderer openedGlyphRenderer;

        private void DrawTreeNode(object sender, DrawTreeNodeEventArgs e)
        {
            if (!e.Node.IsVisible)
            {
                return;
            }

            e.Graphics.FillRectangle(new SolidBrush(BackColor), e.Bounds);

            int startX = e.Bounds.X + (e.Node.Parent is null ? 22 : 41);
            int startY = e.Bounds.Y;

            if (e.Node.Parent is null && e.Node.Nodes.Count > 0)
            {
                if (e.Node.IsExpanded)
                {
                    openedGlyphRenderer.DrawBackground(e.Graphics, new(e.Bounds.X + startX / 2 - 8, startY, 16, 16));
                }
                else
                {
                    closedGlyphRenderer.DrawBackground(e.Graphics, new(e.Bounds.X + startX / 2 - 8, startY, 16, 16));
                }
            }

            CheckBoxState checkBoxState = e.Node.TreeView.Enabled
                    ? (e.Node.Checked ? CheckBoxState.CheckedNormal : CheckBoxState.UncheckedNormal)
                    : (e.Node.Checked ? CheckBoxState.CheckedDisabled : CheckBoxState.UncheckedDisabled);
            CheckBoxRenderer.DrawCheckBox(e.Graphics, new(startX, startY + 1), checkBoxState);

            TextRenderer.DrawText(e.Graphics, e.Node.Text, e.Node.NodeFont, e.Node.Bounds.Location, Color.Black);
        }
    }
}