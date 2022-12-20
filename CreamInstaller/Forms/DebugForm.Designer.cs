using System.ComponentModel;
using System.Windows.Forms;

namespace CreamInstaller.Forms;

partial class DebugForm
{
    /// <summary>
    /// Required designer variable.
    /// </summary>
    private IContainer components = null;

    /// <summary>
    /// Clean up any resources being used.
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
    /// Required method for Designer support - do not modify
    /// the contents of this method with the code editor.
    /// </summary>
    private void InitializeComponent()
    {
            this.debugTextBox = new System.Windows.Forms.RichTextBox();
            this.SuspendLayout();
            // 
            // debugTextBox
            // 
            this.debugTextBox.Dock = System.Windows.Forms.DockStyle.Fill;
            this.debugTextBox.Location = new System.Drawing.Point(10, 10);
            this.debugTextBox.Name = "debugTextBox";
            this.debugTextBox.ReadOnly = true;
            this.debugTextBox.ScrollBars = System.Windows.Forms.RichTextBoxScrollBars.ForcedBoth;
            this.debugTextBox.Size = new System.Drawing.Size(544, 321);
            this.debugTextBox.TabIndex = 0;
            this.debugTextBox.TabStop = false;
            this.debugTextBox.Text = "";
            // 
            // DebugForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(564, 341);
            this.ControlBox = false;
            this.Controls.Add(this.debugTextBox);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "DebugForm";
            this.Padding = new System.Windows.Forms.Padding(10);
            this.ShowIcon = false;
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.Manual;
            this.Text = "Debug";
            this.ResumeLayout(false);

    }

    #endregion

    private RichTextBox debugTextBox;
}