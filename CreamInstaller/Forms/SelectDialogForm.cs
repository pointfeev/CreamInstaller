using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

using CreamInstaller.Components;

namespace CreamInstaller;

internal partial class SelectDialogForm : CustomForm
{
    internal SelectDialogForm(IWin32Window owner) : base(owner) => InitializeComponent();

    private readonly List<(string platform, string id, string name)> selected = new();
    internal List<(string platform, string id, string name)> QueryUser(string groupBoxText, List<(string platform, string id, string name, bool alreadySelected)> choices)
    {
        groupBox.Text = groupBoxText;
        allCheckBox.Enabled = false;
        acceptButton.Enabled = false;
        selectionTreeView.AfterCheck += OnTreeNodeChecked;
        foreach ((string platform, string id, string name, bool alreadySelected) in choices)
        {
            TreeNode node = new();
            node.Tag = platform;
            node.Name = id;
            node.Text = name;
            node.Checked = alreadySelected;
            OnTreeNodeChecked(node);
            selectionTreeView.Nodes.Add(node);
        }
        allCheckBox.CheckedChanged -= OnAllCheckBoxChanged;
        allCheckBox.Checked = selectionTreeView.Nodes.Cast<TreeNode>().All(n => n.Checked);
        allCheckBox.CheckedChanged += OnAllCheckBoxChanged;
        allCheckBox.Enabled = true;
        acceptButton.Enabled = selected.Any();
        OnResize(null, null);
        Resize += OnResize;
        return ShowDialog() == DialogResult.OK ? selected : null;
    }

    private void OnTreeNodeChecked(object sender, TreeViewEventArgs e)
    {
        OnTreeNodeChecked(e.Node);
        acceptButton.Enabled = selected.Any();
    }

    private void OnTreeNodeChecked(TreeNode node)
    {
        string id = node.Name;
        if (node.Checked)
            selected.Add((node.Tag as string, id, node.Text));
        else
            selected.RemoveAll(s => s.id == id);
        allCheckBox.CheckedChanged -= OnAllCheckBoxChanged;
        allCheckBox.Checked = selectionTreeView.Nodes.Cast<TreeNode>().All(n => n.Checked);
        allCheckBox.CheckedChanged += OnAllCheckBoxChanged;
    }

    private void OnResize(object s, EventArgs e) =>
        Text = TextRenderer.MeasureText(Program.ApplicationName, Font).Width > Size.Width - 100
            ? Program.ApplicationNameShort
            : Program.ApplicationName;

    private void OnAllCheckBoxChanged(object sender, EventArgs e)
    {
        bool shouldCheck = false;
        foreach (TreeNode node in selectionTreeView.Nodes)
            if (!node.Checked)
                shouldCheck = true;
        foreach (TreeNode node in selectionTreeView.Nodes)
        {
            node.Checked = shouldCheck;
            OnTreeNodeChecked(node);
        }
        allCheckBox.CheckedChanged -= OnAllCheckBoxChanged;
        allCheckBox.Checked = shouldCheck;
        allCheckBox.CheckedChanged += OnAllCheckBoxChanged;
    }
}
