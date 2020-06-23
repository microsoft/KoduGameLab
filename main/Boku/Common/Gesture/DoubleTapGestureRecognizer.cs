using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using System.Diagnostics;

namespace Boku.Common.Gesture
{
    public class DoubleTapGestureRecognizer : GestureRecognizer
    {

        /// <summary>
        /// The maximum gap (in seconds) that a touch can have before the next touch starts before the double tap is disqualified.
        /// </summary>
        const float k_MaxTouchGapTime = 0.5f;

        /// <summary>
        /// The maximum lifetime (in seconds) that a touch can have before it is disqualified from being considered
        /// a tap.
        /// </summary>
        const float k_MaxTouchLifetime = 0.6f;

        /// <summary>
        /// How far can the touch positions drift before we consider the gesture failed
        /// </summary>
        /// //FIXME: these numbers need to be converted to a percentage of screen space - raw pixel values will cause differing behaviour 
        /// //from one touch device to the next
        const float k_MaxTouchDrift = 50.0f;


        private double m_lastTapTime = 0;
        private bool m_bWaitingForNextTap = false;
        private int m_FingerId = -1;
        private Vector2 m_initialPosition = Vector2.Zero;
        private Vector2 m_position = Vector2.Zero;

        public Vector2 Position
        {
            get { return m_position; }
        }

        public Vector2 InitialPosition
        {
            get { return m_initialPosition; }        
        }

        protected override int GetRequiredTouchCount()
        {
            return 1;
        }

        public bool WasDoubleTapped()
        {
            return wasRecognized;
        }

        protected override void OnReset()
        {
            if( m_bWaitingForNextTap )
            {
                //If we reset for too long. then we don't wait for next tap.
                if ((float)(Time.WallClockTotalSeconds - m_lastTapTime) >= k_MaxTouchGapTime)
                {
                    m_bWaitingForNextTap = false;
                }
            }
            
            if( !m_bWaitingForNextTap )
            {
                m_lastTapTime = 0;
            }

            m_FingerId = -1;
        }

        protected override void OnTouchPressed(TouchContact[] touches)
        {
            if( touches.Length != GetRequiredTouchCount() )
            {
                SetState(GestureState.Failed);
                return;
            }

            m_FingerId = touches[0].fingerId;
            m_position = touches[0].position;

            if (!m_bWaitingForNextTap)
            {
                m_initialPosition = m_position;
            }
        }

        protected override void OnTouchReleased(TouchContact[] touches)
        {
            bool bFailed = touches.Length != GetRequiredTouchCount();
            
            TouchContact tc = TouchInput.GetTouchContactByFingerId(m_FingerId, touches);
            bFailed |= !IsTouchContactValidForTap( tc );

            if(bFailed)
            {
                SetState(GestureState.Failed);
                return;
            }
            else
            {
                if( m_bWaitingForNextTap )
                {
                    m_bWaitingForNextTap = false;
                    
                    Debug.Assert( null != tc );

                    m_position = tc.position;

                    float deltaPosition = (m_position - m_initialPosition).Length();

                    if ((float)(Time.WallClockTotalSeconds - m_lastTapTime) <= k_MaxTouchGapTime &&
                        deltaPosition <= k_MaxTouchDrift)
                    {
                        SetState(GestureState.Recognized);
                    }
                    else
                    {
                        SetState(GestureState.Failed);
                    }
                }
                else
                {
                    m_lastTapTime = Time.WallClockTotalSeconds;
                    m_bWaitingForNextTap = true;
                }
            }
        }

        protected override void OnTouchMoved(TouchContact[] touches)
        {
            bool bFailed = touches.Length != GetRequiredTouchCount();

            bFailed |= !IsTouchContactValidForTap(TouchInput.GetTouchContactByFingerId(m_FingerId, touches));

            if (bFailed)
            {
                m_bWaitingForNextTap = false;
                SetState(GestureState.Failed);
            }
        }


        private bool IsTouchContactValidForTap(TouchContact tc)
        {
            bool bValid = (null != tc);

            if (bValid)
            {
                //Check position change from start.
                bValid &= (tc.position - tc.startPosition).LengthSquared() <= (k_MaxTouchDrift * k_MaxTouchDrift);

                //Check Time change since start
                bValid &= ((float)(Time.WallClockTotalSeconds - tc.startTime)) <= k_MaxTouchLifetime;
            }

            return bValid;
        }
    }
}
