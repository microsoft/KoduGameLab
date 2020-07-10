// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.


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

namespace KoiX.Input
{
    public enum GestureType
    {
        None,
        Tap,
        DoubleTap,
        Hold,
        OnePointDragBegin,
        OnePointDrag,
        OnePointDragEnd,
        TwoPointDragBegin,
        TwoPointDrag,
        TwoPointDragEnd,
    }

    public class BaseGestureEventArgs
    {
        #region Members

        GestureType gesture = GestureType.None;
        protected Gestures.Touch touch;

        #endregion

        #region Accessors

        /// <summary>
        /// Which gesture occurred.
        /// </summary>
        public GestureType Gesture
        {
            get { return gesture; }
        }

        /// <summary>
        /// Should be the object hit by this gesture.
        /// </summary>
        public InputEventHandler HitObject
        {
            get { return touch.Sample.HitObject; }
        }

        /// <summary>
        /// Id of TouchSample for this gesture.
        /// </summary>
        public int Id
        {
            get { return touch.Id; }
        }

        /// <summary>
        /// Time when first touch for this gesture occurred.
        /// </summary>
        public double StartTime
        {
            get { return touch.StartTime; }
        }

        #endregion

        #region Public

        public BaseGestureEventArgs(GestureType gesture, Gestures.Touch touch)
        {
            this.gesture = gesture;
            this.touch = touch;
        }

        #endregion

        #region Internal
        #endregion

    }   // end of class BaseGestureEventArgs

    /// <summary>
    /// Event args used for Tap, DoubleTap and Hold events.
    /// </summary>
    public class TapGestureEventArgs : BaseGestureEventArgs
    {
        #region Members
        #endregion

        #region Accessors

        /// <summary>
        /// Position for gesture.
        /// TODO Should be be 1st or last position?  Does it matter?
        /// </summary>
        public Vector2 Position
        {
            get { return touch.Position; }
        }

        #endregion

        #region Public

        public TapGestureEventArgs(GestureType gesture, Gestures.Touch touch)
            : base(gesture, touch)
        {
        }

        #endregion

        #region Internal
        #endregion

    }   // end of class TapGestureEventArgs

    /// <summary>
    /// Event args used for OnePointDrag events.
    /// </summary>
    public class OnePointDragGestureEventArgs : BaseGestureEventArgs
    {
        #region Members
        #endregion

        #region Accessors

        /// <summary>
        /// Position at start of gesture.
        /// </summary>
        public Vector2 StartPosition
        {
            get { return touch.StartPosition; }
        }

        /// <summary>
        /// Position this frame.
        /// </summary>
        public Vector2 CurrentPosition
        {
            get { return touch.Position; }
        }

        /// <summary>
        /// Change in position since last frame.
        /// </summary>
        public Vector2 DeltaPosition
        {
            get { return touch.DeltaPosition; }
        }

        #endregion

        #region Public

        public OnePointDragGestureEventArgs(GestureType gesture, Gestures.Touch touch)
            : base(gesture, touch)
        {
        }

        #endregion

        #region Internal
        #endregion

    }   // end of class OnePointDragGestureEventArgs

    /// <summary>
    /// Event args used for TwoPointDrag events.
    /// </summary>
    public class TwoPointDragGestureEventArgs : BaseGestureEventArgs
    {
        #region Members

        Gestures.Touch touch1;      // Second touch.

        Vector2 startCenter;        // Value for centroid of touch points.
        Vector2 currentCenter;
        Vector2 deltaCenter;

        float startDistance;        // Distance between points.
        float currentDistance;
        float deltaDistance;

        float startAngle;           // Angle along line from point0 to point1.
        float currentAngle;
        float deltaAngle;

        #endregion

        #region Accessors

        /// <summary>
        /// Position at start of gesture for Touch0.
        /// </summary>
        public Vector2 StartPosition0
        {
            get { return touch.StartPosition; }
        }

        /// <summary>
        /// Position this frame for Touch0.
        /// </summary>
        public Vector2 CurrentPosition0
        {
            get { return touch.Position; }
        }

        /// <summary>
        /// Change in position since last frame for Touch0.
        /// </summary>
        public Vector2 DeltaPosition0
        {
            get { return touch.DeltaPosition; }
        }

        /// <summary>
        /// Position at start of gesture for Touch1.
        /// </summary>
        public Vector2 StartPosition1
        {
            get { return touch1.StartPosition; }
        }

        /// <summary>
        /// Position this frame for Touch1.
        /// </summary>
        public Vector2 CurrentPosition1
        {
            get { return touch1.Position; }
        }

        /// <summary>
        /// Change in position since last frame for Touch1.
        /// </summary>
        public Vector2 DeltaPosition1
        {
            get { return touch1.DeltaPosition; }
        }

        /// <summary>
        /// Distance between touch locations at start of gesture.
        /// </summary>
        public float StartDistance
        {
            get { return startDistance; }
        }
        
        /// <summary>
        /// Currnet distance between touch locations.
        /// </summary>
        public float CurrentDistance
        {
            get { return currentDistance; }
        }

        /// <summary>
        /// Change in distance between touch locations for this frame.
        /// </summary>
        public float DeltaDistance
        {
            get { return deltaDistance; }
        }

        /// <summary>
        /// Angle between touch locations at start of gesture.
        /// </summary>
        public float StartAngle
        {
            get { return startAngle; }
        }

        /// <summary>
        /// Currnet angle between touch locations.
        /// </summary>
        public float CurrentAngle
        {
            get { return currentAngle; }
        }

        /// <summary>
        /// Change in angle between touch locations for this frame.
        /// </summary>
        public float DeltaAngle
        {
            get { return deltaAngle; }
        }

        /// <summary>
        /// Initial centroid of touch contacts when first detected.
        /// </summary>
        public Vector2 StartCenter
        {
            get { return startCenter; }
        }

        /// <summary>
        /// Current centroid of touch contacts.
        /// </summary>
        public Vector2 CurrentCenter
        {
            get { return currentCenter; }
        }

        /// <summary>
        /// Change in centroid since beginning of gesture.
        /// </summary>
        public Vector2 DeltaCenter
        {
            get { return deltaCenter; }
        }

        #endregion

        #region Public

        public TwoPointDragGestureEventArgs(GestureType gesture, Gestures.Touch touch0, Gestures.Touch touch1,
            float startDistance, float currentDistance, float deltaDistance,
            float startAngle, float currentAngle, float deltaAngle)
            : base(gesture, touch0)
        {
            this.touch1 = touch1;
            this.startDistance = startDistance;
            this.currentDistance = currentDistance;
            this.deltaDistance = deltaDistance;
            this.startAngle = startAngle;
            this.currentAngle = currentAngle;
            this.deltaAngle = deltaAngle;

            this.startCenter = (touch0.StartPosition + touch1.StartPosition) * 0.5f;
            this.currentCenter = (touch0.Position + touch1.Position) * 0.5f;
            this.deltaCenter = (touch0.DeltaPosition + touch1.DeltaPosition) * 0.5f;
        }

        #endregion

        #region Internal
        #endregion

    }   // end of class TowPointDragGestureEventArgs

}   // end of namespace KoiX.Input
