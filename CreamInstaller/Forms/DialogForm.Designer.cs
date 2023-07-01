using System.ComponentModel;
using System.Windows.Forms;

namespace CreamInstaller.Forms
{
    partial class DialogForm
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
            cancelButton = new Button();
            acceptButton = new Button();
            descriptionLabel = new LinkLabel();
            icon = new PictureBox();
            descriptionPanel = new FlowLayoutPanel();
            descriptionLabelPanel = new FlowLayoutPanel();
            buttonPanel = new FlowLayoutPanel();
            ((ISupportInitialize)icon).BeginInit();
            descriptionPanel.SuspendLayout();
            descriptionLabelPanel.SuspendLayout();
            buttonPanel.SuspendLayout();
            SuspendLayout();
            // 
            // cancelButton
            // 
            cancelButton.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            cancelButton.AutoSize = true;
            cancelButton.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            cancelButton.DialogResult = DialogResult.Cancel;
            cancelButton.Location = new System.Drawing.Point(136, 10);
            cancelButton.Name = "cancelButton";
            cancelButton.Padding = new Padding(12, 0, 12, 0);
            cancelButton.Size = new System.Drawing.Size(115, 24);
            cancelButton.TabIndex = 1;
            cancelButton.Text = "cancelButton";
            cancelButton.UseVisualStyleBackColor = true;
            // 
            // acceptButton
            // 
            acceptButton.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            acceptButton.AutoSize = true;
            acceptButton.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            acceptButton.DialogResult = DialogResult.OK;
            acceptButton.Location = new System.Drawing.Point(257, 9);
            acceptButton.Name = "acceptButton";
            acceptButton.Padding = new Padding(12, 0, 12, 0);
            acceptButton.Size = new System.Drawing.Size(112, 25);
            acceptButton.TabIndex = 0;
            acceptButton.Text = "acceptButton";
            acceptButton.UseVisualStyleBackColor = true;
            // 
            // descriptionLabel
            // 
            descriptionLabel.AutoSize = true;
            descriptionLabel.LinkArea = new LinkArea(0, 0);
            descriptionLabel.Location = new System.Drawing.Point(9, 0);
            descriptionLabel.Margin = new Padding(9, 0, 3, 0);
            descriptionLabel.Name = "descriptionLabel";
            descriptionLabel.Size = new System.Drawing.Size(94, 15);
            descriptionLabel.TabIndex = 2;
            descriptionLabel.Text = "descriptionLabel";
            // 
            // icon
            // 
            icon.Location = new System.Drawing.Point(15, 15);
            icon.Name = "icon";
            icon.Size = new System.Drawing.Size(48, 48);
            icon.SizeMode = PictureBoxSizeMode.AutoSize;
            icon.TabIndex = 4;
            icon.TabStop = false;
            // 
            // descriptionPanel
            // 
            descriptionPanel.AutoSize = true;
            descriptionPanel.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            descriptionPanel.Controls.Add(icon);
            descriptionPanel.Controls.Add(descriptionLabelPanel);
            descriptionPanel.Dock = DockStyle.Fill;
            descriptionPanel.Location = new System.Drawing.Point(0, 0);
            descriptionPanel.Margin = new Padding(0);
            descriptionPanel.Name = "descriptionPanel";
            descriptionPanel.Padding = new Padding(12, 12, 12, 6);
            descriptionPanel.Size = new System.Drawing.Size(384, 72);
            descriptionPanel.TabIndex = 5;
            // 
            // descriptionLabelPanel
            // 
            descriptionLabelPanel.AutoScroll = true;
            descriptionLabelPanel.AutoSize = true;
            descriptionLabelPanel.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            descriptionLabelPanel.Controls.Add(descriptionLabel);
            descriptionLabelPanel.Dock = DockStyle.Fill;
            descriptionLabelPanel.Location = new System.Drawing.Point(66, 12);
            descriptionLabelPanel.Margin = new Padding(0);
            descriptionLabelPanel.Name = "descriptionLabelPanel";
            descriptionLabelPanel.Size = new System.Drawing.Size(106, 54);
            descriptionLabelPanel.TabIndex = 6;
            // 
            // buttonPanel
            // 
            buttonPanel.AutoSize = true;
            buttonPanel.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            buttonPanel.Controls.Add(acceptButton);
            buttonPanel.Controls.Add(cancelButton);
            buttonPanel.Dock = DockStyle.Bottom;
            buttonPanel.FlowDirection = FlowDirection.RightToLeft;
            buttonPanel.Location = new System.Drawing.Point(0, 72);
            buttonPanel.Name = "buttonPanel";
            buttonPanel.Padding = new Padding(12, 6, 0, 12);
            buttonPanel.Size = new System.Drawing.Size(384, 49);
            buttonPanel.TabIndex = 6;
            // 
            // DialogForm
            // 
            AcceptButton = acceptButton;
            AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            AutoSize = true;
            AutoSizeMode = AutoSizeMode.GrowAndShrink;
            CancelButton = cancelButton;
            ClientSize = new System.Drawing.Size(384, 121);
            Controls.Add(descriptionPanel);
            Controls.Add(buttonPanel);
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            MaximumSize = new System.Drawing.Size(1600, 900);
            MinimizeBox = false;
            MinimumSize = new System.Drawing.Size(400, 160);
            Name = "DialogForm";
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.CenterParent;
            Text = "DialogForm";
            ((ISupportInitialize)icon).EndInit();
            descriptionPanel.ResumeLayout(false);
            descriptionPanel.PerformLayout();
            descriptionLabelPanel.ResumeLayout(false);
            descriptionLabelPanel.PerformLayout();
            buttonPanel.ResumeLayout(false);
            buttonPanel.PerformLayout();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private Button cancelButton;
        private Button acceptButton;
        private PictureBox icon;
        private FlowLayoutPanel descriptionPanel;
        private FlowLayoutPanel buttonPanel;
        private LinkLabel descriptionLabel;
        private FlowLayoutPanel descriptionLabelPanel;
    }
}