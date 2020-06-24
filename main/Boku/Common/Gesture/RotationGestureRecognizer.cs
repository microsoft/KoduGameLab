
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

using Microsoft.Xna.Framework;

using KoiX;

using Boku.Common;

namespace Boku.Common.Gesture
{
    public class RotationGestureRecognizer : GestureRecognizer
    {

        const int kNumTouchesRequired = 2;

        protected override int GetRequiredTouchCount()
        {
            return kNumTouchesRequired;
        }

        /// <summary>
        /// Rotation DOT product threshold. This controls how tolerant the twist gesture detector is to the two fingers 
        /// moving in opposite directions.
        /// Setting this to -1 means the fingers have to move in exactly opposite directions relative to each other.
        /// This value has to be kept between -1 and 0
        /// </summary>
        const float k_MinDOT = -0.8f;

        /// <summary>
        /// Minimum amount of rotation required to start the rotation gesture( in radians )
        /// </summary>
        private float k_MinRotation = MathHelper.ToRadians(2.5f);
        private float k_MinInitialRotation = MathHelper.ToRadians(5.0f);

        const float k_NoRotationTimeout = 0.25f;

        //how much to multiply the output rotation by
        const float k_RotationMultiplier = 1.75f;
        
        //
        private int[] m_FingerId = new int[kNumTouchesRequired];
        private float m_RotationDelta = 0.0f;
        
        private float m_AccumulatedRotation = 0.0f;

        private double m_MinRotationLastTime = 0;
        private float m_MinRotationDelta = 0.0f;

        private Vector2 m_averagePosition = new Vector2(0.0f, 0.0f);


        public bool AreFingerIDValid()
        {
            Debug.Assert( m_FingerId.Length >= 2 );
            return (null == m_FingerId) ? false : (m_FingerId[0] >= 0 && m_FingerId[1] >= 0);
        }

        /// <summary>
        /// Get the total rotation since gesture started( in radians )
        /// </summary>
        public float TotalRotation
        {
            get { return m_AccumulatedRotation * k_RotationMultiplier; }
        }

        /// <summary>
        /// Get the total rotation since gesture started( in degrees )
        /// </summary>
        public float TotalRotationDegrees
        {
            get { return (TotalRotation * (180.0f / (float)Math.PI)); }
        }


        /// <summary>
        /// Get rotation change since last move( in radians )
        /// </summary>
        public float RotationDelta
        {
            get { return m_RotationDelta * k_RotationMultiplier; }
        }

        /// <summary>
        /// Get rotation change since last move( in degrees )
        /// </summary>
        public float RotationDeltaDegrees
        {
            get { return (RotationDelta * (180.0f / (float)Math.PI)); }
        }

        /// <summary>
        /// A rotation is considered "rotating" if the gesture is active and both fingers are down. 
        /// It is not rotating when the user lifts off one of the fingers, even if the gesture as a whole 
        /// is still considered active.
        /// </summary>
        public bool IsRotating
        {
            get
            {
                return GestureState.Changed == GetState() ||
                       GestureState.Began == GetState();
            }
        }

        /// <summary>
        /// The average position between the two fingers when this gesture was last active
        /// </summary>
        public Vector2 AveragePosition
        {
            get
            {
                return m_averagePosition;
            }
        }

        /// <summary>
        /// Returns a signed angle in degrees between current touch position and a reference position
        /// </summary>
        /// <param name="touchZero"></param>
        /// <param name="touchOne"></param>
        /// <param name="refPosZero"></param>
        /// <param name="refPosOne"></param>
        /// <returns></returns>
        private static float SignedAngularGap( TouchContact touchZero, TouchContact touchOne, Vector2 refPosZero, Vector2 refPosOne)
        {
            Vector2 curDir = touchZero.position - touchOne.position;
            curDir.Normalize();
            Vector2 refDir = refPosZero - refPosOne;
            refDir.Normalize();

            return SignedAngle(refDir, curDir);
        }

