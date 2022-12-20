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
            this.acceptButton = new System.Windows.Forms.Button();
            this.groupBox = new System.Windows.Forms.GroupBox();
            this.allCheckBoxFlowPanel = new System.Windows.Forms.FlowLayoutPanel();
            this.allCheckBox = new System.Windows.Forms.CheckBox();
            this.sortCheckBox = new System.Windows.Forms.CheckBox();
            this.cancelButton = new System.Windows.Forms.Button();
            this.loadButton = new System.Windows.Forms.Button();
            this.saveButton = new System.Windows.Forms.Button();
            this.selectionTreeView = new Components.CustomTreeView();
            this.groupBox.SuspendLayout();
            this.allCheckBoxFlowPanel.SuspendLayout();
            this.SuspendLayout();
            // 
            // acceptButton
            // 
            this.acceptButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.acceptButton.AutoSize = true;
            this.acceptButton.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.acceptButton.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.acceptButton.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.acceptButton.Location = new System.Drawing.Point(360, 243);
            this.acceptButton.Name = "acceptButton";
            this.acceptButton.Padding = new System.Windows.Forms.Padding(12, 0, 12, 0);
            this.acceptButton.Size = new System.Drawing.Size(61, 24);
            this.acceptButton.TabIndex = 6;
            this.acceptButton.Text = "OK";
            this.acceptButton.UseVisualStyleBackColor = true;
            // 
            // groupBox
            // 
            this.groupBox.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.groupBox.Controls.Add(this.selectionTreeView);
            this.groupBox.Controls.Add(this.allCheckBoxFlowPanel);
            this.groupBox.Location = new System.Drawing.Point(12, 12);
            this.groupBox.MinimumSize = new System.Drawing.Size(240, 40);
            this.groupBox.Name = "groupBox";
            this.groupBox.Size = new System.Drawing.Size(409, 225);
            this.groupBox.TabIndex = 3;
            this.groupBox.TabStop = false;
            this.groupBox.Text = "Choices";
            // 
            // selectionTreeView
            // 
            this.selectionTreeView.BackColor = System.Drawing.SystemColors.Control;
            this.selectionTreeView.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.selectionTreeView.CheckBoxes = true;
            this.selectionTreeView.Dock = System.Windows.Forms.DockStyle.Fill;
            this.selectionTreeView.DrawMode = System.Windows.Forms.TreeViewDrawMode.OwnerDrawAll;
            this.selectionTreeView.Location = new System.Drawing.Point(3, 19);
            this.selectionTreeView.Name = "selectionTreeView";
            this.selectionTreeView.ShowLines = false;
            this.selectionTreeView.ShowPlusMinus = false;
            this.selectionTreeView.ShowRootLines = false;
            this.selectionTreeView.Size = new System.Drawing.Size(403, 203);
            this.selectionTreeView.Sorted = true;
            this.selectionTreeView.TabIndex = 0;
            // 
            // allCheckBoxFlowPanel
            // 
            this.allCheckBoxFlowPanel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.allCheckBoxFlowPanel.AutoSize = true;
            this.allCheckBoxFlowPanel.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.allCheckBoxFlowPanel.Controls.Add(this.allCheckBox);
            this.allCheckBoxFlowPanel.Location = new System.Drawing.Point(370, -1);
            this.allCheckBoxFlowPanel.Margin = new System.Windows.Forms.Padding(0);
            this.allCheckBoxFlowPanel.Name = "allCheckBoxFlowPanel";
            this.allCheckBoxFlowPanel.Size = new System.Drawing.Size(34, 19);
            this.allCheckBoxFlowPanel.TabIndex = 1007;
            // 
            // allCheckBox
            // 
            this.allCheckBox.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.allCheckBox.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.allCheckBox.Location = new System.Drawing.Point(2, 0);
            this.allCheckBox.Margin = new System.Windows.Forms.Padding(2, 0, 0, 0);
            this.allCheckBox.Name = "allCheckBox";
            this.allCheckBox.Size = new System.Drawing.Size(32, 19);
            this.allCheckBox.TabIndex = 1;
            this.allCheckBox.Text = "All";
            this.allCheckBox.CheckedChanged += new System.EventHandler(this.OnAllCheckBoxChanged);
            // 
            // sortCheckBox
            // 
            this.sortCheckBox.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.sortCheckBox.AutoSize = true;
            this.sortCheckBox.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.sortCheckBox.Location = new System.Drawing.Point(105, 245);
            this.sortCheckBox.Margin = new System.Windows.Forms.Padding(3, 0, 0, 0);
            this.sortCheckBox.Name = "sortCheckBox";
            this.sortCheckBox.Size = new System.Drawing.Size(104, 20);
            this.sortCheckBox.TabIndex = 3;
            this.sortCheckBox.Text = "Sort By Name";
            this.sortCheckBox.CheckedChanged += new System.EventHandler(this.OnSortCheckBoxChanged);
            // 
            // cancelButton
            // 
            this.cancelButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.cancelButton.AutoSize = true;
            this.cancelButton.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.cancelButton.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.cancelButton.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.cancelButton.Location = new System.Drawing.Point(12, 243);
            this.cancelButton.Name = "cancelButton";
            this.cancelButton.Padding = new System.Windows.Forms.Padding(12, 0, 12, 0);
            this.cancelButton.Size = new System.Drawing.Size(81, 24);
            this.cancelButton.TabIndex = 2;
            this.cancelButton.Text = "Cancel";
            this.cancelButton.UseVisualStyleBackColor = true;
            // 
            // loadButton
            // 
            this.loadButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.loadButton.AutoSize = true;
            this.loadButton.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.loadButton.Enabled = false;
            this.loadButton.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.loadButton.Location = new System.Drawing.Point(283, 243);
            this.loadButton.Name = "loadButton";
            this.loadButton.Padding = new System.Windows.Forms.Padding(12, 0, 12, 0);
            this.loadButton.Size = new System.Drawing.Size(71, 24);
            this.loadButton.TabIndex = 5;
            this.loadButton.Text = "Load";
            this.loadButton.UseVisualStyleBackColor = true;
            this.loadButton.Click += new System.EventHandler(this.OnLoad);
            // 
            // saveButton
            // 
            this.saveButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.saveButton.AutoSize = true;
            this.saveButton.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.saveButton.Enabled = false;
            this.saveButton.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.saveButton.Location = new System.Drawing.Point(208, 243);
            this.saveButton.Name = "saveButton";
            this.saveButton.Padding = new System.Windows.Forms.Padding(12, 0, 12, 0);
            this.saveButton.Size = new System.Drawing.Size(69, 24);
            this.saveButton.TabIndex = 4;
            this.saveButton.Text = "Save";
            this.saveButton.UseVisualStyleBackColor = true;
            this.saveButton.Click += new System.EventHandler(this.OnSave);
            // 
            // SelectDialogForm
            // 
            this.AcceptButton = this.acceptButton;
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.AutoSize = true;
            this.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.ClientSize = new System.Drawing.Size(433, 279);
            this.Controls.Add(this.sortCheckBox);
            this.Controls.Add(this.saveButton);
            this.Controls.Add(this.loadButton);
            this.Controls.Add(this.cancelButton);
            this.Controls.Add(this.acceptButton);
            this.Controls.Add(this.groupBox);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "SelectDialogForm";
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.Manual;
            this.Text = "SelectDialogForm";
            this.groupBox.ResumeLayout(false);
            this.groupBox.PerformLayout();
            this.allCheckBoxFlowPanel.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

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
    }
}