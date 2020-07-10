// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.


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

namespace KoiX.Input
{
    public class PhoneTouchInput
    {
        #region Members

        static List<TouchSample> tc;  // List we pass to consumers.

        #endregion

        #region Accessors
        #endregion

        // c'tor
        PhoneTouchInput()
        {
        }   // end of c'tor

        /// <summary>
        /// One time call to set up Touch input functionality.
        /// </summary>
        public static void Init()
        {
            tc = new List<TouchSample>();
        }   // end of Init()

        /// <summary>
        /// Must be called once per frame to update the current state of the Touch input handling.
        /// When events are detected, also calls any registered event handlers.
        /// 
        /// NOTE: The TouchPanel code only works with phone, not Windows.  Need to hook
        /// WM_TOUCH messages those.
        /// </summary>
        /// <returns>True if the Touch input changed this frame.</returns>
        public static bool Update()
        {
            var caps = TouchPanel.GetCapabilities();
            if (!caps.IsConnected)
            {
                return false;
            }

            TouchCollection collection = TouchPanel.GetState();
            LowLevelTouchInput.WasTouched = collection.Count > 0;

            if (!LowLevelTouchInput.IgnoreTouchEvents && !LowLevelTouchInput.IgnoreUntilReleased)
            {
                // Create local, mutable list of TouchLocations.
                tc.Clear();
                foreach (TouchLocation tl in collection)
                {
                    // Filter out Invalid ones.  No clue what to do with them anyway...
                    if (tl.State != TouchLocationState.Invalid)
                    {
                        TouchSample ts = new TouchSample(tl);
                        tc.Add(ts);
                    }
                }

                // Give gestures first look at touches.
                Gestures.ProcessTouchSamples(tc);

                // Then pass raw touches to InputEventManager.
                KoiLibrary.InputEventManager.ProcessTouchEvent(tc);

            }   // end if IgnoreTouchEvents

            if (LowLevelTouchInput.IgnoreUntilReleased && !LowLevelTouchInput.WasTouched)
            {
                LowLevelTouchInput.IgnoreUntilReleased = false;
            }

            return LowLevelTouchInput.WasTouched;
        }   // end of Update()

    }   // end of class PhoneTouchInput

}   // end of namespace KoiX.Input
