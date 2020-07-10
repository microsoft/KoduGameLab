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
using Microsoft.Xna.Framework.Input;

using KoiX;
using KoiX.Geometry;
using KoiX.Input;
using KoiX.Managers;
using KoiX.Text;
using KoiX.UI;

using Boku;
using Boku.Common;

namespace KoiX.UI.Dialogs
{
    /// <summary>
    /// Modal dialog for displaying a blobs of text.  The size of the 
    /// dialog is scalable depending on the text it is given.  It will
    /// grow to fit the text.  If there is too much text it will display
    /// it as scrollable.
    /// 
    /// For the text display we want the rendertarget that the text is 
    /// written to to be 1:1 scaled with the screen to prevent any 
    /// sampling issues.  So, even though this gets a camera with some
    /// zoom value we convert everything to screen coords for rendering.
    /// 
    /// TODO (****) This would probably be better if we made the 
    /// scrollable text part of this a first class widget rather 
    /// than making it part of the dialog.  Maybe we should be using
    /// ScrollableTextBox here?  The issues is that this dialog scales
    /// depending on the text.  ScrollableTextBox assumes a fixed size.
    /// It could be made to work but just not interested today.
    /// 
    /// </summary>
    public class TextDialog : BaseDialog
    {
        const int numBufferLines = 6;   // Number of extra lines of text we allow for above and below
                                        // the current window.

        #region Members

        RenderTarget2D textRT;      // Contains the text being displayed.  Should be 1:1 with screen resolution.
        RenderTarget2D tmpRT;       // Used when scrolling to copy common chunks of text.

        bool draggingText = false;
        int numLinesInRT;           // How many lines of text can the RT hold?
        float numLinesInWindow;     // How many lines can the window display.  May be fractional.

        float textOffset;           // Amount text has been scrolled, in pixels.  This is measured from top of text blob.
        float rtOffset;             // This is the offset from the start of the blob to the first line in the RT.  This
                                    // will always be a multiple of blob.TotalSpacing

        string titleId;             // Id fed into localization.  If null, assumes that string has been set explicitly.
        string bodyId;              // Id for text in main body of display.

        TextBlob blob;
        SystemFont blobFont;
        bool doNotLocalize = false; // If true, textId is used literally, rather than as a key for localization.

        bool textDirty = true;      // Need a recalc and refresh of the RT?  

        Label title;
        string titleText;
        SystemFont titleFont;

        Button okButton;

        SpriteCamera camera;        // Just a local ref we can hang on to.
        float scaleFactor = 1.0f;   // Based on the above camera's zoom.
        SpriteCamera pixelCamera;   // Camera in pixel space.

        // Overall size of dialog.
        Vector2 size;
        
        // Rect for text region in dialog coords which uses camera scaling.
        RectangleF textRect;

        // Left/right margin for text.
        float margin = 16.0f;
        float knobRadius = 32;

        #endregion

        #region Accessors

        public bool DoNotLocalize
        {
            get { return doNotLocalize; }
            set { doNotLocalize = value; }
        }

        /// <summary>
        /// Id for the title string.
        /// </summary>
        public string TitleId
        {
            get { return title.LabelId; }
            set
            {
                title.LabelId = value;
            }
        }

        /// <summary>
        /// Text for the title string.  If explicitly set
        /// this is not localized.
        /// </summary>
        public string TitleText
        {
            get { return title.LabelText; }
            set
            {
                title.LabelText = value;
            }
        }

        /// <summary>
        /// Id for string to be displayed in the body of the dialog.
        /// </summary>
        public string BodyId
        {
            get { return bodyId; }
            set
            {
                if (bodyId != value)
                {
                    bodyId = value;
                    BodyText = Strings.Localize(bodyId);
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
                    bodyId = null;
                    Dirty = true;
                    textDirty = true;
                    Recalc();
                }
            }
        }

        #endregion

        #region Public

