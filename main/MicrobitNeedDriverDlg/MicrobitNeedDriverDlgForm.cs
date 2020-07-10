// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace MicrobitNeedDriverDlg
{
    public partial class MicrobitNeedDriverDlgForm : Form
    {
        public MicrobitNeedDriverDlgForm()
        {
            InitializeComponent();
        }

        private void MicrobitNeedDriverDlgForm_Load(object sender, EventArgs e)
        {
            try
            {
                LinkLabel.Link link = new LinkLabel.Link();
                linkLabel.Links.Add(0, linkLabel.Text.Length, link);
            }
            catch
            { }
        }

        private void linkLabel1_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            try
            {
                string url = @"http://www.kodugamelab.com/bbc-microbit/";
                System.Diagnostics.Process.Start(url);
            }
            catch
            { }
        }

        public DialogResult ShowModal(string title, string message, string linkLabel, string cancelLabel, string installLabel, Form parent)
        {
            this.Text = title;
            this.message.Text = message;
            this.linkLabel.Text = linkLabel;
            this.cancelBtn.Text = cancelLabel;
            this.installBtn.Text = installLabel;
            return this.ShowDialog(parent);
        }
    }
}
