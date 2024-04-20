using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using CreamInstaller.Components;
using CreamInstaller.Utility;

namespace CreamInstaller.Forms;

internal sealed partial class SelectDialogForm : CustomForm
{
    private readonly List<(Platform platform, string id, string name)> selected = new();

    internal SelectDialogForm(IWin32Window owner) : base(owner)
    {
        InitializeComponent();
        selectionTreeView.TreeViewNodeSorter = PlatformIdComparer.NodeName;
    }

    internal DialogResult QueryUser(string groupBoxText,
        List<(Platform platform, string id, string name, bool alreadySelected)> potentialChoices,
        out List<(Platform platform, string id, string name)> choices)
    {
        choices = null;
        if (potentialChoices.Count < 1)
            return DialogResult.Cancel;
        groupBox.Text = groupBoxText;
        allCheckBox.Enabled = false;
        acceptButton.Enabled = false;
        selectionTreeView.AfterCheck += OnTreeNodeChecked;
        foreach ((Platform platform, string id, string name, bool alreadySelected) in potentialChoices)
        {
            TreeNode node = new() { Tag = platform, Name = id, Text = name, Checked = alreadySelected };
            OnTreeNodeChecked(node);
            _ = selectionTreeView.Nodes.Add(node);
        }

        if (selected.Count < 1)
            OnLoad(null, null);
        allCheckBox.CheckedChanged -= OnAllCheckBoxChanged;
        allCheckBox.Checked = selectionTreeView.Nodes.Cast<TreeNode>().All(n => n.Checked);
        allCheckBox.CheckedChanged += OnAllCheckBoxChanged;
        allCheckBox.Enabled = true;
        acceptButton.Enabled = selected.Count > 0;
        saveButton.Enabled = acceptButton.Enabled;
        loadButton.Enabled = ProgramData.ReadProgramChoices() is not null;
        OnResize(null, null);
        Resize += OnResize;
        choices = selected;
        return ShowDialog();
    }

    private void OnTreeNodeChecked(object sender, TreeViewEventArgs e)
    {
        OnTreeNodeChecked(e.Node);
        acceptButton.Enabled = selected.Count > 0;
        saveButton.Enabled = acceptButton.Enabled;
    }

    private void OnTreeNodeChecked(TreeNode node)
    {
        string id = node.Name;
        Platform platform = (Platform)node.Tag;
        if (node.Checked)
            selected.Add((platform, id, node.Text));
        else
            _ = selected.RemoveAll(s => s.platform == platform && s.id == id);
        allCheckBox.CheckedChanged -= OnAllCheckBoxChanged;
        allCheckBox.Checked = selectionTreeView.Nodes.Cast<TreeNode>().All(n => n.Checked);
        allCheckBox.CheckedChanged += OnAllCheckBoxChanged;
    }

    private void OnResize(object s, EventArgs e)
        => Text = TextRenderer.MeasureText(Program.ApplicationName, Font).Width > Size.Width - 100
            ? Program.ApplicationNameShort
            : Program.ApplicationName;

    private void OnSortCheckBoxChanged(object sender, EventArgs e)
        => selectionTreeView.TreeViewNodeSorter =
            sortCheckBox.Checked ? PlatformIdComparer.NodeText : PlatformIdComparer.NodeName;

    private void OnAllCheckBoxChanged(object sender, EventArgs e)
    {
        bool shouldCheck = selectionTreeView.Nodes.Cast<TreeNode>().Any(n => !n.Checked);
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
        List<(Platform platform, string id)> choices = ProgramData.ReadProgramChoices().ToList();
        if (choices.Count < 1)
            return;
        foreach (TreeNode node in selectionTreeView.Nodes)
        {
            node.Checked = choices.Any(n => n.platform == (Platform)node.Tag && n.id == node.Name);
            OnTreeNodeChecked(node);
        }
    }

    private void OnSave(object sender, EventArgs e)
    {
        ProgramData.WriteProgramChoices(selectionTreeView.Nodes.Cast<TreeNode>().Where(n => n.Checked)
            .Select(node => ((Platform)node.Tag, node.Name)));
        loadButton.Enabled = ProgramData.ReadProgramChoices() is not null;
    }
}