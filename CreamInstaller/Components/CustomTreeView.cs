using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using System.Windows.Forms.VisualStyles;
using CreamInstaller.Forms;
using CreamInstaller.Resources;
using CreamInstaller.Utility;
using static CreamInstaller.Resources.Resources;

namespace CreamInstaller.Components;

internal sealed class CustomTreeView : TreeView
{
    private const string ProxyToggleString = "Proxy";

    private static readonly Color C1 = ColorTranslator.FromHtml("#FFFF99");
    private static readonly Color C2 = ColorTranslator.FromHtml("#696900");
    private static readonly Color C3 = ColorTranslator.FromHtml("#AAAA69");
    private static readonly Color C4 = ColorTranslator.FromHtml("#99FFFF");
    private static readonly Color C5 = ColorTranslator.FromHtml("#006969");
    private static readonly Color C6 = ColorTranslator.FromHtml("#69AAAA");
    private static readonly Color C7 = ColorTranslator.FromHtml("#006900");
    private static readonly Color C8 = ColorTranslator.FromHtml("#69AA69");

    private readonly Dictionary<Selection, Rectangle> checkBoxBounds = [];
    private readonly Dictionary<Selection, Rectangle> comboBoxBounds = [];

    private readonly Dictionary<TreeNode, Rectangle> selectionBounds = [];
    private SolidBrush backBrush;
    private ToolStripDropDown comboBoxDropDown;
    private Font comboBoxFont;
    private Form form;

    internal CustomTreeView()
    {
        DrawMode = TreeViewDrawMode.OwnerDrawAll;
        Invalidated += OnInvalidated;
        DrawNode += DrawTreeNode;
        Disposed += OnDisposed;
    }

    protected override void WndProc(ref Message m)
    {
        if (m.Msg == 0x203)
            m.Result = nint.Zero;
        else
            base.WndProc(ref m);
        form = FindForm();
    }

    private void OnDisposed(object sender, EventArgs e)
    {
        backBrush?.Dispose();
        backBrush = null;
        comboBoxFont?.Dispose();
        comboBoxFont = null;
        comboBoxDropDown?.Dispose();
        comboBoxDropDown = null;
    }

    private void OnInvalidated(object sender, EventArgs e)
    {
        checkBoxBounds.Clear();
        comboBoxBounds.Clear();
        selectionBounds.Clear();
    }