        /// <summary>
        /// Standard dialog for displaying a blob of text.  The dialog will size
        /// itself to match the text it's displaying.  If the text is too long
        /// it will be scrollable.
        /// </summary>
        /// <param name="_">Dummy placeholder which forces all the rest of the params to be named.</param>
        /// <param name="titleId">id for title string, will be passed to Localization.</param>
        /// <param name="titleText">String used for title.</param>
        /// <param name="bodyId">id for body string, will be passed to Localization.</param>
        /// <param name="bodyText">String displayed in body of dialog.</param>
        public TextDialog(
#if DEBUG
            // This forces us to use named arguments for _all_ arguements.
            int _ = 0, 
#endif
                            string titleId = null, string titleText = null, string bodyId = null, string bodyText = null)
            : base()
        {
#if DEBUG
            _name = "TextDialog";
#endif

            Debug.Assert(titleId == null || titleText == null, "Only one should be set");
            Debug.Assert(bodyId == null || bodyText == null, "Only one should be set");

            hitTestable = true;     // We need this since the text is local and needs to be dragged.  We could probably
                                    // remove it if the text became ScrollableTextBox.

            this.titleId = titleId;
            this.titleText = titleText;
            this.bodyId = bodyId;

            theme = Theme.CurrentThemeSet;

            blobFont = SysFont.GetSystemFont(theme.TextFontFamily, theme.TextBaseFontSize, System.Drawing.FontStyle.Regular);
            blob = new TextBlob(BlobFont, bodyText, (int)(textRect.Width - 2 * margin));

            // Get localized versions of string if we have Ids.
            if (bodyId != null)
            {
                blob.RawText = Strings.Localize(bodyId);
            }

            // Set default values which will be overwritten in Recalc().
            size = new Vector2(1200, 800);
            rect = new RectangleF(-size / 2.0f, size);

            Vector2 textBoxPosition = new Vector2(16, 80);
            Vector2 textBoxSize = new Vector2(size.X - 2 * 16, size.Y - 2 * 76);
            textRect = new RectangleF(textBoxPosition, textBoxSize);

            titleFont = SysFont.GetSystemFont(theme.TextFontFamily, 1.5f * theme.TextBaseFontSize, System.Drawing.FontStyle.Bold);
            title = new Label(this, titleFont, theme.LightTextColor, outlineColor: theme.DarkTextColor, outlineWidth: 1.2f, labelId: titleId, labelText: titleText);
            title.Size = title.CalcMinSize();
            title.Size = new Vector2(textBoxSize.X, title.Size.Y);  // Expand rect for title to cover full width of dialog.
            title.Position = new Vector2(32, 8);
            title.HorizontalJustification = Justification.Left;
            AddWidget(title);

            // OkButton and widgetSet to keep it in lower right hand corner.
            WidgetSet buttonSet = new WidgetSet(this, RectangleF.EmptyRect, Orientation.Vertical, horizontalJustification: Justification.Right, verticalJustification: Justification.Bottom);
            buttonSet.FitToParentDialog = true;
            AddWidget(buttonSet);
            okButton = new Button(this, RectangleF.EmptyRect, labelId: "textDialog.ok", OnChange: OnOk, element: GamePadInput.Element.AButton);
            okButton.Margin = new Padding(32, 16, 32, 16);
            okButton.Size = okButton.CalcMinSize() + new Vector2(margin, 0);  // Match button size to label, with a bit of margin.
            okButton.Label.Size = okButton.Size;                              // Make label same size so it gets centered correctly.
            buttonSet.AddWidget(okButton);

            Dirty = true;
            textDirty = true;
            Recalc();

            // Connect navigation links.  The only focusable widget should
            // be the okButton so this really won't be doing much at all.
            CreateTabList();
            CreateDPadLinks();

            pixelCamera = new SpriteCamera();
            pixelCamera.Update();

        }   // end of c'tor

