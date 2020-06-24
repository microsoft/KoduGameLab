
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input.Touch;

#if !NETFX_CORE
using TouchHook;
using System.Windows.Forms;
#endif

namespace KoiX.Input
{
    public static class PCTouchInput
    {
        // Callback for allowing outside code to process the raw
        // touches before gestures or events happen.
        public delegate void ProcessTouchesCallback(List<TouchSample> sampleList);

        #region Members

        static WM_TouchHook cwTouchHook;

        static bool TouchAvailable = false;
        static int MaxTouchCount = 0;

        static List<TouchSample> touchSampleList;

        static ProcessTouchesCallback processTouches;

        #endregion

        #region Accessors

        public static ProcessTouchesCallback ProcessTouches
        {
            set { processTouches = value; }
        }

        #endregion

        #region Public

        /// <summary>
        /// Init TouchHook
        /// </summary>
        /// <param name="controlHandle">Handle for the XNAControl we're rendering into.</param>
        public static void Init(IntPtr xnaControlHandle)
        {
            LowLevelTouchInput.Touches = new List<TouchSample>();
            touchSampleList = new List<TouchSample>();

            cwTouchHook = new WM_TouchHook(xnaControlHandle, TouchHook.HookType.WH_CALLWNDPROC);

            WM_TouchHook.DisableNativePressAndHoldGesture = true;

            cwTouchHook.InstallHook();

            cwTouchHook.TouchDown += new EventHandler<TouchEventArgs>(TouchDownHandler);
            cwTouchHook.TouchMove += new EventHandler<TouchEventArgs>(TouchMoveHandler);
            cwTouchHook.TouchUp += new EventHandler<TouchEventArgs>(TouchUpHandler);

            //messageTouchHook.TouchDown += new EventHandler<TouchEventArgs>(TouchDownHandler);
            //messageTouchHook.TouchMove += new EventHandler<TouchEventArgs>(TouchMoveHandler);
            //messageTouchHook.TouchUp += new EventHandler<TouchEventArgs>(TouchUpHandler);

            // Store the max touch count detected at startup.
            // TODO (****) Move these to LowLeveTouchInput?
            TouchAvailable = cwTouchHook.IsTouchAvailable();
            MaxTouchCount = cwTouchHook.GetMaxTouches();

        }   // end of Init()

        /// <summary>
        /// 
        /// </summary>
        /// <returns>True if the Touch input changed this frame.</returns>
        public static bool Update()
        {
            if (!TouchAvailable)
            {
                return false;
            }

            if (!LowLevelTouchInput.IgnoreTouchEvents && !LowLevelTouchInput.IgnoreUntilReleased)
            {
                LowLevelTouchInput.WasTouched = touchSampleList.Count > 0;

                // Strip out duplicates.
                for (int i = 0; i < touchSampleList.Count - 1; i++)
                {
                    if (touchSampleList[i] == touchSampleList[i + 1])
                    {
                        touchSampleList.RemoveAt(i + 1);
                    }

                    // If we get a Pressed event, clear TouchFocusObject.  We do
                    // this here so every single object doesn't have to do it itself.
                    // Basically, this is setting it up to be taken by the hit object.
                    // Widgets should still clear this on Release.
                    if (touchSampleList[i].State == TouchLocationState.Pressed)
                    {
                        KoiLibrary.InputEventManager.TouchFocusObject = null;
                    }
                }
                
                // Allows outside pre-processing of touches if needed.  In particular
                // the SceneManager will do hit testing on the touch locations and set
                // HitObjects on the touches.
                if (processTouches != null)
                {
                    processTouches(touchSampleList);
                }

                // Give gestures first look at touches.
                Gestures.ProcessTouchSamples(touchSampleList);

                // Then pass raw touches to InputEventManager.
                KoiLibrary.InputEventManager.ProcessTouchEvent(touchSampleList);

            }   // end if IgnoreTouchEvents

            if (LowLevelTouchInput.IgnoreUntilReleased && !LowLevelTouchInput.WasTouched)
            {
                LowLevelTouchInput.IgnoreUntilReleased = false;
            }

            // Swap with LowLevelTouchInput.Touches.
            var tmp = touchSampleList;
            touchSampleList = LowLevelTouchInput.Touches;
            LowLevelTouchInput.Touches = tmp;

            // Clear list for next frame.
            touchSampleList.Clear();

            return LowLevelTouchInput.WasTouched;

        }   // end of Update()

        #endregion

        #region Internal

        private static void TouchDownHandler(object sender, TouchEventArgs e)
        {
            // Touch locations are in device coords.  If we're windowed, need to adjust.
            Vector2 winPosition = KoiLibrary.ClientRect.Location.ToVector2();
            TouchSample ts = new TouchSample(e.id, TouchLocationState.Pressed, new Vector2(e.x, e.y) - winPosition);
            touchSampleList.Add(ts);
        }   // end of TouchDownHandler()

        private static void TouchMoveHandler(object sender, TouchEventArgs e)
        {
            // Touch locations are in device coords.  If we're windowed, need to adjust.
            Vector2 winPosition = KoiLibrary.ClientRect.Location.ToVector2();
            TouchSample ts = new TouchSample(e.id, TouchLocationState.Moved, new Vector2(e.x, e.y) - winPosition);
            touchSampleList.Add(ts);
        }   // end of TouchMoveHandler()

        private static void TouchUpHandler(object sender, TouchEventArgs e)
        {
            // Touch locations are in device coords.  If we're windowed, need to adjust.
            Vector2 winPosition = KoiLibrary.ClientRect.Location.ToVector2();
            TouchSample ts = new TouchSample(e.id, TouchLocationState.Released, new Vector2(e.x, e.y) - winPosition);
            touchSampleList.Add(ts);
        }   // end of TouchUpHandler()

        #endregion

    }   // end of class PCTouchInput

}   // end of namespace KoiX.Input
