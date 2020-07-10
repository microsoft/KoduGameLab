// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.


// Controls whether or not the mouse input is on a seperate thread.
// The idea is to have mouse input be on it's own thread so that
// on slow machines we don't miss mouse clicks.
// We do thins by sampling the mouse on its own thread and enqueueing
// mouse events when we see things (mouse button status) change.
// Note that this shouldn't have any effect of the mouse position.
#define THREADED_MOUSE_INPUT

/// This define changes the behavior from having ingame cursors track the
/// mouse when the window has no focus(when defined) to ignoring mouse 
/// without window focus.
/// In either case, clicks are ignored without focus, including a click
/// that brings the window back into focus.
//#define MOVE_MOUSE_WITHOUT_FOCUS

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

#if !NETFX_CORE
using System.Threading;
#endif

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

using Microsoft.Xna.Framework.Graphics;

using Boku.Common.Xml;

using Boku.Web;

namespace Boku.Common
{
    /// <summary>
    /// Singleton wrapper for mouse input.
    /// </summary>
    public static class MouseInput
    {
#if !NETFX_CORE && THREADED_MOUSE_INPUT
        /// <summary>
        /// Class which wraps normal mouse state along with a flag indicating
        /// that the state should be ignored until the buttons are released.
        /// This is set when transitioning from non-focus to focus.
        /// </summary>
        public class MouseState2
        {
            public MouseState state;
            public bool ignoreUntilReleased = false;

            public MouseState2(MouseState state, bool ignoreUntilReleased)
            {
                this.state = state;
                this.ignoreUntilReleased = ignoreUntilReleased;
            }
        }

        public class MouseWorker
        {
            MouseState curState = new MouseState();
            bool prevActive = false;

            public void SampleMouseState()
            {
                while (true)
                {
                    bool active = System.Windows.Forms.Form.ActiveForm != null;
                    bool ignoreUntilReleased = false;

                    MouseState state = Mouse.GetState();

                    // Adjust position for tutorial mode.
                    int x = state.X - (int)BokuGame.ScreenPosition.X;
                    int y = state.Y - (int)BokuGame.ScreenPosition.Y;
                    int wheel = state.ScrollWheelValue;
                    ButtonState left = state.LeftButton;
                    ButtonState middle = state.MiddleButton;
                    ButtonState right = state.RightButton;
                    ButtonState x1 = state.XButton1;
                    ButtonState x2 = state.XButton2;

                    if (System.Windows.Forms.SystemInformation.MouseButtonsSwapped)
                    {
                        ButtonState tmp = left;
                        left = right;
                        right = tmp;
                    }

                    // If mouse is outside of the screen, treat all the buttons as up.
                    if (x < 0 || y < 0 || x > BokuGame.ScreenSize.X || y > BokuGame.ScreenSize.Y)
                    {
                        left = ButtonState.Released;
                        middle = ButtonState.Released;
                        right = ButtonState.Released;
                        x1 = ButtonState.Released;
                        x2 = ButtonState.Released;
                        wheel = 0;

                        // This prevents resizing the window from looking like a left button released event.
                        MouseInput.Left.IgnoreUntilReleased = true;
                    }

                    // Adjust position for tutorial mode.
                    state = new MouseState(x, y, wheel, left, middle, right, x1, x2);

                    // When transitioning from not active to active we want to ignore all the buttons 
                    // until they've been released.
                    if (active && !prevActive)
                    {
                        ignoreUntilReleased = true;
                    }
                    prevActive = active;

                    if (active && !state.Equals(curState))
                    {
                        lock (MouseInput.MouseStateQueue)
                        {
                            // Need to decide whether to add a new entry or to just update last one.
                            // If the queue is empty, add.
                            // If the state change is button or wheel changes, then add a new one.
                            // If the state change is just X,Y changes, update the last one.
                            // This prevents the queue from growing huge if the user is just moving the mouse.
                            if (MouseInput.MouseStateQueue.Count == 0
                                || state.LeftButton != curState.LeftButton
                                || state.RightButton != curState.RightButton
                                || state.MiddleButton != curState.MiddleButton
                                //|| state.ScrollWheelValue != curState.ScrollWheelValue
                                )
                            {
                                // New entry.
                                MouseInput.MouseStateQueue.Add(new MouseState2(state, ignoreUntilReleased));
                            }
                            else
                            {
                                // Update X,Y of last entry.  (We do this just by replacing the whole 
                                // entry since X,Y is the only thing that changed.)
                                int index = MouseInput.MouseStateQueue.Count - 1;
                                MouseInput.MouseStateQueue[index].state = state;
                                MouseInput.MouseStateQueue[index].ignoreUntilReleased |= ignoreUntilReleased;
                            }

                        }
                        curState = state;
                    }

                    // Sample every 5ms.  Probably overkill.
                    Thread.Sleep(5);

                }   // end of while loop.
            }
        }
#endif

