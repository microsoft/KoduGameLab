using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;

using Boku.Programming;
using System.Diagnostics;

namespace Boku.Common.Gesture
{
    public class SwipeGestureRecognizer : GestureRecognizer
    {
        //IMPORTANT. FOR GESTURE TO WORK PROPERLY
        //k_SwipeDeadZone < k_RefPointChangeThreshold < k_SwipeThreshold 

        //DeadZone radius for the swipe detection.  
        //When the distance from the ref position to the current position is greater than this then we can actually start direction detection.
        const float k_SwipeDeadZone = 15.0f;

        //When the distance from refPosition to current is greater than this, ref pos becomes current.
        const float k_RefPointChangeThreshold = 35.0f;

        //Minimum travel distance for gesture to validate.
        const float k_SwipeThreshold = 50.0f;
        
        //Time that 
        const float k_IdleTimeout = 0.25f;

        /// <summary>
        /// The gesture must follow the four cardinal directions of up/down, left/right. This value 
        /// lets it deviate a bit from the horizontal or vertical directions.
        /// Should be kept between 0 and 0.5f, where 0 means no tolerance and 0.5f means about 
        /// 45 degrees of tolerance.
        /// </summary>
        const float k_MaxAveragedDeviationDOT = 0.2f;

        /// <summary>
        /// The points that make up the average direction of the swipe must fall within this
        /// deviation, otherwise the swipe is disqualified due to it having clearly changed direction during
        /// the movement.
        /// </summary>
        const float k_MaxAddendDeviationDOT = 0.6f;

        /// <summary>
        /// A swipe is a fast gesture, if the length of the resulting velocity vector goes below
        /// this threshold, we're not a swipe.
        /// </summary>
        const float k_MinimumSpeed = 10.0f;


        /// <summary>
        /// The speed factor can be used as a gauge of how vigorously the user has swiped. It uses the actual
        /// velocity of the swipe, the screen resolution, and average velocities to assign a percentage
        /// value to the swipe, with 1.0 being 'as fast as possible'.
        /// The min and max travel factors indicate what % of the user's screen the user must travel to
        /// attain a minimum or maximum speed factor. Anything above or below these limits is clamped..
        /// </summary>
        float m_SpeedFactor = 0.0f;
        const float MIN_TRAVEL_FACTOR = 0.01f;
        const float MAX_TRAVEL_FACTOR = 0.25f;

        public float SpeedFactor
        {
            get { return m_SpeedFactor; }
        }

        private List<Vector2> m_TouchDeltaList = new List<Vector2>();
        private Vector2 m_AverageVelocity = Vector2.Zero;



        //Swipe Touch Finger ID
        private int m_FingerID = -1;
        public bool IdentifiedFinger { get { return m_FingerID >= 0; } }

        private double m_DeadZoneIdleTime = 0;
        

        //This is the location to compare the current position to ensure we're still going in the right direction.  
        //It gets updated when the distance from ref to Current is greater than k_RefPointChangeThreshold.
        private Vector2 m_SwipeRefPosition = Vector2.Zero;

        //origin point for a succesful swipe
        private Vector2 m_InitialPosition = Vector2.Zero;
        public Vector2 InitialPosition
        {
            get { return m_InitialPosition; }
        }

        /// <summary>
        ///  Return the last registered swipe direction. This could be None if no valid swipe was detected
        /// </summary>
        private Directions m_SwipeDirection = Directions.None;
        public Directions SwipeDirection
        {
            get { return m_SwipeDirection; }
            protected set { m_SwipeDirection = value; }
        }



        /// <summary>
        /// Check if the user completed a swipe gesture on *this* frame.
        /// </summary>
        public bool WasSwiped()
        {            
            return (m_SwipeDirection != Directions.None) && wasRecognized;
        }

        protected override int GetRequiredTouchCount()
        {
            return 1;
        }


        protected override void OnTouchReleased(TouchContact[] touches)
        {
            TouchContact tc = (GetRequiredTouchCount() != touches.Length || m_FingerID < 0) ? null : TouchInput.GetTouchContactByFingerId( m_FingerID, touches );
            
            bool bFailed = (null == tc);
            if( !bFailed )
            {
                bFailed |= Directions.None == m_SwipeDirection;
                bFailed |= (tc.position - tc.startPosition).LengthSquared() < (k_SwipeThreshold * k_SwipeThreshold);
                bFailed |= m_SwipeDirection != GetSwipeDirection(Vector2.Normalize(m_AverageVelocity), k_MaxAddendDeviationDOT);
            }
            
            if( bFailed )
            {
                SetState(GestureState.Failed);
            }
            else
            {
                ComputeSpeedFactor();
                SetState(GestureState.Recognized);
            }


        }

