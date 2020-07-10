// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

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
using Boku.Input;
using Boku.Common.Gesture;

namespace Boku.Programming
{
    public enum TouchGestureFilterType
    {
        Touching,
        Tap,
        Rotate,
        Swipe,
        Slide            // Implies inputs are used in screen space rather than world space.
    }

    /// <summary>
    /// Hybrid filter that provides the source of mouse button input
    /// 
    /// 
    /// </summary>
    public class TouchGestureFilter : Filter
    {
        [XmlAttribute]
        public TouchGestureFilterType type;

        /// <summary>
        /// Angle of rotation performed by a rotate gesture
        /// </summary>
        private float deltaRotation = 0.0f;

        /// <summary>
        /// Angle of rotation performed by a rotate gesture
        /// </summary>
        [XmlIgnore]
        public float DeltaRotation
        {
            get { return deltaRotation; }
        }

        /// <summary>
        /// Last captured tap position by this reflex
        /// </summary>
        private Vector2 tapPosition = Vector2.Zero;
        
        [XmlIgnore]
        public Vector2 TapPosition
        {
            get { return tapPosition; }
        }

        /// <summary>
        /// Swipe direction from the swipe gesture
        /// </summary>
        private Directions direction = Directions.None;

        [XmlIgnore]
        public Directions Direction
        {
            get { return direction; }
        }

        [XmlIgnore]
        public bool IsPerformed
        {
            get
            {
                switch (type)
                {
                    case TouchGestureFilterType.Tap:
                        return TouchGestureManager.Get().TapGesture.WasTapped();

                    case TouchGestureFilterType.Rotate:
                        return TouchGestureManager.Get().RotateGesture.IsRotating;

                    case TouchGestureFilterType.Slide:
                        return TouchGestureManager.Get().DragGesture.IsDragging;

                    case TouchGestureFilterType.Swipe:
                        return TouchGestureManager.Get().SwipeGesture.WasSwiped();

                    case TouchGestureFilterType.Touching:
                        return TouchInput.IsTouched;

                    default:
                        throw new Exception("Unknown Touch filter");
                }
            }
        }

        private bool wasPerformed;

        [XmlIgnore]
        public bool WasPerformed
        {
            get
            {
                switch (type)
                {
                    case TouchGestureFilterType.Tap:
                        return TouchGestureManager.Get().TapGesture.WasActivated;

                    case TouchGestureFilterType.Rotate:
                        return TouchGestureManager.Get().RotateGesture.WasActivated;

                    case TouchGestureFilterType.Slide:
                        return TouchGestureManager.Get().DragGesture.WasActivated;

                    case TouchGestureFilterType.Swipe:
                        return TouchGestureManager.Get().SwipeGesture.WasActivated;

                    case TouchGestureFilterType.Touching:
                        return TouchInput.WasTouched;

                    default:
                        throw new Exception("Unknown Touch filter");
                }
            }
        }

        public TouchGestureFilter()
        {
        }

        public override ProgrammingElement Clone()
        {
            TouchGestureFilter clone = new TouchGestureFilter();
            CopyTo(clone);
            return clone;
        }

        protected void CopyTo(TouchGestureFilter clone)
        {
            base.CopyTo(clone);
            clone.type = this.type;
        }

        public override bool MatchTarget(Reflex reflex, SensorTarget sensorTarget)
        {
            return true;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="reflex"></param>
        /// <param name="param"></param>
        /// <returns>Return true if the TouchGestureFilter passes.  Ignoring the other filters.</returns>
        public override bool MatchAction(Reflex reflex, out object param)
        {
            // Based on gesture type, we check if the gesture has been recognized/performed this frame to see
            // if this filter may have something to do!
            bool result = IsPerformed;

            // ** UNUSED **
            // Return as a parameter a vector that can be used for input to the movement system, so
            // that players can drive and turn bots using mouse buttons.
            // The touch filters don't have a use for this parameter at this time.
            param = null;

            // Did the gesture just begin this frame? Useful for 'once' filters and such.
            wasPerformed = WasPerformed;

            if (result)
            {
                // If we have a Once modifier, clear it on wasPerformed
                if (wasPerformed)
                {
                    for (int i = 0; i < reflex.Modifiers.Count; i++)
                    {
                        OnceModifier om = reflex.Modifiers[i] as OnceModifier;
                        if (om != null)
                        {
                            om.Reset(reflex);
                        }
                    }
                }

                // Set the touch position and touch actor based on TouchEdit data and 
                // whether we've hit an actor or not.
                bool meFilter = reflex.Data.FilterExists("filter.me");
                if (TouchEdit.HitInfo.ActorHit != null)
                {
                    reflex.TouchPosition = TouchEdit.HitInfo.ActorPosition;
                    reflex.TouchActor = TouchEdit.HitInfo.ActorHit;
                }
                else
                {
                    reflex.TouchActor = null;

                    if (!meFilter)
                    {
                        reflex.TouchPosition = TouchEdit.HitInfo.TerrainPosition;
                    }
                    else
                    {
                        reflex.TouchPosition = null;
                    }
                }

                // Gesture-type specific behavior goes here.
                if (type == TouchGestureFilterType.Rotate)
                {
                    deltaRotation = TouchGestureManager.Get().RotateGesture.RotationDelta;
                }
                else if (type == TouchGestureFilterType.Swipe)
                {
                    direction = TouchGestureManager.Get().SwipeGesture.SwipeDirection;
                }
                else if (type == TouchGestureFilterType.Slide)
                {
                    // Slide is unique in that it cares about what actor you began the slide on, not where
                    // you are now. The touch position is your current touch position, however.
                    // Here we override the TouchActor set above.
                    reflex.TouchPosition = TouchEdit.HitInfo.TerrainPosition;
                    reflex.TouchActor = TouchInput.InitialActorHit;
                }
                else if (type == TouchGestureFilterType.Tap)
                {
                    tapPosition = TouchGestureManager.Get().TapGesture.Position;
                }
                else if (type == TouchGestureFilterType.Touching)
                {
                }
            }

            // Don't hold onto a ref to a dead bot.
            if (reflex.TouchActor != null && reflex.TouchActor.CurrentState == GameThing.State.Inactive)
            {
                reflex.TouchActor = null;
                reflex.TouchPosition = null;
            }

            return result;
        }

        public override void Reset(Reflex reflex)
        {
            base.Reset(reflex);
        }

    }   // end of class MouseButtonFilter

}   // end of namespace Boku.Programming
