
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
    /// A widget combo container that holds a button, a Label, and the Help Button.
    /// The Help Button is optional and only appears if there is help text to display.
    /// 
    /// Note that the Label is actualy implemented by a TextBox so that it may be multi-line.
    /// </summary>
    public class ButtonLabelHelp : WidgetSet
    {
        #region Members

        Button button;
        TextBox label;
        HelpButton helpButton;

        string helpId;  // Id for help text associated with this combo.

        // Width of entire container.
        float width;

        #endregion

        #region Accessors

        public TextBox Label
        {
            get { return label; }
        }

        #endregion

        #region Public

        public ButtonLabelHelp(BaseDialog parentDialog, GetFont Font, string labelId, string helpId, float width, Callback OnChange = null, ThemeSet theme = null, int indent = 0)
            : base(parentDialog, RectangleF.EmptyRect, orientation: Orientation.None, horizontalJustification: Justification.Full, verticalJustification: Justification.Top)
        {
            TreatAsSingleWidgetForNavigation = true;

            int margin = 8;

            if (theme == null)
            {
                theme = Theme.CurrentThemeSet.Clone() as ThemeSet;
            }

            // Modify Button settings to support contrasting color for outline
            // when button is in focus and surrounded by FocusColor.
            theme.ButtonNormalFocused.OutlineColor = theme.DarkTextColor;
            theme.ButtonSelectedFocused.OutlineColor = theme.DarkTextColor;
            theme.ButtonNormalFocusedHover.OutlineColor = theme.DarkTextColor;
            theme.ButtonSelectedFocusedHover.OutlineColor = theme.DarkTextColor;

            theme.ButtonDisabled.BevelStyle = BevelStyle.Round;
            theme.ButtonNormal.BevelStyle = BevelStyle.Round;
            theme.ButtonNormalFocused.BevelStyle = BevelStyle.Round;
            theme.ButtonNormalHover.BevelStyle = BevelStyle.Round;
            theme.ButtonNormalFocusedHover.BevelStyle = BevelStyle.Round;
            theme.ButtonSelected.BevelStyle = BevelStyle.Round;
            theme.ButtonSelectedFocused.BevelStyle = BevelStyle.Round;
            theme.ButtonSelectedHover.BevelStyle = BevelStyle.Round;
            theme.ButtonSelectedFocusedHover.BevelStyle = BevelStyle.Round;

            // Add a little vertical spacing between elements.
            Margin = new Padding(0, margin, 0, margin);

            this.width = width;
            this.helpId = helpId;

            Debug.Assert(!string.IsNullOrEmpty(labelId), "Can't have an empty label.");

            // Match the sizing of the checkboxes.
            RectangleF rect = new RectangleF(0, 0, 2 * theme.CheckBoxNormal.DefaultSize.X, theme.CheckBoxNormal.DefaultSize.Y);
            button = new Button(parentDialog, rect, element: GamePadInput.Element.AButton, OnChange: OnChange, theme: theme);

            if (helpId != null)
            {
                helpButton = new HelpButton(parentDialog, OnHelp, data: data);
            }

            label = new TextBox(parentDialog, Font, theme.DarkTextColor, textId: labelId);
            // Calc width of label.
            float textWidth = width - button.LocalRect.Width;
            textWidth -= helpId != null ? helpButton.LocalRect.Width : 0;
            textWidth -= 3 * margin;
            label.Width = (int)textWidth;

            localRect.Width = width;
            localRect.Height = label.NumLines * label.TotalSpacing;

            AddWidget(button);
            AddWidget(label);
            if (helpButton != null)
            {
                AddWidget(helpButton);
            }

            // Set fixed positions.  Since we're using Orientation.None, no layout gets applied.
            // This also means that Margin and Padding are ignored.
            button.Position = new Vector2(margin + indent, margin);
            // Put label next to Button.
            label.Position = new Vector2(button.LocalRect.Width + 2 * margin + margin + indent, 0);
            // Right justify help.
            if (helpButton != null)
            {
                helpButton.Position = new Vector2(width - helpButton.LocalRect.Width - margin, 0);
            }

        }   // end of c'tor

        public override void Render(SpriteCamera camera, Vector2 parentPosition)
        {
            if (Active)
            {
                // We look for Hover here since it requires that no buttons are currently pressed.
                // The reason for this has to do with not changing focus while manipulating
                // another widget.  For instance, if while moving a slider (right button pressed)
                // you drag the mouse down to the next widget, the original one should stay in
                // focus until the button is released.
                if (Hover && KoiLibrary.LastTouchedDeviceIsMouse)
                {
                    button.SetFocus();
                }
            }

            // If in focus, render a box underneath to indicate this.
            if (button.InFocus && Alpha > 0)
            {
                RectangleF rect = LocalRect;
                rect.Position += parentPosition;
                rect.Inflate(2);
                Geometry.RoundedRect.Render(camera, rect, theme.ButtonCornerRadius, theme.FocusColor * Alpha);
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
            KoiLibrary.InputEventManager.RegisterForEvent(this, InputEventManager.Event.MouseLeftDown);
            KoiLibrary.InputEventManager.RegisterForEvent(this, InputEventManager.Event.GamePad);
            KoiLibrary.InputEventManager.RegisterForEvent(this, InputEventManager.Event.Tap);

            // Focus.
            KoiLibrary.InputEventManager.RegisterForEvent(this, InputEventManager.Event.Touch);
        }   // end of RegisterForInputEvents()

        public override void SetOnChange(Callback onChange)
        {
            button.SetOnChange(onChange);
        }

        #endregion

        #region InputEventHandler

        public override bool ProcessKeyboardEvent(KeyInput input)
        {
            Debug.Assert(Active);

            if (button.InFocus)
            {
                switch (input.Key)
                {
                    case Keys.Enter:
                        if (button != null)
                        {
                            button.OnButtonSelect();
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
                    button.OnButtonSelect();

                    return true;
                }
            }

            return false;
        }   // end of ProcessMouseLeftDownEvent()

        public override bool ProcessGamePadEvent(GamePadInput pad)
        {
            Debug.Assert(Active);

            if (button.InFocus)
            {
                if (pad.ButtonY.WasPressedOrRepeat)
                {
                    if(helpButton != null)
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
                if (button != null)
                {
                    button.SetFocus();
                    if (hitObject != helpButton)
                    {
                        button.OnButtonSelect();
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
                    && (KoiLibrary.InputEventManager.TouchHitObject == button || KoiLibrary.InputEventManager.TouchHitObject == label || KoiLibrary.InputEventManager.TouchHitObject == helpButton))
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

    }   // end of class ButtonLabelHelp

}   // end of namespace KoiX.UI
