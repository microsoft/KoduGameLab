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
    /// A widget combo container that holds a single line text input widget and a Help Button.
    /// The Help Button is optional and only appears if there is help text to display.
    /// </summary>
    public class SingleLineInputHelp : WidgetSet
    {
        #region Members

        SingleLineTextEditBox textBox;
        HelpButton helpButton;

        string helpId;  // Id for help text associated with this combo.

        // Width of entire container.
        float width;

        #endregion

        #region Accessors

        /// <summary>
        /// Get the text currently in the text box.  Returns
        /// the default text if being shown.
        /// </summary>
        public string Text
        {
            get { return textBox.CurrentText; }
            set { textBox.RawText = value; }
        }

        /// <summary>
        /// Get the text currently in the text box.  Returns
        /// empty string if default is being shown.
        /// </summary>
        public string TextNoDefault
        {
            get { return textBox.CurrentTextNoDefault; }
        }

        #endregion

        #region Public

        /// <summary>
        /// 
        /// </summary>
        /// <param name="parentDialog"></param>
        /// <param name="Font"></param>
        /// <param name="defaultTextId">Text which is shown greyed out when actual text is empty.</param>
        /// <param name="helpId"></param>
        /// <param name="width"></param>
        /// <param name="OnChange"></param>
        /// <param name="theme"></param>
        public SingleLineInputHelp(BaseDialog parentDialog, GetFont Font, string defaultTextId, string helpId, float width, Callback OnChange = null, ThemeSet theme = null)
            : base(parentDialog, RectangleF.EmptyRect, orientation: Orientation.None, horizontalJustification: Justification.Full, verticalJustification: Justification.Top)
        {
            TreatAsSingleWidgetForNavigation = true;

            int margin = 8;

            if (theme == null)
            {
                theme = Theme.CurrentThemeSet.Clone() as ThemeSet;
            }

            this.width = width;
            this.helpId = helpId;

            if (helpId != null)
            {
                helpButton = new HelpButton(parentDialog, OnHelp, data: data);
            }

            localRect.Width = width;
            localRect.Height = 40;

            // Calc width of text box, leaving room for help button.
            int boxWidth = (int)(width - (helpButton == null ? 0 : 80));
            string defaultText = defaultTextId == null ? "" : Strings.Localize(defaultTextId);
            textBox = new SingleLineTextEditBox(parentDialog, Font, boxWidth, defaultText: defaultText, prefilledText: "", OnChange: OnChange, theme: theme, maxCharacters: 30);

            localRect.Width = width;
            localRect.Height = helpButton.LocalRect.Height;

            AddWidget(textBox);
            if (helpButton != null)
            {
                AddWidget(helpButton);
            }

            // Set fixed positions.  Since we're using Orientation.None, no layout gets applied.
            // This also means that Margin and Padding are ignored.
            textBox.Position = new Vector2(0, 0);
            // Right justify help.
            if (helpButton != null)
            {
                helpButton.Position = new Vector2(width - helpButton.LocalRect.Width - margin, 0);
            }

        }   // end of c'tor

        public override void Update(SpriteCamera camera, Vector2 parentPosition)
        {


            base.Update(camera, parentPosition);
        }   // end of Update()

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
                    textBox.SetFocus();
                }
            }

            // If in focus, render a box underneath to indicate this.
            if (textBox.InFocus && Alpha > 0)
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
            KoiLibrary.InputEventManager.RegisterForEvent(this, InputEventManager.Event.Tap);

            // Focus.
            KoiLibrary.InputEventManager.RegisterForEvent(this, InputEventManager.Event.Touch);
        }   // end of RegisterForInputEvents()

        public override void SetOnChange(Callback onChange)
        {
            textBox.SetOnChange(onChange);
        }

        #endregion

        #region InputEventHandler

        public override bool ProcessKeyboardEvent(KeyInput input)
        {
            Debug.Assert(Active);

            if (textBox.InFocus)
            {
                switch (input.Key)
                {
                    /*
                    case Keys.Enter:
                        if (textBox != null)
                        {
                            textBox.OnChange();
                        }
                        return true;
                    */

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
                /*
                if (MouseOver)
                {
                    textBox.OnChange();

                    return true;
                }
                */
            }

            return false;
        }   // end of ProcessMouseLeftDownEvent()

        public override bool ProcessTouchTapEvent(TapGestureEventArgs gesture)
        {
            Debug.Assert(Active);

            BaseWidget hitObject = gesture.HitObject as BaseWidget;
            if (hitObject != null && Widgets.Contains(hitObject))
            {
                if (textBox != null)
                {
                    textBox.SetFocus();
                    if (hitObject != helpButton)
                    {
                        textBox.OnChange();
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
                    && (KoiLibrary.InputEventManager.TouchHitObject == textBox || KoiLibrary.InputEventManager.TouchHitObject == helpButton))
                {
                    textBox.SetFocus();
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

    }   // end of class SingleLineInputHelp

}   // end of namespace KoiX.UI
