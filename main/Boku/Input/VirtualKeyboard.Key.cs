// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.


using System;
using System.Xml;
using System.Xml.Serialization;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Storage;

using Boku.Base;
using Boku.Common;

namespace Boku.Input
{
    public static partial class VirtualKeyboard
    {
        public class Key
        {
            #region Members

            KeySet parent = null;

            UI2D.Shared.GetFont Font = null;
            string label = "";                  // What is printed on this key, if anything.
            public string value = "";           // What character(s) is sent when this key is pressed.
            public Keys keyValue = Keys.None;   // If set, ignore value and use this.

            public OnKeyDelegate OnKey = null;      // Delegate used to capture trigger trasitions started by this key.

            Texture2D texture = null;

            AABB2D hitBox = new AABB2D();   // HitBox relative to upper left hand corner of keyboard.

            bool pressed = false;   // Is this key currently pressed?

            Color keyCapColor = VirtualKeyboard.darkKeyColor;
            public Color labelColor = VirtualKeyboard.textColor;       // Color used for the label whether it's text or a texture.

            Vector2 position;
            Vector2 size;

            double timeOfLastPress = 0;     // Time when user pressed key.
            double timeOfLastRepeat = 0;    // Time when we generated an autorepeat.

            static TextBlob blob = null;

            #endregion

            #region Accessors

            public bool Pressed
            {
                get { return pressed; }
                set 
                {
                    if (pressed != value)
                    {
                        pressed = value;
                        VirtualKeyboard.dirty = true;
                    }
                }
            }

            #endregion

            #region Public

            #region c'tors

            /// <summary>
            /// c'tor for normal keys.
            /// </summary>
            /// <param name="parent"></param>
            /// <param name="position"
            /// <param name="size"
            /// <param name="label"></param>
            /// <param name="Font"></param>
            /// <param name="keyCapColor"></param>
            /// <param name="value"></param>
            public Key(KeySet parent, Vector2 position, Vector2 size, string label, UI2D.Shared.GetFont Font, Color labelColor, Color keyCapColor, string value)
            {
                this.parent = parent;
                this.position = position;
                this.size = size;
                this.label = label;
                this.Font = Font;
                this.labelColor = labelColor;
                this.keyCapColor = keyCapColor;
                this.value = value;
            }

            /// <summary>
            /// c'tor for textured keys.
            /// </summary>
            /// <param name="parent"></param>
            /// <param name="position"
            /// <param name="size"
            /// <param name="texture"></param>
            /// <param name="keyCapColor"></param>
            /// <param name="value"></param>
            public Key(KeySet parent, Vector2 position, Vector2 size, Texture2D texture, Color labelColor, Color keyCapColor, string value)
            {
                this.parent = parent;
                this.position = position;
                this.size = size;
                this.texture = texture;
                this.labelColor = labelColor;
                this.keyCapColor = keyCapColor;
                this.value = value;
            }

            /// <summary>
            /// c'tor for normal keys which have both a label and a texture.
            /// </summary>
            /// <param name="parent"></param>
            /// <param name="position"
            /// <param name="size"
            /// <param name="label"></param>
            /// <param name="Font"></param>
            /// <param name="keyCapColor"></param>
            /// <param name="value"></param>
            public Key(KeySet parent, Vector2 position, Vector2 size, string label, UI2D.Shared.GetFont Font, Color labelColor, Color keyCapColor, Texture2D texture, string value)
            {
                this.parent = parent;
                this.position = position;
                this.size = size;
                this.label = label;
                this.Font = Font;
                this.labelColor = labelColor;
                this.keyCapColor = keyCapColor;
                this.texture = texture;
                this.value = value;
            }


