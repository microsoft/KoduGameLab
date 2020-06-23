using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using System.Diagnostics;

namespace Boku.Common.Gesture
{
    public class TouchHoldGestureRecognizer : GestureRecognizer
    {

        /// <summary>
        /// The gesture will automatically trigger after a hold for this amount of time has been detected
        /// </summary>
        const float k_TriggerHoldTime = 0.75f;

        /// <summary>
        /// The gesture first trigger time.  This can be used by some classes that need a smaller touch hold time.
        /// </summary>
        const float k_SlightHoldTime = 0.15f;

        /// <summary>
        /// The maximum radius that the touch can drift from its start position before it is no longer considered
        /// a this gesture.
        /// </summary>
        const float k_DriftRadiusLimit = 20.0f;


        /// <summary>
        /// When a touch sequence starts, we store the finger id in order to know which finger is tapping
        /// </summary>
        int m_FingerId = -1;


        //This variable is necessary to hold the recognized value for the rest of the frame.
        bool m_bRecognized;

        bool m_bCheckSlightHold = true;
        bool m_bSlightHoldMade = false;
        public bool SlightHoldMade { get { return m_bSlightHoldMade; } }

        Vector2 m_Position = new Vector2();
        public Vector2 Position { get { return m_Position; } }

        protected override int GetRequiredTouchCount()
        {
            return 1;
        }

        public bool WasHeld()
        {
            return m_bRecognized;
        }


        protected override void OnTouchReleased(TouchContact[] touches)
        {
            //When we release any finger we fail.
            SetState(GestureState.Failed);
        }

        protected override void OnTouchMoved(TouchContact[] touches)
        {
            TouchContact tc = (GetRequiredTouchCount() != touches.Length || m_FingerId < 0) ? null : TouchInput.GetTouchContactByFingerId( m_FingerId, touches );

            bool bFailed = (null == tc);
            if( !bFailed )
            {
                m_Position = tc.position;

                //Check if finger has drifted.
                bFailed |= (tc.position - tc.startPosition).LengthSquared() > (k_DriftRadiusLimit*k_DriftRadiusLimit);
            
                //Recognize gesture if we have exceeded the time.
                m_bRecognized = !bFailed && (Time.WallClockTotalSeconds - tc.startTime) >= k_TriggerHoldTime;

                //Recognize the slight hold if delta time is greater than check.
                m_bSlightHoldMade = !bFailed && m_bCheckSlightHold && (Time.WallClockTotalSeconds - tc.startTime) >= k_SlightHoldTime;
                if (m_bSlightHoldMade)
                {
                    //When slight hold is made, Stop checking until gesture is reset.
                    m_bCheckSlightHold = false;
                }
            }

            //Assert that both values are never both true.
            Debug.Assert( !(bFailed && m_bRecognized) );
            if (bFailed)
            {
                m_Position = new Vector2();
                SetState(GestureState.Failed);
            }
            else if (m_bRecognized)
            {
                
                SetState(GestureState.Recognized);
            }
            else
            {
                //not failed, but not recognized - began!
                if (IsValidated)
                {
                    SetState(GestureState.Changed);
                }
                else
                {
                    SetState(GestureState.Began);
                }
            }

        }

        protected override void OnTouchPressed(TouchContact[] touches)
        {
            if( touches.Length != GetRequiredTouchCount() )
            {
                SetState(GestureState.Failed);
            }

            Debug.Assert(touches.Length > 0);
            m_FingerId = touches[0].fingerId;
            m_Position = touches[0].position;
            m_bCheckSlightHold = true;
        }

        protected override void OnReset()
        {
            //Reset only if not recognized.
            if( GestureState.Recognized != GetState() )
            {
                m_bRecognized = false;
            }

            m_bCheckSlightHold = true;
            m_FingerId = -1;
        }

    }
}
