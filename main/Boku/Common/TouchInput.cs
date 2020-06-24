using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Microsoft.Xna.Framework;

#if !NETFX_CORE
    using TouchHook;
#endif

using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input.Touch;

using KoiX;

using Boku.Common.Xml;
using Boku.Common.Gesture;
using System.Diagnostics;
#if !NETFX_CORE
    using System.Windows.Forms;
#endif


namespace Boku.Common 
{
    /// <summary>
    /// Singleton wrapper for touch input.
    /// </summary>
    public static class TouchInput
    {
        #region Members
        // Amount of time a contact must remain unchanged before it is called stationary
        private const float STATIONARY_TIME = 0.05f;

        // Contains the list of touches that are exposed to queries from the application.
        // This list is populated during the Update() by the "touches" list.
        private static List<TouchContact> touchContacts = new List<TouchContact>();

        private static bool multiTouchEnabled = false;

        private static int maxTouchCount;
        public static int MaxTouchCount
        {
            get { return maxTouchCount; }
            set { maxTouchCount = value; }
        }

        private static bool touchAvailable;
        public static bool TouchAvailable
        {
            get { return touchAvailable; }
            set { touchAvailable = value; }
        }
        /// <summary>
        /// Were any touches captured this frame that moved 
        /// </summary>
        private static bool wasMoved = false;

        /// <summary>
        /// Return true if any touches are in contact
        /// </summary>
        private static bool isTouched = false;

        /// <summary>
        /// Return true if any new touches were captured *this* frame
        /// </summary>
        private static bool wasTouched = false;

        /// <summary>
        /// Return true if any multi touch frames were seen since the last no touch frame
        /// </summary>
        private static bool wasMultiTouch = false;

        /// <summary>
        /// Return true if any touches were released *this* frame
        /// </summary>
        private static bool wasReleased = false;

        /// <summary>
        /// This field is filled in if the touch sequence BEGAN on an actor.
        /// This is useful information for gestures such as Drag, which 
        /// need to know what character the user began the movement on, not where they are now.
        /// </summary>
        private static Base.GameActor initialActorHit = null;

        #endregion Members

        #region Accessors

        /// <summary>
        /// Returns array of structs representing status of all touches during last frame
        /// (Read only, allocates new array)
        /// </summary>
        public static TouchContact[] Touches
        {
            get { return touchContacts.ToArray(); }
        }

        /// <summary>
        /// Property indicating whether the system handles multiple touches.
        /// </summary>
        public static bool MultiTouchEnabled
        {
            get { return multiTouchEnabled; }
        }

        /// <summary>
        /// Number of touches. Guaranteed not to change throughout the frame. (Read Only).
        /// </summary>
        public static int TouchCount
        {
            get { return touchContacts.Count; }
        }

        /// <summary>
        /// Return true if any active touches were captured
        /// </summary>
        public static bool IsTouched
        {
            get { return isTouched; }
        }

        /// <summary>
        /// Return true if any new touches were captured this frame
        /// </summary>
        public static bool WasTouched
        {
            get { return wasTouched; }
        }

        /// <summary>
        /// True if the first touch was just detected this frame
        /// </summary>
        public static bool WasFirstTouch
        {
            get { return ((TouchCount == 1) && wasTouched); }
        }

        /// <summary>
        /// True if we've seen a multi touch frame in since touch last began
        /// </summary>
        public static bool WasMultiTouch
        {
            get { return wasMultiTouch; }
        }

        /// <summary>
        /// Return true if any touches were released this frame
        /// </summary>
        public static bool WasReleased
        {
            get { return wasReleased; }
        }

        /// <summary>
        /// True if the final finger has been released this frame
        /// </summary>
        public static bool WasLastReleased
        {
            get { return ((TouchCount == 1) && wasReleased); }
        }

        /// <summary>
        /// Return true if any touch captured this frame were moved 
        /// </summary>
        public static bool WasMoved
        {
            get { return wasMoved; }
        }