    private void DrawTreeNode(object sender, DrawTreeNodeEventArgs e)
    {
        e.DrawDefault = true;
        TreeNode node = e.Node;
        if (node is not { IsVisible: true })
            return;

        bool highlighted = (e.State & TreeNodeStates.Selected) == TreeNodeStates.Selected && Focused;
        Graphics graphics = e.Graphics;
        backBrush ??= new(BackColor);
        Font font = node.NodeFont ?? Font;
        Brush brush = highlighted ? SystemBrushes.Highlight : backBrush;
        Rectangle bounds = node.Bounds;
        Rectangle selectionBounds = bounds;

        Form form = FindForm();
        if (form is not SelectForm and not SelectDialogForm)
            return;

        string id = node.Name;
        Platform platform = (node.Tag as Platform?).GetValueOrDefault(Platform.None);
        DLCType dlcType = (node.Tag as DLCType?).GetValueOrDefault(DLCType.None);
        if (string.IsNullOrWhiteSpace(id) || platform is Platform.None && dlcType is DLCType.None)
            return;

        Color color = highlighted
            ? C1
            : Enabled
                ? C2
                : C3;
        string text;
        if (dlcType is not DLCType.None)
        {
            SelectionDLC dlc = SelectionDLC.FromId(dlcType, node.Parent?.Name, id);
            text = dlc?.Selection != null ? dlc.Selection.Platform.ToString() : dlcType.ToString();
        }
        else text = platform.ToString();

        Size size = TextRenderer.MeasureText(graphics, text, font);
        bounds = bounds with { X = bounds.X + bounds.Width, Width = size.Width };
        selectionBounds = new(selectionBounds.Location, selectionBounds.Size + bounds.Size with { Height = 0 });
        graphics.FillRectangle(brush, bounds);
        Point point = new(bounds.Location.X - 1, bounds.Location.Y + 1);
        TextRenderer.DrawText(graphics, text, font, point, color, TextFormatFlags.Default);

        if (platform is not Platform.Paradox)
        {
            color = highlighted
                ? C4
                : Enabled
                    ? C5
                    : C6;
            text = id;
            size = TextRenderer.MeasureText(graphics, text, font);
            const int left = -4;
            bounds = bounds with { X = bounds.X + bounds.Width + left, Width = size.Width };
            selectionBounds = new(selectionBounds.Location,
                selectionBounds.Size + new Size(bounds.Size.Width + left, 0));
            graphics.FillRectangle(brush, bounds);
            point = new(bounds.Location.X - 1, bounds.Location.Y + 1);
            TextRenderer.DrawText(graphics, text, font, point, color, TextFormatFlags.Default);
        }

        if (form is SelectForm)
        {
            Selection selection = Selection.FromId(platform, id);
            if (selection is not null && selection.CanUseProxy)
            {
                if (bounds == node.Bounds)
                {
                    size = new(4, 0);
                    bounds = bounds with { X = bounds.X + bounds.Width, Width = size.Width };
                    graphics.FillRectangle(brush, bounds);
                }

                CheckBoxState checkBoxState = selection.UseProxy
                    ? Enabled ? CheckBoxState.CheckedPressed : CheckBoxState.CheckedDisabled
                    : Enabled
                        ? CheckBoxState.UncheckedPressed
                        : CheckBoxState.UncheckedDisabled;
                size = CheckBoxRenderer.GetGlyphSize(graphics, checkBoxState);
                bounds = bounds with { X = bounds.X + bounds.Width, Width = size.Width };
                selectionBounds = new(selectionBounds.Location, selectionBounds.Size + bounds.Size with { Height = 0 });
                Rectangle checkBoxBounds = bounds;
                graphics.FillRectangle(backBrush, bounds);
                point = new(bounds.Left, bounds.Top + bounds.Height / 2 - size.Height / 2 - 1);
                CheckBoxRenderer.DrawCheckBox(graphics, point, checkBoxState);

                text = ProxyToggleString;
                size = TextRenderer.MeasureText(graphics, text, font);
                int left = 1;
                bounds = bounds with { X = bounds.X + bounds.Width, Width = size.Width + left };
                selectionBounds = new(selectionBounds.Location, selectionBounds.Size + bounds.Size with { Height = 0 });
                checkBoxBounds = new(checkBoxBounds.Location, checkBoxBounds.Size + bounds.Size with { Height = 0 });
                graphics.FillRectangle(backBrush, bounds);
                point = new(bounds.Location.X - 1 + left, bounds.Location.Y + 1);
                TextRenderer.DrawText(graphics, text, font, point, Enabled ? C7 : C8, TextFormatFlags.Default);

                this.checkBoxBounds[selection] = RectangleToClient(checkBoxBounds);

                if (selection.UseProxy)
                {
                    comboBoxFont ??= new(font.FontFamily, 6, font.Style, font.Unit, font.GdiCharSet,
                        font.GdiVerticalFont);
                    ComboBoxState comboBoxState = Enabled ? ComboBoxState.Normal : ComboBoxState.Disabled;
                    ButtonState buttonState = Enabled ? ButtonState.Normal : ButtonState.Inactive;

                    text = (selection.Proxy ?? Selection.DefaultProxy) + ".dll";
                    size = TextRenderer.MeasureText(graphics, text, comboBoxFont) + new Size(6, 0);
                    const int padding = 2;
                    bounds = new(bounds.X + bounds.Width, bounds.Y + padding / 2, size.Width, bounds.Height - padding);
                    selectionBounds = new(selectionBounds.Location,
                        selectionBounds.Size + bounds.Size with { Height = 0 });
                    Rectangle comboBoxBounds = bounds;
                    graphics.FillRectangle(backBrush, bounds);
                    if (ComboBoxRenderer.IsSupported)
                        ComboBoxRenderer.DrawTextBox(graphics, bounds, text, comboBoxFont, comboBoxState);
                    else
                    {
                        graphics.FillRectangle(SystemBrushes.ControlText, bounds);
                        ControlPaint.DrawButton(graphics, bounds, buttonState);
                        point = new(bounds.Location.X + 3 + bounds.Width / 2 - size.Width / 2,
                            bounds.Location.Y + bounds.Height / 2 - size.Height / 2);
                        TextRenderer.DrawText(graphics, text, comboBoxFont, point,
                            Enabled ? SystemColors.ControlText : SystemColors.GrayText, TextFormatFlags.Default);
                    }

                    size = new(14, 0);
                    left = -1;
                    bounds = bounds with { X = bounds.X + bounds.Width + left, Width = size.Width };
                    selectionBounds = new(selectionBounds.Location,
                        selectionBounds.Size + new Size(bounds.Size.Width + left, 0));
                    comboBoxBounds = new(comboBoxBounds.Location,
                        comboBoxBounds.Size + new Size(bounds.Size.Width + left, 0));
                    if (ComboBoxRenderer.IsSupported)
                        ComboBoxRenderer.DrawDropDownButton(graphics, bounds, comboBoxState);
                    else
                        ControlPaint.DrawComboButton(graphics, bounds, buttonState);

                    this.comboBoxBounds[selection] = RectangleToClient(comboBoxBounds);
                }
                else
                    _ = comboBoxBounds.Remove(selection);
            }
        }

        this.selectionBounds[node] = RectangleToClient(selectionBounds);
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);
        Refresh();
        Point clickPoint = PointToClient(e.Location);
        SelectForm selectForm = (form ??= FindForm()) as SelectForm;
        foreach (KeyValuePair<TreeNode, Rectangle> pair in selectionBounds)
            if (pair.Key.TreeView is null)
                _ = selectionBounds.Remove(pair.Key);
            else if (pair.Key.IsVisible && pair.Value.Contains(clickPoint))
            {
                SelectedNode = pair.Key;
                if (e.Button is MouseButtons.Right && selectForm is not null)
                    selectForm.OnNodeRightClick(pair.Key, e.Location);
                break;
            }

