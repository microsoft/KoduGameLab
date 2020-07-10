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

using Boku.Base;
using Boku.Fx;
using Boku.Common;
using Boku.Common.Xml;
using Boku.Input;
using Boku.UI2D;

namespace Boku.UI2D
{
    public delegate Texture2D GetTexture();

    /// <summary>
    /// Light weight wrapper around commonly used button elements.
    /// Combines a button icon with a label and other info needed for rendering.
    /// </summary>
    public class Button
    {
        #region Members

        GetTexture getTexture = null;       // Texture2D for button icon.
        string label;                       // Label to be rendered next to icon.
        AABB2D box = null;                  // Hit box for mouse testing.

        ButtonState state = ButtonState.Released;
        Color color = Color.White;                  // Color when not hovered over.
        Color hoverColor = new Color(50, 255, 50);  // Color when hovered over.

        Color renderColor = Color.White;    // What is currently rendered.
        Color targetColor = Color.White;    // Where the twitch is going.

        UI2D.Shared.GetFont Font = null;
        private Vector2 fixedSize;
        private bool bUseFixedSize = false;
        private Vector2 labelOffset = Vector2.Zero;

        Vector2 buttonSize = new Vector2(56, 56);           // Size texture is rendered.
        Vector2 visibleButtonSize = new Vector2(40, 40);    // Visible part of the texture.  The graphic isn't centered...
        #endregion

        #region Accessors

        /// <summary>
        /// Spacing to be used between buttons.  Results in 4-6 pixel gap.
        /// </summary>
        public static int Margin
        {
            get { return 16; }
        }

        /// <summary>
        /// Allows direct manipulation of button size
        /// </summary>
        public Vector2 FixedSize
        {
            get
            {
                Vector2 size = Vector2.Zero;
                size.X = (box.Max.X - box.Min.X);
                size.Y = (box.Max.Y - box.Min.Y);
                return size;
            }            

            set 
            { 
                fixedSize = value;
                box.Set( new Vector2(box.Min.X,box.Min.Y), new Vector2(box.Min.X + value.X, box.Min.Y + value.Y));
            }            
        }

        public bool IsPressed
        {
            get { return state == ButtonState.Pressed; }
        }

        public bool UseFixedSize
        {
            get { return bUseFixedSize; }
            set { bUseFixedSize = value; }
        }

        public AABB2D Box
        {
            get { return box; }
        }

        /// <summary>
        /// Set the normal (not hovered over) color for the button.
        /// </summary>
        public Color Color
        {
            set { color = value; }
        }

        /// <summary>
        /// Optional offset for the label text
        /// </summary>
        public Vector2 LabelOffset
        {
            get
            {
                return labelOffset;
            }
            set
            {
                labelOffset = value;
            }
        }

        #endregion

        #region Public

        public Button(string label, Color color, GetTexture getTexture, UI2D.Shared.GetFont Font)
        {
            this.label = label;
            this.color = color;
            this.renderColor = color;
            this.getTexture = getTexture;
            this.Font = Font;
            this.state = ButtonState.Released;

            this.box = new AABB2D();
            this.labelOffset = new Vector2();
        }

        public string Label
        {
            set
            {
                label = value;
            }       
        }

        public Vector2 GetSize()
        {
            if (bUseFixedSize)
            {
                return fixedSize;
            }

            Vector2 result = Vector2.Zero;
            if (getTexture != null)
            {
                result = visibleButtonSize;
            }
            if (Font != null)
            {
                result.X += Font().MeasureString(label).X;
                result.Y = Math.Max(result.Y, Font().LineSpacing);
            }
    
            return result;
        }   // end of GetSize()

        private Vector4 GetDrawColor()
        {
            switch (state)
            {
                case ButtonState.Pressed:
                {
                    return new Vector4(0.7f, 0.7f, 0.7f, 0.4f);
                }
                case ButtonState.Released:
                {
                    return new Vector4(1.0f, 1.0f, 1.0f, 0.95f);
                }
                default:
                    return new Vector4(1.0f, 1.0f, 1.0f, 0.95f);
            }
        }

