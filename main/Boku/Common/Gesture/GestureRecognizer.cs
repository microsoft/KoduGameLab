using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using System.Diagnostics;

namespace Boku.Common.Gesture
{
    public abstract class GestureRecognizer
    {

        public enum GestureState
        {
            /// <summary>
            /// Gesture is possible to make.
            /// </summary>
            Possible,

            /// <summary>
            /// These 3 states are used for continuous gestures. (e.g Rotation)
            /// </summary>
            Began,
            Changed,
            Ended,


            /// <summary>
            /// This state is for discrete gestures like a tap.  There is no begin - end. It just happened.
            /// </summary>
            Recognized,

            /// <summary>
            /// Gesture validation has failed.  Forces a reset on this recognizer.
            /// </summary>
            Failed,

        }

        GestureState m_state = GestureState.Possible;
        GestureState m_prevState = GestureState.Possible;


        public GestureState GetState() { return m_state; }
        public GestureState GetPrevState() { return m_prevState; }


        /// <summary>
        /// A gesture is considered 'validated' from the time it has become active till the time it is reset.
        /// This question is useful to ask to ensure that once a particular touch sequence has been recognized
        /// as a certain gesture, that other gestures can prevent themselves from activating.
        /// </summary>
        public bool IsValidated
        {
            get
            {
                return m_state == GestureState.Began ||
                       m_state == GestureState.Changed ||
                       wasActivated ||
                       wasRecognized;
            }
        }

        /// <summary>
        /// A gesture sets its 'wasActivated' flag only on the *first* frame that the gesture becomes valid.
        /// It is cleared every update step by the base class, so the subclasses are only responsible for
        /// setting it to true under the appropriate conditions.
        /// </summary>
        protected bool wasActivated;
        public bool WasActivated
        {
            get { return wasActivated; }
        }

        /// <summary>
        /// A gesture sets its 'wasRecognized' flag only on the *first* frame that the gesture completes.
        /// It is cleared every update step by the base class, so the subclasses are only responsible for
        /// setting it to true under the appropriate conditions.
        /// </summary>
        protected bool wasRecognized;
        public bool WasRecognized
        {
            get { return wasRecognized; }
        }

        /// <summary>
        /// A gesture sets its 'wasRecognized' flag only on the *first* frame that the gesture completes.
        /// It is cleared every update step by the base class, so the subclasses are only responsible for
        /// setting it to true under the appropriate conditions.
        /// </summary>
        private const float kRecentlyRecognizedTimeout = 0.5f;
        protected bool wasRecentlyRecognized;
        public bool WasRecentlyRecognized
        {
            get { return wasRecentlyRecognized; }
        }

        protected float timeSinceRecognized = 0.0f;
        public float TimeSinceRecognized
        {
            get { return timeSinceRecognized; }
        }


        //SubClasses need to override these to handle gesture recognition
        protected abstract int GetRequiredTouchCount(); //From old implementation.
        protected abstract void OnTouchPressed( TouchContact[] touches );
        protected abstract void OnTouchReleased( TouchContact[] touches );
        protected abstract void OnTouchMoved( TouchContact[] touches );

        protected virtual void OnReset() {}
        protected virtual void OnRecognized() {}

        private void Reset()
        {
            m_prevState = m_state;
            m_state = GestureState.Possible;

            if (m_prevState == GestureState.Recognized)
            {
                wasRecentlyRecognized = true;
            }

            OnReset();
        }

        protected void ClearRecentFlag()
        {
            wasRecentlyRecognized = false;
        }

        protected void SetState( GestureState state )
        {
            m_prevState = m_state;
            m_state = state;

            if (m_state != GestureState.Recognized && m_prevState == GestureState.Recognized)
            {
                wasRecentlyRecognized = true;
            }

            switch (state)
            {
                //FOR
                //Continuous Gestures
                case GestureState.Began: 
                    wasActivated = true;
                    OnRecognized();
                    break;

                case GestureState.Changed: 
                    OnRecognized();
                    break;

                case GestureState.Ended:
                    wasRecognized = true;
                    OnRecognized();
                    Reset();
                    break;

                //FOR
                //Discrete Gestures
                case GestureState.Recognized: 
                    wasActivated = true;
                    wasRecognized = true;
                    wasRecentlyRecognized = false;
                    timeSinceRecognized = 0.0f;
                    OnRecognized();
                    Reset();
                    break;


                //FOR
                //ALL
                case GestureState.Failed:
                    Reset();
                    break;
            }
        }