        if (e.Button is not MouseButtons.Left || !ComboBoxRenderer.IsSupported)
            return;

        if (comboBoxBounds.Count > 0 && selectForm is not null)
            foreach (KeyValuePair<Selection, Rectangle> pair in comboBoxBounds)
                if (!Selection.All.ContainsKey(pair.Key) || !pair.Key.CanUseProxy)
                    _ = comboBoxBounds.Remove(pair.Key);
                else if (pair.Value.Contains(clickPoint))
                {
                    HashSet<string> proxies = EmbeddedResources
                        .Where(r => r.StartsWith("Koaloader", StringComparison.Ordinal)).Select(p =>
                        {
                            p.GetProxyInfoFromIdentifier(out string proxyName, out _);
                            return proxyName;
                        }).ToHashSet();
                    comboBoxDropDown ??= new();
                    comboBoxDropDown.ShowItemToolTips = false;
                    comboBoxDropDown.Items.Clear();
                    foreach (string proxy in proxies)
                    {
                        bool canUse = true;
                        foreach ((string directory, BinaryType _) in pair.Key.ExecutableDirectories)
                        {
                            string path = directory + @"\" + proxy + ".dll";
                            if (!path.FileExists() || path.IsResourceFile(ResourceIdentifier.Koaloader))
                                continue;
                            canUse = false;
                            break;
                        }

                        if (canUse)
                            _ = comboBoxDropDown.Items.Add(new ToolStripButton(proxy + ".dll", null, (_, _) =>
                            {
                                pair.Key.Proxy = proxy == Selection.DefaultProxy ? null : proxy;
                                selectForm.OnProxyChanged();
                            }) { Font = comboBoxFont });
                    }

                    comboBoxDropDown.Show(this, PointToScreen(new(pair.Value.Left, pair.Value.Bottom - 1)));
                    break;
                }

        foreach (KeyValuePair<Selection, Rectangle> pair in checkBoxBounds)
            if (!Selection.All.ContainsKey(pair.Key) || !pair.Key.CanUseProxy)
                _ = checkBoxBounds.Remove(pair.Key);
            else if (pair.Value.Contains(clickPoint))
            {
                pair.Key.UseProxy = !pair.Key.UseProxy;
                selectForm?.OnProxyChanged();
                break;
            }
    }
}