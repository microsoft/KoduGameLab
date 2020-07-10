// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Windows.Forms;


namespace Surfacer
{
    partial class CharacterEdit
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        private ComboBox SurfSetMenu;
        private Label label1;
        private ComboBox slot1Pick;
        private ComboBox slot2Pick;
        private Label label2;
        private ComboBox slot3Pick;
        private Label label3;
        private ComboBox slot4Pick;
        private Label label4;
        private ComboBox slot5Pick;
        private Label label5;
        private ComboBox slot6Pick;
        private Label label6;
        private ComboBox slot7Pick;
        private Label label7;
        private ComboBox slot8Pick;
        private Label label8;

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
            this.SurfSetMenu = new System.Windows.Forms.ComboBox();
            this.label1 = new System.Windows.Forms.Label();
            this.slot1Pick = new System.Windows.Forms.ComboBox();
            this.slot2Pick = new System.Windows.Forms.ComboBox();
            this.label2 = new System.Windows.Forms.Label();
            this.slot3Pick = new System.Windows.Forms.ComboBox();
            this.label3 = new System.Windows.Forms.Label();
            this.slot4Pick = new System.Windows.Forms.ComboBox();
            this.label4 = new System.Windows.Forms.Label();
            this.slot5Pick = new System.Windows.Forms.ComboBox();
            this.label5 = new System.Windows.Forms.Label();
            this.slot6Pick = new System.Windows.Forms.ComboBox();
            this.label6 = new System.Windows.Forms.Label();
            this.slot7Pick = new System.Windows.Forms.ComboBox();
            this.label7 = new System.Windows.Forms.Label();
            this.slot8Pick = new System.Windows.Forms.ComboBox();
            this.label8 = new System.Windows.Forms.Label();
            this.SaveBtn = new System.Windows.Forms.Button();
            this.SaveAsBtn = new System.Windows.Forms.Button();
            this.RevertBtn = new System.Windows.Forms.Button();
            this.DirtPickBtn = new System.Windows.Forms.Button();
            this.DirtFld = new System.Windows.Forms.TextBox();
            this.label9 = new System.Windows.Forms.Label();
            this.BumpFld = new System.Windows.Forms.TextBox();
            this.BumpPickBtn = new System.Windows.Forms.Button();
            this.label10 = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // SurfSetMenu
            // 
            this.SurfSetMenu.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.SurfSetMenu.FormattingEnabled = true;
            this.SurfSetMenu.Location = new System.Drawing.Point(12, 12);
            this.SurfSetMenu.Name = "SurfSetMenu";
            this.SurfSetMenu.Size = new System.Drawing.Size(260, 21);
            this.SurfSetMenu.TabIndex = 0;
            this.SurfSetMenu.SelectedIndexChanged += new System.EventHandler(this.SurfSetMenu_SelectedIndexChanged);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(12, 60);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(37, 13);
            this.label1.TabIndex = 1;
            this.label1.Text = "Slot 1:";
            // 
            // slot1Pick
            // 
            this.slot1Pick.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.slot1Pick.FormattingEnabled = true;
            this.slot1Pick.Location = new System.Drawing.Point(55, 57);
            this.slot1Pick.Name = "slot1Pick";
            this.slot1Pick.Size = new System.Drawing.Size(217, 21);
            this.slot1Pick.TabIndex = 2;
            this.slot1Pick.SelectedIndexChanged += new System.EventHandler(this.slot1Pick_SelectedIndexChanged);
            // 
            // slot2Pick
            // 
            this.slot2Pick.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.slot2Pick.FormattingEnabled = true;
            this.slot2Pick.Location = new System.Drawing.Point(55, 84);
            this.slot2Pick.Name = "slot2Pick";
            this.slot2Pick.Size = new System.Drawing.Size(217, 21);
            this.slot2Pick.TabIndex = 4;
            this.slot2Pick.SelectedIndexChanged += new System.EventHandler(this.slot2Pick_SelectedIndexChanged);
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(12, 87);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(37, 13);
            this.label2.TabIndex = 3;
            this.label2.Text = "Slot 2:";
            // 
            // slot3Pick
            // 
            this.slot3Pick.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.slot3Pick.FormattingEnabled = true;
            this.slot3Pick.Location = new System.Drawing.Point(55, 111);
            this.slot3Pick.Name = "slot3Pick";
            this.slot3Pick.Size = new System.Drawing.Size(217, 21);
            this.slot3Pick.TabIndex = 6;
            this.slot3Pick.SelectedIndexChanged += new System.EventHandler(this.slot3Pick_SelectedIndexChanged);
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(12, 114);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(37, 13);
            this.label3.TabIndex = 5;
            this.label3.Text = "Slot 3:";
            // 
            // slot4Pick
            // 
            this.slot4Pick.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.slot4Pick.FormattingEnabled = true;
            this.slot4Pick.Location = new System.Drawing.Point(55, 138);
            this.slot4Pick.Name = "slot4Pick";
            this.slot4Pick.Size = new System.Drawing.Size(217, 21);
            this.slot4Pick.TabIndex = 8;
            this.slot4Pick.SelectedIndexChanged += new System.EventHandler(this.slot4Pick_SelectedIndexChanged);
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(12, 141);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(37, 13);
            this.label4.TabIndex = 7;
            this.label4.Text = "Slot 4:";
            // 
            // slot5Pick
            // 
            this.slot5Pick.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.slot5Pick.FormattingEnabled = true;
            this.slot5Pick.Location = new System.Drawing.Point(55, 165);
            this.slot5Pick.Name = "slot5Pick";
            this.slot5Pick.Size = new System.Drawing.Size(217, 21);
            this.slot5Pick.TabIndex = 10;
            this.slot5Pick.SelectedIndexChanged += new System.EventHandler(this.slot5Pick_SelectedIndexChanged);
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(12, 168);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(37, 13);
            this.label5.TabIndex = 9;
            this.label5.Text = "Slot 5:";
            // 
            // slot6Pick
            // 
            this.slot6Pick.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.slot6Pick.FormattingEnabled = true;
            this.slot6Pick.Location = new System.Drawing.Point(55, 192);
            this.slot6Pick.Name = "slot6Pick";
            this.slot6Pick.Size = new System.Drawing.Size(217, 21);
            this.slot6Pick.TabIndex = 12;
            this.slot6Pick.SelectedIndexChanged += new System.EventHandler(this.slot6Pick_SelectedIndexChanged);
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Location = new System.Drawing.Point(12, 195);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(37, 13);
            this.label6.TabIndex = 11;
            this.label6.Text = "Slot 6:";
            // 
            // slot7Pick
            // 
            this.slot7Pick.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.slot7Pick.FormattingEnabled = true;
            this.slot7Pick.Location = new System.Drawing.Point(55, 219);
            this.slot7Pick.Name = "slot7Pick";
            this.slot7Pick.Size = new System.Drawing.Size(217, 21);
            this.slot7Pick.TabIndex = 14;
            this.slot7Pick.SelectedIndexChanged += new System.EventHandler(this.slot7Pick_SelectedIndexChanged);
            // 
            // label7
            // 
            this.label7.AutoSize = true;
            this.label7.Location = new System.Drawing.Point(12, 222);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(37, 13);
            this.label7.TabIndex = 13;
            this.label7.Text = "Slot 7:";
            // 
            // slot8Pick
            // 
            this.slot8Pick.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.slot8Pick.FormattingEnabled = true;
            this.slot8Pick.Location = new System.Drawing.Point(55, 246);
            this.slot8Pick.Name = "slot8Pick";
            this.slot8Pick.Size = new System.Drawing.Size(217, 21);
            this.slot8Pick.TabIndex = 16;
            this.slot8Pick.SelectedIndexChanged += new System.EventHandler(this.slot8Pick_SelectedIndexChanged);
            // 
            // label8
            // 
            this.label8.AutoSize = true;
            this.label8.Location = new System.Drawing.Point(12, 249);
            this.label8.Name = "label8";
            this.label8.Size = new System.Drawing.Size(37, 13);
            this.label8.TabIndex = 15;
            this.label8.Text = "Slot 8:";
            // 
            // SaveBtn
            // 
            this.SaveBtn.Location = new System.Drawing.Point(15, 342);
            this.SaveBtn.Name = "SaveBtn";
            this.SaveBtn.Size = new System.Drawing.Size(75, 23);
            this.SaveBtn.TabIndex = 17;
            this.SaveBtn.Text = "Save";
            this.SaveBtn.UseVisualStyleBackColor = true;
            this.SaveBtn.Click += new System.EventHandler(this.SaveBtn_Click);
            // 
            // SaveAsBtn
            // 
            this.SaveAsBtn.Location = new System.Drawing.Point(96, 342);
            this.SaveAsBtn.Name = "SaveAsBtn";
            this.SaveAsBtn.Size = new System.Drawing.Size(75, 23);
            this.SaveAsBtn.TabIndex = 18;
            this.SaveAsBtn.Text = "Save As...";
            this.SaveAsBtn.UseVisualStyleBackColor = true;
            this.SaveAsBtn.Click += new System.EventHandler(this.SaveAsBtn_Click);
            // 
            // RevertBtn
            // 
            this.RevertBtn.Location = new System.Drawing.Point(177, 342);
            this.RevertBtn.Name = "RevertBtn";
            this.RevertBtn.Size = new System.Drawing.Size(75, 23);
            this.RevertBtn.TabIndex = 19;
            this.RevertBtn.Text = "Revert";
            this.RevertBtn.UseVisualStyleBackColor = true;
            this.RevertBtn.Click += new System.EventHandler(this.RevertBtn_Click);
            // 
            // DirtPickBtn
            // 
            this.DirtPickBtn.Location = new System.Drawing.Point(319, 297);
            this.DirtPickBtn.Name = "DirtPickBtn";
            this.DirtPickBtn.Size = new System.Drawing.Size(28, 25);
            this.DirtPickBtn.TabIndex = 25;
            this.DirtPickBtn.Text = "...";
            this.DirtPickBtn.UseVisualStyleBackColor = true;
            this.DirtPickBtn.Click += new System.EventHandler(this.DirtPickBtn_Click);
            // 
            // DirtFld
            // 
            this.DirtFld.Location = new System.Drawing.Point(55, 300);
            this.DirtFld.Name = "DirtFld";
            this.DirtFld.Size = new System.Drawing.Size(258, 20);
            this.DirtFld.TabIndex = 24;
            this.DirtFld.TextChanged += new System.EventHandler(this.DirtFld_TextChanged);
            // 
            // label9
            // 
            this.label9.AutoSize = true;
            this.label9.Location = new System.Drawing.Point(23, 303);
            this.label9.Name = "label9";
            this.label9.Size = new System.Drawing.Size(26, 13);
            this.label9.TabIndex = 23;
            this.label9.Text = "Dirt:";
            // 
            // BumpFld
            // 
            this.BumpFld.Location = new System.Drawing.Point(55, 273);
            this.BumpFld.Name = "BumpFld";
            this.BumpFld.Size = new System.Drawing.Size(258, 20);
            this.BumpFld.TabIndex = 22;
            this.BumpFld.TextChanged += new System.EventHandler(this.BumpFld_TextChanged);
            // 
            // BumpPickBtn
            // 
            this.BumpPickBtn.Location = new System.Drawing.Point(319, 271);
            this.BumpPickBtn.Name = "BumpPickBtn";
            this.BumpPickBtn.Size = new System.Drawing.Size(27, 25);
            this.BumpPickBtn.TabIndex = 21;
            this.BumpPickBtn.Text = "...";
            this.BumpPickBtn.UseVisualStyleBackColor = true;
            this.BumpPickBtn.Click += new System.EventHandler(this.BumpPickBtn_Click);
            // 
            // label10
            // 
            this.label10.AutoSize = true;
            this.label10.Location = new System.Drawing.Point(12, 276);
            this.label10.Name = "label10";
            this.label10.Size = new System.Drawing.Size(37, 13);
            this.label10.TabIndex = 20;
            this.label10.Text = "Bump:";
            // 
            // CharacterEdit
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(358, 379);
            this.Controls.Add(this.DirtPickBtn);
            this.Controls.Add(this.DirtFld);
            this.Controls.Add(this.label9);
            this.Controls.Add(this.BumpFld);
            this.Controls.Add(this.BumpPickBtn);
            this.Controls.Add(this.label10);
            this.Controls.Add(this.RevertBtn);
            this.Controls.Add(this.SaveAsBtn);
            this.Controls.Add(this.SaveBtn);
            this.Controls.Add(this.slot8Pick);
            this.Controls.Add(this.label8);
            this.Controls.Add(this.slot7Pick);
            this.Controls.Add(this.label7);
            this.Controls.Add(this.slot6Pick);
            this.Controls.Add(this.label6);
            this.Controls.Add(this.slot5Pick);
            this.Controls.Add(this.label5);
            this.Controls.Add(this.slot4Pick);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.slot3Pick);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.slot2Pick);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.slot1Pick);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.SurfSetMenu);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.Fixed3D;
            this.MaximizeBox = false;
            this.Name = "CharacterEdit";
            this.SizeGripStyle = System.Windows.Forms.SizeGripStyle.Hide;
            this.Text = "CharacterEdit";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private Button SaveBtn;
        private Button SaveAsBtn;
        private Button RevertBtn;
        private Button DirtPickBtn;
        private TextBox DirtFld;
        private Label label9;
        private TextBox BumpFld;
        private Button BumpPickBtn;
        private Label label10;
    }
}
