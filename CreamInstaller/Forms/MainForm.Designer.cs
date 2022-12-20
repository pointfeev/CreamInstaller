using System.ComponentModel;
using System.Windows.Forms;
using CreamInstaller.Components;

namespace CreamInstaller.Forms
{
    partial class MainForm
    {
        private IContainer components = null;

        #region Windows Form Designer generated code

        /// <summary>
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.progressLabel = new System.Windows.Forms.Label();
            this.updateButton = new System.Windows.Forms.Button();
            this.ignoreButton = new System.Windows.Forms.Button();
            this.progressBar = new System.Windows.Forms.ProgressBar();
            this.changelogTreeView = new CustomTreeView();
            this.SuspendLayout();
            // 
            // progressLabel
            // 
            this.progressLabel.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.progressLabel.Location = new System.Drawing.Point(12, 16);
            this.progressLabel.Margin = new System.Windows.Forms.Padding(3, 0, 3, 12);
            this.progressLabel.Name = "progressLabel";
            this.progressLabel.Size = new System.Drawing.Size(218, 15);
            this.progressLabel.TabIndex = 0;
            this.progressLabel.Text = "Checking for updates . . .";
            // 
            // updateButton
            // 
            this.updateButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.updateButton.Enabled = false;
            this.updateButton.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.updateButton.Location = new System.Drawing.Point(317, 12);
            this.updateButton.Margin = new System.Windows.Forms.Padding(3, 3, 3, 12);
            this.updateButton.Name = "updateButton";
            this.updateButton.Size = new System.Drawing.Size(75, 23);
            this.updateButton.TabIndex = 2;
            this.updateButton.Text = "Update";
            this.updateButton.UseVisualStyleBackColor = true;
            // 
            // ignoreButton
            // 
            this.ignoreButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.ignoreButton.Enabled = false;
            this.ignoreButton.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.ignoreButton.Location = new System.Drawing.Point(236, 12);
            this.ignoreButton.Margin = new System.Windows.Forms.Padding(3, 3, 3, 12);
            this.ignoreButton.Name = "ignoreButton";
            this.ignoreButton.Size = new System.Drawing.Size(75, 23);
            this.ignoreButton.TabIndex = 1;
            this.ignoreButton.Text = "Ignore";
            this.ignoreButton.UseVisualStyleBackColor = true;
            this.ignoreButton.Click += new System.EventHandler(this.OnIgnore);
            // 
            // progressBar
            // 
            this.progressBar.Location = new System.Drawing.Point(12, 41);
            this.progressBar.Name = "progressBar";
            this.progressBar.Size = new System.Drawing.Size(380, 23);
            this.progressBar.TabIndex = 4;
            this.progressBar.Visible = false;
            // 
            // changelogTreeView
            // 
            this.changelogTreeView.DrawMode = System.Windows.Forms.TreeViewDrawMode.OwnerDrawAll;
            this.changelogTreeView.Location = new System.Drawing.Point(12, 70);
            this.changelogTreeView.Margin = new System.Windows.Forms.Padding(0, 0, 0, 12);
            this.changelogTreeView.Name = "changelogTreeView";
            this.changelogTreeView.Size = new System.Drawing.Size(380, 179);
            this.changelogTreeView.Sorted = true;
            this.changelogTreeView.TabIndex = 5;
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.AutoSize = true;
            this.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.ClientSize = new System.Drawing.Size(404, 261);
            this.Controls.Add(this.changelogTreeView);
            this.Controls.Add(this.progressBar);
            this.Controls.Add(this.ignoreButton);
            this.Controls.Add(this.updateButton);
            this.Controls.Add(this.progressLabel);
            this.DoubleBuffered = true;
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "MainForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "MainForm";
            this.Load += new System.EventHandler(this.OnLoad);
            this.ResumeLayout(false);

        }

        #endregion

        private Label progressLabel;
        private Button updateButton;
        private Button ignoreButton;
        private ProgressBar progressBar;
        private CustomTreeView changelogTreeView;
    }
}

