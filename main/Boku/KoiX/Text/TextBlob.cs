// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

// Draws overlays on words and icons as rendered to help debug the layout.
//#define DEBUG_SPACING

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Storage;

using Boku;
using Boku.Base;
using Boku.Common;
using Boku.UI2D;
using Boku.Fx;
using Boku.Programming;

namespace KoiX.Text
{
    /// <summary>
    /// The TextBlob class wraps up a user entered text string along 
    /// with methods for navigating, editing and rendering it.
    /// </summary>
    public partial class TextBlob
    {
        /// <summary>
        /// A Word is either a contiguous section of text or a control input icon.
        /// </summary>
        protected class Word
        {
            // A "word" is either text or an icon.
            //  For text, str will be filled in and the icon and keyIcon values will be null.
            //  For normal icons, the text will be null and the icon will be filled in.
            //  For keys, icon==key and either text will contain the string to print on the key
            //      or keyIcon will contain the icon to render on top of the key face.
            public List<FatChar> text = new List<FatChar>();
            public string str;      // Processed string in display order.
                                    // May be the text of the word or text for the icon (if one exists).
            public TextHelper.ControlInputs icon = TextHelper.ControlInputs.none;
            public TextHelper.ControlInputs keyIcon = TextHelper.ControlInputs.none;

            public Vector2 offset = Vector2.Zero;   // Offset in pixels for rendering.
            public int width = 0;                   // Width in pixels of this word.
            public int cursor = 0;                  // Cursor position where this word starts in the raw string.
        }

        /// <summary>
        /// A Line represents a collection of words which are all rendered to the same line on screen.
        /// </summary>
        protected class Line
        {
            public List<FatChar> text = null;   // "String" originally set in logical order but converted to display order.
            public List<Word> words = null;     // Above string split apart into renderable segments.
                                                // Each segment is either a single fatChar representing
                                                // an icon or a string of text.
            public int cursor = 0;              // Cursor position of first word in line.

            public Line()
            {
                words = new List<Word>();
                text = new List<FatChar>();
            }
        }

        char koduIcon = '\uF8FF';           // Character used to indicate that this char
                                            // is a Kodu icon.  Defaults to neutral so 
                                            // should work well with bidi processing.
        #region Members

        GetFont GetFont = null;
        SystemFont keyFont;                 // This is the font used to render keycaps.  This will be the same as the
                                            // above font but with 75% scaling.

        int maxWidth = 0;                   // Max width (in pixels) of each line.  Used for flow/wrapping.

        // Both rawText and scrubbedText strings are in logical order.
        string rawText = null;              // The text string as the user has typed it.  We do all the editing on this version.
        string scrubbedText = null;         // This is the text post bad word filtering.  This is the version we display.
        string displayString = null;        // Display ordered string post bidi processing.

        int cursorPosition = 0;             // Position in string in logical ordering post-icon substitution. 
                                            // 0==before first character, 1==between 1st and 2nd character, etc.
                                            // Note that in mixed R and L text, at each transition the cursor position on the screen is 
                                            // ambiguous.  To determine the correct placement we need to keep track of the previous 
                                            // character the cursor went over.
        FatChar cursorChar = null;          // See above.

        List<FatChar> fatChars = new List<FatChar>();
        bool hasRtoL = false;

        List<Line> lines = null;

        TextHelper.Justification justification = TextHelper.Justification.Left;

        bool dirty = true;                  // Do we need to re-flow the text?

        int lineSpacingAdjustment = 0;      // Adds of subtracts from the font's line spacing.

        bool singleLineMode = false;

        bool programmingTileBackdrop = false;   // Should we render a backdrop/frame around programming tiles?

        #endregion

        #region Accessors

        /// <summary>
        /// The raw version of the text string as typed by the user.
        /// When set, the text is scrubbed for offensive words.
        /// </summary>
        public string RawText
        {
            set
            {
                string text = value != null ? TextHelper.FilterInvalidCharacters(value) : "";
                if (rawText != text)
                {
                    rawText = value != null ? TextHelper.FilterInvalidCharacters(value) : "";
                    if (!Censor.Scrub(rawText, ref scrubbedText))
                    {
                        scrubbedText = rawText;
                    }
                    dirty = true;
                    FlowText();
                    // Prevent the cursor position from being invalid.
                    cursorPosition = Math.Min(fatChars.Count, cursorPosition);
                    cursorPosition = Math.Max(0, cursorPosition);
                }
            }
            get { return rawText; }
        }

        /// <summary>
        /// The raw version of the text string as typed by the user.
        /// When set, the text is NOT scrubbed for offensive words.
        /// </summary>
        public string RawTextNoScrub
        {
            set
            {
                rawText = value != null ? TextHelper.FilterInvalidCharacters(value) : "";
                scrubbedText = rawText;
                dirty = true;
                FlowText();
                // Prevent the cursor position from being invalid.
                cursorPosition = Math.Min(fatChars.Count, cursorPosition);
                cursorPosition = Math.Max(0, cursorPosition);
            }
            get { return rawText; }
        }

        /// <summary>
        /// The scrubed version of the text string.
        /// </summary>
        public string ScrubbedText
        {
            //set { scrubbedText = value; }
            get { return scrubbedText; }
        }

        public int NumLines
        {
            get
            {
                FlowText(); // Does nothing if not dirty.
                return lines != null ? lines.Count : 0;
            }
        }

        public GetFont Font
        {
            set
            {
                if (GetFont != value)
                {
                    // We want to be a bit anal here about not triggering
                    // a reflow if we don't have to.
                    FontWrapper curFont = GetFont();
                    FontWrapper newFont = value();
                    if (curFont != newFont)
                    {
                        GetFont = value;

                        // Null keyFont.  It will be regnerated as needed in Render().
                        keyFont = null;

                        dirty = true;
                    }
                }
            }
            get { return GetFont; }
        }

        /// <summary>
        /// Size to render button glyph.
        /// </summary>
        public Vector2 ButtonSize
        {
            get { return new Vector2(Font().LineSpacing * 1.55f); }
        }

        /// <summary>
        /// Width to allow for button spacing.
        /// </summary>
        public int ButtonWidth
        {
            get { return (int)(ButtonSize.X * 40.0f / 64.0f); }
        }

        /// <summary>
        /// Forces single line mode.  Any character that would casue the line
        /// width to exceed the maz valid is ignored.
        /// </summary>
        public bool SingleLineMode
        {
            get { return singleLineMode; }
            set
            {
                if (singleLineMode != value)
                {
                    singleLineMode = value; 
                    dirty = true;
                }
            }
        }

        /// <summary>
        /// Add or subtracts from the font's default line spacing.
        /// </summary>
        public int LineSpacingAdjustment
        {
            get { return lineSpacingAdjustment; }
            set
            {
                if (lineSpacingAdjustment != value)
                {
                    lineSpacingAdjustment = value; 
                    dirty = true;
                }
            }
        }

        /// <summary>
        /// Total line spacing used.  The sum of the font's 
        /// line spacing plus the LineSpacingAdjustment value.
        /// </summary>
        public int TotalSpacing
        {
            get { return Font().LineSpacing + LineSpacingAdjustment; }
        }

        /// <summary>
        /// Max width for text formatting.  Set in c'tor.
        /// Clamped to match max width of SysFont rendering.
        /// </summary>
        public int Width
        {
            get { return maxWidth; }
            set 
            {
                int newWidth = 2048;
#if NETFX_CORE
                newWidth = (int)MathHelper.Min(value, 2048); 
#else
                newWidth = (int)MathHelper.Min(value, SysFont.MaxWidth); 
#endif
                if (newWidth != maxWidth)
                {
                    maxWidth = newWidth;
                    dirty = true;
                }
            }
        }

        /// <summary>
        /// Should we rendering programming tiles with a backdrop.  This is
        /// useful for rendering over dark backgrounds.
        /// </summary>
        public bool ProgrammingTileBackdrop
        {
            get { return programmingTileBackdrop; }
            set { programmingTileBackdrop = value; }
        }

        /// <summary>
        /// Set the justification for the blob of text.  This applies to
        /// the whole blob.  Currently no way to change individual parts.
        /// </summary>
        public TextHelper.Justification Justification
        {
            get { return justification; }
            set { justification = value; dirty = true; }
        }

        /// <summary>
        /// Returns true if there are any RtoL characters in the current text.
        /// </summary>
        public bool HasRtoL
        {
            get { return hasRtoL; }
        }

        #endregion

        #region Public

        // c'tor
        public TextBlob(GetFont Font, string rawText, int width)
        {
            this.GetFont = Font;
#if NETFX_CORE
            this.maxWidth = (int)MathHelper.Min(width, 2048);
#else
            this.maxWidth = (int)MathHelper.Min(width, SysFont.MaxWidth);
#endif

            this.rawText = rawText != null ? TextHelper.FilterInvalidCharacters(rawText) : "";
            if (!Censor.Scrub(this.rawText, ref scrubbedText))
            {
                scrubbedText = this.rawText;
            }

            lines = new List<Line>();
        }

        /// <summary>
        /// Returns the width in pixels of line i.  If i is out of
        /// range, returns 0.
        /// </summary>
        /// <param name="i"></param>
        /// <returns></returns>
        public int GetLineWidth(int i)
        {
            FlowText();

            int result = 0;

            if (i >= 0 && i < lines.Count)
            {
                Line line = lines[i];

                for (int l = 0; l < line.words.Count; l++)
                {
                    result += line.words[l].width;
                }
            }

            return result;
        }   // end of GetLineWidth() 

        //
        // Cursor functions.
        //

        public void CursorUp()
        {
            if (lines != null)
            {
                int line;
                int xPos;
                FindCursorLineAndPosition(out line, out xPos);
                if (line > 0)
                {
                    --line;
                    SetCursorPosition(line, xPos);
                }
            }
        }   // end of CursorUp()

