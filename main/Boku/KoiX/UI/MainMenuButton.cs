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

namespace KoiX.UI
{
    /// <summary>
    /// Variation of button class specifically for the Main Menu.
    /// Slightly different redering including left justification of label.
    /// </summary>
    public class MainMenuButton : Button
    {
        #region Members
        #endregion

        #region Accessors
        #endregion

        #region Public

        public MainMenuButton(BaseDialog parentDialog, RectangleF rect, string labelId = null, string labelText = null, Callback onSelect = null, GamePadInput.Element element = GamePadInput.Element.None, ThemeSet theme = null)
            : base(parentDialog, rect, labelId: labelId, labelText: labelText, OnChange: onSelect, element: element, theme: theme)
        {
            // Since MainMenu buttons are left-justified, give them a bit of a 
            // margin so the text isn't right on the edge of the button shape.
            label.Padding = new UI.Padding(24, 0, 0, 0);
            label.HorizontalJustification = Justification.Left;

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
                RectangleF labelRect = label.LocalRect;
                labelRect.Width = LocalRect.Width;
                label.LocalRect = labelRect;
                label.Render(camera, pos);
            }

        }   // end of Render()

        #endregion

        #region Internal
        #endregion

    }   // end of class MainMenuButton

}   // end of namespace KoiX.UI