        /// <summary>
        /// This field is filled in if the touch sequence BEGAN on an actor.
        /// This is useful information for gestures such as Drag, which need to know what 
        /// character the user began the movement on, not where they are now.
        /// </summary>
        public static Base.GameActor InitialActorHit
        {
            get { return initialActorHit; }
        }

        /// <summary>
        /// This is called by TouchEdit once it has filled in its actorHit data for this frame.
        /// This is better than a direct accessor and keeps this field protected so that
        /// it is only modified on WasTouched.
        /// </summary>
        public static void SetInitialActorHit()
        {
            if (wasTouched)
            {
                initialActorHit = TouchEdit.MouseTouchHitInfo.ActorHit;
            }
        }

        #endregion Accessors

        #region Public
        public static void Init()
        {
#if NETFX_CORE
#else
            Input.Init();
#endif
        }

        //static private bool skipShow = false;

        public static void Update()
        {
            //if (BokuGame.bokuGame.IsActive)
            {
#if NETFX_CORE
                Touch[] touchesThisFrame = AltGetTouchContacts();
#else
                //----------------------------------------------------------------------------------
                // This is a stub until Unity support is added. Unity doesn't need its update called
                //----------------------------------------------------------------------------------
                Input.Update();
                //----------------------------------------------------------------------------------
                // Please remove the above hack when Unity is integrated
                //----------------------------------------------------------------------------------

                Touch[] touchesThisFrame = Input.touches; //From touchesThisFrame list in UnityTouchEmulation.
#endif

                // Sort the array of Touch objects on the fingerId
                Array.Sort(touchesThisFrame, delegate(Touch touchZero, Touch touchOne)
                {
                    return touchZero.fingerId.CompareTo(touchOne.fingerId);
                });

                // Initially set it to nothing is touching the screen
                isTouched = false;
                wasMoved = false;
                wasTouched = false;

                if (touchesThisFrame.Length <= 0)
                {
                    wasMultiTouch = false;
                }
                else if (touchesThisFrame.Length>1)
                {
                    //a single frame of multi touch means multi touch was detected, don't reset until no data
                    wasMultiTouch = true;
                }


                // Assume we just released until proven false below
                wasReleased = true;

                // If there are a different number of touches than touch contacts, we either added
                // or deleted some contacts.
                int touchContactsLength = touchContacts.Count;
                int touchesThisFrameLength = touchesThisFrame.Length;

                if (touchContacts.Count != touchesThisFrame.Length)
                {
                    //RemoveEndedTouchContacts();

                    // A touch was added, or multiple touches
                    if (touchesThisFrame.Length > touchContacts.Count)
                    {
                        wasTouched = true;
                        for (int i = 0; i < touchesThisFrame.Length; ++i)
                        {
                            Touch t = touchesThisFrame[i];
                            TouchContact contact = GetTouchContactByFingerId(t.fingerId);
                            if (contact == null)
                            {
                                contact = new TouchContact();
                                contact.fingerId = t.fingerId;
                                touchContacts.Add(contact);
                            }
                        }
                    }
                    else // A touch was deleted, or multiple touches
                    {
                        List<TouchContact> temp = new List<TouchContact>();

                        for (int i = 0; i < touchesThisFrame.Length; ++i)
                        {
                            Touch t = touchesThisFrame[i];
                            TouchContact contact = GetTouchContactByFingerId(t.fingerId);
                            if (contact != null)
                            {
                                //search for existing touch with this id
                                TouchContact existT = temp.Find(delegate(TouchContact p)
                                {
                                    return p.fingerId == contact.fingerId;
                                });
                                
                                if(existT==null )
                                    temp.Add(contact);
                            }
                        }
                        touchContacts = temp;
                    }
                }

                // Now that the number of TouchContact's is correct, lets sort them on the fingerId
                touchContacts.Sort(delegate(TouchContact contactZero, TouchContact contactOne)
                {
                    return contactZero.fingerId.CompareTo(contactOne.fingerId);
                });

                //string message = "";
                if (touchContacts.Count != touchesThisFrame.Length)
                {
                    /*
                    Debug.Assert((touchContacts.Count == touchesThisFrame.Length), message +
                        "old:\n" + oldTouchesThisFrameString + oldTouchContactsString +
                        "new:\n" + newTouchesThisFrameString + newTouchContactsString);
                     */

#if !NETFX_CORE
                    Input.ClearEvents();
#endif
                    touchContacts.Clear();
                    wasReleased = false;
                    return;

                }

                // Perform a deep copy of each touch object
                for (int i = 0; i < touchesThisFrame.Length; ++i)
                {
                    Touch touch = touchesThisFrame[i];
                    TouchContact touchContact = touchContacts[i];

                    switch (touch.phase)
                    {
                        case TouchPhase.Began:
                            //Console.WriteLine("New TouchContact");
                            touchContact.phase = TouchPhase.Began;
                            touchContact.startPosition = touch.position;
                            touchContact.previousPosition = touch.position;
                            touchContact.startTime = Time.WallClockTotalSeconds;
                            touchContact.elapsedTime = Time.WallClockFrameSeconds;
                            break;
                        case TouchPhase.Moved:
                        case TouchPhase.Stationary:
                            // JW - NOTE: Even when the Unity layer reports a touch as stationary once the
                            // delta position is unchanged for one frame, that is not a very accurate measure.
                            // We won't set TouchContact objects to be stationary until we've detected no 
                            // delta position change for STATIONARY_TIME seconds.
                            if (touch.deltaPosition == Vector2.Zero)
                            {
                                if ((Time.WallClockTotalSeconds - touchContact.lastMoveTime) > STATIONARY_TIME)
                                {
                                    touchContact.phase = TouchPhase.Stationary;
                                }
                            }
                            else
                            {
                                touchContact.lastMoveTime = Time.WallClockTotalSeconds;
                                touchContact.phase = TouchPhase.Moved;
                             }
                            touchContact.previousPosition = touchContact.position;
                            touchContact.elapsedTime = Time.WallClockFrameSeconds;
                            wasMoved = true;
                            break;
                        case TouchPhase.Ended:
                            //Console.WriteLine("TouchContact has ended");
                            touchContact.previousPosition = touchContact.position;
                            touchContact.phase = TouchPhase.Ended;
                            touchContact.elapsedTime = Time.WallClockFrameSeconds;
                            break;
                        default:
                            break;
                    }

                    if (touchContact.phase != TouchPhase.Ended)
                    {
                        isTouched = true;
                        wasReleased = false;
                    }

                    touchContact.position = touch.position;
                    touchContact.deltaPosition = touch.deltaPosition;
                    touchContact.fingerId = touch.fingerId;
                    //touchContact.tapCount = touch.tapCount;
                    touchContact.deltaTime = touch.deltaTime;
                    if ( touchContact.elapsedTime > 0.0f )
                        touchContact.Velocity = (touchContact.position - touchContact.previousPosition) / touchContact.elapsedTime;
                    else
                        touchContact.Velocity = Vector2.Zero;
                }

                // If we have no touch objects, then we are at least one frame after the last touch ended
                // so we are not released.
                if (touchContacts.Count == 0)
                {
                    wasReleased = false;
                }
            }

            TouchGestureManager.Get().Update();
        }

