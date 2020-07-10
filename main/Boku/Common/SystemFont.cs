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

#if NETFX_CORE
    /// <summary>
    /// Empty placeholder class until we can figure out how to make this work for store builds.
    /// </summary>
    public class SystemFont
    {
    }
#else
    /// <summary>
    /// Simple wrapper for a System Font class that
    /// helps it act more like SpriteFonts.
    /// </summary>
    public class SystemFont
    {
        Font font;
        float padding;

        public Font Font
        {
            get { return font; }
        }

        /// <summary>
        /// graphics.DrawString adds padding at the beginning and end of a rendered
        /// string based on font size.  This is just one end, not the sum of both ends.
        /// </summary>
        public float Padding
        {
            get
            {
                if (padding == 0)
                {
                    Graphics graphics = SysFont.Graphics;

                    StringFormat format = new StringFormat(StringFormatFlags.MeasureTrailingSpaces);
                    SizeF m = graphics.MeasureString("M", font, PointF.Empty, format);
                    SizeF mm = graphics.MeasureString("MM", font, PointF.Empty, format);

                    padding = Math.Max(0.0f, (2.0f * m.Width - mm.Width) / 2.0f);
                }
                return padding;
            }
        }

        public int LineSpacing
        {
            get { return (int)font.GetHeight(); }
        }

        public SystemFont(string familyName, float emSize, System.Drawing.FontStyle style = FontStyle.Regular)
        {
            font = new Font(familyName, emSize, style);
        }

        /// <summary>
        /// Returns the actual size of the rendered string without padding.
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        public Vector2 MeasureString(string text)
        {
            Graphics graphics = SysFont.Graphics;
            Vector2 result = Vector2.Zero;

            StringFormat format = new StringFormat(StringFormatFlags.MeasureTrailingSpaces);
            SizeF size = graphics.MeasureString(text, font, PointF.Empty, format);
            result = new Vector2(size.Width, size.Height);

            // Remove padding.
            result.X -= 2.0f * Padding;

            return result;
        }   // end of MeasureString()

    }   // end of class SystemFont
#endif
}   // end of namespace Boku.Common