        protected override void OnTouchMoved(TouchContact[] touches)
        {
            TouchContact tc =  (GetRequiredTouchCount() != touches.Length || m_FingerID < 0) ? null : TouchInput.GetTouchContactByFingerId( m_FingerID, touches );

            bool bFailed = (tc == null);
            if( !bFailed )
            {
                if( tc.deltaPosition != Vector2.Zero )
                {
                    m_TouchDeltaList.Add(tc.deltaPosition);
                }
                
                Vector2 refDeltaPos = tc.position - m_SwipeRefPosition;
                float refLengthSqr = refDeltaPos.LengthSquared();

                if (refLengthSqr > k_SwipeDeadZone * k_SwipeDeadZone)
                {
                    //Calculate current average velocity.
                    m_AverageVelocity = Vector2.Zero;
                    if (m_TouchDeltaList.Count > 0)
                    {
                        foreach (Vector2 vel in m_TouchDeltaList)
                        {
                            m_AverageVelocity += vel;
                        }
                        m_AverageVelocity /= m_TouchDeltaList.Count;

                        m_AverageVelocity /= (float)(Time.WallClockTotalSeconds - tc.startTime);
                    }

                    //Velocity Check
                    bFailed |= m_AverageVelocity.LengthSquared() < (k_MinimumSpeed * k_MinimumSpeed);

                    //Debug.WriteLine( "VelocityCheck -> AverageSqr: "+ m_AverageVelocity.LengthSquared() +" | MinimumSqr:"+ (k_MinimumSpeed * k_MinimumSpeed) );

                    if( !bFailed )
                    {
                        //Check if a direction has been specified.  If not assign one for the gesture.
                        if (m_SwipeDirection == Directions.None)
                        {
                            m_SwipeDirection = GetSwipeDirection(Vector2.Normalize(refDeltaPos), k_MaxAveragedDeviationDOT);
                            m_InitialPosition = tc.position;
                        }
                        else
                        {
                            //Ensure deltaPos is in correct swipe direction.
                            bFailed |= m_SwipeDirection != GetSwipeDirection(Vector2.Normalize(refDeltaPos), k_MaxAveragedDeviationDOT);
                            if (!bFailed)
                            {
                                if (refLengthSqr > k_RefPointChangeThreshold * k_RefPointChangeThreshold)
                                {
                                    m_DeadZoneIdleTime = Time.WallClockTotalSeconds;
                                    m_SwipeRefPosition = tc.position;
                                }
                            }
                        }
                        
                    }
                }

                //If we're idling too long in dead zone, fail.
                bFailed |= (Time.WallClockTotalSeconds - m_DeadZoneIdleTime > k_IdleTimeout);
                
            }

            if( bFailed )
            {
                Debug.Assert( !wasRecognized ); //If we recognized this gesture this frame and we're failing too then something is very very wrong.
                SetState(GestureState.Failed);
            }
        }

        protected override void OnTouchPressed(TouchContact[] touches)
        {
            if( touches.Length != GetRequiredTouchCount() )
            {
                SetState(GestureState.Failed);
            }
            else
            {
                Debug.Assert( touches.Length > 0 );
                m_FingerID = touches[0].fingerId;
                m_SwipeRefPosition = m_InitialPosition = touches[0].position;

                m_DeadZoneIdleTime = Time.WallClockTotalSeconds;
            }
        }

        protected override void OnReset()
        {
            //Reset frame after was recognized.
            if( !wasRecognized )
            {
                m_SpeedFactor = 0.0f; 
                m_SwipeDirection = Directions.None;
            }
            
            m_AverageVelocity = Vector2.Zero;
            m_TouchDeltaList.Clear();
            m_FingerID = -1;
        }



         /// <summary>
         /// Extract a swipe direction from a direction vector and a tolerance percent 
         /// </summary>
         /// <param name="dir">The non-constrained direction vector. Must be normalized.</param>
         /// <param name="tolerance">Percentage of tolerance</param>
         /// <returns>The swipe direction</returns>
         public static Directions GetSwipeDirection(Vector2 swipeDir, float tolerance)
         {
             // Check for cardinal directions
             float minCardinalDot = MathHelper.Clamp((1.0f - tolerance), 0.0f, 1.0f);
 
             if (Vector2.Dot(swipeDir, Vector2.UnitX) >= minCardinalDot)
                 return Directions.East;
 
             if (Vector2.Dot(swipeDir, -Vector2.UnitX) >= minCardinalDot)
                 return Directions.West;
 
             if (Vector2.Dot(swipeDir, Vector2.UnitY) >= minCardinalDot)
                 return Directions.South;
 
             if (Vector2.Dot(swipeDir, -Vector2.UnitY) >= minCardinalDot)
                 return Directions.North;
 
             // Check for intercardinal directions
             float minIntercardinalDot = MathHelper.Clamp((1.0f - 0.5f), 0.0f, 1.0f);
 
             if (Vector2.Dot(swipeDir, Vector2.UnitX) >= minIntercardinalDot)
             {
                 if (Vector2.Dot(swipeDir, -Vector2.UnitY) >= minIntercardinalDot)
                 {
                     return Directions.East | Directions.North;
                 }
                 else if (Vector2.Dot(swipeDir, Vector2.UnitY) >= minIntercardinalDot)
                 {
                     return Directions.East | Directions.South;
                 }
             }
             else if (Vector2.Dot(swipeDir, -Vector2.UnitX) >= minIntercardinalDot)
             {
                 if (Vector2.Dot(swipeDir, -Vector2.UnitY) >= minIntercardinalDot)
                 {
                     return Directions.West | Directions.North;
                 }
                 else if (Vector2.Dot(swipeDir, Vector2.UnitY) >= minIntercardinalDot)
                 {
                     return Directions.West | Directions.South;
                 }
             }
 
             return Directions.None;
         }

         private void ComputeSpeedFactor()
         {
             float travelFactor = m_AverageVelocity.Length(); // how far along the screen the user has travelled
             
             travelFactor /= (SwipeDirection == Directions.North || SwipeDirection == Directions.South) ? BokuGame.ScreenSize.Y : BokuGame.ScreenSize.X;
 
             travelFactor = MathHelper.Clamp(travelFactor, MIN_TRAVEL_FACTOR, MAX_TRAVEL_FACTOR);
             
             m_SpeedFactor = travelFactor / MAX_TRAVEL_FACTOR;
         }
        
    }
}
