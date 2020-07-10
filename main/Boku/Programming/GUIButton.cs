// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.


using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Xml;
using System.Xml.Serialization;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Storage;

using Boku.Base;
using Boku.Common;
using Boku.Common.Gesture;
using Boku.Fx;
using Boku.UI;
using Boku.UI2D;
using Boku.Common.Xml;

namespace Boku.Programming
{
    /// <summary>
    /// TODO (****)
    /// This class has some issues.  For instance, it only seems to work
    /// in a WasPressed sense.  Continuous pressing isn't detected.  Also,
    /// the click detection is on release, which is fine for a UI button
    /// but tends to be bad for game controller buttons.
    /// </summary>
    public class GUIButton
    {
        /// <summary>
        /// Enum needed to maintain back compat with older levels and
        /// XML serialization.  Currently we don't even bother with 
        /// this enum because it's overkill and conflates state in
        /// a way that's just not useful.
        /// </summary>
        public enum DisplayType
        {
            DT_Labeled,
            DT_Solid,
            DT_Hidden,
        }

        public static Vector2 DefaultSize = new Vector2(128, 96);

        #region Members

        bool active = false;    // Set by any brain that is using this button.  Controls whether we render button or not.

        ButtonState state = ButtonState.Released;
        ButtonState prevState = ButtonState.Released;

        int fingerID = -1;
        bool mouseControlled = false;
        bool mouseLeftClick = false;

        string label = "";
        bool labelFits = true;  // Does the label fit on the button.  Used by the single line editor to cut things off if they get too long.
        bool dirty = true;      // Has the label changed?  Does the rt need refreshing?

        Vector2 position = new Vector2();
        Color color = Color.HotPink;    // Should get overwritten.

        AABB2D hitBox;

        bool mouseOver = false;

        #endregion

        #region Accessors

        public bool Active
        {
            get { return active; }
            set { active = value; }
        }

        /// <summary>
        /// TODO Figure out what this actually means.
        /// </summary>
        public bool MouseControlled
        {
            get { return mouseControlled; }
            set { mouseControlled = value; }
        }

        /// <summary>
        /// TODO Figure out what this actually means.
        /// </summary>
        public bool MouseLeftClick
        {
            get { return mouseLeftClick; }
            set { mouseLeftClick = value; }
        }

        public int FingerID
        {
            get { return fingerID; }
            set { fingerID = value; }
        }

        /// <summary>
        /// Optional text label on button.
        /// </summary>
        public string Label
        {
            get { return label; }
            set
            {
                // Prefer emptry string to null.
                if (value == null)
                {
                    value = "";
                }
                if (label != value)
                {
                    label = value;
                    Dirty = true;
                }
            }
        }

        /// <summary>
        /// Does the label fit on the button.  Used by the single
        /// line editor to cut things off if they get too long.
        /// </summary>
        public bool LabelFits
        {
            get { return labelFits; }
            set { labelFits = value; }
        }

        /// <summary>
        /// Does the RT image need refreshing?  Probably due to
        /// the label changing.
        /// </summary>
        public bool Dirty
        {
            get { return dirty; }
            set 
            { 
                dirty = value;
                if (dirty)
                {
                    GUIButtonManager.Dirty = true;
                }
            }
        }

        public Vector2 Position
        {
            get { return position; }
            set 
            {
                if (position != value)
                {
                    position = value;
                    hitBox.Set(position, position + DefaultSize);
                }
            }
        }

        public AABB2D HitBox
        {
            get { return hitBox; }
        }

        public ButtonState ButtonState
        {
            get { return state; }
        }
        public bool StateChanged
        {
            get { return state != prevState; }
        }

        public bool Clicked
        {
            get
            {
                return StateChanged && state == ButtonState.Released;
            }
        }

        public Color Color
        {
            get { return color; }
        }

        public bool MouseOver
        {
            get { return mouseOver; }
            set { mouseOver = value; }
        }

        #endregion

        #region Public

        public GUIButton(Color color)
        {
            this.color = color;
            hitBox = new AABB2D();
        }
        
        public void ChangeState(ButtonState newState)
        {
            prevState = state;
            state = newState;
        }

        public void ResetButton()
        {
            state = ButtonState.Released;
            prevState = state;
        }

        #endregion

        #region Internal
        #endregion

    }   // end of class GUIButton

}   // end of namespace Boku.Programming
