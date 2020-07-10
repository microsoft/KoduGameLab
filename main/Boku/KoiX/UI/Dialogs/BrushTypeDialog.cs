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
    /// <summary>
    /// Modal dialog for choosing brush shape.
    /// </summary>
    public class BrushTypeDialog : BaseDialogWithTitle
    {
        #region Members

        GraphicRadioButton roundButton;
        GraphicRadioButton squareButton;
        GraphicRadioButton linearRoundButton;
        GraphicRadioButton linearSquareButton;
        GraphicRadioButton magicButton;

        GraphicRadioButton mediumRoundButton;   // For hills only.
        GraphicRadioButton softRoundButton;     // For hills only.

        Button okButton;
        int margin = 8;         // Around button text.

        List<GraphicRadioButton> siblings;

        string curBrushId;
        EditModeTools curTool;  // Which tool was active when this dialog was launched.

        #endregion

        #region Accessors

        /// <summary>
        /// Stringly typed identifier for the current brush selection.
        /// </summary>
        public string CurBrushId
        {
            get { return curBrushId; }
            set 
            {
                if (curBrushId != value)
                {
                    curBrushId = value;
                    foreach (GraphicRadioButton b in siblings)
                    {
                        if (b.Id == curBrushId)
                        {
                            // Note that we don't need to un-select the other buttons
                            // since they are based on RadioButtons.
                            b.Selected = true;
                            break;
                        }
                    }
                }
            }
        }

        #endregion

        #region Public

        public BrushTypeDialog(RectangleF rect, string titleId, ThemeSet theme = null, Color backdropColor = default(Color))
            : base(rect, titleId, theme: theme)
        {
#if DEBUG
            _name = "BrushTypeDialog";
#endif
            // Change ref to what base class has set.
            // Allows us to use theme rather than this.Theme.
            theme = this.ThemeSet;

            Rectangle = new RectangleF(-368, -210, 736, 340);

            // Don't show a backdrop?
            //BackdropColor = Color.Transparent;

            siblings = new List<GraphicRadioButton>();

            // Create sets.
            fullSet = new WidgetSet(this, rect, Orientation.Horizontal, horizontalJustification: Justification.Center, verticalJustification: Justification.Center);
            fullSet.FitToParentDialog = true;
            AddWidget(fullSet);

            Vector2 buttonSize = new Vector2(96, 96);

            bodySet.Orientation = Orientation.Horizontal;
            bodySet.HorizontalJustification = Justification.Center;
            bodySet.VerticalJustification = Justification.Center;

            roundButton = new GraphicRadioButton(this, siblings, buttonSize, textureName: @"Textures\Terrain\Round", OnChange: OnChange, theme: theme, id: "round", data: Brush2DManager.BrushShape.Round);
            bodySet.AddWidget(roundButton);

            squareButton = new GraphicRadioButton(this, siblings, buttonSize, textureName: @"Textures\Terrain\Square", OnChange: OnChange, theme: theme, id: "square", data: Brush2DManager.BrushShape.Square);
            bodySet.AddWidget(squareButton);

            linearRoundButton = new GraphicRadioButton(this, siblings, buttonSize, textureName: @"Textures\Terrain\LinearRound", OnChange: OnChange, theme: theme, id: "linearRound", data: Brush2DManager.BrushShape.LinearRound);
            bodySet.AddWidget(linearRoundButton);

            linearSquareButton = new GraphicRadioButton(this, siblings, buttonSize, textureName: @"Textures\Terrain\LinearSquare", OnChange: OnChange, theme: theme, id: "linearSquare", data: Brush2DManager.BrushShape.LinearSquare);
            bodySet.AddWidget(linearSquareButton);

            magicButton = new GraphicRadioButton(this, siblings, buttonSize, textureName: @"Textures\Terrain\Magic", OnChange: OnChange, theme: theme, id: "magic", data: Brush2DManager.BrushShape.Magic);
            bodySet.AddWidget(magicButton);

            mediumRoundButton = new GraphicRadioButton(this, siblings, buttonSize, textureName: @"Textures\Terrain\MediumRound", OnChange: OnChange, theme: theme, id: "mediumRound", data: Brush2DManager.BrushShape.MediumRound);
            bodySet.AddWidget(mediumRoundButton);

            softRoundButton = new GraphicRadioButton(this, siblings, buttonSize, textureName: @"Textures\Terrain\SoftRound", OnChange: OnChange, theme: theme, id: "softRound", data: Brush2DManager.BrushShape.SoftRound);
            bodySet.AddWidget(softRoundButton);

            // Clone the current theme and modify for these buttons.
            theme = MainMenuDialog.GetButtonTheme(theme);

            buttonSet.Padding = new Padding(32, 16, 32, 16);

            okButton = new Button(this, new RectangleF(), OnChange: OnOK, theme: theme, labelId: "textDialog.ok");
            okButton.Size = okButton.CalcMinSize() + new Vector2(margin, 0);  // Match button size to label, with a bit of margin.
            okButton.Label.Size = okButton.Size;                              // Make label same size so it gets centered correctly.
            buttonSet.AddWidget(okButton);

            SetSize();

            // Connect navigation links.
            CreateTabList();

            // Dpad nav includes all widgets.
            CreateDPadLinks();

        }   // end of c'tor

        /// <summary>
        /// Common handler for all selections.
        /// </summary>
        /// <param name="w"></param>
        void OnChange(BaseWidget w)
        {
            Brush2DManager.BrushShape shape = (Brush2DManager.BrushShape)w.Data;
            Brush2DManager.SetBrushOnTool(curTool, shape);
        }   // end of OnRound()

        void OnOK(BaseWidget w)
        {
            DialogManagerX.KillDialog(this);
        }   // end of OnSignOut()

        /// <summary>
        /// Sets up the dialog with the buttons needed to match the current tool.
        /// </summary>
        /// <param name="curTool"></param>
        public void Init(EditModeTools curTool)
        {
            this.curTool = curTool;

            bodySet.Widgets.Clear();

            switch(curTool)
            {
                case EditModeTools.TerrainPaint:
                    bodySet.AddWidget(roundButton);
                    bodySet.AddWidget(squareButton);
                    bodySet.AddWidget(linearRoundButton);
                    bodySet.AddWidget(linearSquareButton);
                    bodySet.AddWidget(magicButton);
                    break;
                case EditModeTools.TerrainRaiseLower:
                case EditModeTools.TerrainSmoothLevel:
                case EditModeTools.TerrainSpikeyHilly:
                    bodySet.AddWidget(roundButton);
                    bodySet.AddWidget(mediumRoundButton);
                    bodySet.AddWidget(softRoundButton);
                    bodySet.AddWidget(squareButton);
                    bodySet.AddWidget(magicButton);
                    break;
                case EditModeTools.EraseObjects:
                    bodySet.AddWidget(roundButton);
                    bodySet.AddWidget(squareButton);
                    bodySet.AddWidget(magicButton);
                    break;
                default:
                    Debug.Assert(false, "Unexpected tool.");
                    break;
            }

            SetSize();

            // Figure out which is Selected.
            Brush2DManager.BrushShape curBrush = Brush2DManager.GetBrushForTool(curTool);
            foreach (BaseWidget w in bodySet.Widgets)
            {
                GraphicRadioButton b = w as GraphicRadioButton;
                if (b != null && (Brush2DManager.BrushShape)b.Data == curBrush)
                {
                    b.Selected = true;
                    b.SetFocus(overrideInactive: true);
                    break;
                }
            }

        }   // end of Init()

        public override void Activate(params object[] args)
        {
            base.Activate(args);
        }   // end of Activate()

        public override void Deactivate()
        {
            base.Deactivate();
        }   // end of Deactivate()

        #endregion

        #region Internal

        /// <summary>
        /// Reset the size of the dialog ot match teh number of available brushes.
        /// </summary>
        void SetSize()
        {
            float width = bodySet.Widgets.Count * 96 + 32 * 2;
            float height = titleSet.LocalRect.Height + buttonSet.LocalRect.Height + 96 + 32;
            Rectangle = new RectangleF(new Vector2(-width / 2.0f, Rectangle.Top), new Vector2(width, height));
            bodySet.Size = new Vector2(width, 96 + 32);

            // Force an update so the size changes flow through.
            Update(new SpriteCamera());

        }   // end of SetSize()

        #endregion

    }   // end of class BrushTypeDialog

}   // end of namespace KoiX.UI.Dialogs
