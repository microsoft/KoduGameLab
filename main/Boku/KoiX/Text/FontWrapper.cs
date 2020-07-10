// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.


using System;
using System.Collections.Generic;
using System.Text;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace KoiX.Text
{
    /// <summary>
    /// These delegates are used to provide a level of indirection for objects that
    /// need to hold onto a reference to a font.  Since a device reset may change the
    /// underlying font we instead give the objects a delegate which returns the 
    /// correct font.
    /// </summary>

    public delegate FontWrapper GetFont();
    public delegate SystemFont GetSystemFont();
    public delegate SpriteFont GetSpriteFont();

    /// <summary>
    /// Simple wrapper which contains both a SpriteFont and a SystemFont.
    /// Only one should ever be valid.
    /// This is done to provide a single type for GetFont() to return
    /// so we can dynamically switch between font systems.
    /// </summary>
    public class FontWrapper
    {
        public SpriteFont spriteFont = null;
        public SystemFont systemFont = null;

        public FontWrapper()
        {
        }

        public FontWrapper(SpriteFont spriteFont, SystemFont systemFont)
        {
            this.spriteFont = spriteFont;
            this.systemFont = systemFont;
        }

        /// <summary>
        /// Vertical measure of font spacing in pixels.
        /// </summary>
        public int LineSpacing
        {
            get
            {
                int result = 0;

                if (Boku.BokuSettings.Settings.UseSystemFontRendering)
                {
#if !NETFX_CORE
                    result = systemFont.LineSpacing;
#endif
                }
                else
                {
                    result = spriteFont.LineSpacing;
                }

                return result;
            }
        }

        /// <summary>
        /// Size of rendered string in pixels.
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        public Vector2 MeasureString(string text)
        {
            Vector2 result = Vector2.Zero;

            // MeasureString gives us the actual size of "\n", since
            // a newline isn't rendered, we want ti to count as zero.
            
            if (text != "\n")
            {
                if (Boku.BokuSettings.Settings.UseSystemFontRendering)
                {
#if !NETFX_CORE
                    result = systemFont.MeasureString(text);
#endif
                }
                else
                {
                    // If we're using SpriteFont, filter out bad characters.
                    text = TextHelper.FilterInvalidCharacters(text);

                    result = spriteFont.MeasureString(text);
                }
            }

            return result;
        }   // end of MeasureString()
    
    }   // end of class FontWrapper

}   // end of namespace KoiX.Text
