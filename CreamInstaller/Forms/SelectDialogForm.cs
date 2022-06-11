using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;

using CreamInstaller.Components;
using CreamInstaller.Utility;

using static CreamInstaller.Components.CustomTreeView;

namespace CreamInstaller;

internal partial class SelectDialogForm : CustomForm
{
    internal SelectDialogForm(IWin32Window owner) : base(owner) => InitializeComponent();

    private readonly List<(string platform, string id, string name)> selected = new();
    internal List<(string platform, string id, string name)> QueryUser(string groupBoxText, List<(string platform, string id, string name, bool alreadySelected)> choices)
    {
        if (!choices.Any()) return null;
        groupBox.Text = groupBoxText;
        allCheckBox.Enabled = false;
        acceptButton.Enabled = false;
        selectionTreeView.AfterCheck += OnTreeNodeChecked;
        foreach ((string platform, string id, string name, bool alreadySelected) in choices)
        {
            TreeNode node = new()
            {
                Tag = platform,
                Name = id,
                Text = name,
                Checked = alreadySelected
            };
            OnTreeNodeChecked(node);
            selectionTreeView.Nodes.Add(node);
        }
        if (!selected.Any()) OnLoad(null, null);
        allCheckBox.CheckedChanged -= OnAllCheckBoxChanged;
        allCheckBox.Checked = selectionTreeView.Nodes.Cast<TreeNode>().All(n => n.Checked);
        allCheckBox.CheckedChanged += OnAllCheckBoxChanged;
        allCheckBox.Enabled = true;
        acceptButton.Enabled = selected.Any();
        saveButton.Enabled = acceptButton.Enabled;
        loadButton.Enabled = File.Exists(ProgramData.ChoicesPath);
        OnResize(null, null);
        Resize += OnResize;
        return ShowDialog() == DialogResult.OK ? selected : null;
    }

    private void OnTreeNodeChecked(object sender, TreeViewEventArgs e)
    {
        OnTreeNodeChecked(e.Node);
        acceptButton.Enabled = selected.Any();
        saveButton.Enabled = acceptButton.Enabled;
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

    private void OnSortCheckBoxChanged(object sender, EventArgs e) => selectionTreeView.TreeViewNodeSorter = new TreeNodeSorter(sortCheckBox.Checked);

    private void OnAllCheckBoxChanged(object sender, EventArgs e)
    {
        bool shouldCheck = false;
        if (selectionTreeView.Nodes.Cast<TreeNode>().Any(n => !n.Checked))
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

    private void OnLoad(object sender, EventArgs e)
    {
        List<string> choices = ProgramData.ReadChoices();
        foreach (TreeNode node in selectionTreeView.Nodes)
        {
            node.Checked = choices.Contains(node.Name);
            OnTreeNodeChecked(node);
        }
    }

    private void OnSave(object sender, EventArgs e)
    {
        List<string> choices = new();
        foreach (TreeNode node in selectionTreeView.Nodes.Cast<TreeNode>().Where(n => n.Checked))
            choices.Add(node.Name);
        ProgramData.WriteChoices(choices);
        loadButton.Enabled = File.Exists(ProgramData.ChoicesPath);
    }
}