        public void CursorDown()
        {
            if (lines != null)
            {
                int line;
                int xPos;
                FindCursorLineAndPosition(out line, out xPos);

                if (line == lines.Count || lines[line].words.Count<1)//fix bug 43718
                {
                    // The reason we are beyond the end of the lines array is that
                    // we must be at the end of the rawText string and it must end
                    // with a \n.  So, we're fine, don't do anything.
                }
                else
                {
                    bool curLineEndsInReturn = false;
                    Word lastWord = lines[line].words[lines[line].words.Count - 1];
                    if (lastWord != null && lastWord.text != null && lastWord.str.EndsWith("\n"))
                    {
                        curLineEndsInReturn = true;
                    }

                    if (line < lines.Count - 1 || curLineEndsInReturn)
                    {
                        ++line;
                        if (line >= lines.Count)
                        {
                            cursorPosition = RawText.Length;
                        }
                        else
                        {
                            SetCursorPosition(line, xPos);
                        }
                    }
                }
            }
        }   // end of CursorDown()

        public void CursorLeft()
        {
            if (cursorPosition > 0)
            {
                --cursorPosition;
                if (cursorPosition >= fatChars.Count)
                    cursorPosition = fatChars.Count-1;
                cursorChar = fatChars[cursorPosition];
            }
        }   // end of CursorLeft()

        public void CursorRight()
        {
            if (cursorPosition < fatChars.Count)
            {
                cursorChar = fatChars[cursorPosition];
                ++cursorPosition;
            }
        }   // end of CursorRight()

        //
        // Special characters.
        //

        public void Enter()
        {
            if (!singleLineMode)
            {
                int len = 0;
                int pos = GetRawCursorPosition(out len);
                RawText = RawText.Insert(pos, "\n");
                cursorChar = fatChars[cursorPosition];
                ++cursorPosition;
                dirty = true;
            }
        }   // end of Enter()

        public void Home()
        {
            // Position cursor at first character.
            FlowText();

            cursorPosition = 0;
            cursorChar = fatChars.Count > 0 ? fatChars[0] : null;
        }   // end of Home()

        public void End()
        {
            // Position cursor at last character.
            FlowText();

            cursorPosition = fatChars.Count;
            cursorChar = fatChars.Count > 0 ? fatChars[fatChars.Count - 1] : null;
        }   // end of End()

        /// <summary>
        /// Deletes the character just passed by the cursor.
        /// </summary>
        public void Backspace()
        {
            FlowText();

            // Don't do anything if at beginning of line.
            if (RawText.Length > 0 && cursorPosition > 0)
            {
                int len = 0;
                int pos = GetRawCursorPosition(out len);

                // Set cursorChar to the character being deleted.
                cursorChar = fatChars[cursorPosition - 1];

                // Adjust cursor position.
                // Note this must happen before next line otherwise at
                // end of line cursor will move twice since assignment
                // to RawText forces cursorPosition into valid range.
                if (len == 0)
                {
                    --cursorPosition;
                }
                else
                {
                    cursorPosition += len - 2;
                }

                RawText = RawText.Substring(0, pos - 1) + RawText.Substring(pos);

                dirty = true;
            }
        }   // end of Backspace()

        /// <summary>
        /// Deletes the character ahead the cursor.
        /// </summary>
        public void Delete()
        {
            FlowText();

            // Don't do anything if at end of line.
            if (RawText.Length > 0 && cursorPosition < fatChars.Count)
            {
                int len = 0;
                int pos = GetRawCursorPosition(out len);

                RawText = RawText.Substring(0, pos) + RawText.Substring(pos + 1);
                dirty = true;
            }
        }

        /// <summary>
        /// Gets the rawString index at the cursor position.
        /// </summary>
        /// <param name="iconLen">if the char in front of the cursor is an icon, this is it's length</param>
        /// <returns></returns>
        int GetRawCursorPosition(out int iconLen)
        {
            int index = 0;
            iconLen = 0;

            // Handle no-text or beginning of line cases.
            if (fatChars.Count == 0 || cursorPosition == 0)
            {
                index = 0;
                return index;
            }

            cursorPosition = (int)MathHelper.Min(cursorPosition, fatChars.Count - 1);

            // Get character in front of cursor.
            FatChar fc = fatChars[cursorPosition - 1];

            // Handle case if cursor is at end of line.
            if (fatChars.Count == cursorPosition)
            {
                index = rawText.Length;
                if (fc.c == koduIcon)
                {
                    iconLen = fc.label.Count + 2;
                }
                return index;
            }

            // For middle case we need to look at character in front
            // of the cursor.  If this is an icon then return a raw
            // cursor position just after the closing '>' or ']'.
            if (fc.c == koduIcon)
            {
                index = fc.rawIndex + fc.label.Count + 2;
                iconLen = fc.label.Count + 2;
            }
            else
            {
                // Normal case.
                index = fc.rawIndex + 1;
            }

            return index;
        }   // end of GetRawCursorPosition()

        //
        // Normal characters.
        //

        /// <summary>
        /// Inserts a string into the blob at the current cursor location.
        /// Fine for use with single characters.
        /// 
        /// May fail in SinglLineMode if line width is exceeded.
        /// </summary>
        /// <param name="str"></param>
        /// <returns>True on success.  False on failure.</returns>
        public bool InsertString(string str)
        {
            bool result = true; // Default to success.

            int len = 0;
            int pos = GetRawCursorPosition(out len);
            int prevLen = fatChars.Count;
            RawText = RawText.Insert(pos, str);
            int postLen = fatChars.Count;

            //cursorPosition += str.Length;

            int delta = postLen - prevLen;
            if (str == "[" || str == "<")
            {
                // Special case where adding a single character makes the fatChars array
                // shorter but only moves the cursor a single character.
                ++cursorPosition;
            }
            else if(delta > 0)
            {
                cursorPosition += delta;
            }

            /*
            // TODO This is probably redundant since assigning to RawText also causes a re-flow.
            dirty = true;
            FlowText();

            // Handle case where this insertion caused an icon to be
            // completed.
            if (str == "]" || str == ">")
            {
                int index = -1;
                for (int i = 0; i < fatChars.Count; i++)
                {
                    if (fatChars[i].rawIndex > pos)
                    {
                        index = i;
                        break;
                    }
                }
                if (index != -1)
                {
                    cursorPosition = index;
                }
            }

            // If this insertion caused a ligature to be completed then
            // we also need to move the cursor
            // TODO Figure out how to detect this case.  Note that we can't
            // move the cursor except if THIS character caused the shortening.
            */

            if( fatChars.Count > 0 &&
                cursorPosition <= fatChars.Count )
            {
                cursorChar = fatChars[cursorPosition - 1];
            }
            else
            {
                cursorChar = null;
            }

            // In SingleLineMode, if this causes the text to wrap, we don't allow it.
            // So back out the change.
            if (singleLineMode)
            {
                FlowText();
                if (NumLines > 1)
                {
                    for (int i = 0; i < str.Length; i++)
                    {
                        Backspace();
                    }
                    result = false;
                }
            }

            dirty = true;

            return result;
        }   // end of InsertString()


        public void AddEllipsisToLine(int index)
        {
            if (!hasRtoL)
            {
                FlowText();

                if (lines.Count > index)
                {
                    Line line = lines[index];
                    Word ellipsis = new Word();

                    // Create FatChar string.
                    // ellipsis.str = @"...";
                    for (int i = 0; i < 3; i++)
                    {
                        FatChar fc = new FatChar('.');
                        fc.rawIndex = i;
                        // The logicalOrderIndex may change due to icon substitution.
                        fc.logicalOrderIndex = i;
                        // The displayOrderIndex will also change based on bidi transformations.
                        fc.displayOrderIndex = i;

                        ellipsis.text.Add(fc);
                    }
                    ellipsis.str = FatCharListToString(ellipsis.text);
                    ellipsis.width = (int)Font().MeasureString(ellipsis.str).X;

                    int lastWord = line.words.Count - 1;

                    // default the ellipsis offset to the last words in case this is the only word on the line
                    ellipsis.offset = line.words[lastWord].offset;

                    // Remove last word to make space for ellipsis.
                    line.words.RemoveAt(lastWord);

                    // If the last line only has 1 word then we don't set the offset again
                    if (lastWord > 0)
                    {
                        ellipsis.offset = line.words[lastWord - 1].offset;
                        ellipsis.offset.X += line.words[lastWord - 1].width;
                    }
                    line.words.Add(ellipsis);
                }
            }
        }   // end of AddEllipsisToLine()


        //
        // Render functions.
        //

