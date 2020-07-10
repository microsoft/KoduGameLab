// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.


using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.Serialization.Formatters.Binary;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using KoiX;
using KoiX.Geometry;
using KoiX.Input;
using KoiX.Text;

using Boku.Common;

namespace KoiX.UI
{
    /// <summary>
    /// A widget which displays a blob of text within a fixed frame.  
    /// If text is longer than easily can fit into the box, it can be scrolled.
    /// 
    /// For the text display we want the rendertarget that the text is 
    /// written to to be 1:1 scaled with the screen to prevent any 
    /// sampling issues.  So, even though this gets a camera with some
    /// zoom value we convert everything to screen coords for rendering.
    /// Yes, it's a bit confusing but it keeps the text rendering 1:1 with
    /// the screen pixels so it stays absolutely crisp.
    /// 
    /// </summary>
    public class ScrollableTextBox: BaseWidget
    {
        const int numBufferLines = 6;   // Number of extra lines of text we allow for above and below
                                        // the current window.

        #region Members

        RenderTarget2D textRT;      // Contains the text being displayed.  Should be 1:1 with screen resolution.
        RenderTarget2D tmpRT;       // Used when scrolling to copy common chunks of text.

        //bool draggingText = false;
        int numLinesInRT;           // How many lines of text can the RT hold?
        float numLinesInWindow;     // How many lines can the window display.  May be fractional.

        float textOffset;           // Amount text has been scrolled, in pixels.  This is measured from top of text blob.
        float rtOffset;             // This is the offset from the start of the blob to the first line in the RT.  This
                                    // will always be a multiple of blob.TotalSpacing

        string textId;              // Id for displayed text.

        TextBlob blob;
        SystemFont font;            // Requested font.
        SystemFont blobFont;        // Scaled font, used by blob.

        bool textDirty = true;      // Need a recalc and refresh of the RT?  

        SpriteCamera camera;        // Just a local ref we can hang on to.
        float scaleFactor = 1.0f;   // Based on the above camera's zoom.
        SpriteCamera pixelCamera;   // Camera in pixel space.

        // Rect for text region in dialog coords which uses camera scaling.
        RectangleF rect;
        bool draggingText = false;  // Are we draggin the text?

        // Left/right margin for text.
        new float margin = 16.0f;
        float knobRadius = 32;

        #endregion

        #region Accessors

        /// <summary>
        /// Id for string to be displayed in the body of the dialog.
        /// </summary>
        public string TextId
        {
            get { return textId; }
            set
            {
                if (textId != value)
                {
                    textId = value;
                    BodyText = Strings.Localize(textId);

                    // Force scrolling back to top.
                    textOffset = 0;
                    rtOffset = 0;
                    Dirty = true;
                    textDirty = true;
                }
            }
        }

        /// <summary>
        /// Text to be displayed in main body of dialog.  Needs to 
        /// be the localized version if being set.
        /// If this changes, the size of the dialog may change in 
        /// response.
        /// </summary>
        public string BodyText
        {
            get { return blob.RawText; }
            set
            {
                if (blob.RawText != value)
                {
                    blob.RawText = value;
                    textId = null;

                    // Force scrolling back to top.
                    textOffset = 0;
                    rtOffset = 0;
                    Dirty = true;
                    textDirty = true;

                    Recalc();
                }
            }
        }

        public TextHelper.Justification Justification
        {
            get { return blob.Justification; }
            set { blob.Justification = value; }
        }

        #endregion

        #region Public

        public ScrollableTextBox(BaseDialog parentDialog, RectangleF rect, SystemFont font, string textId = null, string text = null, ThemeSet theme = null, string id = null, Object data = null)
            : base(parentDialog, theme: theme, id : id, data: data)
        {
            Debug.Assert(textId == null || text == null, "Only one should be set");

            this.rect = rect;
            this.localRect = rect;
            this.font = font;

            this.textId = textId;

            FontWrapper wrapper = new FontWrapper(null, font);
            GetFont Font = delegate() { return wrapper; };

            blob = new TextBlob(Font, text, (int)(rect.Width - 2 * margin));

            // Get localized versions of string if we have Ids.
            if (textId != null)
            {
                blob.RawText = Strings.Localize(textId);
            }

            // Set 'theme' to ref our current theme which may have changed in base c'tor.
            theme = Theme.CurrentThemeSet;

            Dirty = true;
            textDirty = true;
            Recalc();

            pixelCamera = new SpriteCamera();
            pixelCamera.Update();

        }   // end of c'tor

