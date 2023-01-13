using System.ComponentModel;
using System.Windows.Forms;

namespace CreamInstaller.Forms
{
    sealed partial class InstallForm
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
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.userProgressBar = new System.Windows.Forms.ProgressBar();
            this.userInfoLabel = new System.Windows.Forms.Label();
            this.acceptButton = new System.Windows.Forms.Button();
            this.retryButton = new System.Windows.Forms.Button();
            this.cancelButton = new System.Windows.Forms.Button();
            this.logTextBox = new System.Windows.Forms.RichTextBox();
            this.reselectButton = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // userProgressBar
            // 
            this.userProgressBar.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.userProgressBar.Location = new System.Drawing.Point(12, 27);
            this.userProgressBar.Name = "userProgressBar";
            this.userProgressBar.Size = new System.Drawing.Size(760, 23);
            this.userProgressBar.TabIndex = 1;
            // 
            // userInfoLabel
            // 
            this.userInfoLabel.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.userInfoLabel.AutoEllipsis = true;
            this.userInfoLabel.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.userInfoLabel.Location = new System.Drawing.Point(12, 9);
            this.userInfoLabel.Name = "userInfoLabel";
            this.userInfoLabel.Size = new System.Drawing.Size(760, 15);
            this.userInfoLabel.TabIndex = 2;
            this.userInfoLabel.Text = "Loading . . . ";
            // 
            // acceptButton
            // 
            this.acceptButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.acceptButton.Enabled = false;
            this.acceptButton.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.acceptButton.Location = new System.Drawing.Point(697, 526);
            this.acceptButton.Name = "acceptButton";
            this.acceptButton.Size = new System.Drawing.Size(75, 23);
            this.acceptButton.TabIndex = 4;
            this.acceptButton.Text = "OK";
            this.acceptButton.UseVisualStyleBackColor = true;
            this.acceptButton.Click += new System.EventHandler(this.OnAccept);
            // 
            // retryButton
            // 
            this.retryButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.retryButton.Enabled = false;
            this.retryButton.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.retryButton.Location = new System.Drawing.Point(616, 526);
            this.retryButton.Name = "retryButton";
            this.retryButton.Size = new System.Drawing.Size(75, 23);
            this.retryButton.TabIndex = 3;
            this.retryButton.Text = "Retry";
            this.retryButton.UseVisualStyleBackColor = true;
            this.retryButton.Click += new System.EventHandler(this.OnRetry);
            // 
            // cancelButton
            // 
            this.cancelButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.cancelButton.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.cancelButton.Location = new System.Drawing.Point(12, 526);
            this.cancelButton.Name = "cancelButton";
            this.cancelButton.Size = new System.Drawing.Size(75, 23);
            this.cancelButton.TabIndex = 1;
            this.cancelButton.Text = "Cancel";
            this.cancelButton.UseVisualStyleBackColor = true;
            this.cancelButton.Click += new System.EventHandler(this.OnCancel);
            // 
            // logTextBox
            // 
            this.logTextBox.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.logTextBox.HideSelection = false;
            this.logTextBox.Location = new System.Drawing.Point(12, 56);
            this.logTextBox.Name = "logTextBox";
            this.logTextBox.ReadOnly = true;
            this.logTextBox.ScrollBars = System.Windows.Forms.RichTextBoxScrollBars.ForcedBoth;
            this.logTextBox.Size = new System.Drawing.Size(760, 464);
            this.logTextBox.TabIndex = 4;
            this.logTextBox.TabStop = false;
            this.logTextBox.Text = "";
            // 
            // reselectButton
            // 
            this.reselectButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.reselectButton.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.reselectButton.Location = new System.Drawing.Point(410, 526);
            this.reselectButton.Name = "reselectButton";
            this.reselectButton.Size = new System.Drawing.Size(200, 23);
            this.reselectButton.TabIndex = 2;
            this.reselectButton.Text = "Reselect Programs / Games";
            this.reselectButton.UseVisualStyleBackColor = true;
            this.reselectButton.Click += new System.EventHandler(this.OnReselect);
            // 
            // InstallForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.AutoSize = true;
            this.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.ClientSize = new System.Drawing.Size(784, 561);
            this.Controls.Add(this.reselectButton);
            this.Controls.Add(this.logTextBox);
            this.Controls.Add(this.cancelButton);
            this.Controls.Add(this.retryButton);
            this.Controls.Add(this.acceptButton);
            this.Controls.Add(this.userProgressBar);
            this.Controls.Add(this.userInfoLabel);
            this.DoubleBuffered = true;
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "InstallForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.Manual;
            this.Text = "InstallForm";
            this.Load += new System.EventHandler(this.OnLoad);
            this.ResumeLayout(false);

        }

        #endregion

        private ProgressBar userProgressBar;
        private Label userInfoLabel;
        private Button acceptButton;
        private Button retryButton;
        private Button cancelButton;
        private RichTextBox logTextBox;
        private Button reselectButton;
    }
}