        #region Members
        /// <summary>
        /// Mouse buttons act exactly like pad buttons so use the same class.
        /// </summary>
        private static GamePadInput.Button left = new GamePadInput.Button();
        private static GamePadInput.Button middle = new GamePadInput.Button();
        private static GamePadInput.Button right = new GamePadInput.Button();
        private static GamePadInput.Button xButton1 = new GamePadInput.Button();
        private static GamePadInput.Button xButton2 = new GamePadInput.Button();

        // Pixel position of mouse releative to 0,0 upper left hand corner of window.
        private static Point curPosition = Point.Zero;
        private static Point prevPosition = Point.Zero;

        private static int curScrollValue = 0;
        private static int prevScrollValue = 0;
        private static int accumulatedScrollValue = 0;

        // This is the object that was clicked on.  Should only be activated if the user
        // releases the button still over this object.  Note that on the frame after a
        // release, this is cleared.
        private static object clickedOnObject = null;

        /// <summary>
        /// Was the mouse window active last frame?
        /// </summary>
        private static bool wasActive = false;
        /// <summary>
        /// Was the mouse touched (button or movement) this frame?
        /// </summary>
        private static bool wasTouched = false;

        private static bool overButton = false;

#if !NETFX_CORE && THREADED_MOUSE_INPUT
        /// <summary>
        /// Has the worker thread for reading the mouse state been started?
        /// </summary>
        private static bool initialized = false;
        // Implemented as a List since we have to do some non-queue behaviours.
        public static List<MouseState2> MouseStateQueue = new List<MouseState2>();
        private static MouseState prevMouseState = new MouseState();
        private static Thread mouseWorkerThread;
#endif

        #endregion

        #region Accessors

        /// <summary>
        /// Current mouse position in pixels.
        /// 0, 0 is upper left hand corner of window.
        /// </summary>
        public static Point Position
        {
            get { return curPosition; }
        }
        /// <summary>
        /// Same as Position but as a Vector2 instead of Point.
        /// </summary>
        public static Vector2 PositionVec
        {
            get { return new Vector2(curPosition.X, curPosition.Y); }
        }

        /// <summary>
        /// Previous frame's mouse position.
        /// </summary>
        public static Point PrevPosition
        {
            get { return prevPosition; }
        }

        public static int ScrollWheel
        {
            get { return curScrollValue; }
        }

        /// <summary>
        /// Accessor to allow external influence of scroll wheel value.
        /// Needed to work with WinKeyboard WndProc().
        /// </summary>
        public static int ExternalScrollValue
        {
            get { return accumulatedScrollValue; }
            set { accumulatedScrollValue = value; }
        }

        public static int PrevScrollWheel
        {
            get { return prevScrollValue; }
        }

        public static GamePadInput.Button Left
        {
            get { return left; }
        }

        public static GamePadInput.Button Middle
        {
            get { return middle; }
        }

        public static GamePadInput.Button Right
        {
            get { return right; }
        }

        public static GamePadInput.Button XButton1
        {
            get { return xButton1; }
        }

        public static GamePadInput.Button XButton2
        {
            get { return xButton2; }
        }

        // This is the object that was clicked on.  Should only be activated if the user
        // releases the button still over this object.    Note that on the frame after a
        // release, this is cleared.
        public static object ClickedOnObject
        {
            get { return clickedOnObject; }
            set { clickedOnObject = value; }
        }