        public void Recalc()
        {
            float zoom = camera == null ? 1.0f : camera.Zoom;

            // Based on the current rect size, calc sizes for everything else.
            if (/*Dirty*/ textDirty || scaleFactor != zoom)
            {
                // Force scrolling back to top.
                textOffset = 0;
                rtOffset = 0;
                Dirty = true;
                textDirty = true;

                scaleFactor = zoom;

                knobRadius = 32.0f * scaleFactor;
                margin = 16.0f * scaleFactor;

                blobFont = SysFont.GetSystemFont(font.Font.FontFamily.Name, font.Font.Size * scaleFactor, font.Font.Style);
                FontWrapper wrapper = new FontWrapper(null, blobFont);
                GetFont Font = delegate() { return wrapper; };
                blob.Font = Font;

                int numLines = (int)(rect.Height / blob.TotalSpacing);

                // Calc _scaled_ width and set on blob.
                // Leave a bit of a margin on either side so that the text isn't right on
                // the edge.  This means that the blobWidth will be smaller than textWidth.
                // Realistically the 3 below should be a 2 since we are subtracting off the 
                // same margin from either side.  For some reason, the occasional line of 
                // text looks like it is going long.  So, by making this 3 we double the 
                // margin on the right edge making things look better.
                // TODO (****) Should we shift everything a bit to the right for R-to-L languages?
                int blobWidth = (int)((rect.Width - 2 * 16.0f) * scaleFactor);
                blob.Width = blobWidth;

                // textRect size is already set, so now set the position.
                Vector2 textPos = rect.Position * scaleFactor;
                localRect.SetPosition(textPos.Round());
            }

        }   // end of Recalc()

        public override void Update(SpriteCamera camera, Vector2 parentPosition)
        {
            this.camera = camera;

            // Set UI camera to standard position/zoom to match current resolution.
            KoiX.Managers.BaseScene.SetCameraToTargetResolution(camera);
            camera.Update();

            // Adjust the pixel camera to have a half pixel offset if the screensize is even.
            // This ensures that the text stays pixel aligned.  Without this, we get sampling
            // errors and blurry text.
            Vector2 pos = 0.5f * new Vector2(1 - ((int)pixelCamera.ScreenSize.X & 0x01), 1 - ((int)pixelCamera.ScreenSize.Y & 0x01));
            pixelCamera.Position = pos;
            pixelCamera.Update();

            if (scaleFactor != camera.Zoom)
            {
                Dirty = true;
                textDirty = true;
            }

            Recalc();
            RefreshRT(camera);

            // Clear this here since both Recalc() and RefreshRT() use it.
            textDirty = false;

            base.Update(camera, parentPosition);
        }   // end of Update()

        public override void Render(SpriteCamera camera, Vector2 parentPosition)
        {
            RectangleF renderRect = rect;
            renderRect.Position = ((parentPosition + renderRect.Position) * scaleFactor).Round();

            // DEBUG HACK!!!   Hide local version...
            //Texture2D textRT = KoiLibrary.LoadTexture2D(@"Textures\Registration1024");

            // The size that the rt would be for a perfect fit.  It may be larger than this
            // since we don't constantly reallocate it.
            Point rtTargetSize = (rect.Size * scaleFactor).RoundToPoint();

            renderRect.Size = rtTargetSize.ToVector2();

            //
            // We want to render the text using pixelCamera so we can be assured of
            // exact 1:1 pixels.  Trying to do this in camera space ends up with
            // texture sampling foo and blurry text.

            // Need to adjust padding so that textRT ends up being 1:1 with pixels and text looks great.
            int left = 0;
            int top = -(int)(textOffset - rtOffset);
            int right = -(int)(left + (textRT.Width - rtTargetSize.X));
            int bottom = -(int)(top + (textRT.Height - rtTargetSize.Y));

            Padding padding = new Padding(left, top, right, bottom);

            RoundedRect.Render(pixelCamera, renderRect, theme.BaseCornerRadius, Color.White,
                                texture: textRT, texturePadding: padding,
                                outlineColor: Color.Black, outlineWidth: scaleFactor * 2.0f,
                                shadowStyle: ShadowStyle.Inner, shadowSize: scaleFactor * 8.0f, shadowOffset: scaleFactor * new Vector2(6, 8), shadowAttenuation: 1.0f);

            // If gamepad is being used, show right stick icon.
            if (KoiLibrary.LastTouchedDeviceIsGamepad && blob.NumLines > numLinesInWindow)
            {
                Disc.Render(pixelCamera, new Vector2(renderRect.Right, renderRect.Top) + new Vector2(knobRadius, 2 * knobRadius), 1.5f * knobRadius, Color.Black,
                            texture: Textures.Get("GamePad RightStick"), texturePadding: new Padding(-2),   // The padding hides the underlying disc.
                            shadowStyle: ShadowStyle.Outer, shadowSize: 4.0f, shadowOffset: new Vector2(2, 3), shadowAttenuation: 0.8f);
            }

            base.Render(camera, parentPosition);
        }   // end of Render()

