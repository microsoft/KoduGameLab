// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.


using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Xml.Serialization;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;

using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Storage;

using KoiX;
using KoiX.Input;
using KoiX.Text;

using Boku.Base;
using Boku.Fx;
using Boku.Common;
using Boku.Common.Gesture;
using Boku.Common.Xml;

namespace Boku.Common.HintSystem
{
    /// <summary>
    /// Provides a single entry point for creating Toasts.  
    /// </summary>
    public class ToastManager
    {
        private static double fadeInTime = 0.5;
        private static double fadeOutTime = 1.0;
        private static double holdTime = 5.0f;

        private static double startShowTime = 0;    // Time when ShowToast was called.

        private static string curToast = null;
        private static bool checkForModal = false;  // Are we checking for the user activating the modal display?
        private static TextBlob blob = null;
        private static float alpha = 0.0f;

        private static Vector2 targetPosition;      // Where we want to render the toast.
        private static Vector2 curPosition;         // Where we're actually rendering it.

        private static Texture2D texture = null;
        private static Texture2D background = null;

        private static bool dirty = true;           // Texture2D needs refreshing.

        private static int margin = 20;             // Margin for text in pixels.
        private static Vector2 size;                // Size to render at in pixels.
        private static bool showYButton = false;    // Show the Y button at the bottom?

        #region Accessors

        public static bool Active
        {
            get { return curToast != null; }
        }

        #endregion

        // c'tor
        private ToastManager()
        {
        }

        public static void Init()
        {
        }   // end of c'tor

        /// <summary>
        /// Updates the toast system.
        /// </summary>
        /// <returns>True if the user has indicated that the modal display should be activated.  (clicking on the toast or pressing Y)</returns>
        public static bool Update()
        {
            double secs = Time.WallClockTotalSeconds;

            alpha = 0.0f;
            if (curToast != null)
            {
                // Find where we are in fade-in, hold, fade-out cycle.
                double elapsedTime = Time.WallClockTotalSeconds - startShowTime;

                if (dirty)
                {
                    RefreshTexture();
                }

                if (elapsedTime < fadeInTime)
                {
                    // Fading in.
                    alpha = (float)(elapsedTime / fadeInTime);
                }
                else if (elapsedTime <= fadeInTime + holdTime)
                {
                    alpha = 1.0f;
                }
                else
                {
                    // Fading out.
                    alpha = 1.0f - (float)((elapsedTime - fadeInTime - holdTime) / fadeOutTime);
                    if (alpha < 0.0f)
                    {
                        alpha = 0.0f;
                        curToast = null;
                    }
                }
            }

            bool result = false;

            if (KoiLibrary.LastTouchedDeviceIsKeyboardMouse)
            {
                if (LowLevelMouseInput.Left.WasPressed)
                {
                    // Did this hit the toast?
                    if (LowLevelMouseInput.Position.X > curPosition.X
                        && LowLevelMouseInput.Position.X < curPosition.X + size.X
                        && LowLevelMouseInput.Position.Y > curPosition.Y
                        && LowLevelMouseInput.Position.Y < curPosition.Y + size.Y)
                    {
                        LowLevelMouseInput.Left.ClearAllWasPressedState();
                        Clear();    // Turn off this toast
                        result = true;
                    }
                }

                // F1 pressed?
                if (KeyboardInput.WasPressed(Keys.F1))
                {
                    LowLevelMouseInput.Left.ClearAllWasPressedState();
                    Clear();    // Turn off this toast
                    result = true;
                }
            }
            else if (KoiLibrary.LastTouchedDeviceIsTouch)
            {
                TapGestureRecognizer tapGesture = TouchGestureManager.Get().TapGesture;
                if (tapGesture.WasRecognized)
                {
                    // Did this hit the toast?
                    if (tapGesture.Position.X > curPosition.X
                        && tapGesture.Position.X < curPosition.X + size.X
                        && tapGesture.Position.Y > curPosition.Y
                        && tapGesture.Position.Y < curPosition.Y + size.Y)
                    {
                        Clear();    // Turn off this toast
                        result = true;
                    }
                }
            }
            else
            {
                // We can't steal input when in run mode since the user may
                // have mapped the Y button for game play.
                if (InGame.inGame.CurrentUpdateMode != InGame.UpdateMode.RunSim)
                {
                    GamePadInput pad = GamePadInput.GetGamePad0();
                    if (pad.ButtonY.WasPressed)
                    {
                        pad.ButtonY.ClearAllWasPressedState();
                        Clear();    // Turn off this toast
                        result = true;
                    }
                }            
            }

            return result;

        }   // end of ToastManager Update()

        public static void Render()
        {
            if (alpha > 0.0f)
            {
                ScreenSpaceQuad ssquad = ScreenSpaceQuad.GetInstance();

                // Animate in from the bottom as well as fade in/out.
                Vector4 color = new Vector4(1, 1, 1, alpha);
                curPosition = targetPosition;
                float t = 1.0f - alpha;
                t = TwitchCurve.Apply(t, TwitchCurve.Shape.EaseInOut);
                curPosition.Y += t * size.Y;
                ssquad.Render(texture, color, curPosition, size, "TexturedRegularAlphaNoZ");
            }

        }   // end of ToastManager Render()

