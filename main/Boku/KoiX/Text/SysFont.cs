
#define PARALLEL
//#define DEBUG_SPEW

using System;
using System.Collections.Generic;
using System.Diagnostics;
#if !NETFX_CORE
using System.Drawing;
using System.Drawing.Imaging;
#endif
using System.Text;
#if PARALLEL
using System.Threading.Tasks;
#endif

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace KoiX.Text
{
#if !NETFX_CORE

    using Point = Microsoft.Xna.Framework.Point;
    using Color = Microsoft.Xna.Framework.Color;

    using Rectangle = System.Drawing.Rectangle;
    using TextRenderingHint = System.Drawing.Text.TextRenderingHint;

    /// <summary>
    /// Static class to provide system style font rendering for XNA apps.
    /// Desinged to be used as a psuedo replacement for SpriteFonts.
    /// </summary>
    public static partial class SysFont
    {
        #region Members

        static public int MaxWidth = 2048;
        static public int MaxHeight = 1024;

        static Graphics graphics;
        static Bitmap bitmap;
        static byte[][] argbValues;
        static string mostRecentlyRenderedString;   // String which matches content of bitmap.  This allows repeated rendering of 
                                                    // text bigger than the cache size to also act as if it is cached.  Note this
                                                    // only works if there is exactly 1 of these being rendered at any given time.

        // Rectangles which match the size of the bitmap.  Used for 
        // clipping when text would exceed bitmap area.
        static Rectangle bitmapRect;
        static Microsoft.Xna.Framework.Rectangle targetRect;

        static SolidBrush brushBlack;
        static SolidBrush brushWhite;
        static StringFormat format;

        static SpriteCamera camera;             // Camera set for current batch.
        static bool inBatch = false;
        static List<BatchEntry> entries;        // Current entries for this batch.
        static List<BatchEntry> freeEntries;    // Free list so we're not constantly reallocating BatchEntrys
        static Dictionary<string, SystemFont> systemFonts;

        // Texture we're writing to.
        static Texture2D texture;
        static Color[] textureColorData;

        static List<CacheEntry> cacheList = new List<CacheEntry>();

        static System.Drawing.Drawing2D.Matrix identityMatrix = new System.Drawing.Drawing2D.Matrix();

        static int frame = -1;
        static int numMisses;
        static int numCached;       // Number served by the cache.
        static int numRendered;     // Number we still needed to render.
        static Vector2 maxSize = Vector2.Zero;

        #endregion

        #region Accessors

        public static Graphics Graphics
        {
            get { return graphics; }
        }

        #endregion

        #region Public

        public static void Init()
        {
            int width = MaxWidth;
            int height = MaxHeight;

            bitmap = new Bitmap(width, height, PixelFormat.Format32bppPArgb);

            bitmapRect = new System.Drawing.Rectangle(0, 0, width, height);

            argbValues = new byte[height][];
            for (int j = 0; j < height; j++)
            {
                argbValues[j] = new byte[width * 4];
            }

            graphics = Graphics.FromImage(bitmap);
            graphics.TextRenderingHint = TextRenderingHint.AntiAlias;

            format = new StringFormat();
            brushBlack = new SolidBrush(System.Drawing.Color.Black);
            brushWhite = new SolidBrush(System.Drawing.Color.White);

            graphics.Clear(System.Drawing.Color.Transparent);

            systemFonts = new Dictionary<string, SystemFont>();
            entries = new List<BatchEntry>();
            freeEntries = new List<BatchEntry>();

            texture = new Texture2D(KoiLibrary.GraphicsDevice, width, height);
            textureColorData = new Microsoft.Xna.Framework.Color[width * height];

            // Create empty cache entries.
            for (int i = 0; i < CacheEntry.MaxEntries; i++)
            {
                cacheList.Add(new CacheEntry());
            }

        }   // end of Init()

        public static void CleanUp()
        {
            foreach (SystemFont font in systemFonts.Values)
            {
                font.Font.Dispose();
            }
            brushBlack.Dispose();
            format.Dispose();
            graphics.Dispose();
            bitmap.Dispose();

            DeviceResetX.Release(ref texture);

            while (cacheList.Count > 0)
            {
                CacheEntry ce = cacheList[0];
                cacheList.RemoveAt(0);
                ce.UnloadContent();
            }
        }   // end of CleanUp()

        /// <summary>
        /// Start a new batch of SysFont text rendering.
        /// Batches may freely mix fonts, styles, sizes and colors.
        /// Batches will be rendered in the order they are Drawn so later
        /// draw calls may overwrite previous ones.
        /// </summary>
        public static void StartBatch(SpriteCamera camera)
        {
            SysFont.camera = camera;
            inBatch = true;
        }   // end of StartBatch()

        /// <summary>
        /// 
        /// </summary>
        /// <param name="text"></param>
        /// <param name="position">Position for rendering.</param>
        /// <param name="clipRect">Rect to clip text to.</param>
        /// <param name="font"></param>
        /// <param name="textColor"></param>
        /// <param name="scaling">Only applied to text, not rect.</param>
        /// <param name="outlineColor"></param>
        /// <param name="outlineWidth"></param>
        /// <returns></returns>
        public static Vector2 DrawString(string text, Vector2 position, RectangleF clipRect, SystemFont font, Color textColor, Vector2 scaling = default(Vector2), Color outlineColor = default(Color), float outlineWidth = 0)
        {
            Debug.Assert(inBatch, "You must call StartBatch() before this and EndBatch() when done.");
            if (string.IsNullOrEmpty(text))
            {
                return Vector2.Zero;
            }

            if (scaling == default(Vector2))
            {
                scaling = Vector2.One;
            }

            float zoom = camera != null ? camera.Zoom : 1.0f;

            if (zoom != 1.0f)
            {
                // Create a new version of the font matching the zoom.
                font = GetSystemFont(font.Font.Name, font.Font.Size * zoom, font.Font.Style);
            }

            BatchEntry entry = BatchEntry.CreateEntry(text, position, zoom, font, textColor, outlineColor, outlineWidth, clipRect, scaling);
            entries.Add(entry);

            return new Vector2(entry.size.Width, entry.size.Height);
        }   // end of DrawString()

        /// <summary>
        /// Ends the current batch and renders the result to the current backbuffer/rendertarget.
        /// This is where all the work gets done.
        /// </summary>
        public static void EndBatch()
        {

            if (frame != Time.FrameCounter)
            {
#if DEBUG_SPEW
                string foo = "numCached:" + numCached.ToString() + " numRendered:" + numRendered.ToString() + " max:" + maxSize.ToString() + " frame:" + Time.FrameCounter.ToString();
                Time.DebugString = foo;
                //WidgetTest.XNAControl.debugString = foo;
#endif

                frame = Time.FrameCounter;
                numMisses = 0;
                numCached = 0;
                numRendered = 0;
                maxSize = Vector2.Zero;
            }

            if (entries.Count > 0)
            {
                // Cumulative rect used for keeping track of extent of all text entries in the batch.
                System.Drawing.RectangleF batchRectF = new System.Drawing.RectangleF(entries[0].position.X, entries[0].position.Y, 0, 0);

                // Grow batchRect to enclose all entries.
                // Also accumulate temp string used only for cache testing.  If it turns out that font
                // and color are differentiating issues then we can just add then to the string.
                string cacheString = null;
                foreach (BatchEntry entry in entries)
                {
                    // Calc rect for rendering text.
                    int w = (int)(entry.size.Width + 2.0f * entry.outlineWidth + 1);    // MeasureString doesn't account for outline.
                    int h = (int)(entry.size.Height + 1);
                    System.Drawing.RectangleF textRect = new System.Drawing.RectangleF(entry.position.X, entry.position.Y, w, h);

                    // Grow batchRect to include this entry which is the intersection of the text and the clip.
                    // TODO (****) In order for clipping to work correctly, we can only process batches with a single entry.  Fix this.
                    batchRectF = System.Drawing.RectangleF.Union(batchRectF, System.Drawing.RectangleF.Intersect(entry.clipRect, textRect));

                    cacheString += entry.text + entry.textColor.ToString() + entry.font.Font.ToString() + entry.font.Font.SizeInPoints.ToString();
                    if (entry.outlineWidth != 0)
                    {
                        cacheString += entry.outlineColor.ToString() + entry.outlineWidth.ToString();
                    }
                    if (entry.scaling != Vector2.One)
                    {
                        cacheString += entry.scaling.ToString();
                    }
                }

                // Creat a pixel aligned clipping rectangle to encompass the batchRect.
                Rectangle clipRect = new Rectangle((int)batchRectF.X, (int)batchRectF.Y, (int)Math.Ceiling(batchRectF.Width + batchRectF.X % 1), (int)Math.Ceiling(batchRectF.Height + batchRectF.Y % 1));

                // We want to shift all the text to start at the origin.  This lets us make
                // maximum use of the bitmap we're rendering to.
                Point offset = new Point(clipRect.X, clipRect.Y);

                // Shift clipRect to match offset position. 
                clipRect.Offset(-offset.X, -offset.Y);

                // Grow clipRect size to a larger multiple of 2 so that we don't get fringing at small scales.
                //clipRect.Width = (clipRect.Width + 8) & 0xfff8;
                //clipRect.Height = (clipRect.Height + 8) & 0xfff8;

#if DEBUG_SPEW
                if (frame == Time.FrameCounter)
                {
                    maxSize.X = Math.Max(maxSize.X, clipRect.Width);
                    maxSize.Y = Math.Max(maxSize.Y, clipRect.Height);
                }
#endif

                // If either of these fire that means you are trying to render too much text in a single
                // call.  So, break it up into multiple calls.  See TextDialog.cs for an example.
                // OR figure out how to tweak this code so that it automatically breaks things up as needed.
                Debug.Assert(clipRect.Width <= texture.Width);
                Debug.Assert(clipRect.Height <= texture.Height);

                // If the size is small enough to fit in a CacheEntry we want to use the cache.
                // So check if it's already there and use it if it is else render it there.
                // If it was too big for a CacheEntry then just render normally.
                if (clipRect.Width <= CacheEntry.Width && clipRect.Height <= CacheEntry.Height)
                {
                    CacheEntry match = null;
                    int matchIndex = -1;
                    for (int i=0; i<cacheList.Count; i++)
                    {
                        // Has cache entry lost it's content?
                        if (cacheList[i].Texture.IsDisposed)
                        {
                            // Invalidate string.
                            cacheList[i].Text = "invalid";
                            // Should we reorder entries to keep good ones at front?
                            // No worry, if they get hit they get pulled up anyway.
                            continue;
                        }
                        if (cacheList[i].Text == cacheString)
                        {
                            match = cacheList[i];
                            matchIndex = i;
                            break;
                        }
                    }

                    if (match != null)
                    {
                        // We've got a match, render the texture associated with it.
                        // Also move it to the front of the list to indicate it was recently used.
                        RenderTexture(camera, match.Texture, match.TargetRect, offset);
                        // If not already at the top, move to top.
                        if (matchIndex != 0)
                        {
                            cacheList.RemoveAt(matchIndex);
                            cacheList.Insert(0, match);
                        }

                        ++numCached;
                    }
                    else
                    {
                        ++numMisses;
                        // No match.  Grab the least recently used cache entry (at the end of the list), 
                        // render to it, and move it to the front of the list.
                        CacheEntry ce = cacheList[CacheEntry.MaxEntries - 1];
                        cacheList.RemoveAt(CacheEntry.MaxEntries - 1);
                        cacheList.Insert(0, ce);

                        // Draw each entry to the bitmap.
                        DrawIntoBitmap(entries, bitmap, offset);

                        // Move the data from the bitmap to the texture.
                        Microsoft.Xna.Framework.Rectangle cachedTargetRect;
                        CopyBitmapToTexture(clipRect, bitmapRect, bitmap, ce.Texture, textureColorData, out cachedTargetRect);

                        // Render the texture to the backbuffer.
                        RenderTexture(camera, ce.Texture, cachedTargetRect, offset);

                        // Clear the bitmap for next call.
                        // TODO (****) is there a dirty rect version of this that is faster?
                        // Maybe try DrawRectangle?
                        graphics.Clear(System.Drawing.Color.Transparent);

                        // Save info for cache testing.
                        ce.TargetRect = cachedTargetRect;
                        ce.Text = cacheString;

                        ++numRendered;
                    }
                }
                else
                {
                    // Too big for caching so render via our big texture.

                    // Treat main bitmap as cachable.
                    if (cacheString != mostRecentlyRenderedString)
                    {
                        // Draw each entry to the bitmap.
                        DrawIntoBitmap(entries, bitmap, offset);

                        // Move the data from the bitmap to the texture.
                        CopyBitmapToTexture(clipRect, bitmapRect, bitmap, texture, textureColorData, out targetRect);

                        mostRecentlyRenderedString = cacheString;
#if DEBUG_SPEW
                        ++numRendered;
#endif
                    }
                    else
                    {
#if DEBUG_SPEW
                        ++numCached;
#endif
                    }

                    // Render the texture to the backbuffer.
                    RenderTexture(camera, texture, targetRect, offset);

                    // Clear the bitmap for next call.
                    // TODO (****) is there a dirty rect version of this that is faster?
                    // Maybe try DrawRectangle?
                    graphics.Clear(System.Drawing.Color.Transparent);
                }

                inBatch = false;

                // Move all batch entries to free list.
                foreach (BatchEntry entry in entries)
                {
                    freeEntries.Add(entry);
                }
                entries.Clear();

            }   // end if entry count > 0

        }   // end of EndBatch()

        #endregion

        #region Internal

        static void DrawIntoBitmap(List<BatchEntry> entries, Bitmap bitmap, Point offset)
        {
            // Draw each entry to the bitmap.

            foreach (BatchEntry entry in entries)
            {
                System.Drawing.Color brushColorFill = System.Drawing.Color.FromArgb(entry.textColor.A, entry.textColor.R, entry.textColor.G, entry.textColor.B);
                SolidBrush brushFill = new SolidBrush(brushColorFill);

                PointF position = new PointF(entry.position.X - offset.X - entry.font.Padding, entry.position.Y - offset.Y);

                bool scaled = false;
                if (entry.scaling != Vector2.One)
                {
                    System.Drawing.Drawing2D.Matrix mat = new System.Drawing.Drawing2D.Matrix();
                    mat.Scale(entry.scaling.X, entry.scaling.Y);
                    graphics.Transform = mat;
                    scaled = true;
                }

                // Draw outlined?
                if (entry.outlineWidth != 0)
                {
                    System.Drawing.Color brushColorOutline = System.Drawing.Color.FromArgb(entry.outlineColor.A, entry.outlineColor.R, entry.outlineColor.G, entry.outlineColor.B);
                    SolidBrush brushOutline = new SolidBrush(brushColorOutline);

                    System.Drawing.Drawing2D.GraphicsPath myPath = new System.Drawing.Drawing2D.GraphicsPath();

                    StringFormat format = StringFormat.GenericDefault;

                    // Add the string to the path.
                    myPath.AddString(entry.text,
                        entry.font.Font.FontFamily,
                        (int)entry.font.Font.Style,
                        entry.font.Font.Size * 96.0f / 72.0f,
                        position,
                        format);

                    // Draw outline.
                    graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                    float zoom = camera != null ? camera.Zoom : 1.0f;
                    // We use 2 * outlineWidth since stroke is centered on edge of text.  For instance,
                    // to get an outline of 3 pixels we need to have a 6 pixel wide stroke since half
                    // the outline stroke will be hidden underneath the main glyph rendering.
                    Pen pen = new Pen(brushOutline, 2.0f * entry.outlineWidth * zoom);
                    pen.LineJoin = System.Drawing.Drawing2D.LineJoin.Round;
                    graphics.DrawPath(pen, myPath);

                    // Fill in center.
                    graphics.FillPath(brushFill, myPath);

                    format.Dispose();
                    myPath.Dispose();
                    brushOutline.Dispose();
                }
                else
                {
                    // Non-outlined case.
                    // Render text with offset.
                    graphics.DrawString(entry.text, entry.font.Font, brushFill, position);
                }

                if (scaled)
                {
                    graphics.Transform = identityMatrix;
                }

                brushFill.Dispose();
            }
        }   // end of DrawIntoBitmap()

        /// <summary>
        /// 
        /// </summary>
        /// <param name="batchRect">Rect around text data we care about.</param>
        /// <param name="bitmapRect">Rect for full size of bitmpa we're rendering into.  Used to prevent rendering outside of valid memory.</param>
        /// <param name="bitmap"></param>
        /// <param name="texture"></param>
        /// <param name="textureData"></param>
        /// <param name="targetRect"></param>
        static void CopyBitmapToTexture(Rectangle batchRect, Rectangle bitmapRect, Bitmap bitmap, Texture2D texture, Color[] textureData, out Microsoft.Xna.Framework.Rectangle targetRect)
        {
            //
            // Transfer the results from the bitmap to the texture.
            //

            // Clip the batchRect to the bitmap.  This is just in case the text we rendered goes outside of the bitmap.
            // We don't want to try and copy data that doesn't exist. 
            batchRect = Rectangle.Intersect(batchRect, bitmapRect);

            // TODO (****) Clip batchRect if it goes outside of the texture.
            // Note this needs to take into account the offset value.


            // Lock down source bits. 
            BitmapData bmpData = bitmap.LockBits(batchRect, ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);

            // Get the address of the first line.
            IntPtr ptr = bmpData.Scan0;
            // Copy data for each scanline.
            for (int i = 0; i < bmpData.Height; i++)
            {
                System.Runtime.InteropServices.Marshal.Copy(ptr + i * bmpData.Stride, argbValues[i], 0, bmpData.Width * 4);
            }
            bitmap.UnlockBits(bmpData);


            // Copy the data to the texture's local memory array.
#if PARALLEL
                Parallel.For(0, batchRect.Height, j =>
#else
            for (int j = 0; j < batchRect.Height; j++)
#endif
            {
                for (int i = 0; i < batchRect.Width; i++)
                {
                    int texelIndex = j * batchRect.Width + i;
                    textureData[texelIndex] = new Color(argbValues[j][i * 4 + 2], argbValues[j][i * 4 + 1], argbValues[j][i * 4 + 0], argbValues[j][i * 4 + 3]);
                    if (textureData[texelIndex] != Color.Transparent && textureData[texelIndex].A != 255)
                    {
                        // Pre-mult alpha.
                        int alpha = textureData[texelIndex].A;
                        textureData[texelIndex].R = (byte)(textureData[texelIndex].R * alpha / 255);
                        textureData[texelIndex].G = (byte)(textureData[texelIndex].G * alpha / 255);
                        textureData[texelIndex].B = (byte)(textureData[texelIndex].B * alpha / 255);
                    }
                }
#if PARALLEL
                });
#else
            }
#endif

            //
            // HACK - Serious hack here.  We occasionally get issues where the system
            // thinks the texture is still set on the device and so it throws on the
            // SetData call.  By drawing with another texture, fully offscreen we 
            // ensure that the texture we care about is not set on the device.  Argh.
            //
            SpriteBatch batch = KoiLibrary.SpriteBatch;
            batch.Begin(SpriteSortMode.Immediate, BlendState.NonPremultiplied);
            {
                batch.Draw(SharedX.BlackButtonTexture, new Vector2(-1000, -1000), Color.White);
            }
            batch.End();

            // Copy the new texture data to the GPU.
            int level = 0;
            targetRect = new Microsoft.Xna.Framework.Rectangle(0, 0, batchRect.Width, batchRect.Height);
            texture.SetData<Color>(level, targetRect, textureData, 0, batchRect.Width * batchRect.Height);

        }   // end of CopyBitmapToTexture()

        static void RenderTexture(SpriteCamera camera, Texture2D texture, Microsoft.Xna.Framework.Rectangle targetRect, Point offset)
        {
            SpriteBatch batch = KoiLibrary.SpriteBatch;
            Microsoft.Xna.Framework.Rectangle destRect = targetRect;
            destRect.Offset(offset);

            if (camera != null)
            {
                Matrix mat = Matrix.CreateScale(1.0f / camera.Zoom);
                mat *= camera.ViewMatrix;
                batch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, samplerState: null, depthStencilState: null, rasterizerState: null, effect: null, transformMatrix: mat);
            }
            else
            {
                batch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);
            }
            {
                // Subtracting a single pixel here keeps random garbage showing up
                // when rotated and scaled small.  Problem appears to be sampling 
                // neighboring pixels.  This shouldn't cause any clipping since 
                // we're kind of generous with our rect sizes to begin with.
                --destRect.Width;
                --destRect.Height;
                --targetRect.Width;
                --targetRect.Height;

                batch.Draw(texture, destRect, targetRect, Color.White);

                //batch.Draw(Boku.Commonures.Get(@"White"), destRect, Color.Red * 0.2f); 
            }
            batch.End();
        }   // end of RenderTexture()

        static string SystemFontKey(string familyName, float emSize, FontStyle style)
        {
            string fontKey = familyName + " " + emSize.ToString() + style.ToString();
            return fontKey;
        }

        public static SystemFont GetSystemFont(string familyName, float emSize, FontStyle style)
        {
            // Force font sizes to be increments of 0.1.
            emSize = (float)Math.Round(emSize, 1);
            emSize = Math.Max(emSize, 0.1f);

            string systemFontKey = SystemFontKey(familyName, emSize, style);

            SystemFont systemFont = null;
            if (!systemFonts.TryGetValue(systemFontKey, out systemFont))
            {
                systemFont = new SystemFont(familyName, emSize, style);
                systemFonts.Add(systemFontKey, systemFont);
            }

            return systemFont;
        }

        #endregion
    }   // end of class SysFont
#endif
}   // end of namespace KoiX.Text