        public void Render(Vector2 pos)
        {
            Render(pos, useBatch: true);
        }

        public void Render(Vector2 pos, bool useBatch)
        {
            ScreenSpaceQuad ssquad = ScreenSpaceQuad.GetInstance();
            SpriteBatch batch = UI2D.Shared.SpriteBatch;
            Vector2 margin = new Vector2(6, 6); // margin around button graphic.

            if (bUseFixedSize)
            {
                buttonSize = fixedSize;
            }

            // Render rectangular buttons if not in gamepad mode or no button/icon graphic is specified.
            if (GamePadInput.ActiveMode != GamePadInput.InputMode.GamePad || getTexture == null)
            {
                Texture2D buttonTexture = UI2D.Shared.BlackButtonTexture;
                ssquad.Render(buttonTexture, box.Min, box.Max - box.Min, "TexturedRegularAlpha");
            }

            Vector2 min = pos - margin;
            if (getTexture != null)
            {
                ssquad.Render(getTexture(), pos, buttonSize, "TexturedRegularAlpha");
                if (!bUseFixedSize)
                {
                    pos.X += visibleButtonSize.X;
                }
            }

            if (Font != null)
            {
                if (useBatch)
                {
                    TextHelper.DrawString(Font, label, pos + LabelOffset, renderColor);
                }
                else
                {
                    TextHelper.DrawStringNoBatch(Font, label, pos + LabelOffset, renderColor);
                }
            }

            Vector2 max = max = min + GetSize();
            max += 2.0f * margin;

            box.Set(min, max);

            // Uncomment to debug hit regions.
            //ssquad.Render(new Vector4(1, 0, 0, 0.5f), min, max - min);

        }   // end of Render()

        public void ClearState()
        {
            state = ButtonState.Released;
            targetColor = color;
            Vector3 curColor = new Vector3(renderColor.R / 255.0f, renderColor.G / 255.0f, renderColor.B / 255.0f);
            Vector3 destColor = new Vector3(color.R / 255.0f, color.G / 255.0f, color.B / 255.0f);

            TwitchManager.Set<Vector3> set = delegate(Vector3 value, Object param)
            {
                renderColor.R = (byte)(value.X * 255.0f + 0.5f);
                renderColor.G = (byte)(value.Y * 255.0f + 0.5f);
                renderColor.B = (byte)(value.Z * 255.0f + 0.5f);
            };
            TwitchManager.CreateTwitch<Vector3>(curColor, destColor, set, 0.1f, TwitchCurve.Shape.EaseOut);
        }

        public void SetHoverState(Vector2 mouseHit)
        {
            Color newColor = box.Contains(mouseHit) ? hoverColor : color;
            if (newColor != targetColor)
            {
                if (newColor != color)
                    state = ButtonState.Pressed;
                else
                    state = ButtonState.Released;

                targetColor = newColor;
                Vector3 curColor = new Vector3(renderColor.R / 255.0f, renderColor.G / 255.0f, renderColor.B / 255.0f);
                Vector3 destColor = new Vector3(newColor.R / 255.0f, newColor.G / 255.0f, newColor.B / 255.0f);

                TwitchManager.Set<Vector3> set = delegate(Vector3 value, Object param)
                {
                    renderColor.R = (byte)(value.X * 255.0f + 0.5f);
                    renderColor.G = (byte)(value.Y * 255.0f + 0.5f);
                    renderColor.B = (byte)(value.Z * 255.0f + 0.5f);
                };
                TwitchManager.CreateTwitch<Vector3>(curColor, destColor, set, 0.1f, TwitchCurve.Shape.EaseOut);
            }

        }   // end of SetHoverState()

        #endregion

    }   // end of class Button

}   // end of namespace Boku.UI2D
