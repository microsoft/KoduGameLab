// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.


using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input.Touch;

namespace KoiX.Input
{
    /// <summary>
    /// Class to replace XNA's TouchLocation since that doesn't have everything we need.
    /// </summary>
    public class TouchSample
    {
        #region members

        int id;
        TouchLocationState state;
        Vector2 position;
        InputEventHandler hitObject;

        #endregion

        #region Accessors

        /// <summary>
        /// Id number for touch.  Allows tracking of a single touch 
        /// over Pressed, Moved, and Released.
        /// </summary>
        public int Id
        {
            get { return id; }
        }

        /// <summary>
        /// State of touch for this sample.
        /// </summary>
        public TouchLocationState State
        {
            get { return state; }
        }

        /// <summary>
        /// Screen coord of touch.
        /// </summary>
        public Vector2 Position
        {
            get { return position; }
        }

        /// <summary>
        /// Object under touch when this id track started.
        /// Note this is NOT set by the touch input code.  Rather it
        /// needs to be set by whatever code is looking for hits (SceneManager).
        /// </summary>
        public InputEventHandler HitObject
        {
            get { return hitObject; }
            set { hitObject = value; }
        }

        #endregion

        #region Public

        public TouchSample(int id, TouchLocationState state, Vector2 position)
        {
            this.id = id;
            this.state = state;
            this.position = position;
        }   // end of c'tor

        public TouchSample(TouchLocation touchLocation)
        {
            this.id = touchLocation.Id;
            this.position = touchLocation.Position;
            this.state = touchLocation.State;
        }   // end of c'tor

        #endregion

        #region Internal
        #endregion

    }   // end of class TouchSample

}   // end of namespace KoiX.Input
