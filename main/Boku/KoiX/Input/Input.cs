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
    /// <summary>
    /// Classes which contains input events, either mouse
    /// or keyboard and include state information about the
    /// system at the time of the input.
    /// </summary>
    public class BaseInput
    {
        double qpcTime; // The time of event.
        bool shift;     // Was shift pressed?
        bool alt;       // Was alt pressed?
        bool ctrl;      // Was ctrl pressed?

        #region Accessors
        // Accessors
        public double QpcTime
        {
            get
            {
                return qpcTime;
            }
            set
            {
                qpcTime = value;
            }
        }
        public bool Shift
        {
            get
            {
                return shift;
            }
            set
            {
                shift = value;
            }
        }
        public bool Alt
        {
            get
            {
                return alt;
            }
            set
            {
                alt = value;
            }
        }
        public bool Ctrl
        {
            get
            {
                return ctrl;
            }
            set
            {
                ctrl = value;
            }
        }
        /// <summary>
        /// Were any of SHift, Alt, or Ctrl pressed?
        /// </summary>
        public bool Modifier
        {
            get { return Shift || Alt || Ctrl; }
        }
        #endregion

        // c'tor
        protected BaseInput( double time )
        {
            QpcTime = time;
            Shift = false;
            Alt = false;
            Ctrl = false;
        }   // end of BaseInput c'tor

    }   // end of class BaseInput

    public class KeyInput : BaseInput
    {
        Keys key = Keys.None;
        char asciiChar = (char)0;

        // c'tor
        public KeyInput( double time, Keys rawKey, char asciiChar )
            :
            base( time )
        {
            this.key = rawKey;
            this.asciiChar = asciiChar;
            this.Alt = LowLevelKeyboardInput.AltPressed;
            this.Ctrl = LowLevelKeyboardInput.CtrlPressed;
            this.Shift = LowLevelKeyboardInput.ShiftPressed;
        }   // end of c'tor

        public KeyInput(double time, char asciiChar)
            :
            base(time)
        {
            this.key = Keys.None;
            this.asciiChar = asciiChar;
            this.Alt = LowLevelKeyboardInput.AltPressed;
            this.Ctrl = LowLevelKeyboardInput.CtrlPressed;
            this.Shift = LowLevelKeyboardInput.ShiftPressed;
        }   // end of c'tor

        #region Accessors
        public Keys Key
        {
            get { return key; }
        }

        public Char AsciiChar
        {
            get { return asciiChar; }
        }
        #endregion

    }   // end of class KeyInput

    public class MouseInput : BaseInput
    {
        // Time over which we look at mouse movement in order to calculate velocity.
        // Note that in reality the time used is betwe 1x and 2x this value.
        float kVelocitySampleFrameTime = 0.05f;

        public enum MouseAction
        {
            Down,
            Up,
            Move,
            Hover,
            Wheel
        };

        Vector3 eye;    // 3D origin of click.
        Vector3 dir;    // Direction of 3D ray from eye through click location.
        MouseEventArgs e;
        MouseAction action;
        Vector2 position;
        Vector2 deltaPosition;
        Vector2 velocity;

        // Internal values used to calc velocity.
        static Vector2 prevPos;
        static Double prevTime = 0;
        static Vector2 prevPrevPos;
        static Double prevPrevTime = 0;
        static Double nextTime = 0;

        // This is the object that was clicked on.  Should only be activated if the user
        // releases the button still over this object.  Note that on the frame after a
        // release, this is cleared.
        static object clickedOnObject = null;

        // TODO (****) Remove this when no longer needed.
        // This is a temporary duplicate of teh above for use by old UI elements.
        // In the new system, clickOnObject gets cleared automatically.
        // In the old system, whatever object takes possession mustdo the clearing.
        // So far, this is needed by toolBar, undo/redo buttons, ...
        static public object OldClickedOnObject = null;
        // Another hack to be removed.  This one just duplicates clickOnObject but
        // lags by a frame.  This allows the value to persist long enough to be
        // detected by old UI code.
        static public object FrameDelayedClickedOnObject = null;   

        #region Accessors
        
        public Vector3 Eye
        {
            get{ return eye; }
            set{ eye = value; }
        }
        
        public Vector3 Dir
        {
            get{ return dir; }
            set{ dir = value; }
        }
        
        public MouseEventArgs E
        {
            get{ return e; }
            set{ e = value; }
        }
        
        public MouseAction Action
        {
            get{ return action; }
            set{ action = value; }
        }

        /// <summary>
        /// Mouse position in pixels as a Vector2.  This is in
        /// window coordinates with 0,0 in the upper left hand
        /// corner.
        /// </summary>
        public Vector2 Position
        {
            get { return position; }
        }

        /// <summary>
        /// Change in position from last frame.
        /// </summary>
        public Vector2 DeltaPosition
        {
            get { return deltaPosition; }
        }

        /// <summary>
        /// Mouse velocity, filtered over multiple frames.
        /// </summary>
        public Vector2 Velocity
        {
            get { return velocity; }
        }

        public static bool WasTouched
        {
            get { return LowLevelMouseInput.WasTouched; }
        }

        // This is the object that was clicked on.  Should only be activated if the user
        // releases the button still over this object.  Note that on the frame after a
        // release, this is cleared.
        public static object ClickedOnObject
        {
            get { return clickedOnObject; }
            set 
            {
                FrameDelayedClickedOnObject = clickedOnObject;
                clickedOnObject = value; 
            }
        }

        #endregion

        // c'tor
        public MouseInput( double time, MouseAction action, MouseEventArgs e )
            :
            base( time )
        {
            eye = new Vector3();
            this.e = e;
            this.action = action;

            position = new Vector2(e.Position.X, e.Position.Y);
            deltaPosition = new Vector2(LowLevelMouseInput.DeltaPosition.X, LowLevelMouseInput.DeltaPosition.Y);

            // If first time, init values.
            if (nextTime == 0)
            {
                prevPrevTime = prevTime = time;
                nextTime = time + kVelocitySampleFrameTime;

                prevPrevPos = prevPos = position;
            }
            // Calc velocity.
            {
                float dt = (float)(time - prevPrevTime);
                Vector2 dPos = position - prevPrevPos;
                if (dt != 0)
                {
                    velocity = dPos / dt;
                }
                else
                {
                    velocity = Vector2.Zero;
                }

                // Need to snap another frame?
                if (time > nextTime)
                {
                    // Shuffle
                    prevPrevTime = prevTime;
                    prevPrevPos = prevPos;

                    prevTime = time;
                    prevPos = position;

                    nextTime = time + kVelocitySampleFrameTime;
                }
            }

            Alt = LowLevelKeyboardInput.AltPressed;
            Ctrl = LowLevelKeyboardInput.CtrlPressed;
            Shift = LowLevelKeyboardInput.ShiftPressed;
            Dir = new Vector3(LowLevelMouseInput.DeltaPosition.X, LowLevelMouseInput.DeltaPosition.Y, 0);

        }   // end of MouseInput c'tor

    }   // end of class MouseInput

}   // end of namespace KoiX.Input
