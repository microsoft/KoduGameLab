// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

namespace Boku
{
    partial class UpdateForm
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(UpdateForm));
            this.CurrentVersionLabel = new System.Windows.Forms.Label();
            this.NewVersionLabel = new System.Windows.Forms.Label();
            this.UpdateButton = new System.Windows.Forms.Button();
            this.IgnoreButton = new System.Windows.Forms.Button();
            this.RemindButton = new System.Windows.Forms.Button();
            this.MessageLabel = new System.Windows.Forms.LinkLabel();
            this.RelaseNotesLabel = new System.Windows.Forms.LinkLabel();
            this.NewVersion = new System.Windows.Forms.Label();
            this.CurrentVersion = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // CurrentVersionLabel
            // 
            this.CurrentVersionLabel.AutoSize = true;
            this.CurrentVersionLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.CurrentVersionLabel.Location = new System.Drawing.Point(22, 79);
            this.CurrentVersionLabel.Name = "CurrentVersionLabel";
            this.CurrentVersionLabel.Size = new System.Drawing.Size(102, 16);
            this.CurrentVersionLabel.TabIndex = 5;
            this.CurrentVersionLabel.Text = "Current Version:";
            // 
            // NewVersionLabel
            // 
            this.NewVersionLabel.AutoSize = true;
            this.NewVersionLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.NewVersionLabel.Location = new System.Drawing.Point(22, 95);
            this.NewVersionLabel.Name = "NewVersionLabel";
            this.NewVersionLabel.Size = new System.Drawing.Size(87, 16);
            this.NewVersionLabel.TabIndex = 7;
            this.NewVersionLabel.Text = "New Version:";
            // 
            // UpdateButton
            // 
            this.UpdateButton.DialogResult = System.Windows.Forms.DialogResult.Yes;
            this.UpdateButton.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.UpdateButton.Location = new System.Drawing.Point(25, 134);
            this.UpdateButton.Name = "UpdateButton";
            this.UpdateButton.Size = new System.Drawing.Size(158, 33);
            this.UpdateButton.TabIndex = 3;
            this.UpdateButton.Text = "Update Now";
            this.UpdateButton.UseVisualStyleBackColor = true;
            // 
            // IgnoreButton
            // 
            this.IgnoreButton.DialogResult = System.Windows.Forms.DialogResult.Ignore;
            this.IgnoreButton.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.IgnoreButton.Location = new System.Drawing.Point(189, 134);
            this.IgnoreButton.Name = "IgnoreButton";
            this.IgnoreButton.Size = new System.Drawing.Size(158, 33);
            this.IgnoreButton.TabIndex = 4;
            this.IgnoreButton.Text = "Ignore This Update";
            this.IgnoreButton.UseVisualStyleBackColor = true;
            // 
            // RemindButton
            // 
            this.RemindButton.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.RemindButton.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.RemindButton.Location = new System.Drawing.Point(353, 134);
            this.RemindButton.Name = "RemindButton";
            this.RemindButton.Size = new System.Drawing.Size(158, 33);
            this.RemindButton.TabIndex = 0;
            this.RemindButton.Text = "Remind Me Later";
            this.RemindButton.UseVisualStyleBackColor = true;
            // 
            // MessageLabel
            // 
            this.MessageLabel.AutoSize = true;
            this.MessageLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.MessageLabel.LinkArea = new System.Windows.Forms.LinkArea(17, 13);
            this.MessageLabel.Location = new System.Drawing.Point(25, 27);
            this.MessageLabel.Name = "MessageLabel";
            this.MessageLabel.Size = new System.Drawing.Size(275, 20);
            this.MessageLabel.TabIndex = 1;
            this.MessageLabel.TabStop = true;
            this.MessageLabel.Text = "A new version of Kodu Game Lab is avalable!";
            this.MessageLabel.UseCompatibleTextRendering = true;
            // 
            // RelaseNotesLabel
            // 
            this.RelaseNotesLabel.AutoSize = true;
            this.RelaseNotesLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.RelaseNotesLabel.LinkArea = new System.Windows.Forms.LinkArea(32, 13);
            this.RelaseNotesLabel.Location = new System.Drawing.Point(25, 47);
            this.RelaseNotesLabel.Name = "RelaseNotesLabel";
            this.RelaseNotesLabel.Size = new System.Drawing.Size(276, 20);
            this.RelaseNotesLabel.TabIndex = 2;
            this.RelaseNotesLabel.TabStop = true;
            this.RelaseNotesLabel.Text = "For more details please see the release notes";
            this.RelaseNotesLabel.UseCompatibleTextRendering = true;
            // 
            // NewVersion
            // 
            this.NewVersion.AutoSize = true;
            this.NewVersion.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.NewVersion.Location = new System.Drawing.Point(130, 95);
            this.NewVersion.Name = "NewVersion";
            this.NewVersion.Size = new System.Drawing.Size(59, 16);
            this.NewVersion.TabIndex = 8;
            this.NewVersion.Text = "1.4.111.0";
            // 
            // CurrentVersion
            // 
            this.CurrentVersion.AutoSize = true;
            this.CurrentVersion.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.CurrentVersion.Location = new System.Drawing.Point(130, 79);
            this.CurrentVersion.Name = "CurrentVersion";
            this.CurrentVersion.Size = new System.Drawing.Size(52, 16);
            this.CurrentVersion.TabIndex = 6;
            this.CurrentVersion.Text = "1.4.92.0";
            // 
            // UpdateForm
            // 
            this.AcceptButton = this.RemindButton;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.RemindButton;
            this.ClientSize = new System.Drawing.Size(537, 184);
            this.Controls.Add(this.NewVersion);
            this.Controls.Add(this.CurrentVersion);
            this.Controls.Add(this.RelaseNotesLabel);
            this.Controls.Add(this.MessageLabel);
            this.Controls.Add(this.RemindButton);
            this.Controls.Add(this.IgnoreButton);
            this.Controls.Add(this.UpdateButton);
            this.Controls.Add(this.NewVersionLabel);
            this.Controls.Add(this.CurrentVersionLabel);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "UpdateForm";
            this.Text = "Kodu Update Available";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        public System.Windows.Forms.LinkLabel MessageLabel;
        public System.Windows.Forms.LinkLabel RelaseNotesLabel;
        public System.Windows.Forms.Label NewVersion;
        public System.Windows.Forms.Label CurrentVersion;
        public System.Windows.Forms.Button UpdateButton;
        public System.Windows.Forms.Button IgnoreButton;
        public System.Windows.Forms.Button RemindButton;
        public System.Windows.Forms.Label CurrentVersionLabel;
        public System.Windows.Forms.Label NewVersionLabel;
    }
}
