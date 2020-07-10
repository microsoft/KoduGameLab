// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using System.Diagnostics;

namespace Boku.Common.Gesture
{
    public class TapGestureRecognizer : GestureRecognizer
    {
        /// <summary>
        /// The maximum lifetime (in seconds) that a touch can have before it is disqualified from being considered
        /// a tap.
        /// </summary>
        const float k_MaxTouchLifetime = 0.6f;

        /// <summary>
        /// The maximum radius that the touch can drift from its start position before it is no longer considered
        /// a tap gesture.
        /// </summary>
        const float k_MaxTouchRadius = 20.0f;


        const float k_ValidTimeForEditTap = 0.25f;

        /// <summary>
        /// When a touch sequence starts, we store the finger id in order to know which finger is tapping
        /// </summary>
        int m_fingerId = -1;

        Vector2 m_position = Vector2.Zero;
        public Vector2 Position
        {
            get { return m_position; }
        }

        public bool WasValidEditObjectTap
        {
            get
            {
                //if we're tapping an object, it's always valid
                //if we're not tapping an object, don't count it until a certain amount of time as passed, allow for rotate/pinch
                //gestures to override the tap
                if ((TouchInput.InitialActorHit == null && WasRecentlyRecognized && TimeSinceRecognized > k_ValidTimeForEditTap)
                    || (TouchInput.InitialActorHit != null && WasRecognized))
                {
                    return true;
                }

                return false;
            }
        }

        public TapGestureRecognizer()
        {
            //ResetMode = GestureResetMode.NextFrame;
        }

        public bool WasTapped()
        {
            return wasRecognized;
        }

        /// <summary>
        /// Clears the recognized state.  This is useful if we're actually
        /// looking at the raw touch inputs and don't want the gestures
        /// to trigger anything.
        /// </summary>
        public void ClearWasTapped()
        {
            wasRecognized = false;
        }

        protected override int GetRequiredTouchCount()
        {
            return 1;
        }

        protected override void OnReset()
        {
            m_fingerId = -1;
        }

        protected override void OnTouchReleased(TouchContact[] touches)
        {
            bool bFailed = touches.Length != GetRequiredTouchCount();

            //Ensure touch contact is still valid.
            bFailed |= !IsTouchContactValidForTap(TouchInput.GetTouchContactByFingerId(m_fingerId, touches));

            if (bFailed)
            {
                SetState(GestureState.Failed);
            }
            else
            {
                m_position = touches[0].position;
                SetState(GestureState.Recognized);
            }
        }

        protected override void OnTouchMoved(TouchContact[] touches)
        {
            if (touches.Length != GetRequiredTouchCount())
            {
                SetState(GestureState.Failed);
            }
        }

        protected override void OnTouchPressed(TouchContact[] touches)
        {
            //whenever a new press comes in, clear the old recent flag
            ClearRecentFlag();

            if( touches.Length != GetRequiredTouchCount() )
            {
                SetState(GestureState.Failed);
                return;
            }
 
            m_fingerId = touches[0].fingerId;
            m_position = touches[0].startPosition;
        }

        //We can use the touch contact start position as the finger never left the screen.
        private bool IsTouchContactValidForTap( TouchContact tc )
        {
            bool bValid = (null != tc);

            if( bValid )
            {
                //Check position change since start.
                bValid &= (tc.position - tc.startPosition).LengthSquared() <= (k_MaxTouchRadius*k_MaxTouchRadius);

                //Check Time change since start
                bValid &= ((float)(Time.WallClockTotalSeconds - tc.startTime)) <= k_MaxTouchLifetime;
            }

            return bValid;
        }

    }
}
