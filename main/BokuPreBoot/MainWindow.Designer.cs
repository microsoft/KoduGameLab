namespace BokuPreBoot
{
    partial class MainWindow
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
            this.CancelBtn = new System.Windows.Forms.Button();
            this.pictureBox1 = new System.Windows.Forms.PictureBox();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.AnimationCk = new System.Windows.Forms.CheckBox();
            this.AntiAliasCk = new System.Windows.Forms.CheckBox();
            this.PostFXCk = new System.Windows.Forms.CheckBox();
            this.Apply = new System.Windows.Forms.Button();
            this.AudioCk = new System.Windows.Forms.CheckBox();
            this.StatusTB = new System.Windows.Forms.TextBox();
            this.ShadMod2RB = new System.Windows.Forms.RadioButton();
            this.groupBox3 = new System.Windows.Forms.GroupBox();
            this.ShadMod3RB = new System.Windows.Forms.RadioButton();
            this.groupBox4 = new System.Windows.Forms.GroupBox();
            this.communityCk = new System.Windows.Forms.CheckBox();
            this.groupBox6 = new System.Windows.Forms.GroupBox();
            this.SavePath = new System.Windows.Forms.TextBox();
            this.UserFolder = new System.Windows.Forms.Button();
            this.VsyncCk = new System.Windows.Forms.CheckBox();
            this.SpriteFontCk = new System.Windows.Forms.CheckBox();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).BeginInit();
            this.groupBox1.SuspendLayout();
            this.groupBox3.SuspendLayout();
            this.groupBox4.SuspendLayout();
            this.groupBox6.SuspendLayout();
            this.SuspendLayout();
            // 
            // CancelBtn
            // 
            this.CancelBtn.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.CancelBtn.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.CancelBtn.Location = new System.Drawing.Point(348, 308);
            this.CancelBtn.Name = "CancelBtn";
            this.CancelBtn.Size = new System.Drawing.Size(89, 23);
            this.CancelBtn.TabIndex = 7;
            this.CancelBtn.Text = "Cancel";
            this.CancelBtn.UseVisualStyleBackColor = true;
            this.CancelBtn.Click += new System.EventHandler(this.CancelBtn_Click);
            // 
            // pictureBox1
            // 
            this.pictureBox1.Image = global::BokuPreBoot.Properties.Resources.VectorBoku;
            this.pictureBox1.Location = new System.Drawing.Point(12, 12);
            this.pictureBox1.Name = "pictureBox1";
            this.pictureBox1.Size = new System.Drawing.Size(54, 64);
            this.pictureBox1.SizeMode = System.Windows.Forms.PictureBoxSizeMode.StretchImage;
            this.pictureBox1.TabIndex = 4;
            this.pictureBox1.TabStop = false;
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.AnimationCk);
            this.groupBox1.Controls.Add(this.AntiAliasCk);
            this.groupBox1.Controls.Add(this.PostFXCk);
            this.groupBox1.Location = new System.Drawing.Point(274, 89);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(257, 93);
            this.groupBox1.TabIndex = 4;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Visual Effects";
            // 
            // AnimationCk
            // 
            this.AnimationCk.AutoSize = true;
            this.AnimationCk.Checked = true;
            this.AnimationCk.CheckState = System.Windows.Forms.CheckState.Checked;
            this.AnimationCk.Location = new System.Drawing.Point(7, 68);
            this.AnimationCk.Name = "AnimationCk";
            this.AnimationCk.Size = new System.Drawing.Size(72, 17);
            this.AnimationCk.TabIndex = 2;
            this.AnimationCk.Text = "Animation";
            this.AnimationCk.UseVisualStyleBackColor = true;
            // 
            // AntiAliasCk
            // 
            this.AntiAliasCk.AutoSize = true;
            this.AntiAliasCk.Checked = true;
            this.AntiAliasCk.CheckState = System.Windows.Forms.CheckState.Checked;
            this.AntiAliasCk.Location = new System.Drawing.Point(7, 44);
            this.AntiAliasCk.Name = "AntiAliasCk";
            this.AntiAliasCk.Size = new System.Drawing.Size(112, 17);
            this.AntiAliasCk.TabIndex = 1;
            this.AntiAliasCk.Text = "Smoothing (FSAA)";
            this.AntiAliasCk.UseVisualStyleBackColor = true;
            // 
            // PostFXCk
            // 
            this.PostFXCk.AutoSize = true;
            this.PostFXCk.Checked = true;
            this.PostFXCk.CheckState = System.Windows.Forms.CheckState.Checked;
            this.PostFXCk.Location = new System.Drawing.Point(7, 20);
            this.PostFXCk.Name = "PostFXCk";
            this.PostFXCk.Size = new System.Drawing.Size(156, 17);
            this.PostFXCk.TabIndex = 0;
            this.PostFXCk.Text = "Glow, Distortion, and Focus";
            this.PostFXCk.UseVisualStyleBackColor = true;
            // 
            // Apply
            // 
            this.Apply.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.Apply.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.Apply.Location = new System.Drawing.Point(442, 308);
            this.Apply.Name = "Apply";
            this.Apply.Size = new System.Drawing.Size(89, 23);
            this.Apply.TabIndex = 8;
            this.Apply.Text = "OK";
            this.Apply.UseVisualStyleBackColor = true;
            this.Apply.Click += new System.EventHandler(this.Apply_Click);
            // 
            // AudioCk
            // 
            this.AudioCk.AutoSize = true;
            this.AudioCk.Checked = true;
            this.AudioCk.CheckState = System.Windows.Forms.CheckState.Checked;
            this.AudioCk.Location = new System.Drawing.Point(7, 21);
            this.AudioCk.Name = "AudioCk";
            this.AudioCk.Size = new System.Drawing.Size(89, 17);
            this.AudioCk.TabIndex = 0;
            this.AudioCk.Text = "Enable Audio";
            this.AudioCk.UseVisualStyleBackColor = true;
            // 
            // StatusTB
            // 
            this.StatusTB.Location = new System.Drawing.Point(72, 12);
            this.StatusTB.Multiline = true;
            this.StatusTB.Name = "StatusTB";
            this.StatusTB.ReadOnly = true;
            this.StatusTB.Size = new System.Drawing.Size(459, 64);
            this.StatusTB.TabIndex = 0;
            this.StatusTB.TabStop = false;
            this.StatusTB.Text = "This computer does not support HiDef; only Standard graphics options will be avai" +
    "lable. Advanced features will be disabled.";
            // 
            // ShadMod2RB
            // 
            this.ShadMod2RB.AutoSize = true;
            this.ShadMod2RB.Location = new System.Drawing.Point(8, 19);
            this.ShadMod2RB.Name = "ShadMod2RB";
            this.ShadMod2RB.Size = new System.Drawing.Size(109, 17);
            this.ShadMod2RB.TabIndex = 0;
            this.ShadMod2RB.TabStop = true;
            this.ShadMod2RB.Text = "Standard (Reach)";
            this.ShadMod2RB.UseVisualStyleBackColor = true;
            this.ShadMod2RB.CheckedChanged += new System.EventHandler(this.ShadMod2RB_CheckedChanged);
            // 
            // groupBox3
            // 
            this.groupBox3.Controls.Add(this.ShadMod3RB);
            this.groupBox3.Controls.Add(this.ShadMod2RB);
            this.groupBox3.Location = new System.Drawing.Point(12, 89);
            this.groupBox3.Name = "groupBox3";
            this.groupBox3.Size = new System.Drawing.Size(257, 73);
            this.groupBox3.TabIndex = 2;
            this.groupBox3.TabStop = false;
            this.groupBox3.Text = "Graphics Quality";
            // 
            // ShadMod3RB
            // 
            this.ShadMod3RB.AutoSize = true;
            this.ShadMod3RB.Location = new System.Drawing.Point(8, 45);
            this.ShadMod3RB.Name = "ShadMod3RB";
            this.ShadMod3RB.Size = new System.Drawing.Size(110, 17);
            this.ShadMod3RB.TabIndex = 1;
            this.ShadMod3RB.TabStop = true;
            this.ShadMod3RB.Text = "Advanced (HiDef)";
            this.ShadMod3RB.UseVisualStyleBackColor = true;
            // 
            // groupBox4
            // 
            this.groupBox4.Controls.Add(this.communityCk);
            this.groupBox4.Controls.Add(this.AudioCk);
            this.groupBox4.Location = new System.Drawing.Point(13, 168);
            this.groupBox4.Name = "groupBox4";
            this.groupBox4.Size = new System.Drawing.Size(256, 69);
            this.groupBox4.TabIndex = 5;
            this.groupBox4.TabStop = false;
            this.groupBox4.Text = "Other Settings";
            // 
            // communityCk
            // 
            this.communityCk.AutoSize = true;
            this.communityCk.Checked = true;
            this.communityCk.CheckState = System.Windows.Forms.CheckState.Checked;
            this.communityCk.Location = new System.Drawing.Point(7, 45);
            this.communityCk.Name = "communityCk";
            this.communityCk.Size = new System.Drawing.Size(178, 17);
            this.communityCk.TabIndex = 1;
            this.communityCk.Text = "Show Community on Main Menu";
            this.communityCk.UseVisualStyleBackColor = true;
            // 
            // groupBox6
            // 
            this.groupBox6.Controls.Add(this.SavePath);
            this.groupBox6.Controls.Add(this.UserFolder);
            this.groupBox6.Location = new System.Drawing.Point(12, 243);
            this.groupBox6.Name = "groupBox6";
            this.groupBox6.Size = new System.Drawing.Size(520, 54);
            this.groupBox6.TabIndex = 6;
            this.groupBox6.TabStop = false;
            this.groupBox6.Text = "Save Folder";
            // 
            // SavePath
            // 
            this.SavePath.Location = new System.Drawing.Point(7, 21);
            this.SavePath.Name = "SavePath";
            this.SavePath.Size = new System.Drawing.Size(442, 20);
            this.SavePath.TabIndex = 0;
            this.SavePath.TextChanged += new System.EventHandler(this.SavePath_TextChanged);
            // 
            // UserFolder
            // 
            this.UserFolder.Location = new System.Drawing.Point(467, 19);
            this.UserFolder.Name = "UserFolder";
            this.UserFolder.Size = new System.Drawing.Size(32, 23);
            this.UserFolder.TabIndex = 1;
            this.UserFolder.Text = "...";
            this.UserFolder.UseVisualStyleBackColor = true;
            this.UserFolder.Click += new System.EventHandler(this.UserFolder_Click);
            // 
            // VsyncCk
            // 
            this.VsyncCk.AutoSize = true;
            this.VsyncCk.Location = new System.Drawing.Point(281, 189);
            this.VsyncCk.Name = "VsyncCk";
            this.VsyncCk.Size = new System.Drawing.Size(57, 17);
            this.VsyncCk.TabIndex = 9;
            this.VsyncCk.Text = "VSync";
            this.VsyncCk.UseVisualStyleBackColor = true;
            // 
            // SpriteFontCk
            // 
            this.SpriteFontCk.AutoSize = true;
            this.SpriteFontCk.Location = new System.Drawing.Point(281, 213);
            this.SpriteFontCk.Name = "SpriteFontCk";
            this.SpriteFontCk.Size = new System.Drawing.Size(172, 17);
            this.SpriteFontCk.TabIndex = 10;
            this.SpriteFontCk.Text = "Use SpriteFont Text Rendering";
            this.SpriteFontCk.UseVisualStyleBackColor = true;
            // 
            // MainWindow
            // 
            this.AcceptButton = this.Apply;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.CancelBtn;
            this.ClientSize = new System.Drawing.Size(544, 343);
            this.Controls.Add(this.SpriteFontCk);
            this.Controls.Add(this.VsyncCk);
            this.Controls.Add(this.groupBox6);
            this.Controls.Add(this.groupBox4);
            this.Controls.Add(this.groupBox3);
            this.Controls.Add(this.StatusTB);
            this.Controls.Add(this.Apply);
            this.Controls.Add(this.groupBox1);
            this.Controls.Add(this.pictureBox1);
            this.Controls.Add(this.CancelBtn);
            this.Name = "MainWindow";
            this.Text = "Kodu Settings";
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).EndInit();
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.groupBox3.ResumeLayout(false);
            this.groupBox3.PerformLayout();
            this.groupBox4.ResumeLayout(false);
            this.groupBox4.PerformLayout();
            this.groupBox6.ResumeLayout(false);
            this.groupBox6.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button CancelBtn;
        private System.Windows.Forms.PictureBox pictureBox1;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.CheckBox AnimationCk;
        private System.Windows.Forms.CheckBox AntiAliasCk;
        private System.Windows.Forms.CheckBox PostFXCk;
        private System.Windows.Forms.Button Apply;
        private System.Windows.Forms.CheckBox AudioCk;
        private System.Windows.Forms.TextBox StatusTB;
        private System.Windows.Forms.RadioButton ShadMod2RB;
        private System.Windows.Forms.GroupBox groupBox3;
        private System.Windows.Forms.RadioButton ShadMod3RB;
        private System.Windows.Forms.GroupBox groupBox4;
        private System.Windows.Forms.CheckBox communityCk;
        private System.Windows.Forms.GroupBox groupBox6;
        private System.Windows.Forms.TextBox SavePath;
        private System.Windows.Forms.Button UserFolder;
        private System.Windows.Forms.CheckBox VsyncCk;
        private System.Windows.Forms.CheckBox SpriteFontCk;
    }
}