        /// <summary>
        /// Return true if any buttons were pressed this frame, or the mouse moved, or
        /// the scroll wheel scrolled. 
        /// </summary>
        public static bool WasTouched
        {
            get { return wasTouched; }
        }

        public static bool OverButton
        {
            set { overButton = value; }
        }
        #endregion

        #region Public

        public static void Update()
        {
            if (TouchInput.TouchCount > 0)
            {
                Clear();
                return;
            }

            if (BokuGame.bokuGame.IsActive)    // Tells if the game is the active application.
            {

#if HAND_CURSOR
                if (overButton)
                {
                    //BokuGame.bokuGame.Window.C
                    System.Windows.Forms.Cursor.Current = System.Windows.Forms.Cursors.Hand;
                }
                else
                {
                    System.Windows.Forms.Cursor.Current = System.Windows.Forms.Cursors.Arrow;
                }
#endif
                overButton = false;

                // If button was released last frame, clear the mouse focus object.
                if (left.WasReleased)
                {
                    clickedOnObject = null;
                }

#if NETFX_CORE || !THREADED_MOUSE_INPUT               
                MouseState state = Mouse.GetState();

#if NETFX_CORE
                // Looks like in .net 4.5 there is SystemParameters.SwapButtons but that 
                // doesn't seem to work with WinRT.  Color me surprised.  SO just assume
                // that the mouse buttons aren't swapped.
                if (false)
#else
                // Adjust position for tutorial mode.
                if (System.Windows.Forms.SystemInformation.MouseButtonsSwapped)
#endif
                {
                    // Swap buttons.
                    state = new MouseState(state.X - (int)BokuGame.ScreenPosition.X, state.Y - (int)BokuGame.ScreenPosition.Y, state.ScrollWheelValue, state.RightButton, state.MiddleButton, state.LeftButton, state.XButton1, state.XButton2);
                }
                else
                {
                    state = new MouseState(state.X - (int)BokuGame.ScreenPosition.X, state.Y - (int)BokuGame.ScreenPosition.Y, state.ScrollWheelValue, state.LeftButton, state.MiddleButton, state.RightButton, state.XButton1, state.XButton2);
                }
#else
                if (!initialized)
                {
                    MouseWorker worker = new MouseWorker();

                    mouseWorkerThread = new Thread(new ThreadStart(worker.SampleMouseState));
                    mouseWorkerThread.Start();

                    initialized = true;
                }

                MouseState state = prevMouseState;

                // Check if we have any changes in the queue.  If so, get them.
                lock (MouseStateQueue)
                {
                    if (MouseStateQueue.Count > 0)
                    {
                        state = MouseStateQueue[0].state;
                        if (MouseStateQueue[0].ignoreUntilReleased)
                        {
                            Clear();
                        }
                        MouseStateQueue.RemoveAt(0);

                        prevMouseState = state;
                    }
                }
#endif

                left.Update(state.LeftButton);
                middle.Update(state.MiddleButton);
                right.Update(state.RightButton);
                xButton1.Update(state.XButton1);
                xButton2.Update(state.XButton2);

                prevPosition = curPosition;
                curPosition = new Point(state.X, state.Y);

#if NETFX_CORE
                prevScrollValue = curScrollValue;
                curScrollValue = state.ScrollWheelValue;
#else
                // NOTE: because of the WinPrc changes, this is the only way
                // we get scroll info.  The Mouse.GetState() call always
                // returns 0.
                prevScrollValue = curScrollValue;
                curScrollValue += accumulatedScrollValue;

                accumulatedScrollValue = 0;
#endif

                if (!wasActive)
                {
#if !MOVE_MOUSE_WITHOUT_FOCUS
                    prevPosition = curPosition;
                    prevScrollValue = curScrollValue;
#endif // !MOVE_MOUSE_WITHOUT_FOCUS

                    wasActive = true;

                    left.IgnoreUntilReleased = true;
                    middle.IgnoreUntilReleased = true;
                    right.IgnoreUntilReleased = true;
                    xButton1.IgnoreUntilReleased = true;
                    xButton2.IgnoreUntilReleased = true;
                }

                wasTouched = Touched();

                //start the instrumentation clock if we recognize user behavior
                if (wasTouched == true && Time.ActiveGameClock == false)
                {
                    Time.startActiveInstrumentationClock();
                }
            }
            else
            {
                //check for inactive time
                Time.startInactiveCheck();

                // Not active, just clear everything.
                Clear();
            }

        }   // end of Update()

