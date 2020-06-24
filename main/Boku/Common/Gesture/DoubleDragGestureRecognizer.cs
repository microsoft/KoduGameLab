using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using System.Diagnostics;

using KoiX;

namespace Boku.Common.Gesture
{
    public class DoubleDragGestureRecognizer : GestureRecognizer
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
        const float k_MaxDOT = 0.9f;

        /// <summary>
        /// Dot between movement direction and up vector - this value tracks how close to vertical we have to be to respond
        /// as a vertical double drag.
        /// </summary>
        const float k_MinDOTForVertical = 0.8f;

        /// <summary>
        /// If a drags minimum distance traveled is too low we don't track it
        /// </summary>
        const float k_MinDragDistance = 15.0f;

        const float k_DoubleDragIdleTimeout = 0.25f;

        private double m_IdleTime = 0;

        /// <summary>
        ///  Return the last registered drag end position. This could be None if no valid drag was detected
        /// </summary>
        private Vector2 m_averagePosition = Vector2.Zero;
        public Vector2 AveragePosition
        {
            get { return m_averagePosition; }
            protected set 
            {
                m_PrevAveragePosition = m_averagePosition;
                m_averagePosition = value;
            }
        }

        private Vector2 m_PrevAveragePosition = Vector2.Zero;
        public Vector2 PreviousAveragePosition
        {
            get { return m_PrevAveragePosition; }
        }

        private Vector2 m_dragDirection= Vector2.Zero;
        public Vector2 DragDirection
        {
            get { return m_dragDirection; }
        }

        private bool m_isVertical = false;
        public bool IsVertical
        {
            get { return m_isVertical; }
        }


        private bool m_dragStarted = false;

        private int[] m_FingerIdx = new int[kNumTouchesRequired]; 
        
        
        //This variable is used to keep track of the movement of the fingers since detection.  
        //We use this because we can detect the finger after the fact that it was pressed and so using startPosition on the touch contact would be wrong some times.
        private Vector2[] m_AccumulatedDeltaDir = new Vector2[kNumTouchesRequired]; 
        
        protected override void OnReset()
        {
            for( int i=0; i<m_FingerIdx.Length; ++i )
            {
                m_FingerIdx[i] = -1;
            }

            //previousAveragePosition = averagePosition = Vector2.Zero;
            m_dragStarted = false;
        }

        public bool GotTwoPresses()
        {
            return (null == m_FingerIdx) ? false : (m_FingerIdx[0] >= 0 && m_FingerIdx[1] >= 0);
        }

        /// <summary>
        /// We are considered dragging throughout the drag gesture, and for the one frame after the drag
        /// has completed (at which point the total movement of the drag is now known)
        /// </summary>
        public bool IsDragging
        {
            get
            {
                return IsValidated;
            }
        }

        protected override void OnTouchReleased(TouchContact[] touches)
        {
            if( touches.Length != GetRequiredTouchCount() )
            {
                SetState(GestureState.Failed);
                return;
            }

            m_FingerIdx[0] = touches[0].fingerId;
            m_FingerIdx[1] = touches[1].fingerId;
        }

        protected override void OnTouchMoved(TouchContact[] touches)
        {
            TouchContact finger1 = null;
            TouchContact finger2 = null;

            bool bFailed = touches.Length != GetRequiredTouchCount();

            
            if( !bFailed  )
            {
                //If we don't have 2 fingers then assign them
                if( !GotTwoPresses() )
                {
                    m_FingerIdx[0] = touches[0].fingerId;
                    m_FingerIdx[1] = touches[1].fingerId;

                    finger1 = touches[0];
                    finger2 = touches[1];

                    m_dragStarted = false;
                }
                else
                {
                    finger1 = TouchInput.GetTouchContactByFingerId(m_FingerIdx[0], touches);
                    finger2 = TouchInput.GetTouchContactByFingerId(m_FingerIdx[1], touches);
                }

                bFailed |= (null == finger1) || (null == finger2);
            }

            if( bFailed )
            {
                SetState(GestureState.Failed);
            }
            else
            {
                if( m_dragStarted )
                {
                    if (TouchesMovedInSameDirection(ref finger1, ref finger2, k_MaxDOT))
                    {
                        m_IdleTime = Time.WallClockTotalSeconds;
                        SetState(GestureState.Changed);
                    }
                    else
                    {
                        //give it a short amount of time to time-out
                        if ((Time.WallClockTotalSeconds - m_IdleTime) < k_DoubleDragIdleTimeout)
                        {
                            SetState(GestureState.Changed);
                        }
                        else
                        {
                            SetState(GestureState.Ended);
                        }
                    }
                }
                else
                {
                    m_AccumulatedDeltaDir[0] += finger1.position - finger1.previousPosition;
                    m_AccumulatedDeltaDir[1] += finger2.position - finger2.previousPosition;

                    if( m_AccumulatedDeltaDir[0].LengthSquared() >= (k_MinDragDistance * k_MinDragDistance) ||
                        m_AccumulatedDeltaDir[1].LengthSquared() >= (k_MinDragDistance * k_MinDragDistance) )
                    {
                        m_AccumulatedDeltaDir[0] = Vector2.Zero;
                        m_AccumulatedDeltaDir[1] = Vector2.Zero;


                        //One Finger has dragged far enough to start dragging.
                        if( TouchesMovedInSameDirection(ref finger1, ref finger2, k_MaxDOT) )
                        {
                            m_dragStarted = true;
                            SetState(GestureState.Began);
                        }
                        else
                        {
                            SetState(GestureState.Failed);
                        }
                    }
                }
            }
        }

        protected override void OnTouchPressed(TouchContact[] touches)
        {
            if( touches.Length != GetRequiredTouchCount() )
            {
                SetState(GestureState.Failed);
                return;
            }

            //Two fingers are down, Record their IDs
            m_FingerIdx[0] = touches[0].fingerId;
            m_FingerIdx[1] = touches[1].fingerId;

            m_AccumulatedDeltaDir[0] = Vector2.Zero;
            m_AccumulatedDeltaDir[1] = Vector2.Zero;

            AveragePosition = GetAverageTouchPosition(ref touches);
            UpdateDragDirection();
        }

        protected override void OnRecognized()
        {
            TouchContact[] fingers = new TouchContact[]
            {
                TouchInput.GetTouchContactByFingerId(m_FingerIdx[0]),
                TouchInput.GetTouchContactByFingerId(m_FingerIdx[1])
            };

            AveragePosition = GetAverageTouchPosition(ref fingers);

            UpdateDragDirection();
        }

        private void UpdateDragDirection()
        {
            Vector2 newDrag = AveragePosition - PreviousAveragePosition;
            if (newDrag.LengthSquared() > 0)
            {
                newDrag.Normalize();

                //assign the new drag direction
                m_dragDirection = newDrag; 

                Vector2 yAxis = new Vector2(0.0f, 1.0f);

                float cosAngle = Vector2.Dot(newDrag, yAxis);

                //check if the movement is within tolerance to be considered vertical
                m_isVertical = (Math.Abs(cosAngle) >= k_MinDOTForVertical);

            }
        }
    }
}
