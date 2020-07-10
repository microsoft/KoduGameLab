// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

//#define MENU

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace Boku
{
    public partial class MainForm
    {
        /// <summary>
        /// Connectes the menu items to their event handlers.
        /// </summary>
        private void ConnectEventHandlers()
        {
        }   // endof ConnectEventHandlers()

        // 
        // System Events
        //

        private void OnExit(object sender, System.EventArgs e)
        {
            // TODO Anything?
        }

        void clientSizeChanged(object sender, System.EventArgs e)
        {
            xnaControl.Size = this.ClientSize;
            xnaControl.Top = 0;

            //BokuGame.ScreenSize = new Microsoft.Xna.Framework.Vector2(ClientSize.Width, ClientSize.Height);
            /*
            Microsoft.Xna.Framework.Graphics.Viewport vp = new Microsoft.Xna.Framework.Graphics.Viewport();
            vp.Width = (int)BokuGame.ScreenSize.X;
            vp.Height = (int)BokuGame.ScreenSize.Y;
            vp.MaxDepth = 1.0f;
            KoiLibrary.GraphicsDevice.Viewport = vp;
            */
        }

        /*
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MainForm));
            this.SuspendLayout();
            // 
            // MainForm
            // 
            this.ClientSize = new System.Drawing.Size(284, 261);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "MainForm";
            this.ResumeLayout(false);

        }
        */
    }   // end of class MainForm

}   // end of namespace EmptyWin2
