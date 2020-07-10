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

using Boku.Common;

namespace KoiX.UI
{
    /// <summary>
    /// A widget combo container that holds a Slider, a Label, and the Help Button.
    /// The Help Button is optional and only appears if there is help text to display.
    /// 
    /// Note that the Label is actualy implemented by a TextBox so that it may be multi-line.
    /// </summary>
    public class SliderLabelHelp : WidgetSet
    {
        #region Members

        Slider slider;
        TextBox label;
        HelpButton helpButton;
        Vector2 curValuePosition;

        GetFont Font;

        string helpId;  // Id for help text associated with this combo.

        // Width of entire container.
        float width;

        bool renderInFocus = true;  // Highlight entire set when in focus.

        #endregion

        #region Accessors

        /// <summary>
        /// Current value of the slider.
        /// </summary>
        public float CurValue
        {
            get { return slider.CurValue; }
            set { slider.CurValue = value; }
        }

        /// <summary>
        /// Target value of the slider.
        /// </summary>
        public float TargetValue
        {
            get { return slider.TargetValue; }
            set { slider.TargetValue = value; }
        }

        /// <summary>
        /// Render a highlight over the entire set when in focus.
        /// True by default.
        /// </summary>
        public bool RenderInFocus
        {
            get { return renderInFocus; }
            set { renderInFocus = value; }
        }

        #endregion

        #region Public

        public SliderLabelHelp(BaseDialog parentDialog, GetFont Font, string labelId, string helpId, float width, float minValue, float maxValue, float increment, int numDecimals, float curValue, Callback OnChange = null, ThemeSet theme = null)
            : base(parentDialog, RectangleF.EmptyRect, orientation: Orientation.None, horizontalJustification: Justification.Full, verticalJustification: Justification.Top)
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
            this.Font = Font;

            Debug.Assert(!string.IsNullOrEmpty(labelId), "Can't have an empty label.");

            RectangleF rect = new RectangleF(new Vector2(margin, 0), new Vector2(width - 2 * margin, theme.BaseCornerRadius));
            slider = new Slider(parentDialog, rect, minValue, maxValue, increment, numDecimals, OnChange: OnChange, theme: theme);

            if (helpId != null)
            {
                helpButton = new HelpButton(parentDialog, OnHelp, data: data);
            }

            // Calc max width for slider's printed value.  We do this by setting the slider to 
            // it's min and max values and measuring the max width of the resulting strings.
            float maxValueWidth = 0;
            slider.TargetValue = minValue;
            maxValueWidth = Font().MeasureString(slider.CurrentValueString).X;
            slider.TargetValue = maxValue;
            maxValueWidth = Math.Max(maxValueWidth, Font().MeasureString(slider.CurrentValueString).X);
            slider.TargetValue = curValue;

            label = new TextBox(parentDialog, Font, theme.DarkTextColor, textId: labelId);
            // Calc width of label.
            float textWidth = width - margin - maxValueWidth;
            textWidth -= helpId != null ? helpButton.LocalRect.Width : 0;
            textWidth -= 3 * margin;
            label.Width = (int)textWidth;

            localRect.Width = width;
            localRect.Height = label.NumLines * label.TotalSpacing;

            AddWidget(slider);
            AddWidget(label);
            if (helpId != null)
            {
                AddWidget(helpButton);
            }

            // Set fixed positions.  Since we're using Orientation.None, no layout gets applied.
            // This also means that Margin and Padding are ignored.
            // Put label all the way on the left edge.
            label.Position = new Vector2(margin, 0);
            // Put slider directly under the label.
            slider.Position = new Vector2(margin, label.NumLines * label.TotalSpacing + margin);

            // Where to put the current value.  Will move if we have a help button.
            curValuePosition = new Vector2(width - margin - maxValueWidth, 0);
            
            // Right justify help.
            if (helpId != null)
            {
                helpButton.Position = new Vector2(width - helpButton.LocalRect.Width - margin, 0);
                // Where to put the current value.
                curValuePosition = new Vector2(helpButton.Position.X - margin - maxValueWidth, 0);
            }

            // Expand WidgetSet to encompass slider.
            localRect.Height = slider.LocalRect.Bottom + margin;

        }   // end of c'tor

        public override void Update(SpriteCamera camera, Vector2 parentPosition)
        {
            base.Update(camera, parentPosition);
        }   // end of Update()

        public override void Render(SpriteCamera camera, Vector2 parentPosition)
        {
            // We look for Hover here since it requires that no buttons are currently pressed.
            // The reason for this has to do with not changing focus while manipulating
            // another widget.  For instance, if while moving a slider (right button pressed)
            // you drag the mouse down to the next widget, the original one should stay in
            // focus until the button is released.
            if (Hover && KoiLibrary.LastTouchedDeviceIsMouse)
            {
                slider.SetFocus();
            }

            // If in focus, render a box underneath to indicate this.
            if (slider.InFocus && RenderInFocus)
            {
                // Create rect which covers all elements.
                RectangleF rect = RectangleF.Union(slider.LocalRect, label.LocalRect);
                if (helpButton != null)
                {
                    rect = RectangleF.Union(rect, helpButton.LocalRect);
                }
                int margin = 8;
                rect.Position += parentPosition + Position - new Vector2(margin, 0);
                rect.Width += 2 * margin;
                rect.Height += 8;
                rect.Inflate(2);
                Geometry.RoundedRect.Render(camera, rect, theme.ButtonCornerRadius, theme.FocusColor);
            }

            TextHelper.DrawStringNoBatch(camera, Font, slider.CurrentValueString, parentPosition + Position + curValuePosition, theme.DarkTextColor);

            base.Render(camera, parentPosition);
        }   // end of Render()

        public override void RegisterForInputEvents()
        {
            // Call base register first.  By putting the child widgets on the input stacks
            // first we can then put oursleves on and have priority.
            base.RegisterForInputEvents();

            // Need these events to trigger help.
            KoiLibrary.InputEventManager.RegisterForEvent(this, InputEventManager.Event.Keyboard);
            KoiLibrary.InputEventManager.RegisterForEvent(this, InputEventManager.Event.GamePad);

            // Focus.
            KoiLibrary.InputEventManager.RegisterForEvent(this, InputEventManager.Event.Touch);
        }   // end of RegisterForInputEvents()

        public override void SetOnChange(Callback onChange)
        {
            slider.SetOnChange(onChange);
        }

        #endregion

        #region InputEventHandler

        public override bool ProcessKeyboardEvent(KeyInput input)
        {
            Debug.Assert(Active);

            if (slider.InFocus)
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

            if (slider.InFocus)
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

        public override bool ProcessTouchEvent(List<TouchSample> touchSampleList)
        {
            Debug.Assert(Active);

            // Is first TouchSample state Pressed?
            if (touchSampleList.Count > 0 && touchSampleList[0].State == TouchLocationState.Pressed)
            {
                if (KoiLibrary.InputEventManager.TouchHitObject != null 
                    && (KoiLibrary.InputEventManager.TouchHitObject == slider || KoiLibrary.InputEventManager.TouchHitObject == label || KoiLibrary.InputEventManager.TouchHitObject == helpButton))
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

    }   // end of class SliderLabelHelp

}   // end of namespace KoiX.UI
