using System;
using System.Windows.Forms;
using System.Drawing;
using System.ComponentModel;
using CreamInstaller.Components;

namespace CreamInstaller
{
    partial class SelectForm
    {
        private IContainer components = null;
        protected override void Dispose(bool disposing)
        {
            if (disposing && components is not null)
                components.Dispose();
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        private void InitializeComponent()
        {
            this.installButton = new();
            this.cancelButton = new();
            this.programsGroupBox = new();
            this.koaloaderFlowPanel = new();
            this.koaloaderAllCheckBox = new();
            this.noneFoundLabel = new();
            this.blockedGamesFlowPanel = new();
            this.blockedGamesCheckBox = new();
            this.blockProtectedHelpButton = new();
            this.selectionTreeView = new();
            this.allCheckBoxLayoutPanel = new();
            this.allCheckBox = new();
            this.progressBar = new();
            this.progressLabel = new();
            this.scanButton = new();
            this.uninstallButton = new();
            this.progressLabelGames = new();
            this.progressLabelDLCs = new();
            this.sortCheckBox = new();
            this.programsGroupBox.SuspendLayout();
            this.koaloaderFlowPanel.SuspendLayout();
            this.blockedGamesFlowPanel.SuspendLayout();
            this.allCheckBoxLayoutPanel.SuspendLayout();
            this.SuspendLayout();
            // 
            // installButton
            // 
            this.installButton.Anchor = ((AnchorStyles)((AnchorStyles.Bottom | AnchorStyles.Right)));
            this.installButton.AutoSize = true;
            this.installButton.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            this.installButton.Enabled = false;
            this.installButton.FlatStyle = FlatStyle.System;
            this.installButton.Location = new Point(420, 325);
            this.installButton.Name = "installButton";
            this.installButton.Padding = new(12, 0, 12, 0);
            this.installButton.Size = new Size(149, 24);
            this.installButton.TabIndex = 10004;
            this.installButton.Text = "Generate and Install";
            this.installButton.UseVisualStyleBackColor = true;
            this.installButton.Click += this.OnInstall;
            // 
            // cancelButton
            // 
            this.cancelButton.Anchor = ((AnchorStyles)((AnchorStyles.Bottom | AnchorStyles.Left)));
            this.cancelButton.AutoSize = true;
            this.cancelButton.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            this.cancelButton.FlatStyle = FlatStyle.System;
            this.cancelButton.Location = new Point(12, 325);
            this.cancelButton.Name = "cancelButton";
            this.cancelButton.Padding = new(12, 0, 12, 0);
            this.cancelButton.Size = new Size(81, 24);
            this.cancelButton.TabIndex = 10000;
            this.cancelButton.Text = "Cancel";
            this.cancelButton.UseVisualStyleBackColor = true;
            this.cancelButton.Click += this.OnCancel;
            // 
            // programsGroupBox
            // 
            this.programsGroupBox.Anchor = ((AnchorStyles)((((AnchorStyles.Top | AnchorStyles.Bottom) | AnchorStyles.Left) | AnchorStyles.Right)));
            this.programsGroupBox.Controls.Add(this.koaloaderFlowPanel);
            this.programsGroupBox.Controls.Add(this.noneFoundLabel);
            this.programsGroupBox.Controls.Add(this.blockedGamesFlowPanel);
            this.programsGroupBox.Controls.Add(this.selectionTreeView);
            this.programsGroupBox.Controls.Add(this.allCheckBoxLayoutPanel);
            this.programsGroupBox.FlatStyle = FlatStyle.System;
            this.programsGroupBox.Location = new Point(12, 12);
            this.programsGroupBox.Name = "programsGroupBox";
            this.programsGroupBox.Size = new Size(560, 239);
            this.programsGroupBox.TabIndex = 8;
            this.programsGroupBox.TabStop = false;
            this.programsGroupBox.Text = "Programs / Games";
            // 
            // koaloaderFlowPanel
            // 
            this.koaloaderFlowPanel.Anchor = ((AnchorStyles)((AnchorStyles.Top | AnchorStyles.Right)));
            this.koaloaderFlowPanel.AutoSize = true;
            this.koaloaderFlowPanel.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            this.koaloaderFlowPanel.Controls.Add(this.koaloaderAllCheckBox);
            this.koaloaderFlowPanel.Location = new Point(430, -1);
            this.koaloaderFlowPanel.Margin = new(0);
            this.koaloaderFlowPanel.Name = "koaloaderFlowPanel";
            this.koaloaderFlowPanel.Size = new Size(73, 19);
            this.koaloaderFlowPanel.TabIndex = 10005;
            this.koaloaderFlowPanel.WrapContents = false;
            // 
            // koaloaderAllCheckBox
            // 
            this.koaloaderAllCheckBox.Anchor = ((AnchorStyles)((AnchorStyles.Top | AnchorStyles.Right)));
            this.koaloaderAllCheckBox.Checked = true;
            this.koaloaderAllCheckBox.CheckState = CheckState.Checked;
            this.koaloaderAllCheckBox.Enabled = false;
            this.koaloaderAllCheckBox.FlatStyle = FlatStyle.System;
            this.koaloaderAllCheckBox.Location = new Point(2, 0);
            this.koaloaderAllCheckBox.Margin = new(2, 0, 0, 0);
            this.koaloaderAllCheckBox.Name = "koaloaderAllCheckBox";
            this.koaloaderAllCheckBox.Size = new Size(71, 19);
            this.koaloaderAllCheckBox.TabIndex = 4;
            this.koaloaderAllCheckBox.Text = "Koaloader";
            this.koaloaderAllCheckBox.CheckedChanged += this.OnKoaloaderAllCheckBoxChanged;
            // 
            // noneFoundLabel
            // 
            this.noneFoundLabel.Dock = DockStyle.Fill;
            this.noneFoundLabel.Location = new Point(3, 19);
            this.noneFoundLabel.Name = "noneFoundLabel";
            this.noneFoundLabel.Size = new Size(554, 217);
            this.noneFoundLabel.TabIndex = 1002;
            this.noneFoundLabel.Text = "No applicable programs nor games were found on your computer!";
            this.noneFoundLabel.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.noneFoundLabel.Visible = false;
            // 
            // blockedGamesFlowPanel
            // 
            this.blockedGamesFlowPanel.Anchor = AnchorStyles.Top;
            this.blockedGamesFlowPanel.AutoSize = true;
            this.blockedGamesFlowPanel.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            this.blockedGamesFlowPanel.Controls.Add(this.blockedGamesCheckBox);
            this.blockedGamesFlowPanel.Controls.Add(this.blockProtectedHelpButton);
            this.blockedGamesFlowPanel.Location = new Point(125, -1);
            this.blockedGamesFlowPanel.Margin = new(0);
            this.blockedGamesFlowPanel.Name = "blockedGamesFlowPanel";
            this.blockedGamesFlowPanel.Size = new Size(162, 20);
            this.blockedGamesFlowPanel.TabIndex = 1005;
            this.blockedGamesFlowPanel.WrapContents = false;
            // 
            // blockedGamesCheckBox
            // 
            this.blockedGamesCheckBox.Checked = true;
            this.blockedGamesCheckBox.CheckState = CheckState.Checked;
            this.blockedGamesCheckBox.Enabled = false;
            this.blockedGamesCheckBox.FlatStyle = FlatStyle.System;
            this.blockedGamesCheckBox.Location = new Point(2, 0);
            this.blockedGamesCheckBox.Margin = new(2, 0, 0, 0);
            this.blockedGamesCheckBox.Name = "blockedGamesCheckBox";
            this.blockedGamesCheckBox.Size = new Size(140, 20);
            this.blockedGamesCheckBox.TabIndex = 1;
            this.blockedGamesCheckBox.Text = "Block Protected Games";
            this.blockedGamesCheckBox.UseVisualStyleBackColor = true;
            this.blockedGamesCheckBox.CheckedChanged += this.OnBlockProtectedGamesCheckBoxChanged;
            // 
            // blockProtectedHelpButton
            // 
            this.blockProtectedHelpButton.Enabled = false;
            this.blockProtectedHelpButton.FlatStyle = FlatStyle.System;
            this.blockProtectedHelpButton.Font = new Font("Segoe UI", 7F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.blockProtectedHelpButton.Location = new Point(142, 0);
            this.blockProtectedHelpButton.Margin = new(0, 0, 1, 0);
            this.blockProtectedHelpButton.Name = "blockProtectedHelpButton";
            this.blockProtectedHelpButton.Size = new Size(19, 19);
            this.blockProtectedHelpButton.TabIndex = 2;
            this.blockProtectedHelpButton.Text = "?";
            this.blockProtectedHelpButton.UseVisualStyleBackColor = true;
            this.blockProtectedHelpButton.Click += this.OnBlockProtectedGamesHelpButtonClicked;
            // 
            // selectionTreeView
            // 
            this.selectionTreeView.BackColor = System.Drawing.SystemColors.Control;
            this.selectionTreeView.BorderStyle = BorderStyle.None;
            this.selectionTreeView.CheckBoxes = true;
            this.selectionTreeView.Dock = DockStyle.Fill;
            this.selectionTreeView.DrawMode = TreeViewDrawMode.OwnerDrawAll;
            this.selectionTreeView.Enabled = false;
            this.selectionTreeView.FullRowSelect = true;
            this.selectionTreeView.Location = new Point(3, 19);
            this.selectionTreeView.Name = "selectionTreeView";
            this.selectionTreeView.Size = new Size(554, 217);
            this.selectionTreeView.Sorted = true;
            this.selectionTreeView.TabIndex = 1001;
            // 
            // allCheckBoxLayoutPanel
            // 
            this.allCheckBoxLayoutPanel.Anchor = ((AnchorStyles)((AnchorStyles.Top | AnchorStyles.Right)));
            this.allCheckBoxLayoutPanel.AutoSize = true;
            this.allCheckBoxLayoutPanel.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            this.allCheckBoxLayoutPanel.Controls.Add(this.allCheckBox);
            this.allCheckBoxLayoutPanel.Location = new Point(520, -1);
            this.allCheckBoxLayoutPanel.Margin = new(0);
            this.allCheckBoxLayoutPanel.Name = "allCheckBoxLayoutPanel";
            this.allCheckBoxLayoutPanel.Size = new Size(34, 19);
            this.allCheckBoxLayoutPanel.TabIndex = 1006;
            this.allCheckBoxLayoutPanel.WrapContents = false;
            // 
            // allCheckBox
            // 
            this.allCheckBox.Anchor = ((AnchorStyles)((AnchorStyles.Top | AnchorStyles.Right)));
            this.allCheckBox.Checked = true;
            this.allCheckBox.CheckState = CheckState.Checked;
            this.allCheckBox.Enabled = false;
            this.allCheckBox.FlatStyle = FlatStyle.System;
            this.allCheckBox.Location = new Point(2, 0);
            this.allCheckBox.Margin = new(2, 0, 0, 0);
            this.allCheckBox.Name = "allCheckBox";
            this.allCheckBox.Size = new Size(32, 19);
            this.allCheckBox.TabIndex = 4;
            this.allCheckBox.Text = "All";
            this.allCheckBox.CheckedChanged += this.OnAllCheckBoxChanged;
            // 
            // progressBar
            // 
            this.progressBar.Anchor = ((AnchorStyles)(((AnchorStyles.Bottom | AnchorStyles.Left) | AnchorStyles.Right)));
            this.progressBar.Location = new Point(12, 296);
            this.progressBar.Name = "progressBar";
            this.progressBar.Size = new Size(560, 23);
            this.progressBar.TabIndex = 9;
            // 
            // progressLabel
            // 
            this.progressLabel.Anchor = ((AnchorStyles)(((AnchorStyles.Bottom | AnchorStyles.Left) | AnchorStyles.Right)));
            this.progressLabel.Location = new Point(12, 254);
            this.progressLabel.Name = "progressLabel";
            this.progressLabel.Size = new Size(560, 15);
            this.progressLabel.TabIndex = 10;
            this.progressLabel.Text = "Gathering and caching your applicable games and their DLCs . . . 0%";
            // 
            // scanButton
            // 
            this.scanButton.Anchor = ((AnchorStyles)((AnchorStyles.Bottom | AnchorStyles.Right)));
            this.scanButton.AutoSize = true;
            this.scanButton.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            this.scanButton.Enabled = false;
            this.scanButton.FlatStyle = FlatStyle.System;
            this.scanButton.Location = new Point(235, 325);
            this.scanButton.Name = "scanButton";
            this.scanButton.Padding = new(12, 0, 12, 0);
            this.scanButton.Size = new Size(82, 24);
            this.scanButton.TabIndex = 10002;
            this.scanButton.Text = "Rescan";
            this.scanButton.UseVisualStyleBackColor = true;
            this.scanButton.Click += this.OnScan;
            // 
            // uninstallButton
            // 
            this.uninstallButton.Anchor = ((AnchorStyles)((AnchorStyles.Bottom | AnchorStyles.Right)));
            this.uninstallButton.AutoSize = true;
            this.uninstallButton.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            this.uninstallButton.Enabled = false;
            this.uninstallButton.FlatStyle = FlatStyle.System;
            this.uninstallButton.Location = new Point(323, 325);
            this.uninstallButton.Name = "uninstallButton";
            this.uninstallButton.Padding = new(12, 0, 12, 0);
            this.uninstallButton.Size = new Size(91, 24);
            this.uninstallButton.TabIndex = 10003;
            this.uninstallButton.Text = "Uninstall";
            this.uninstallButton.UseVisualStyleBackColor = true;
            this.uninstallButton.Click += this.OnUninstall;
            // 
            // progressLabelGames
            // 
            this.progressLabelGames.Anchor = ((AnchorStyles)(((AnchorStyles.Bottom | AnchorStyles.Left) | AnchorStyles.Right)));
            this.progressLabelGames.Font = new Font("Segoe UI", 7F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.progressLabelGames.Location = new Point(12, 269);
            this.progressLabelGames.Name = "progressLabelGames";
            this.progressLabelGames.Size = new Size(560, 12);
            this.progressLabelGames.TabIndex = 11;
            this.progressLabelGames.Text = "Remaining games (2): Game 1, Game 2";
            // 
            // progressLabelDLCs
            // 
            this.progressLabelDLCs.Anchor = ((AnchorStyles)(((AnchorStyles.Bottom | AnchorStyles.Left) | AnchorStyles.Right)));
            this.progressLabelDLCs.Font = new Font("Segoe UI", 7F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.progressLabelDLCs.Location = new Point(12, 281);
            this.progressLabelDLCs.Name = "progressLabelDLCs";
            this.progressLabelDLCs.Size = new Size(560, 12);
            this.progressLabelDLCs.TabIndex = 12;
            this.progressLabelDLCs.Text = "Remaining DLC (2): 123456, 654321";
            // 
            // sortCheckBox
            // 
            this.sortCheckBox.Anchor = ((AnchorStyles)((AnchorStyles.Bottom | AnchorStyles.Left)));
            this.sortCheckBox.AutoSize = true;
            this.sortCheckBox.FlatStyle = FlatStyle.System;
            this.sortCheckBox.Location = new Point(120, 328);
            this.sortCheckBox.Margin = new(3, 0, 0, 0);
            this.sortCheckBox.Name = "sortCheckBox";
            this.sortCheckBox.Size = new Size(104, 20);
            this.sortCheckBox.TabIndex = 10001;
            this.sortCheckBox.Text = "Sort By Name";
            this.sortCheckBox.CheckedChanged += this.OnSortCheckBoxChanged;
            // 
            // SelectForm
            // 
            this.AutoScaleDimensions = new SizeF(7F, 15F);
            this.AutoScaleMode = AutoScaleMode.Font;
            this.AutoSize = true;
            this.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            this.ClientSize = new Size(584, 361);
            this.Controls.Add(this.sortCheckBox);
            this.Controls.Add(this.progressLabelDLCs);
            this.Controls.Add(this.progressLabelGames);
            this.Controls.Add(this.uninstallButton);
            this.Controls.Add(this.scanButton);
            this.Controls.Add(this.programsGroupBox);
            this.Controls.Add(this.progressBar);
            this.Controls.Add(this.cancelButton);
            this.Controls.Add(this.installButton);
            this.Controls.Add(this.progressLabel);
            this.DoubleBuffered = true;
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "SelectForm";
            this.StartPosition = FormStartPosition.CenterParent;
            this.Text = "SelectForm";
            this.Load += this.OnLoad;
            this.programsGroupBox.ResumeLayout(false);
            this.programsGroupBox.PerformLayout();
            this.koaloaderFlowPanel.ResumeLayout(false);
            this.blockedGamesFlowPanel.ResumeLayout(false);
            this.allCheckBoxLayoutPanel.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private Button installButton;
        private Button cancelButton;
        private GroupBox programsGroupBox;
        private ProgressBar progressBar;
        private Label progressLabel;
        private CheckBox allCheckBox;
        private Button scanButton;
        private Label noneFoundLabel;
        private CustomTreeView selectionTreeView;
        private CheckBox blockedGamesCheckBox;
        private Button blockProtectedHelpButton;
        private FlowLayoutPanel blockedGamesFlowPanel;
        private FlowLayoutPanel allCheckBoxLayoutPanel;
        private Button uninstallButton;
        private Label progressLabelGames;
        private Label progressLabelDLCs;
        private CheckBox sortCheckBox;
        private FlowLayoutPanel koaloaderFlowPanel;
        private CheckBox koaloaderAllCheckBox;
    }
}

