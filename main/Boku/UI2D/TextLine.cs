// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.


using System;
using System.Collections;
using System.Collections.Generic;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Storage;

using KoiX;
using KoiX.Text;

using Boku.Base;
using Boku.Common;
using Boku.Fx;

namespace Boku.UI2D
{
    /// <summary>
    /// Internal class use to hold a single line of text and the texture it's rendered to.
    /// </summary>
    public class TextLine : INeedsDeviceReset
    {
        private static Point margin = new Point(32, 12);    // Margin used by individual TextLines.

        private UIGridElement parent = null;
        private string text;
        private GetFont Font = null;
        private bool checkbox = false;
        private Vector2 position = Vector2.Zero;    // Where this TextLine lives.
        private Vector2 size;                       // Size in pixels of diffuse.

        private RenderTarget2D diffuse = null;

        private bool hidden = false;                // Should line be rendered?

        #region Accessors
        /// <summary>
        /// Position to render in pixels.
        /// </summary>
        public Vector2 Position
        {
            get { return position; }
            set
            {
                position = value;
                parent.Dirty = true;
            }
        }
        /// <summary>
        /// Size of TextLine texture in pixels.
        /// </summary>
        public Vector2 Size
        {
            get { return size; }
            set { size = value; parent.Dirty = true; }
        }
        public Texture2D Texture
        {
            get
            {
                if (diffuse != null)
                    return diffuse;
                else
                    return null;
            }
        }
        public string Text
        {
            get { return text; }
        }

        /// <summary>
        /// Should line be hidden.
        /// </summary>
        public bool Hidden
        {
            get { return hidden; }
            set { hidden = value; }
        }
        #endregion

        /// <summary>
        /// C'tor for TextLine
        /// </summary>
        /// <param name="parent">The UIGridElement owner.</param>
        /// <param name="text">The text to display.</param>
        /// <param name="Font">The function to get the current font.</param>
        /// <param name="checkbox">Should this line have a checkbox at the beginning?</param>
        public TextLine(UIGridElement parent, string text, GetFont Font, bool checkbox)
        {
            this.parent = parent;
            this.text = TextHelper.FilterInvalidCharacters(text);
            this.Font = Font;
            this.checkbox = checkbox;
        }   // end of TextLine c'tor

        /// <summary>
        /// Renders the text string into the texture.
        /// </summary>
        private void RefreshTexture()
        {
            InGame.SetRenderTarget(diffuse);
            InGame.Clear(Color.Transparent);

            Point position = UIGridTextListElement.Margin;
            // Render the checkbox if needed.
            if (checkbox)
            {
                ScreenSpaceQuad quad = ScreenSpaceQuad.GetInstance();
                Vector2 size = new Vector2(40.0f, 40.0f);
                quad.Render(UIGridTextListElement.Checkbox, new Vector2(position.X, position.Y), size, @"TexturedRegularAlpha");
                position.X += (int)size.X;
            }

            // Render the text.         
            SpriteBatch batch = KoiLibrary.SpriteBatch;

            batch.Begin();
            TextHelper.DrawStringWithShadow(Font, batch, position.X, position.Y, text, Color.White, Color.Black, false);
            batch.End();

            // Restore backbuffer.
            InGame.RestoreRenderTarget();

            Size = new Vector2(diffuse.Width, diffuse.Height);
        }   // end of TextLine RefreshTexture()

        public void LoadContent(bool immediate)
        {
        }   // end of TextLine LoadContent()

        public void InitDeviceResources(GraphicsDevice device)
        {
            CreateRenderTargets(device);
        }

        public void UnloadContent()
        {
            ReleaseRenderTargets();
        }

        /// <summary>
        /// Recreate render targets
        /// </summary>
        /// <param name="graphics"></param>
        public void DeviceReset(GraphicsDevice device)
        {
            ReleaseRenderTargets();
            CreateRenderTargets(device);
        }

        private void CreateRenderTargets(GraphicsDevice device)
        {
            int stringWidth = (int)(Font().MeasureString(text).X) + 2;    // +2 for good Karma.

            // Create the diffuse texture.
            int width = margin.X + stringWidth;
            int height = margin.Y + Font().LineSpacing;

            if (checkbox)
            {
                width += UIGridTextListElement.Checkbox.Width + UIGridTextListElement.Margin.X;
            }

            if (BokuGame.RequiresPowerOf2)
            {
                width = MyMath.GetNextPowerOfTwo(width);
                height = MyMath.GetNextPowerOfTwo(height);
            }

            diffuse = new RenderTarget2D(device,
                width, height,
                false,                      // Mip levels
                SurfaceFormat.Color,
                DepthFormat.None);
            SharedX.GetRT("TextLine", diffuse);

            RefreshTexture();
        }

        private void ReleaseRenderTargets()
        {
            SharedX.RelRT("TextLine", diffuse);
            DeviceResetX.Release(ref diffuse);
        }

    }   // end of class TextLine

}   // end of namespace Boku.Ui2d
