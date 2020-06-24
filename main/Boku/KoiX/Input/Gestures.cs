
using System;
using System.Collections;
using System.Collections.Generic;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Input.Touch;
using Microsoft.Xna.Framework.Storage;
using Microsoft.Xna.Framework.Content;

using System.Diagnostics;

using KoiX;

namespace KoiX.Input
{
    /// <summary>
    /// Static class that calculates gestures from raw touch events.
    /// </summary>
    public static class Gestures
    {
        static double kTapTime = 0.3;           // Time must be shorter than this to count as a Tap.
                                                // Longer than this counts as a Hold.
        static double kDoubleTapTime = 0.3f;    // Time after first tap ends that next tap must 
                                                // start to be considered a double tap.
        static float kDragDistance = 20.0f;     // Min dist to be considered a drag.
                                                // Note:  In best of all possible worlds this would
                                                // be in inches rather than pixels to take into account
                                                // screen dpi.

        // States for the gesture recognizer state machine.
        enum State
        {
            None,
            OnePoint,
            OnePointDrag,
            TwoPoint,
            TwoPointDrag,
        }

        public class Touch
        {
            public TouchSample Sample;
            public bool Valid = false;
            public double StartTime;
            public int Frame;
            public Vector2 StartPosition;
            public Vector2 Position;
            public Vector2 DeltaPosition;

            public int Id
            {
                get 
                { 
                    return Sample != null ? Sample.Id : -1; 
                }
            }

            public void Init(TouchSample sample)
            {
                Sample = sample;
                Valid = true;
                StartTime = Time.GameTimeTotalSeconds;
                Frame = Time.FrameCounter;
                StartPosition = sample.Position;
                Position = sample.Position;
            }
        }

        #region Members

        static Touch touch0 = new Touch();  // Allocate touches up front.
        static Touch touch1 = new Touch();

        static State state = State.None;
        static double prevTapTime = 0;  // Time the previous tap ended for detecting double taps.
        static int prevTapFrame = 0;    // Frame the previous tap ended.  Helps when frame rate is low.

        /// <summary>
        /// Intermediate values associated with TwoPointDrag.
        /// </summary>
        static float startDist;
        static float curDist;
        static float deltaDist;
        static float startAngle;
        static float curAngle;
        static float deltaAngle;

        static bool twoPointMoved = false;  // Flag cleared each frame and set when a 2-point move event is created.
                                            // This is then used to prevent a second event firing on the same frame.
        static bool wasTwoPoint = false;    // Set once we're in two point drag mode andl ceared when back to none.
                                            // This prevents one point gestures from being reported at the tail of a
                                            // two point.

        #endregion

        #region Accessors
        #endregion

        #region Public

