// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.


using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Storage;


using Boku.Base;
using Boku.Common;
using Boku.Common.HintSystem;
using Boku.Common.Xml;
using Boku.UI;
using Boku.UI2D;
using Boku.Input;
using Boku.Programming;
using Boku.SimWorld;
using Boku.Web;
using Boku.Fx;

using Boku.Audio;
using BokuShared;

namespace Boku
{
    /// <summary>
    /// Modal hint display.  Used for hints that are short enough to fit without scrolling.
    /// </summary>
    public class ModalHint : INeedsDeviceReset
    {
        #region Members

        private enum States
        {
            Inactive,
            Active,
        }
        private States state = States.Inactive;

        private BaseHint curHint = null;

        private CommandMap commandMap = new CommandMap("ModalHint");

        private Texture2D backgroundTexture = null;  // The background frame we render over.

        private bool useBackgroundThumbnail = true;
        private Texture2D thumbnail = null;         // Scene image for background unless the above is false.

        private bool useRtCoords = false;           // True if this dialog is being rendered to a rendertarget.

        // Text color for button label.
        private Color labelAColor = new Color(191, 191, 191);
        private Color labelBColor = new Color(191, 191, 191);
        // Color targetted by the twitch.  Used for comparisons so
        // we know whether or not to start a new twitch.
        private Color labelATargetColor = Color.Gray;
        private Color labelBTargetColor = Color.Gray;

        // Color constants.
        private Color lightTextColor = new Color(191, 191, 191);
        private Color hoverTextColor = new Color(50, 255, 50);

        private TextBlob blob = null;
        private AABB2D hitBoxA = new AABB2D();          // Mouse hit region for <A> Continue.
        private AABB2D hitBoxB = new AABB2D();          // Mouse hit region for <X> Never show me this again.

        private Vector2 textPosition = new Vector2(20, 20);
        private int textWidth = 512 - 40;

        private int maxLines = 7;           // Max lines we can display.

        private Vector2 renderPosition;     // Where to render the rendertarget on the backbuffer.
        //private float renderScale = 1.0f;   // Scale used to shrink rendertarget if needed.  SHould always be <= 1.0.

        private bool prevRenderWorldAsThumbnail = false;

        #endregion

        #region Accessors

        private UI2D.Shared.GetFont Font
        {
            get { return UI2D.Shared.GetGameFont20; }
        }

        public bool Active
        {
            get { return (state == States.Active); }
        }

        public bool Overflow
        {
            get { return blob.NumLines > maxLines; }
        }

        public bool UseBackgroundThumbnail
        {
            get { return useBackgroundThumbnail; }
        }

        #endregion

        #region Public

        // c'tor
        public ModalHint()
        {
        }   // end of c'tor

        public void Update(Camera camera)
        {
            if (Active)
            {
                GamePadInput pad = GamePadInput.GetGamePad0();

                if (InGame.inGame.State == InGame.States.Active && InGame.inGame.CurrentUpdateMode == InGame.UpdateMode.RunSim)
                {
                    // We need to be able to slip out to the mini-hub here since
                    // continuous, repeated calls to ModalHint can lock the 
                    // user out of control.
                    if (Actions.MiniHub.WasPressed)
                    {
                        Actions.MiniHub.ClearAllWasPressedState();

                        Deactivate();
                        InGame.inGame.SwitchToMiniHub();
                    }

                    // We need to be able to slip out to the tool menu here since
                    // continuous, repeated calls to ModalHint can lock the 
                    // user out of control.
                    if (Actions.ToolMenu.WasPressed)
                    {
                        Actions.ToolMenu.ClearAllWasPressedState();

                        Deactivate();
                        InGame.inGame.CurrentUpdateMode = InGame.UpdateMode.ToolMenu;
                    }
                }

                if (Actions.Select.WasPressed)
                {
                    Actions.Select.ClearAllWasPressedState();

                    // Disable this hint for this session.
                    if (curHint.ShowOnce)
                    {
                        curHint.Disabled = true;
                    }

                    Deactivate();
                }
                if (Actions.X.WasPressed)
                {
                    Actions.X.ClearAllWasPressedState();

                    // Disable this hint until reset by user.
                    XmlOptionsData.SetHintAsDisabled(curHint.ID);

                    // Disable this hint for this session.
                    if (curHint.ShowOnce)
                    {
                        curHint.Disabled = true;
                    }

                    Deactivate();
                }

                if (useBackgroundThumbnail)
                {
                    camera = new PerspectiveUICamera();
                    camera.Resolution = new Point(1280, 720);
                }

                if (GamePadInput.ActiveMode == GamePadInput.InputMode.Touch)
                {
                    for (int i = 0; i < TouchInput.TouchCount; i++)
                    {
                        TouchContact touch = TouchInput.GetTouchContactByIndex(i);

                        Vector2 touchHit = touch.position;
                        if (useRtCoords)
                        {
                            touchHit = ScreenWarp.ScreenToRT(touch.position);
                        }
                        HandleTouchInput(touch, touchHit);
                    }
                }
                else if (GamePadInput.ActiveMode == GamePadInput.InputMode.KeyboardMouse)
                {
                    Vector2 hit = MouseInput.PositionVec;
                    if (useRtCoords)
                    {
                        hit = MouseInput.GetMouseInRtCoords();
                    }
                    HandleMouseInput(hit);
                }

            }   // end if active.

        }   // end of Update()

