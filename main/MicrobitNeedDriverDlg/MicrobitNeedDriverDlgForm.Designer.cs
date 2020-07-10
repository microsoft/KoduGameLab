// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

namespace MicrobitNeedDriverDlg
{
    partial class MicrobitNeedDriverDlgForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

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
            this.message = new System.Windows.Forms.Label();
            this.linkLabel = new System.Windows.Forms.LinkLabel();
            this.installBtn = new System.Windows.Forms.Button();
            this.cancelBtn = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // message
            // 
            this.message.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.message.Location = new System.Drawing.Point(45, 20);
            this.message.Name = "message";
            this.message.Size = new System.Drawing.Size(436, 74);
            this.message.TabIndex = 0;
            this.message.Text = "We need to install a driver so that Kodu can talk to your BBC micro:bit. Click th" +
    "e Install button to begin. Click the link below for more information.";
            // 
            // linkLabel
            // 
            this.linkLabel.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.linkLabel.Location = new System.Drawing.Point(49, 94);
            this.linkLabel.Name = "linkLabel";
            this.linkLabel.Size = new System.Drawing.Size(432, 23);
            this.linkLabel.TabIndex = 1;
            this.linkLabel.TabStop = true;
            this.linkLabel.Text = "Kodu && BBC micro:bit - Getting Started Guide";
            this.linkLabel.TextAlign = System.Drawing.ContentAlignment.TopCenter;
            this.linkLabel.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.linkLabel1_LinkClicked);
            // 
            // installBtn
            // 
            this.installBtn.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.installBtn.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.installBtn.Location = new System.Drawing.Point(417, 149);
            this.installBtn.Name = "installBtn";
            this.installBtn.Size = new System.Drawing.Size(91, 36);
            this.installBtn.TabIndex = 2;
            this.installBtn.Text = "Install";
            this.installBtn.UseVisualStyleBackColor = true;
            // 
            // cancelBtn
            // 
            this.cancelBtn.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.cancelBtn.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.cancelBtn.Location = new System.Drawing.Point(307, 149);
            this.cancelBtn.Name = "cancelBtn";
            this.cancelBtn.Size = new System.Drawing.Size(91, 36);
            this.cancelBtn.TabIndex = 3;
            this.cancelBtn.Text = "Cancel";
            this.cancelBtn.UseVisualStyleBackColor = true;
            // 
            // MicrobitNeedDriverDlgForm
            // 
            this.AcceptButton = this.installBtn;
            this.AutoScaleDimensions = new System.Drawing.SizeF(144F, 144F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
            this.CancelButton = this.cancelBtn;
            this.ClientSize = new System.Drawing.Size(531, 209);
            this.Controls.Add(this.cancelBtn);
            this.Controls.Add(this.installBtn);
            this.Controls.Add(this.linkLabel);
            this.Controls.Add(this.message);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "MicrobitNeedDriverDlgForm";
            this.Padding = new System.Windows.Forms.Padding(20);
            this.Text = "BBC micro:bit needs driver";
            this.Load += new System.EventHandler(this.MicrobitNeedDriverDlgForm_Load);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Label message;
        private System.Windows.Forms.LinkLabel linkLabel;
        private System.Windows.Forms.Button installBtn;
        private System.Windows.Forms.Button cancelBtn;

    }
}
