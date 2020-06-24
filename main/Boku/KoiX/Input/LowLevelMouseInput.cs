
using System;
using System.Collections;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Storage;
using Microsoft.Xna.Framework.Content;

using System.Diagnostics;

namespace KoiX.Input
{
    public class LowLevelMouseInput
    {
        #region Members

        static MouseState prevState;
        static MouseState curState;

        static bool wasTouched = false;     // Did anything change this frame?

        static GamePadInput.Button left = new GamePadInput.Button();
        static GamePadInput.Button right = new GamePadInput.Button();
        static GamePadInput.Button middle = new GamePadInput.Button();

        static Point deltaPosition = Point.Zero;
        static int deltaScrollWheel = 0;

        static bool invertYCoord = false;

        static bool ignoreMouseEvents = false;  // Gloabls switch to ignore all mouse events.

        static bool insideWindow = false;
        static bool prevInsideWindow = false;   // Used to detect teh outside->inside transition.
                                                // When we see the transition, we zero out the 
                                                // mouse position delta for that frame.

        #endregion

        #region Accessors

        /// <summary>
        /// The position of the mouse reltive to the upper left hand corner of the window.
        /// Note, changes to the position will not be reflected until next Update.
        /// </summary>
        public static Point Position
        {
            get { return new Point(prevState.X, prevState.Y); }
            set 
            {
                if (invertYCoord)
                {
                    value.Y = KoiLibrary.ClientRect.Height - value.Y;
                }
                Mouse.SetPosition(value.X, value.Y);
            }
        }

        /// <summary>
        /// Same as Position but as a Vector2 instead of Point.
        /// </summary>
        public static Vector2 PositionVec
        {
            get { return new Vector2(Position.X, Position.Y); }
        }

        /// <summary>
        /// Change in position from last frame.
        /// </summary>
        public static Point DeltaPosition
        {
            get { return deltaPosition; }
        }

        /// <summary>
        /// Helper based on current position and delta.
        /// </summary>
        public static Point PrevPosition
        {
            get { return new Point(Position.X - DeltaPosition.X, Position.Y - DeltaPosition.Y); }
        }

        /// <summary>
        /// Helper based on current position and delta.
        /// </summary>
        public static Vector2 PrevPositionVec
        {
            get { return new Vector2(Position.X - DeltaPosition.X, Position.Y - DeltaPosition.Y); }
        }

        public static GamePadInput.Button Left
        {
            get { return left; }
        }

        public static GamePadInput.Button Right
        {
            get { return right; }
        }

        public static GamePadInput.Button Middle
        {
            get { return middle; }
        }

        public static int ScrollWheel
        {
            get { return curState.ScrollWheelValue; }
        }

        /// <summary>
        /// Change in scroll wheel value since last Update().
        /// </summary>
        public static int DeltaScrollWheel
        {
            get { return deltaScrollWheel; }
        }

        /// <summary>
        /// Is the mouse cursor currently in the window?
        /// </summary>
        public static bool InWindow
        {
            get
            {
                Point window = KoiLibrary.ClientRect.GetSize();
                bool inside = curState.X >= 0 && curState.X < window.X && curState.Y >= 0 && curState.Y < window.Y;
                return inside;
            }
        }

        /// <summary>
        /// Flag which controls whether or not mouse events are processed.
        /// </summary>
        public static bool IgnoreMouseEvents
        {
            get { return ignoreMouseEvents; }
            set { ignoreMouseEvents = value; }
        }

        public static bool WasTouched
        {
            get 
            { 
                bool touched = wasTouched;

                // If the screen size/position has changed this may falsely
                // trigger touched so ignore it for a few frames.
                if (ignoreTouchedFrameCount > 0)
                {
                    touched = false;
                    --ignoreTouchedFrameCount;
                }

                return touched;
            }
        }

        /// <summary>
        /// If true, inverts the Y coord of mouse output so that 0, 0 is lower-left 
        /// instead of upper-left corner.
        /// </summary>
        public static bool InvertYCoord
        {
            get { return invertYCoord; }
            set { invertYCoord = value; }
        }

        #endregion

        // c'tor
        LowLevelMouseInput()
        {
            prevState = Mouse.GetState();
            if (invertYCoord)
            {
                prevState = InvertY(prevState);
            }
        }   // end of LowLevelMouseInput c'tor

        /// <summary>
        /// One time call to set up mouse input functionality.
        /// </summary>
        public static void Init()
        {
            prevState = curState = Mouse.GetState();
            if (invertYCoord)
            {
                curState = InvertY(curState);
                prevState = curState;
            }
        }   // end of LowLevelMouseInput Init()

