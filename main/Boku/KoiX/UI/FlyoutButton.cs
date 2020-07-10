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

using Boku.Audio;
using Boku.Common;

namespace KoiX.UI
{
    /// <summary>
    /// Variation of button class specifically for the Main Menu.
    /// Slightly different rendering including right justification of label.
    /// 
    /// Also hacked to support adding a graphics to the end.  Currently this
    /// is used for the Report Abuse button for Community levels.
    /// </summary>
    public class FlyoutButton : Button
    {
        #region Members

        bool reportAbuse = false;

        #endregion

        #region Accessors
        #endregion

        #region Public

        public FlyoutButton(BaseDialog parentDialog, RectangleF rect, string labelId = null, string labelText = null, Callback onSelect = null, GamePadInput.Element element = GamePadInput.Element.None, ThemeSet theme = null)
            : base(parentDialog, rect, labelId: labelId, labelText: labelText, OnChange: onSelect, element: element, theme: theme)
        {
            // Since Flyout buttons are right-justified, give them a bit of a 
            // margin so the text isn't right on the edge of the button shape.
            label.Padding = new UI.Padding(24, 0, 24, 0);

            // "Report" Hack!!!
            if (label.LabelText.Contains("<ReportAbuseIcon>"))
            {
                label.Padding = new UI.Padding(24, 0, 64, 0);
                int index = label.LabelText.IndexOf('<');
                label.LabelText = label.LabelText.Substring(0, index);
                reportAbuse = true;
            }

            // Use rect size if non-zero.
            if (rect.Size != Vector2.Zero)
            {
                Size = rect.Size;
                label.Size = rect.Size - new Vector2(label.Padding.Horizontal, label.Padding.Vertical);
            }
        }   // end of c'tor

        public override void Recalc(Vector2 parentPosition)
        {
            base.Recalc(parentPosition);
        }   // end of Recalc()

        public override void Update(SpriteCamera camera, Vector2 parentPosition)
        {
            if (Active)
            {
                // For these buttons we want hover to also force focus to happen.
                if (Hover)
                {
                    if (!InFocus)
                    {
                        SetFocus();

                        if (!ParentDialog.Quiet)
                        {
                            Foley.PlayClickUp();
                        }
                    }
                }
            }

            if (Dirty)
            {
                Recalc(parentPosition);
            }

            base.Update(camera, parentPosition);
        }   // end of Update()

        public override void Render(SpriteCamera camera, Vector2 parentPosition)
        {
            Vector2 pos = Position + parentPosition;

            // Render for mouse.
            RoundedRect.Render(camera, pos, LocalRect.Size, CornerRadius, BodyColor,
                                outlineWidth: OutlineWidth, outlineColor: OutlineColor);
            if (label != null)
            {
                label.HorizontalJustification = Justification.Right;
                RectangleF labelRect = label.LocalRect;
                labelRect.Width = LocalRect.Width;
                label.LocalRect = labelRect;
                label.Render(camera, pos);

                if (reportAbuse)
                {
                    SpriteBatch batch = KoiLibrary.SpriteBatch;
                    batch.Begin(SpriteSortMode.Immediate, BlendState.NonPremultiplied, null, null, null, null, camera.ViewMatrix);
                    Texture2D icon = ButtonTextures.ReportAbuseIcon;
                    batch.Draw(icon, pos + new Vector2(labelRect.Width - 52, 0), Color.White);
                    batch.End();
                }
            }

        }   // end of Render()

        #endregion

        #region Internal
        #endregion

    }   // end of class FlyoutButton

}   // end of namespace KoiX.UI