        public override void Recalc()
        {
            float zoom = camera == null ? 1.0f : camera.Zoom;

            // Based on the current rect size, calc sizes for everything else.
            if (/*Dirty*/ textDirty || scaleFactor != zoom)
            {
                if (scaleFactor != zoom)
                {
                    // Reset this.  Since the text has been resized and reflowed it's difficult
                    // to get the right value.  If we just leave the value as-is we
                    // can end up with out-of-range results.
                    textOffset = 0;
                }

                scaleFactor = zoom;

                // Constants...
                float dialogAspectRatio = 1.5f;
                int minLines = 5;
                int maxLines = 16;

                knobRadius = 32.0f * scaleFactor;
                margin = 16.0f * scaleFactor;

                blobFont = SysFont.GetSystemFont(theme.TextFontFamily, theme.TextBaseFontSize * scaleFactor, System.Drawing.FontStyle.Regular);
                blob.Font = BlobFont;

                // Figure out how many lines of text to display.  Valid range is 3..16.
                // Since we're trying to keep a fixed aspect ratio for the overall dialog
                // the number of lines vertically determines the width.
                // Start with the smallest possible and keep increasing until either all
                // the text fits or we've gone beyond maxLines;
                {
                    // If we've got a big blob of text, try full size first.  This will
                    // hopefully minimize the amount of thrashing we're doing on the 
                    // text blob.  1000 characters is purely arbitrary but seems about
                    // right for the cut-off where the full size dialog would be needed.
                    bool bigBlob = blob.RawText.Length > 1000;

                    int numLines = bigBlob ? maxLines : minLines;
                    while (true)
                    {
                        // Everything gets based off the height of the text box.
                        // Note that all these values are in pixel coords.  So when we
                        // set the dialog size we need to adjust for the zoom BUT the
                        // text display is done in these coordinates.
                        int textHeight = numLines * blob.TotalSpacing;
                        int dialogHeight = textHeight + (int)(2 * 76 * scaleFactor);    // Add space for title and button.
                        int dialogWidth = (int)(dialogHeight * dialogAspectRatio);
                        int textWidth = dialogWidth - (int)(2 * 16 * scaleFactor);      // Leave space for left/right margins.

                        // Calc _scaled_ width and set on blob.
                        // Leave a bit of a margin on either side so that the text isn't right on
                        // the edge.  This means that the blobWIdth will be smaller than textWidth.
                        // Realistically the 3 below should be a 2 since we are subtracting off the 
                        // same margin from either side.  For some reason, the occasional line of 
                        // text looks like it is going long.  So, by making this 3 we double the 
                        // margin on the right edge making things look better.
                        // TODO (****) Should we shift everything a bit to the right for R-to-L languages?
                        int blobWidth = (int)((textWidth - 3 * margin));
                        blob.Width = blobWidth;

                        if (blob.NumLines <= numLines || numLines >= maxLines)
                        {
                            // If we tried the bigBlob option and came out with fewer
                            // lines that we thought, restart at the small size.
                            if (bigBlob && blob.NumLines < numLines)
                            {
                                numLines = minLines - 1;
                                bigBlob = false;
                            }
                            else
                            {
                                // We've found the size we want.  Add an extra half line.  For text that
                                // fully fits, this adds a bit of margin at the bottom which looks more 
                                // finished.  For text that will end up scrolling, this makes sure that 
                                // the final line is cut off giving an indicator that the text needs to
                                // be scrolled to see the rest of it.
                                int halfLine = (int)(0.5f * blob.TotalSpacing);
                                textRect.SetSize(new Vector2(textWidth, textHeight + halfLine) / scaleFactor);

                                // Set overall size and position.
                                size = new Vector2(dialogWidth, dialogHeight + halfLine) / scaleFactor;
                                rect = new RectangleF(-size / 2.0f, size);

                                break;
                            }
                        }
                        ++numLines;
                    }
                }

                // textRect size is already set, so now set the position.
                Vector2 textPos = new Vector2(16, 80) * scaleFactor;
                textRect.SetPosition(textPos.Round());
            }

            base.Recalc();
        }   // end of Recalc()

        public override void Update(SpriteCamera camera)
        {
            this.camera = camera;

            // Set UI camera to standard position/zoom to match current resolution.
            BaseScene.SetCameraToTargetResolution(camera);
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

            base.Update(camera);

        }   // end of Update()

        public override void Render(SpriteCamera camera)
        {
            // This dialog is designed to be instantiated once and then shared so
            // only render when we are the CurrentFocusDialog.
            if (Active && DialogManagerX.CurrentFocusDialog == this)
            {
                // The base render call draws the underlying rectangle, title, and ok button.
                base.Render(camera);

                title.Render(camera, rect.Position);

                RectangleF renderRect = textRect;
                renderRect.Position = (renderRect.Position + rect.Position * scaleFactor).Round();

                // DEBUG HACK!!!   Hide local version...
                //Texture2D textRT = KoiLibrary.LoadTexture2D(@"Textures\Registration1024");

                // The size that the rt would be for a perfect fit.  It may be larger than this
                // since we don't constantly reallocate it.
                Point rtTargetSize = (textRect.Size * scaleFactor).RoundToPoint();

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
            }

            /*
            // Show textRT
            SpriteBatch batch = KoiLibrary.SpriteBatch;
            batch.Begin();
            {
                // Calc overlapping position.
                Vector2 pos = (rect.Position * scaleFactor + camera.ScreenSize / 2.0f);
                pos += new Vector2(16, 80) * scaleFactor; // offset for text box
                pos = pos.Round();

                // Just shrink and render in corner.
                Rectangle tmpRect = new Rectangle(0, 0, textRT.Width / 2, textRT.Height / 2);
                batch.Draw(textRT, tmpRect, Color.White);
            }
            batch.End();
            */

            /*
            // Ghost over textRect to show hit area.
            batch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, null, null, null, null, camera.ViewMatrix);
            {
                Rectangle localRect = new Rectangle();
                Vector2 pos = textRect.Position / scaleFactor + rect.Position;
                Vector2 size = textRect.Size;
                localRect.Location = pos.RoundToPoint();
                localRect.Width = (int)size.X;
                localRect.Height = (int)size.Y;
                batch.Draw(SharedX.WhiteTexture, localRect, Color.Green * 0.3f);
            }
            batch.End();
            */
        }   // end of Render()

