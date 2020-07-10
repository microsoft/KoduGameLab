// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.


using System;
using System.Collections.Generic;
using System.Diagnostics;
#if !NETFX_CORE
using System.Drawing;
using System.Drawing.Imaging;
#endif
using System.Text;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace Boku.Common
{
#if !NETFX_CORE
    public static partial class SysFont
    {
        /// <summary>
        /// Class for caching text written with SysFont.
        /// </summary>
        public class CacheEntry
        {
            // Fixed size values for cached textures.  These are deliberately small since we only
            // expect them to be used in minor rolls.  Anything bigger should be cached as part of
            // the UI element's design.
            public const int Width = 640;
            public const int Height = 128;

            public static int MaxEntries = 24;   // Generally not needed to be this big but it makes some UI elements work better without having to rewrite them.

            #region Members

            Texture2D texture;
            Microsoft.Xna.Framework.Rectangle targetRect;
            
            string text;
            SystemFont font;

            // Data used when transfering into textures.  We can share this across all cache entries.
            static Microsoft.Xna.Framework.Color[] data = new Microsoft.Xna.Framework.Color[Width * Height];

            #endregion

            #region Accessors

            public Texture2D Texture
            {
                get { return texture; }
            }

            public string Text
            {
                get { return text; }
                set { text = value; }
            }

            public SystemFont Font
            {
                get { return font; }
                set { font = value; }
            }

            public Microsoft.Xna.Framework.Rectangle TargetRect
            {
                get { return targetRect; }
                set { targetRect = value; }
            }

            #endregion

            #region Public

            public CacheEntry()
            {
                GraphicsDevice device = BokuGame.bokuGame.GraphicsDevice;

                texture = new Texture2D(device, Width, Height, false, SurfaceFormat.Color);
            }

            #endregion

            #region Internal

            public void UnloadContent()
            {
                BokuGame.Release(ref texture);
            }

            #endregion

        }   // end of class CacheEntry

    }   // end of class SysFont
#endif
}   // end of namespace Boku.Common
