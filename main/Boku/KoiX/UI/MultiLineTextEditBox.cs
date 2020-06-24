
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.Serialization.Formatters.Binary;

using Microsoft.Xna.Framework;

using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

using KoiX;
using KoiX.Geometry;
using KoiX.Input;
using KoiX.Text;

using Boku.Audio;
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
    public class MultiLineTextEditBox : BaseWidget
    {
        const int numBufferLines = 6;   // Number of extra lines of text we allow for above and below
                                        // the current window.

        #region Members

        RenderTarget2D textRT;      // Contains the text being displayed.  Should be 1:1 with screen resolution.
        RenderTarget2D tmpRT;       // Used when scrolling to copy common chunks of text.

        int numLinesInRT;           // How many lines of text can the RT hold?
        float numLinesInWindow;     // How many lines can the window display.  May be fractional.

        Twitchable<int> textOffset; // Amount text has been scrolled, in pixels.  This is measured from top of text blob.
        int rtOffset;               // This is the offset from the start of the blob to the first line in the RT.  This
                                    // will always be a multiple of blob.TotalSpacing except when dragging.

        string textId;              // Id for displayed text.

        TextBlob blob;
        GetFont Font;
        SystemFont font;            // Requested font.
        SystemFont blobFont;        // Scaled font, used by blob to ensure 1 to 1 pixel scaling.
        SpriteCamera blobCamera;    // Camera used for rendering blob text.  Only really needed for setting cursor to correct position.

        bool textDirty = true;      // Need a recalc and refresh of the RT?  

        SpriteCamera camera;        // Just a local ref we can hang on to.
        float scaleFactor = 1.0f;   // Based on the above camera's zoom.
        SpriteCamera pixelCamera;   // Camera in pixel space.

        UIState prevCombinedState = UIState.Inactive;

        Twitchable<Color> bodyColor;

        Twitchable<Color> outlineColor;
        Twitchable<float> outlineWidth;

        Twitchable<Color> textColor;

        TextEditBoxTheme curTheme;  // Current theme settings based on state.

        // Rect for text region in dialog coords which uses camera scaling.
        RectangleF rect;
        bool draggingText = false;  // Using mouse or touch to drag scrolling.
        bool clickNotDrag = false;  // On left down, this is set to true.  On dragging, returns to false.  On up
                                    // we can use this to differentiate a click versus a drag.

        // Left/right margin for text.
        new float margin = 16.0f;
        float knobRadius = 32;

        bool prevInFocus = false;   // If the InFocus state changes, we need to re-render since the cursor
                                    // is only rendered when in focus.

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
                    textOffset.Value = 0;
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
                    textOffset.Value = 0;
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

        public MultiLineTextEditBox(BaseDialog parentDialog, RectangleF rect, GetFont Font, string textId = null, string text = null, ThemeSet theme = null, string id = null, Object data = null)
            : base(parentDialog, theme: theme, id: id, data: data)
        {
            Debug.Assert(theme != null);

            this.theme = theme;
            curTheme = theme.TextEditBoxNormal;
            
            Debug.Assert(textId == null || text == null, "Only one should be set");

            textOffset = new Twitchable<int>(0.2f, TwitchCurve.Shape.EaseOut, startingValue: 0);

            this.rect = rect;
            this.localRect = rect;
            this.Font = Font;
            this.font = Font().systemFont;

            bodyColor = new Twitchable<Color>(Theme.QuickTwitchTime, TwitchCurve.Shape.EaseInOut, this, startingValue: curTheme.BodyColor);

            outlineColor = new Twitchable<Color>(Theme.QuickTwitchTime, TwitchCurve.Shape.EaseInOut, this, startingValue: curTheme.OutlineColor);
            outlineWidth = new Twitchable<float>(Theme.QuickTwitchTime, TwitchCurve.Shape.EaseInOut, this, startingValue: curTheme.OutlineWidth);

            textColor = new Twitchable<Color>(Theme.QuickTwitchTime, TwitchCurve.Shape.EaseInOut, this, startingValue: curTheme.TextColor);

            this.textId = textId;

            FontWrapper wrapper = new FontWrapper(null, font);
            GetFont BlobFont = delegate() { return wrapper; };

            blob = new TextBlob(BlobFont, text, (int)(rect.Width - 2 * margin));

            blobCamera = new SpriteCamera();

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
            // Note that textDirty gets set even if just the cursor moves since
            // we still need to re-render.
            if (/*Dirty textDirty ||*/ scaleFactor != zoom)
            {
                // Force scrolling back to top.
                textOffset.Value = 0;
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
                // TODO (scoy) Should we shift everything a bit to the right for R-to-L languages?
                int blobWidth = (int)((rect.Width - 2 * 16.0f) * scaleFactor);
                blob.Width = blobWidth;

            }

        }   // end of Recalc()

        public override void Update(SpriteCamera camera, Vector2 parentPosition)
        {
            this.camera = camera;

            if (Active)
            {
                if (InFocus != prevInFocus)
                {
                    textDirty = true;
                }
                prevInFocus = InFocus;

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

                // Needed to handle focus changes.
                base.Update(camera, parentPosition);

                UIState combinedState = CombinedState;
                if (combinedState != prevCombinedState)
                {
                    // Set new state params.  Note that dirty flag gets
                    // set internally by setting individual values so
                    // we don't need to worry about it here.
                    switch (combinedState)
                    {
                        case UIState.Disabled:
                            curTheme = theme.TextEditBoxDisabled;
                            break;

                        case UIState.Active:
                        case UIState.ActiveHover:
                        case UIState.ActiveSelected:
                        case UIState.ActiveSelectedHover:
                            curTheme = theme.TextEditBoxNormal;
                            break;

                        case UIState.ActiveFocused:
                        case UIState.ActiveFocusedHover:
                        case UIState.ActiveSelectedFocused:
                        case UIState.ActiveSelectedFocusedHover:
                            curTheme = theme.TextEditBoxNormalFocused;
                            break;

                        default:
                            // Should only happen on state.None
                            break;

                    }   // end of switch

                    // Now that we have the new theme, set all the Twitchable values from it.
                    // Non-twitchable values we get directly from the theme.
                    bodyColor.Value = curTheme.BodyColor;
                    outlineColor.Value = curTheme.OutlineColor;
                    outlineWidth.Value = curTheme.OutlineWidth;

                    textColor.Value = curTheme.TextColor;

                    prevCombinedState = combinedState;
                    dirty = true;
                    this.camera = camera;
                }   // end if state changed

            }   // end if Active
        }   // end of Update()

        public override void Render(SpriteCamera camera, Vector2 parentPosition)
        {
            RectangleF renderRect = rect;
            renderRect.Position = ((parentPosition + localRect.Position + renderRect.Position) * scaleFactor).Round();

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
            int top = -(int)(textOffset.Value - rtOffset);
            int right = -(int)(left + (textRT.Width - rtTargetSize.X));
            int bottom = -(int)(top + (textRT.Height - rtTargetSize.Y));

            Padding padding = new Padding(left, top, right, bottom);
            
            RoundedRect.Render(pixelCamera, renderRect, scaleFactor * curTheme.CornerRadius, Color.White,
                                texture: textRT, texturePadding: padding,
                                outlineColor: outlineColor.Value, outlineWidth: scaleFactor * outlineWidth.Value
                                //shadowStyle: ShadowStyle.Inner, shadowSize: scaleFactor * 8.0f, shadowOffset: scaleFactor * new Vector2(6, 8), shadowAttenuation: 1.0f
                                );
            
            // If gamepad is being used, show right stick icon.
            if (KoiLibrary.LastTouchedDeviceIsGamepad && blob.NumLines > numLinesInWindow)
            {
                Disc.Render(pixelCamera, new Vector2(renderRect.Right, renderRect.Top) + new Vector2(knobRadius, 2 * knobRadius), 1.5f * knobRadius, Color.Black,
                            texture: Textures.Get("GamePad RightStick"), texturePadding: new Padding(-2),   // The padding hides the underlying disc.
                            shadowStyle: ShadowStyle.Outer, shadowSize: 4.0f, shadowOffset: new Vector2(2, 3), shadowAttenuation: 0.8f);
            }

            base.Render(camera, parentPosition);
        }   // end of Render()

        public override void Activate(params object[] args)
        {
            blob.Home();
            textDirty = true;
            
            base.Activate(args);
        }   // end of Activate()

        public override void RegisterForInputEvents()
        {
            // Events used to support scrolling.
            KoiLibrary.InputEventManager.RegisterForEvent(this, InputEventManager.Event.MouseLeftDown);
            KoiLibrary.InputEventManager.RegisterForEvent(this, InputEventManager.Event.MouseWheel);
            KoiLibrary.InputEventManager.RegisterForEvent(this, InputEventManager.Event.OnePointDrag);
            // Set focus and set cursor position.
            KoiLibrary.InputEventManager.RegisterForEvent(this, InputEventManager.Event.Tap);
            // Register to get keyboard input. 
            KoiLibrary.InputEventManager.RegisterForEvent(this, InputEventManager.Event.Keyboard);          // Control keys.
            KoiLibrary.InputEventManager.RegisterForEvent(this, InputEventManager.Event.WinFormsKeyboard);  // Text.

            KoiLibrary.InputEventManager.RegisterForEvent(this, InputEventManager.Event.GamePad);

        }   // end of RegisterForInputEvents()

        public override bool ProcessMouseLeftDownEvent(MouseInput input)
        {
            Debug.Assert(Active);

            // Move from Window coords to camera coords,
            Vector2 hit = camera.ScreenToCamera(input.Position);
            // into dialog coords,
            hit -= ParentDialog.Rectangle.Position;
            // into parent container coords,
            hit -= parentPosition;
            // and finally into widget/rt coords.
            hit -= LocalRect.Position;

            if (rect.Contains(hit))
            {
                draggingText = true;
                clickNotDrag = true;
                KoiLibrary.InputEventManager.RegisterForEvent(this, InputEventManager.Event.MouseLeftUp);
                KoiLibrary.InputEventManager.RegisterForEvent(this, InputEventManager.Event.MouseMove);

                // Whether dragging or setting the cursor position, we always want to grab focus.
                SetFocus();

                return true;
            }

            return base.ProcessMouseLeftDownEvent(input);
        }

        public override bool ProcessMouseMoveEvent(MouseInput input)
        {
            Debug.Assert(Active);

            if (draggingText)
            {
                Scroll((int)(-input.DeltaPosition.Y));

                if (input.DeltaPosition.Y != 0)
                {
                    clickNotDrag = false;
                }

                return true;
            }

            return base.ProcessMouseMoveEvent(input);
        }

        public override bool ProcessMouseWheelEvent(MouseInput input)
        {
            Debug.Assert(Active);

            if (KoiLibrary.InputEventManager.MouseHitObject == this)
            {
                int deltaPixels = (int)(-input.E.Delta / 120.0f * blob.TotalSpacing / 2.0f);
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

                if (clickNotDrag)
                {
                    // User just clicked on form so set cursor.
                    Vector2 hit = input.Position;
                    Vector2 blobPos = ParentDialog.Rectangle.Position + LocalRect.Position + ParentPosition;    // All in camera coords.
                    Vector2 blobScreenPos = camera.CameraToScreen(blobPos);
                    blobScreenPos.Y -= textOffset.Value;
                    Vector2 pos = input.Position - blobScreenPos;
                    blob.SetCursorToMousePosition(pos);

                    // We've moved the cursor.
                    textDirty = true;
                }
                else
                {
                    // Dragging, already handled in MouseMove.  Nothing to do here.
                }

                clickNotDrag = false;
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
                Scroll((int)(-gesture.DeltaPosition.Y));

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
        }   // end of ProcessTouchOnePointDragEvent()

        public override bool ProcessWinFormsKeyboardEvent(KeyInput input)
        {
            Debug.Assert(Active);

            if (InFocus)
            {
                switch (input.Key)
                {
                    default:
                        if (input.AsciiChar == 8)
                        {
                            return false;
                        }
                        else
                        {
                            char c = input.AsciiChar;
                            // Ignore control characters.
                            if (!char.IsControl(c))
                            {
                                if (!blob.InsertString(input.AsciiChar.ToString()))
                                {
                                    Foley.PlayNoBudget();
                                }
                                OnChange();
                                textDirty = true;
                            }
                        }
                        break;
                }

                return true;
            }

            return base.ProcessWinFormsKeyboardEvent(input);
        }   // end of ProcessWinFormsKeyboardEvent()

        public override bool ProcessKeyboardEvent(KeyInput input)
        {
            Debug.Assert(Active);

            if (InFocus)
            {
                bool result = true;

                switch (input.Key)
                {
                    case Keys.Right:
                        blob.CursorRight();
                        break;
                    case Keys.Left:
                        blob.CursorLeft();
                        break;
                    case Keys.Up:
                        blob.CursorUp();
                        break;
                    case Keys.Down:
                        blob.CursorDown();
                        break;
                    case Keys.Back:
                        blob.Backspace();
                        break;
                    case Keys.Delete:
                        blob.Delete();
                        break;
                    case Keys.Home:
                        blob.Home();
                        break;
                    case Keys.End:
                        blob.End();
                        break;

                    case Keys.Enter:
                        blob.InsertString("\n");
                        break;

                    case Keys.Escape:
                    case Keys.Tab:
                        // Do nothing.  These handled at dialog level.
                        //Debug.Assert(false, "Shouldn't the dialog get these first?");
                        return false;

                    default:
                        // Ignore all the "regular" keys since they get picked up by the other input handler.
                        break;
                }

                if (result)
                {
                    textDirty = true;
                }

                return result;
            }

            return base.ProcessKeyboardEvent(input);
        }   // end of ProcessKeyboardEvent()


        public override bool ProcessGamePadEvent(GamePadInput pad)
        {
            Debug.Assert(Active);

            if (pad.RightStick.Y != 0)
            {
                float tics = Time.WallClockFrameSeconds;
                float deltaPixels = 1000.0f * tics * pad.RightStick.Y;
                Scroll((int)(-deltaPixels));
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
        void Scroll(int deltaPixels)
        {
            // Nothing to do here, move along.
            if (deltaPixels == 0)
            {
                return;
            }

            int prevTextOffset = textOffset.TargetValue;

            // Apply scrolling and clamp to valid range before setting result.
            int targetOffset = prevTextOffset + deltaPixels;

            // Clamp to valid range.
            if (targetOffset < 0)
            {
                targetOffset = 0;
            }
            else
            {
                // Calc the max line we want to use as our start and make sure we haven't gone beyond there.
                // Also, don't let this go negative.
                // The -1 below just lets this display an extra blank line at the end.  This helps make
                // it more clear to the user that there is no more.
                int maxStartLine = Math.Max(0, blob.NumLines - (int)(numLinesInWindow - 1));
                targetOffset = Math.Min(targetOffset, (int)(maxStartLine * blob.TotalSpacing));
            }

            // After adjusting for clamping, if the textOffset didn't change, nothing to do here.
            if (targetOffset == prevTextOffset)
            {
                return;
            }

            // Apply to actual value.
            textOffset.Value = targetOffset;

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
            if (targetOffset >= rtOffset)
            {
                // Now check we haven't scrolled too far down.
                if (targetOffset - rtOffset + rect.Height <= textRT.Height)
                {
                    // All's good, no need to touch the textRT.
                    return;
                }
            }

            // Figure out what we want the new rtOffset to be.
            int desiredRTOffset = Math.Max(0, (int)(targetOffset - numBufferLines * blob.TotalSpacing));
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
                    blob.RenderText(null, new Vector2(margin, 0), theme.DarkTextColor, startLine: startLine, maxLines: numLinesInRT - usableLines, renderCursor: InFocus);
                }
                else
                {
                    Debug.Assert(false, "Should never get here");
                    // No usable region so just redraw all text.
                    int startLine = (int)(desiredRTOffset / (float)blob.TotalSpacing);
                    blob.RenderText(null, new Vector2(margin, 0), theme.DarkTextColor, startLine: startLine, maxLines: numLinesInRT, renderCursor: InFocus);
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
                    blob.RenderText(null, new Vector2(margin, usableSize), theme.DarkTextColor, startLine: startLine, maxLines: numLinesInRT - usableLines, renderCursor: InFocus);
                }
                else
                {
                    Debug.Assert(false, "Should never get here");
                    // No usable region so just redraw all text.
                    int startLine = (int)(desiredRTOffset / (float)blob.TotalSpacing);
                    blob.RenderText(null, new Vector2(margin, 0), theme.DarkTextColor, startLine: startLine, maxLines: numLinesInRT, renderCursor: InFocus);
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
                textOffset.Value = 0;
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

            // If textDirty is true, then the user has done some kind of input.  In which case
            // we may need to force some scrolling to keep the cursor on the screen.
            if (textDirty)
            {
                // Figure out where the cursor is.
                int cursorLine = 0;
                int posX = 0;
                blob.FindCursorLineAndPosition(out cursorLine, out posX);

                // Is cursor off the top edge?  If so, scroll to see it.
                int topOfCursorLine = (int)(cursorLine * blob.TotalSpacing);   // Note, cursorLine is 0 based.
                if (topOfCursorLine < textOffset.TargetValue)
                {
                    Scroll(topOfCursorLine - textOffset.TargetValue);
                }

                // Is cursor off the bottom edge?
                int bottomEdge = textOffset.TargetValue + (int)(numLinesInWindow * blob.TotalSpacing);
                int bottomOfCursorLine = (int)((cursorLine + 1) * blob.TotalSpacing);   // Note, cursorLine is 0 based.
                if (bottomOfCursorLine > bottomEdge)
                {
                    Scroll(bottomOfCursorLine - bottomEdge);
                }
            }

            if (refreshNeeded)
            {
                GraphicsDevice device = KoiLibrary.GraphicsDevice;

                device.SetRenderTarget(textRT);
                device.Clear(Color.Transparent);

                Vector2 pos = new Vector2(margin, -rtOffset);
                pos = pos.Truncate();

                // Update blobCamera.
                Vector2 rtSize = new Vector2(textRT.Width, textRT.Height);
                blobCamera.Position = rtSize / 2.0f;
                blobCamera.Update(rtSize);

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
                    blob.RenderText(blobCamera, pos + new Vector2(0, curLine * blob.TotalSpacing), textColor.Value, startLine: curLine, maxLines: numLinesAtATime, renderCursor: InFocus);
                    curLine += numLinesAtATime;
                }
                //blob.RenderText(null, pos, themeSet.DarkTextColor, startLine: 0, maxLines: numLinesInRT);

                device.SetRenderTarget(null);
            }

        }   // end of RefreshRT()


        #endregion


    }   // end of class MultiLineTextEditBox
}   // end of namespace KoiX.UI
