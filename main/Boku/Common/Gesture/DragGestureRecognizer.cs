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
    public class DragGestureRecognizer : GestureRecognizer
    {
        const int kNumTouchesRequired = 1;

        protected override int GetRequiredTouchCount()
        {
            return kNumTouchesRequired;
        }

        /// <summary>
        /// If a drags minimum distance traveled is too low we don't track it.  Also used to determine if the drag is now idle.
        /// </summary>
        const float k_MinDragDistance = 15.0f;

        /// <summary>
        /// The time in seconds that if the drag is idle the gesture will end.
        /// </summary>
        const float k_IdleTimeOut = 0.5f;

        public Vector2 InitialPosition
        {
            get { return m_InitialPosition; }
        }
  
        public Vector2 DragPosition
        {
            get { return m_Position; }
        }

        public Vector2 DragPrevPosition
        {
            get { return m_PrevPosition; }
        }

        private int m_FingerID = -1;
        private Vector2 m_InitialPosition = Vector2.Zero;
        private Vector2 m_Position = Vector2.Zero;
        private Vector2 m_PrevPosition = Vector2.Zero;

        private double m_MinMoveTime = 0;
        private Vector2 m_MinMoveDelta = Vector2.Zero;


        /// <summary>
        /// We are considered dragging throughout the drag gesture.
        /// </summary>
        public bool IsDragging
        {
            get 
            { 
                return GestureState.Began == GetState() ||
                       GestureState.Changed == GetState(); 
            }
        }


        protected override void OnReset()
        {
            m_FingerID = -1;
            m_MinMoveDelta = Vector2.Zero;
            m_MinMoveTime = 0.0f;
        }

        protected override void OnTouchPressed(TouchContact[] touches) 
        {
            Debug.Assert(GetRequiredTouchCount() > 0);
            if( GetRequiredTouchCount() != touches.Length )
            {
                SetState(GestureState.Failed);
            }
            else
            {
                m_MinMoveTime = Time.WallClockTotalSeconds;
                m_FingerID = touches[0].fingerId;
                m_PrevPosition = m_Position = touches[0].position;
            }
        }

        protected override void OnTouchReleased(TouchContact[] touches)
        {
            //Always fail when a finger is raised.
            if( GetRequiredTouchCount() != touches.Length || m_FingerID < 0 )
            {
                SetState(GestureState.Failed);
            }

            SetState(GestureState.Ended);
        }

        protected override void OnTouchMoved(TouchContact[] touches)
        {
            TouchContact tc =  (GetRequiredTouchCount() != touches.Length || m_FingerID < 0) ? null : TouchInput.GetTouchContactByFingerId( m_FingerID, touches );
            
            if( null == tc )
            {
                SetState(GestureState.Failed);
            }
            else
            {
                m_PrevPosition = m_Position;
                m_Position = tc.position;

                m_MinMoveDelta += tc.deltaPosition;

                if( GestureState.Possible == GetState() )
                {
                    bool bMoved = m_MinMoveDelta.LengthSquared() >= (k_MinDragDistance * k_MinDragDistance);
                    bool bIdle = !bMoved && (Time.WallClockTotalSeconds - m_MinMoveTime) >= k_IdleTimeOut;
                    if ( bMoved )
                    {
                        m_MinMoveDelta = Vector2.Zero;
                        m_MinMoveTime = Time.WallClockTotalSeconds;
                        m_InitialPosition = m_Position;

                        SetState(GestureState.Began);
                    }
                }
                else
                {
                    SetState(GestureState.Changed);
                }
            }
        }
    }
}