        // Previous frame's touch list.  Used for calculating deltaPosition.
        static List<Touch> prevTouchList = null;

        /// <summary>
        /// Alternative way to get contacts.  Instead of using TouchHook
        /// this uses the XNA touch support.
        /// </summary>
        public static Touch[] AltGetTouchContacts()
        {
#if DEBUG
            TouchPanelCapabilities caps = TouchPanel.GetCapabilities();
            Debug.Assert(caps.IsConnected, "Should be a touch panel?!?");
#endif

            // Get the contacts.
            TouchCollection collection = TouchPanel.GetState();

            // Translate the contacts into an array of Touch.
            List<Touch> touchList = new List<Touch>();
            foreach (TouchLocation tl in collection)
            {
                // Filter out Invalid ones.  No clue what to do with them anyway...
                if (tl.State != TouchLocationState.Invalid)
                {
                    Touch touch = new Touch();
                    touch.position = tl.Position;

                    // Set position with offset for Tutorial mode.
                    touch.position = touch.position - BokuGame.ScreenPosition;

                    touch.fingerId = tl.Id;
                    touch.phase = TouchPhase.Stationary;
                    switch(tl.State)
                    {
                        case TouchLocationState.Moved:
                            touch.phase = TouchPhase.Moved;
                            break;
                        case TouchLocationState.Pressed:
                            touch.phase = TouchPhase.Began;
                            break;
                        case TouchLocationState.Released:
                            touch.phase = TouchPhase.Ended;
                            break;
                    }
                    touch.deltaTime = Time.WallClockFrameSeconds;

                    // Look for matching Id to calc deltaPosition.
                    touch.deltaPosition = Vector2.Zero;
                    if (prevTouchList != null)
                    {
                        foreach (Touch t in prevTouchList)
                        {
                            if (t.fingerId == touch.fingerId)
                            {
                                touch.deltaPosition = touch.position - t.position;
                            }
                        }
                    }

                    touchList.Add(touch);
                }
            }   // end of loop over collection

            // Save a reference to this list for next frame.
            prevTouchList = touchList;

            return touchList.ToArray();

        }   // end of AltGetTouchContacts()

