// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.


#region Using Statements

using System;
using System.Collections;
using System.Collections.Generic;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Storage;
using Microsoft.Xna.Framework.Content;

using System.Diagnostics;

#endregion

namespace KoiX.Input
{
    [Flags]
    public enum MouseButtons
    {
        // No mouse button was pressed.
        None = 0,
    
        // The left mouse button was pressed.
        Left = 0x00100000,
        
        // The right mouse button was pressed.
        Right = 0x00200000,
        
        // The middle mouse button was pressed.
        Middle = 0x00400000,
        
        // The first XButton was pressed.
        XButton1 = 0x00800000,
        
        // The second XButton was pressed.
        XButton2 = 0x01000000,

    }   // end of enum MouseButtons

    /// <summary>
    /// Clone of Windows.Forms MouseEventArgs class since we'd not doing
    /// a WinForms based app but we still want to used mouse input.
    /// </summary>
    public class MouseEventArgs : EventArgs
    {
        #region Members

        MouseButtons button;    // Which button was pressed.
        int x;                  // Location of mouse in pixels.
        int y;
        int clicks;             // The number of time the mouse button was pressed.
        int delta;              // A signed count of the number of detents the wheel has rotated.

        #endregion

        #region Accessors

        /// <summary>
        /// Which button was pressed.
        /// </summary>
        public MouseButtons Button
        {
            get { return button; }
        }

        /// <summary>
        /// Number of times the button was clicked.
        /// </summary>
        public int Clicks
        {
            get { return clicks; }
        }

        /// <summary>
        /// A signed count of the number of detents the wheel has rotated.
        /// </summary>
        public int Delta
        {
            get { return delta; }
        }

        /// <summary>
        /// X position of mouse click in pixels.
        /// </summary>
        public int X
        {
            get { return x; }
        }

        /// <summary>
        /// Y position of mouse click in pixels.
        /// </summary>
        public int Y
        {
            get { return y; }
        }

        /// <summary>
        /// Position of mouse click in pixels.
        /// </summary>
        public Point Position
        {
            get { return new Point(x, y); }
        }

        #endregion

        #region Public

        // c'tor
        public MouseEventArgs( MouseButtons button, int clicks, int x, int y, int delta )
        {
            this.button = button;
            this.clicks = clicks;
            this.x = x;
            this.y = y;
            this.delta = delta;
        }   // end of c'tor

        #endregion

    }   // end of class MouseEventArgs

}   // end of namespace KoiX.Input
