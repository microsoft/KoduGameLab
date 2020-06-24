
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

using Boku.Common;

namespace KoiX.UI
{
    /// <summary>
    /// A widget combo container that holds a label (title), a single help button, and
    /// a set of labelled radio buttons.
    /// 
    /// Note that the labels are actualy implemented by a TextBox so that they may be multi-line.
    /// </summary>
    public class GraphicRadioButtonSetLabelHelp : WidgetSet
    {
        #region Members

        TextBox label;
        HelpButton helpButton;
        List<GraphicRadioButton> radioButtons;

        GraphicRadioButton focusButton = null;      // Which button, if any, is in focus.

        string labelId;     // Normally we wouldn't store this redundantly since it will
        string labelText;   // be in the actual label, but for dynamically changing the
                            // text, we need this.
        string helpId;      // Id for help text associated with this combo.

        // Width of entire container.
        float width;

        WidgetSet titleSet;     // Contains the title and help button.
        WidgetSet buttonSet;    // Contains the buttons.

        #endregion

        #region Accessors

        // Is this set in focus.  This happens when one of the contained radio buttons
        // is in focus.
        new public bool InFocus
        {
            get { return focusButton != null; }
        }

        public List<GraphicRadioButton> RadioButtons
        {
            get { return radioButtons; }
        }

        #endregion

        #region Public

        public GraphicRadioButtonSetLabelHelp(BaseDialog parentDialog, GetFont Font, string helpId, float width, List<GraphicRadioButton> radioButtons, string labelId = null, string labelText = null, Callback OnChange = null, ThemeSet theme = null, object data = null)
            : base(parentDialog, RectangleF.EmptyRect, orientation: Orientation.None, horizontalJustification: Justification.Full, verticalJustification: Justification.Top, data: data)
        {
            // Change ref to what base class has set.
            // Allows us to use theme rather than this.Theme.
            theme = this.ThemeSet;

            this.labelId = labelId;
            this.labelText = labelText;
            if (labelId != null)
            {
                this.labelText = Strings.Localize(labelId);
            }

            // Set up nested sets.
            Orientation = UI.Orientation.Vertical;
            HorizontalJustification = Justification.Left;
            VerticalJustification = Justification.Top;

            titleSet = new WidgetSet(parentDialog, RectangleF.EmptyRect, orientation: UI.Orientation.None);
            buttonSet = new WidgetSet(parentDialog, RectangleF.EmptyRect, orientation: UI.Orientation.None);

            AddWidget(titleSet);
            AddWidget(buttonSet);

            this.radioButtons = radioButtons;
            Debug.Assert(radioButtons.Count > 1, "This requires at least 2 radio buttons.");

            int margin = 8;

            this.width = width;
            this.helpId = helpId;

            if (helpId != null)
            {
                helpButton = new HelpButton(parentDialog, OnHelp, data: data);
            }

            label = new TextBox(parentDialog, Font, theme.DarkTextColor, textId: labelId, displayText: labelText);
            // Calc width of label.
            float textWidth = width;
            textWidth -= helpId != null ? helpButton.LocalRect.Width : 0;
            textWidth -= margin;
            label.Width = (int)textWidth;

            localRect.Width = width;
            localRect.Height = label.NumLines * label.TotalSpacing;

            // For titleSet to be full width.
            {
                RectangleF rect = titleSet.LocalRect;
                rect.Width = width;
                rect.Height = 48;   // Vertical size of help button.
                titleSet.LocalRect = rect;
            }

            titleSet.AddWidget(label);
            if (helpId != null)
            {
                titleSet.AddWidget(helpButton);
            }

            foreach (GraphicRadioButton r in radioButtons)
            {
                buttonSet.AddWidget(r);
                // Expand buttonSet to containt all the buttons.
                buttonSet.LocalRect = RectangleF.Union(buttonSet.LocalRect, r.LocalRect);
            }

            // Set fixed positions.  Since we're using Orientation.None, no layout gets applied.
            label.Position = new Vector2(margin, 0);
            // Right justify help.
            if (helpId != null)
            {
                helpButton.Position = new Vector2(width - helpButton.LocalRect.Width - margin, 0);
            }

        }   // end of c'tor