        private void HandleTouchInput(TouchContact touch, Vector2 hit)
        {
            if (hitBoxA.Touched(touch, hit))
            {
                // Disable this hint for this session.
                if (curHint.ShowOnce)
                {
                    curHint.Disabled = true;
                }

                Deactivate();
            }
            else if (hitBoxB.Touched(touch, hit))
            {
                // Disable this hint until reset by user.
                XmlOptionsData.SetHintAsDisabled(curHint.ID);

                // Disable this hint for this session.
                if (curHint.ShowOnce)
                {
                    curHint.Disabled = true;
                }

                Deactivate();
            }

            // Check for hover and adjust text color to match.
            Color newColor;

            newColor = hitBoxA.Contains(hit) ? hoverTextColor : lightTextColor;
            if (newColor != labelATargetColor)
            {
                labelATargetColor = newColor;
                Vector3 curColor = new Vector3(labelAColor.R / 255.0f, labelAColor.G / 255.0f, labelAColor.B / 255.0f);
                Vector3 destColor = new Vector3(newColor.R / 255.0f, newColor.G / 255.0f, newColor.B / 255.0f);

                TwitchManager.Set<Vector3> set = delegate(Vector3 value, Object param)
                {
                    labelAColor.R = (byte)(value.X * 255.0f + 0.5f);
                    labelAColor.G = (byte)(value.Y * 255.0f + 0.5f);
                    labelAColor.B = (byte)(value.Z * 255.0f + 0.5f);
                };
                TwitchManager.CreateTwitch<Vector3>(curColor, destColor, set, 0.1f, TwitchCurve.Shape.EaseOut);
            }

            newColor = hitBoxB.Contains(hit) ? hoverTextColor : lightTextColor;
            if (newColor != labelBTargetColor)
            {
                labelBTargetColor = newColor;
                Vector3 curColor = new Vector3(labelBColor.R / 255.0f, labelBColor.G / 255.0f, labelBColor.B / 255.0f);
                Vector3 destColor = new Vector3(newColor.R / 255.0f, newColor.G / 255.0f, newColor.B / 255.0f);

                TwitchManager.Set<Vector3> set = delegate(Vector3 value, Object param)
                {
                    labelBColor.R = (byte)(value.X * 255.0f + 0.5f);
                    labelBColor.G = (byte)(value.Y * 255.0f + 0.5f);
                    labelBColor.B = (byte)(value.Z * 255.0f + 0.5f);
                };
                TwitchManager.CreateTwitch<Vector3>(curColor, destColor, set, 0.1f, TwitchCurve.Shape.EaseOut);
            }
        }   // end of HandleTouchInput()