        public static TouchContact GetTouchContactByIndex(int touchIndex)
        {
            if (touchIndex < 0 || touchIndex >= touchContacts.Count)
            {
                return null;
            }
            return touchContacts[touchIndex];
        }

        public static TouchContact GetTouchContactByFingerId(int fingerId)
        {
            return GetTouchContactByFingerId(fingerId, touchContacts.ToArray());
        }

        public static TouchContact GetTouchContactByFingerId( int fingerId, TouchContact[] touches )
        {
            TouchContact found = null;
            for (int i = 0; i < touches.Length; ++i)
            {
                if (fingerId == touches[i].fingerId)
                {
                    found = touches[i];
                    break;
                }
            }
            return found;
        }

        public static void RemoveEndedTouchContacts()
        {
            for (int i = 0; i <touchContacts.Count ; ++i)
            {
                TouchContact tc=touchContacts[i];
                if (tc.phase==TouchPhase.Ended)
                {
                    touchContacts.Remove(tc);
                    --i;
                    continue;
                }
            }
        }

        /// <summary>
        /// Returns the touch position but adjusted for using a rt that has
        /// a different aspect ratio than the window being rendered into.
        /// </summary>
        /// <param name="touchPosition">Position of touch input to return the adjusted position</param>
        /// <param name="useOverscan">Also take into account the overscan setting.</param>
        public static Vector2 GetAspectRatioAdjustedPosition(Vector2 touchPosition, Camera camera, bool useOverscan)
        {
            return GetAspectRatioAdjustedPosition(touchPosition, camera, useOverscan, false);
        }

        /// <summary>
        /// Returns the toudh position but adjusted for using a rt that has
        /// a different aspect ratio than the window being rendered into.
        /// </summary>
        /// <param name="touchPosition">Position of touch input to return the adjusted position</param>
        /// <param name="useOverscan">Also take into account the overscan setting.</param>
        /// <param name="ignoreScreenPosition">Needed for tutorial modal display.</param>
        public static Vector2 GetAspectRatioAdjustedPosition(Vector2 touchPosition, Camera camera, bool useOverscan, bool ignoreScreenPosition)
        {
            if (!ignoreScreenPosition)
            {
                touchPosition -= BokuGame.ScreenPosition;
            }

            GraphicsDevice device = KoiLibrary.GraphicsDevice;
            Vector2 winRes = BokuGame.ScreenSize;
            if (ignoreScreenPosition)
            {
                winRes = new Vector2(KoiLibrary.GraphicsDevice.Viewport.Width, KoiLibrary.GraphicsDevice.Viewport.Height);
            }

            Vector2 rtRes = new Vector2(camera.Resolution.X, camera.Resolution.Y);

            // Transform mouse coords to be 0,0 at center of screen.
            touchPosition -= winRes / 2.0f;

            // Scale to adjust for vertical res difference.
            touchPosition *= rtRes.Y / winRes.Y;

            // Transform coords back out to having 0,0 in the upper left.  This time using the rt size.
            touchPosition += rtRes / 2.0f;

            return touchPosition;
        }   // end of GetAspectRatioAdjustedPosition()

