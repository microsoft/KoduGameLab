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
using Boku.Common;
using Boku.SimWorld.Terra;

namespace KoiX.UI.Dialogs
{
    public class TerrainMaterialDialog : BaseDialogWithTitle
    {
        #region Members

        Button okButton;
        int margin = 8;         // Around button text.

        List<TerrainMaterialButton> siblings;

        List<WidgetSet> materialSets;   // Horizontal sets which each hold a row of material buttons.
        WidgetSet cubicSmoothSet;       // Set for holding cubic/smooth radio buttons.  Aligns with buttonSet.

        RadioButtonLabelHelp cubic;
        RadioButtonLabelHelp smooth;

        static bool fabricMode = false; // Keeps track of cubic vs fabric.

        #endregion

        #region Accessors

        /// <summary>
        /// If true, terrain painting is using fabric mode.
        /// If false, we're using cube mode.
        /// 
        /// This lives here because I can't really think of
        /// any place better for it.
        /// </summary>
        static public bool FabricMode
        {
            get { return fabricMode; }
            set { fabricMode = value; }
        }

        #endregion

        #region Public

        public TerrainMaterialDialog(RectangleF rect, string titleId, ThemeSet theme = null, Color backdropColor = default(Color))
            : base(rect, titleId, theme: theme)
        {
#if DEBUG
            _name = "TerrainMaterialDialog";
#endif
            // Change ref to what base class has set.
            // Allows us to use theme rather than this.Theme.
            theme = this.ThemeSet;


            siblings = new List<TerrainMaterialButton>();

            Vector2 displaySize = new Vector2(50, 50);

            // Body
            {
                bodySet.Orientation = Orientation.Vertical;
                bodySet.HorizontalJustification = Justification.Center;
                bodySet.VerticalJustification = Justification.Center;

                materialSets = new List<WidgetSet>();
                int numButtonsPerRow = 16;
                int numButtons = TerrainMaterial.MaxNum;
                int numRows = (int)Math.Ceiling(numButtons / (float)numButtonsPerRow);

                int index = 0;
                for (int row = 0; row < numRows; row++)
                {
                    WidgetSet set = new WidgetSet(this, RectangleF.EmptyRect, Orientation.Horizontal);
                    bodySet.AddWidget(set);

                    for (int i = 0; i < numButtonsPerRow; i++)
                    {
                        if (index < numButtons)
                        {
                            TerrainMaterialButton b = new TerrainMaterialButton(this, index, siblings, displaySize);
                            set.AddWidget(b);
                        }
                        ++index;
                    }
                }

            }

            //
            // Clone the current theme and modify for these buttons.
            theme = MainMenuDialog.GetButtonTheme(theme);

            buttonSet.Padding = new Padding(32, 16, 32, 16);

            buttonSet.HorizontalJustification = Justification.Full;

            // Cubic/Smooth.
            {
                SystemFont font = new SystemFont(theme.TextFontFamily, theme.TextBaseFontSize * 1.2f, System.Drawing.FontStyle.Regular);
                FontWrapper wrapper = new FontWrapper(null, font);
                GetFont Font = delegate() { return wrapper; };

                cubicSmoothSet = new WidgetSet(this, RectangleF.EmptyRect, Orientation.Horizontal, horizontalJustification: Justification.Left);
                buttonSet.AddWidget(cubicSmoothSet);

                List<RadioButton> siblings2 = new List<RadioButton>();

                float width = 128 + font.MeasureString(Strings.Localize("tools.cubic")).X;
                cubic = new RadioButtonLabelHelp(this, Font, null, width, siblings2, labelId: "tools.cubic", OnChange: OnCubic);
                cubicSmoothSet.AddWidget(cubic);

                width = 128 + font.MeasureString(Strings.Localize("tools.smooth")).X;
                smooth = new RadioButtonLabelHelp(this, Font, null, width, siblings2, labelId: "tools.smooth", OnChange: OnSmooth);
                cubicSmoothSet.AddWidget(smooth);
            }

            okButton = new Button(this, RectangleF.EmptyRect, OnChange: OnOK, theme: theme, labelId: "textDialog.ok");
            okButton.Size = okButton.CalcMinSize() + new Vector2(margin, 0);  // Match button size to label, with a bit of margin.
            okButton.Label.Size = okButton.Size;                              // Make label same size so it gets centered correctly.
            buttonSet.AddWidget(okButton);

            // Calc size of dialog...
            Rectangle = new RectangleF(-700, -450, 1400, 900);

            // Connect navigation links.
            CreateTabList();

            // Dpad nav includes all widgets.
            CreateDPadLinks();

        }   // end of c'tor