        public static void StopMouseWorkerThread()
        {
#if !NETFX_CORE && THREADED_MOUSE_INPUT
            mouseWorkerThread.Abort();
#endif
        }   // end of StopMouseWorkerThread()

        public static void Clear()
        {
            clickedOnObject = null;

            left.ClearAllWasPressedState();
            middle.ClearAllWasPressedState();
            right.ClearAllWasPressedState();
            xButton1.ClearAllWasPressedState();
            xButton2.ClearAllWasPressedState();

#if MOVE_MOUSE_WITHOUT_FOCUS
                // Keep the position alive just to help debugging.
                MouseState state = Mouse.GetState();
                // Adjust position for tutorial mode.
                if (System.Windows.Forms.SystemInformation.MouseButtonsSwapped)
                {
                    // Swap buttons.
                    state = new MouseState(state.X - (int)BokuGame.ScreenPosition.X, state.Y - (int)BokuGame.ScreenPosition.Y, state.ScrollWheelValue, state.RightButton, state.MiddleButton, state.LeftButton, state.XButton1, state.XButton2);
                }
                else
                {
                    state = new MouseState(state.X - (int)BokuGame.ScreenPosition.X, state.Y - (int)BokuGame.ScreenPosition.Y, state.ScrollWheelValue, state.LeftButton, state.MiddleButton, state.RightButton, state.XButton1, state.XButton2);
                }
                prevPosition = curPosition;
                curPosition = new Point(state.X, state.Y);
#endif // MOVE_MOUSE_WITHOUT_FOCUS

            wasActive = false;
            wasTouched = false;
        }

        /// <summary>
        /// Gets the mouse position but transformed into current rendertarget coordinates.
        /// Note:  This requires that ScreenWarp.FitRtToScreen() has been called with the
        /// correct RT size.
        /// </summary>
        /// <returns></returns>
        public static Vector2 GetMouseInRtCoords()
        {
            Vector2 mousePosition = new Vector2(MouseInput.Position.X, MouseInput.Position.Y);
            mousePosition = ScreenWarp.ScreenToRT(mousePosition);

            return mousePosition;
        }   // end of GetMouseInRtCoords()


        /// <summary>
        /// Returns the mouse position but adjusted for using a rt that has
        /// a different aspect ratio than the window being rendered into.
        /// </summary>
        /// <param name="useOverscan">Also take into account the overscan setting.</param>
        public static Vector2 GetAspectRatioAdjustedPosition(Camera camera, bool useOverscan)
        {
            return GetAspectRatioAdjustedPosition(camera, useOverscan, false);
        }

        /// <summary>
        /// Returns the mouse position but adjusted for using a rt that has
        /// a different aspect ratio than the window being rendered into.
        /// </summary>
        /// <param name="useOverscan">Also take into account the overscan setting.</param>
        /// <param name="ignoreScreenPosition">Needed for tutorial modal display.</param>
        public static Vector2 GetAspectRatioAdjustedPosition(Camera camera, bool useOverscan, bool ignoreScreenPosition)
        {
            Vector2 mousePosition = new Vector2(MouseInput.Position.X, MouseInput.Position.Y);

            mousePosition = AdjustHitPosition(mousePosition, camera, useOverscan, ignoreScreenPosition);

            return mousePosition;
        }   // end of GetAspectRatioAdjustedPosition()