        /// <summary>
        /// Must be called once per frame to update the current state of the mouse input handling.
        /// When events are detected, also calls any registered event handlers.
        /// </summary>
        /// <returns>True if the mouse input changed this frame.</returns>
        public static bool Update()
        {
            curState = Mouse.GetState();
            if (invertYCoord)
            {
                curState = InvertY(curState);
            }

            prevInsideWindow = insideWindow;
            insideWindow = !(curState.X < 0 || curState.Y < 0 || curState.X >= KoiLibrary.ClientRect.Width || curState.Y >= KoiLibrary.ClientRect.Height);

            // If the mouse is outside of the app's window, we still want to let painting happen but
            // we shouldn't trigger any new mouse down events.

            /*
            if (winFormControl != 0 || curState.X < 0 || curState.Y < 0 || curState.X >= KoiLibrary.ClientRect.Width || curState.Y >= KoiLibrary.ClientRect.Height)
            {
                // Fake events so objects get back to not-pressed state..
                if (Left.IsPressed)
                {
                    MouseEventArgs e = new MouseEventArgs( MouseButtons.Left, 1, curState.X, curState.Y, DeltaScrollWheel );
                    MouseInput input = new MouseInput( Time.WallClockTotalSeconds, MouseInput.MouseAction.Up, e );
                    KoiLibrary.InputEventManager.ProcessMouseLeftUpEvent( input );
                }
                if (Middle.IsPressed)
                {
                    MouseEventArgs e = new MouseEventArgs( MouseButtons.Middle, 1, curState.X, curState.Y, DeltaScrollWheel );
                    MouseInput input = new MouseInput( Time.WallClockTotalSeconds, MouseInput.MouseAction.Up, e );
                    KoiLibrary.InputEventManager.ProcessMouseMiddleUpEvent( input );
                }
                if (Right.IsPressed)
                {
                    MouseEventArgs e = new MouseEventArgs( MouseButtons.Right, 1, curState.X, curState.Y, DeltaScrollWheel );
                    MouseInput input = new MouseInput( Time.WallClockTotalSeconds, MouseInput.MouseAction.Up, e );
                    KoiLibrary.InputEventManager.ProcessMouseRightUpEvent( input );
                }

                Left.Reset();
                Middle.Reset();
                Right.Reset();

                // The mouse looses focus when it leaves the window.
                KoiLibrary.InputEventManager.MouseFocusObject = null;
            }
            else
            */
            {
                // Mouse is inside window, handle normally.

                Left.Update(curState.LeftButton, insideWindow);
                Middle.Update(curState.MiddleButton, insideWindow);
                Right.Update(curState.RightButton, insideWindow);

                if (insideWindow && !prevInsideWindow)
                {
                    // Mouse is transitioning from outside to inside so zero out movement.
                    deltaPosition = Point.Zero;
                    deltaScrollWheel = 0;
                }
                else
                {
                    deltaPosition.X = curState.X - prevState.X;
                    deltaPosition.Y = curState.Y - prevState.Y;
                    deltaScrollWheel = curState.ScrollWheelValue - prevState.ScrollWheelValue;
                }

                // Notify InputManager of the changes.
                MouseEventArgs e = null;
                MouseInput input = null;

                if (!IgnoreMouseEvents)
                {
                    // Left button down.
                    if (Left.WasPressed)
                    {
                        e = new MouseEventArgs(MouseButtons.Left, 1, curState.X, curState.Y, DeltaScrollWheel);
                        input = new MouseInput(Time.WallClockTotalSeconds, MouseInput.MouseAction.Down, e);
                        KoiLibrary.InputEventManager.ProcessMouseLeftDownEvent(input);
                    }
                    // Left button up.
                    if (Left.WasReleased)
                    {
                        e = new MouseEventArgs(MouseButtons.Left, 1, curState.X, curState.Y, DeltaScrollWheel);
                        input = new MouseInput(Time.WallClockTotalSeconds, MouseInput.MouseAction.Up, e);
                        KoiLibrary.InputEventManager.ProcessMouseLeftUpEvent(input);
                        
                        MouseInput.ClickedOnObject = null;
                    }

                    // Middle button down.
                    if (Middle.WasPressed)
                    {
                        e = new MouseEventArgs(MouseButtons.Middle, 1, curState.X, curState.Y, DeltaScrollWheel);
                        input = new MouseInput(Time.WallClockTotalSeconds, MouseInput.MouseAction.Down, e);
                        KoiLibrary.InputEventManager.ProcessMouseMiddleDownEvent(input);
                    }
                    // Middle button up.
                    if (Middle.WasReleased)
                    {
                        e = new MouseEventArgs(MouseButtons.Middle, 1, curState.X, curState.Y, DeltaScrollWheel);
                        input = new MouseInput(Time.WallClockTotalSeconds, MouseInput.MouseAction.Up, e);
                        KoiLibrary.InputEventManager.ProcessMouseMiddleUpEvent(input);
                    }

                    // Right button down.
                    if (Right.WasPressed)
                    {
                        e = new MouseEventArgs(MouseButtons.Right, 1, curState.X, curState.Y, DeltaScrollWheel);
                        input = new MouseInput(Time.WallClockTotalSeconds, MouseInput.MouseAction.Down, e);
                        KoiLibrary.InputEventManager.ProcessMouseRightDownEvent(input);
                    }
                    // Right button up.
                    if (Right.WasReleased)
                    {
                        e = new MouseEventArgs(MouseButtons.Right, 1, curState.X, curState.Y, DeltaScrollWheel);
                        input = new MouseInput(Time.WallClockTotalSeconds, MouseInput.MouseAction.Up, e);
                        KoiLibrary.InputEventManager.ProcessMouseRightUpEvent(input);
                    }

                    // Mouse Move
                    if (DeltaPosition != Point.Zero)
                    {
                        e = new MouseEventArgs(MouseButtons.None, 1, curState.X, curState.Y, DeltaScrollWheel);
                        input = new MouseInput(Time.WallClockTotalSeconds, MouseInput.MouseAction.Move, e);
                        KoiLibrary.InputEventManager.ProcessMouseMoveEvent(input);
                    }

                    // Mouse Position
                    {
                        e = new MouseEventArgs(MouseButtons.None, 1, curState.X, curState.Y, DeltaScrollWheel);
                        input = new MouseInput(Time.WallClockTotalSeconds, MouseInput.MouseAction.Move, e);
                        KoiLibrary.InputEventManager.ProcessMousePositionEvent(input);
                    }

                    // Scroll Wheel
                    if (DeltaScrollWheel != 0)
                    {
                        e = new MouseEventArgs(MouseButtons.None, 1, curState.X, curState.Y, DeltaScrollWheel);
                        input = new MouseInput(Time.WallClockTotalSeconds, MouseInput.MouseAction.Wheel, e);
                        KoiLibrary.InputEventManager.ProcessMouseWheelEvent(input);
                    }

                    // TODO Hover???
                    // Mouse Hover
                    // Note, hover in this case just means that the mouse is over the object.  There
                    // is no time component.
                    {
                        e = new MouseEventArgs(MouseButtons.None, 1, curState.X, curState.Y, DeltaScrollWheel);
                        input = new MouseInput(Time.WallClockTotalSeconds, MouseInput.MouseAction.Hover, e);
                        KoiLibrary.InputEventManager.ProcessMouseHoverEvent(input);
                    }

                }   // end if IgnoreMouseEvents
            }

            wasTouched = !IgnoreMouseEvents && !prevState.Equals(curState);

            prevState = curState;

            return wasTouched;
        }   // end of LowLevelMouseInput Update()