        private void HandleMouseInput(Vector2 hit)
        {
            if (hitBoxA.LeftPressed(hit))
            {
                // Disable this hint for this session.
                if (curHint.ShowOnce)
                {
                    curHint.Disabled = true;
                }

                Deactivate();
            }
            if (hitBoxB.LeftPressed(hit))
            {
                // Disable this hint until reset by user.
                XmlOptionsData.SetHintAsDisabled(curHint.ID);

                // Disable this hint for this session.
                if (curHint.ShowOnce)
                {
                    curHint.Disabled = true;
                }
                
                Deactivate();
            }

            // Check for hover and adjust text color to match.
            Color newColor;

            newColor = hitBoxA.Contains(hit) ? hoverTextColor : lightTextColor;
            if (newColor != labelATargetColor)
            {
                labelATargetColor = newColor;
                Vector3 curColor = new Vector3(labelAColor.R / 255.0f, labelAColor.G / 255.0f, labelAColor.B / 255.0f);
                Vector3 destColor = new Vector3(newColor.R / 255.0f, newColor.G / 255.0f, newColor.B / 255.0f);

                TwitchManager.Set<Vector3> set = delegate(Vector3 value, Object param)
                {
                    labelAColor.R = (byte)(value.X * 255.0f + 0.5f);
                    labelAColor.G = (byte)(value.Y * 255.0f + 0.5f);
                    labelAColor.B = (byte)(value.Z * 255.0f + 0.5f);
                };
                TwitchManager.CreateTwitch<Vector3>(curColor, destColor, set, 0.1f, TwitchCurve.Shape.EaseOut);
            }

            newColor = hitBoxB.Contains(hit) ? hoverTextColor : lightTextColor;
            if (newColor != labelBTargetColor)
            {
                labelBTargetColor = newColor;
                Vector3 curColor = new Vector3(labelBColor.R / 255.0f, labelBColor.G / 255.0f, labelBColor.B / 255.0f);
                Vector3 destColor = new Vector3(newColor.R / 255.0f, newColor.G / 255.0f, newColor.B / 255.0f);

                TwitchManager.Set<Vector3> set = delegate(Vector3 value, Object param)
                {
                    labelBColor.R = (byte)(value.X * 255.0f + 0.5f);
                    labelBColor.G = (byte)(value.Y * 255.0f + 0.5f);
                    labelBColor.B = (byte)(value.Z * 255.0f + 0.5f);
                };
                TwitchManager.CreateTwitch<Vector3>(curColor, destColor, set, 0.1f, TwitchCurve.Shape.EaseOut);
            }
        }   // end of HandleMouseInput()

        public void Render()
        {
            if (Active)
            {
                GraphicsDevice device = BokuGame.bokuGame.GraphicsDevice;
                InGame.SetViewportToScreen();

                ScreenSpaceQuad ssquad = ScreenSpaceQuad.GetInstance();

                Color darkTextColor = new Color(20, 20, 20);
                Color greyTextColor = new Color(127, 127, 127);
                Color greenTextColor = new Color(8, 123, 110);
                Color whiteTextColor = new Color(255, 255, 255);

                // Start with the background.
                if (useBackgroundThumbnail)
                {
                    if (!thumbnail.GraphicsDevice.IsDisposed && !thumbnail.IsDisposed)
                    {
                        // Render the blurred thumbnail (if valid) full screen.
                        if (!thumbnail.GraphicsDevice.IsDisposed)
                        {
                            InGame.RestoreViewportToFull();
                            Vector2 screenSize = new Vector2(device.Viewport.Width, device.Viewport.Height);
                            ssquad.Render(thumbnail, Vector2.Zero, screenSize, @"TexturedNoAlpha");
                            InGame.SetViewportToScreen();
                        }
                    }
                    else
                    {
                        // No valid thumbnail, clear to dark.
                        device.Clear(darkTextColor);
                    }
                }

                //
                // Background frame.
                //
                Vector2 size = new Vector2(backgroundTexture.Width, backgroundTexture.Height);
                renderPosition = (BokuGame.ScreenSize - size) / 2.0f;
                
                ssquad.Render(backgroundTexture, renderPosition, size, "TexturedRegularAlpha");

                //
                // Text.
                //

                // Disable write to alpha.
                device.BlendState = UI2D.Shared.BlendStateColorWriteRGB;

                Vector2 pos = renderPosition;
                int blankLines = maxLines - blob.NumLines;
                pos.Y += blankLines / 2.0f * Font().LineSpacing;
                blob.RenderWithButtons(pos + textPosition, greyTextColor);

                //
                // Add button icons with labels.
                //

                SpriteBatch batch = UI2D.Shared.SpriteBatch;
                batch.Begin();

                Vector2 min;    // Used to capture info for mouse hit boxes.
                Vector2 max;

                // Calc center position.
                pos = new Vector2(BokuGame.ScreenSize.X / 2.0f, BokuGame.ScreenSize.Y / 2.0f + size.Y / 2.0f - 1.8f * Font().LineSpacing);

                // Calc overall width buttons and text.  Leave a button width's space between each section.
                float aWidth = Font().MeasureString(Strings.Localize("toast.continue")).X;
                float xWidth = Font().MeasureString(Strings.Localize("toast.dismiss")).X;
                int buttonWidth = 48;
                float totalWidth = aWidth + xWidth + 3.0f * buttonWidth;

                pos.X -= totalWidth / 2.0f;

                // A button
                min = pos;
                ssquad.Render(ButtonTextures.AButton, pos, new Vector2(56, 56), @"TexturedRegularAlpha");
                pos.X += buttonWidth;
                TextHelper.DrawString(Font, Strings.Localize("toast.continue"), pos, labelAColor);
                max = new Vector2(pos.X + aWidth, min.Y + buttonWidth);
                hitBoxA.Set(min, max);

                pos.X += aWidth;

                // Space
                pos.X += buttonWidth;

                // X button
                min = pos;
                ssquad.Render(ButtonTextures.XButton, pos, new Vector2(56, 56), @"TexturedRegularAlpha");
                pos.X += buttonWidth;
                TextHelper.DrawString(Font, Strings.Localize("toast.dismiss"), pos, labelBColor);
                max = new Vector2(pos.X + xWidth, min.Y + buttonWidth);
                hitBoxB.Set(min, max);

                batch.End();

                // Restore write to alpha.
                device.BlendState = BlendState.AlphaBlend;

            }
        }   // end of ModalHint Render()