            /// <summary>
            /// c'tor for special keys.
            /// </summary>
            /// <param name="parent"></param>
            /// <param name="position"
            /// <param name="size"
            /// <param name="label"></param>
            /// <param name="Font"></param>
            /// <param name="keyCapColor"></param>
            /// <param name="value"></param>
            public Key(KeySet parent, Vector2 position, Vector2 size, string label, UI2D.Shared.GetFont Font, Color labelColor, Color keyCapColor, Keys value)
            {
                this.parent = parent;
                this.position = position;
                this.size = size;
                this.label = label;
                this.Font = Font;
                this.labelColor = labelColor;
                this.keyCapColor = keyCapColor;
                this.keyValue = value;
            }

            /// <summary>
            /// c'tor for textured special keys.
            /// </summary>
            /// <param name="parent"></param>
            /// <param name="position"
            /// <param name="size"
            /// <param name="texture"></param>
            /// <param name="keyCapColor"></param>
            /// <param name="value"></param>
            public Key(KeySet parent, Vector2 position, Vector2 size, Texture2D texture, Color labelColor, Color keyCapColor, Keys value)
            {
                this.parent = parent;
                this.position = position;
                this.size = size;
                this.texture = texture;
                this.labelColor = labelColor;
                this.keyCapColor = keyCapColor;
                this.keyValue = value;
            }

            /// <summary>
            /// c'tor for keys which cause a transition.  Transition can be to another KeySet
            /// or to deactivate the virtual keyboard.
            /// </summary>
            /// <param name="parent"></param>
            /// <param name="position"
            /// <param name="size"
            /// <param name="label"></param>
            /// <param name="Font"></param>
            /// <param name="keyCapColor"></param>
            /// <param name="OnKey"></param>
            public Key(KeySet parent, Vector2 position, Vector2 size, string label, UI2D.Shared.GetFont Font, Color labelColor, Color keyCapColor, OnKeyDelegate OnKey)
            {
                this.parent = parent;
                this.position = position;
                this.size = size;
                this.label = label;
                this.Font = Font;
                this.labelColor = labelColor;
                this.keyCapColor = keyCapColor;
                this.OnKey = OnKey;
            }

            /// <summary>
            /// c'tor for transition keys with a texture.
            /// </summary>
            /// <param name="parent"></param>
            /// <param name="position"
            /// <param name="size"
            /// <param name="texture"></param>
            /// <param name="keyCapColor"></param>
            /// <param name="OnKey"></param>
            public Key(KeySet parent, Vector2 position, Vector2 size, Texture2D texture, Color labelColor, Color keyCapColor, OnKeyDelegate OnKey)
            {
                this.parent = parent;
                this.position = position;
                this.size = size;
                this.texture = texture;
                this.labelColor = labelColor;
                this.keyCapColor = keyCapColor;
                this.OnKey = OnKey;
            }

            /// <summary>
            /// c'tor for keys which cause a transition.  Transition can be to another KeySet
            /// or to deactivate the virtual keyboard.  This version is for keys with both a lable and a texture.
            /// </summary>
            /// <param name="parent"></param>
            /// <param name="position"
            /// <param name="size"
            /// <param name="label"></param>
            /// <param name="Font"></param>
            /// <param name="keyCapColor"></param>
            /// <param name="OnKey"></param>
            public Key(KeySet parent, Vector2 position, Vector2 size, string label, UI2D.Shared.GetFont Font, Color labelColor, Color keyCapColor, Texture2D texture, OnKeyDelegate OnKey)
            {
                this.parent = parent;
                this.position = position;
                this.size = size;
                this.label = label;
                this.Font = Font;
                this.labelColor = labelColor;
                this.keyCapColor = keyCapColor;
                this.texture = texture;
                this.OnKey = OnKey;
            }

            #endregion

