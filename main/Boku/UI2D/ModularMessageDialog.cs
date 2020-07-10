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
using Boku.Common;
using Boku.Input;
using Boku.SimWorld;
using Boku.UI;
using Boku.UI2D;
using Boku.Fx;

namespace Boku
{
    /// <summary>
    /// A replacement for the MessageBox which is more flexible about
    /// allowing several button options for the user.
    /// This also breaks away from the Shared/UpdateObj/RenderObj
    /// paradigm.
    /// </summary>
    public class ModularMessageDialog : INeedsDeviceReset
    {
        #region Members

        // delegates
        public delegate void ButtonHandler(ModularMessageDialog dialog);

        // User supplied functions which will be called in response to a button press.
        ButtonHandler handlerA = null;
        ButtonHandler handlerB = null;
        ButtonHandler handlerX = null;
        ButtonHandler handlerY = null;

        Button aButton = null;
        Button bButton = null;
        Button xButton = null;
        Button yButton = null;

        // Labels for buttons.
        string labelA = null;
        string labelB = null;
        string labelX = null;
        string labelY = null;

        private bool active = false;

        private bool useRtCoords = false;       // Are we rendering over a rendertarget?

        private TextBlob textBlob = null;       // The message to display.

        private Color textColor = new Color(127, 127, 127);
        private Color lightTextColor = new Color(191, 191, 191);
        private Color hoverTextColor = new Color(50, 255, 50);
        private Color shadowColor = new Color(0, 0, 0, 10);
        private Vector2 shadowOffset = new Vector2(0, 6);

        private RenderTarget2D diffuse = null;  // We're grabbig this from Shared.  Is this going to be a problem???
                                                // TODO Try testing multiple simultanious dialog boxes.
        private Texture2D background = null;

        private int width = 512;                // Size of dialog in pixels.
        private int height = 302;               // Determined by size of background art and the fact that this
                                                // makes the width/height ratio close to phi.
        private int blackHeight = 91;           // The height of the black band at the bottom of the dialog.

        private int margin = 24;                // Margin for text in pixels.

        private Vector2 pos;                    // Position on screen (upper left, in pixels)

        // TODO Restore dirty flag handling.  Right now we re-render the whole dialog each 
        // frame since we have to way to tell if a button has become dirty (label changes color on hover)
        private bool dirty = true;              // Does the texture need refreshing.

        private CommandMap commandMap = new CommandMap(@"ModularMessageDialog");

        #endregion

        #region Accessors

        /// <summary>
        /// Is this dialog currently active?
        /// </summary>
        public bool Active
        {
            get { return active; }
        }

        public ButtonHandler HandlerA
        {
            get { return handlerA; }
            set { handlerA = value; }
        }

        public ButtonHandler HandlerB
        {
            get { return handlerB; }
            set { handlerB = value; }
        }

        public ButtonHandler HandlerX
        {
            get { return handlerX; }
            set { handlerX = value; }
        }

        public ButtonHandler HandlerY
        {
            get { return handlerY; }
            set { handlerY = value; }
        }

        public CommandMap CommandMap
        {
            get { return commandMap; }
        }

        public string LabelA
        {
            get { return labelA; }
            set 
            { 
                labelA = value;
                if (labelA != null)
                {
                    GetTexture getTexture = delegate() { return ButtonTextures.AButton; };
                    aButton = new Button(labelA, Color.White, getTexture, SharedX.GetGameFont20);
                }
                else
                {
                    aButton = null;
                }
            }
        }

        public string LabelB
        {
            get { return labelB; }
            set
            {
                labelB = value;
                if (labelB != null)
                {
                    GetTexture getTexture = delegate() { return ButtonTextures.BButton; };
                    bButton = new Button(labelB, Color.White, getTexture, SharedX.GetGameFont20);
                }
                else
                {
                    bButton = null;
                }
            }
        }


        public string LabelX
        {
            get { return labelX; }
            set
            {
                labelX = value;
                if (labelX != null)
                {
                    GetTexture getTexture = delegate() { return ButtonTextures.XButton; };
                    xButton = new Button(labelX, Color.White, getTexture, SharedX.GetGameFont20);
                }
                else
                {
                    xButton = null;
                }
            }
        }


        public string LabelY
        {
            get { return labelY; }
            set
            {
                labelY = value;
                if (labelY != null)
                {
                    GetTexture getTexture = delegate() { return ButtonTextures.YButton; };
                    yButton = new Button(labelY, Color.White, getTexture, SharedX.GetGameFont20);
                }
                else
                {
                    yButton = null;
                }
            }
        }

        #endregion