        /// <summary>
        /// Returns the mouse position but adjusted for using a rt that has
        /// a different aspect ratio than the window being rendered into.
        /// </summary>
        /// <param name="useOverscan">Also take into account the overscan setting.</param>
        /// <param name="ignoreScreenPosition">Needed for tutorial modal display.</param>
        public static Vector2 AdjustHitPosition(Vector2 hit, Camera camera, bool useOverscan, bool ignoreScreenPosition)
        {
            if (!ignoreScreenPosition)
            {
                hit -= BokuGame.ScreenPosition;
            }

            GraphicsDevice device = BokuGame.bokuGame.GraphicsDevice;
            Vector2 winRes = BokuGame.ScreenSize;
            if (ignoreScreenPosition)
            {
                winRes = new Vector2(BokuGame.bokuGame.GraphicsDevice.Viewport.Width, BokuGame.bokuGame.GraphicsDevice.Viewport.Height);
            }

            Vector2 rtRes = new Vector2(camera.Resolution.X, camera.Resolution.Y);

            // Transform mouse coords to be 0,0 at center of screen.
            hit -= winRes / 2.0f;

            // Scale to adjust for vertical res difference.
            hit *= rtRes.Y / winRes.Y;

            // Transform coords back out to having 0,0 in the upper left.  This time using the rt size.
            hit += rtRes / 2.0f;

            return hit;
        }   // end of AdjustHitPosition

        /// <summary>
        /// Helper function for mouse selection of standard 2d UI elements.
        /// </summary>
        /// <param name="camera">Current camera.</param>
        /// <param name="invWorldMatrix">Inverse of element's world matrix.</param>
        /// <param name="width">Width of UI element in world units.</param>
        /// <param name="height">Height of UI element in world units.</param>
        /// <param name="useRtCoords">Assumes rendering to a rendertarget rather than the backbuffer.</param>
        /// <returns>UV coord of mouse hit. Should be in [0, 1][0, 1] range.  Outside of this implies a miss.</returns>
        public static Vector2 GetHitUV(Camera camera, ref Matrix invWorldMatrix, float width, float height, bool useRtCoords)
        {
            Vector2 mousePosition;

            if(useRtCoords)
            {
                mousePosition = GetMouseInRtCoords();
            }
            else
            {
                mousePosition = MouseInput.PositionVec;
            }

            // Get 3D direction for mouse position.
            Vector3 mouseDir = camera.ScreenToWorldCoords(mousePosition);

            // Transform mouse ray into local space.
            Vector3 position = Vector3.Transform(camera.ActualFrom, invWorldMatrix);
            Vector3 direction = Vector3.TransformNormal(mouseDir, invWorldMatrix);

            // Project to Z==0 plane, calc hit in local units (origin in center).
            float dist = -position.Z / direction.Z;
            Vector3 hit = position + direction * dist;

            Target = hit - invWorldMatrix.Translation;

            Vector2 hitUV = new Vector2(hit.X / width + 0.5f, -hit.Y / height + 0.5f);

            return hitUV;
        }   // end of GetHitUV

