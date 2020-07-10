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
using Microsoft.Xna.Framework.Input.Touch;

using KoiX;
using KoiX.Geometry;
using KoiX.Input;
using KoiX.Managers;
using KoiX.Text;
using KoiX.UI.Dialogs;

using Boku;
using Boku.Base;
using Boku.Common;
using Boku.Common.Xml;
using Boku.Programming;

namespace KoiX.UI
{
    /// <summary>
    /// A widget combo container that holds a GUI Button, a text edit box for its label, and a help button.
    /// 
    /// </summary>
    public class GUIButtonLabelHelp : WidgetSet
    {
        #region Members

        GUIButton button;
        SingleLineInputHelp buttonLabel;

        Classification.Colors color;
        Color colorRGB = Color.White;

        // Width of entire container.
        float width;

        string labelHelpId = "GuiButton";

        SpriteCamera camera;        // Local ref.

        #endregion

        #region Accessors
        #endregion

        #region Public

        public GUIButtonLabelHelp(BaseDialog parentDialog, GetFont Font, float width, Classification.Colors color, Callback OnChange = null, ThemeSet theme = null)
            : base(parentDialog, RectangleF.EmptyRect, orientation: Orientation.None, horizontalJustification: Justification.Full, verticalJustification: Justification.Top)
        {
#if DEBUG
            _name = "GUIButtonLabelHelp " + color.ToString();
#endif

            this.color = color;
            colorRGB = Classification.XnaColor(color);

            int margin = 8;
            OutlinePadding = new Padding(margin, margin, margin, margin);
            RenderOutline = true;

            if (theme == null)
            {
                theme = Theme.CurrentThemeSet.Clone() as ThemeSet;
            }

            button = GUIButtonManager.GetButton(color);

            this.width = width;

            Orientation = UI.Orientation.Horizontal;
            VerticalJustification = Justification.Center;
            HorizontalJustification = Justification.Right;

            {
                Callback onChange = delegate(BaseWidget w)
                {
                    string label = CleanButtonLabel(buttonLabel.Text);
                    button.Label = label;
                };
                buttonLabel = new SingleLineInputHelp(parentDialog, Font, "editWorldParams.label", labelHelpId, width - GUIButton.DefaultSize.X - margin, OnChange: onChange, theme: theme);
                AddWidget(buttonLabel);
            }

            // Set fixed positions.  Since we're using Orientation.None, no layout gets applied.
            // This also means that Margin and Padding are ignored.
            buttonLabel.Position = new Vector2(0, 0);
            localRect.Size = new Vector2(width, GUIButton.DefaultSize.Y);
        }   // end of c'tor

        public override void Update(SpriteCamera camera, Vector2 parentPosition)
        {
            this.camera = camera;

            base.Update(camera, parentPosition);
        }   // end of Update()

        public override void Render(SpriteCamera camera, Vector2 parentPosition)
        {
            base.Render(camera, parentPosition);

            // Render GUIButton over top of base render.
            GUIButtonManager.RenderButtonPreview(camera, button, Position + parentPosition, buttonLabel.TextNoDefault);

        }   // end of Render()

        public override void RegisterForInputEvents()
        {
            // Call base register first.  By putting the child widgets on the input stacks
            // first we can then put ourselves on and have priority.
            base.RegisterForInputEvents();
        }   // end of RegisterForInputEvents()

        public override void Activate(params object[] args)
        {
            // Init textbox with current label.
            buttonLabel.Text = button.Label;

            base.Activate(args);
        }   // end of Activate()

        public override void SetOnChange(Callback onChange)
        {
            this.onChange = onChange;
        }

        #endregion

        #region InputEventHandler
        #endregion

        #region Internal

        void OnHelp(BaseWidget w)
        {
            TextDialog helpDialog = SharedX.TextDialog;

            Debug.Assert(helpDialog.Active == false);

            helpDialog.TitleId = "mainMenu.help";
            helpDialog.BodyText = TweakScreenHelp.GetHelp(labelHelpId);
            DialogManagerX.ShowDialog(helpDialog);
        }   // end of OnHelp()

        string CleanButtonLabel(string buttonLabel)
        {
            buttonLabel = buttonLabel.Replace("\r", "");

            //Buttons used to allow a single new line. 
            //Now they autowrap via text blob so replace
            //old \n with space.
            buttonLabel = buttonLabel.Replace("\r", " ");

            buttonLabel = buttonLabel.Trim();

            buttonLabel = TextHelper.FilterURLs(buttonLabel);
            buttonLabel = TextHelper.FilterEmail(buttonLabel);

            return buttonLabel;
        }

        #endregion

    }   // end of class GUIButtonLabelHelp

}   // end of namespace KoiX.UI
