
using System.Windows.Forms;

namespace CreamInstaller
{
    partial class SelectForm
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.installButton = new System.Windows.Forms.Button();
            this.cancelButton = new System.Windows.Forms.Button();
            this.label1 = new System.Windows.Forms.Label();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.flowLayoutPanel4 = new System.Windows.Forms.FlowLayoutPanel();
            this.koaloaderAllCheckBox = new System.Windows.Forms.CheckBox();
            this.flowLayoutPanel3 = new System.Windows.Forms.FlowLayoutPanel();
            this.noneFoundLabel = new System.Windows.Forms.Label();
            this.flowLayoutPanel1 = new System.Windows.Forms.FlowLayoutPanel();
            this.blockedGamesCheckBox = new System.Windows.Forms.CheckBox();
            this.blockProtectedHelpButton = new System.Windows.Forms.Button();
            this.selectionTreeView = new Components.CustomTreeView();
            this.flowLayoutPanel2 = new System.Windows.Forms.FlowLayoutPanel();
            this.allCheckBox = new System.Windows.Forms.CheckBox();
            this.progressBar = new System.Windows.Forms.ProgressBar();
            this.progressLabel = new System.Windows.Forms.Label();
            this.scanButton = new System.Windows.Forms.Button();
            this.uninstallButton = new System.Windows.Forms.Button();
            this.progressLabelGames = new System.Windows.Forms.Label();
            this.progressLabelDLCs = new System.Windows.Forms.Label();
            this.sortCheckBox = new System.Windows.Forms.CheckBox();
            this.contextMenuStrip = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.groupBox1.SuspendLayout();
            this.flowLayoutPanel4.SuspendLayout();
            this.flowLayoutPanel1.SuspendLayout();
            this.flowLayoutPanel2.SuspendLayout();
            this.SuspendLayout();
            // 
            // installButton
            // 
            this.installButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.installButton.AutoSize = true;
            this.installButton.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.installButton.Enabled = false;
            this.installButton.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.installButton.Location = new System.Drawing.Point(420, 325);
            this.installButton.Name = "installButton";
            this.installButton.Padding = new System.Windows.Forms.Padding(12, 0, 12, 0);
            this.installButton.Size = new System.Drawing.Size(149, 24);
            this.installButton.TabIndex = 10004;
            this.installButton.Text = "Generate and Install";
            this.installButton.UseVisualStyleBackColor = true;
            this.installButton.Click += new System.EventHandler(this.OnInstall);
            // 
            // cancelButton
            // 
            this.cancelButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.cancelButton.AutoSize = true;
            this.cancelButton.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.cancelButton.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.cancelButton.Location = new System.Drawing.Point(12, 325);
            this.cancelButton.Name = "cancelButton";
            this.cancelButton.Padding = new System.Windows.Forms.Padding(12, 0, 12, 0);
            this.cancelButton.Size = new System.Drawing.Size(81, 24);
            this.cancelButton.TabIndex = 10000;
            this.cancelButton.Text = "Cancel";
            this.cancelButton.UseVisualStyleBackColor = true;
            this.cancelButton.Click += new System.EventHandler(this.OnCancel);
            // 
            // label1
            // 
            this.label1.Location = new System.Drawing.Point(0, 0);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(100, 23);
            this.label1.TabIndex = 0;
            // 
            // groupBox1
            // 
            this.groupBox1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.groupBox1.Controls.Add(this.flowLayoutPanel4);
            this.groupBox1.Controls.Add(this.flowLayoutPanel3);
            this.groupBox1.Controls.Add(this.noneFoundLabel);
            this.groupBox1.Controls.Add(this.flowLayoutPanel1);
            this.groupBox1.Controls.Add(this.selectionTreeView);
            this.groupBox1.Controls.Add(this.flowLayoutPanel2);
            this.groupBox1.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.groupBox1.Location = new System.Drawing.Point(12, 12);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(560, 239);
            this.groupBox1.TabIndex = 8;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Programs / Games";
            // 
            // flowLayoutPanel4
            // 
            this.flowLayoutPanel4.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.flowLayoutPanel4.AutoSize = true;
            this.flowLayoutPanel4.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.flowLayoutPanel4.Controls.Add(this.koaloaderAllCheckBox);
            this.flowLayoutPanel4.Location = new System.Drawing.Point(430, -1);
            this.flowLayoutPanel4.Margin = new System.Windows.Forms.Padding(0);
            this.flowLayoutPanel4.Name = "flowLayoutPanel4";
            this.flowLayoutPanel4.Size = new System.Drawing.Size(73, 19);
            this.flowLayoutPanel4.TabIndex = 10005;
            this.flowLayoutPanel4.WrapContents = false;
            // 
            // koaloaderAllCheckBox
            // 
            this.koaloaderAllCheckBox.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.koaloaderAllCheckBox.Checked = true;
            this.koaloaderAllCheckBox.CheckState = System.Windows.Forms.CheckState.Checked;
            this.koaloaderAllCheckBox.Enabled = false;
            this.koaloaderAllCheckBox.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.koaloaderAllCheckBox.Location = new System.Drawing.Point(2, 0);
            this.koaloaderAllCheckBox.Margin = new System.Windows.Forms.Padding(2, 0, 0, 0);
            this.koaloaderAllCheckBox.Name = "koaloaderAllCheckBox";
            this.koaloaderAllCheckBox.Size = new System.Drawing.Size(71, 19);
            this.koaloaderAllCheckBox.TabIndex = 4;
            this.koaloaderAllCheckBox.Text = "Koaloader";
            this.koaloaderAllCheckBox.CheckedChanged += new System.EventHandler(this.OnKoaloaderAllCheckBoxChanged);
            // 
            // flowLayoutPanel3
            // 
            this.flowLayoutPanel3.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.flowLayoutPanel3.AutoSize = true;
            this.flowLayoutPanel3.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.flowLayoutPanel3.Location = new System.Drawing.Point(500, -1);
            this.flowLayoutPanel3.Margin = new System.Windows.Forms.Padding(0);
            this.flowLayoutPanel3.Name = "flowLayoutPanel3";
            this.flowLayoutPanel3.Size = new System.Drawing.Size(0, 0);
            this.flowLayoutPanel3.TabIndex = 1007;
            this.flowLayoutPanel3.WrapContents = false;
            // 
            // noneFoundLabel
            // 
            this.noneFoundLabel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.noneFoundLabel.Location = new System.Drawing.Point(3, 19);
            this.noneFoundLabel.Name = "noneFoundLabel";
            this.noneFoundLabel.Size = new System.Drawing.Size(554, 217);
            this.noneFoundLabel.TabIndex = 1002;
            this.noneFoundLabel.Text = "No applicable programs nor games were found on your computer!";
            this.noneFoundLabel.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.noneFoundLabel.Visible = false;
            // 
            // flowLayoutPanel1
            // 
            this.flowLayoutPanel1.Anchor = System.Windows.Forms.AnchorStyles.Top;
            this.flowLayoutPanel1.AutoSize = true;
            this.flowLayoutPanel1.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.flowLayoutPanel1.Controls.Add(this.blockedGamesCheckBox);
            this.flowLayoutPanel1.Controls.Add(this.blockProtectedHelpButton);
            this.flowLayoutPanel1.Location = new System.Drawing.Point(125, -1);
            this.flowLayoutPanel1.Margin = new System.Windows.Forms.Padding(0);
            this.flowLayoutPanel1.Name = "flowLayoutPanel1";
            this.flowLayoutPanel1.Size = new System.Drawing.Size(162, 20);
            this.flowLayoutPanel1.TabIndex = 1005;
            this.flowLayoutPanel1.WrapContents = false;
            // 
            // blockedGamesCheckBox
            // 
            this.blockedGamesCheckBox.Checked = true;
            this.blockedGamesCheckBox.CheckState = System.Windows.Forms.CheckState.Checked;
            this.blockedGamesCheckBox.Enabled = false;
            this.blockedGamesCheckBox.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.blockedGamesCheckBox.Location = new System.Drawing.Point(2, 0);
            this.blockedGamesCheckBox.Margin = new System.Windows.Forms.Padding(2, 0, 0, 0);
            this.blockedGamesCheckBox.Name = "blockedGamesCheckBox";
            this.blockedGamesCheckBox.Size = new System.Drawing.Size(140, 20);
            this.blockedGamesCheckBox.TabIndex = 1;
            this.blockedGamesCheckBox.Text = "Block Protected Games";
            this.blockedGamesCheckBox.UseVisualStyleBackColor = true;
            this.blockedGamesCheckBox.CheckedChanged += new System.EventHandler(this.OnBlockProtectedGamesCheckBoxChanged);
            // 
            // blockProtectedHelpButton
            // 
            this.blockProtectedHelpButton.Enabled = false;
            this.blockProtectedHelpButton.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.blockProtectedHelpButton.Font = new System.Drawing.Font("Segoe UI", 7F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.blockProtectedHelpButton.Location = new System.Drawing.Point(142, 0);
            this.blockProtectedHelpButton.Margin = new System.Windows.Forms.Padding(0, 0, 1, 0);
            this.blockProtectedHelpButton.Name = "blockProtectedHelpButton";
            this.blockProtectedHelpButton.Size = new System.Drawing.Size(19, 19);
            this.blockProtectedHelpButton.TabIndex = 2;
            this.blockProtectedHelpButton.Text = "?";
            this.blockProtectedHelpButton.UseVisualStyleBackColor = true;
            this.blockProtectedHelpButton.Click += new System.EventHandler(this.OnBlockProtectedGamesHelpButtonClicked);
            // 
            // selectionTreeView
            // 
            this.selectionTreeView.BackColor = System.Drawing.SystemColors.Control;
            this.selectionTreeView.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.selectionTreeView.CheckBoxes = true;
            this.selectionTreeView.Dock = System.Windows.Forms.DockStyle.Fill;
            this.selectionTreeView.DrawMode = System.Windows.Forms.TreeViewDrawMode.OwnerDrawAll;
            this.selectionTreeView.Enabled = false;
            this.selectionTreeView.FullRowSelect = true;
            this.selectionTreeView.Location = new System.Drawing.Point(3, 19);
            this.selectionTreeView.Name = "selectionTreeView";
            this.selectionTreeView.Size = new System.Drawing.Size(554, 217);
            this.selectionTreeView.Sorted = true;
            this.selectionTreeView.TabIndex = 1001;
            // 
            // flowLayoutPanel2
            // 
            this.flowLayoutPanel2.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.flowLayoutPanel2.AutoSize = true;
            this.flowLayoutPanel2.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.flowLayoutPanel2.Controls.Add(this.allCheckBox);
            this.flowLayoutPanel2.Location = new System.Drawing.Point(520, -1);
            this.flowLayoutPanel2.Margin = new System.Windows.Forms.Padding(0);
            this.flowLayoutPanel2.Name = "flowLayoutPanel2";
            this.flowLayoutPanel2.Size = new System.Drawing.Size(34, 19);
            this.flowLayoutPanel2.TabIndex = 1006;
            this.flowLayoutPanel2.WrapContents = false;
            // 
            // allCheckBox
            // 
            this.allCheckBox.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.allCheckBox.Checked = true;
            this.allCheckBox.CheckState = System.Windows.Forms.CheckState.Checked;
            this.allCheckBox.Enabled = false;
            this.allCheckBox.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.allCheckBox.Location = new System.Drawing.Point(2, 0);
            this.allCheckBox.Margin = new System.Windows.Forms.Padding(2, 0, 0, 0);
            this.allCheckBox.Name = "allCheckBox";
            this.allCheckBox.Size = new System.Drawing.Size(32, 19);
            this.allCheckBox.TabIndex = 4;
            this.allCheckBox.Text = "All";
            this.allCheckBox.CheckedChanged += new System.EventHandler(this.OnAllCheckBoxChanged);
            // 
            // progressBar
            // 
            this.progressBar.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.progressBar.Location = new System.Drawing.Point(12, 296);
            this.progressBar.Name = "progressBar";
            this.progressBar.Size = new System.Drawing.Size(560, 23);
            this.progressBar.TabIndex = 9;
            // 
            // progressLabel
            // 
            this.progressLabel.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.progressLabel.Location = new System.Drawing.Point(12, 254);
            this.progressLabel.Name = "progressLabel";
            this.progressLabel.Size = new System.Drawing.Size(560, 15);
            this.progressLabel.TabIndex = 10;
            this.progressLabel.Text = "Gathering and caching your applicable games and their DLCs . . . 0%";
            // 
            // scanButton
            // 
            this.scanButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.scanButton.AutoSize = true;
            this.scanButton.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.scanButton.Enabled = false;
            this.scanButton.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.scanButton.Location = new System.Drawing.Point(235, 325);
            this.scanButton.Name = "scanButton";
            this.scanButton.Padding = new System.Windows.Forms.Padding(12, 0, 12, 0);
            this.scanButton.Size = new System.Drawing.Size(82, 24);
            this.scanButton.TabIndex = 10002;
            this.scanButton.Text = "Rescan";
            this.scanButton.UseVisualStyleBackColor = true;
            this.scanButton.Click += new System.EventHandler(this.OnScan);
            // 
            // uninstallButton
            // 
            this.uninstallButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.uninstallButton.AutoSize = true;
            this.uninstallButton.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.uninstallButton.Enabled = false;
            this.uninstallButton.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.uninstallButton.Location = new System.Drawing.Point(323, 325);
            this.uninstallButton.Name = "uninstallButton";
            this.uninstallButton.Padding = new System.Windows.Forms.Padding(12, 0, 12, 0);
            this.uninstallButton.Size = new System.Drawing.Size(91, 24);
            this.uninstallButton.TabIndex = 10003;
            this.uninstallButton.Text = "Uninstall";
            this.uninstallButton.UseVisualStyleBackColor = true;
            this.uninstallButton.Click += new System.EventHandler(this.OnUninstall);
            // 
            // progressLabelGames
            // 
            this.progressLabelGames.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.progressLabelGames.Font = new System.Drawing.Font("Segoe UI", 7F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.progressLabelGames.Location = new System.Drawing.Point(12, 269);
            this.progressLabelGames.Name = "progressLabelGames";
            this.progressLabelGames.Size = new System.Drawing.Size(560, 12);
            this.progressLabelGames.TabIndex = 11;
            this.progressLabelGames.Text = "Remaining games (2): Game 1, Game 2";
            // 
            // progressLabelDLCs
            // 
            this.progressLabelDLCs.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.progressLabelDLCs.Font = new System.Drawing.Font("Segoe UI", 7F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.progressLabelDLCs.Location = new System.Drawing.Point(12, 281);
            this.progressLabelDLCs.Name = "progressLabelDLCs";
            this.progressLabelDLCs.Size = new System.Drawing.Size(560, 12);
            this.progressLabelDLCs.TabIndex = 12;
            this.progressLabelDLCs.Text = "Remaining DLC (2): 123456, 654321";
            // 
            // sortCheckBox
            // 
            this.sortCheckBox.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.sortCheckBox.AutoSize = true;
            this.sortCheckBox.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.sortCheckBox.Location = new System.Drawing.Point(120, 328);
            this.sortCheckBox.Margin = new System.Windows.Forms.Padding(3, 0, 0, 0);
            this.sortCheckBox.Name = "sortCheckBox";
            this.sortCheckBox.Size = new System.Drawing.Size(104, 20);
            this.sortCheckBox.TabIndex = 10001;
            this.sortCheckBox.Text = "Sort By Name";
            this.sortCheckBox.CheckedChanged += new System.EventHandler(this.OnSortCheckBoxChanged);
            // 
            // contextMenuStrip
            // 
            this.contextMenuStrip.AllowMerge = false;
            this.contextMenuStrip.LayoutStyle = System.Windows.Forms.ToolStripLayoutStyle.VerticalStackWithOverflow;
            this.contextMenuStrip.Name = "contextMenuStrip";
            this.contextMenuStrip.ShowItemToolTips = false;
            this.contextMenuStrip.Size = new System.Drawing.Size(61, 4);
            this.contextMenuStrip.TabStop = true;
            // 
            // SelectForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.AutoSize = true;
            this.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.ClientSize = new System.Drawing.Size(584, 361);
            this.Controls.Add(this.sortCheckBox);
            this.Controls.Add(this.progressLabelDLCs);
            this.Controls.Add(this.progressLabelGames);
            this.Controls.Add(this.uninstallButton);
            this.Controls.Add(this.scanButton);
            this.Controls.Add(this.groupBox1);
            this.Controls.Add(this.progressBar);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.cancelButton);
            this.Controls.Add(this.installButton);
            this.Controls.Add(this.progressLabel);
            this.DoubleBuffered = true;
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "SelectForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "SelectForm";
            this.Load += new System.EventHandler(this.OnLoad);
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.flowLayoutPanel4.ResumeLayout(false);
            this.flowLayoutPanel1.ResumeLayout(false);
            this.flowLayoutPanel2.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button installButton;
        private System.Windows.Forms.Button cancelButton;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.ProgressBar progressBar;
        private System.Windows.Forms.Label progressLabel;
        private System.Windows.Forms.CheckBox allCheckBox;
        private Button scanButton;
        private Label noneFoundLabel;
        private Components.CustomTreeView selectionTreeView;
        private CheckBox blockedGamesCheckBox;
        private Button blockProtectedHelpButton;
        private FlowLayoutPanel flowLayoutPanel1;
        private FlowLayoutPanel flowLayoutPanel2;
        private Button uninstallButton;
        private Label progressLabelGames;
        private Label progressLabelDLCs;
        private CheckBox sortCheckBox;
        private FlowLayoutPanel flowLayoutPanel3;
        private ContextMenuStrip contextMenuStrip;
        private FlowLayoutPanel flowLayoutPanel4;
        private CheckBox koaloaderAllCheckBox;
    }
}

