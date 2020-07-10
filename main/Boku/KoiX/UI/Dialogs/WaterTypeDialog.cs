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

using Boku;
using Boku.SimWorld.Terra;

namespace KoiX.UI.Dialogs
{
    public class WaterTypeDialog : BaseDialogWithTitle
    {
        #region Members

        Button okButton;
        int margin = 8;         // Around button text.

        List<WaterTypeButton> siblings;

        WidgetSet row1Set;
        WidgetSet row2Set;

        #endregion

        #region Accessors
        #endregion

        #region Public

        public WaterTypeDialog(RectangleF rect, string titleId, ThemeSet theme = null, Color backdropColor = default(Color))
            : base(rect, titleId, theme: theme)
        {
#if DEBUG
            _name = "WaterTypeDialog";
#endif
            // Change ref to what base class has set.
            // Allows us to use theme rather than this.Theme.
            theme = this.ThemeSet;

            Rectangle = new RectangleF(-368, -210, 736, 420);

            siblings = new List<WaterTypeButton>();

            Vector2 displaySize = new Vector2(96, 96);

            // Body
            {
                bodySet.Orientation = Orientation.Vertical;
                bodySet.HorizontalJustification = Justification.Center;
                bodySet.VerticalJustification = Justification.Center;

                row1Set = new WidgetSet(this, RectangleF.EmptyRect, Orientation.Horizontal);
                bodySet.AddWidget(row1Set);
                row2Set = new WidgetSet(this, RectangleF.EmptyRect, Orientation.Horizontal);
                bodySet.AddWidget(row2Set);

                Debug.Assert(Water.Types.Count == 10, "This layout is set up for 10 elements.  Need to redo for more.");

                for (int i = 0; i < Water.Types.Count; i++)
                {
                    WaterTypeButton button = new WaterTypeButton(this, i, siblings, displaySize);

                    if (i < 5)
                    {
                        row1Set.AddWidget(button);
                    }
                    else
                    {
                        row2Set.AddWidget(button);
                    }
                }
            }

            //
            // Clone the current theme and modify for these buttons.
            theme = MainMenuDialog.GetButtonTheme(theme);

            buttonSet.Padding = new Padding(32, 16, 32, 16);

            okButton = new Button(this, new RectangleF(), OnChange: OnOK, theme: theme, labelId: "textDialog.ok");
            okButton.Size = okButton.CalcMinSize() + new Vector2(margin, 0);  // Match button size to label, with a bit of margin.
            okButton.Label.Size = okButton.Size;                              // Make label same size so it gets centered correctly.
            buttonSet.AddWidget(okButton);

            // Connect navigation links.
            CreateTabList();

            // Dpad nav includes all widgets.
            CreateDPadLinks();

        }   // end of c'tor

        void OnOK(BaseWidget w)
        {
            DialogManagerX.KillDialog(this);
        }   // end of OnSignOut()

        double nextAnimTime = 0;

        public override void Update(SpriteCamera camera)
        {
            if (Active)
            {
                Random rnd = KoiLibrary.Random;
                if (Time.WallClockTotalSeconds > nextAnimTime)
                {
                    float targetAngle = 2.0f * MathHelper.Pi * (float)rnd.NextDouble() - MathHelper.Pi;
                    int index = rnd.Next(10);
                    float time = 0.5f + 2.0f * (float)rnd.NextDouble();

                    TwitchManager.Set<float> set = delegate(float val, Object param) { siblings[index].Rotation = val; };
                    TwitchManager.CreateTwitch<float>(siblings[index].Rotation, targetAngle, set, time, TwitchCurve.Shape.EaseInOut);

                    nextAnimTime = Time.WallClockTotalSeconds + 0.1 + rnd.NextDouble();
                }
            }

            base.Update(camera);
        }   // end of Update()

        public override void Activate(params object[] args)
        {
            // Match button selections to current value.
            foreach (WaterTypeButton b in siblings)
            {
                if (b.WaterType == Water.CurrentType)
                {
                    b.Selected = true;
                    b.SetFocus(overrideInactive: true);
                    break;
                }
            }

            base.Activate(args);
        }   // end of Activate()

        public override void Deactivate()
        {
            // Copy resulting changes back to water.
            foreach (WaterTypeButton b in siblings)
            {
                if (b.Selected)
                {
                    Water.CurrentType = b.WaterType;
                }
            }

            base.Deactivate();
        }   // end of Deactivate()

        #endregion

        #region Internal
        #endregion

    }   // end of class WaterTypeDialog

}   // end of namespace KoiX.UI.Dialogs