        /// <summary>
        /// Used to get proper amount of scolling relative to drag.
        /// </summary>
        /// <param name="camera"></param>
        /// <returns></returns>
        public static Vector2 GetWinRTRatio(Camera camera)
        {
            Vector2 winRes = new Vector2(KoiLibrary.GraphicsDevice.Viewport.Width, KoiLibrary.GraphicsDevice.Viewport.Height);
            Vector2 rtRes = new Vector2(camera.Resolution.X, camera.Resolution.Y);

            return winRes / rtRes;
        }   // end of GetWinRTRatio()

        /// <summary>
        /// Helper function for touch selection of standard 2d UI elements.
        /// </summary>
        /// <param name="touchPosition">Position to test against</param>
        /// <param name="camera">Current camera.</param>
        /// <param name="invWorldMatrix">Inverse of element's world matrix.</param>
        /// <param name="width">Width of UI element in world units.</param>
        /// <param name="height">Height of UI element in world units.</param>
        /// <param name="useRtCoords">Assumes rendering to a rendertarget rather than the backbuffer.</param>
        /// <returns>UV coord of mouse hit. Should be in [0, 1][0, 1] range.  Outside of this implies a miss.</returns>
        public static Vector2 GetHitUV(Vector2 touchPosition, Camera camera, ref Matrix invWorldMatrix, float width, float height, bool useRtCoords)
        {
            Vector2 adjustedTouchPosition = touchPosition;

            if (useRtCoords)
            {
                adjustedTouchPosition = ScreenWarp.ScreenToRT(adjustedTouchPosition);
            }

            // Get 3D direction for mouse position.
            Vector3 touchDir = camera.ScreenToWorldCoords(adjustedTouchPosition);

            // Transform mouse ray into local space.
            Vector3 position = Vector3.Transform(camera.ActualFrom, invWorldMatrix);
            Vector3 direction = Vector3.TransformNormal(touchDir, invWorldMatrix);

            // Project to Z==0 plane, calc hit in local units (origin in center).
            float dist = -position.Z / direction.Z;
            Vector3 hit = position + direction * dist;

            Target = hit - invWorldMatrix.Translation;

            Vector2 hitUV = new Vector2(hit.X / width + 0.5f, -hit.Y / height + 0.5f);

            return hitUV;
        }   // end of GetHitUV

        /// <summary>
        /// Helper function for touch selection of standard 2d UI elements.
        /// This function takes a normalized coordinate position, a camera and the inverse world matrix
        /// of the UI element, and returns the local space XY for the element. The assumption is made that
        /// the UI elements are positioned at Z=0 in view space. The assumption is also made that overscan
        /// is not compensated for.
        /// </summary>
        /// <param name="screenCoords">Position to test against</param>
        /// <param name="camera">Current camera.</param>
        /// <param name="invWorldMatrix">Inverse of element's world matrix.</param>
        /// <returns>XY coordinate in local space based on a ray projected from the camera to the element.</returns>
        public static Vector2 GetLocalXYFromScreenCoords(Vector2 screenCoords, Camera camera, ref Matrix invWorldMatrix)
        {
            Vector2 adjustedTouchPosition = GetAspectRatioAdjustedPosition(screenCoords, camera, false);

            // Get 3D direction for mouse position.
            Vector3 touchDir = camera.ScreenToWorldCoords(adjustedTouchPosition);

            // Transform mouse ray into local space.
            Vector3 position = Vector3.Transform(camera.ActualFrom, invWorldMatrix);
            Vector3 direction = Vector3.TransformNormal(touchDir, invWorldMatrix);

            // Project to Z==0 plane, calc hit in local units (origin in center).
            float dist = -position.Z / direction.Z;
            Vector3 hit = position + direction * dist;

            Target = hit - invWorldMatrix.Translation;
            return new Vector2(hit.X, hit.Y);
        }