        private static float SignedAngularGap( Vector2 dir, Vector2 refDir )
        {
            dir.Normalize();
            refDir.Normalize();

            return SignedAngle( refDir, dir );
        }

        protected override void OnReset()
        {
            for(int i=0; i<kNumTouchesRequired; ++i )
            {
                m_FingerId[i] = -1;
            }

            m_MinRotationLastTime = 0;
            m_MinRotationDelta = 0.0f;
            m_RotationDelta = 0.0f;
            m_AccumulatedRotation = 0.0f;
        }

        protected override void OnTouchReleased(TouchContact[] touches)
        {
            int numTouches = GetRequiredTouchCount();
            if (touches.Length != numTouches || !AreFingerIDValid())
            {
                SetState(GestureState.Failed);
                return;
            }

            SetState(GestureState.Ended);
        }

        protected override void OnTouchMoved(TouchContact[] touches)
        {
            int numTouch = GetRequiredTouchCount();

            bool bFailed = touches.Length != numTouch;

            TouchContact[] tc = new TouchContact[numTouch];
            if( !bFailed )
            {
                if( AreFingerIDValid() )
                {
                    for (int i = 0; !bFailed && i < numTouch; ++i)
                    {
                        tc[i] = TouchInput.GetTouchContactByFingerId(m_FingerId[i], touches);
                        bFailed |= (null == tc[i]); //Fail if we didn't find the touch.
                    }
                }
                else
                {
                    for (int i = 0; i<numTouch; ++i)
                    {
                        tc[i] = touches[i];
                        m_FingerId[i] = touches[i].fingerId;
                    }
                }
            }

            if ( bFailed )
            {
                SetState(GestureState.Failed);
            }
            else
            {
                //We must have 2 valid touches here.
                Debug.Assert( null != tc[0] && null != tc[1] );

                if( GestureState.Possible == GetState() )
                {
                    if (TouchesMovedInOppositeDirection( tc[0], tc[1], k_MinDOT) )
                    {
                        //return false;
                        float rotation = SignedAngularGap( (tc[0].position - tc[1].position), (tc[0].previousPosition - tc[1].previousPosition) );
                        m_MinRotationDelta += rotation;
                        if (Math.Abs(m_MinRotationDelta) >= k_MinInitialRotation)
                        {
                            m_MinRotationDelta = 0.0f;
                            m_MinRotationLastTime = Time.WallClockTotalSeconds;
                            m_averagePosition = (tc[0].position + tc[1].position) * 0.5f;
                            SetState(GestureState.Began);
                        }
                    }
                }
                else
                {
                    bool bEnded = false;

                    //Calculate rotation delta and apply to total rotation
                    m_RotationDelta = SignedAngularGap( (tc[0].position - tc[1].position), (tc[0].previousPosition - tc[1].previousPosition) );
                    
                    m_AccumulatedRotation += m_RotationDelta;
                    m_MinRotationDelta += m_RotationDelta;

                    m_averagePosition = (tc[0].position + tc[1].position) * 0.5f;

                    //Check to see if we rotated fast enough or else the gesture ended.
                    if (Math.Abs(m_MinRotationDelta) < k_MinRotation)
                    {
                        bEnded = (Time.WallClockTotalSeconds - m_MinRotationLastTime) >= k_NoRotationTimeout;
                    }
                    else
                    {
                        m_MinRotationLastTime = Time.WallClockTotalSeconds;
                        m_MinRotationDelta = 0.0f;
                    }

                    if( bEnded )
                    {
                        SetState(GestureState.Ended);
                    }
                    else
                    {
                        SetState(GestureState.Changed);
                    }
                }

            }
        }

        protected override void OnTouchPressed(TouchContact[] touches)
        {
            int numTouches = GetRequiredTouchCount();
            if (touches.Length != numTouches)
            {
                SetState(GestureState.Failed);
                return;
            }

            Debug.Assert(m_FingerId.Length >= numTouches);
            for (int i = 0; i < numTouches; ++i)
            {
                m_FingerId[i] = touches[i].fingerId;
            }
        }
    }
}