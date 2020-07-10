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

    using Point = Microsoft.Xna.Framework.Point;
    using Color = Microsoft.Xna.Framework.Color;

    using Rectangle = System.Drawing.Rectangle;
    using TextRenderingHint = System.Drawing.Text.TextRenderingHint;

	public static partial class SysFont
	{
	    /// <summary>
        /// Multiple DrawString calls can be batched together to minimize the perf
        /// hit when transferring data from system to GPU memory.  Each BatchEntry
        /// encapulates one call in the current batch.
        /// </summary>
        public class BatchEntry
        {
            public SystemFont font;     // Encapsulates the font, style and point size.
            public string text;         // Text to be rendered.
            public Color textColor;     // Color for text.  May be partially transparent.
            public Vector2 position;    // Pixel position for rendering.  Note that this doesn't change if the outline
                                        // gets thicker.  We do, however, stretch the clipRect if needed.
            public float cameraZoom;    // Zoom from camera.  Needed to scale a bunch of things.

            public SizeF size;          // Size of layed out glyphs without padding.
            public System.Drawing.RectangleF clipRect;

            public Color outlineColor;
            public float outlineWidth;  // No outline if width = 0.

            public Vector2 scaling = Vector2.One;

            /// <summary>
            /// private c'tor.
            /// </summary>
            BatchEntry()
            {
            }

            /// <summary>
            /// 
            /// </summary>
            /// <param name="text"></param>
            /// <param name="position"></param>
            /// <param name="cameraZoom"></param>
            /// <param name="font"></param>
            /// <param name="textColor"></param>
            /// <param name="outlineColor"></param>
            /// <param name="outlineWidth"></param>
            /// <param name="clipRect">Rect to clip against.  Note this will be inflated to compensate for outline width.</param>
            /// <param name="scaling"></param>
            /// <returns></returns>
            public static BatchEntry CreateEntry(string text, Vector2 position, float cameraZoom, SystemFont font, Color textColor, Color outlineColor = default(Color), float outlineWidth = 0, RectangleF clipRect = default(RectangleF), Vector2 scaling = default(Vector2))
            {
                BatchEntry entry = null;
                if (freeEntries.Count > 0)
                {
                    entry = freeEntries[freeEntries.Count - 1];
                    freeEntries.RemoveAt(freeEntries.Count - 1);
                }
                else
                {
                    entry = new BatchEntry();
                }

                if (scaling == default(Vector2))
                {
                    scaling = Vector2.One;
                }

                entry.cameraZoom = cameraZoom;
                entry.scaling = scaling;
                entry.text = text;

                // Adjust position for cameraZoom and outline thickness.
                entry.position = cameraZoom * position;
                
                entry.font = font;
                entry.textColor = textColor;

                Vector2 textSize = font.MeasureString(text);

                entry.size = new SizeF(textSize.X + 2.0f * cameraZoom * outlineWidth, textSize.Y);
                if (clipRect == default(RectangleF))
                {
                    // No clipRect was given so make one to fit the text.
                    entry.clipRect = new System.Drawing.RectangleF(position.X * cameraZoom, position.Y * cameraZoom, entry.size.Width, entry.size.Height);
                }
                else
                {
                    entry.clipRect = new System.Drawing.RectangleF(clipRect.X * cameraZoom, clipRect.Y * cameraZoom, clipRect.Width * cameraZoom, clipRect.Height * cameraZoom);
                }
                // Inflate clipRect to account for outline width.
                entry.clipRect.Inflate(cameraZoom * outlineWidth, cameraZoom * outlineWidth);

                entry.outlineColor = outlineColor;
                entry.outlineWidth = outlineWidth;

                return entry;
            }   // end of CreateEntry()

        }   // end of class BatchEntry

	}   // end of class SysFont
#endif
}   // end of namespace Boku.Common
