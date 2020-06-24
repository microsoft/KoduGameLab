
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
    /// A widget combo container that holds a RadioButton, a Label, and the Help Button.
    /// The Help Button is optional and only appears if there is help text to display.
    /// 
    /// Note that the Label is actualy implemented by a TextBox so that it may be multi-line.
    /// </summary>
    public class RadioButtonLabelHelp : WidgetSet
    {
        #region Members

        RadioButton radioButton;
        TextBox label;
        HelpButton helpButton;

        string helpId;  // Id for help text associated with this combo.

        // Width of entire container.
        float width;

        #endregion

        #region Accessors

        public new bool Selected
        {
            get { return radioButton.Selected; }
            set
            {
                if (value != radioButton.Selected)
                {
                    radioButton.Selected = value;
                }
            }
        }

        /// <summary>
        /// For the set, return the InFocus state of the radio button.
        /// </summary>
        new public bool InFocus
        {
            get { return radioButton.InFocus; }
        }

        #endregion

        #region Public

        public RadioButtonLabelHelp(BaseDialog parentDialog, GetFont Font, string helpId, float width, List<RadioButton> siblings, string labelId = null, string labelText = null, Callback OnChange = null, ThemeSet theme = null, object data = null)
            : base(parentDialog, RectangleF.EmptyRect, orientation: Orientation.None, horizontalJustification: Justification.Full, verticalJustification: Justification.Top, data: data)
        {
            TreatAsSingleWidgetForNavigation = true;

            // Change ref to what base class has set.
            // Allows us to use theme rather than this.Theme.
            theme = this.ThemeSet;

            int margin = 8;

            // Add a little vertical spacing between elements.
            Margin = new Padding(0, margin, 0, margin);

            this.width = width;
            this.helpId = helpId;

            radioButton = new RadioButton(parentDialog, siblings, OnChange: OnChange, theme: theme, data: data);

            if (helpId != null)
            {
                helpButton = new HelpButton(parentDialog, OnHelp, data: data);
            }

            label = new TextBox(parentDialog, Font, theme.DarkTextColor, textId: labelId, displayText: labelText);
            // Calc width of label.
            float textWidth = width - radioButton.LocalRect.Width;
            textWidth -= helpId != null ? helpButton.LocalRect.Width : 0;
            textWidth -= 3 * margin;
            label.Width = (int)textWidth;

            localRect.Width = width;
            localRect.Height = label.NumLines * label.TotalSpacing;

            AddWidget(radioButton);
            AddWidget(label);
            if (helpId != null)
            {
                AddWidget(helpButton);
            }

            // Set fixed positions.  Since we're using Orientation.None, no layout gets applied.
            // This also means that Margin and Padding are ignored.
            radioButton.Position = new Vector2(margin, margin);
            // Put label next to RadioButton.
            label.Position = new Vector2(radioButton.LocalRect.Width + 2 * margin + margin, 0);
            // Right justify help.
            if (helpId != null)
            {
                helpButton.Position = new Vector2(width - helpButton.LocalRect.Width - margin, 0);
            }

        }   // end of c'tor

        public override void Render(SpriteCamera camera, Vector2 parentPosition)
        {
            // We look for Hover here since it requires that no buttons are currently pressed.
            // The reason for this has to do with not changing focus while manipulating
            // another widget.  For instance, if while moving a slider (right button pressed)
            // you drag the mouse down to the next widget, the original one should stay in
            // focus until the button is released.
            if (Hover && KoiLibrary.LastTouchedDeviceIsMouse)
            {
                radioButton.SetFocus();
            }

            // If in focus, render a box underneath to indicate this.
            if (radioButton.InFocus)
            {
                // For hightlight, start with rect over whole set.
                RectangleF rect = LocalRect;
                rect.Position += parentPosition;
                rect.Inflate(2);
                Geometry.RoundedRect.Render(camera, rect, theme.ButtonCornerRadius, theme.FocusColor);
            }

            base.Render(camera, parentPosition);
        }

        public override void RegisterForInputEvents()
        {
            // Call base register first.  By putting the child widgets on the input stacks
            // first we can then put oursleves on and have priority.
            base.RegisterForInputEvents();

            // Need to look for events in areas other than the child widgets.
            KoiLibrary.InputEventManager.RegisterForEvent(this, InputEventManager.Event.Keyboard);
            KoiLibrary.InputEventManager.RegisterForEvent(this, InputEventManager.Event.MouseLeftDown);
            KoiLibrary.InputEventManager.RegisterForEvent(this, InputEventManager.Event.GamePad);
            KoiLibrary.InputEventManager.RegisterForEvent(this, InputEventManager.Event.Tap);

            // Focus.
            KoiLibrary.InputEventManager.RegisterForEvent(this, InputEventManager.Event.Touch);
        }   // end of RegisterForInputEvents()

        public override void SetOnChange(Callback onChange)
        {
            radioButton.SetOnChange(onChange);
        }

        #endregion

        #region InputEventHandler

        public override bool ProcessKeyboardEvent(KeyInput input)
        {
            Debug.Assert(Active);

            if (radioButton.InFocus)
            {
                switch (input.Key)
                {
                    case Keys.Enter:
                        if (radioButton != null)
                        {
                            radioButton.Selected = true;
                        }
                        return true;

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

        public override bool ProcessMouseLeftDownEvent(MouseInput input)
        {
            Debug.Assert(Active);

            if (KoiLibrary.InputEventManager.MouseFocusObject == null)
            {
                if (MouseOver)
                {
                    // Claim mouse focus as ours.
                    KoiLibrary.InputEventManager.MouseFocusObject = this;

                    // Register to get left up events.
                    KoiLibrary.InputEventManager.RegisterForEvent(this, InputEventManager.Event.MouseLeftUp);

                    return true;
                }
            }

            return false;
        }   // end of ProcessMouseLeftDownEvent()

        public override bool ProcessMouseLeftUpEvent(MouseInput input)
        {
            Debug.Assert(Active);

            if (KoiLibrary.InputEventManager.MouseFocusObject == this)
            {
                // Release mouse focus.
                if (KoiLibrary.InputEventManager.MouseFocusObject == this)
                {
                    KoiLibrary.InputEventManager.MouseFocusObject = null;
                }

                // Stop getting up events.
                KoiLibrary.InputEventManager.UnregisterForEvent(this, InputEventManager.Event.MouseLeftUp);

                // If mouse is still over region, set RadioButton "on".
                if (MouseOver)
                {
                    radioButton.Selected = true;
                }

                return true;
            }
            return false;
        }   // end of ProcessMouseLeftUpEvent()

        public override bool ProcessGamePadEvent(GamePadInput pad)
        {
            Debug.Assert(Active);

            if (radioButton.InFocus)
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

        public override bool ProcessTouchTapEvent(TapGestureEventArgs gesture)
        {
            Debug.Assert(Active);

            BaseWidget hitObject = gesture.HitObject as BaseWidget;
            if (hitObject != null && Widgets.Contains(hitObject))
            {
                if (radioButton != null)
                {
                    radioButton.SetFocus();
                    if (hitObject != helpButton)
                    {
                        radioButton.Selected = true;
                        return true;
                    }
                }
            }   // end of ProcessTouchTapEvent()

            return base.ProcessTouchTapEvent(gesture);
        }   // end of ProcessTouchTapEvent()

        public override bool ProcessTouchEvent(List<TouchSample> touchSampleList)
        {
            Debug.Assert(Active);

            // Is first TouchSample state Pressed?
            if (touchSampleList.Count > 0 && touchSampleList[0].State == TouchLocationState.Pressed)
            {
                if (KoiLibrary.InputEventManager.TouchHitObject != null 
                    && (KoiLibrary.InputEventManager.TouchHitObject == radioButton || KoiLibrary.InputEventManager.TouchHitObject == label || KoiLibrary.InputEventManager.TouchHitObject == helpButton))
                {
                    SetFocus();
                    return true;
                }
            }

            return base.ProcessTouchEvent(touchSampleList);
        }   // end of ProcessTouchEvent()

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

    }   // end of class RadioButtonLabelHelp

}   // end of namespace KoiX.UI