        /// <summary>
        /// Returns it in world coords of ray through touch pixel.
        /// </summary>
        /// <param name=param name="touchPosition">Position to test against</param>
        /// <param name="camera">UiCamera (orthographic)</param>
        /// <param name="invWorldMatrix">Inverse of object's world matrix.</param>
        /// <param name="useRtCoords">Adjust hit for rendering to a render target?</param>
        /// <returns></returns>
        public static Vector2 GetHitOrtho(Vector2 touchPosition, UiCamera camera, ref Matrix invWorldMatrix, bool useRtCoords)
        {
            Vector2 adjustedTouchPosition = touchPosition;

            if (useRtCoords)
            {
                adjustedTouchPosition = ScreenWarp.ScreenToRT(adjustedTouchPosition);
            }

            Vector2 hit = Vector2.Zero;

            // Convert from pixels to world units.
            hit.X = adjustedTouchPosition.X * camera.Width / camera.Resolution.X;
            hit.Y = adjustedTouchPosition.Y * camera.Height / camera.Resolution.Y;

            // Put origin at center.
            hit.X -= camera.Width / 2.0f;
            hit.Y -= camera.Height / 2.0f;

            // Flip vertical axis.
            hit.Y = -hit.Y;

            Vector3 actualFrom = camera.ActualFrom;
            Vector3 actualAt = camera.ActualAt;

            // Apply object and camera translation
            hit.X += invWorldMatrix.Translation.X + actualAt.X + camera.Offset.X;
            hit.Y += invWorldMatrix.Translation.Y + actualAt.Y + camera.Offset.Y;

            // Adjust if camera not going along Z axis.
            hit.X *= (float)Math.Cos(Math.Atan(camera.ViewDir.X / camera.ViewDir.Z));
            hit.Y *= (float)Math.Cos(Math.Atan(camera.ViewDir.Y / camera.ViewDir.Z));

            // Need to adjust if object is not at z==0.
            if (invWorldMatrix.Translation.Z != 0)
            {
                float fraction = invWorldMatrix.Translation.Z / (actualFrom.Z - actualAt.Z);
                hit.X -= (actualFrom.X - actualAt.X) * fraction;
                hit.Y -= (actualFrom.Y - actualAt.Y) * fraction;
            }

            return hit;
        }   // end of GetHitOrtho()

        /// <summary>
        /// Debug helper...
        /// </summary>
        static public Vector3 Target;
        #endregion Public

        private static Vector2 ScreenToClient(int x, int y)
        {
#if NETFX_CORE
            Point screenPos = BokuGame.bokuGame.Window.ClientBounds.Location;
#else
            Point screenPos = new Point(XNAControl.Instance.ClientRectangle.Location.X, XNAControl.Instance.ClientRectangle.Location.Y);
#endif
            return new Vector2(x - screenPos.X, y - screenPos.Y);
        }

        public static Point GetAsPoint(Vector2 vec)
        {
            return new Point((int)vec.X, (int)vec.Y);
        }

        public static TouchContact GetOldestTouch()
        {
            TouchContact[] touches = TouchInput.Touches;
            return GetOldestTouch(ref touches);
        }

        public static TouchContact GetOldestTouch(ref TouchContact[] touches)
        {
            double oldestTime = double.MaxValue;
            int index = -1;
            for (int i = 0; i < touches.Length; ++i)
            {
                if (touches[i].startTime <= oldestTime)
                {
                    oldestTime = touches[i].startTime;
                    index = i;
                }
            }
            if (index != -1)
            {
                return touches[index];
            }
            return null;
        }