        /// <summary>
        /// Render
        /// </summary>
        /// <param name="camera">Camera for text transformation.  If null this gives 0, 0 in the upper left hand corner and no zoom.</param>
        /// <param name="position"></param>
        /// <param name="color"></param>
        /// <param name="shadowColor"></param>
        /// <param name="shadowOffset"></param>
        /// <param name="outlineWidth">Width, in pixels, of text outline.</param>
        /// <param name="outlineColor">Color of text outline.</param>
        /// <param name="renderCursor">Should the cursor be drawn?</param>
        /// <param name="maxLines">The max number of lines to render.</param>
        /// <param name="startLine">The line number to start rendering with.</param>
        public void RenderText(SpriteCamera camera, Vector2 position, Color color, Color shadowColor = default(Color), Vector2 shadowOffset = default(Vector2), Color outlineColor = default(Color), float outlineWidth = 0, bool renderCursor = false, int startLine = 0, int maxLines = int.MaxValue)
        {
            FlowText();

            bool drawShadow = shadowOffset != Vector2.Zero;
            bool drawOutline = outlineWidth > 0;
            ScreenSpaceQuad ssquad = ScreenSpaceQuad.GetInstance();
            SpriteBatch batch = KoiLibrary.SpriteBatch;

            // This is a bit of a hack.  This issue is that we now create
            // the cardspace tile on demand.  But, that causes a batch
            // conflict if we're trying to render a tile while rendering
            // text.  So, try to pre-load all the tiles.  This should cause
            // them to be created before we start the batch.
            for (int i = 0; i < Math.Min(lines.Count, maxLines); i++)
            {
                for (int w = 0; w < lines[i].words.Count; w++)
                {
                    Word word = lines[i].words[w];
                    if (word.icon == TextHelper.ControlInputs.programmingTile)
                    {
                        Texture2D texture = CardSpace.Cards.CardFaceTexture(word.str + "_button");
                    }
                }
            }

#if NETFX_CORE
            // Scan through the text and pre-load any CardSpace textures which are normally lazy-created.
            // We have to do it here otherwise the begin/end pair from this code conflicts with the
            // begin/end pair used to render the texture.
            for (int i = 0; i < Math.Min(lines.Count, maxLines); i++)
            {
                for (int w = 0; w < lines[i].words.Count; w++)
                {
                    Word word = lines[i].words[w];
                    if (word.icon == TextHelper.ControlInputs.programmingTile)
                    {
                        Texture2D texture = CardSpace.Cards.CardFaceTexture(word.str + "_button");
                    }
                }
            }
#endif

            if(BokuSettings.Settings.UseSystemFontRendering)
            {
#if !NETFX_CORE
                SysFont.StartBatch(camera);
#endif
            }
            else
            {
                batch.Begin();
            }

            // Yes, the end condition looks a bit funky.  Math.Max(startLine + maxLines, maxLines) looks
            // like it should always return startLine + maxLines but when maxLines is MaxInt, the sum 
            // wraps, we get a negative result and nothing is rendered.
            for (int i = startLine; i < Math.Min(lines.Count, Math.Max(startLine + maxLines, maxLines)); i++)
            {
                int lineWidth = 0;
                for (int w = 0; w < lines[i].words.Count; w++)
                {
                    lineWidth += lines[i].words[w].width;
                }

                int justificationOffset = 0;
                if (justification == TextHelper.Justification.Center)
                {
                    justificationOffset = (maxWidth - lineWidth) / 2;
                }
                else if (justification == TextHelper.Justification.Right)
                {
                    justificationOffset = maxWidth - lineWidth;
                }

                for (int w = 0; w < lines[i].words.Count; w++)
                {
                    Word word = lines[i].words[w];
                    Vector2 pos = position + word.offset;
                    pos.X += justificationOffset;

                    // Adjust pos for startLine.
                    pos.Y -= startLine * TotalSpacing;

                    // Text word.
                    if (word.icon == TextHelper.ControlInputs.none && word.str != null)
                    {
                        if(BokuSettings.Settings.UseSystemFontRendering)
                        {
#if !NETFX_CORE
                            SystemFont font = GetFont().systemFont;
                            RectangleF clipRect = new RectangleF(pos.X, pos.Y, lineWidth + 0.0f * outlineWidth, font.LineSpacing + 2.0f * outlineWidth);
                            if (drawOutline)
                            {
                                SysFont.DrawString(word.str, pos, clipRect, font, color, scaling: Vector2.One, outlineColor: outlineColor, outlineWidth: outlineWidth);
                            }
                            else
                            {
                                if (drawShadow)
                                {
                                    SysFont.DrawString(word.str, pos + shadowOffset, clipRect, font, shadowColor, scaling: Vector2.One);
                                }
                                SysFont.DrawString(word.str, pos, clipRect, font, color, scaling: Vector2.One);
                            }
#if DEBUG_SPACING
                            ssquad.Render(new Vector4(0, 1, 0, 0.5f), pos, new Vector2(word.width, Font().LineSpacing));
#endif
#endif
                        }
                        else
                        {
                            if (drawShadow)
                            {
                                batch.DrawString(GetFont().spriteFont, word.str, pos + shadowOffset, shadowColor);
                            }
                            batch.DrawString(GetFont().spriteFont, word.str, pos, color);
                        }

                    }

                    // Standard button icon.
                    if (word.icon != TextHelper.ControlInputs.none
                        && word.icon != TextHelper.ControlInputs.key
                        && word.icon != TextHelper.ControlInputs.programmingTile)
                    {
                        Texture2D texture = ButtonTexture(word.icon);
                        Matrix mat = camera == null ? Matrix.Identity : camera.ViewMatrix;
                        batch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, samplerState: null, depthStencilState: null, rasterizerState: null, effect: null, transformMatrix: mat);
                        batch.Draw(texture, new Rectangle((int)pos.X, (int)pos.Y, (int)ButtonSize.X, (int)ButtonSize.Y), Color.White);
                        batch.End();

                        //ssquad.Render(texture, pos, ButtonSize, "TexturedRegularAlpha");
#if DEBUG_SPACING
                        ssquad.Render(new Vector4(1, 0, 0, 0.5f), pos, ButtonSize);
#endif
                    }

                    // Programming tile icon.
                    if (word.icon == TextHelper.ControlInputs.programmingTile)
                    {
                        Texture2D texture = CardSpace.Cards.CardFaceTexture(word.str + "_button");
                        // The 40/64 scale is to shrink the images to better match the 
                        // buttons which are 40*40 images in the upper left hand corner of a 64x64 texture.
                        // The 'foo' amount enlarges the tile to be larger than a button to make them a bit more readable.
                        Vector2 foo = new Vector2(3.0f, 3.0f);
                        if (ProgrammingTileBackdrop)
                        {
                            ssquad.Render(new Vector4(0.8f, 0.9f, 0.9f, 1), pos - foo, ButtonSize * 40.0f / 64.0f + 2.0f * foo);
                        }
                        Debug.Assert(texture != null, "Why do we have a null texture here?");
                        if (texture != null)
                        {
                            ssquad.Render(texture, pos - foo, ButtonSize * 40.0f / 64.0f + 2.0f * foo, "TexturedPreMultAlpha");
                        }
                    }

                    // Keyboard Icon
                    if (word.icon == TextHelper.ControlInputs.key)
                    {
                        Texture2D texture = ButtonTexture(word.icon);

                        Matrix mat = camera == null ? Matrix.Identity : camera.ViewMatrix;
                        batch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, samplerState: null, depthStencilState: null, rasterizerState: null, effect: null, transformMatrix: mat);
                        batch.Draw(texture, new Rectangle((int)pos.X, (int)pos.Y, (int)word.width, (int)ButtonSize.Y), Color.White);
                        batch.End();

                        // Text label?
                        if (word.keyIcon == TextHelper.ControlInputs.none)
                        {
                            // This code assumes that we are using SystemFont rendering, not SpriteFont.
                            // No longer works with SpriteFont.
                            Debug.Assert(keyFont != null);

                            if (BokuSettings.Settings.UseSystemFontRendering)
                            {
                                int indent = (word.width - (int)keyFont.MeasureString(word.str).X) / 2;
                                RectangleF clipRect = new RectangleF(pos.X, pos.Y, maxWidth, keyFont.LineSpacing);
                                SysFont.DrawString(word.str, pos + new Vector2(indent, 0), clipRect, keyFont, new Color(40, 40, 40));
                            }
                            else
                            {
                                Debug.Assert(false, "KeyCap rendering no longer supported by TextBlob when using SpriteFonts.");
                                //int indent = (word.width - (int)KeyFont().MeasureString(word.str).X) / 2;
                                //batch.DrawString(KeyFont().spriteFont, word.str, pos + new Vector2(indent, 0), new Color(40, 40, 40));
                            }
                        }
                        else
                        {
                            // Keycap with icon.
                            texture = ButtonTexture(word.keyIcon);
                            ssquad.Render(texture, pos, new Vector2(word.width * 1.6f, ButtonSize.Y), "TexturedRegularAlpha");
                        }
                    }
                }
            }

            if (BokuSettings.Settings.UseSystemFontRendering)
            {
#if !NETFX_CORE
                SysFont.EndBatch();
#endif
            }
            else
            {
                batch.End();
            }

