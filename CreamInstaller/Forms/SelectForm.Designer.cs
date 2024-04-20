using System.ComponentModel;
using System.Windows.Forms;

using CreamInstaller.Components;

namespace CreamInstaller.Forms
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
            installButton = new Button();
            cancelButton = new Button();
            programsGroupBox = new GroupBox();
            koaloaderFlowPanel = new FlowLayoutPanel();
            koaloaderAllCheckBox = new CheckBox();
            noneFoundLabel = new Label();
            blockedGamesFlowPanel = new FlowLayoutPanel();
            blockedGamesCheckBox = new CheckBox();
            blockProtectedHelpButton = new Button();
            allCheckBoxLayoutPanel = new FlowLayoutPanel();
            allCheckBox = new CheckBox();
            progressBar = new ProgressBar();
            progressLabel = new Label();
            scanButton = new Button();
            uninstallButton = new Button();
            progressLabelGames = new Label();
            progressLabelDLCs = new Label();
            sortCheckBox = new CheckBox();
            saveButton = new Button();
            loadButton = new Button();
            resetKoaloaderButton = new Button();
            resetButton = new Button();
            saveKoaloaderButton = new Button();
            loadKoaloaderButton = new Button();
            selectionTreeView = new CustomTreeView();
            programsGroupBox.SuspendLayout();
            koaloaderFlowPanel.SuspendLayout();
            blockedGamesFlowPanel.SuspendLayout();
            allCheckBoxLayoutPanel.SuspendLayout();
            SuspendLayout();
            // 
            // installButton
            // 
            installButton.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            installButton.AutoSize = true;
            installButton.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            installButton.Enabled = false;
            installButton.Location = new System.Drawing.Point(427, 326);
            installButton.Name = "installButton";
            installButton.Padding = new Padding(12, 0, 12, 0);
            installButton.Size = new System.Drawing.Size(145, 25);
            installButton.TabIndex = 10000;
            installButton.Text = "Generate and Install";
            installButton.UseVisualStyleBackColor = true;
            installButton.Click += OnInstall;
            // 
            // cancelButton
            // 
            cancelButton.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            cancelButton.AutoSize = true;
            cancelButton.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            cancelButton.Location = new System.Drawing.Point(12, 326);
            cancelButton.Name = "cancelButton";
            cancelButton.Padding = new Padding(12, 0, 12, 0);
            cancelButton.Size = new System.Drawing.Size(77, 25);
            cancelButton.TabIndex = 10004;
            cancelButton.Text = "Cancel";
            cancelButton.UseVisualStyleBackColor = true;
            cancelButton.Click += OnCancel;
            // 
            // programsGroupBox
            // 
            programsGroupBox.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            programsGroupBox.Controls.Add(koaloaderFlowPanel);
            programsGroupBox.Controls.Add(noneFoundLabel);
            programsGroupBox.Controls.Add(blockedGamesFlowPanel);
            programsGroupBox.Controls.Add(allCheckBoxLayoutPanel);
            programsGroupBox.Controls.Add(selectionTreeView);
            programsGroupBox.Location = new System.Drawing.Point(12, 12);
            programsGroupBox.Name = "programsGroupBox";
            programsGroupBox.Size = new System.Drawing.Size(560, 209);
            programsGroupBox.TabIndex = 8;
            programsGroupBox.TabStop = false;
            programsGroupBox.Text = "Programs / Games";
            // 
            // koaloaderFlowPanel
            // 
            koaloaderFlowPanel.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            koaloaderFlowPanel.AutoSize = true;
            koaloaderFlowPanel.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            koaloaderFlowPanel.Controls.Add(koaloaderAllCheckBox);
            koaloaderFlowPanel.Location = new System.Drawing.Point(422, -1);
            koaloaderFlowPanel.Margin = new Padding(0);
            koaloaderFlowPanel.Name = "koaloaderFlowPanel";
            koaloaderFlowPanel.Size = new System.Drawing.Size(81, 19);
            koaloaderFlowPanel.TabIndex = 10005;
            koaloaderFlowPanel.WrapContents = false;
            // 
            // koaloaderAllCheckBox
            // 
            koaloaderAllCheckBox.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            koaloaderAllCheckBox.AutoSize = true;
            koaloaderAllCheckBox.Checked = true;
            koaloaderAllCheckBox.CheckState = CheckState.Checked;
            koaloaderAllCheckBox.Enabled = false;
            koaloaderAllCheckBox.Location = new System.Drawing.Point(2, 0);
            koaloaderAllCheckBox.Margin = new Padding(2, 0, 0, 0);
            koaloaderAllCheckBox.Name = "koaloaderAllCheckBox";
            koaloaderAllCheckBox.Size = new System.Drawing.Size(79, 19);
            koaloaderAllCheckBox.TabIndex = 4;
            koaloaderAllCheckBox.Text = "Koaloader";
            koaloaderAllCheckBox.CheckedChanged += OnKoaloaderAllCheckBoxChanged;
            // 
            // noneFoundLabel
            // 
            noneFoundLabel.Dock = DockStyle.Fill;
            noneFoundLabel.Location = new System.Drawing.Point(3, 19);
            noneFoundLabel.Name = "noneFoundLabel";
            noneFoundLabel.Size = new System.Drawing.Size(554, 187);
            noneFoundLabel.TabIndex = 1002;
            noneFoundLabel.Text = "No applicable programs nor games were found on your computer!";
            noneFoundLabel.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            noneFoundLabel.Visible = false;
            // 
            // blockedGamesFlowPanel
            // 
            blockedGamesFlowPanel.Anchor = AnchorStyles.Top;
            blockedGamesFlowPanel.AutoSize = true;
            blockedGamesFlowPanel.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            blockedGamesFlowPanel.Controls.Add(blockedGamesCheckBox);
            blockedGamesFlowPanel.Controls.Add(blockProtectedHelpButton);
            blockedGamesFlowPanel.Location = new System.Drawing.Point(125, -1);
            blockedGamesFlowPanel.Margin = new Padding(0);
            blockedGamesFlowPanel.Name = "blockedGamesFlowPanel";
            blockedGamesFlowPanel.Size = new System.Drawing.Size(170, 19);
            blockedGamesFlowPanel.TabIndex = 1005;
            blockedGamesFlowPanel.WrapContents = false;
            // 
            // blockedGamesCheckBox
            // 
            blockedGamesCheckBox.AutoSize = true;
            blockedGamesCheckBox.Checked = true;
            blockedGamesCheckBox.CheckState = CheckState.Checked;
            blockedGamesCheckBox.Enabled = false;
            blockedGamesCheckBox.Location = new System.Drawing.Point(2, 0);
            blockedGamesCheckBox.Margin = new Padding(2, 0, 0, 0);
            blockedGamesCheckBox.Name = "blockedGamesCheckBox";
            blockedGamesCheckBox.Size = new System.Drawing.Size(148, 19);
            blockedGamesCheckBox.TabIndex = 1;
            blockedGamesCheckBox.Text = "Block Protected Games";
            blockedGamesCheckBox.UseVisualStyleBackColor = true;
            blockedGamesCheckBox.CheckedChanged += OnBlockProtectedGamesCheckBoxChanged;
            // 
            // blockProtectedHelpButton
            // 
            blockProtectedHelpButton.Enabled = false;
            blockProtectedHelpButton.Font = new System.Drawing.Font("Segoe UI", 7F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            blockProtectedHelpButton.Location = new System.Drawing.Point(150, 0);
            blockProtectedHelpButton.Margin = new Padding(0, 0, 1, 0);
            blockProtectedHelpButton.Name = "blockProtectedHelpButton";
            blockProtectedHelpButton.Size = new System.Drawing.Size(19, 19);
            blockProtectedHelpButton.TabIndex = 2;
            blockProtectedHelpButton.Text = "?";
            blockProtectedHelpButton.UseVisualStyleBackColor = true;
            blockProtectedHelpButton.Click += OnBlockProtectedGamesHelpButtonClicked;
            // 
            // selectionTreeView
            // 
            selectionTreeView.BackColor = System.Drawing.SystemColors.Control;
            selectionTreeView.BorderStyle = System.Windows.Forms.BorderStyle.None;
            selectionTreeView.CheckBoxes = true;
            selectionTreeView.Dock = System.Windows.Forms.DockStyle.Fill;
            selectionTreeView.Enabled = false;
            selectionTreeView.FullRowSelect = true;
            selectionTreeView.Location = new System.Drawing.Point(3, 19);
            selectionTreeView.Name = "selectionTreeView";
            selectionTreeView.Size = new System.Drawing.Size(554, 187);
            selectionTreeView.TabIndex = 1001;
            // 
            // allCheckBoxLayoutPanel
            // 
            allCheckBoxLayoutPanel.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            allCheckBoxLayoutPanel.AutoSize = true;
            allCheckBoxLayoutPanel.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            allCheckBoxLayoutPanel.Controls.Add(allCheckBox);
            allCheckBoxLayoutPanel.Location = new System.Drawing.Point(512, -1);
            allCheckBoxLayoutPanel.Margin = new Padding(0);
            allCheckBoxLayoutPanel.Name = "allCheckBoxLayoutPanel";
            allCheckBoxLayoutPanel.Size = new System.Drawing.Size(42, 19);
            allCheckBoxLayoutPanel.TabIndex = 1006;
            allCheckBoxLayoutPanel.WrapContents = false;
            // 
            // allCheckBox
            // 
            allCheckBox.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            allCheckBox.AutoSize = true;
            allCheckBox.Checked = true;
            allCheckBox.CheckState = CheckState.Checked;
            allCheckBox.Enabled = false;
            allCheckBox.Location = new System.Drawing.Point(2, 0);
            allCheckBox.Margin = new Padding(2, 0, 0, 0);
            allCheckBox.Name = "allCheckBox";
            allCheckBox.Size = new System.Drawing.Size(40, 19);
            allCheckBox.TabIndex = 4;
            allCheckBox.Text = "All";
            allCheckBox.CheckedChanged += OnAllCheckBoxChanged;
            // 
            // progressBar
            // 
            progressBar.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            progressBar.Location = new System.Drawing.Point(12, 266);
            progressBar.Name = "progressBar";
            progressBar.Size = new System.Drawing.Size(560, 23);
            progressBar.TabIndex = 9;
            // 
            // progressLabel
            // 
            progressLabel.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            progressLabel.Location = new System.Drawing.Point(12, 224);
            progressLabel.Name = "progressLabel";
            progressLabel.Size = new System.Drawing.Size(560, 15);
            progressLabel.TabIndex = 10;
            progressLabel.Text = "Gathering and caching your applicable games and their DLCs . . . 0%";
            // 
            // scanButton
            // 
            scanButton.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            scanButton.AutoSize = true;
            scanButton.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            scanButton.Enabled = false;
            scanButton.Location = new System.Drawing.Point(250, 326);
            scanButton.Name = "scanButton";
            scanButton.Padding = new Padding(12, 0, 12, 0);
            scanButton.Size = new System.Drawing.Size(78, 25);
            scanButton.TabIndex = 10002;
            scanButton.Text = "Rescan";
            scanButton.UseVisualStyleBackColor = true;
            scanButton.Click += OnScan;
            // 
            // uninstallButton
            // 
            uninstallButton.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            uninstallButton.AutoSize = true;
            uninstallButton.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            uninstallButton.Enabled = false;
            uninstallButton.Location = new System.Drawing.Point(334, 326);
            uninstallButton.Name = "uninstallButton";
            uninstallButton.Padding = new Padding(12, 0, 12, 0);
            uninstallButton.Size = new System.Drawing.Size(87, 25);
            uninstallButton.TabIndex = 10001;
            uninstallButton.Text = "Uninstall";
            uninstallButton.UseVisualStyleBackColor = true;
            uninstallButton.Click += OnUninstall;
            // 
            // progressLabelGames
            // 
            progressLabelGames.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            progressLabelGames.Font = new System.Drawing.Font("Segoe UI", 7F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            progressLabelGames.Location = new System.Drawing.Point(12, 239);
            progressLabelGames.Name = "progressLabelGames";
            progressLabelGames.Size = new System.Drawing.Size(560, 12);
            progressLabelGames.TabIndex = 11;
            progressLabelGames.Text = "Remaining games (2): Game 1, Game 2";
            // 
            // progressLabelDLCs
            // 
            progressLabelDLCs.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            progressLabelDLCs.Font = new System.Drawing.Font("Segoe UI", 7F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            progressLabelDLCs.Location = new System.Drawing.Point(12, 251);
            progressLabelDLCs.Name = "progressLabelDLCs";
            progressLabelDLCs.Size = new System.Drawing.Size(560, 12);
            progressLabelDLCs.TabIndex = 12;
            progressLabelDLCs.Text = "Remaining DLC (2): 123456, 654321";
            // 
            // sortCheckBox
            // 
            sortCheckBox.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            sortCheckBox.AutoSize = true;
            sortCheckBox.Location = new System.Drawing.Point(125, 330);
            sortCheckBox.Margin = new Padding(3, 0, 0, 0);
            sortCheckBox.Name = "sortCheckBox";
            sortCheckBox.Size = new System.Drawing.Size(98, 19);
            sortCheckBox.TabIndex = 10003;
            sortCheckBox.Text = "Sort By Name";
            sortCheckBox.CheckedChanged += OnSortCheckBoxChanged;
            // 
            // saveButton
            // 
            saveButton.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            saveButton.AutoSize = true;
            saveButton.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            saveButton.Enabled = false;
            saveButton.Location = new System.Drawing.Point(432, 295);
            saveButton.Name = "saveButton";
            saveButton.Size = new System.Drawing.Size(66, 25);
            saveButton.TabIndex = 10006;
            saveButton.Text = "Save DLC";
            saveButton.UseVisualStyleBackColor = true;
            saveButton.Click += OnSaveDlc;
            // 
            // loadButton
            // 
            loadButton.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            loadButton.AutoSize = true;
            loadButton.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            loadButton.Enabled = false;
            loadButton.Location = new System.Drawing.Point(504, 295);
            loadButton.Name = "loadButton";
            loadButton.Size = new System.Drawing.Size(68, 25);
            loadButton.TabIndex = 10005;
            loadButton.Text = "Load DLC";
            loadButton.UseVisualStyleBackColor = true;
            loadButton.Click += OnLoadDlc;
            // 
            // resetKoaloaderButton
            // 
            resetKoaloaderButton.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            resetKoaloaderButton.AutoSize = true;
            resetKoaloaderButton.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            resetKoaloaderButton.Enabled = false;
            resetKoaloaderButton.Location = new System.Drawing.Point(12, 295);
            resetKoaloaderButton.Name = "resetKoaloaderButton";
            resetKoaloaderButton.Size = new System.Drawing.Size(101, 25);
            resetKoaloaderButton.TabIndex = 10010;
            resetKoaloaderButton.Text = "Reset Koaloader";
            resetKoaloaderButton.UseVisualStyleBackColor = true;
            resetKoaloaderButton.Click += OnResetKoaloader;
            // 
            // resetButton
            // 
            resetButton.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            resetButton.AutoSize = true;
            resetButton.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            resetButton.Enabled = false;
            resetButton.Location = new System.Drawing.Point(356, 295);
            resetButton.Name = "resetButton";
            resetButton.Size = new System.Drawing.Size(70, 25);
            resetButton.TabIndex = 10007;
            resetButton.Text = "Reset DLC";
            resetButton.UseVisualStyleBackColor = true;
            resetButton.Click += OnResetDlc;
            // 
            // saveKoaloaderButton
            // 
            saveKoaloaderButton.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            saveKoaloaderButton.AutoSize = true;
            saveKoaloaderButton.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            saveKoaloaderButton.Enabled = false;
            saveKoaloaderButton.Location = new System.Drawing.Point(119, 295);
            saveKoaloaderButton.Name = "saveKoaloaderButton";
            saveKoaloaderButton.Size = new System.Drawing.Size(97, 25);
            saveKoaloaderButton.TabIndex = 10009;
            saveKoaloaderButton.Text = "Save Koaloader";
            saveKoaloaderButton.UseVisualStyleBackColor = true;
            saveKoaloaderButton.Click += OnSaveKoaloader;
            // 
            // loadKoaloaderButton
            // 
            loadKoaloaderButton.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            loadKoaloaderButton.AutoSize = true;
            loadKoaloaderButton.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            loadKoaloaderButton.Enabled = false;
            loadKoaloaderButton.Location = new System.Drawing.Point(222, 295);
            loadKoaloaderButton.Name = "loadKoaloaderButton";
            loadKoaloaderButton.Size = new System.Drawing.Size(99, 25);
            loadKoaloaderButton.TabIndex = 10008;
            loadKoaloaderButton.Text = "Load Koaloader";
            loadKoaloaderButton.UseVisualStyleBackColor = true;
            loadKoaloaderButton.Click += OnLoadKoaloader;
            // 
            // SelectForm
            // 
            AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            AutoSize = true;
            AutoSizeMode = AutoSizeMode.GrowAndShrink;
            ClientSize = new System.Drawing.Size(584, 361);
            Controls.Add(loadKoaloaderButton);
            Controls.Add(saveKoaloaderButton);
            Controls.Add(resetButton);
            Controls.Add(resetKoaloaderButton);
            Controls.Add(loadButton);
            Controls.Add(saveButton);
            Controls.Add(sortCheckBox);
            Controls.Add(progressLabelDLCs);
            Controls.Add(progressLabelGames);
            Controls.Add(uninstallButton);
            Controls.Add(scanButton);
            Controls.Add(programsGroupBox);
            Controls.Add(progressBar);
            Controls.Add(cancelButton);
            Controls.Add(installButton);
            Controls.Add(progressLabel);
            DoubleBuffered = true;
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "SelectForm";
            StartPosition = FormStartPosition.Manual;
            Text = "SelectForm";
            Load += OnLoad;
            programsGroupBox.ResumeLayout(false);
            programsGroupBox.PerformLayout();
            koaloaderFlowPanel.ResumeLayout(false);
            koaloaderFlowPanel.PerformLayout();
            blockedGamesFlowPanel.ResumeLayout(false);
            blockedGamesFlowPanel.PerformLayout();
            allCheckBoxLayoutPanel.ResumeLayout(false);
            allCheckBoxLayoutPanel.PerformLayout();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private Button installButton;
        private Button cancelButton;
        private GroupBox programsGroupBox;
        private ProgressBar progressBar;
        private Label progressLabel;
        internal CheckBox allCheckBox;
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
        internal CheckBox koaloaderAllCheckBox;
        private Button saveButton;
        private Button loadButton;
        private Button resetKoaloaderButton;
        private Button resetButton;
        private Button saveKoaloaderButton;
        private Button loadKoaloaderButton;
    }
}

