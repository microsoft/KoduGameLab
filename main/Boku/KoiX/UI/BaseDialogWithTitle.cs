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
using KoiX.Input;
using KoiX.Geometry;
using KoiX.Managers;
using KoiX.Text;

namespace KoiX.UI
{
    /// <summary>
    /// A slightly more structured base dialog.  Allows for title region at top
    /// and buttons across the botton.
    /// </summary>
    public class BaseDialogWithTitle : BaseDialog
    {
        #region Members

        protected SpriteCamera camera;  // Ref passed in by DialogManager during Update().

        protected string titleId;       // Id string used for title.
        protected string titleText;     // Localized version of title.
        protected SystemFont titleFont;
        protected Label titleLabel;

        protected WidgetSet fullSet;    // Covers full dialog.
        protected WidgetSet titleSet;   // Covers title area.
        protected WidgetSet bodySet;    // Covers body area.  Note orientation is None, assumes user controlled layout.
        protected WidgetSet buttonSet;  // Covers buttons at bottom.

        protected bool showTitle = false;

        #endregion

        #region Accessors
        #endregion

        #region Public

        public BaseDialogWithTitle(RectangleF rect, string titleId = null, string titleText = null, ThemeSet theme = null)
            : base(theme: theme)
        {
            // Change ref to what base class has set.
            // Allows us to use theme rather than this.Theme.
            theme = this.ThemeSet;

            Debug.Assert(titleId == null || titleText == null, "Both can't have values.");

            this.titleId = titleId;
            this.titleText = titleText;

            showTitle = titleId != null || titleText != null;

            this.Rectangle = rect;

            // Create sets.
            fullSet = new WidgetSet(this, rect, Orientation.Vertical, verticalJustification: Justification.Full);
            fullSet.FitToParentDialog = true;
            
            titleSet = new WidgetSet(this, RectangleF.EmptyRect, Orientation.Horizontal, horizontalJustification: Justification.Left);
            bodySet = new WidgetSet(this, RectangleF.EmptyRect, Orientation.None);
            buttonSet = new WidgetSet(this, RectangleF.EmptyRect, Orientation.Horizontal, horizontalJustification: Justification.Right);

            AddWidget(fullSet);
            fullSet.AddWidget(titleSet);
            fullSet.AddWidget(bodySet);
            fullSet.AddWidget(buttonSet);

            // Create title and add to titleSet.
            if (showTitle)
            {
                titleFont = SysFont.GetSystemFont(theme.TextFontFamily, 1.5f * theme.TextBaseFontSize, System.Drawing.FontStyle.Bold);
                //titleLabel = new Label(this, titleId, titleFont, theme.LightTextColor, outlineColor: theme.DarkTextColor, outlineWidth: 1.5f);
                titleLabel = new Label(this, titleFont, theme.LightTextColor, labelId: titleId, labelText: titleText);
                titleLabel.Margin = new Padding(32, 8, 8, 8);
                titleLabel.Size = titleLabel.CalcMinSize();
                titleSet.AddWidget(titleLabel);
            }

            /*
            titleSet.RenderDebugOutline = true;
            bodySet.RenderDebugOutline = true;
            buttonSet.RenderDebugOutline = true;
            */
        }   // end of c'tor

        public override void Update(SpriteCamera camera)
        {
            this.camera = camera;

            // Set title size.
            if (showTitle)
            {
                float titleHeight = titleSet.Widgets[0].Size.Y + titleSet.Widgets[0].Margin.Vertical + titleSet.Padding.Vertical;
                titleSet.Size = new Vector2(rect.Width, titleHeight);
            }
            else
            {
                titleSet.Size = Vector2.Zero;
            }

            // Set button size.
            if (buttonSet.Widgets.Count > 0)
            {
                float buttonHeight = buttonSet.Widgets[0].Size.Y + buttonSet.Widgets[0].Margin.Vertical + buttonSet.Padding.Vertical;
                buttonSet.Size = new Vector2(rect.Width, buttonHeight);
            }

            // Fit body set between title and button sets.
            float bodyHeight = rect.Size.Y - titleSet.Size.Y - buttonSet.Size.Y;
            bodySet.Size = new Vector2(rect.Width, bodyHeight);

            // Force position updates.  Should we be able to do this via set alignment?
            /*
            Vector2 pos = Vector2.Zero;
            titleSet.Position = pos;
            pos.Y += titleHeight;
            bodySet.Position = pos;
            pos.Y += bodyHeight;
            buttonSet.Position = pos;
            */

            base.Update(camera);

        }   // end of Update()

        public override void Render(SpriteCamera camera)
        {
            if (state != State.Inactive)
            {
                // Cull if possible.
                if (camera.CullTest(Rectangle) == Boku.Common.Frustum.CullResult.TotallyOutside)
                {
                    return;
                }

                SpriteBatch batch = KoiLibrary.SpriteBatch;

                RenderBackdrop();

                if (RenderBaseTile)
                {
                    if (showTitle)
                    {
                        RoundedRect.Render(camera, Rectangle, cornerRadius.Value, theme.AccentColor,
                                            outlineColor: outlineColor.Value, outlineWidth: outlineWidth.Value,
                                            twoToneSecondColor: bodyColor.Value, twoToneSplitPosition: titleSet.LocalRect.Bottom, twoToneHorizontalSplit: true,
                                            bevelStyle: bevelStyle, bevelWidth: bevelWidth.Value,
                                            shadowStyle: shadowStyle, shadowOffset: shadowOffset.Value, shadowSize: shadowSize.Value, shadowAttenuation: 0.85f);
                    }
                    else
                    {
                        RoundedRect.Render(camera, Rectangle, cornerRadius.Value, bodyColor.Value,
                                            outlineColor: outlineColor.Value, outlineWidth: outlineWidth.Value,
                                            bevelStyle: bevelStyle, bevelWidth: bevelWidth.Value,
                                            shadowStyle: shadowStyle, shadowOffset: shadowOffset.Value, shadowSize: shadowSize.Value, shadowAttenuation: 0.85f);
                    }
                }                        

                RenderWidgets(camera);
            }
        }   // end of Render()

        #endregion

        #region Internal
        #endregion

    }   // end of class BaseDialogWithTitle

}   // end of namespace KoiX.UI
