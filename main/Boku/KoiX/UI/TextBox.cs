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
    /// TextBox is a multi-line text display.  Note this is only for display,
    /// not editing.
    /// </summary>
    public class TextBox : BaseWidget
    {
        #region Members

        string textId;          // Id string fed into Localize.  If null we assume that the displayText was passed in and should be used as-is.
        string displayText;     // String we get back from Localize and actually render.

        TextBlob blob;

        GetFont font;

        Twitchable<Color> color;
        Twitchable<Color> outlineColor;
        Twitchable<float> outlineWidth;

        #endregion

        #region Accessors

        // Width for rendering text.
        public int Width
        {
            get { return blob.Width; }
            set
            {
                if (blob.Width != value)
                {
                    blob.Width = value;
                }
            }
        }

        /// <summary>
        /// Justification of text within the TextBox.
        /// </summary>
        public TextHelper.Justification Justification
        {
            get { return blob.Justification; }
            set { blob.Justification = value; }
        }

        /// <summary>
        /// Given the current text string and the width, this is
        /// how many lines of text will be rendered.
        /// </summary>
        public int NumLines
        {
            get { return blob.NumLines; }
        }

        public int LineSpacingAdjustment
        {
            get { return blob.LineSpacingAdjustment; }
            set { blob.LineSpacingAdjustment = value; }
        }

        /// <summary>
        /// TotalSpacing from the text blob.  This is the total height, in pixels, of a single line of text.
        /// </summary>
        public float TotalSpacing
        {
            get { return blob.TotalSpacing; }
        }

        /// <summary>
        /// Scrubbed text in blob.
        /// </summary>
        public string ScrubbedText
        {
            get { return blob.ScrubbedText; }
        }

        /// <summary>
        /// Raw text in blob.
        /// </summary>
        public string RawText
        {
            get { return blob.RawText; }
            set { blob.RawText = value; }
        }

        public string DisplayText
        {
            get { return displayText; }
            set
            {
                string foo = TextHelper.CleanUpString(value);
                if (displayText != foo)
                {
                    displayText = foo;
                    textId = null;
                    blob.RawText = displayText;
                    dirty = true;
                }
            }
        }

        #endregion

        #region Public

        public TextBox(BaseDialog parentDialog, GetFont Font, Color color, Color outlineColor = default(Color), string textId = null, string displayText = null, float outlineWidth = 0, string id = null)
            : base(parentDialog, id: id)
        {
            focusable = false;

            Debug.Assert(textId != null || displayText != null, "Can't both be null, wouldn't know what to do.");

            this.textId = textId;
            this.displayText = displayText;
            if (!string.IsNullOrEmpty(textId))
            {
                displayText = Strings.Localize(textId);
            }
            displayText = TextHelper.CleanUpString(displayText);

            this.font = Font;
            this.color = new Twitchable<Color>(Theme.QuickTwitchTime, TwitchCurve.Shape.EaseInOut, this, color);
            this.outlineColor = new Twitchable<Color>(Theme.QuickTwitchTime, TwitchCurve.Shape.EaseInOut, this, outlineColor);
            this.outlineWidth = new Twitchable<float>(Theme.QuickTwitchTime, TwitchCurve.Shape.EaseInOut, this, outlineWidth);

            blob = new TextBlob(Font, displayText, 100);

            Recalc(Vector2.Zero);
        }   // end of c'tor

        public override void Recalc(Vector2 parentPosition)
        {
            this.parentPosition = parentPosition;

            base.Recalc(parentPosition);
        }   // end of Recalc()

        public override void  Update(SpriteCamera camera, Vector2 parentPosition)
        {
            if (Active)
            {
                // Did text change?  May happen if language changes.
                if (!string.IsNullOrEmpty(textId))
                {
                    if (displayText != Strings.Localize(textId))
                    {
                        displayText = Strings.Localize(textId);
                        displayText = TextHelper.CleanUpString(displayText);
                        blob.RawText = displayText;
                        dirty = true;
                    }
                }

                if (dirty)
                {
                    Recalc(parentPosition);
                }

                localRect.Size = new Vector2(blob.Width, blob.NumLines * blob.TotalSpacing);
            }

 	        base.Update(camera, parentPosition);
        }   // end of Update()

        public override void  Render(SpriteCamera camera, Vector2 parentPosition)
        {
            if (alpha.Value > 0)
            {
                // Don't use scissor rect here.  We need to be able to pass a clipping rectangle down
                // into the text rendering code once rendered, it's just a texture which can be spun
                // around.

                // Render.
                blob.RenderText(camera, parentPosition + localRect.Position, color.Value * alpha.Value, outlineColor: outlineColor.Value * alpha.Value, outlineWidth: outlineWidth.Value);
            }
	        base.Render(camera, parentPosition);
        }

        /// <summary>
        /// Based on the width, font, etc. calculates the min
        /// size to display all the text.
        /// </summary>
        /// <returns></returns>
        public override Vector2 CalcMinSize()
        {
            return new Vector2(Width, blob.NumLines * blob.TotalSpacing);
        }   // end of CalcMinSize()

        public override void RegisterForInputEvents()
        {
            // Zzz...
        }

        public override void UnregisterForInputEvents()
        {
            // Zzz...
        }

        #endregion

        #region Internal
        #endregion

    }   // end of class TextBox
}
