// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

//#define TEST_INPUT

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using System.Diagnostics;

using KoiX;

using Boku.Common;

namespace Boku.Common.Gesture
{
    public class PinchGestureRecognizer : GestureRecognizer
    {
        public enum PinchState
        {
            /// <summary>
            /// The fingers distance is growing
            /// </summary>
            Growing,

            /// <summary>
            /// The fingers distance is shrinking
            /// </summary>
            Shrinking,

            /// <summary>
            /// The fingers are not moving
            /// </summary>
            Static,

            Invalid,
        }
        
        private float k_MovementAlongLineDot = 0.75f;
        private float k_MovementAlongLineDot_PostBegin = 0.6f; //Allow a bigger range of side motion to the gesture to allow rotation/pinch


        const float k_PinchStaticDeadZone = 10.0f;
        const float k_MinDeltaDistance = 8.0f; //min accumulated distance before checking for failure
        const float k_MinStateChangeDistance = 2.0f; //min single frame distance before switching pinch direction state

        //how long fingers stop moving before we put the gesture into static state
        const float k_PinchStaticTimeout = 0.125f;

        //how long fingers stop moving before pinch gesture fails
        const float k_PinchIdleTimeout = 0.50f;
        
        const int kNumTouchesRequired = 2;


        public PinchGestureRecognizer()
        {
            //ResetMode = GestureResetMode.StartOfTouchSequence;
            m_FingerIdx = new int[kNumTouchesRequired];
            m_AccumulatedDelta = new Vector2[kNumTouchesRequired];
            m_PreviousPosition = new Vector2[kNumTouchesRequired];
        }

        protected override int GetRequiredTouchCount()
        {
            return kNumTouchesRequired;
        }


        private int[] m_FingerIdx;
        private Vector2[] m_AccumulatedDelta;
        private Vector2[] m_PreviousPosition;
        private float m_AccumulatedDeltaLength = 0.0f;

        private bool m_bPinching = false;
        private float m_originalDist = 0.0f;
        
        private float m_Scale = 1.0f;
        private float m_DeltaScale = 0.0f;

        private Vector2 m_averagePosition;
        
        
        private double m_IdleTime = 0;
        private float m_StaticTime = 0.0f;
        private PinchState m_pinchState;


        public bool IsPinching
        {
            get { return IsValidated; }
        }
        
        public float Scale
        {
            get{ return m_Scale; }
        }

        public float DeltaScale
        {
            get{ return (IsPinching ? m_DeltaScale : 0.0f); } 
        }

        public bool IsPinchStateValid
        {
            get { return IsPinching && (PinchState.Growing == m_pinchState ||
                                        PinchState.Shrinking == m_pinchState); }
        }

        /// <summary>
        /// The average position between the two fingers when this gesture was last active
        /// </summary>
        public Vector2 AveragePosition
        { 
            get { return m_averagePosition; }
        }

        public PinchState GetPinchState()
        {
            return (IsPinching) ? m_pinchState : PinchState.Invalid;
        }

        private bool AreFingerIDValid()
        {
            return (null == m_FingerIdx) ? false : (m_FingerIdx[0] >= 0 && m_FingerIdx[1] >= 0);
        }

        protected override void OnReset()
        {
            for( int i = 0; i<kNumTouchesRequired; ++i )
            {
                m_FingerIdx[i] = -1;
                m_AccumulatedDelta[i] = Vector2.Zero;
                m_PreviousPosition[i] = Vector2.Zero;
            }

            if( !m_bPinching )
            {
                m_DeltaScale = 0.0f;
                m_pinchState = PinchState.Invalid;
            }

            m_bPinching = false;
        }


        protected override void OnTouchReleased(TouchContact[] touches)
        {
            int numTouch = GetRequiredTouchCount();

            bool bFailed = touches.Length != numTouch || !AreFingerIDValid();

            TouchContact[] tc = new TouchContact[numTouch];
            for (int i = 0; !bFailed && i < numTouch; ++i)
            {
                tc[i] = TouchInput.GetTouchContactByFingerId(m_FingerIdx[i], touches);
                bFailed |= (null == tc);
            }

            if (bFailed)
            {
                SetState(GestureState.Failed);
            }
            else
            {
                SetState(GestureState.Ended);
            }
        }