        /// <summary>
        /// take an existing MouseState object and returns a new one with the Y coordinate inverted.
        /// </summary>
        /// <param name="state"></param>
        /// <returns></returns>
        static MouseState InvertY(MouseState state)
        {
            // TODO (scoy) Figure this out...
            // Is this ever not true?  If so, which is the right one to use?
            Debug.Assert(KoiLibrary.ClientRect.Height == KoiLibrary.ViewportSize.Y);
            return new MouseState(state.X, KoiLibrary.ClientRect.Height - state.Y, state.ScrollWheelValue, state.LeftButton, state.MiddleButton, state.RightButton, state.XButton1, state.XButton2);
        }

        /// <summary>
        /// Gets the mouse position but transformed into current rendertarget coordinates.
        /// Note:  This requires that ScreenWarp.FitRtToScreen() has been called with the
        /// correct RT size.
        /// </summary>
        /// <returns></returns>
        public static Vector2 GetMouseInRtCoords()
        {
            Vector2 mousePosition = new Vector2(LowLevelMouseInput.Position.X, LowLevelMouseInput.Position.Y);
            mousePosition = Boku.Common.ScreenWarp.ScreenToRT(mousePosition);

            return mousePosition;
        }   // end of GetMouseInRtCoords()


        /// <summary>
        /// Returns the mouse position but adjusted for using a rt that has
        /// a different aspect ratio than the window being rendered into.
        /// </summary>
        /// <param name="useOverscan">Also take into account the overscan setting.</param>
        public static Vector2 GetAspectRatioAdjustedPosition(Boku.Common.Camera camera, bool useOverscan)
        {
            return GetAspectRatioAdjustedPosition(camera, useOverscan, false);
        }

