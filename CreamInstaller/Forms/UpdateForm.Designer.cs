using System.ComponentModel;
using System.Windows.Forms;

using CreamInstaller.Components;

namespace CreamInstaller.Forms
{
    partial class UpdateForm
    {
        private IContainer components = null;

        #region Windows Form Designer generated code

        /// <summary>
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            progressLabel = new Label();
            updateButton = new Button();
            ignoreButton = new Button();
            progressBar = new ProgressBar();
            changelogTreeView = new CustomTreeView();
            SuspendLayout();
            // 
            // progressLabel
            // 
            progressLabel.Location = new System.Drawing.Point(12, 16);
            progressLabel.Margin = new Padding(3, 0, 3, 12);
            progressLabel.Name = "progressLabel";
            progressLabel.Size = new System.Drawing.Size(218, 15);
            progressLabel.TabIndex = 0;
            progressLabel.Text = "Checking for updates . . .";
            // 
            // updateButton
            // 
            updateButton.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            updateButton.Enabled = false;
            updateButton.Location = new System.Drawing.Point(317, 12);
            updateButton.Margin = new Padding(3, 3, 3, 12);
            updateButton.Name = "updateButton";
            updateButton.Size = new System.Drawing.Size(75, 23);
            updateButton.TabIndex = 2;
            updateButton.Text = "Update";
            updateButton.UseVisualStyleBackColor = true;
            // 
            // ignoreButton
            // 
            ignoreButton.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            ignoreButton.Enabled = false;
            ignoreButton.Location = new System.Drawing.Point(236, 12);
            ignoreButton.Margin = new Padding(3, 3, 3, 12);
            ignoreButton.Name = "ignoreButton";
            ignoreButton.Size = new System.Drawing.Size(75, 23);
            ignoreButton.TabIndex = 1;
            ignoreButton.Text = "Ignore";
            ignoreButton.UseVisualStyleBackColor = true;
            ignoreButton.Click += OnIgnore;
            // 
            // progressBar
            // 
            progressBar.Location = new System.Drawing.Point(12, 41);
            progressBar.Name = "progressBar";
            progressBar.Size = new System.Drawing.Size(380, 23);
            progressBar.TabIndex = 4;
            progressBar.Visible = false;
            // 
            // changelogTreeView
            // 
            changelogTreeView.Location = new System.Drawing.Point(12, 70);
            changelogTreeView.Margin = new Padding(0, 0, 0, 12);
            changelogTreeView.Name = "changelogTreeView";
            changelogTreeView.Size = new System.Drawing.Size(380, 179);
            changelogTreeView.TabIndex = 5;
            // 
            // UpdateForm
            // 
            AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            AutoSize = true;
            AutoSizeMode = AutoSizeMode.GrowAndShrink;
            ClientSize = new System.Drawing.Size(404, 261);
            Controls.Add(changelogTreeView);
            Controls.Add(progressBar);
            Controls.Add(ignoreButton);
            Controls.Add(updateButton);
            Controls.Add(progressLabel);
            DoubleBuffered = true;
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "UpdateForm";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "UpdateForm";
            Load += OnLoad;
            ResumeLayout(false);
        }

        #endregion

        private Label progressLabel;
        private Button updateButton;
        private Button ignoreButton;
        private ProgressBar progressBar;
        private CustomTreeView changelogTreeView;
    }
}

