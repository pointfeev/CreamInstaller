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
            userProgressBar = new ProgressBar();
            userInfoLabel = new Label();
            acceptButton = new Button();
            retryButton = new Button();
            cancelButton = new Button();
            logTextBox = new RichTextBox();
            reselectButton = new Button();
            SuspendLayout();
            // 
            // userProgressBar
            // 
            userProgressBar.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            userProgressBar.Location = new System.Drawing.Point(12, 27);
            userProgressBar.Name = "userProgressBar";
            userProgressBar.Size = new System.Drawing.Size(760, 23);
            userProgressBar.TabIndex = 1;
            // 
            // userInfoLabel
            // 
            userInfoLabel.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            userInfoLabel.AutoEllipsis = true;
            userInfoLabel.Location = new System.Drawing.Point(12, 9);
            userInfoLabel.Name = "userInfoLabel";
            userInfoLabel.Size = new System.Drawing.Size(760, 15);
            userInfoLabel.TabIndex = 2;
            userInfoLabel.Text = "Loading . . . ";
            // 
            // acceptButton
            // 
            acceptButton.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            acceptButton.Enabled = false;
            acceptButton.Location = new System.Drawing.Point(697, 526);
            acceptButton.Name = "acceptButton";
            acceptButton.Size = new System.Drawing.Size(75, 23);
            acceptButton.TabIndex = 4;
            acceptButton.Text = "OK";
            acceptButton.UseVisualStyleBackColor = true;
            acceptButton.Click += OnAccept;
            // 
            // retryButton
            // 
            retryButton.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            retryButton.Enabled = false;
            retryButton.Location = new System.Drawing.Point(616, 526);
            retryButton.Name = "retryButton";
            retryButton.Size = new System.Drawing.Size(75, 23);
            retryButton.TabIndex = 3;
            retryButton.Text = "Retry";
            retryButton.UseVisualStyleBackColor = true;
            retryButton.Click += OnRetry;
            // 
            // cancelButton
            // 
            cancelButton.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            cancelButton.Location = new System.Drawing.Point(12, 526);
            cancelButton.Name = "cancelButton";
            cancelButton.Size = new System.Drawing.Size(75, 23);
            cancelButton.TabIndex = 1;
            cancelButton.Text = "Cancel";
            cancelButton.UseVisualStyleBackColor = true;
            cancelButton.Click += OnCancel;
            // 
            // logTextBox
            // 
            logTextBox.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            logTextBox.HideSelection = false;
            logTextBox.Location = new System.Drawing.Point(12, 56);
            logTextBox.Name = "logTextBox";
            logTextBox.ReadOnly = true;
            logTextBox.ScrollBars = RichTextBoxScrollBars.ForcedBoth;
            logTextBox.Size = new System.Drawing.Size(760, 464);
            logTextBox.TabIndex = 4;
            logTextBox.TabStop = false;
            logTextBox.Text = "";
            // 
            // reselectButton
            // 
            reselectButton.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            reselectButton.Location = new System.Drawing.Point(410, 526);
            reselectButton.Name = "reselectButton";
            reselectButton.Size = new System.Drawing.Size(200, 23);
            reselectButton.TabIndex = 2;
            reselectButton.Text = "Reselect Programs / Games";
            reselectButton.UseVisualStyleBackColor = true;
            reselectButton.Click += OnReselect;
            // 
            // InstallForm
            // 
            AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            AutoSize = true;
            AutoSizeMode = AutoSizeMode.GrowAndShrink;
            ClientSize = new System.Drawing.Size(784, 561);
            Controls.Add(reselectButton);
            Controls.Add(logTextBox);
            Controls.Add(cancelButton);
            Controls.Add(retryButton);
            Controls.Add(acceptButton);
            Controls.Add(userProgressBar);
            Controls.Add(userInfoLabel);
            DoubleBuffered = true;
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "InstallForm";
            StartPosition = FormStartPosition.Manual;
            Text = "InstallForm";
            Load += OnLoad;
            ResumeLayout(false);
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