        protected override void OnTouchMoved(TouchContact[] touches)
        {
            int numTouch = GetRequiredTouchCount();

            bool bFailed = touches.Length != numTouch;

            TouchContact[] tc = new TouchContact[numTouch];
            if (!bFailed)
            {
                if (AreFingerIDValid())
                {
                    for (int i = 0; !bFailed && i < numTouch; ++i)
                    {
                        tc[i] = TouchInput.GetTouchContactByFingerId(m_FingerIdx[i], touches);
                        bFailed |= (null == tc[i]); //Fail if we didn't find the touch.
                    }
                }
                else
                {
                    for (int i = 0; i < numTouch; ++i)
                    {
                        tc[i] = touches[i];
                        m_FingerIdx[i] = touches[i].fingerId;
                        m_AccumulatedDelta[i] = Vector2.Zero;
                        m_PreviousPosition[i] = touches[i].position;
                    }

                    m_IdleTime = Time.WallClockTotalSeconds;
                    m_AccumulatedDeltaLength = 0.0f;
                }
            }

            if( !bFailed )
            {
                Debug.Assert( tc.Length >= 2 );
                Vector2 currentDir = tc[1].position - tc[0].position;
                Vector2 prevDir = m_PreviousPosition[1] - m_PreviousPosition[0];

                m_AccumulatedDelta[0] += tc[0].position - m_PreviousPosition[0];
                m_AccumulatedDelta[1] += tc[1].position - m_PreviousPosition[1];

                //update previous positions
                m_PreviousPosition[0] = tc[0].position;
                m_PreviousPosition[1] = tc[1].position;

                float currentLength = currentDir.Length();
                float previousLength = prevDir.Length();
                float deltaLength = currentLength - previousLength;

                //determine pinch direction based on one frame change, but only if the change is big enough
                //and only switch to static after a time out period
                if (Math.Abs(deltaLength) > k_MinStateChangeDistance && currentLength > previousLength)
                {
                    //big change apart, pinch is growing
                    m_StaticTime = 0.0f;
                    m_pinchState = PinchState.Growing;
                }
                else if (Math.Abs(deltaLength) > k_MinStateChangeDistance && previousLength > currentLength)
                {
                    //big change together, pinch is shrinking
                    m_StaticTime = 0.0f;
                    m_pinchState = PinchState.Shrinking;
                }
                else if (m_pinchState != PinchState.Static)
                {
                    //not enough change, count up the static timer
                    m_StaticTime += Time.WallClockFrameSeconds;
                    if (m_StaticTime > k_PinchStaticTimeout)
                    {
                        m_pinchState = PinchState.Static;
                    }
                }

                //accumulate changes until we reach a threshold, at that point, check to make sure direction conditions still apply
                m_AccumulatedDeltaLength += deltaLength;

                bool Finger1LengthOk = (m_AccumulatedDelta[0].LengthSquared() > (k_PinchStaticDeadZone * k_PinchStaticDeadZone));
                bool Finger2LengthOk = (m_AccumulatedDelta[1].LengthSquared() > (k_PinchStaticDeadZone * k_PinchStaticDeadZone));

                if( Math.Abs(m_AccumulatedDeltaLength) > k_MinDeltaDistance && (Finger1LengthOk || Finger2LengthOk) )
                {
                    if(GestureState.Possible == GetState())
                    {
                        //Fix the original distance and scale. Since we started pinching here but we've moved the fingers some amount to actual
                        if (!m_bPinching)
                        {
                            m_StaticTime = 0.0f;
                            m_Scale = 1.0f;
                            m_originalDist = currentLength;
                            m_bPinching = true;
                        }

                        bFailed |= Finger1LengthOk && !AreVectorsParallel(prevDir, m_AccumulatedDelta[0], k_MovementAlongLineDot);
                        bFailed |= Finger2LengthOk && !AreVectorsParallel(prevDir, m_AccumulatedDelta[1], k_MovementAlongLineDot);
                    }
                    else
                    {
                        bFailed |= Finger1LengthOk && !AreVectorsParallel(prevDir, m_AccumulatedDelta[0], k_MovementAlongLineDot_PostBegin);
                        bFailed |= Finger2LengthOk && !AreVectorsParallel(prevDir, m_AccumulatedDelta[1], k_MovementAlongLineDot_PostBegin);
                    }

                    m_AccumulatedDelta[0] = Vector2.Zero;
                    m_AccumulatedDelta[1] = Vector2.Zero;

                    m_AccumulatedDeltaLength = 0.0f;

                    m_IdleTime = Time.WallClockTotalSeconds;
                }
                else
                {
                    bFailed |= (Time.WallClockTotalSeconds - m_IdleTime) > k_PinchIdleTimeout;
                }

                m_DeltaScale = m_Scale;
                m_Scale = (m_originalDist > 0) ? currentLength / m_originalDist : 1.0f;
                m_DeltaScale = (m_Scale - m_DeltaScale);

                m_averagePosition = (tc[0].position + tc[1].position) * 0.5f;

                if( !m_bPinching )
                {
                    m_pinchState = PinchState.Invalid;
                }
            }

            if( bFailed )
            {
                SetState(GestureState.Failed);
            }
            else
            {
                if (GestureState.Possible == GetState())
                {
                    if ( m_bPinching )
                    {
                        SetState(GestureState.Began);
                    }
                }
                else
                {
                    SetState(GestureState.Changed);
                }
            }
        }

        protected override void OnTouchPressed(TouchContact[] touches)
        {
            int numTouches = GetRequiredTouchCount();
            if( touches.Length != numTouches )
            {
                SetState(GestureState.Failed);
                return;
            }

            Debug.Assert(m_FingerIdx.Length >= numTouches);
            for( int i=0; i<numTouches; ++i )
            {
                m_FingerIdx[i] = touches[i].fingerId;
                m_AccumulatedDelta[i] = Vector2.Zero;
                m_PreviousPosition[i] = Vector2.Zero;
            }

            Debug.Assert(touches.Length >= 2 );
            m_originalDist = (touches[0].position - touches[1].position).Length();
            m_AccumulatedDeltaLength = 0.0f;
            m_IdleTime = Time.WallClockTotalSeconds;
            m_StaticTime = 0.0f;
            m_Scale = 1.0f;
            m_DeltaScale = 0.0f;

        }

        private bool AreVectorsParallel( Vector2 line0, Vector2 line1, float dotThreshold )
        {
            //Perform Dot on both normalized vectors.
            //Checking both direction with Absolute on Dot.
            return Math.Abs(Vector2.Dot( Vector2.Normalize(line0), Vector2.Normalize(line1))) > dotThreshold;
        }
    }
}