        public override void RegisterForInputEvents()
        {
            // Events used to support scrolling.
            KoiLibrary.InputEventManager.RegisterForEvent(this, InputEventManager.Event.MouseLeftDown);
            KoiLibrary.InputEventManager.RegisterForEvent(this, InputEventManager.Event.MouseWheel);
            KoiLibrary.InputEventManager.RegisterForEvent(this, InputEventManager.Event.OnePointDrag);
            KoiLibrary.InputEventManager.RegisterForEvent(this, InputEventManager.Event.GamePad);

        }   // end of RegisterForInputEvents()

        public override bool ProcessMouseLeftDownEvent(MouseInput input)
        {
            Debug.Assert(Active);

            Vector2 hit = camera.ScreenToCamera(input.Position);

            if (rect.Contains(hit))
            {
                draggingText = true;
                KoiLibrary.InputEventManager.RegisterForEvent(this, InputEventManager.Event.MouseLeftUp);
                KoiLibrary.InputEventManager.RegisterForEvent(this, InputEventManager.Event.MouseMove);

                return true;
            }

            return base.ProcessMouseLeftDownEvent(input);
        }

        public override bool ProcessMouseMoveEvent(MouseInput input)
        {
            Debug.Assert(Active);

            if (draggingText)
            {
                Scroll(-input.DeltaPosition.Y);

                return true;
            }

            return base.ProcessMouseMoveEvent(input);
        }

        public override bool ProcessMouseWheelEvent(MouseInput input)
        {
            Debug.Assert(Active);

            if (KoiLibrary.InputEventManager.MouseHitObject == this)
            {
                float deltaPixels = -input.E.Delta / 120.0f * blob.TotalSpacing / 2.0f;
                Scroll(deltaPixels);

                return true;
            }

            return base.ProcessMouseWheelEvent(input);
        }

        public override bool ProcessMouseLeftUpEvent(MouseInput input)
        {
            Debug.Assert(Active);

            if (draggingText)
            {
                draggingText = false;

                KoiLibrary.InputEventManager.UnregisterForEvent(this, InputEventManager.Event.MouseLeftUp);
                KoiLibrary.InputEventManager.UnregisterForEvent(this, InputEventManager.Event.MouseMove);
            }

            return base.ProcessMouseLeftUpEvent(input);
        }

        public override bool ProcessTouchOnePointDragEvent(OnePointDragGestureEventArgs gesture)
        {
            Debug.Assert(Active);

            if (!draggingText && gesture.Gesture == GestureType.OnePointDragBegin)
            {
                Vector2 hit = camera.ScreenToCamera(gesture.StartPosition);
                if (rect.Contains(hit))
                {
                    draggingText = true;
                }
            }
            if (draggingText)
            {
                Scroll(-gesture.DeltaPosition.Y);

                if (draggingText)
                {
                    if (gesture.Gesture == GestureType.OnePointDragEnd)
                    {
                        draggingText = false;
                    }
                }

                return true;
            }

            return base.ProcessTouchOnePointDragEvent(gesture);
        }

        public override bool ProcessGamePadEvent(GamePadInput pad)
        {
            Debug.Assert(Active);

            if (pad.RightStick.Y != 0)
            {
                float tics = Time.WallClockFrameSeconds;
                float deltaPixels = 1000.0f * tics * pad.RightStick.Y;
                Scroll(-deltaPixels);
                return true;
            }

            return base.ProcessGamePadEvent(pad);
        }

        #endregion

        #region Internal

