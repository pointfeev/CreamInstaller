
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
            this.installButton = new System.Windows.Forms.Button();
            this.cancelButton = new System.Windows.Forms.Button();
            this.label1 = new System.Windows.Forms.Label();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.noneFoundLabel = new System.Windows.Forms.Label();
            this.selectionTreeView = new CreamInstaller.CustomTreeView();
            this.blockProtectedHelpButton = new System.Windows.Forms.Button();
            this.blockedGamesCheckBox = new System.Windows.Forms.CheckBox();
            this.allCheckBox = new System.Windows.Forms.CheckBox();
            this.progressBar1 = new System.Windows.Forms.ProgressBar();
            this.label2 = new System.Windows.Forms.Label();
            this.scanButton = new System.Windows.Forms.Button();
            this.flowLayoutPanel1 = new System.Windows.Forms.FlowLayoutPanel();
            this.flowLayoutPanel2 = new System.Windows.Forms.FlowLayoutPanel();
            this.uninstallButton = new System.Windows.Forms.Button();
            this.groupBox1.SuspendLayout();
            this.flowLayoutPanel1.SuspendLayout();
            this.flowLayoutPanel2.SuspendLayout();
            this.SuspendLayout();
            // 
            // installButton
            // 
            this.installButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.installButton.Enabled = false;
            this.installButton.Location = new System.Drawing.Point(422, 326);
            this.installButton.Name = "installButton";
            this.installButton.Size = new System.Drawing.Size(150, 23);
            this.installButton.TabIndex = 10003;
            this.installButton.Text = "Generate and Install";
            this.installButton.UseVisualStyleBackColor = true;
            this.installButton.Click += new System.EventHandler(this.OnInstall);
            // 
            // cancelButton
            // 
            this.cancelButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.cancelButton.Location = new System.Drawing.Point(12, 326);
            this.cancelButton.Name = "cancelButton";
            this.cancelButton.Size = new System.Drawing.Size(75, 23);
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
            this.groupBox1.Controls.Add(this.noneFoundLabel);
            this.groupBox1.Controls.Add(this.selectionTreeView);
            this.groupBox1.Location = new System.Drawing.Point(12, 12);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(560, 308);
            this.groupBox1.TabIndex = 8;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Programs / Games";
            // 
            // noneFoundLabel
            // 
            this.noneFoundLabel.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.noneFoundLabel.Location = new System.Drawing.Point(6, 22);
            this.noneFoundLabel.Name = "noneFoundLabel";
            this.noneFoundLabel.Size = new System.Drawing.Size(548, 280);
            this.noneFoundLabel.TabIndex = 1002;
            this.noneFoundLabel.Text = "No CreamAPI-applicable programs were found on your computer!";
            this.noneFoundLabel.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.noneFoundLabel.Visible = false;
            // 
            // selectionTreeView
            // 
            this.selectionTreeView.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.selectionTreeView.BackColor = System.Drawing.SystemColors.Control;
            this.selectionTreeView.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.selectionTreeView.CheckBoxes = true;
            this.selectionTreeView.Enabled = false;
            this.selectionTreeView.FullRowSelect = true;
            this.selectionTreeView.HotTracking = true;
            this.selectionTreeView.Location = new System.Drawing.Point(6, 22);
            this.selectionTreeView.Name = "selectionTreeView";
            this.selectionTreeView.Size = new System.Drawing.Size(548, 280);
            this.selectionTreeView.TabIndex = 1001;
            // 
            // blockProtectedHelpButton
            // 
            this.blockProtectedHelpButton.Enabled = false;
            this.blockProtectedHelpButton.Font = new System.Drawing.Font("Segoe UI", 7F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.blockProtectedHelpButton.Location = new System.Drawing.Point(151, 3);
            this.blockProtectedHelpButton.Margin = new System.Windows.Forms.Padding(0, 3, 3, 3);
            this.blockProtectedHelpButton.Name = "blockProtectedHelpButton";
            this.blockProtectedHelpButton.Size = new System.Drawing.Size(19, 19);
            this.blockProtectedHelpButton.TabIndex = 1004;
            this.blockProtectedHelpButton.Text = "?";
            this.blockProtectedHelpButton.UseVisualStyleBackColor = true;
            this.blockProtectedHelpButton.Click += new System.EventHandler(this.OnBlockProtectedGamesHelpButtonClicked);
            // 
            // blockedGamesCheckBox
            // 
            this.blockedGamesCheckBox.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.blockedGamesCheckBox.AutoSize = true;
            this.blockedGamesCheckBox.Checked = true;
            this.blockedGamesCheckBox.CheckState = System.Windows.Forms.CheckState.Checked;
            this.blockedGamesCheckBox.Enabled = false;
            this.blockedGamesCheckBox.Location = new System.Drawing.Point(3, 3);
            this.blockedGamesCheckBox.Margin = new System.Windows.Forms.Padding(3, 3, 0, 3);
            this.blockedGamesCheckBox.Name = "blockedGamesCheckBox";
            this.blockedGamesCheckBox.Size = new System.Drawing.Size(148, 19);
            this.blockedGamesCheckBox.TabIndex = 1003;
            this.blockedGamesCheckBox.Text = "Block Protected Games";
            this.blockedGamesCheckBox.UseVisualStyleBackColor = true;
            this.blockedGamesCheckBox.CheckedChanged += new System.EventHandler(this.OnBlockProtectedGamesCheckBoxChanged);
            // 
            // allCheckBox
            // 
            this.allCheckBox.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.allCheckBox.AutoSize = true;
            this.allCheckBox.Enabled = false;
            this.allCheckBox.Location = new System.Drawing.Point(3, 3);
            this.allCheckBox.Margin = new System.Windows.Forms.Padding(3, 3, 0, 3);
            this.allCheckBox.Name = "allCheckBox";
            this.allCheckBox.Size = new System.Drawing.Size(40, 19);
            this.allCheckBox.TabIndex = 1;
            this.allCheckBox.Text = "All";
            this.allCheckBox.UseVisualStyleBackColor = true;
            this.allCheckBox.CheckedChanged += new System.EventHandler(this.OnAllCheckBoxChanged);
            // 
            // progressBar1
            // 
            this.progressBar1.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.progressBar1.Location = new System.Drawing.Point(12, 297);
            this.progressBar1.Name = "progressBar1";
            this.progressBar1.Size = new System.Drawing.Size(560, 23);
            this.progressBar1.TabIndex = 9;
            // 
            // label2
            // 
            this.label2.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.label2.Location = new System.Drawing.Point(12, 279);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(760, 15);
            this.label2.TabIndex = 10;
            this.label2.Text = "Loading . . .";
            // 
            // scanButton
            // 
            this.scanButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.scanButton.Enabled = false;
            this.scanButton.Location = new System.Drawing.Point(155, 326);
            this.scanButton.Name = "scanButton";
            this.scanButton.Size = new System.Drawing.Size(180, 23);
            this.scanButton.TabIndex = 10001;
            this.scanButton.Text = "Rescan Programs / Games";
            this.scanButton.UseVisualStyleBackColor = true;
            this.scanButton.Click += new System.EventHandler(this.OnScan);
            // 
            // flowLayoutPanel1
            // 
            this.flowLayoutPanel1.Anchor = System.Windows.Forms.AnchorStyles.Top;
            this.flowLayoutPanel1.AutoSize = true;
            this.flowLayoutPanel1.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.flowLayoutPanel1.Controls.Add(this.blockedGamesCheckBox);
            this.flowLayoutPanel1.Controls.Add(this.blockProtectedHelpButton);
            this.flowLayoutPanel1.Location = new System.Drawing.Point(230, 9);
            this.flowLayoutPanel1.Name = "flowLayoutPanel1";
            this.flowLayoutPanel1.Size = new System.Drawing.Size(173, 25);
            this.flowLayoutPanel1.TabIndex = 1005;
            // 
            // flowLayoutPanel2
            // 
            this.flowLayoutPanel2.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.flowLayoutPanel2.AutoSize = true;
            this.flowLayoutPanel2.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.flowLayoutPanel2.Controls.Add(this.allCheckBox);
            this.flowLayoutPanel2.Location = new System.Drawing.Point(520, 9);
            this.flowLayoutPanel2.Name = "flowLayoutPanel2";
            this.flowLayoutPanel2.Size = new System.Drawing.Size(43, 25);
            this.flowLayoutPanel2.TabIndex = 1006;
            // 
            // uninstallButton
            // 
            this.uninstallButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.uninstallButton.Enabled = false;
            this.uninstallButton.Location = new System.Drawing.Point(341, 326);
            this.uninstallButton.Name = "uninstallButton";
            this.uninstallButton.Size = new System.Drawing.Size(75, 23);
            this.uninstallButton.TabIndex = 10002;
            this.uninstallButton.Text = "Uninstall";
            this.uninstallButton.UseVisualStyleBackColor = true;
            this.uninstallButton.Click += new System.EventHandler(this.OnUninstall);
            // 
            // SelectForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(584, 361);
            this.Controls.Add(this.uninstallButton);
            this.Controls.Add(this.flowLayoutPanel1);
            this.Controls.Add(this.scanButton);
            this.Controls.Add(this.flowLayoutPanel2);
            this.Controls.Add(this.groupBox1);
            this.Controls.Add(this.progressBar1);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.cancelButton);
            this.Controls.Add(this.installButton);
            this.Controls.Add(this.label2);
            this.DoubleBuffered = true;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.MinimumSize = new System.Drawing.Size(600, 400);
            this.Name = "SelectForm";
            this.SizeGripStyle = System.Windows.Forms.SizeGripStyle.Show;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "SelectForm";
            this.TopMost = true;
            this.Load += new System.EventHandler(this.OnLoad);
            this.groupBox1.ResumeLayout(false);
            this.flowLayoutPanel1.ResumeLayout(false);
            this.flowLayoutPanel1.PerformLayout();
            this.flowLayoutPanel2.ResumeLayout(false);
            this.flowLayoutPanel2.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button installButton;
        private System.Windows.Forms.Button cancelButton;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.ProgressBar progressBar1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.CheckBox allCheckBox;
        private Button scanButton;
        private Label noneFoundLabel;
        private CustomTreeView selectionTreeView;
        private CheckBox blockedGamesCheckBox;
        private Button blockProtectedHelpButton;
        private FlowLayoutPanel flowLayoutPanel1;
        private FlowLayoutPanel flowLayoutPanel2;
        private Button uninstallButton;
    }
}