        public void OnOk(BaseWidget w)
        {
            DialogManagerX.KillDialog(this);
        }   // end of OnOk()

        public override void Activate(params object[] args)
        {
            if (!Active)
            {
                // Force scrolling back to top.
                textOffset = 0;
                rtOffset = 0;
                Dirty = true;
                textDirty = true;
            }

            base.Activate(args);
        }   // end of Activate()

        public override void RegisterForInputEvents()
        {
            // Call base register first.  By putting the child widgets on the input stacks
            // first we can then put oursleves on and have priority.
            base.RegisterForInputEvents();

            KoiLibrary.InputEventManager.RegisterForEvent(this, InputEventManager.Event.Keyboard);
            KoiLibrary.InputEventManager.RegisterForEvent(this, InputEventManager.Event.MouseLeftDown);
            KoiLibrary.InputEventManager.RegisterForEvent(this, InputEventManager.Event.MouseWheel);
            KoiLibrary.InputEventManager.RegisterForEvent(this, InputEventManager.Event.OnePointDrag);

        }   // end of RegisterForInputEvents()

        public override void UnregisterForInputEvents()
        {
            base.UnregisterForInputEvents();
        }   // end of UnregisterForInputEvents()

        public override bool ProcessKeyboardEvent(KeyInput input)
        {
            Debug.Assert(Active);

            switch(input.Key)
            {
                case Keys.Up:
                    Scroll(-blob.TotalSpacing / 2.0f);
                    return true;
                case Keys.Down:
                    Scroll(blob.TotalSpacing / 2.0f);
                    return true;
            }

            return base.ProcessKeyboardEvent(input);
        }   // end of ProcessKeyboardEvent()

        public override bool ProcessMouseLeftDownEvent(MouseInput input)
        {
            Debug.Assert(Active);

            Vector2 hit = camera.ScreenToCamera(input.Position);

            // Text box.
            hit -= rect.Position;
            if (textRect.Contains(hit))
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
                Vector2 hit = pixelCamera.ScreenToCamera(gesture.StartPosition);
                hit -= rect.Position; 
                if (textRect.Contains(hit))
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
                float deltaPixels = -1000.0f * tics * pad.RightStick.Y;
                Scroll(deltaPixels);
                return true;
            }

            return base.ProcessGamePadEvent(pad);
        }

        #endregion

        #region Internal

        FontWrapper BlobFont()
        {
            FontWrapper wrapper = new FontWrapper(null, blobFont);
            return wrapper;
        }

        /// <summary>
        /// Scrolls the text.  If needed, will also refresh the RT.
        /// Clamps to valid range.
        /// TODO (****) Think about adding a twitch to this so that the scrolling is smooth.
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
                if (textOffset - rtOffset + textRect.Height <= textRT.Height)
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
            InGame.SetRenderTarget(tmpRT);
            InGame.Clear(Color.Transparent);

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
            
            InGame.SetRenderTarget(null);

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

            Point rtWindowSize = (textRect.Size * scaleFactor).RoundToPoint();

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
            if (DeviceResetX.NeedsLoad(textRT) || textRT.Width < rtWindowSize.X || textRT.Height < desiredRTHeight)
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

                InGame.SetRenderTarget(textRT);
                InGame.Clear(Color.Transparent);

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

                InGame.SetRenderTarget(null);
            }

        }   // end of RefreshRT()

        #endregion

    }   // end of class TextDialog

}   // end of namespace KoiX.UI.Dialogs