        public static TouchContact GetNewestTouch()
        {
            TouchContact[] touches = TouchInput.Touches;
            return GetNewestTouch(ref touches);
        }

        public static TouchContact GetNewestTouch(ref TouchContact[] touches)
        {
            double newestTime = 0;
            int index = -1;
            for (int i = 0; i < touches.Length; ++i)
            {
                if (touches[i].startTime >= newestTime)
                {
                    newestTime = touches[i].startTime;
                    index = i;
                }
            }
            if (index != -1)
            {
                return touches[index];
            }
            return null;
        }
    }

    public class TouchContact
    {
        /// <summary>
        /// These are a series of getters and setters where all the setters are marked internal. This allows data to be read-only to external sources.
        /// </summary>
        private int mFingerId;
        public int fingerId
        {
            get { return mFingerId; }
            internal set { mFingerId = value; }
        }
        private Vector2 mPosition;
        public Vector2 position
        {
            get { return mPosition; }
            internal set { mPosition = value; }
        }
        private Vector2 mDeltaPosition;
        public Vector2 deltaPosition
        {
            get { return mDeltaPosition; }
            internal set { mDeltaPosition = value; }
        }

        private int mTapCount;
        public int tapCount
        {
            get { return mTapCount; }
            internal set { mTapCount = value; }
        }
        private TouchPhase mPhase;
        public TouchPhase phase
        {
            get { return mPhase; }
            internal set { mPhase = value; }
        }
        private Vector2 mStartPosition;
        public Vector2 startPosition
        {
            get { return mStartPosition; }
            internal set { mStartPosition = value; }
        }
        private Vector2 mPreviousPosition;
        public Vector2 previousPosition
        {
            get { return mPreviousPosition; }
            internal set { mPreviousPosition = value; }
        }
        private double mStartTime;
        public double startTime
        {
            get { return mStartTime; }
            internal set { mStartTime = value; }
        }

        /// <summary>
        /// This records the wallclock time that this touch last reported an actual movement
        /// (i.e. a non-zero deltaPosition)
        /// </summary>
        private double mLastMoveTime;
        public double lastMoveTime
        {
            get { return mLastMoveTime; }
            internal set { mLastMoveTime = value; }
        }

        internal double timeOfLastChange;
        private float timeDelta;
        public float deltaTime
        {
            get { return timeDelta; }
            internal set { timeDelta = value; }
        }

        internal bool delayedEnd;

        private Vector2 velocity; //how fast is this touch moving (pixels/second)
        public float elapsedTime;
        public Vector2 Velocity
        {
            get { return velocity; }
            set { velocity = value; }
        }
        // This is the object that was touched. Should only be activated if the user
        // ends the touch still over this object. Note that the frame after the touch 
        // is ended, this is cleared.
        private object mTouchedObject;
        public object TouchedObject
        {
            get { return mTouchedObject; }
            set
            {
                if (value == null)
                {
                    mTouchedObject = null;
                }
                mTouchedObject = value;
            }
        }

        /// <summary>
        /// Constructor!! Default initializes those values we care about.
        /// </summary>
        public TouchContact()
        {
            position = new Vector2();
            deltaPosition = new Vector2();
            timeDelta = 0.0f;
            tapCount = 0;
            phase = TouchPhase.Ended;
            lastMoveTime = 0.0f;

            timeOfLastChange = 0.0;
            delayedEnd = false;
            TouchedObject = null;
            startTime = Time.GameTimeTotalSeconds; //PV test

            elapsedTime = Time.WallClockFrameSeconds;
            velocity = new Vector2();
        }

        internal void UpdateDeltaTime(double wallClock)
        {
            timeDelta = (float)(wallClock - timeOfLastChange);
        }

        internal void UpdateTimeOfChange(double wallClock)
        {
            timeDelta = 0.0f;
            timeOfLastChange = wallClock;
        }
    }

}