        public override void Update(SpriteCamera camera, Vector2 parentPosition)
        {
            // Keep focusButton up to date.
            focusButton = null;
            foreach (GraphicRadioButton r in radioButtons)
            {
                if (r.InFocus)
                {
                    focusButton = r;
                    break;
                }
            }

            // Keep label up to date.
            if (InFocus)
            {
                // If one of our buttons is in focus, use that as the label.
                // Note these should be localized versions of the strings.
                string str = labelText + ": " + focusButton.LabelText;
                label.DisplayText = str;
            }
            else
            {
                // If not if focus, use the string from the selected button.
                string str = labelText + ": ";
                foreach (GraphicRadioButton grb in radioButtons)
                {
                    if (grb.Selected)
                    {
                        str += grb.LabelText;
                        break;
                    }
                }
                label.DisplayText = str;
            }

            base.Update(camera, parentPosition);
        }   // end of Update()

        public override void Render(SpriteCamera camera, Vector2 parentPosition)
        {
            // We look for Hover here since it requires that no buttons are currently pressed.
            // The reason for this has to do with not changing focus while manipulating
            // another widget.  For instance, if while moving a slider (right button pressed)
            // you drag the mouse down to the next widget, the original one should stay in
            // focus until the button is released.
            if (KoiLibrary.LastTouchedDeviceIsMouse)
            {
                foreach (GraphicRadioButton r in radioButtons)
                {
                    if (r.Hover)
                    {
                        r.SetFocus();
                        break;
                    }
                }
            }

            base.Render(camera, parentPosition);
        }   // end of Render()

        public override void RegisterForInputEvents()
        {
            // Call base register first.  By putting the child widgets on the input stacks
            // first we can then put oursleves on and have priority.
            base.RegisterForInputEvents();

            // Need to look for events in areas other than the child widgets.
            KoiLibrary.InputEventManager.RegisterForEvent(this, InputEventManager.Event.Keyboard);
            KoiLibrary.InputEventManager.RegisterForEvent(this, InputEventManager.Event.GamePad);
        }   // end of RegisterForInputEvents()

        /*
        /// <summary>
        /// Changes the text of the label.  Note that this assumes we've got localized strings.
        /// TODO (****) Update this so it also works with string ids.
        /// </summary>
        /// <param name="label"></param>
        public void SetLabel(string labelString)
        {
            label.DisplayText = labelString;
        }   // end of SetLabel()
        */

        #endregion

        #region InputEventHandler

        public override bool ProcessKeyboardEvent(KeyInput input)
        {
            Debug.Assert(Active);

            if (focusButton != null)
            {
                switch (input.Key)
                {
                    case Keys.F1:
                        if (helpButton != null)
                        {
                            helpButton.OnButtonSelect();
                        }
                        return true;

                    default:
                        // Do nothing here, just let fall through to base.
                        break;
                }
            }

            return base.ProcessKeyboardEvent(input);
        }   // end of ProcessKeyboardEvent()

        public override bool ProcessGamePadEvent(GamePadInput pad)
        {
            Debug.Assert(Active);

            if (focusButton != null)
            {
                if (pad.ButtonY.WasPressedOrRepeat)
                {
                    if (helpButton != null)
                    {
                        helpButton.OnButtonSelect();
                        return true;
                    }
                }
            }

            return base.ProcessGamePadEvent(pad);
        }   // end of ProcessGamePadEvent()

        #endregion

        #region Internal

        void OnHelp(BaseWidget w)
        {
            TextDialog helpDialog = SharedX.TextDialog;

            Debug.Assert(helpDialog.Active == false);

            helpDialog.TitleId = "mainMenu.help";
            helpDialog.BodyText = TweakScreenHelp.GetHelp(helpId);
            DialogManagerX.ShowDialog(helpDialog);
        }   // end of OnHelp()

        #endregion

    }   // end of class GraphicRadioButtonSetLabelHelp

}   // end of namespace KoiX.UI