        /// <summary>
        /// Returns it in world coords of ray through mouse pixel.
        /// </summary>
        /// <param name="camera">UiCamera (orthographic)</param>
        /// <param name="invWorldMatrix">Inverse of object's world matrix.</param>
        /// <param name="useRtCoords">Adjust mouse hit for rendering to a render target?</param>
        /// <returns></returns>
        public static Vector2 GetHitOrtho(UiCamera camera, ref Matrix invWorldMatrix, bool useRtCoords)
        {
            Vector2 mousePosition;

            if (useRtCoords)
            {
                mousePosition = MouseInput.GetMouseInRtCoords();
            }
            else
            {
                mousePosition = new Vector2(MouseInput.Position.X, MouseInput.Position.Y);
            }

            Vector2 hit = Vector2.Zero;

            // Convert from pixels to world units.
            hit.X = mousePosition.X * camera.Width / camera.Resolution.X;
            hit.Y = mousePosition.Y * camera.Height / camera.Resolution.Y;

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

        // Time used to determine autorepeat rates for At* functions.
        private static double edgeTic = 0;
        private static double repeatRate = 0.07;

        // Width around window allowed for mouse to still be considered active for At* functions.
        private static int borderWidth = 20;

        // Is the mouse in the border region of the window.
        static private bool MouseNearEdge()
        {
            bool result = false;

            // First check if we're in window at all.
            if (Position.X < 0 || Position.X >= BokuGame.ScreenSize.X || Position.Y < 0 || Position.Y >= BokuGame.ScreenSize.Y)
            {
                // Outside of window.
                result = false;
            }
            else
            {
                if (Position.X < borderWidth
                    || Position.X > BokuGame.ScreenSize.X - borderWidth
                    || Position.Y < borderWidth
                    || Position.Y > BokuGame.ScreenSize.Y - borderWidth)
                {
                    result = true;
                }
            }

            return result;
        }   // end of InRange()

        /// <summary>
        /// Quick test to see if we should ignore the mouse positioned at the edges of the window.
        /// Returns true if any of the following conditions are true:
        ///     Game doesn't have focus.
        ///     Mouse is not at edge or withing 20 pixels of window.
        ///     Input mode is not KeyboardMouse.
        /// </summary>
        /// <returns>True if we should ignore the mouse at the edge of the window.</returns>
        private static bool IgnoreEdges()
        {
            bool result = !BokuGame.bokuGame.IsActive || !MouseNearEdge() || GamePadInput.ActiveMode != GamePadInput.InputMode.KeyboardMouse;

            return result;
        }

        /// <summary>
        /// Is the mouse at the top edge of the window?
        /// </summary>
        /// <returns></returns>
        static public bool AtWindowTop()
        {
            bool result = false;

            if (!IgnoreEdges() && Position.Y <= 0)
            {
                if (Time.WallClockTotalSeconds > edgeTic + repeatRate)
                {
                    edgeTic = Time.WallClockTotalSeconds;
                    result = true;
                }
            }

            return result;
        }   // end of AtWindowTop()

        /// <summary>
        /// Is the mouse at the bottom edge of the window?
        /// </summary>
        /// <returns></returns>
        static public bool AtWindowBottom()
        {
            bool result = false;

            if (!IgnoreEdges() && Position.Y >= BokuGame.ScreenSize.Y - 1)
            {
                if (Time.WallClockTotalSeconds > edgeTic + repeatRate)
                {
                    edgeTic = Time.WallClockTotalSeconds;
                    result = true;
                }
            }

            return result;
        }   // end of AtWindowBottom()

        /// <summary>
        /// Is the mouse at the left edge of the window?
        /// </summary>
        /// <returns></returns>
        static public bool AtWindowLeft()
        {
            bool result = false;

            if (!IgnoreEdges() && Position.X <= 0)
            {
                if (Time.WallClockTotalSeconds > edgeTic + repeatRate)
                {
                    edgeTic = Time.WallClockTotalSeconds;
                    result = true;
                }
            }

            return result;
        }   // end of AtWindowLeft()

        /// <summary>
        /// Is the mouse at the right edge of the window?
        /// </summary>
        /// <returns></returns>
        static public bool AtWindowRight()
        {
            bool result = false;

            if (!IgnoreEdges() && Position.X >= BokuGame.ScreenSize.X - 1)
            {
                if (Time.WallClockTotalSeconds > edgeTic + repeatRate)
                {
                    edgeTic = Time.WallClockTotalSeconds;
                    result = true;
                }
            }

            return result;
        }   // end of AtWindowRight()


        /// <summary>
        /// Debug helper...
        /// </summary>
        static public Vector3 Target;

        #endregion

        #region Internal
        /// <summary>
        /// Evaluate whether the user used the mouse this frame.
        /// </summary>
        /// <returns></returns>
        private static bool Touched()
        {
            // If we switch in or out of tutorial mode then the mouse value
            // gets offset.  We don't want to consider this a mouse move.  So
            // in typical hacky fashion we only look for moves in both axes
            // over a certain amount (in this case 32 which I just made up).
            // This is because the amount of offset based on tutorial mode 
            // is twitched into place so it looks just like a small mouse move.
            int dx = Math.Abs(prevPosition.X - curPosition.X);
            int dy = Math.Abs(prevPosition.Y - curPosition.Y);
            bool moved = (dx > 32) && (dy > 32);

            bool touched = left.WasPressed
                || middle.WasPressed
                || right.WasPressed
                || xButton1.WasPressed
                || xButton2.WasPressed
                //|| (prevPosition != curPosition)
                || moved
                || (prevScrollValue != curScrollValue);

            return touched;
        }
        #endregion Internal

    }   // end of class MouseInput

}   // end of namespace Boku.Common