        /// <summary>
        /// Signals the tool toast manager to create a new toast.
        /// If another toast is already being displayed then this is ignored.
        /// Note that when in RunSim mode the option to get a modal display via the Y button is disabled since this may conflict with gameplay.
        /// </summary>
        /// <param name="toastText">The text to be displayed on the toast.</param>
        /// <param name="checkForModal">True if we want the system to check for switching to modal display.</param>
        public static void ShowToast(string toastText, bool checkForModal)
        {
            if (!XmlOptionsData.ShowHints || toastText == null)
                return;

            // Already showing one?
            if (curToast != null)
            {
                return;
            }

            // If input is showing a dialog, something got unplugged so
            // don't show toast.
            if (GamePadInput.DialogActive)
            {
                return;
            }

            // Clean up string.
            toastText = toastText.Trim();
            curToast = TextHelper.FilterInvalidCharacters(toastText);

            if (curToast == null || curToast.Length == 0)
            {
                curToast = null;
                return;
            }

            ToastManager.checkForModal = checkForModal;

            // If we have a modal display and we're using the gamepad and we're
            // not in RunSim mode then add text to the display string indicating
            // that the Y button can be used for more information.  In mouse
            // keyboard mode, just at the F1 key.
            if (ToastManager.checkForModal)
            {
                if (KoiLibrary.LastTouchedDeviceIsGamepad)
                {
                    if (InGame.inGame.CurrentUpdateMode != InGame.UpdateMode.RunSim)
                    {
                        curToast += "\n\n<ybutton>  " + Strings.Localize("toast.yButtonMore");
                    }
                }
                else if (KoiLibrary.LastTouchedDeviceIsKeyboardMouse)
                {
                    curToast += "\n\n[F1]  " + Strings.Localize("toast.yButtonMore");
                }
            }

            // Create the texture to display.
            blob = new TextBlob(SharedX.GetGameFont24, curToast, 512 - margin * 2);

            startShowTime = Time.WallClockTotalSeconds;

            dirty = true;

        }   // end of ShowToast()

        public static void Clear()
        {
            // Adjust the start time so that we start fading immediately.
            double t = Time.WallClockTotalSeconds - fadeInTime - holdTime;
            if (t < startShowTime)
            {
                startShowTime = t;
            }
        }   // end of Clear()

        private static void RefreshTexture()
        {
            // TODO (****) *** Does this make sense any more since we require a min height of 600?
            bool lores = BokuGame.ScreenSize.Y <= 480;

            GetFont Font = SharedX.GetGameFont24Bold;
            if (lores)
            {
                Font = SharedX.GetGameFont30Bold;
            }
            blob.Font = Font;

            ScreenSpaceQuad ssquad = ScreenSpaceQuad.GetInstance();
            RenderTarget2D rt = SharedX.RenderTarget512_302;

            InGame.SetRenderTarget(rt);

            InGame.Clear(Color.Transparent);

            ssquad.Render(background, Vector2.Zero, new Vector2(512, 302), "TexturedRegularAlpha");

            // Text description.
            if (blob != null)
            {
                Vector2 pos = new Vector2(margin, margin);
                int maxLines = showYButton ? (lores ? 3 : 4) : (lores ? 4 : 6);

                // Modify final line to end with ellipsis.
                blob.AddEllipsisToLine(maxLines - 1);

                // If less than maxLines of text, center on texture.
                int spareLines = maxLines - blob.NumLines - 1;
                if (spareLines > 0)
                {
                    pos.Y += spareLines * 0.5f * Font().LineSpacing;
                }

                blob.RenderText(null, pos, Color.Yellow, maxLines: maxLines);
            }


            InGame.RestoreRenderTarget();

            //
            // Copy result to local texture.
            //
            int[] data = new int[512 * 302];
            rt.GetData<int>(data);
            texture.SetData<int>(data);

            // Scale size to 1/4 screen height.
            int w = (int)BokuGame.ScreenSize.X;
            int h = (int)BokuGame.ScreenSize.Y;
            float scale = h / 4.0f / 302.0f;
            size = new Vector2(512, 302);
            size *= scale;

            // Position in lower right hand corner.
            targetPosition = new Vector2(w, h);
            targetPosition -= size;

            dirty = false;

        }   // end of RefreshTexture()

        public static void LoadContent(bool immediate)
        {
            // Load the background texture.
            if (background == null)
            {
                background = KoiLibrary.LoadTexture2D(@"Textures\HelpCard\ToolTip");
            }

        }   // end of LoadContent()

        public static void InitDeviceResources(GraphicsDevice device)
        {
            // Create the texture.
            if (texture == null)
            {
                texture = new Texture2D(device, 512, 302, false, SurfaceFormat.Color);
            }

        }   // end of InitDeviceResources()

        public static void UnloadContent()
        {
            DeviceResetX.Release(ref texture);
            DeviceResetX.Release(ref background);
        }   // end of UnloadContent()

        /// <summary>
        /// Recreate render targets.
        /// </summary>
        public static void DeviceReset(GraphicsDevice device)
        {
        }

    }   // end of class ToastManager

}   // end of namespace Boku.Common.HintSystem