        #region Public

        // c'tor
        public ModularMessageDialog(
            string text,
            ButtonHandler handlerA, string labelA,
            ButtonHandler handlerB, string labelB,
            ButtonHandler handlerX, string labelX,
            ButtonHandler handlerY, string labelY)
        {
            SetText(text);

            commandMap.name += text;

            this.handlerA = handlerA;
            this.handlerB = handlerB;
            this.handlerX = handlerX;
            this.handlerY = handlerY;

            this.labelA = labelA;
            this.labelB = labelB;
            this.labelX = labelX;
            this.labelY = labelY;

            if (handlerA != null)
            {
                Debug.Assert(labelA != null, "If you have a button handler you must have a label for it.");
            }
            if (handlerB != null)
            {
                Debug.Assert(labelB != null, "If you have a button handler you must have a label for it.");
            }
            if (handlerX != null)
            {
                Debug.Assert(labelX != null, "If you have a button handler you must have a label for it.");
            }
            if (handlerY != null)
            {
                Debug.Assert(labelY != null, "If you have a button handler you must have a label for it.");
            }


            if (labelA != null)
            {
                GetTexture getTexture = delegate() { return ButtonTextures.AButton; };
                aButton = new Button(labelA, Color.White, getTexture, SharedX.GetGameFont20);
            }
            if (labelB != null)
            {
                GetTexture getTexture = delegate() { return ButtonTextures.BButton; };
                bButton = new Button(labelB, Color.White, getTexture, SharedX.GetGameFont20);
            }
            if (labelX != null)
            {
                GetTexture getTexture = delegate() { return ButtonTextures.XButton; };
                xButton = new Button(labelX, Color.White, getTexture, SharedX.GetGameFont20);
            }
            if (labelY != null)
            {
                GetTexture getTexture = delegate() { return ButtonTextures.YButton; };
                yButton = new Button(labelY, Color.White, getTexture, SharedX.GetGameFont20);
            }
        }   // end of c'tor

