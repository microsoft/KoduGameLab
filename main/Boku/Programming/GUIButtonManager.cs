// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.


using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Xml;
using System.Xml.Serialization;

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

    public static class GUIButtonManager
    {
        const float maxCompression = 1.5f;  // Max amount we allow text on label to be squished.
        const float splitThreshold = 1.25f; // Amount of compression we allow before splitting into 2 lines.

        #region Members

        static Texture2D buttonTexture;
        static Texture2D buttonPressedTexture;
        static Texture2D transparentTexture;

        static RenderTarget2D buttonPanelRT;
        static bool dirty = true;       // Do we need to refresh the RT?

        static TextBlob blob;
        static int margin = 16;         // Left/right margin for text on buttons.

        // List of all the button types to be handled.
        const int kNumButtons = (int)Classification.ColorInfo.Count;

        static bool initialized = false;

        static GUIButton[] buttons = new GUIButton[kNumButtons];

        static Vector2 panelOffset = new Vector2(16, 24);
        static Vector2 panelStartPosition = panelOffset;

        #endregion

        #region Accessors

        /// <summary>
        /// Has something changed requiring a refresh of the RT?
        /// Primarily used if label strings change.
        /// </summary>
        public static bool Dirty
        {
            get { return dirty; }
            set { dirty = value; }
        }

        #endregion

        #region Public
        
        public static void Init()
        {
            for (int i = 0; i < kNumButtons; i++)
            {
                Color color = Classification.XnaColor((Classification.Colors)((int)Classification.ColorInfo.First + i));
                buttons[i] = new GUIButton(color);
            }

            blob = new TextBlob(UI2D.Shared.GetGameFont20, "", (int)(GUIButton.DefaultSize.X - 2 * margin));
        }   // end of Init()

        public static void Update()
        {
            // Handles touch input and keeping button state
            TouchContact[] touches = TouchInput.Touches;
            Debug.Assert(null != touches);

            bool bActive = InGame.UpdateMode.RunSim == InGame.inGame.CurrentUpdateMode;

            bool bTouchMode = GamePadInput.InputMode.Touch == GamePadInput.ActiveMode;
            bool bMouseMode = GamePadInput.InputMode.KeyboardMouse == GamePadInput.ActiveMode;

            //Should we clear the mouse left button input so game doesn't receive
            bool bClearMouseInput = false;

            //See if we need to reset all button state.
            if (!bActive || GamePadInput.PreviousMode != GamePadInput.ActiveMode)
            {
                ResetButtonState();
            }

            if (bActive)
            {
                //Update Buttons.
                Vector2 cursor = panelStartPosition;

                for (int i = 0; i < kNumButtons; i++)
                {
                    GUIButton button = buttons[i];

                    if (button.Active)
                    {
                        // Set Hit Box.
                        button.HitBox.Set(button.Position, button.Position + GUIButton.DefaultSize);

                        // Update mouseOver.
                        button.MouseOver = false;
                        if (bMouseMode)
                        {
                            button.MouseOver = button.HitBox.Contains(MouseInput.PositionVec);
                        }

                        //No Finger ID Assigned means we want to press
                        if (button.FingerID < 0)
                        {
                            //Look for new finger press.
                            bool buttonDown = false;

                            //Process Inputs
                            if (bTouchMode)
                            {
                                //Ensure Button Is Not controlled by mouse or by other finger.
                                for (int j = 0; j < touches.Length; ++j)
                                {
                                    buttonDown = TouchPhase.Began == touches[j].phase && button.HitBox.Contains(touches[j].position);

                                    if (buttonDown)
                                    {
                                        button.FingerID = touches[j].fingerId;
                                        button.MouseControlled = false;
                                        break;
                                    }
                                }

                            }
                            else if (bMouseMode)
                            {
                                buttonDown = button.MouseOver && (MouseInput.Left.IsPressed || MouseInput.Right.IsPressed);

                                if (buttonDown)
                                {
                                    bClearMouseInput = true;
                                    button.FingerID = 0;
                                    button.MouseControlled = true;
                                    button.MouseLeftClick = MouseInput.Left.IsPressed;
                                }
                                else
                                {
                                    button.FingerID = -1;
                                    button.MouseControlled = false;
                                }
                            }

                            button.ChangeState(buttonDown ? ButtonState.Pressed : ButtonState.Released);
                        }
                        else
                        {
                            bool bReleasedButton = true;
                            bool bIntersect = false;

                            if (bTouchMode)
                            {
                                TouchContact touch = null;
                                //Try to find the touch with finger ID.
                                for (int j = 0; j < touches.Length; ++j)
                                {
                                    if (buttons[i].FingerID == touches[j].fingerId)
                                    {
                                        touch = touches[j];
                                        break;
                                    }
                                }

                                if (null != touch)
                                {
                                    bIntersect = button.HitBox.Contains(touch.position);
                                    bReleasedButton = TouchPhase.Ended == touch.phase;
                                }
                            }
                            else if (bMouseMode)
                            {
                                //Reset the left button when pressed to prevent game from receiving button press.
                                bClearMouseInput |= buttons[i].MouseControlled;

                                bIntersect = button.MouseOver;
                                bReleasedButton = !MouseInput.Left.IsPressed;
                            }


                            if (bIntersect)
                            {
                                button.ChangeState(bReleasedButton ? ButtonState.Released : ButtonState.Pressed);
                            }
                            else
                            {
                                button.ResetButton();
                            }

                            //When released reset any variables related to who pressed this button.
                            if (bReleasedButton)
                            {
                                button.FingerID = -1;
                            }
                        }

                    }   // end if Active
                }
            }

            //Clear mouse input if we intercepted.
            if (bClearMouseInput)
            {
                MouseInput.Left.Reset();
            }

            // If state changed, update the rendered buttons.
            RefreshRT();

        }   // end of Update()

        #endregion

        #region Internal



        public static void LoadContent(bool immediate)
        {
            Debug.Assert(!initialized, "GUIButtons was already initialized!");
            initialized = true;

            // Load button Textures.
            buttonTexture = BokuGame.Load<Texture2D>(BokuGame.Settings.MediaPath + @"Textures\Programming\TouchGUIButton");
            buttonPressedTexture = BokuGame.Load<Texture2D>(BokuGame.Settings.MediaPath + @"Textures\Programming\TouchGUIButton_Pressed");
            transparentTexture = BokuGame.Load<Texture2D>(BokuGame.Settings.MediaPath + @"Textures\BlackTransp");

            // Allocate rt for panel of all buttons.  This will hold all pre-rendered buttons, both pressed and non-pressed versions.
            buttonPanelRT = new RenderTarget2D(BokuGame.bokuGame.GraphicsDevice,
                (int)(GUIButton.DefaultSize.X * kNumButtons), (int)(GUIButton.DefaultSize.Y * 2),
                mipMap: false,
                preferredFormat: SurfaceFormat.Color,
                preferredDepthFormat: DepthFormat.None,
                preferredMultiSampleCount: 1,
                usage: RenderTargetUsage.PreserveContents); // <- This is the important bit here!
        }

        public static void UnloadContent()
        {
            BokuGame.Release(ref buttonTexture);
            BokuGame.Release(ref buttonPressedTexture);
            BokuGame.Release(ref transparentTexture);

            initialized = false;
        }

        public static void InitDeviceResources(GraphicsDevice device)
        {
        }

        public static void DeviceReset(GraphicsDevice device)
        {
        }

        #endregion






        public static bool IsOverUIButton(Vector2 pos)
        {
            if (null != buttons && InGame.UpdateMode.RunSim == InGame.inGame.CurrentUpdateMode)
            {
                for (int i = 0; i < buttons.Length; ++i)
                {
                    GUIButton button = buttons[i];
                    if (button != null && button.Active)
                    {
                        if (button.HitBox.Contains(pos))
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
        }


        public static GUIButton[] GetButtons()
        {
            return buttons;
        }

        public static GUIButton GetButton(Classification.Colors color)
        {
            int idx = GetButtonIdx(color);
            if (idx >= 0 && idx < kNumButtons)
            {
                return GetButton(idx);
            }
            return null;
        }

        public static GUIButton GetButton(int idx)
        {
            Debug.Assert(idx >= 0 && idx < kNumButtons);
            return buttons[idx];
        }

        public static int GetButtonIdx(Classification.Colors color)
        {
            Debug.Assert((int)color >= (int)Classification.ColorInfo.First && (int)color <= (int)Classification.ColorInfo.Last);
            int retIdx = (int)color - (int)Classification.ColorInfo.First;
            return (retIdx < 0) ? 0 : ((retIdx >= kNumButtons) ? kNumButtons - 1 : retIdx);
        }
        public static Classification.Colors GetColorFromButtonIdx(int idx)
        {
            Debug.Assert(idx >= 0 && idx < kNumButtons);
            return (Classification.Colors)((int)Classification.ColorInfo.First + idx);
        }

        public static void ClearAllButtonState()
        {
            for (int i = 0; i < kNumButtons; i++)
            {
                buttons[i].Active = false;
                buttons[i].Label = "";
            }
        }

        public static void ResetButtonState()
        {
            for (int i = 0; i < kNumButtons; i++)
            {
                buttons[i].ResetButton();
            }
        }

        /// <summary>
        /// Deactivates all buttons.  Doesn't affect any other state.
        /// </summary>
        public static void DeactivateAllButtons()
        {
            for (int i = 0; i < kNumButtons; i++)
            {
                buttons[i].Active = false;
            }
        }

        /// <summary>
        /// Renders the active buttons.  If too many are active to fit
        /// across the top of the screen we wrap to a second row.
        /// </summary>
        public static void Render()
        {
            GraphicsDevice device = BokuGame.bokuGame.GraphicsDevice;
            SpriteBatch batch = UI2D.Shared.SpriteBatch;

            int maxPerRow = (int)((BokuGame.ScreenSize.X - panelOffset.X) / GUIButton.DefaultSize.X);
            int numRendered = 0;

            // Only render the buttons during run time when we're not on pre or post game mode.
            if (InGame.UpdateMode.RunSim == InGame.inGame.CurrentUpdateMode &&
                (InGame.inGame.PreGame == null || InGame.inGame.PreGame.Active == false) &&
                (!VictoryOverlay.Active && !InGame.inGame.DialogActive))
            {
                Vector2 pos = panelOffset;
                batch.Begin();
                for (int i = 0; i < kNumButtons; i++)
                {
                    if (buttons[i].Active)
                    {
                        Rectangle srcRect = new Rectangle((int)(i * GUIButton.DefaultSize.X), 0, (int)GUIButton.DefaultSize.X, (int)GUIButton.DefaultSize.Y);
                        if (buttons[i].ButtonState == ButtonState.Pressed)
                        {
                            srcRect.Y += (int)GUIButton.DefaultSize.Y;
                        }
                        Rectangle dstRect = new Rectangle((int)pos.X, (int)pos.Y, (int)GUIButton.DefaultSize.X, (int)GUIButton.DefaultSize.Y);
                        batch.Draw(buttonPanelRT, dstRect, srcRect, Color.White);
                        buttons[i].Position = pos;

                        ++numRendered;
                        pos.X += GUIButton.DefaultSize.X;
                        if (numRendered == maxPerRow)
                        {
                            pos = panelOffset;
                            pos.Y += GUIButton.DefaultSize.Y;
                        }
                    }
                }
                batch.End();
            }

        }   // end of Render()

        public static void RefreshRT()
        {
            if (Dirty && buttonTexture != null)
            {
                GraphicsDevice device = BokuGame.bokuGame.GraphicsDevice;
                SpriteBatch batch = UI2D.Shared.SpriteBatch;

                InGame.SetRenderTarget(buttonPanelRT);

                // If all butttons are dirty (start of game), just clear the whole RT.
                // Else cleart the individual spaces.
                bool allButtonsDirty = true;
                foreach (GUIButton button in buttons)
                {
                    allButtonsDirty &= button.Dirty;
                }

                if (allButtonsDirty)
                {
                    InGame.Clear(Color.Transparent);
                }
                else
                {
                    batch.Begin(SpriteSortMode.Texture, BlendState.Opaque);
                    {
                        for (int i = 0; i < buttons.Length; i++)
                        {
                            if (buttons[i].Dirty)
                            {
                                // Draw over both pressed and unpressed at once.
                                Rectangle dstRect = new Rectangle((int)(i * GUIButton.DefaultSize.X), 0, (int)GUIButton.DefaultSize.X, (int)(GUIButton.DefaultSize.Y * 2));
                                batch.Draw(transparentTexture, dstRect, Color.Transparent);
                            }
                        }
                    }
                    batch.End();
                }

                //
                // Draw main button shapes.
                //
                batch.Begin(SpriteSortMode.Texture, BlendState.AlphaBlend);
                {
                    // Render underlying buttons.
                    for (int i = 0; i < buttons.Length; i++)
                    {
                        if (buttons[i].Dirty)
                        {
                            // Normal state.
                            Rectangle rect = new Rectangle((int)(i * GUIButton.DefaultSize.X), 0, (int)GUIButton.DefaultSize.X, (int)GUIButton.DefaultSize.Y);
                            batch.Draw(buttonTexture, rect, buttons[i].Color);

                            // Pressed state.
                            rect.Y = (int)GUIButton.DefaultSize.Y;
                            batch.Draw(buttonPressedTexture, rect, buttons[i].Color);
                        }
                    }
                }
                batch.End();

                // Render labels on top of buttons.
                for (int i = 0; i < kNumButtons; i++)
                {
                    if (buttons[i].Dirty && !string.IsNullOrEmpty(buttons[i].Label))
                    {
                        buttons[i].LabelFits = RenderLabelButton(i);

                        buttons[i].Dirty = false;
                    }   // end of label rendering.

                }   // end of loop over buttons.

                InGame.SetRenderTarget(null);

                Dirty = false;
            }

        }   // end of RefreshRT()

        /// <summary>
        /// Renders the buttons label onto the button textures.
        /// 
        /// </summary>
        /// <param name="buttonIndex"></param>
        /// <returns>True if label fits, false if it's too big.</returns>
        static bool RenderLabelButton(int buttonIndex)
        {
            bool fits = true;
            string line1 = buttons[buttonIndex].Label;
            string line2 = null;

            UI2D.Shared.GetFont Font = UI2D.Shared.GetGameFont24;
            int margin = 8;
            int width = 128 - 2 * margin;
            Vector2 size1 = Font().MeasureString(line1);
            Vector2 size2 = Vector2.Zero;

            // Need to split into 2 lines?
            if (size1.X > width * splitThreshold)
            {
                SplitString(Font, buttons[buttonIndex].Label, out line1, out line2);

                if (line2 != null)
                {
                    size1 = Font().MeasureString(line1);
                    size2 = Font().MeasureString(line2);
                }
            }

            // Calc position.  This assumes a single line.
            Vector2 pos = new Vector2(margin + buttonIndex * GUIButton.DefaultSize.X, (int)((GUIButton.DefaultSize.Y - size1.Y) / 2.0f));
            // Adjust if we've got 2 lines.
            if (line2 != null)
            {
                pos.Y -= (int)(Font().LineSpacing / 2.0f);
            }

            SysFont.StartBatch(null);
            {
                fits &= RenderLine(Font, pos, line1, size1, width);
                if (line2 != null)
                {
                    pos.Y += Font().LineSpacing;
                    fits &= RenderLine(Font, pos, line2, size2, width);
                }
            }
            SysFont.EndBatch();

            return fits;
        }   // end of LabelButton()

        /// <summary>
        /// Returns true if line fits with reasonable compression.  False if compressed too much.
        /// </summary>
        /// <param name="Font"></param>
        /// <param name="pos"></param>
        /// <param name="line"></param>
        /// <param name="size"></param>
        /// <param name="width"></param>
        /// <returns></returns>
        static bool RenderLine(UI2D.Shared.GetFont Font, Vector2 pos, string line, Vector2 size, int width)
        {
            Vector2 scale = Vector2.One;
            if (size.X > width)
            {
                // Need to compress.
                scale.X = width / size.X;
            }
            else
            {
                // No compression, just center.
                pos.X += (int)((width - size.X) / 2.0f);
            }

            RectangleF rect = new RectangleF(pos.X, pos.Y, width, 96);

            SysFont.DrawString(line, pos, rect, Font().systemFont, Color.Black, scale, outlineColor: Color.White, outlineWidth: 1.5f);
            pos.Y += GUIButton.DefaultSize.Y;
            rect.Y += GUIButton.DefaultSize.Y;
            SysFont.DrawString(line, pos, rect, Font().systemFont, Color.Black, scale, outlineColor: Color.White, outlineWidth: 1.5f);

            return scale.X > 0.5f;
        }   // end of RenderLine()

        /// <summary>
        /// Tries to split the input string into 2 strings as balanced as possible.
        /// In this case "balanced" means trying to make each substring as close as
        /// possible to the same length.
        /// </summary>
        /// <param name="Font"></param>
        /// <param name="input"></param>
        /// <param name="line1">First substring.</param>
        /// <param name="line2">Second substring.  May be null.</param>
        static void SplitString(UI2D.Shared.GetFont Font, string input, out string line1, out string line2)
        {
            input = input.Trim();   // Clean off whitespace since it can throw off calculations.
            line1 = input;
            line2 = null;

            int bestIndex = 0;
            float bestRatio = float.MaxValue;   // Ratio of substring lengths.  Always flipped so ration >= 1.0.  Lower values are better.

            int index = -1;
            while (true)
            {
                index = input.IndexOf(' ', index + 1);
                if (index == -1)
                {
                    // No more spaces found.
                    break;
                }

                // Split the line base on the space at i.
                line1 = input.Substring(0, index);
                line2 = input.Substring(index + 1);

                float ratio = Font().MeasureString(line1).X / Font().MeasureString(line2).X;
                if (ratio < 1)
                {
                    ratio = 1 / ratio;
                }

                if (ratio < bestRatio)
                {
                    bestRatio = ratio;
                    bestIndex = index;
                }
            }

            if (bestIndex != -1)
            {
                // Split the string based on the best index.
                line1 = input.Substring(0, bestIndex);
                line2 = input.Substring(bestIndex + 1);
            }

        }   // end of SplitString()

        /// <summary>
        /// Tests whether the given string will fit as a label.
        /// </summary>
        /// <param name="label"></param>
        /// <returns></returns>
        public static bool TestLabelFit(string label)
        {
            bool fits = true;
            string line1 = label;
            string line2 = null;

            UI2D.Shared.GetFont Font = UI2D.Shared.GetGameFont24;
            int margin = 8;
            int width = 128 - 2 * margin;
            Vector2 size1 = Font().MeasureString(line1);
            Vector2 size2 = Vector2.Zero;

            // Need to split into 2 lines?
            if (size1.X > width * splitThreshold)
            {
                SplitString(Font, label, out line1, out line2);

                if (line2 != null)
                {
                    size1 = Font().MeasureString(line1);
                    size2 = Font().MeasureString(line2);
                }
            }

            if ((size1.X > maxCompression * width) || (size2.X > maxCompression * width))
            {
                fits = false;
            }

            return fits;
        }   // end of TestLabelFit()

        /// <summary>
        /// Used to preview button layout when editing label.
        /// </summary>
        /// <param name="button"></param>
        /// <param name="center"></param>
        /// <param name="text"></param>
        public static void RenderButtonPreview(GUIButton button, Vector2 center, string text)
        {
            int buttonIndex = -1;
            for (int i = 0; i < buttons.Length; i++)
            {
                if (buttons[i] == button)
                {
                    buttonIndex = i;
                    break;
                }
            }
            Debug.Assert(buttonIndex != -1);

            button.Label = text;

            Rectangle srcRect = new Rectangle(buttonIndex * (int)GUIButton.DefaultSize.X, 0, (int)GUIButton.DefaultSize.X, (int)GUIButton.DefaultSize.Y);
            center -= GUIButton.DefaultSize / 2.0f;
            Rectangle dstRect = new Rectangle((int)center.X, (int)center.Y, (int)GUIButton.DefaultSize.X, (int)GUIButton.DefaultSize.Y);
            SpriteBatch batch = UI2D.Shared.SpriteBatch;
            batch.Begin();
            batch.Draw(buttonPanelRT, dstRect, srcRect, Color.White);
            batch.End();
        }   // end of RenderButtonPreview()

    }   // end of class GUIButtonManager

}   // end of namespace Boku.Programming
