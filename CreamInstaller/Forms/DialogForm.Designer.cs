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
            this.cancelButton = new System.Windows.Forms.Button();
            this.acceptButton = new System.Windows.Forms.Button();
            this.descriptionLabel = new System.Windows.Forms.LinkLabel();
            this.icon = new System.Windows.Forms.PictureBox();
            this.descriptionPanel = new System.Windows.Forms.FlowLayoutPanel();
            this.buttonPanel = new System.Windows.Forms.FlowLayoutPanel();
            ((System.ComponentModel.ISupportInitialize)(this.icon)).BeginInit();
            this.descriptionPanel.SuspendLayout();
            this.buttonPanel.SuspendLayout();
            this.SuspendLayout();
            // 
            // cancelButton
            // 
            this.cancelButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.cancelButton.AutoSize = true;
            this.cancelButton.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.cancelButton.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.cancelButton.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.cancelButton.Location = new System.Drawing.Point(32, 9);
            this.cancelButton.Name = "cancelButton";
            this.cancelButton.Padding = new System.Windows.Forms.Padding(12, 0, 12, 0);
            this.cancelButton.Size = new System.Drawing.Size(115, 24);
            this.cancelButton.TabIndex = 1;
            this.cancelButton.Text = "cancelButton";
            this.cancelButton.UseVisualStyleBackColor = true;
            // 
            // acceptButton
            // 
            this.acceptButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.acceptButton.AutoSize = true;
            this.acceptButton.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.acceptButton.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.acceptButton.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.acceptButton.Location = new System.Drawing.Point(153, 9);
            this.acceptButton.Name = "acceptButton";
            this.acceptButton.Padding = new System.Windows.Forms.Padding(12, 0, 12, 0);
            this.acceptButton.Size = new System.Drawing.Size(116, 24);
            this.acceptButton.TabIndex = 0;
            this.acceptButton.Text = "acceptButton";
            this.acceptButton.UseVisualStyleBackColor = true;
            // 
            // descriptionLabel
            // 
            this.descriptionLabel.AutoSize = true;
            this.descriptionLabel.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.descriptionLabel.LinkArea = new System.Windows.Forms.LinkArea(0, 0);
            this.descriptionLabel.Location = new System.Drawing.Point(75, 12);
            this.descriptionLabel.Margin = new System.Windows.Forms.Padding(9, 0, 3, 0);
            this.descriptionLabel.Name = "descriptionLabel";
            this.descriptionLabel.Size = new System.Drawing.Size(94, 15);
            this.descriptionLabel.TabIndex = 2;
            this.descriptionLabel.Text = "descriptionLabel";
            // 
            // icon
            // 
            this.icon.Location = new System.Drawing.Point(15, 15);
            this.icon.Name = "icon";
            this.icon.Size = new System.Drawing.Size(48, 48);
            this.icon.SizeMode = System.Windows.Forms.PictureBoxSizeMode.AutoSize;
            this.icon.TabIndex = 4;
            this.icon.TabStop = false;
            // 
            // descriptionPanel
            // 
            this.descriptionPanel.AutoSize = true;
            this.descriptionPanel.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.descriptionPanel.Controls.Add(this.icon);
            this.descriptionPanel.Controls.Add(this.descriptionLabel);
            this.descriptionPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.descriptionPanel.Location = new System.Drawing.Point(0, 0);
            this.descriptionPanel.Margin = new System.Windows.Forms.Padding(0);
            this.descriptionPanel.Name = "descriptionPanel";
            this.descriptionPanel.Padding = new System.Windows.Forms.Padding(12, 12, 12, 6);
            this.descriptionPanel.Size = new System.Drawing.Size(284, 73);
            this.descriptionPanel.TabIndex = 5;
            // 
            // buttonPanel
            // 
            this.buttonPanel.AutoSize = true;
            this.buttonPanel.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.buttonPanel.Controls.Add(this.acceptButton);
            this.buttonPanel.Controls.Add(this.cancelButton);
            this.buttonPanel.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.buttonPanel.FlowDirection = System.Windows.Forms.FlowDirection.RightToLeft;
            this.buttonPanel.Location = new System.Drawing.Point(0, 73);
            this.buttonPanel.Name = "buttonPanel";
            this.buttonPanel.Padding = new System.Windows.Forms.Padding(12, 6, 0, 12);
            this.buttonPanel.Size = new System.Drawing.Size(284, 48);
            this.buttonPanel.TabIndex = 6;
            // 
            // DialogForm
            // 
            this.AcceptButton = this.acceptButton;
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.AutoSize = true;
            this.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.CancelButton = this.cancelButton;
            this.ClientSize = new System.Drawing.Size(284, 121);
            this.Controls.Add(this.descriptionPanel);
            this.Controls.Add(this.buttonPanel);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "DialogForm";
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "DialogForm";
            ((System.ComponentModel.ISupportInitialize)(this.icon)).EndInit();
            this.descriptionPanel.ResumeLayout(false);
            this.descriptionPanel.PerformLayout();
            this.buttonPanel.ResumeLayout(false);
            this.buttonPanel.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private Button cancelButton;
        private Button acceptButton;
        private PictureBox icon;
        private FlowLayoutPanel descriptionPanel;
        private FlowLayoutPanel buttonPanel;
        private LinkLabel descriptionLabel;
    }
}