        public void SetText(string text)
        {
            int maxWidth = width - 2 * margin;
            
            textBlob = new TextBlob(SharedX.GetGameFont30Bold, text, maxWidth);
            textBlob.Justification = TextHelper.Justification.Center;

            // If this big a font is too many lines, use a smaller one.
            if (textBlob.NumLines > 3)
            {
                textBlob.Font = SharedX.GetGameFont24;
            }

            // Still too big?
            if (textBlob.NumLines > 4)
            {
                textBlob.Font = SharedX.GetGameFont20;
            }

            shadowOffset.Y = textBlob.TotalSpacing / 8;

            dirty = true;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="userRtCoords">Are we rendering on a rendertarget?</param>
        public void Activate(bool useRtCoords = false)
        {
            if (!active)
            {
                this.useRtCoords = useRtCoords;

                // We rely on this render target to get fully running. Refuse
                // to activate until it's available.
                if (SharedX.RenderTarget512_302 != null)
                {
                    // If we're still dirty, someone is trying to activate us before the system
                    // is loaded enough to support us. So refuse to activate until we have everything
                    // we need.
                    active = true;

                    RefreshTexture();

                    HelpOverlay.Push("ModularMessageDialog");

                    // Force a re-render of the texture.
                    dirty = true;
                    CommandStack.Push(commandMap);
                }
            }
        }   // and of Activate()

        public void Deactivate()
        {
            if (active)
            {
                active = false;
                diffuse = null;
                HelpOverlay.Pop();
                CommandStack.Pop(commandMap);
            }
        }   // end of Deactivate()

        /// <summary>
        /// Called once per frame.
        /// Assumes everything is in screenspace.
        /// </summary>
        public void Update()
        {
            Update(null);
        }

        /// <summary>
        /// Called once per frame.
        /// If camera is null, assumes everything is in screenspace.
        /// </summary>
        public void Update(Camera camera)
        {
            if (Active)
            {
                // Don't let anyone else grab focus, if anyone has, grab it back.
                if (CommandStack.Peek() != commandMap)
                {
                    CommandStack.Pop(commandMap);
                    CommandStack.Push(commandMap);
                }
                // Check if we have input focus.
                if (CommandStack.Peek() == commandMap)
                {
                    GamePadInput pad = GamePadInput.GetGamePad0();

                    if (handlerA != null && Actions.A.WasPressed)
                    {
                        Actions.A.ClearAllWasPressedState();
                        Actions.A.IgnoreUntilReleased();
                        handlerA(this);
                    }

                    if (handlerB != null && Actions.B.WasPressed)
                    {
                        Actions.B.ClearAllWasPressedState();
                        Actions.B.IgnoreUntilReleased();
                        handlerB(this);
                    }

                    if (handlerX != null && Actions.X.WasPressed)
                    {
                        Actions.X.ClearAllWasPressedState();
                        Actions.X.IgnoreUntilReleased();
                        handlerX(this);
                    }

                    if (handlerY != null && Actions.Y.WasPressed)
                    {
                        Actions.Y.ClearAllWasPressedState();
                        Actions.Y.IgnoreUntilReleased();
                        handlerY(this);
                    }
                    Vector2 hit;

                    if (KoiLibrary.LastTouchedDeviceIsTouch)
                    {
                        for (int i = 0; i < TouchInput.TouchCount; i++)
                        {
                            TouchContact touch = TouchInput.GetTouchContactByIndex(i);
                            hit = touch.position;
                            if(camera != null)
                            {
                                hit = LowLevelMouseInput.AdjustHitPosition(hit, camera, false, false);
                            }
                            hit = hit - pos;
                           
                            HandleTouchInput(hit, touch);
                        }
                    }
                    else
                    {
                        // Since the dialog is screenspace we can use the mouse position directly.
                        hit = LowLevelMouseInput.PositionVec;
                        if (useRtCoords)
                        {
                            hit = ScreenWarp.ScreenToRT(hit);
                        }
                        hit -= pos;

                        HandleMouseInput(hit);
                    }

                }   // end if we have input focus.

                RefreshTexture();
            }
        }   // end of Update()


        private void HandleTouchInput(Vector2 hit, TouchContact touch)
        {
            // Hovering?
            if (touch.phase == TouchPhase.Moved || touch.phase == TouchPhase.Stationary)
            {
                if (aButton != null)
                    aButton.SetHoverState(hit);

                if (bButton != null)
                    bButton.SetHoverState(hit);

                if (xButton != null)
                    xButton.SetHoverState(hit);

                if (yButton != null)
                    yButton.SetHoverState(hit);
            }
            if (touch.phase == TouchPhase.Ended)
            {
                // Clicked on? 
                if (aButton != null && aButton.Box.Touched(touch,hit,false))
                {
                    if (handlerA != null)
                    {
                        handlerA(this);
                    }
                    Deactivate();
                }
                if (bButton != null && bButton.Box.Touched(touch, hit, false))
                {
                    if (handlerB != null)
                    {
                        handlerB(this);
                    }
                    Deactivate();
                }
                if (xButton != null && xButton.Box.Touched(touch, hit, false))
                {
                    if (handlerX != null)
                    {
                        handlerX(this);
                    }
                    Deactivate();
                }
                if (yButton != null && yButton.Box.Touched(touch, hit, false))
                {
                    if (handlerY != null)
                    {
                        handlerY(this);
                    }
                    Deactivate();
                }

            }


        }

        private void HandleMouseInput(Vector2 hit)
        {
            // Hovering?
            if (aButton != null)
            {
                aButton.SetHoverState(hit);
            }
            if (bButton != null)
            {
                bButton.SetHoverState(hit);
            }
            if (xButton != null)
            {
                xButton.SetHoverState(hit);
            }
            if (yButton != null)
            {
                yButton.SetHoverState(hit);
            }

            // Clicked on?
            if (aButton != null && aButton.Box.LeftPressed(hit))
            {
                if (handlerA != null)
                {
                    handlerA(this);
                }
                Deactivate();
            }
            if (bButton != null && bButton.Box.LeftPressed(hit))
            {
                if (handlerB != null)
                {
                    handlerB(this);
                }
                Deactivate();
            }
            if (xButton != null && xButton.Box.LeftPressed(hit))
            {
                if (handlerX != null)
                {
                    handlerX(this);
                }
                Deactivate();
            }
            if (yButton != null && yButton.Box.LeftPressed(hit))
            {
                if (handlerY != null)
                {
                    handlerY(this);
                }
                Deactivate();
            }

        }   // end of HandleMouseInput()

        /// <summary>
        /// Rendering call.
        /// </summary>
        public void Render()
        {
            if (Active)
            {
                // Get screen size.
                Vector2 screenSize = InGame.GetCurrentRenderTargetSize();
                // If no rendertarget, use system size.
                if (screenSize == Vector2.Zero)
                {
                    screenSize = BokuGame.ScreenSize;
                }

                // Center box on screen.
                Vector2 size = new Vector2(width, height);
                pos = (screenSize - size)/2.0f; 

                ScreenSpaceQuad ssquad = ScreenSpaceQuad.GetInstance();

                try
                {
                    ssquad.Render(diffuse, pos, size, @"TexturedRegularAlpha");
                }
                catch
                {
                    // Another one of those places where the first time through the system thinks
                    // that the render target is still set on the device and throws an exception
                    // when we try and get the texture.  We can catch it here and do nothing since
                    // it will be fine next frame.
                }
            }
        }   // end of Render()

        #endregion

        #region Internal

        /// <summary>
        /// If the text being displayed has changed, we need to refresh the texture.
        /// Note this requires changing the rendertarget so this should no be called
        /// during the normal rendering loop.
        /// </summary>
        private void RefreshTexture()
        {
            // This needs to be refactored.  The problem is that when the buttons change color
            // (for hover for instance) the changing state of thier color needs to set the dirty
            // flag.  For now, just always set it so we always have the right behavior.
            dirty = true;
            if (dirty)
            {
                GraphicsDevice device = KoiLibrary.GraphicsDevice;

                LoadContent(true);

                diffuse = SharedX.RenderTarget512_302;
                if (diffuse == null)
                {
                    // Not ready yet, remain dirty.
                    return;
                }
                SpriteBatch batch = KoiLibrary.SpriteBatch;
                GetFont Font20 = SharedX.GetGameFont20;
                GetFont Font24 = SharedX.GetGameFont24;

                InGame.SetRenderTarget(diffuse);
                InGame.Clear(Color.Transparent);

                // Render the backdrop.
                ScreenSpaceQuad quad = ScreenSpaceQuad.GetInstance();
                quad.Render(background, Vector2.Zero, new Vector2(512, 302), @"TexturedPreMultAlpha");

                //
                // Render the text message.
                //

                Vector2 pos = new Vector2(margin, 0.0f);
                // Calc vertical center of display.
                pos.Y = (int)((height - blackHeight - 1.5f * textBlob.TotalSpacing) / 2.0f);
                // Offset based on number of lines.
                pos.Y -= (int)(textBlob.TotalSpacing * (textBlob.NumLines - 1) / 2.0f);

                textBlob.RenderText(null, pos + shadowOffset, shadowColor);
                textBlob.RenderText(null, pos, textColor);

                //
                // Render any active buttons and the text that goes with them.
                //

                batch.Begin();

                // We need to calc the width of each active button and it's label 
                // so we can center the whole set at the bottom of the dialog.
                // TODO Set this up so we can also right/left justify?

                Vector2 position = Vector2.Zero;
                position.X = width / 2.0f;
                position.Y = height - margin - 40.0f;
                int gap = Button.Margin;

                int totalWidth = 0;
                if (aButton != null)
                {
                    totalWidth += (int)aButton.GetSize().X;
                    totalWidth += gap;
                }
                if (bButton != null)
                {
                    totalWidth += (int)bButton.GetSize().X;
                    totalWidth += gap;
                }
                if (xButton != null)
                {
                    totalWidth += (int)xButton.GetSize().X;
                    totalWidth += gap;
                }
                if (yButton != null)
                {
                    totalWidth += (int)yButton.GetSize().X;
                    totalWidth += gap;
                }

                totalWidth -= gap;   // Remove the extra one...
                position.X -= (int)(totalWidth / 2);

                if (aButton != null)
                {
                    aButton.Render(position);
                    position.X += aButton.GetSize().X + gap;
                }
                if (bButton != null)
                {
                    bButton.Render(position);
                    position.X += bButton.GetSize().X + gap;
                }
                if (xButton != null)
                {
                    xButton.Render(position);
                    position.X += xButton.GetSize().X + gap;
                }
                if (yButton != null)
                {
                    yButton.Render(position);
                    position.X += yButton.GetSize().X + gap;
                }

                batch.End();

                // Restore backbuffer.
                InGame.RestoreRenderTarget();

                dirty = false;
            }

        }   // end of ModularMessageDialog RefreshTexture()

        public void LoadContent(bool immediate)
        {
            // Load the textures.
            if (background == null)
            {
                background = KoiLibrary.LoadTexture2D(@"Textures\MessageBox\MessageBoxBackground");
            }

        }   // end of ModularMessageDialog LoadContent();

        public void InitDeviceResources(GraphicsDevice device)
        {
            dirty = true;
        }   // end of InitDeviceResources()

        public void UnloadContent()
        {
            DeviceResetX.Release(ref background);
        }

        /// <summary>
        /// Recreate render targets
        /// </summary>
        /// <param name="graphics"></param>
        public void DeviceReset(GraphicsDevice device)
        {
            dirty = true;
        }

        #endregion

    }   // end of class ModularMessageDialog

}   // end of namespace Boku