        /// <summary>
        /// Scrolls the text.  If needed, will also refresh the RT.
        /// Clamps to valid range.
        /// </summary>
        /// <param name="deltaPixels">Amount to scroll in pixels.  Positive scrolls down in text (ie texture moves up).</param>
        void Scroll(float deltaPixels)
        {
            // Nothing to do here, move along.
            if (deltaPixels == 0)
            {
                return;
            }

            float prevTextOffset = textOffset;

            // Apply the scrolling.
            textOffset += deltaPixels;

            // Clamp to valid range.
            if (textOffset < 0)
            {
                textOffset = 0;
            }
            else
            {
                // Calc the max line we want to use as our start and make sure we haven't gone beyond there.
                // Also, don't let this go negative.
                // The -1 below just lets this display an extra blank line at the end.  This helps make
                // it more clear to the user that there is no more.
                int maxStartLine = Math.Max(0, blob.NumLines - (int)(numLinesInWindow - 1));
                textOffset = Math.Min(textOffset, maxStartLine * blob.TotalSpacing);
            }

            // After adjusting for clamping, if the textOffset didn't change, nothing to do here.
            if (textOffset == prevTextOffset)
            {
                return;
            }

            //
            // Now that we know what we want to display, look at the RT and see if
            // we have that text available.  If we do, then no problem.  If we don't
            // then we need to do some scrolling magic.
            //

            // If all the text fits on the current RT then we're good.  No
            // need to do anything else.  Especially for smaller text blobs
            // this will be the default.
            if (numLinesInRT >= blob.NumLines)
            {
                return;
            }

            // If what we want to display is available on the textRT then
            // we don't need to refresh.
            // First, check we haven't scrolled back up.
            if (textOffset >= rtOffset)
            {
                // Now check we haven't scrolled too far down.
                if (textOffset - rtOffset + rect.Height <= textRT.Height)
                {
                    // All's good, no need to touch the textRT.
                    return;
                }
            }

            // Figure out what we want the new rtOffset to be.
            int desiredRTOffset = Math.Max(0, (int)(textOffset - numBufferLines * blob.TotalSpacing));
            // Round so we're at nearest full line.
            desiredRTOffset = blob.TotalSpacing * (int)(Math.Round(desiredRTOffset / (float)blob.TotalSpacing));

            // If the rtOffset is not going to change we don't need to change
            // the rt contents.  This may happen right at the ends of the range.
            if (desiredRTOffset == rtOffset)
            {
                return;
            }

            // Ok, it doesn't all fit.  Time to update textRT.
            GraphicsDevice device = KoiLibrary.GraphicsDevice;
            SpriteBatch batch = KoiLibrary.SpriteBatch;

            // Set rt to copy over reusable bits of text.
            device.SetRenderTarget(tmpRT);
            device.Clear(Color.Transparent);

            // First check if we've scrolled backwards and need to move back up.
            if (deltaPixels < 0)
            {
                // Figure out how much is reusable.  If we're 
                // scrolling really fast, this may be none.
                int usableSize = (int)(textRT.Height - (rtOffset - desiredRTOffset));
                int usableLines = (int)(usableSize / (float)blob.TotalSpacing);

                if (usableSize > 0)
                {
                    batch.Begin();
                    {
                        Rectangle srcRect = new Rectangle(0, 0, textRT.Width, usableSize);
                        Rectangle dstRect = new Rectangle(0, textRT.Height - usableSize, textRT.Width, usableSize);
                        batch.Draw(textRT, dstRect, srcRect, Color.White);
                    }
                    batch.End();

                    // Fill in missing text at top.
                    int startLine = (int)(desiredRTOffset / (float)blob.TotalSpacing);
                    blob.RenderText(null, new Vector2(margin, 0), theme.DarkTextColor, startLine: startLine, maxLines: numLinesInRT - usableLines);
                }
                else
                {
                    Debug.Assert(false, "Should never get here");
                    // No usable region so just redraw all text.
                    int startLine = (int)(desiredRTOffset / (float)blob.TotalSpacing);
                    blob.RenderText(null, new Vector2(margin, 0), theme.DarkTextColor, startLine: startLine, maxLines: numLinesInRT);
                }

            }
            else
            {
                // We're scrolling down and have scrolled further than the content in textRT.

                // Figure out how much is reusable.  If we're 
                // scrolling really fast, this may be none.
                int usableSize = (int)(textRT.Height - (desiredRTOffset - rtOffset));
                int usableLines = (int)(usableSize / (float)blob.TotalSpacing);

                if (usableSize > 0)
                {
                    batch.Begin();
                    {
                        Rectangle srcRect = new Rectangle(0, textRT.Height - usableSize, textRT.Width, usableSize);
                        Rectangle dstRect = new Rectangle(0, 0, textRT.Width, usableSize);
                        batch.Draw(textRT, dstRect, srcRect, Color.White);
                    }
                    batch.End();

                    // Fill in missing text at bottom.
                    int startLine = (int)((desiredRTOffset + usableSize) / (float)blob.TotalSpacing);
                    blob.RenderText(null, new Vector2(margin, usableSize), theme.DarkTextColor, startLine: startLine, maxLines: numLinesInRT - usableLines);
                }
                else
                {
                    Debug.Assert(false, "Should never get here");
                    // No usable region so just redraw all text.
                    int startLine = (int)(desiredRTOffset / (float)blob.TotalSpacing);
                    blob.RenderText(null, new Vector2(margin, 0), theme.DarkTextColor, startLine: startLine, maxLines: numLinesInRT);
                }

            }

            rtOffset = desiredRTOffset;

            device.SetRenderTarget(null);

            // Swap RTs.
            RenderTarget2D tmp = textRT;
            textRT = tmpRT;
            tmpRT = tmp;

        }   // end of Scroll()