        /// <summary>
        /// Returns the mouse position but adjusted for using a rt that has
        /// a different aspect ratio than the window being rendered into.
        /// </summary>
        /// <param name="useOverscan">Also take into account the overscan setting.</param>
        /// <param name="ignoreScreenPosition">Needed for tutorial modal display.</param>
        public static Vector2 GetAspectRatioAdjustedPosition(Boku.Common.Camera camera, bool useOverscan, bool ignoreScreenPosition)
        {
            Vector2 mousePosition = new Vector2(LowLevelMouseInput.Position.X, LowLevelMouseInput.Position.Y);

            mousePosition = AdjustHitPosition(mousePosition, camera, useOverscan, ignoreScreenPosition);

            return mousePosition;
        }   // end of GetAspectRatioAdjustedPosition()

        /// <summary>
        /// Returns the mouse position but adjusted for using a rt that has
        /// a different aspect ratio than the window being rendered into.
        /// </summary>
        /// <param name="useOverscan">Also take into account the overscan setting.</param>
        /// <param name="ignoreScreenPosition">Needed for tutorial modal display.</param>
        public static Vector2 AdjustHitPosition(Vector2 hit, Boku.Common.Camera camera, bool useOverscan, bool ignoreScreenPosition)
        {
            if (!ignoreScreenPosition)
            {
                hit -= Boku.BokuGame.ScreenPosition;
            }

            GraphicsDevice device = KoiLibrary.GraphicsDevice;
            Vector2 winRes = Boku.BokuGame.ScreenSize;
            if (ignoreScreenPosition)
            {
                winRes = new Vector2(KoiLibrary.GraphicsDevice.Viewport.Width, KoiLibrary.GraphicsDevice.Viewport.Height);
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
        public static Vector2 GetHitUV(Boku.Common.Camera camera, ref Matrix invWorldMatrix, float width, float height, bool useRtCoords)
        {
            Vector2 mousePosition;

            if (useRtCoords)
            {
                mousePosition = GetMouseInRtCoords();
            }
            else
            {
                mousePosition = LowLevelMouseInput.PositionVec;
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
        /// Debug helper...
        /// </summary>
        static public Vector3 Target;

        /// <summary>
        /// Returns it in world coords of ray through mouse pixel.
        /// </summary>
        /// <param name="camera">UiCamera (orthographic)</param>
        /// <param name="invWorldMatrix">Inverse of object's world matrix.</param>
        /// <param name="useRtCoords">Adjust mouse hit for rendering to a render target?</param>
        /// <returns></returns>
        public static Vector2 GetHitOrtho(Boku.Common.UiCamera camera, ref Matrix invWorldMatrix, bool useRtCoords)
        {
            Vector2 mousePosition;

            if (useRtCoords)
            {
                mousePosition = LowLevelMouseInput.GetMouseInRtCoords();
            }
            else
            {
                mousePosition = new Vector2(LowLevelMouseInput.Position.X, LowLevelMouseInput.Position.Y);
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
            if (Position.X < 0 || Position.X >= Boku.BokuGame.ScreenSize.X || Position.Y < 0 || Position.Y >= Boku.BokuGame.ScreenSize.Y)
            {
                // Outside of window.
                result = false;
            }
            else
            {
                if (Position.X < borderWidth
                    || Position.X > Boku.BokuGame.ScreenSize.X - borderWidth
                    || Position.Y < borderWidth
                    || Position.Y > Boku.BokuGame.ScreenSize.Y - borderWidth)
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
            bool result = !Boku.BokuGame.bokuGame.IsActive || !MouseNearEdge() || !KoiLibrary.LastTouchedDeviceIsKeyboardMouse;

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

            if (!IgnoreEdges() && Position.Y >= Boku.BokuGame.ScreenSize.Y - 1)
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

            if (!IgnoreEdges() && Position.X >= Boku.BokuGame.ScreenSize.X - 1)
            {
                if (Time.WallClockTotalSeconds > edgeTic + repeatRate)
                {
                    edgeTic = Time.WallClockTotalSeconds;
                    result = true;
                }
            }

            return result;
        }   // end of AtWindowRight()

        static int ignoreTouchedFrameCount = 0;

        static public void IgnoreTouched()
        {
            ignoreTouchedFrameCount = 3;    // 3 is just a guess.  May even work well with just 1.
        }   // end of BlockTouched()

    }   // end of class LowLevelMouseInput

}   // end of namespace KoiX.Input