        #endregion

        #region Internal

        public void LoadContent(bool immediate)
        {
        }   // end of ModalHint LoadContent()

        public void InitDeviceResources(GraphicsDevice device)
        {
            if (backgroundTexture == null)
            {
                backgroundTexture = BokuGame.Load<Texture2D>(BokuGame.Settings.MediaPath + @"Textures\TextEditor\SmallTextDisplayBackground");
            }
        }   // end of ModalHint InitDeviceResources()

        public void UnloadContent()
        {
            BokuGame.Release(ref backgroundTexture);
        }   // end of ModalHint UnloadContent()

        /// <summary>
        /// Recreate render targets
        /// </summary>
        /// <param name="graphics"></param>
        public void DeviceReset(GraphicsDevice device)
        {
        }

        public void Activate(BaseHint curHint, bool useBackgroundThumbnail, bool useRtCoords)
        {
            this.curHint = curHint;

            if (curHint == null)
                return;

            if (state != States.Active)
            {
                this.useBackgroundThumbnail = useBackgroundThumbnail;
                this.useRtCoords = useRtCoords;

                // Do stack handling here.  If we do it in the update object we have no
                // clue which order things get pushed and popped and madness ensues.
                CommandStack.Push(commandMap);

                state = States.Active;

                // Get the current scene thumbnail.  If we're using this from the main menu (options)
                // then use the title screen image instead.
                if (InGame.inGame.State == InGame.States.Inactive)
                {
                    thumbnail = BokuGame.bokuGame.mainMenu.BackgroundTexture;
                }
                else
                {
                    thumbnail = InGame.inGame.SmallThumbNail;
                }

                // Tell InGame we're using the thumbnail so no need to do full render.
                prevRenderWorldAsThumbnail = InGame.inGame.RenderWorldAsThumbnail;
                if (!prevRenderWorldAsThumbnail)
                {
                    InGame.inGame.RenderWorldAsThumbnail = true;
                }

                Time.Paused = true;

                HelpOverlay.Push(@"ModalHint");

                // Get text string.
                blob = new TextBlob(Font, curHint.ModalText, textWidth);
                blob.Justification = UIGridElement.Justification.Center;
            }
        }   // end of Activate

        public void Deactivate()
        {
            if (state != States.Inactive)
            {
                // Do stack handling here.  If we do it in the update object we have no
                // clue which order things get pushed and popped and madness ensues.
                CommandStack.Pop(commandMap);

                state = States.Inactive;

                HelpOverlay.Pop();

                // Turn off thumbnail rendering if needed.
                if (!prevRenderWorldAsThumbnail)
                {
                    InGame.inGame.RenderWorldAsThumbnail = false;
                }
                Time.Paused = false;

                curHint = null;
            }
        }

        #endregion

    }   // end of class ModalHint

}   // end of namespace Boku