            //
            // Cursor
            //
            if (renderCursor)
            {
                Vector2 pos = position;

                int line = 0;
                int xPos = 0;

                FindCursorLineAndPosition(out line, out xPos);
                if (BokuSettings.Settings.UseSystemFontRendering)
                {
                    // HACK This hack is due to the way system font rendering works.
                    // When DrawString is called it pads some space on either end of 
                    // the string.  The result is that things don't line up in the 
                    // same way they did for SpriteFont.  So we look where the cursor 
                    // is and move it a bit to the left.

                    // At beginning of line.
                    if (cursorPosition == 0)
                    {
                        xPos += (int)(Font().systemFont.Padding);
                    }
                }

                pos.X = position.X + xPos;
                pos.Y += line * TotalSpacing;

                float cursorHeight = TotalSpacing + 4.0f;
                Vector2 cursorTop = new Vector2(pos.X, pos.Y);
                Vector2 cursorBottom = cursorTop;
                cursorBottom.Y += cursorHeight;

                // Render the cursor.
                KoiX.Geometry.Line.DrawLine(camera, cursorTop, cursorBottom, color, color, 2.0f, 2.0f);

            }   // end if renderCursor.

            
            /*
            // Debug code.
            if (renderCursor)
            {
                BokuGame.DebugString = "cursorPosition : " + cursorPosition.ToString();
                if (cursorChar != null)
                {
                    BokuGame.DebugString += "  cursorChar : '" + cursorChar.c + "' " + cursorChar.type;
                }
            }
            */

        }   // end of RenderText()

        #endregion

        #region Internal

        /// <summary>
        /// Checks if the cursor is at the beginning of a valid alias.
        /// </summary>
        /// <param name="text">The text string to check.</param>
        /// <param name="cursorPos">The cursor position with the string.</param>
        /// <param name="end">If function returns true, this is the position
        /// of the character just past the '>' of the alias.</param>
        /// <returns>ControlInputs.none if there's no alias, or the input enum if there is.</returns>
        TextHelper.ControlInputs AliasStartingAtCursor(string text, int cursorPos, out int end)
        {
            end = -1;
            TextHelper.ControlInputs result = TextHelper.ControlInputs.none;

            if (text[cursorPos] == '<')
            {
                end = text.IndexOf('>', cursorPos);
                if (end != -1)
                {
                    ++end;  // Include final '>'
                    string str = text.Substring(cursorPos, end - cursorPos);

                    result = TextHelper.MatchControlAlias(str);

                    if (result > TextHelper.ControlInputs.nonButton)
                    {
                        result = TextHelper.ControlInputs.none;
                    }
                    else if (result == TextHelper.ControlInputs.none)
                    {
                        Texture2D tile = CardSpace.Cards.CardFaceTexture(str.Substring(1, str.Length - 2));
                        if (tile != null)
                        {
                            result = TextHelper.ControlInputs.programmingTile;
                        }
                        else
                        {
                            // Hmm, what to do...
                        }
                    }

                }
            }
            else if (text[cursorPos] == '[')
            {
                end = text.IndexOf(']', cursorPos);
                // If we find a matching ']' and there's no \n in between...
                int newline = text.IndexOf('\n', cursorPos);
                if (end != -1 && (newline == -1 || newline > end))
                {
                    ++end;  // Include final ']'
                    result = TextHelper.ControlInputs.key;
                }
            }

            return result;
        }   // end of AliasStartingAtCursor()

        /// <summary>
        /// Checks if the cursor is at the end of a valid alias, ie if the 
        /// cursor is at position N then '>' for the alias would have to 
        /// be the (N+1)th character in the raw text.
        /// </summary>
        /// <param name="text">The text string to check.</param>
        /// <param name="cursorPos">The cursor position with the string.</param>
        /// <param name="start">If function returns true, this is the 
        /// starting character of the alias.</param>
        /// <returns>ControlInputs.none if there's no alias, or the input enum if there is.</returns>
        TextHelper.ControlInputs AliasEndingAtCursor(string text, int cursorPos, out int start)
        {
            start = -1;
            TextHelper.ControlInputs result = TextHelper.ControlInputs.none;

            if (text[cursorPos - 1] == '>')
            {
                // Create a tmp string cut off at the '>'
                string str = text.Substring(0, cursorPos);
                start = str.LastIndexOf('<');
                if (start != -1)
                {
                    str = str.Substring(start);

                    result = TextHelper.MatchControlAlias(str);
                }
            }
            else if (text[cursorPos - 1] == ']')
            {
                // Create a tmp string cut off at the ']'
                string str = text.Substring(0, cursorPos);
                start = str.LastIndexOf('[');
                // Make sure to ignore any strings with newlines.
                if (start != -1)
                {
                    int newLine = str.IndexOf('\n', start);
                    if (start != -1 && newLine == -1)
                    {
                        result = TextHelper.ControlInputs.key;
                    }
                }
            }

            return result;
        }   // end of AliasEndingAtCursor()

        /// <summary>
        /// Top level function which controls the overall re-flowing of the text.
        /// </summary>
        void FlowText()
        {
            Debug.Assert(maxWidth > 0, "Pretty sure this will mess up otherwise.");

            if (dirty)
            {
                // Fill in FatChar "string".  While doing 
                // this check for any RtoL chars.
                CreateFatString();

                // Process icon substitution.  This will replace icon 
                // text ala <apple> with a single, neutral character.
                IconSubstitution();

                // If any RtoL
                //      Do first part of bidi processing
                if (hasRtoL)
                {
                    BiDi.LogicalToVisualPass1(fatChars);

                    // TODO Do we need to run bidi processing on text on keycaps?
                }

                // Convert any ligatures.  Do this before reversal of string.
                ApplyLigatures(fatChars);

                // Apply contextual forms.
                if (hasRtoL)
                {
                    ApplyContextualForms(fatChars);
                }

                // Break into lines.
                List<Word> words = SplitIntoWords(fatChars);
                lines = LayoutWords(words);

                // If any RtoL
                //      Do substring reversal.
                //      Do mirroring.
                // Note this must work on the individual lines.
                if (hasRtoL)
                {
                    foreach (Line line in lines)
                    {
                        BiDi.LogicalToVisualPass2(line.text);
                    }

                    // TODO Reverse RotL string in keycaps.

                }

                // Convert lines into words for rendering.
                // Split fatChars text into an array of words.
                foreach (Line line in lines)
                {
                    // TODO (****) Why are we splitting into words again?  Is this only needed for RtoL?
                    line.words = SplitIntoWords(line.text);
                }

                // For SystemFont rendering we want to take adjacent text words and merge them.
                // This make sthe spacing work out correctly and also minimizes the number of
                // text draw calls we make.
                // TODO (****) Should move the merging into LayoutWords so that we end up with
                // smaller gaps at the ends of lines when wrapping.  Note that my initial attempt
                // to do this utterly failed since BiDi.LogicalToVisualPass2() needs to change
                // the order of the FatChars list.
                if(BokuSettings.Settings.UseSystemFontRendering)
                {
                    // Merge adjacent words to simplify rendering.
                    foreach (Line line in lines)
                    {
                        for (int i = 0; i < line.words.Count - 1; )
                        {
                            // Try and merge work i and i+1
                            // In order to do this, both words must not be icons.
                            if (line.words[i].icon == TextHelper.ControlInputs.none && line.words[i].keyIcon == TextHelper.ControlInputs.none
                                && line.words[i + 1].icon == TextHelper.ControlInputs.none && line.words[i + 1].keyIcon == TextHelper.ControlInputs.none)
                            {
                                // Merger word i+1 into word i.
                                line.words[i].str += line.words[i + 1].str;
                                // Ignore width here.  We'll recalc width below after all the merging is handled.

                                foreach (FatChar fc in line.words[i + 1].text)
                                {
                                    line.words[i].text.Add(fc);
                                }

                                line.words.RemoveAt(i + 1);
                            }
                            else
                            {
                                ++i;
                            }
                        }
                        // Reclac widths since merging words will make them narrower due to spacing issues.
                        for (int i = 0; i < line.words.Count; i++)
                        {
                            if (line.words[i].icon == TextHelper.ControlInputs.none && line.words[i].keyIcon == TextHelper.ControlInputs.none)
                            {
                                line.words[i].width = (int)GetFont().MeasureString(line.words[i].str).X;
                            }
                        }
                    }
                }

                CalcWordOffsets(lines);

                if (!BokuSettings.Settings.UseSystemFontRendering)
                {
                    // When using systemn font rendering this is done in BiDi.LogicalToVisualPass2().
                    // TODO Should we also move this there for SpriteFont rendering?  Does it matter?
                    // Set displayOrderIdices for chars.
                    int idx = 0;
                    foreach (Line line in lines)
                    {
                        foreach (FatChar fc in line.text)
                        {
                            fc.displayOrderIndex = idx;
                            ++idx;
                        }
                    }
                }
                //Debug.Print("===");

                // Create display ordered string.
                if (hasRtoL)
                {
                    displayString = string.Empty;
                    for (int l = 0; l < lines.Count; l++)
                    {
                        for (int i = 0; i < lines[l].words.Count; i++)
                        {
                            for (int j = 0; j < lines[l].words[i].text.Count; j++)
                            {
                                displayString += lines[l].words[i].text[j].c;
                                //Debug.Print(lines[l].words[i].text[j].c + " l:" + lines[l].words[i].text[j].logicalOrderIndex.ToString() + " d:" + lines[l].words[i].text[j].displayOrderIndex.ToString());
                            }
                        }
                    }
                }
                else
                {
                    displayString = scrubbedText;
                }

                dirty = false;
            }
        }   // end of FlowText()

        /// <summary>
        /// Creates the List of FatChars based on the scrubbed text string.
        /// </summary>
        void CreateFatString()
        {
            if (fatChars == null)
            {
                fatChars = new List<FatChar>();
            }
            else
            {
                fatChars.Clear();
            }

            hasRtoL = false;

            for (int i = 0; i < scrubbedText.Length; i++)
            {
                FatChar fc = new FatChar(scrubbedText[i]);
                fc.rawIndex = i;
                // The logicalOrderIndex may change due to icon substitution.
                fc.logicalOrderIndex = i;
                // The displayOrderIndex will also change based on bidi transformations.
                fc.displayOrderIndex = i;

                fatChars.Add(fc);

                if (fc.type == "R" || fc.type == "RLE" || fc.type == "RLO" || fc.type == "AL")
                {
                    hasRtoL = true;
                }
            }

        }   // end of CreateFatString()

        /// <summary>
        /// Find and replace any icon substutution strings with single neutral characters.
        /// </summary>
        void IconSubstitution()
        {
            // Do this as two passes to keep keycaps and icons seperate.

            // Keycaps first.
            int start = 0;
            int end = 0;
            while (start < fatChars.Count - 1)
            {
                if (fatChars[start].c == '[')
                {
                    // Try and find the matching end.
                    end = start + 1;
                    while (end < fatChars.Count)
                    {
                        // Don't allow keycaps with embedded newlines.
                        if (fatChars[end].c == '\n')
                            break;

                        if (fatChars[end].c == ']')
                        {
                            // Save the current rawIndex .
                            int rawIndex = fatChars[start].rawIndex;

                            // Found it!  Replace the while substring with a single icon char.
                            // Note the the brackets are removed but not kept in the label.
                            // We do the replacement by just changing the opening bracket to
                            // the icon character.
                            fatChars[start] = new FatChar(koduIcon);
                            fatChars[start].label = new List<FatChar>();
                            fatChars[start].icon = TextHelper.ControlInputs.key;

                            // Assign the raw index from the '<' or '[' as the rawIndex for the icon.
                            fatChars[start].rawIndex = rawIndex;

                            int len = end - start;
                            for (int i = 0; i < len; i++)
                            {
                                // Add the character to the label.
                                // Don't copy right bracket.
                                if (i < len - 1)
                                {
                                    fatChars[start].label.Add(fatChars[start + 1]);
                                }
                                // Remove it from the full string.
                                fatChars.RemoveAt(start + 1);
                            }

                            // See if the text for this key is actually meant to be a key icon.
                            // If so, find the icon.
                            // Note: The _only_ case where this is used is with arrowLeft, arrowRight, arrowDown, and arrowUp.
                            // So only call MatchControlAlias if "arrow" is found in the string.
                            string fatAsString = FatCharListToString(fatChars[start].label);
                            fatAsString = fatAsString.ToLower();
                            if (fatAsString.Contains("arrow"))
                            {
                                TextHelper.ControlInputs icon = TextHelper.MatchControlAlias(fatAsString);
                                if (icon != TextHelper.ControlInputs.none && icon > TextHelper.ControlInputs.nonButton)
                                {
                                    fatChars[start].keyIcon = icon;
                                }
                            }

                            start = end - len;
                            break;
                        }
                        else
                        {
                            ++end;
                        }
                    }
                }

                ++start;
            }

            // Now Icons.
            start = 0;
            end = 0;
            while (start < fatChars.Count - 1)
            {
                if (fatChars[start].c == '<')
                {
                    // Try and find the matching end.
                    end = start + 1;
                    while (end < fatChars.Count)
                    {
                        // Don't allow keycaps with embedded newlines.
                        if (fatChars[end].c == '\n')
                            break;

                        if (fatChars[end].c == '>')
                        {
                            // Found it!  
                            // Before anything else we need to check if it's a valid
                            // icon.  If not, just leave it as text.
                            string str = "";
                            for (int i = start + 1; i < end; i++)
                            {
                                str += fatChars[i].c;
                            }
                            TextHelper.ControlInputs icon = TextHelper.ControlInputs.none;
                            icon = TextHelper.MatchControlAlias(str);

                            // Handle score substitutions depending on mode.
                            if (icon > TextHelper.ControlInputs.nonButton)
                            {
                                if (InGame.inGame.CurrentUpdateMode == InGame.UpdateMode.RunSim)
                                {
                                    // If in run mode, we need to replace the <score ***> text with 
                                    // the actual score value.
                                    string score = TextHelper.GetStringSubstitution(icon, null);

                                    // Remove characters to be replaced.
                                    int len = end - start;
                                    for (int i = 0; i < len; i++)
                                    {
                                        // Remove char from the full string.
                                        fatChars.RemoveAt(start + 1);
                                    }

                                    // Add in the score string.
                                    for (int i = 0; i < str.Length; i++)
                                    {
                                        FatChar fc = new FatChar(str[i]);
                                        fatChars.Insert(start, fc);
                                    }
                                }
                                // In either case break since we don't want to create an icon.
                                break;
                            }


                            // If we've found a valid icon.
                            if (icon != TextHelper.ControlInputs.none)
                            {
                                // Save the current rawIndex .
                                int rawIndex = fatChars[start].rawIndex;

                                // Replace the while substring with a single icon char.
                                // Note the the brackets are removed but not kept in the label.
                                // We do the replacement by just changing the opening bracket to
                                // the icon character.
                                fatChars[start] = new FatChar(koduIcon);
                                fatChars[start].label = new List<FatChar>();
                                fatChars[start].icon = icon;

                                // Assign the raw index from the '<' or '[' as the rawIndex for the icon.
                                fatChars[start].rawIndex = rawIndex;

                                int len = end - start;
                                for (int i = 0; i < len; i++)
                                {
                                    // Add the character to the label.
                                    // Don't copy right bracket.
                                    if (i < len - 1)
                                    {
                                        fatChars[start].label.Add(fatChars[start + 1]);
                                    }
                                    // Remove it from the full string.
                                    fatChars.RemoveAt(start + 1);
                                }

                                start = end - len;
                            }

                            break;
                        }
                        else
                        {
                            ++end;
                        }
                    }
                }

                ++start;
            }

            // If we've done any substitutions we need to reset the logical and display order indices.
            for (int i = 0; i < fatChars.Count; i++)
            {
                fatChars[i].logicalOrderIndex = i;
                fatChars[i].displayOrderIndex = i;
            }

            // The cursor may also no longer be valid, ensure that it is.
            cursorPosition = (int)MathHelper.Clamp(cursorPosition, 0, fatChars.Count);
        }   // end of IconSubstitution()

        /// <summary>
        /// Takes a fatChars list and splits it into a single array of Words.
        /// NOTE  The offset member of the Words is not set.
        /// </summary>
        /// <returns>array or Words</returns>
        List<Word> SplitIntoWords(List<FatChar> fatChars)
        {
            List<Word> words = new List<Word>();

            int cursor = 0;

            while (cursor < fatChars.Count)
            {
                // Look at the current char.
                FatChar fc = fatChars[cursor];

                // Is it an icon?
                if (fc.c == koduIcon)
                {
                    Word w = new Word();
                    if (fc.icon == TextHelper.ControlInputs.key)
                    {
                        // Must be a keycap
                        if (fc.keyIcon != TextHelper.ControlInputs.none)
                        {
                            // Keycap with icon.
                            w.icon = fc.icon;
                            w.keyIcon = fc.keyIcon;
                            w.cursor = cursor;
                            w.str = FatCharListToString(fc.label);
                            w.text.Add(fc);
                        }
                        else
                        {
                            // Keycap with text.
                            w.icon = TextHelper.ControlInputs.key;
                            w.cursor = cursor;
                            w.str = FatCharListToString(fc.label);
                            w.text.Add(fc);
                        }
                    }
                    else if (fc.icon < TextHelper.ControlInputs.nonButton)
                    {
                        w.icon = fc.icon;
                        w.cursor = cursor;
                        w.str = FatCharListToString(fc.label);
                        w.str = w.str.ToLower();
                        w.text.Add(fc);
                    }
                    else
                    {
                        // TODO Should never get here.  We should either have left this alone(edit mode)
                        // or substituted the score(run mode) in IconSubstitution();
                        // Score text?
                        // Must be a score text substitution.  Only do this during runSim.
                        if (InGame.inGame.CurrentUpdateMode == InGame.UpdateMode.RunSim)
                        {
                            w.str = TextHelper.GetStringSubstitution(fc.icon, null);
                        }
                    }
                    ++cursor;
                    words.Add(w);
                }
                else if (fc.c == '\n')
                {
                    // Just a return.  Treat as it's own word.
                    Word w = new Word();
                    w.text.Add(fc);
                    w.str = "\n";
                    w.cursor = cursor;
                    ++cursor;
                    words.Add(w);
                }
                else
                {
                    // Make this more unicode friendly since there's more than spaces.
                    bool space = fc.originalType == "WS";
                    int pos = cursor + 1;

                    while (pos < fatChars.Count)
                    {
                        // Transitioning from space to non-space.
                        if (space && fatChars[pos].originalType != "WS")
                        {
                            break;
                        }

                        // Found an icon
                        if (fatChars[pos].c == koduIcon)
                        {
                            break;
                        }

                        // Found a return.
                        if (fatChars[pos].c == '\n')
                        {
                            ++pos;  // Include the return with the current word.
                            break;
                        }

                        space = fatChars[pos].originalType == "WS";
                        ++pos;
                    }
                    Word w = new Word();
                    w.cursor = cursor;
                    w.str = "";
                    for (int i = cursor; i < pos; i++)
                    {
                        w.text.Add(fatChars[i]);
                        ++cursor;
                        w.str += fatChars[i].c;
                    }
                    words.Add(w);
                }
            }

            // Calc width for all words.
            // Since SystemFont adds padding at the beginning and end of a rendered string we
            // subtract off an esitmate of that padding so that wrapped text is closer to correct.
#if !NETFX_CORE
            float estimatedPadding = GetFont().systemFont.Padding;
#else
            float estimatedPadding = 0;
#endif
            estimatedPadding = 0;
            for (int i = 0; i < words.Count; i++)
            {
                if (words[i].icon == TextHelper.ControlInputs.none)
                {
                    // Plain text word.
                    words[i].width = (int)(Math.Max(0, GetFont().MeasureString(words[i].str).X - 2.0f * estimatedPadding));
                }
                else if (words[i].icon == TextHelper.ControlInputs.key)
                {
                    // Create new keyFont if needed.
                    if (keyFont == null)
                    {
                        float keyFontSize = 14.0f;
                        string keyFontName = "Calibri";
                        System.Drawing.FontStyle keyFontStyle = System.Drawing.FontStyle.Regular;
                        SystemFont font = GetFont().systemFont;
                        if (font != null)
                        {
                            keyFontSize = font.Font.Size * 0.75f;
                        }
                        keyFont = SysFont.GetSystemFont(keyFontName, keyFontSize, keyFontStyle);
                    }

                    // Keycaps, both kinds.
                    if (words[i].keyIcon == TextHelper.ControlInputs.none)
                    {
                        // Keycap with text.  The 32 below just adds some spacing on either side of
                        // the text so it doesn't interfere with the graphic.
                        int strWidth = (int)keyFont.MeasureString(words[i].str).X + 32;
                        words[i].width = (int)Math.Max(ButtonSize.X * 40.0f / 64.0f, strWidth);
                    }
                    else
                    {
                        // Keycap with icon.
                        words[i].width = (int)(ButtonSize.X * 40.0f / 64.0f);
                    }
                }
                else if (words[i].icon == TextHelper.ControlInputs.programmingTile)
                {
                    // Programming tile.
                    words[i].width = ButtonWidth;
                }
                else if (words[i].icon != TextHelper.ControlInputs.none)
                {
                    // Standard button.
                    words[i].width = ButtonWidth;
                }
                else
                {
                }

            }

            return words;
        }   // end of SplitIntoWords()

        /// <summary>
        /// Convert a FatChar list to a simple string.
        /// </summary>
        /// <param name="fatChars"></param>
        /// <returns></returns>
        string FatCharListToString(List<FatChar> fatChars)
        {
            string result = "";

            for (int i = 0; i < fatChars.Count; i++)
            {
                result += fatChars[i].c;
            }

            return result;
        }   // end of FatCharListToString()

        /// <summary>
        /// Figures out which words go on a line.  Also handles adding hard-breaks to single words longer than a line.
        /// Note that this has to be done on the string in logical order, ie before reordering due to bidi rules.
        /// </summary>
        /// <param name="words">The input array of words which we divvy up into lines.</param>
        /// <returns></returns>
        List<Line> LayoutWords(List<Word> words)
        {
            List<Line> lines = new List<Line>();

            int totalWidth = 0;
            Line line = new Line();
            for (int i = 0; i < words.Count; i++)
            {
                // If the word would make the line too long then break.
                // But if this is the first word on the line then we
                // have to give it a hard-break.
                if (totalWidth + words[i].width > maxWidth)
                {
                    // If we've only got a single word and it's still 
                    // too big then we need to add a hard-break.
                    if (line.words.Count == 0)
                    {
                        // Save away the full word text.
                        string text = words[i].str;

                        // Break the word that was too long.
                        TextHelper.ClipStringToWidth(Font, maxWidth, ref words[i].str);
                        words[i].width = (int)Font().MeasureString(words[i].str).X;

                        // Create a new word to hold the remainder of the broken word.
                        Word newWord = new Word();
                        newWord.str = text.Substring(words[i].str.Length);
                        newWord.width = (int)Font().MeasureString(newWord.str).X;
                        newWord.cursor = words[i].cursor + words[i].str.Length;
                        // Copy fatchars from old word to new.
                        int len = words[i].str.Length;
                        while (words[i].text.Count > len)
                        {
                            newWord.text.Add(words[i].text[len]);
                            words[i].text.RemoveAt(len);
                        }

                        line.words.Add(words[i]);
                        words.Insert(i + 1, newWord);
                    }
                    else
                    {
                        --i;    // Keep the word for the next line.
                    }
                    lines.Add(line);
                    line = new Line();
                    totalWidth = 0;
                }
                else
                {
                    // The word fits.
                    /*
                    
                    // MErging words here seems to cause all kinds of problems, so let's not do that.
                     
                    // If the new word and the previous word are both text, merge the new one
                    // into the previous one.  This gives us proper spacing for SystemFont 
                    // rendering plus reduces draw calls.
                    if (line.words.Count > 0 &&
                        line.words[line.words.Count - 1].icon == TextHelper.ControlInputs.none && line.words[line.words.Count - 1].keyIcon == TextHelper.ControlInputs.none &&
                        words[i].icon == TextHelper.ControlInputs.none && words[i].keyIcon == TextHelper.ControlInputs.none)
                    {
                        // Merge words.
                        // Subtract off current word width.
                        totalWidth -= line.words[line.words.Count - 1].width;

                        // Combine text.
                        line.words[line.words.Count - 1].str += words[i].str;
                        foreach(FatChar fc in words[i].text)
                        {
                            line.words[line.words.Count - 1].text.Add(fc);
                        }

                        // Calc new word width.
                        line.words[line.words.Count - 1].width = (int)GetFont().MeasureString(line.words[line.words.Count - 1].str).X;
                        totalWidth += line.words[line.words.Count - 1].width;
                    }
                    else
                    */
                    {
                        line.words.Add(words[i]);
                        totalWidth += words[i].width;
                    }

                    // If last word, output current line.
                    if (i == words.Count - 1)
                    {
                        lines.Add(line);
                        continue;
                    }

                    // If word ends in \n, output current line.
                    if (words[i].str != null && words[i].str.Length > 0 && words[i].str[words[i].str.Length - 1] == '\n')
                    {
                        lines.Add(line);
                        line = new Line();
                        totalWidth = 0;
                    }
                }
            }

            // Copy fatchars for each line.
            foreach (Line l in lines)
            {
                foreach (Word w in l.words)
                {
                    foreach (FatChar fc in w.text)
                    {
                        l.text.Add(fc);
                    }
                }
            }

            // Calc offsets for all words.
            CalcWordOffsets(lines);

            // Calc cursor position for all like.
            foreach (Line l in lines)
            {
                l.cursor = l.words[0].cursor;
            }

            return lines;
        }   // end of LayoutWords()

        /// <summary>
        /// For each line, calculates the X offset of each word.
        /// </summary>
        /// <param name="lines"></param>
        void CalcWordOffsets(List<Line> lines)
        {
            int y = 0;
            for (int i = 0; i < lines.Count; i++)
            {
                int x = 0;
                for (int w = 0; w < lines[i].words.Count; w++)
                {
                    lines[i].words[w].offset = new Vector2(x, y);
                    x += lines[i].words[w].width;
                }
                y += TotalSpacing;
            }
        }   // end of CalcWordOffsets()

        void ApplyLigatures(List<FatChar> text)
        {
            for (int i = 0; i < text.Count - 1; i++)
            {
                Unicode.LigatureData ld = Unicode.GetLigatureData(text[i].c);
                
                // If not null, we have a possible winner.
                if (ld != null)
                {
                    // Look at each possible ligature.
                    foreach (Unicode.LigatureEntry le in ld.ligatures)
                    {
                        // Are there enough characters left in this line to be worth testing?
                        int numRemaining = text.Count - i - 1;
                        if (numRemaining >= le.chars.Length)
                        {
                            bool match = true;
                            for (int j = 0; j < le.chars.Length; j++)
                            {
                                if (le.chars[j] != text[i + j + 1].c)
                                {
                                    match = false;
                                    break;
                                }
                            }

                            if (match)
                            {
                                // We've found a match, do the substitution.
                                // Convert the existing first character to the joined result.
                                text[i].c = le.joined;
                                // Remove the no longer needed extra characters.
                                for (int j = 0; j < le.chars.Length; j++)
                                {
                                    text.RemoveAt(i + 1);
                                }
                            }
                        }
                    }
                }
            }
        }   // end of ApplyLigatures()

        void ApplyContextualForms(List<FatChar> text)
        {
            for (int i = 0; i < text.Count; i++)
            {
                Unicode.DecompMappingData dmd = Unicode.GetDecompMappingData(text[i].c);
                if (dmd != null)
                {
                    // Determine if the characters ahead and behind this one are also R.
                    bool ahead = false;
                    if (i > 0 && (text[i - 1].originalType == "R" || text[i - 1].originalType == "AL"))
                    {
                        ahead = true;
                    }

                    bool behind = false;
                    if (i < text.Count - 1 && (text[i + 1].originalType == "R" || text[i + 1].originalType == "AL"))
                    {
                        behind = true;
                    }

                    // isolated
                    if (ahead == false && behind == false && dmd.isolated != 0)
                    {
                        text[i].c = dmd.isolated;
                    }

                    // initial
                    if (ahead == false && behind == true && dmd.initial != 0)
                    {
                        text[i].c = dmd.initial;
                    }

                    // medial
                    if (ahead == true && behind == true && dmd.medial != 0)
                    {
                        text[i].c = dmd.medial;
                    }
                    
                    // final
                    if (ahead == true && behind == false && dmd.final != 0)
                    {
                        text[i].c = dmd.final;
                    }
                }
            }
        }   // end of ApplyContextualForms()


        Texture2D ButtonTexture(TextHelper.ControlInputs input)
        {
            Texture2D tex = null;

            switch (input)
            {
                case TextHelper.ControlInputs.aButton:
                    tex = ButtonTextures.AButton;
                    break;
                case TextHelper.ControlInputs.bButton:
                    tex = ButtonTextures.BButton;
                    break;
                case TextHelper.ControlInputs.xButton:
                    tex = ButtonTextures.XButton;
                    break;
                case TextHelper.ControlInputs.yButton:
                    tex = ButtonTextures.YButton;
                    break;
                case TextHelper.ControlInputs.leftStick:
                    tex = ButtonTextures.LeftStick;
                    break;
                case TextHelper.ControlInputs.rightStick:
                    tex = ButtonTextures.RightStick;
                    break;
                case TextHelper.ControlInputs.leftShoulder:
                    tex = ButtonTextures.LeftShoulderArrow;
                    break;
                case TextHelper.ControlInputs.rightShoulder:
                    tex = ButtonTextures.RightShoulderArrow;
                    break;
                case TextHelper.ControlInputs.leftTrigger:
                    tex = ButtonTextures.LeftTrigger;
                    break;
                case TextHelper.ControlInputs.rightTrigger:
                    tex = ButtonTextures.RightTrigger;
                    break;
                case TextHelper.ControlInputs.dpadUpDown:
                    tex = ButtonTextures.DPadUpDown;
                    break;
                case TextHelper.ControlInputs.dpadRightLeft:
                    tex = ButtonTextures.DPadRightLeft;
                    break;
                case TextHelper.ControlInputs.dpadUp:
                    tex = ButtonTextures.DPadUp;
                    break;
                case TextHelper.ControlInputs.dpadDown:
                    tex = ButtonTextures.DPadDown;
                    break;
                case TextHelper.ControlInputs.dpadRight:
                    tex = ButtonTextures.DPadRight;
                    break;
                case TextHelper.ControlInputs.dpadLeft:
                    tex = ButtonTextures.DPadLeft;
                    break;
                case TextHelper.ControlInputs.start:
                    tex = ButtonTextures.StartButton;
                    break;
                case TextHelper.ControlInputs.back:
                    tex = ButtonTextures.BackButton;
                    break;
                case TextHelper.ControlInputs.gamepad:
                    tex = ButtonTextures.Gamepad;
                    break;

                case TextHelper.ControlInputs.key:
                    tex = ButtonTextures.KeyFace;
                    break;
                case TextHelper.ControlInputs.arrowleft:
                    tex = ButtonTextures.ArrowLeft;
                    break;
                case TextHelper.ControlInputs.arrowright:
                    tex = ButtonTextures.ArrowRight;
                    break;
                case TextHelper.ControlInputs.arrowup:
                    tex = ButtonTextures.ArrowUp;
                    break;
                case TextHelper.ControlInputs.arrowdown:
                    tex = ButtonTextures.ArrowDown;
                    break;

                case TextHelper.ControlInputs.keyboard:
                    tex = ButtonTextures.Keyboard;
                    break;
                case TextHelper.ControlInputs.mouse:
                    tex = ButtonTextures.Mouse;
                    break;
                case TextHelper.ControlInputs.leftmouse:
                    tex = ButtonTextures.LeftMouse;
                    break;
                case TextHelper.ControlInputs.middlemouse:
                    tex = ButtonTextures.MiddleMouse;
                    break;
                case TextHelper.ControlInputs.rightmouse:
                    tex = ButtonTextures.RightMouse;
                    break;
                case TextHelper.ControlInputs.drag:
                    tex = ButtonTextures.Drag;
                    break;
                case TextHelper.ControlInputs.doubleDrag:
                    tex = ButtonTextures.DoubleDrag;
                    break;
                case TextHelper.ControlInputs.rotate:
                    tex = ButtonTextures.Rotate;
                    break;
                case TextHelper.ControlInputs.pinch:
                    tex = ButtonTextures.Pinch;
                    break;
                case TextHelper.ControlInputs.tap:
                    tex = ButtonTextures.Tap;
                    break;
                case TextHelper.ControlInputs.doubleTap:
                    tex = ButtonTextures.DoubleTap;
                    break;
                case TextHelper.ControlInputs.touchHold:
                    tex = ButtonTextures.TouchHold;
                    break;
                case TextHelper.ControlInputs.brushBigger:
                    tex = ButtonTextures.BrushBigger;
                    break;
                case TextHelper.ControlInputs.brushSmaller:
                    tex = ButtonTextures.BrushSmaller;
                    break;
                case TextHelper.ControlInputs.undo:
                    tex = ButtonTextures.Undo;
                    break;
                case TextHelper.ControlInputs.redo:
                    tex = ButtonTextures.Redo;
                    break;

                case TextHelper.ControlInputs.apple:
                    tex = ButtonTextures.Apple;
                    break;
                case TextHelper.ControlInputs.ball:
                    tex = ButtonTextures.Ball;
                    break;
                case TextHelper.ControlInputs.balloon:
                    tex = ButtonTextures.Balloon;
                    break;
                case TextHelper.ControlInputs.blimp:
                    tex = ButtonTextures.Blimp;
                    break;
                case TextHelper.ControlInputs.boat:
                    tex = ButtonTextures.Boat;
                    break;
                case TextHelper.ControlInputs.boku:
                    tex = ButtonTextures.Boku;
                    break;
                case TextHelper.ControlInputs.bullet:
                    tex = ButtonTextures.Bullet;
                    break;
                case TextHelper.ControlInputs.castle:
                    tex = ButtonTextures.Castle;
                    break;
                case TextHelper.ControlInputs.clam:
                    tex = ButtonTextures.Clam;
                    break;
                case TextHelper.ControlInputs.cloud:
                    tex = ButtonTextures.Cloud;
                    break;
                case TextHelper.ControlInputs.coin:
                    tex = ButtonTextures.Coin;
                    break;
                case TextHelper.ControlInputs.cursor:
                    tex = ButtonTextures.Cursor;
                    break;
                case TextHelper.ControlInputs.drum:
                    tex = ButtonTextures.Drum;
                    break;
                case TextHelper.ControlInputs.factory:
                    tex = ButtonTextures.Factory;
                    break;
                case TextHelper.ControlInputs.fan:
                    tex = ButtonTextures.Fan;
                    break;
                case TextHelper.ControlInputs.fastbot:
                    tex = ButtonTextures.Fastbot;
                    break;
                case TextHelper.ControlInputs.flyfish:
                    tex = ButtonTextures.Flyfish;
                    break;
                case TextHelper.ControlInputs.heart:
                    tex = ButtonTextures.Heart;
                    break;
                case TextHelper.ControlInputs.hut:
                    tex = ButtonTextures.Hut;
                    break;
                case TextHelper.ControlInputs.iceBerg:
                    tex = ButtonTextures.IceBerg;
                    break;
                case TextHelper.ControlInputs.inkjet:
                    tex = ButtonTextures.InkJet;
                    break;
                case TextHelper.ControlInputs.jet:
                    tex = ButtonTextures.Jet;
                    break;
                case TextHelper.ControlInputs.light:
                    tex = ButtonTextures.Light;
                    break;
                case TextHelper.ControlInputs.lilypad:
                    tex = ButtonTextures.Lilypad;
                    break;
                case TextHelper.ControlInputs.mine:
                    tex = ButtonTextures.Mine;
                    break;
                case TextHelper.ControlInputs.missile:
                    tex = ButtonTextures.Missile;
                    break;
                case TextHelper.ControlInputs.octopus:
                    tex = ButtonTextures.Octopus;
                    break;
                case TextHelper.ControlInputs.pad:
                    tex = ButtonTextures.Pad;
                    break;
                case TextHelper.ControlInputs.pipe:
                    tex = ButtonTextures.Pipe;
                    break;
                case TextHelper.ControlInputs.puck:
                    tex = ButtonTextures.Puck;
                    break;
                case TextHelper.ControlInputs.rock:
                    tex = ButtonTextures.Rock;
                    break;
                case TextHelper.ControlInputs.rockLowValue:
                    tex = ButtonTextures.RockLowValue;
                    break;
                case TextHelper.ControlInputs.rockHighValue:
                    tex = ButtonTextures.RockHighValue;
                    break;
                case TextHelper.ControlInputs.rockLowValueUnknown:
                    tex = ButtonTextures.RockLowValueUnknown;
                    break;
                case TextHelper.ControlInputs.rockHighValueUnknown:
                    tex = ButtonTextures.RockHighValueUnknown;
                    break;
                case TextHelper.ControlInputs.satellite:
                    tex = ButtonTextures.Satellite;
                    break;
                case TextHelper.ControlInputs.saucer:
                    tex = ButtonTextures.Saucer;
                    break;
                case TextHelper.ControlInputs.seagrass:
                    tex = ButtonTextures.Seagrass;
                    break;
                case TextHelper.ControlInputs.star:
                    tex = ButtonTextures.Star;
                    break;
                case TextHelper.ControlInputs.starfish:
                    tex = ButtonTextures.Starfish;
                    break;
                case TextHelper.ControlInputs.stick:
                    tex = ButtonTextures.Stick;
                    break;
                case TextHelper.ControlInputs.sub:
                    tex = ButtonTextures.Sub;
                    break;
                case TextHelper.ControlInputs.swimfish:
                    tex = ButtonTextures.Swimfish;
                    break;
                case TextHelper.ControlInputs.terracannon:
                    tex = ButtonTextures.Terracannon;
                    break;
                case TextHelper.ControlInputs.tree:
                    tex = ButtonTextures.Tree;
                    break;
                case TextHelper.ControlInputs.turtle:
                    tex = ButtonTextures.Turtle;
                    break;
                case TextHelper.ControlInputs.rover:
                    tex = ButtonTextures.Rover;
                    break;
                case TextHelper.ControlInputs.wisp:
                    tex = ButtonTextures.Wisp;
                    break;

                case TextHelper.ControlInputs.play:
                    tex = ButtonTextures.Play;
                    break;
                case TextHelper.ControlInputs.homeMenu:
                    tex = ButtonTextures.HomeMenu;
                    break;
                case TextHelper.ControlInputs.cameraMove:
                    tex = ButtonTextures.CameraMove;
                    break;
                case TextHelper.ControlInputs.objectEdit:
                    tex = ButtonTextures.ObjectEdit;
                    break;
                case TextHelper.ControlInputs.objectSettings:
                    tex = ButtonTextures.ObjectSettings;
                    break;
                case TextHelper.ControlInputs.paths:
                    tex = ButtonTextures.Paths;
                    break;
                case TextHelper.ControlInputs.terrainPaint:
                    tex = ButtonTextures.TerrainPaint;
                    break;
                case TextHelper.ControlInputs.terrainUpDown:
                    tex = ButtonTextures.TerrainUpDown;
                    break;
                case TextHelper.ControlInputs.terrainSmoothLevel:
                    tex = ButtonTextures.TerrainSmoothLevel;
                    break;
                case TextHelper.ControlInputs.terrainSpikeyHilly:
                    tex = ButtonTextures.TerrainSpikeyHilly;
                    break;
                case TextHelper.ControlInputs.deleteObjects:
                    tex = ButtonTextures.DeleteObjects;
                    break;
                case TextHelper.ControlInputs.water:
                    tex = ButtonTextures.Water;
                    break;
                case TextHelper.ControlInputs.worldSettings:
                    tex = ButtonTextures.WorldSettings;
                    break;
                case TextHelper.ControlInputs.waterType:
                    tex = ButtonTextures.WaterType;
                    break;
                case TextHelper.ControlInputs.materialType:
                    tex = ButtonTextures.MaterialType;
                    break;
                case TextHelper.ControlInputs.brushType:
                    tex = ButtonTextures.BrushType;
                    break;

                case TextHelper.ControlInputs.heartIcon:
                    tex = ButtonTextures.HeartIcon;
                    break;
                case TextHelper.ControlInputs.brokenHeartIcon:
                    tex = ButtonTextures.BrokenHeartIcon;
                    break;
                case TextHelper.ControlInputs.reportAbuseIcon:
                    tex = ButtonTextures.ReportAbuseIcon;
                    break;
                case TextHelper.ControlInputs.reportAbuseGreyIcon:
                    tex = ButtonTextures.ReportAbuseGreyIcon;
                    break;

            }

            return tex;
        }   // end of ButtonTexture()

        /// <summary>
        /// Takes the current cursor position and finds the line the cursor
        /// is on and the position in pixels of the cursor within that line.
        /// cursorPosition is in logical ordering.
        /// Note that we also need to know cursorChar to get the correct position
        /// when working with RtoL or mixed text.
        /// </summary>
        /// <param name="curLine">out : line the cursor is on.</param>
        /// <param name="xPos">out : position in pixels of cursor from beginning of line.</param>
        public void FindCursorLineAndPosition(out int curLine, out int xPos)
        {
            FlowText();

            // First, figure out which line the cursor is on.
            curLine = 0;
            for (int i = 1; i < lines.Count; i++)
            {
                if (lines[i].cursor > cursorPosition)
                {
                    break;
                }
                curLine = i;
            }

            // cursorPosition is in logical ordering.  For this we need
            // the display position of the char the cursor is at.
            int displayCursor = cursorPosition;

            if (hasRtoL)
            {
                // Handle end of line case.
                if (displayCursor == fatChars.Count)
                {
                    if (fatChars.Count > 0)
                    {
                        // Put the cursor 1 past the last character.
                        displayCursor = fatChars[cursorPosition - 1].displayOrderIndex + 1;
                        // Pull it back another space if R.
                        if (cursorChar.type == "R")
                        {
                            --displayCursor;
                        }
                    }
                }
                else if (cursorPosition == 0)
                {
                    // Beginning of paragraph case
                    displayCursor = fatChars[0].type == "L" ? 0 : lines[curLine].text.Count;
                }
                else if (cursorPosition > 0)
                {
                    // Get the type of the chars to the left and right of the cursor.
                    string left = "L";
                    string right = "R";

                    if (cursorPosition > 0)
                    {
                        left = fatChars[cursorPosition - 1].type;
                    }
                    if (cursorPosition < fatChars.Count)
                    {
                        right = fatChars[cursorPosition].type;
                    }

                    if (left == "L" && right == "L")
                    {
                        displayCursor = fatChars[cursorPosition].displayOrderIndex;
                    }
                    else if (left == "R" && right == "R")
                    {
                        displayCursor = fatChars[cursorPosition - 1].displayOrderIndex;
                        // Handle end of line case.
                        if (fatChars[cursorPosition - 1].c == '\n')
                        {
                            displayCursor = lines[curLine].cursor + lines[curLine].text.Count;
                        }
                    }
                    else if (left == "L" && right == "R")
                    {
                        if (cursorChar.type == "L")
                        {
                            displayCursor = fatChars[cursorPosition - 1].displayOrderIndex + 1;
                        }
                        else
                        {
                            displayCursor = fatChars[cursorPosition].displayOrderIndex + 1;
                        }
                    }
                    else if (left == "R" && right == "L")
                    {
                        if (cursorChar.type == "L")
                        {
                            displayCursor = fatChars[cursorPosition].displayOrderIndex;
                        }
                        else
                        {
                            displayCursor = fatChars[cursorPosition - 1].displayOrderIndex;
                        }
                    }

                }   // end of else in middle of line.

            }   // end if hasRtoL

            xPos = 0;

            // Check for no text case.
            if (lines.Count == 0)
            {
                if (justification == TextHelper.Justification.Left)
                {
                    xPos = 0;
                }
                else if (justification == TextHelper.Justification.Center)
                {
                    xPos = Width / 2;
                }
                else if (justification == TextHelper.Justification.Right)
                {
                    xPos = Width;
                }
                return;
            }

            // Words within a line have their cursor position set starting with 0.  Now
            // that we know the line we're on, offset displayCursor to match.
            displayCursor -= lines[curLine].cursor;

            // Now find how far from the left edge the cursor is.
            xPos = 0;
            Line line = lines[curLine];

            // First figure out the whole words.  We need to find the word
            // which has the largest cursor position less than or equal to 
            // the cursorPosition.
            // Note that this all has to be working on the text in display order.
            Word word = null;
            if (line.words.Count > 0)//fix bug 43718
            {
                word = line.words[0];
                Word prevWord = null;
                for (int i = 1; i < line.words.Count; i++)
                {
                    prevWord = line.words[i - 1];
                    word = line.words[i];

                    if (word.cursor > displayCursor)
                    {
                        word = prevWord;
                        break;
                    }

                    xPos += prevWord.width;
                }
            }

            // Add in any partial words.
            if (word != null)
            {
                if (word.text != null && word.icon == TextHelper.ControlInputs.none)
                {
                    int len = displayCursor - word.cursor;
                    len = (int)MathHelper.Min(len, word.str.Length);
                    string str = word.str;
                    if (BokuSettings.Settings.UseSystemFontRendering)
                    {
                        if (hasRtoL)
                        {
                            // Rebuild str using display ordering otherwise character widths are off and cursor positioning is wrong.

                            // TODO  Need to find minimal displayOrderIndex in word and start from there!

                            str = null;
                            for (int i = 0; i < word.text.Count; i++)
                            {
                                // Find fatchar with displayOrderIndex == (i + cursor) and add it's char to str.
                                for (int j = 0; j < word.text.Count; j++)
                                {
                                    if (word.text[j].displayOrderIndex == i + word.cursor)
                                    {
                                        str += word.text[j].c;
                                        break;
                                    }
                                }
                            }
                        }
                    }

                    if (!string.IsNullOrEmpty(str))
                    {
                        xPos += (int)GetFont().MeasureString(str.Substring(0, len)).X;
                    }
                }

                if (word.icon != TextHelper.ControlInputs.none)
                {
                    if (displayCursor > word.cursor)
                    {
                        xPos += word.width;
                    }
                }

            }

            // Handle the case where the last line ends in a \n.
            // The test is slightly different depending on whether or not we're in an RtoL or LtoR run.
            bool LtoREnd = curLine + 1 == lines.Count && word != null && word.text != null && displayCursor == word.cursor + word.str.Length && word.str.EndsWith("\n");
            bool RtoLEnd = curLine + 1 == lines.Count && line.words != null && line.words.Count > 0 && line.words[0].str == "\n";
            if (LtoREnd || RtoLEnd)
            {
                ++curLine;
                xPos = 0;
            }
            
            // Now take into account justification.
            if (justification == TextHelper.Justification.Left)
            {
                // Nothing to do here, move along.
            }
            else if (justification == TextHelper.Justification.Center)
            {
                // Calc margin due to centering.
                int margin = (Width - GetLineWidth(curLine)) / 2;
                xPos += margin;
            }
            else if (justification == TextHelper.Justification.Right)
            {
                // Calc margin due to right justification.
                int margin = Width - GetLineWidth(curLine);
                xPos += margin;
            }

        }   // end of FindCursorLineAndPosition()

        /// <summary>
        /// Given a line and a position in pixels, calculate the 
        /// matching cursor position as a character index.
        /// Note that this has to work from the display ordered words
        /// and return the position in logical ordering.
        /// </summary>
        /// <param name="line"></param>
        /// <param name="xPos">Position ini pixels from left edge of line on screen.</param>
        public void SetCursorPosition(int lineIndex, int xPos)
        {
            dirty = true;

            // Handle no text case.
            if (lines.Count == 0)
            {
                cursorPosition = 0;
                return;
            }

            // Hanldle end of paragraph case.
            if (lineIndex == lines.Count)
            {
                cursorPosition = fatChars.Count;
                return;
            }

            // Limit lineIndex to valid range.
            lineIndex = Math.Min(lines.Count - 1, lineIndex);

            Line line = lines[lineIndex];

            int position = 0;
            bool found = true;

            if (line.words.Count != 0)
            {
                position = line.words[0].cursor;

                // Find the best word.
                Word word = null;
                for (int i = 0; i < line.words.Count; i++)
                {
                    if (line.words[i].offset.X <= xPos)
                    {
                        position = line.words[i].cursor;
                        word = line.words[i];
                    }
                    else
                    {
                        break;
                    }
                }

                // Step through word to find best character.
                if (word != null)
                {
                    if (word.icon != TextHelper.ControlInputs.none)
                    {
                        // Should we be on right side of icon instead of left?
                        if (xPos - word.offset.X > word.width / 2.0f)
                        {
                            ++position;
                        }
                        // Word seems to determine value by whichever character was clicked on when
                        // the cursor position was set.  So, do the same.
                        cursorChar = word.text[0];
                    }
                    else
                    {
                        found = false;
                        int x = xPos - (int)word.offset.X;
                        for (int i = 1; i < word.str.Length; i++)
                        {
                            // When delta is positive that means we're just past the position we want.
                            int delta = (int)GetFont().MeasureString(word.str.Substring(0, i)).X - x;
                            if (delta >= 0)
                            {
                                // Decide which position we're closest to and pick that one.
                                int prevDelta = x - (int)GetFont().MeasureString(word.str.Substring(0, i - 1)).X;
                                if (prevDelta > delta)
                                {
                                    ++position;
                                }

                                // Word seems to determine value by whichever character was clicked on when
                                // the cursor position was set.  So, do the same.
                                cursorChar = word.text[i - 1];

                                found = true;
                                break;
                            }
                            ++position;
                        }

                    }
                }

            }

            // Translate back into logical order.
            position = Math.Min(position, line.text.Count);
            // If at end of line.
            if (position == line.text.Count)
            {
                cursorPosition = line.text.Count + line.cursor;
            }
            else
            {
                cursorPosition = line.text[position].logicalOrderIndex;
            }
            if (!found && cursorPosition == fatChars.Count - 1)
            {
                ++cursorPosition;
            }

        }   // end of SetCursorPosition()

        /// <summary>
        /// Moves the cursor based on the given mouse position.  Assumes that the blob
        /// is at 0,0 so the mouse position has been offset accordingly.
        /// </summary>
        /// <param name="mouse"></param>
        public void SetCursorToMousePosition(Vector2 mouse)
        {
            // Find line we should be on.
            int targetLine = (int)(mouse.Y / TotalSpacing);

            // Move there.
            int curLine = 0;
            int x = 0;
            FindCursorLineAndPosition(out curLine, out x);
            while (curLine > targetLine)
            {
                CursorUp();
                --curLine;
            }
            while (curLine < targetLine)
            {
                CursorDown();
                ++curLine;
            }

            // Now handle horizontal movement.

            FindCursorLineAndPosition(out curLine, out x);

            mouse.X = MathHelper.Clamp(mouse.X, 0, GetLineWidth(curLine));

            while (x > mouse.X)
            {
                CursorLeft();

                int newX = x;
                FindCursorLineAndPosition(out curLine, out newX);

                // Are we going in circles?  Happens on center alignment if we can't scroll any further.
                if (newX == x)
                {
                    break;
                }

                x = newX;
            }

            while (x < mouse.X)
            {
                CursorRight();

                int newX = x;
                FindCursorLineAndPosition(out curLine, out newX);
                mouse.X = Math.Min(mouse.X, GetLineWidth(curLine));

                // Special case.  When we get to the end of the line the cursor may
                // wrap to the next line.  In that case, just back it up one space
                // and break out of the loop.
                if (curLine != targetLine)
                {
                    CursorLeft();
                    break;
                }

                // Are we going in circles?  happens on center alignment if we can't scroll any further.
                if (newX == x)
                {
                    break;
                }

                x = newX;
            }

            // At this point the cursor is to the right of the character that was clicked on.  So, if it's
            // on the left half of this character we should move it left.
            if (x > 0)
            {
                // Calc error to right.
                float errorRight = x - mouse.X;

                // Move cursor to left and calc error there.
                CursorLeft();
                FindCursorLineAndPosition(out curLine, out x);
                float errorLeft = mouse.X - x;

                // If we were closer before, move it back.
                if (errorLeft > errorRight)
                {
                    CursorRight();
                }
            }

        }   // SetCursorToMousePosition()


        #endregion

    }   // end of class TextBlob

}   // end of namespace KoiX.Text