        void OnOK(BaseWidget w)
        {
            DialogManagerX.KillDialog(this);
        }   // end of OnOk()

        void OnCubic(BaseWidget w)
        {
            FabricMode = false;
        }   // end of OnCubic()

        void OnSmooth(BaseWidget w)
        {
            FabricMode = true;
        }   // end of OnSmooth()

        double nextAnimTime = 0;

        public override void Update(SpriteCamera camera)
        {
            if (Active)
            {
                Random rnd = KoiLibrary.Random;
                if (Time.WallClockTotalSeconds > nextAnimTime)
                {
                    float targetAngle = 2.0f * MathHelper.Pi * (float)rnd.NextDouble() - MathHelper.Pi;
                    float time = 0.5f + 2.0f * (float)rnd.NextDouble();

                    Vector2 rippleCenter = 2000.0f * new Vector2((float)rnd.NextDouble() - (float)rnd.NextDouble(), (float)rnd.NextDouble() - (float)rnd.NextDouble());
                    rippleCenter += camera.ScreenSize / 2.0f;
                    float rippleSpeed = 500.0f; // Pixels per second.

                    for (int i = 0; i< siblings.Count; i++)
                    {
                        Vector2 pos = siblings[i].ParentPosition + siblings[i].Position;
                        float dist = (rippleCenter - pos).Length();
                        float delay = dist / rippleSpeed;

                        Rotate(siblings[i], targetAngle, delay, time);

                    }   // end of loop over siblings.

                    nextAnimTime = Time.WallClockTotalSeconds + 4.1 + 2.0 * rnd.NextDouble();
                }
            }

            base.Update(camera);
        }   // end of Update()

        void Rotate(TerrainMaterialButton button, float angle, float delay, float time)
        {
            TwitchCompleteEvent onComplete = delegate(Object obj)
            {
                // Init twitch to do the actual rotation.
                TwitchManager.Set<float> set = delegate(float val, Object param) { button.Rotation = val; };
                TwitchManager.CreateTwitch<float>(button.Rotation, angle, set, time, TwitchCurve.Shape.EaseInOut);
            };

            // Init delay twitch.
            TwitchManager.Set<float> setDelay = delegate(float val, Object param) { };
            TwitchManager.CreateTwitch<float>(0, 1, setDelay, delay, TwitchCurve.Shape.Linear, onComplete: onComplete);
        }   // end of Rotate()

        public override void Render(SpriteCamera camera)
        {
            base.Render(camera);

            // Render the focus reticule _after_ the normal rendering.
            foreach (TerrainMaterialButton b in siblings)
            {
                b.RenderReticule(camera);
            }
        }   // end of Render()

        public override void Activate(params object[] args)
        {
            // Match button selections to current value.
            foreach (TerrainMaterialButton b in siblings)
            {
                if (b.MaterialIndex == Terrain.CurrentMaterialIndex)
                {
                    b.Selected = true;
                    b.SetFocus(overrideInactive: true);
                    break;
                }
            }

            if (FabricMode)
            {
                smooth.Selected = true;
            }
            else
            {
                cubic.Selected = true;
            }

            base.Activate(args);
        }   // end of Activate()

        public override void Deactivate()
        {
            // Copy resulting changes back to terrain.
            foreach (TerrainMaterialButton b in siblings)
            {
                if (b.Selected)
                {
                    Terrain.CurrentMaterialIndex = (ushort)b.MaterialIndex;
                }
            }

            base.Deactivate();
        }   // end of Deactivate()

        #endregion

        #region Internal
        #endregion

    }   // end of class TerrainMaterialDialog

}   // end of namespace KoiX.UI.Dialogs