        /// <summary>
        /// Takes the raw set of touch locations and combines them with any
        /// previous state to determine which, if any, gestures have occurred.
        /// </summary>
        /// <param name="touchSampleList">List of TouchSamples for this frame.</param>
        public static void ProcessTouchSamples(List<TouchSample> touchSampleList)
        {
            if(touchSampleList.Count > 0)
            {
                //Debug.WriteLine("========" + state.ToString());
            }

            // Update the touch values first and then process for gestures.
            // This is mostly important for the TwoPointDrag where we only
            // want to send one drag event per frame.
            foreach (TouchSample ts in touchSampleList)
            {
                // Ignore invalid touches.
                if (ts.State == TouchLocationState.Invalid)
                    continue;

                // Update touch positions and deltas.
                if (touch0.Valid && touch0.Id == ts.Id)
                {
                    touch0.DeltaPosition = ts.Position - touch0.Position;
                    touch0.Position = ts.Position;
                }
                if (touch1.Valid && touch1.Id == ts.Id)
                {
                    touch1.DeltaPosition = ts.Position - touch1.Position;
                    touch1.Position = ts.Position;
                }
            }

            // Update values associated with TwoPoint if both touches are valid.
            if (touch0.Valid && touch1.Valid)
            {
                float prevDist = curDist;
                curDist = (touch0.Position - touch1.Position).Length();
                deltaDist = curDist - prevDist;

                float prevAngle = curAngle;
                curAngle = MyMath.RotationFromDirection(touch0.Position - touch1.Position);
                deltaAngle = curAngle - prevAngle;
                deltaAngle = MathHelper.WrapAngle(deltaAngle);
            }

            twoPointMoved = false;

            // Loop over collection a second time recognizing gestures.
            foreach (TouchSample ts in touchSampleList)
            {
                // Ignore invalid touches.
                if (ts.State == TouchLocationState.Invalid)
                    continue;

                //Debug.WriteLine("  id " + tl.Id + " " + tl.State.ToString());

                switch (state)
                {
                    case State.None:
                        {
                            Debug.Assert(!touch0.Valid, "In State.None, why is this valid?");
                            Debug.Assert(!touch1.Valid, "In State.None, why is this valid?");
                            if (touch0.Valid || touch1.Valid)
                            {
                                Debug.Assert(false, "huh?");
                            }

                            // Normally we will only see Pressed here BUT in the user is rapidly
                            // banging on the screen we can miss it.  So, if we get a Move when
                            // both touch points are not valid, treat it as a Pressed.
                            if (ts.State == TouchLocationState.Pressed || ts.State == TouchLocationState.Moved)
                            {
                                //Debug.WriteLine("  init touch0 id " + tl.Id);
                                //Debug.WriteLine("    switch to OnePoint");
                                touch0.Init(ts);
                                state = State.OnePoint;
                            }
                            else
                            {
                                Debug.Assert(false, "huh?");
                            }

                            wasTwoPoint = false;
                        }
                        break;
                    case State.OnePoint:
                        {
                            // If id matches, it should be a move or release.
                            if (touch0.Id == ts.Id)
                            {
                                // Sometimes the Press doesn't get hit tested so we miss the HitObject.
                                // Not sure why this fails.
                                // TODO (****) Figure out why some Press events don't get tested.
                                if (touch0.Sample.HitObject == null)
                                {
                                    touch0.Sample.HitObject = ts.HitObject;
                                }

                                if (ts.State == TouchLocationState.Released)
                                {
                                    // Short enough to be considered a tap?  Also look at frame count so that
                                    // if we're running slow it still comes out right.
                                    if (Time.GameTimeTotalSeconds - touch0.StartTime < kTapTime || touch0.Frame + 2 >= Time.FrameCounter)
                                    {
                                        // Don't send events if wasTwoPoint is true.
                                        if (!wasTwoPoint)
                                        {
                                            // Check if prev tap was recent enough so this counts as a double tap.
                                            // TODO Should double taps be required to be close to each other in space as well as time?
                                            if (touch0.StartTime - prevTapTime < kDoubleTapTime || prevTapFrame + 2 >= Time.FrameCounter)
                                            {
                                                TapGestureEventArgs gesture = new TapGestureEventArgs(GestureType.DoubleTap, touch0);
                                                KoiLibrary.InputEventManager.ProcessTouchDoubleTapEvent(gesture);
                                            }
                                            else
                                            {
                                                TapGestureEventArgs gesture = new TapGestureEventArgs(GestureType.Tap, touch0);
                                                KoiLibrary.InputEventManager.ProcessTouchTapEvent(gesture);
                                            }
                                        }
                                        // Reset prev tap time in either case.
                                        prevTapTime = Time.GameTimeTotalSeconds;
                                        prevTapFrame = Time.FrameCounter;
                                    }
                                    else
                                    {
                                        // Don't send events if wasTwoPoint is true.
                                        if (!wasTwoPoint)
                                        {
                                            // Not a tap, then counts as a hold.
                                            TapGestureEventArgs gesture = new TapGestureEventArgs(GestureType.Hold, touch0);
                                            KoiLibrary.InputEventManager.ProcessTouchHoldEvent(gesture);
                                        }
                                    }

                                    touch0.Valid = false;
                                    state = State.None;
                                }
                                else if (ts.State == TouchLocationState.Moved)
                                {
                                    // Don't send events or switch to OnePointDrag if wasTwoPoint is true.
                                    if (!wasTwoPoint)
                                    {
                                        // Have we moved far enough to go into drag mode?
                                        float dist = (ts.Position - touch0.StartPosition).Length();
                                        if (dist > kDragDistance)
                                        {
                                            OnePointDragGestureEventArgs gesture = new OnePointDragGestureEventArgs(GestureType.OnePointDragBegin, touch0);
                                            KoiLibrary.InputEventManager.ProcessTouchOnePointDragEvent(gesture);
                                            state = State.OnePointDrag;
                                        }
                                        else
                                        {
                                            // Haven't gone far enough to count as a drag.
                                            // Nothing to do here.
                                        }
                                    }
                                }
                                else
                                {
                                    Debug.Assert(false, "huh?");
                                }
                            }
                            else
                            {
                                // Must be a new, second press.
                                // Normally we will only see Pressed here BUT in the user is rapidly
                                // banging on the screen we can miss it.  So, if we get a Move when
                                // both touch points are not valid, treat it as a Pressed.
                                if (ts.State == TouchLocationState.Pressed || ts.State == TouchLocationState.Moved)
                                {
                                    touch1.Init(ts);

                                    // Init starting values.
                                    startDist = (touch0.StartPosition - touch1.StartPosition).Length();
                                    curDist = startDist;
                                    deltaDist = 0;
                                    startAngle = MyMath.RotationFromDirection(touch0.StartPosition - touch1.StartPosition);
                                    curAngle = startAngle;
                                    deltaAngle = 0;

                                    //Debug.WriteLine("  init touch1 id " + tl.Id);
                                    //Debug.WriteLine("    switch to TwoPoint");

                                    state = State.TwoPoint;
                                }
                                else
                                {
                                    // We can accidentally get here if user is moving/pressing quickly.
                                    Debug.Assert(false, "huh?");
                                }
                            }


                        }
                        break;
                    case State.OnePointDrag:
                        {
                            // Once we're in drag mode, ignore any other presses.
                            // Only process those matching touch0's id.
                            if (touch0.Id == ts.Id)
                            {
                                if (ts.State == TouchLocationState.Moved)
                                {
                                    OnePointDragGestureEventArgs gesture = new OnePointDragGestureEventArgs(GestureType.OnePointDrag, touch0);
                                    KoiLibrary.InputEventManager.ProcessTouchOnePointDragEvent(gesture);
                                }
                                else if (ts.State == TouchLocationState.Released)
                                {
                                    OnePointDragGestureEventArgs gesture = new OnePointDragGestureEventArgs(GestureType.OnePointDragEnd, touch0);
                                    KoiLibrary.InputEventManager.ProcessTouchOnePointDragEvent(gesture);
                                    touch0.Valid = false;
                                    state = State.None;
                                }
                                else
                                {
                                    // We can accidentally get here if user is moving/pressing quickly.
                                    Debug.Assert(false, "huh?");
                                }
                            }
                        }
                        break;
                    case State.TwoPoint:
                        {
                            wasTwoPoint = true;

                            // Check for release on touch0.
                            if (touch0.Id == ts.Id && ts.State == TouchLocationState.Released)
                            {
                                // HACK HACK If the user does a very fast double tap it can look
                                // like the presses overlap.  This appears to be caused by the 
                                // fact that for each contact there is always at least 1 Moved
                                // event between the Press and Release.
                                //
                                //              touch0      touch1
                                //  frame n     press
                                //  frame n+1   move        press
                                //  frame n+2   release     move
                                //  frame n+3               release
                                //
                                // We need to detect this case and acto accordingly to get the right results.

                                if (touch0.Frame + 2 == Time.FrameCounter && touch1.Frame == touch0.Frame + 1)
                                {
                                    // Trigger a tap event for touch0.
                                    TapGestureEventArgs gesture = new TapGestureEventArgs(GestureType.Tap, touch0);
                                    KoiLibrary.InputEventManager.ProcessTouchTapEvent(gesture);

                                    // Reset values so we candetect double taps.
                                    prevTapTime = Time.GameTimeTotalSeconds;
                                    prevTapFrame = Time.FrameCounter;

                                    // Clear wasTwoPoint flag since it wasn't really.
                                    wasTwoPoint = false;
                                }

                                // Move touch1 to touch0 and invalidate second touch.
                                Touch tmp = touch0;
                                touch0 = touch1;
                                touch1 = tmp;
                                touch1.Valid = false;

                                //Debug.WriteLine("  touch0 released.  touch1 copied to touch0, touch0 is now id " + touch0.Id.ToString());
                                //Debug.WriteLine("    switch to OnePoint");
                                state = State.OnePoint;
                            }

                            // Check for release on touch1.  Note we have to check for valid again
                            // since the above clause may have invalidated touch1.
                            if (touch1.Valid && touch1.Id == ts.Id && ts.State == TouchLocationState.Released)
                            {
                                touch1.Valid = false;

                                //Debug.WriteLine("  touch0 released.");
                                //Debug.WriteLine("    switch to OnePoint");
                                state = State.OnePoint;
                            }

                            // Check for moved.
                            if (!twoPointMoved && (touch0.Id == ts.Id || touch1.Id == ts.Id) && ts.State == TouchLocationState.Moved)
                            {
                                // Have we moved far enough to go into drag mode?
                                if (curDist > kDragDistance)
                                {
                                    // Switch to drag mode.
                                    TwoPointDragGestureEventArgs gesture = new TwoPointDragGestureEventArgs(GestureType.TwoPointDragBegin, touch0, touch1,
                                                                                                            startDist, curDist, deltaDist,
                                                                                                            startAngle, curAngle, deltaAngle);
                                    KoiLibrary.InputEventManager.ProcessTouchTwoPointDragEvent(gesture);

                                    state = State.TwoPointDrag;
                                }
                                else
                                {
                                    // Haven't gone far enough to count as a drag.
                                    // Nothing to do here.
                                }

                                twoPointMoved = true;
                            }

                        }
                        break;
                    case State.TwoPointDrag:
                        {
                            // Once we're in drag mode, ignore any other presses.
                            // Only process those matching our touch ids.

                            // Check for moved.  Note we're checking both Ids so moving either location can 
                            // trigger this hence the flag checking if we've already sent an event this frame.
                            if (!twoPointMoved && (touch0.Id == ts.Id || touch1.Id == ts.Id) && ts.State == TouchLocationState.Moved)
                            {
                                TwoPointDragGestureEventArgs gesture = new TwoPointDragGestureEventArgs(GestureType.TwoPointDrag, touch0, touch1, 
                                                                                                        startDist, curDist, deltaDist,
                                                                                                        startAngle, curAngle, deltaAngle);

                                if (deltaAngle != 0)
                                {
                                    // Debug.Print(deltaAngle.ToString());
                                }

                                KoiLibrary.InputEventManager.ProcessTouchTwoPointDragEvent(gesture);
                                twoPointMoved = true;
                            }

                            // Check for release on touch0.
                            if (touch0.Id == ts.Id && ts.State == TouchLocationState.Released)
                            {
                                TwoPointDragGestureEventArgs gesture = new TwoPointDragGestureEventArgs(GestureType.TwoPointDragEnd, touch0, touch1,
                                                                                                        startDist, curDist, deltaDist,
                                                                                                        startAngle, curAngle, deltaAngle);
                                KoiLibrary.InputEventManager.ProcessTouchTwoPointDragEvent(gesture);

                                // Move touch1 to touch0 and invalidate second touch.
                                Touch tmp = touch0;
                                touch0 = touch1;
                                touch1 = tmp;
                                touch1.Valid = false;

                                state = State.OnePoint;
                            }

                            // Check for release on touch1
                            if (touch1.Id == ts.Id && ts.State == TouchLocationState.Released)
                            {
                                TwoPointDragGestureEventArgs gesture = new TwoPointDragGestureEventArgs(GestureType.TwoPointDragEnd, touch0, touch1,
                                                                                                        startDist, curDist, deltaDist,
                                                                                                        startAngle, curAngle, deltaAngle);
                                KoiLibrary.InputEventManager.ProcessTouchTwoPointDragEvent(gesture);

                                touch1.Valid = false;

                                state = State.OnePoint;
                            }

                        }
                        break;

                }   // end of switch on state
                
            }   // end of loop over TouchCollection

            if (touchSampleList.Count > 0)
            {
                //Debug.WriteLine("    ====" + state.ToString());
            }


        }   // end of ProcessTouchGestures()

        #endregion

        #region Internal
        #endregion

    }   // end of class Gestures
}   // end of namespace KoiX.Input