            /// <summary>
            /// 
            /// </summary>
            /// <returns>Returns true if autorepeating.</returns>
            public bool Update()
            {
                bool result = false;

                hitBox.Set(position, position + size);

                // If pressed, see if touch is still active.  If not, clear pressed state.
                if (Pressed)
                {
                    TouchContact touch = TouchInput.GetOldestTouch();
                    if(touch == null || touch.phase == TouchPhase.Ended)
                    {
                        Pressed = false;
                    }

                    // If previously pressed, check for possible autorepeat.
                    // Don't do this for keys with delegates (transition keys).
                    // They should never autorepeat.
                    if (Pressed && OnKey == null)
                    {
                        // Waited long enough to autorepeat?
                        if (Time.GameTimeTotalSeconds - timeOfLastPress >= GamePadInput.Button.AutoRepeatDelay)
                        {
                            if (Time.GameTimeTotalSeconds - timeOfLastRepeat >= 1.0 / GamePadInput.Button.AutoRepeatRate)
                            {
                                // Trigger an autorepeat.
                                result = true;
                                timeOfLastRepeat = Time.GameTimeTotalSeconds;
                            }
                        }
                    }
                }

                return result;
            }   // end of Update()

            public void Render()
            {
                if (blob == null)
                {
                    blob = new TextBlob(UI2D.Shared.GetGameFont30Bold, "foo", 1000);
                }

                // NOTE: Assumes Begin has already been called on batch
                // and End will be called elsewhere.
                SpriteBatch batch = UI2D.Shared.SpriteBatch;

                bool invertColors = false;
                if (pressed && OnKey == null)
                {
                    invertColors = true;
                }

                // KeyCap
                batch.Begin(SpriteSortMode.Deferred, BlendState.NonPremultiplied);
                Rectangle rect = new Rectangle((int)hitBox.Min.X, (int)hitBox.Min.Y, (int)hitBox.Size.X, (int)hitBox.Size.Y);
                batch.Draw(VirtualKeyboard.whiteTexture, rect, invertColors ? labelColor : keyCapColor);
                batch.End();

                // Texture
                // Note we render textures before labels just in case key has both.
                if (texture != null)
                {
                    // Center texture on keycap.
                    Vector2 size = new Vector2(texture.Width, texture.Height);
                    Vector2 pos = this.position;
                    pos += (hitBox.Size - size) / 2.0f;
                    batch.Begin(SpriteSortMode.Deferred, BlendState.NonPremultiplied);
                    batch.Draw(texture, pos, labelColor);
                    batch.End();
                }

                // Label
                if (!string.IsNullOrEmpty(label))
                {
                    blob.Font = Font;
                    blob.RawText = label;

                    float width = blob.GetLineWidth(0);

                    // Center side to side based on actual width but top to bottom based on line spacing.
                    Vector2 pos = this.position;
                    pos.X += (hitBox.Size.X - width) / 2.0f;
                    pos.Y += (hitBox.Size.Y - Font().LineSpacing) / 2.0f;
                    // Clamp to int to get better looking glyphs.
                    pos.X = (int)pos.X;
                    pos.Y = (int)pos.Y;
                    blob.RenderWithButtons(pos, invertColors ? keyCapColor : labelColor);
                }

            }   // end of Render()

            /// <summary>
            /// Does hit testing for key.
            /// </summary>
            /// <param name="hitPosition"></param>
            /// <returns></returns>
            public bool HitTest(Vector2 hitPosition)
            {
                // Detect user press.
                if (!Pressed && hitBox.Contains(hitPosition))
                {
                    Pressed = true;
                    timeOfLastPress = Time.GameTimeTotalSeconds;
                    timeOfLastRepeat = 0;
                }

                return Pressed;
            }   // end of HitTest()

            #endregion

            #region Internal
            
            public void LoadContent(bool immediate)
            {
            }

            public void InitDeviceResources(GraphicsDevice device)
            {
            }

            public void UnloadContent()
            {
            }

            public void DeviceReset(GraphicsDevice device)
            {
            }

            #endregion

        }   // end of class Key


    }   // end of class VirtualKeyboard

}   // end of namespace Boku.Input
