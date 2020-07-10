// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using KoiX;
using KoiX.Text;

using Boku.Common;

namespace Boku.Fx
{
    public class SimpleMessage
    {
        #region Members
        private Point center = new Point(0, 0);
        private Point size = new Point(0, 0);
        private List<Texture2D> textures = new List<Texture2D>();
        private double frequency = 1.0f;
        private string text = "";
        private GetFont font = null;
        private Point textCenter = new Point(0, 0);
        #endregion Members

        #region Accessors
        /// <summary>
        /// The center (in pixels) of the texture on screen.
        /// </summary>
        public Point Center
        {
            get { return center; }
            set { center = value; }
        }
        /// <summary>
        /// The size (in pixels) of the texture on screen.
        /// </summary>
        public Point Size
        {
            get { return size; }
            set { size = value; }
        }
        /// <summary>
        /// Width (in pixels) of the rendered region on screen.
        /// </summary>
        public int Width
        {
            get { return Size.X; }
        }
        /// <summary>
        /// Height (in pixels) of the rendered region on screen.
        /// </summary>
        public int Height
        {
            get { return Size.Y; }
        }
        /// <summary>
        /// The texture to blit onto screen.
        /// </summary>
        public Texture2D Texture
        {
            get 
            {
                if (textures.Count == 0)
                    return null;
                return textures[CurrentTexture]; 
            }
        }
        /// <summary>
        /// Number of seconds for a cycle through all textures.
        /// </summary>
        public double Period
        {
            get { return 1.0 / frequency; }
            set 
            {
                Debug.Assert(value > 0);
                frequency = 1.0 / value; 
            }
        }
        /// <summary>
        /// Text caption for the texture.
        /// </summary>
        public string Text
        {
            get { return text; }
            set { text = value; }
        }
        /// <summary>
        /// Center of the text caption on screen.
        /// </summary>
        public Point TextCenter
        {
            get { return textCenter; }
            set { textCenter = value; }
        }
        /// <summary>
        /// Font to use for the text.
        /// </summary>
        public GetFont Font
        {
            get { return font; }
            set { font = value; }
        }

        /// <summary>
        /// Compute the index (based on Wall Clock) of the current texture.
        /// </summary>
        private int CurrentTexture
        {
            get
            {
                if (textures.Count < 2)
                    return 0;

                double phase = Time.WallClockTotalSeconds * frequency;
                double frac = phase - (int)phase;
                int idx = (int)(frac * textures.Count);
                return idx;
            }
        }
        #endregion Accessors

        #region Public

        /// <summary>
        /// Render the texture and text to the screen.
        /// </summary>
        /// <param name="data"></param>
        public void Render(object data)
        {
            if (Texture != null)
            {
                ScreenSpaceQuad quad = ScreenSpaceQuad.GetInstance();
                quad.Render(Texture,
                    Vector4.One,
                    new Vector2(Center.X - Width / 2, Center.Y - Height / 2),
                    new Vector2(Width, Height),
                    @"TexturedRegularAlpha");
            }

            if (!string.IsNullOrEmpty(Text))
            {
                SpriteBatch batch = KoiLibrary.SpriteBatch;

                Vector2 stringSize = Font().MeasureString(TextHelper.FilterInvalidCharacters(text));
                Point textPos = new Point(
                    TextCenter.X - (int)(stringSize.X * 0.5f + 0.5f),
                    TextCenter.Y);

                batch.Begin();
                TextHelper.DrawStringWithShadow(Font,
                    batch,
                    textPos.X, textPos.Y,
                    text,
                    Color.White,
                    Color.DimGray,
                    false);
                batch.End();
            }

        }

        /// <summary>
        /// Add a texture to cycle through
        /// </summary>
        /// <param name="tex"></param>
        public void AddTexture(Texture2D tex)
        {
            textures.Add(tex);
        }
        /// <summary>
        /// Remove a texture from the cycle.
        /// </summary>
        /// <param name="tex"></param>
        public void RemoveTexture(Texture2D tex)
        {
            Debug.Assert(textures.Contains(tex), "Removing a texture I don't have");
            textures.Remove(tex);
        }
        /// <summary>
        /// Clear all textures, render only text.
        /// </summary>
        public void ClearTextures()
        {
            textures.Clear();
        }

        #endregion Public

        #region Internal
        #endregion Internal
    };
};
