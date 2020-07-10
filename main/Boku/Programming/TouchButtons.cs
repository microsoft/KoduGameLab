// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Text;

using System.Xml;
using System.Xml.Serialization;
using System.Diagnostics;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Storage;

using Boku.Base;
using Boku.Common;
using Boku.Common.Gesture;
using Boku.Fx;
using Boku.UI;
using Boku.UI2D;
using Boku.Common.Xml;

namespace Boku.Programming
{
    /// <summary>
    /// The primary responsibility of this class is to render and maintain the internal state of
    /// any buttons required by Kode containing TouchButtonFilter tiles. When any such filter exists, we 
    /// need to draw the button on the screen and respond to touches by the user. The TouchButtonFilters 
    /// will be querying this class to find out if their button conditions have been met.
    /// </summary>
    public class TouchButtons
    {
        private static int numButtons = (int)TouchButtonFilter.TouchButtonType.SIZEOF;

        private static AABB2D[] touchButtonBoxes = new AABB2D[numButtons];
        private static Texture[] touchButtonTextures = new Texture[numButtons];
        private static ButtonState[] touchButtonState = new ButtonState[numButtons];
        public static bool[] touchButtonVisibility = new bool[numButtons];

        private static bool isInitialized = false;
        private static Vector2 defaultButtonSize = new Vector2(128, 128);

        public enum ButtonState
        {
            Normal,
            Focus,
            Pressed
        }

        /// <summary>
        /// Returns the state of a given touch button type.
        /// </summary>
        public static ButtonState GetButtonState(TouchButtonFilter.TouchButtonType buttonType)
        {
            return touchButtonState[(int)buttonType];
        }

        /// <summary>
        /// Sets the touch button as visible for this frame. This must be called on a given button type
        /// every frame to keep the button visible.
        /// </summary>
        public static void MakeVisible(TouchButtonFilter.TouchButtonType buttonType)
        {
            touchButtonVisibility[(int)buttonType] = true;
        }

        public static void LoadContent(bool immediate)
        {
            Debug.Assert(!isInitialized, "TouchButtons was already initialized!");
            isInitialized = true;

            for (int i = 0; i < numButtons; i++)
            {
                touchButtonBoxes[i] = new AABB2D();
            }

            touchButtonTextures[0] = BokuGame.Load<Texture2D>(
                BokuGame.Settings.MediaPath + @"Textures\Programming\TouchButton1");

            touchButtonTextures[1] = BokuGame.Load<Texture2D>(
                BokuGame.Settings.MediaPath + @"Textures\Programming\TouchButton2");

            touchButtonTextures[2] = BokuGame.Load<Texture2D>(
                BokuGame.Settings.MediaPath + @"Textures\Programming\TouchButton3");

            touchButtonTextures[3] = BokuGame.Load<Texture2D>(
                BokuGame.Settings.MediaPath + @"Textures\Programming\TouchButton4");

            //touchButtonTextures[4] = BokuGame.Load<Texture2D>(
            //    BokuGame.Settings.MediaPath + @"Textures\Programming\TouchButton5");

            //touchButtonTextures[5] = BokuGame.Load<Texture2D>(
            //    BokuGame.Settings.MediaPath + @"Textures\Programming\TouchButton6");
        }

        public static void UnloadContent()
        {
            isInitialized = false;
            for (int i = 0; i < numButtons; i++)
            {
                BokuGame.Release(ref touchButtonTextures[i]);
            }
        }

        public static void InitDeviceResources(GraphicsDeviceManager graphics)
        {
        }

        public static void DeviceReset(GraphicsDeviceManager graphics)
        {
        }

        public static void Update()
        {
            if (InGame.inGame.CurrentUpdateMode != InGame.UpdateMode.RunSim)
            {
                return;
            }

            // All buttons are assumed to be invisible at the start every frame. Only when an active filter
            // from a brain attempts to read the state of the button does a button become visible.
            for (int i = 0; i < numButtons; i++)
            {
                touchButtonVisibility[i] = false;
            }

            // Handles touch input and keeping button state
            TouchContact touch = TouchInput.GetOldestTouch();
            if ((GamePadInput.ActiveMode != GamePadInput.InputMode.Touch) || (touch == null))
            {
                for (int i = 0; i < numButtons; i++)
                {
                    touchButtonState[i] = ButtonState.Normal;
                }
            }
            else
            {
                Vector2 touchHit = touch.position;
                for (int i = 0; i < numButtons; i++)
                {
                    if (touchButtonBoxes[i].Contains(touchHit))
                    {
                        // Touch is over this button.
                        if (touch.phase == TouchPhase.Ended)
                        {
                            touchButtonState[i] = ButtonState.Pressed;
                        }
                        else
                        {
                            touchButtonState[i] = ButtonState.Focus;
                        }
                    }
                    else
                    {
                        touchButtonState[i] = ButtonState.Normal;
                    }
                }
            }
        }

        public static void Render()
        {
            if (InGame.inGame.CurrentUpdateMode != InGame.UpdateMode.RunSim)
            {
                return;
            }

            float scale = Math.Min((float)BokuGame.bokuGame.GraphicsDevice.Viewport.Height / 1024.0f, 1.0f);
            int center = BokuGame.bokuGame.GraphicsDevice.Viewport.Width / 2;
            float overscan = BokuGame.bokuGame.GraphicsDevice.Viewport.Height * 
                XmlOptionsData.OverscanPercent / 200.0f;
            ScreenSpaceQuad quad = ScreenSpaceQuad.GetInstance();

            Vector2 buttonSize = defaultButtonSize * scale;

            // We adjust for widescreen to keep the buttons close to the far right of the screen
            float startXPos = 0;
            if (BokuGame.IsWidescreen)
            {
                startXPos = 725 * scale * BokuGame.Graphics.GraphicsDevice.Viewport.AspectRatio;
            }
            else
            {
                startXPos = 650 * scale * BokuGame.Graphics.GraphicsDevice.Viewport.AspectRatio;
            }
            float startYPos = 600 * scale;
            float xSpacing = 150 * scale;
            float ySpacing = 150 * scale;

            Vector2[] buttonsPos = new Vector2[numButtons];

            for (int i = 0; i < numButtons; i++)
            {
                if (!touchButtonVisibility[i]) { continue; }

                Vector4 drawColor = GetDrawColor((uint)i);
                buttonsPos[i] = new Vector2(startXPos, startYPos);
                buttonsPos[i].X += xSpacing * (i % 2);
                buttonsPos[i].Y += ySpacing * (i / 2);
                buttonsPos[i].Y += overscan;
                touchButtonBoxes[i].Set(buttonsPos[i], buttonsPos[i] + buttonSize);

                quad.Render(touchButtonTextures[i], drawColor, buttonsPos[i], buttonSize, 
                    "TexturedRegularAlpha");
            }
        }

        private static Vector4 GetDrawColor(uint index)
        {
            if (index < numButtons)
            {
                if (touchButtonState[index] == ButtonState.Normal)
                {
                    return new Vector4(1.0f, 1.0f, 1.0f, 1.0f);
                }
                else if (touchButtonState[index] == ButtonState.Focus)
                {
                    return new Vector4(0.7f, 0.7f, 0.7f, 0.7f);
                }
                else if (touchButtonState[index] == ButtonState.Pressed)
                {
                    return new Vector4(0.7f, 0.7f, 0.7f, 0.4f);
                }
            }
            return new Vector4(0.0f, 0.0f, 0.0f, 0.0f);
        }
    }

}
