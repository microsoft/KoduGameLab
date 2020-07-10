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
using Microsoft.Xna.Framework.GamerServices;

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
    public class MessageDialog : INeedsDeviceReset
    {
        #region Members

        // delegates
        public delegate void ButtonHandler(MessageDialog dialog);

        // User supplied functions which will be called in response to a button press.
        ButtonHandler handlerA = null;
        ButtonHandler handlerB = null;
        ButtonHandler handlerX = null;
        ButtonHandler handlerY = null;

        // Labels for buttons.
        string labelA = null;
        string labelB = null;
        string labelX = null;
        string labelY = null;

        private bool active = false;

        private String text = null;     // The text of the message.

        private Color textColor = Color.White;
        private Color dropShadowColor = new Color(30, 30, 30);
        private bool useDropShadow = false;
        private bool invertDropShadow = false;
        private UIGridElement.Justification justification = UIGridElement.Justification.Center;

        private static Effect effect = null;

        private RenderTarget2D diffuse = null;
        private Texture background = null;      // Background image.

        private float width = 512;              // Size of dialog in pixels.
        private float height = 302;             // Determined by size of background art and the fact that this
                                                // makes the width/height ratio close to phi.

        private int margin = 24;                // Margin for text in pixels.
        private int maxWidth = 0;               // Max width for a line of text in pixels.

        private Vector2 pos;

        private bool dirty = true;              // Does the texture need refreshing.

        private CommandMap commandMap = new CommandMap(@"MessageDialog");

        #endregion

        #region Accessors

        /// <summary>
        /// Is this dialog currently active?
        /// </summary>
        public bool Active
        {
            get { return active; }
        }

        /// <summary>
        /// The text string displayed by the dialog.
        /// </summary>
        public String Text
        {
            get { return text; }
            set { text = value; dirty = true; }
        }
        public Color TextColor
        {
            get { return textColor; }
            set { textColor = value; dirty = true; }
        }
        public Color DropShadowColor
        {
            get { return dropShadowColor; }
            set { dropShadowColor = value; dirty = true; }
        }
        public bool UseDropShadow
        {
            get { return useDropShadow; }
            set { useDropShadow = value; dirty = true; }
        }
        public bool InvertDropShadow
        {
            get { return invertDropShadow; }
            set { invertDropShadow = value; dirty = true; }
        }
        public UIGridElement.Justification Justification
        {
            get { return justification; }
            set { justification = value; dirty = true; }
        }

        #endregion

        #region Public

        // c'tor
        public MessageDialog(string text, ButtonHandler handlerA, string labelA, ButtonHandler handlerB, string labelB, ButtonHandler handlerX, string labelX, ButtonHandler handlerY, string labelY)
        {
            this.text = text;

            this.handlerA = handlerA;
            this.handlerB = handlerB;
            this.handlerX = handlerX;
            this.handlerY = handlerY;

            this.labelA = labelA;
            this.labelB = labelB;
            this.labelX = labelX;
            this.labelY = labelY;

        }   // end of c'tor

        public void Activate()
        {
            /// We rely on this render target to get fully running. Refuse
            /// to activate until it's available.
            if (UI2D.Shared.RenderTarget512_302 != null)
            {
                /// If we're still dirty, someone is trying to activate us before the system
                /// is loaded enough to support us. So refuse to activate until we have everything
                /// we need.
                active = true;

                HelpOverlay.Push("MessageDialog");

                // Force a re-render of the texture.
                dirty = true;
                CommandStack.Push(commandMap);
            }
        }   // and of Activate()

        public void Deactivate()
        {
            active = false;
            diffuse = null;
            HelpOverlay.Pop();
            CommandStack.Pop(commandMap);
        }   // end of Deactivate()

        /// <summary>
        /// Called once per frame by the 
        /// </summary>
        public void Update()
        {
            if (Active)
            {
                /// Don't let anyone else grab focus, if anyone has, grab it back.
                if (CommandStack.Peek() != commandMap)
                {
                    CommandStack.Pop(commandMap);
                    CommandStack.Push(commandMap);
                }
                // Check if we have input focus.
                if (CommandStack.Peek() == commandMap)
                {
                    GamePadInput pad = GamePadInput.GetGamePad0();

                    if (handlerA != null && (pad.ButtonA.WasPressed || KeyboardInput.WasPressed(Keys.A)))
                    {
                        GamePadInput.ClearAllWasPressedState();
                        GamePadInput.IgnoreUntilReleased(Buttons.A);
                        handlerA(this);
                    }

                    if (handlerB != null && (pad.ButtonB.WasPressed || KeyboardInput.WasPressed(Keys.B)))
                    {
                        GamePadInput.ClearAllWasPressedState();
                        GamePadInput.IgnoreUntilReleased(Buttons.B);
                        handlerB(this);
                    }

                    if (handlerX != null && (pad.ButtonX.WasPressed || KeyboardInput.WasPressed(Keys.X)))
                    {
                        GamePadInput.ClearAllWasPressedState();
                        GamePadInput.IgnoreUntilReleased(Buttons.X);
                        handlerX(this);
                    }

                    if (handlerY != null && (pad.ButtonY.WasPressed || KeyboardInput.WasPressed(Keys.Y)))
                    {
                        GamePadInput.ClearAllWasPressedState();
                        GamePadInput.IgnoreUntilReleased(Buttons.Y);
                        handlerY(this);
                    }

                }   // end if we have input focus.

                if (dirty)
                {
                    // Calc max width for text string.
                    maxWidth = (int)(width - 2.0f * margin);

                    RefreshTexture();
                }
            }
        }   // end of Update()

        /// <summary>
        /// Rendering call.
        /// </summary>
        public void Render()
        {
            if (Active)
            {
                // Center box on screen.
                // Note we do this here instead of in the Update call because it is 
                // dependent on the viewport size which may be different at the time
                // when Update is called versus when Render is called.
                pos = new Vector2(BokuGame.bokuGame.GraphicsDevice.Viewport.Width, BokuGame.bokuGame.GraphicsDevice.Viewport.Height);
                pos = (pos - new Vector2(width, height)) * 0.5f;
                ScreenSpaceQuad ssquad = ScreenSpaceQuad.GetInstance();

                try
                {
                    ssquad.Render(diffuse, pos, new Vector2(width, height), @"TexturedRegularAlpha");
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
            if (dirty)
            {
                GraphicsDevice device = BokuGame.Graphics.GraphicsDevice;

                LoadContent(true);

                diffuse = UI2D.Shared.RenderTarget512_302;
                if (diffuse == null)
                {
                    /// Not ready yet, remain dirty.
                    return;
                }
                SpriteBatch batch = UI2D.Shared.SpriteBatch;
                SpriteFont font20 = UI2D.Shared.GameFont20;
                SpriteFont font24 = UI2D.Shared.GameFont24;

                InGame.SetRenderTarget(diffuse);
                InGame.Clear(Color.Transparent);

                // Render the backdrop.
                ScreenSpaceQuad ssquad = ScreenSpaceQuad.GetInstance();
                ssquad.Render(background, Vector2.Zero, new Vector2(512, 512), @"TexturedPreMultAlpha");

                //
                // Render the text message.
                //

                List<string> lineList = new List<string>();
                TextHelper.SplitMessage(Text, (int)(width - margin * 2), font24, false, lineList);

                // Calc center of display.
                int y = (int)((height - 1.5f * font24.LineSpacing) / 2.0f);
                int dy = font24.LineSpacing;
                // Offset based on number of lines.
                y -= (int)(dy * (lineList.Count - 1) / 2.0f);

                batch.Begin();

                for (int i = 0; i < lineList.Count; i++)
                {
                    string line = lineList[i];

                    // Render the label text into the texture.
                    int x = 0;
                    Vector2 textSize = font24.MeasureString(line);

                    x = TextHelper.CalcJustificationOffset(margin, (int)width, (int)textSize.X, Justification);

                    if (UseDropShadow)
                    {
                        TextHelper.DrawStringWithShadow(font24, batch, x, y, line, TextColor, DropShadowColor, InvertDropShadow);
                    }
                    else
                    {
                        batch.DrawString(font24, line, new Vector2(x, y), TextColor);
                    }

                    y += dy;
                }   // end of i loop over lines in list.

                //
                // Render any active buttons and the text that goes with them.
                //

                // We need to cal the width of each active button and it's label 
                // so we can center the whole set at the bottom of the dialog.
                // TODO Set this up so we can also right/left justify?

                Vector2 position = Vector2.Zero;
                position.X = width / 2.0f;
                position.Y = height - margin - 40.0f;

                Vector2 buttonSize = new Vector2(64, 64);   // Size for rendering.
                int buttonWidth = 40;                       // Size for spacing.
                int gap = 8;                                // Space between sets.

                float totalWidth = 0;

                if (handlerA != null)
                {
                    totalWidth += buttonWidth;
                    totalWidth += font20.MeasureString(labelA).X;
                }
                if (handlerB != null)
                {
                    if (totalWidth != 0)
                        totalWidth += gap;
                    totalWidth += buttonWidth;
                    totalWidth += font20.MeasureString(labelB).X;
                }
                if (handlerX != null)
                {
                    if (totalWidth != 0)
                        totalWidth += gap;
                    totalWidth += buttonWidth;
                    totalWidth += font20.MeasureString(labelX).X;
                }
                if (handlerY != null)
                {
                    if (totalWidth != 0)
                        totalWidth += gap;
                    totalWidth += buttonWidth;
                    totalWidth += font20.MeasureString(labelY).X;
                }

                position.X -= (int)totalWidth / 2;

                //
                // Render each button/label pair if needed.
                //
                ScreenSpaceQuad quad = ScreenSpaceQuad.GetInstance();

                if (handlerA != null)
                {
                    quad.Render(ButtonTextures.AButton, new Vector2(position.X, position.Y - 2), buttonSize, @"TexturedRegularAlpha");
                    position.X += buttonWidth;
                    //TextHelper.DrawStringWithShadow(font20, batch, position.X, position.Y, labelA, TextColor, DropShadowColor, InvertDropShadow);
                    batch.DrawString(font20, labelA, position, TextColor);
                    position.X += (int)font20.MeasureString(labelA).X + gap;
                }
                if (handlerB != null)
                {
                    quad.Render(ButtonTextures.BButton, new Vector2(position.X, position.Y - 2), buttonSize, @"TexturedRegularAlpha");
                    position.X += buttonWidth;
                    //TextHelper.DrawStringWithShadow(font20, batch, position.X, position.Y, labelB, TextColor, DropShadowColor, InvertDropShadow);
                    batch.DrawString(font20, labelB, position, TextColor);
                    position.X += (int)font20.MeasureString(labelB).X + gap;
                }
                if (handlerX != null)
                {
                    quad.Render(ButtonTextures.XButton, new Vector2(position.X, position.Y - 2), buttonSize, @"TexturedRegularAlpha");
                    position.X += buttonWidth;
                    //TextHelper.DrawStringWithShadow(font20, batch, position.X, position.Y, labelX, TextColor, DropShadowColor, InvertDropShadow);
                    batch.DrawString(font20, labelX, position, TextColor);
                    position.X += (int)font20.MeasureString(labelX).X + gap;
                }
                if (handlerY != null)
                {
                    quad.Render(ButtonTextures.YButton, new Vector2(position.X, position.Y - 2), buttonSize, @"TexturedRegularAlpha");
                    position.X += buttonWidth;
                    //TextHelper.DrawStringWithShadow(font20, batch, position.X, position.Y, labelY, TextColor, DropShadowColor, InvertDropShadow);
                    batch.DrawString(font20, labelY, position, TextColor);
                    position.X += (int)font20.MeasureString(labelY).X + gap;
                }

                batch.End();

                // Restore backbuffer.
                InGame.RestoreRenderTarget();

                dirty = false;
            }

        }   // end of MessageDialog RefreshTexture()

        public void LoadContent(bool immediate)
        {
            // Init the effect.
            if (effect == null)
            {
                effect = BokuGame.Load<Effect>(BokuGame.Settings.MediaPath + @"Shaders\UI2D");
            }

            // Load the background texture.
            if (background == null)
            {
                background = BokuGame.Load<Texture2D>(BokuGame.Settings.MediaPath + @"Textures\MessageBox\MessageBoxBackground");
            }

        }   // end of MessageDialog LoadContent();

        public void InitDeviceResources(GraphicsDeviceManager graphics)
        {
            dirty = true;
        }   // end of InitDeviceResources()

        public void UnloadContent()
        {
            BokuGame.Release(ref effect);
            BokuGame.Release(ref background);
        }

        public void DeviceReset(GraphicsDeviceManager graphics)
        {
            dirty = true;
            RefreshTexture();
        }

        #endregion

    }   // end of class MessageDialog

}   // end of namespace Boku

