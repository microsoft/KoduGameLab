// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.


//#define MARGIN_DEBUG

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
    public class Label : BaseWidget
    {
        #region Members

        // Extra padding added to whatever padding is set by user.  This causes the
        // label to move down one pixel when clicked adding to the illusion that
        // this is a real button.
        static Padding unselectedPadding = new Padding(0, 0, 0, 1);
        static Padding selectedPadding = new Padding(0, 1, 0, 0);

        // If rect is bigger than needed, how do we align text in the space.
        Justification horizontalJustification = Justification.Center;
        Justification verticalJustification = Justification.Center;

        string labelId;         // Id string fed into Localize.
        string labelText;       // String we get back from Localize and actually render.  Or, if
                                // set explicitly, just render as is.

        SystemFont font;

        Twitchable<Color> color;
        Twitchable<Color> outlineColor;
        Twitchable<float> outlineWidth;

        #endregion

        #region Accessors

        public Twitchable<Color> TextColor
        {
            get { return color; }
        }

        public Twitchable<Color> TextOutlineColor
        {
            get { return outlineColor; }
        }

        public Twitchable<float> TextOutlineWidth
        {
            get { return outlineWidth; }
        }

        public Justification HorizontalJustification
        {
            get { return horizontalJustification; }
            set
            {
                if (horizontalJustification != value)
                {
                    horizontalJustification = value;
                    dirty = true;
                }
            }
        }

        public Justification VerticalJustification
        {
            get { return verticalJustification; }
            set
            {
                if (verticalJustification != value)
                {
                    verticalJustification = value;
                    dirty = true;
                }
            }
        }

        /// <summary>
        /// String Id send to Strings.Localize.  If this changes
        /// then the changes will propagate to the text.
        /// </summary>
        public string LabelId
        {
            get { return labelId; }
            set
            {
                if (labelId != value)
                {
                    labelId = value;
                    if (labelId != null)
                    {
                        labelText = Strings.Localize(labelId);
                    }
                    ParentDialog.Dirty = true;
                    Dirty = true;
                }
            }
        }

        /// <summary>
        /// Localized text that is seen on screen.
        /// </summary>
        public string LabelText
        {
            get { return labelText; }
            set
            {
                if (labelText != value)
                {
                    labelText = value;
                    labelId = null;     // Since we're setting the text explicitly, we don't have an Id.

                    ParentDialog.Dirty = true;
                    Dirty = true;
                }
            }
        }

        #endregion

        #region Public

        /// <summary>
        /// Create a label.
        /// Note there's no rectangle or position.  So, this c'tor assumes that the position
        /// will be set by the parent and that the size will grow as needed to fit
        /// the content.
        /// 
        /// If a labelId is given, it is then fed into the localization system to get the
        /// localized text to display.
        /// If the labelText is given, then it is displayed as is.
        /// If both are given, it is an error.
        /// </summary>
        /// <param name="parentDialog"></param>
        /// <param name="font"></param>
        /// <param name="color"></param>
        /// <param name="outlineColor"></param>
        /// <param name="outlineWidth"></param>
        /// <param name="labelId"></param>
        /// <param name="labelText"></param>
        public Label(BaseDialog parentDialog, SystemFont font, Color color, Color outlineColor = default(Color), float outlineWidth = 0, string labelId = null, string labelText = null, string id = null)
            : base(parentDialog, id: id)
        {
            Debug.Assert(labelId == null || labelText == null, "Only one should be given.");

            this.color = new Twitchable<Color>(Theme.QuickTwitchTime, TwitchCurve.Shape.EaseInOut, parent: parentDialog, startingValue: color);
            this.outlineColor = new Twitchable<Color>(Theme.QuickTwitchTime, TwitchCurve.Shape.EaseInOut, parent: parentDialog, startingValue: outlineColor);
            this.outlineWidth = new Twitchable<float>(Theme.QuickTwitchTime, TwitchCurve.Shape.EaseInOut, parent: parentDialog, startingValue: outlineWidth);

            this.font = font;

            this.labelId = labelId;
            this.labelText = labelText;

            // If we have a labelId, get hte localized text.
            if (labelId != null)
            {
                this.labelText = Strings.Localize(labelId);
            }

            focusable = false;
        }   // end of c'tor

        public override void Recalc(Vector2 parentPosition)
        {
            base.Recalc(parentPosition);
        }

        public override void Update(SpriteCamera camera, Vector2 parentPosition)
        {
            base.Update(camera, parentPosition);

            if (Active)
            {
                // See if string changed.  May happen when language changes...
                if (labelId != null)
                {
                    string newText = Strings.Localize(labelId);
                    if (newText != labelText)
                    {
                        labelText = newText;
                        ParentDialog.Dirty = true;
                        Dirty = true;
                    }
                }

                if (dirty)
                {
                    Recalc(parentPosition);
                }
            }

        }   // end of Update()

        public override void Render(SpriteCamera camera, Vector2 parentPosition)
        {
            bool yes = parentPosition.X == 456;

            if (Alpha > 0)
            {
                SpriteBatch batch = KoiLibrary.SpriteBatch;

                // Calc rect inside padding that we clip text to.
                RectangleF textRect = new RectangleF(LocalRect);

                // If size is 0, 0 just expand to min.  This stops us
                // from trying to render a 0 sized label.
                if (textRect.Size == Vector2.Zero)
                {
                    Debug.Assert(false, "Why do you have a 0 sized label?");
                    textRect.Size = CalcMinSize();
                }

                textRect.Position += parentPosition;
                Padding curPadding = Padding + (Selected ? selectedPadding : unselectedPadding);
                textRect.Position += new Vector2(curPadding.Left, curPadding.Top);
                textRect.Size -= new Vector2(curPadding.Horizontal, curPadding.Vertical);

                // Cull if possible.
                if (camera.CullTest(textRect) == Frustum.CullResult.TotallyOutside)
                {
                    return;
                }

                // CalcMinSize has padding added in so calc actual size of text only.
                Vector2 textSize = font.MeasureString(labelText);
                textSize = textSize.Ceiling();

                // Decide if we need to do scaling on text to get it to fit.
                Vector2 scaling = Vector2.One;
                if (textRect.Size != textSize)
                {
                    // If text is too wide to fit, compress it horizontally.
                    if (textRect.Width < textSize.X)
                    {
                        scaling.X = textRect.Width / textSize.X;
                    }

                    // If textRect is bigger than needed, apply justification.
                    Vector2 extraSpace = textRect.Size - textSize;

                    // Get position.  Note this is inside of Padding so
                    // we don't need to do anything for left or top edge.
                    Vector2 pos = textRect.Position;
                    if (extraSpace.X > 0)
                    {
                        switch (HorizontalJustification)
                        {
                            case Justification.Left:
                                // Nothing to do here.
                                break;
                            case Justification.Center:
                                pos.X += extraSpace.X / 2.0f;
                                break;
                            case Justification.Right:
                                pos.X += extraSpace.X + 1;
                                break;
                        }
                    }
                    if (extraSpace.Y > 0)
                    {
                        switch (VerticalJustification)
                        {
                            case Justification.Top:
                                // Nothing to do here.
                                break;
                            case Justification.Center:
                                pos.Y += extraSpace.Y / 2.0f;
                                break;
                            case Justification.Bottom:
                                pos.Y += extraSpace.Y + 1;
                                break;
                        }
                    }
                    textRect.Position = pos.Truncate();
                }

                // Render the text.
                SysFont.StartBatch(camera);
                {
                    SysFont.DrawString(labelText, textRect.Position, textRect, font, textColor: color.Value * Alpha, scaling: scaling, outlineColor: outlineColor.Value * Alpha, outlineWidth: outlineWidth.Value);
                }
                SysFont.EndBatch();

                // Note we cut-paste this code instead of calling the base render 
                // since we don't store away the rendered version of Padding.
                if (marginDebug)
                {
                    // Debug overlay, full rect.  This could actually go on BaseWidget Render() if we called it regularly...
                    RectangleF marginRect = new RectangleF(LocalRect);
                    marginRect.Position += parentPosition;
                    marginRect.Position -= new Vector2(margin.Left, margin.Top);
                    marginRect.Size += new Vector2(margin.Horizontal, margin.Vertical);
                    RoundedRect.Render(camera, marginRect, 0, Color.White * 0.25f);

                    RectangleF fullRect = new RectangleF(LocalRect);
                    fullRect.Position += parentPosition;
                    RoundedRect.Render(camera, fullRect, 0, Color.Blue * 0.25f);

                    RectangleF paddingRect = new RectangleF(LocalRect);
                    paddingRect.Position += parentPosition;
                    paddingRect.Position += new Vector2(curPadding.Left, curPadding.Top);
                    paddingRect.Size -= new Vector2(curPadding.Horizontal, curPadding.Vertical);
                    RoundedRect.Render(camera, paddingRect, 0, Color.Red * 0.25f);
                }
            }   // end if alpha > 0

            //base.Render(camera, parentPosition);
        }   // end of Render()

        public override void RegisterForInputEvents()
        {
            // Zzz...
        }

        public override void UnregisterForInputEvents()
        {
            // Zzz...
        }

        /// <summary>
        /// Returns the size of the label based on font and text.
        /// Assumes no compression of the text has happened.
        /// Result is sum of text size PLUS PADDING, ie this
        /// is the min size needed to render this without compression.
        /// Note this does not include any margin.
        /// </summary>
        /// <returns></returns>
        public override Vector2 CalcMinSize()
        {
            Vector2 size = font.MeasureString(labelText);
            size = size.Ceiling();
            size.X += Padding.Horizontal;
            size.Y += Padding.Vertical;

            return size;
        }   // end of CalcMinSize()

        #endregion

        #region InputEventHandler

        public override InputEventHandler HitTest(Vector2 hitLocation)
        {
            // Never register a hit for a label.
            return null;
        }   // end of HitTest()

        #endregion

        #region Internal
        #endregion


    }   // end of class Label

}   // end of namespace KoiX.UI