        /// <summary>
        /// Updates the textRT with the display text in it.  This happens if we scroll, 
        /// if the RT loses the content, or if the screen size changes.
        /// </summary>
        /// <param name="camera"></param>
        void RefreshRT(SpriteCamera camera)
        {
            bool refreshNeeded = textDirty;

            Point rtWindowSize = (rect.Size * scaleFactor).RoundToPoint();

            // How many lines do we want to make room for.  The additional buffer lines
            // at the beginning and end give us room when scrolling so we don't have to
            // always refresh the texture.
            int numLines = (int)Math.Ceiling(rtWindowSize.Y / (float)blob.TotalSpacing + 2 * numBufferLines);
            int desiredRTHeight = numLines * blob.TotalSpacing;

            // Limit max RT size to 2k.
            if (desiredRTHeight > 2048)
            {
                numLines = (int)(2048 / (float)blob.TotalSpacing);
                desiredRTHeight = numLines * blob.TotalSpacing;
            }

            // Do we need to reallocate the rendertargets?
            if (textRT == null || textRT.IsDisposed || textRT.GraphicsDevice.IsDisposed || textRT.Width < rtWindowSize.X || textRT.Height < desiredRTHeight)
            {
                DeviceResetX.Release(ref textRT);
                textRT = new RenderTarget2D(KoiLibrary.GraphicsDevice, rtWindowSize.X, desiredRTHeight);

                DeviceResetX.Release(ref tmpRT);
                tmpRT = new RenderTarget2D(KoiLibrary.GraphicsDevice, rtWindowSize.X, desiredRTHeight);

                refreshNeeded = true;

                numLinesInRT = numLines;

                // Need to reset scrolling to top.
                textOffset = 0;
                rtOffset = 0;
            }
            else
            {
                // If we've changed zoom the font may be smaller which means
                // we can fit more text onto the textRT.
                int rtLines = (int)(textRT.Height / (float)blob.TotalSpacing);
                if (rtLines != numLinesInRT)
                {
                    numLinesInRT = rtLines;
                    refreshNeeded = true;
                }
            }

            // This was inside the above "if" block but this causes it to fail to
            // update when the window size changes.
            numLinesInWindow = rtWindowSize.Y / (float)blob.TotalSpacing;

            if (textRT.IsContentLost)
            {
                refreshNeeded = true;
            }

            if (refreshNeeded)
            {
                GraphicsDevice device = KoiLibrary.GraphicsDevice;

                device.SetRenderTarget(textRT);
                device.Clear(Color.Transparent);

                // If we're displaying a text blob that fully fits and has some blank space,
                // try and center the text vertically.
                float blank = (numLinesInWindow - blob.NumLines) * blob.TotalSpacing;
                if (blank > 0)
                {
                    // Note, technically this should be 2.0 but by making it larger
                    // we shift the text up slightly which looks better.
                    blank = (int)(blank / 2.2f);
                }
                else
                {
                    blank = 0;
                }
                Vector2 pos = new Vector2(margin, -rtOffset + blank);
                pos = pos.Truncate();

                // By passing null for the camera we get 0, 0 in the upper left hand corner and no zoom.
                // Currently we need to break the rendering into chunks since SysFont has a vertical max
                // of 1024 pixels.
                int numLinesAtATime = 1024 / blob.TotalSpacing;
                int curLine = 0;
                while (curLine < numLinesInRT)
                {
                    // Limit the number of lines we're rendering to the minimum of
                    // the number of lines we can render and the number of lines in
                    // the blob and the size of the RT.
                    numLines = Math.Min(numLinesAtATime, blob.NumLines - curLine);
                    numLines = Math.Min(numLines, numLinesInRT - curLine);
                    blob.RenderText(null, pos + new Vector2(0, curLine * blob.TotalSpacing), theme.DarkTextColor, startLine: curLine, maxLines: numLinesAtATime);
                    curLine += numLinesAtATime;
                }
                //blob.RenderText(null, pos, themeSet.DarkTextColor, startLine: 0, maxLines: numLinesInRT);

                device.SetRenderTarget(null);
            }

        }   // end of RefreshRT()


        #endregion


    }   // end of class ScrollableTextBox
}   // end of namespace KoiX.UI
