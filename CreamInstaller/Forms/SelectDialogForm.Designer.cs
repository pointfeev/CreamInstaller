using System.ComponentModel;
using System.Windows.Forms;

using CreamInstaller.Components;

namespace CreamInstaller.Forms
{
    partial class SelectDialogForm
    {
        private IContainer components = null;
        protected override void Dispose(bool disposing)
        {
            if (disposing && components is not null)
                components.Dispose();
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            acceptButton = new Button();
            groupBox = new GroupBox();
            allCheckBoxFlowPanel = new FlowLayoutPanel();
            allCheckBox = new CheckBox();
            sortCheckBox = new CheckBox();
            cancelButton = new Button();
            loadButton = new Button();
            saveButton = new Button();
            uninstallAllButton = new Button();
            selectionTreeView = new CustomTreeView();
            groupBox.SuspendLayout();
            allCheckBoxFlowPanel.SuspendLayout();
            SuspendLayout();
            // 
            // acceptButton
            // 
            acceptButton.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            acceptButton.AutoSize = true;
            acceptButton.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            acceptButton.DialogResult = DialogResult.OK;
            acceptButton.Location = new System.Drawing.Point(479, 243);
            acceptButton.Name = "acceptButton";
            acceptButton.Padding = new Padding(12, 0, 12, 0);
            acceptButton.Size = new System.Drawing.Size(57, 25);
            acceptButton.TabIndex = 6;
            acceptButton.Text = "OK";
            acceptButton.UseVisualStyleBackColor = true;
            // 
            // groupBox
            // 
            groupBox.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            groupBox.Controls.Add(selectionTreeView);
            groupBox.Controls.Add(allCheckBoxFlowPanel);
            groupBox.Location = new System.Drawing.Point(12, 12);
            groupBox.MinimumSize = new System.Drawing.Size(240, 40);
            groupBox.Name = "groupBox";
            groupBox.Size = new System.Drawing.Size(524, 225);
            groupBox.TabIndex = 3;
            groupBox.TabStop = false;
            groupBox.Text = "Choices";
            // 
            // selectionTreeView
            // 
            selectionTreeView.BackColor = System.Drawing.SystemColors.Control;
            selectionTreeView.BorderStyle = BorderStyle.None;
            selectionTreeView.CheckBoxes = true;
            selectionTreeView.Dock = DockStyle.Fill;
            selectionTreeView.DrawMode = TreeViewDrawMode.OwnerDrawAll;
            selectionTreeView.Location = new System.Drawing.Point(3, 19);
            selectionTreeView.Name = "selectionTreeView";
            selectionTreeView.ShowLines = false;
            selectionTreeView.ShowPlusMinus = false;
            selectionTreeView.ShowRootLines = false;
            selectionTreeView.Size = new System.Drawing.Size(518, 203);
            selectionTreeView.Sorted = true;
            selectionTreeView.TabIndex = 0;
            // 
            // allCheckBoxFlowPanel
            // 
            allCheckBoxFlowPanel.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            allCheckBoxFlowPanel.AutoSize = true;
            allCheckBoxFlowPanel.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            allCheckBoxFlowPanel.Controls.Add(allCheckBox);
            allCheckBoxFlowPanel.Location = new System.Drawing.Point(477, -1);
            allCheckBoxFlowPanel.Margin = new Padding(0);
            allCheckBoxFlowPanel.Name = "allCheckBoxFlowPanel";
            allCheckBoxFlowPanel.Size = new System.Drawing.Size(42, 19);
            allCheckBoxFlowPanel.TabIndex = 1007;
            // 
            // allCheckBox
            // 
            allCheckBox.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            allCheckBox.AutoSize = true;
            allCheckBox.Location = new System.Drawing.Point(2, 0);
            allCheckBox.Margin = new Padding(2, 0, 0, 0);
            allCheckBox.Name = "allCheckBox";
            allCheckBox.Size = new System.Drawing.Size(40, 19);
            allCheckBox.TabIndex = 1;
            allCheckBox.Text = "All";
            allCheckBox.CheckedChanged += OnAllCheckBoxChanged;
            // 
            // sortCheckBox
            // 
            sortCheckBox.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            sortCheckBox.AutoSize = true;
            sortCheckBox.Location = new System.Drawing.Point(220, 247);
            sortCheckBox.Margin = new Padding(3, 0, 0, 0);
            sortCheckBox.Name = "sortCheckBox";
            sortCheckBox.Size = new System.Drawing.Size(98, 19);
            sortCheckBox.TabIndex = 3;
            sortCheckBox.Text = "Sort By Name";
            sortCheckBox.CheckedChanged += OnSortCheckBoxChanged;
            // 
            // cancelButton
            // 
            cancelButton.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            cancelButton.AutoSize = true;
            cancelButton.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            cancelButton.DialogResult = DialogResult.Cancel;
            cancelButton.Location = new System.Drawing.Point(12, 243);
            cancelButton.Name = "cancelButton";
            cancelButton.Padding = new Padding(12, 0, 12, 0);
            cancelButton.Size = new System.Drawing.Size(77, 25);
            cancelButton.TabIndex = 2;
            cancelButton.Text = "Cancel";
            cancelButton.UseVisualStyleBackColor = true;
            // 
            // loadButton
            // 
            loadButton.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            loadButton.AutoSize = true;
            loadButton.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            loadButton.Enabled = false;
            loadButton.Location = new System.Drawing.Point(406, 243);
            loadButton.Name = "loadButton";
            loadButton.Padding = new Padding(12, 0, 12, 0);
            loadButton.Size = new System.Drawing.Size(67, 25);
            loadButton.TabIndex = 5;
            loadButton.Text = "Load";
            loadButton.UseVisualStyleBackColor = true;
            loadButton.Click += OnLoad;
            // 
            // saveButton
            // 
            saveButton.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            saveButton.AutoSize = true;
            saveButton.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            saveButton.Enabled = false;
            saveButton.Location = new System.Drawing.Point(335, 243);
            saveButton.Name = "saveButton";
            saveButton.Padding = new Padding(12, 0, 12, 0);
            saveButton.Size = new System.Drawing.Size(65, 25);
            saveButton.TabIndex = 4;
            saveButton.Text = "Save";
            saveButton.UseVisualStyleBackColor = true;
            saveButton.Click += OnSave;
            // 
            // uninstallAllButton
            // 
            uninstallAllButton.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            uninstallAllButton.AutoSize = true;
            uninstallAllButton.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            uninstallAllButton.DialogResult = DialogResult.Abort;
            uninstallAllButton.Location = new System.Drawing.Point(95, 243);
            uninstallAllButton.Name = "uninstallAllButton";
            uninstallAllButton.Padding = new Padding(12, 0, 12, 0);
            uninstallAllButton.Size = new System.Drawing.Size(104, 25);
            uninstallAllButton.TabIndex = 7;
            uninstallAllButton.Text = "Uninstall All";
            uninstallAllButton.UseVisualStyleBackColor = true;
            // 
            // SelectDialogForm
            // 
            AcceptButton = acceptButton;
            AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            AutoSize = true;
            AutoSizeMode = AutoSizeMode.GrowAndShrink;
            ClientSize = new System.Drawing.Size(548, 279);
            Controls.Add(uninstallAllButton);
            Controls.Add(sortCheckBox);
            Controls.Add(saveButton);
            Controls.Add(loadButton);
            Controls.Add(cancelButton);
            Controls.Add(acceptButton);
            Controls.Add(groupBox);
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "SelectDialogForm";
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.Manual;
            Text = "SelectDialogForm";
            groupBox.ResumeLayout(false);
            groupBox.PerformLayout();
            allCheckBoxFlowPanel.ResumeLayout(false);
            allCheckBoxFlowPanel.PerformLayout();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private Button acceptButton;
        private GroupBox groupBox;
        private CustomTreeView selectionTreeView;
        private FlowLayoutPanel allCheckBoxFlowPanel;
        private CheckBox allCheckBox;
        private Button cancelButton;
        private Button loadButton;
        private Button saveButton;
        private CheckBox sortCheckBox;
        private Button uninstallAllButton;
    }
}