        private bool ShouldSendEvents( GestureState state )
	    {
		    return state == GestureState.Possible ||
			       state == GestureState.Began ||
			       state == GestureState.Changed;
	    }


        public virtual void Update(TouchContact[] touches)
        {

            //// Clear any edge-case flags that were set last frame.
            wasActivated = false;
            wasRecognized = false;

            bool bDidBegin = false;
            bool bDidEnd = false;
            bool bDidMove = false;
            
            for (int i = 0; i < touches.Length; ++i)
            {
                bDidBegin |= TouchPhase.Began == touches[i].phase;
                bDidEnd |= TouchPhase.Ended == touches[i].phase;
                bDidMove |= TouchPhase.Moved == touches[i].phase || TouchPhase.Stationary == touches[i].phase;
            }

            if (ShouldSendEvents(m_state))
            {
                if (bDidEnd)
                {
                    OnTouchReleased(touches);
                }

                if (bDidBegin)
                {
                    OnTouchPressed(touches);
                }

                if (bDidMove)
                {
                    OnTouchMoved(touches);
                }
            }

            if ( touches.Length == 0 || (bDidEnd && touches.Length == 1))
            {
                Reset();
            }

            if (m_state != GestureState.Recognized && wasRecentlyRecognized)
            {
                timeSinceRecognized += Time.WallClockFrameSeconds;
                if (timeSinceRecognized > kRecentlyRecognizedTimeout)
                {
                    wasRecentlyRecognized = false;
                }
            }
        }

        #region Utils
        

        /// <summary>
        /// Check if the input touches are moving in opposite direction
        /// </summary>
        public static bool TouchesMovedInOppositeDirection( TouchContact tc0, TouchContact tc1, float minDOT)
        {
            bool bValid = Vector2.Zero == tc0.deltaPosition ^ Vector2.Zero == tc1.deltaPosition;
            if( !bValid )
            {
                Vector2 touchZeroDeltaNormal = Vector2.Normalize(tc0.deltaPosition);
                Vector2 touchOneDeltaNormal = Vector2.Normalize(tc1.deltaPosition);

                bValid = Vector2.Dot(touchZeroDeltaNormal, touchOneDeltaNormal) < minDOT;
            }
            return bValid;
        }

        /// <summary>
        /// Check if the input touches are moving in the same direction
        /// </summary>
        public static bool TouchesMovedInSameDirection(ref TouchContact tc0, ref TouchContact tc1, float maxDOT)
        {
            if(Vector2.Zero != tc0.deltaPosition && Vector2.Zero != tc1.deltaPosition)
            {
                Vector2 touchZeroDeltaNormal = Vector2.Normalize(tc0.deltaPosition);
                Vector2 touchOneDeltaNormal = Vector2.Normalize(tc1.deltaPosition);

                return Vector2.Dot(touchZeroDeltaNormal, touchOneDeltaNormal) > maxDOT;
            }
            return false;
        }


        /// <summary>
        /// returns signed angle in radians between "from" -> "to"
        /// </summary>
        public static float SignedAngle(Vector2 from, Vector2 to)
        {
            if (from == to)
            {
                return 0.0f;
            }
            // perpendicular dot product
            float perpDot = (from.X * to.Y) - (from.Y * to.X);
            return (float)Math.Atan2(perpDot, Vector2.Dot(from, to));
        }

        public static Vector2 GetAverageTouchPosition(ref TouchContact[] touches)
        {
            Vector2 averagePosition = Vector2.Zero;
            
            int count = 0;
            for (int i = 0; i < touches.Length; ++i)
            {
                if( null != touches[i] )
                {
                    averagePosition += touches[i].position;
                    count++;
                }
            }
            
            averagePosition /= (float)(count > 0 ? count : 1);
            return averagePosition;
        }

        #endregion Utils
    }
}
