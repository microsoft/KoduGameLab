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


namespace KoiX.UI
{
    /// <summary>
    /// Simple checkbox.  No label.
    /// </summary>
    public class CheckBox : BaseWidget
    {
        #region Members

        UIState prevCombinedState = UIState.Inactive;

        CheckBoxTheme curTheme;     // Colors and sizes for current state.

        Twitchable<Color> bodyColor;
        Twitchable<Color> outlineColor;
        Twitchable<float> outlineWidth;
        Twitchable<float> cornerRadius;

        #endregion

        #region Accessors

        public bool Checked
        {
            get { return Selected; }
            set 
            {
                if (Selected != value)
                {
                    Selected = value;
                    OnChange();
                }
            }
        }

        public Color BodyColor
        {
            get { return bodyColor.Value; }
            set { bodyColor.Value = value; }
        }

        public Color _BodyColor
        {
            get { return bodyColor.TargetValue; }
            set { bodyColor.TargetValue = value; }
        }

        public Color OutlineColor
        {
            get { return outlineColor.Value; }
            set { outlineColor.Value = value; }
        }

        public Color _OutlineColor
        {
            get { return outlineColor.TargetValue; }
            set { outlineColor.TargetValue = value; }
        }

        public float OutlineWidth
        {
            get { return outlineWidth.Value; }
            set { outlineWidth.Value = value; }
        }

        public float _OutlineWidth
        {
            get { return outlineWidth.TargetValue; }
            set { outlineWidth.TargetValue = value; }
        }

        public float CornerRadius
        {
            get { return cornerRadius.Value; }
            set { cornerRadius.Value = value; }
        }

        public float _CornerRadius
        {
            get { return cornerRadius.TargetValue; }
            set { cornerRadius.TargetValue = value; }
        }

        #endregion

        #region Public

        public CheckBox(BaseDialog parentDialog, Callback OnChange = null, ThemeSet theme = null, string id = null, object data = null)
            : base(parentDialog, OnChange: OnChange, theme: theme, id: id, data: data)
        {
            if (theme == null)
            {
                theme = Theme.CurrentThemeSet;
            }

            curTheme = theme.CheckBoxNormal;

            // Create all the Twitchables and set initial values.
            bodyColor = new Twitchable<Color>(Theme.TwitchTime, TwitchCurve.Shape.EaseInOut, parent: this, startingValue: curTheme.BodyColor);
            outlineColor = new Twitchable<Color>(Theme.TwitchTime, TwitchCurve.Shape.EaseInOut, parent: this, startingValue: curTheme.OutlineColor);
            outlineWidth = new Twitchable<float>(Theme.TwitchTime, TwitchCurve.Shape.EaseInOut, parent: this, startingValue: curTheme.OutlineWidth);
            cornerRadius = new Twitchable<float>(Theme.TwitchTime, TwitchCurve.Shape.EaseInOut, parent: this, startingValue: 4.0f);

            localRect.Size = curTheme.DefaultSize;
        }   // end of c'tor

        public override void Update(SpriteCamera camera, Vector2 parentPosition)
        {
            // Needed to handle focus changes.
            base.Update(camera, parentPosition);

            UIState combinedState = CombinedState;
            if (combinedState != prevCombinedState)
            {
                // Set new state params.  Note that dirty flag gets
                // set internally by setting individual values so
                // we don't need to worry about it here.
                switch (combinedState)
                {
                    case UIState.Disabled:
                    case UIState.DisabledSelected:
                        curTheme = theme.CheckBoxDisabled;
                        break;

                    case UIState.Active:
                    case UIState.ActiveHover:
                        curTheme = theme.CheckBoxNormal;
                        break;

                    case UIState.ActiveFocused:
                    case UIState.ActiveFocusedHover:
                        curTheme = theme.CheckBoxNormalFocused;
                        break;

                    case UIState.ActiveSelected:
                    case UIState.ActiveSelectedHover:
                        curTheme = theme.CheckBoxSelected;
                        break;

                    // For checkbox selected == checked.
                    case UIState.ActiveSelectedFocused:
                    case UIState.ActiveSelectedFocusedHover:
                        curTheme = theme.CheckBoxSelectedFocused;
                        break;

                    default:
                        // Should only happen on state.None
                        break;

                }   // end of switch

                // Now that we have the new theme, set all the Twitchable values from it.
                // Non-twitchable values we get directly from the theme.
                bodyColor.Value = curTheme.BodyColor;
                cornerRadius.Value = curTheme.CornerRadius;
                outlineColor.Value = curTheme.OutlineColor;
                outlineWidth.Value = curTheme.OutlineWidth;

                prevCombinedState = combinedState;

            }   // end if state changed.

            localRect.Size = curTheme.DefaultSize;

        }   // end of Update()

        public override void Render(SpriteCamera camera, Vector2 parentPosition)
        {
            Vector2 pos = Position + parentPosition;

            RoundedRect.Render(camera, pos, LocalRect.Size, cornerRadius.Value, bodyColor.Value,
                    outlineWidth: outlineWidth.Value, outlineColor: outlineColor.Value,
                    bevelStyle: BevelStyle.Round, bevelWidth: cornerRadius.Value);

            // Needed for debug rendering.
            base.Render(camera, parentPosition);

        }   // end of Render()

        public override void RegisterForInputEvents()
        {
            // Register to get left down mouse event.  
            KoiLibrary.InputEventManager.RegisterForEvent(this, InputEventManager.Event.MouseLeftDown);

            // Also register for Keyboard.  If this button has focus and enter is pressed that's the same as a mouse click.
            KoiLibrary.InputEventManager.RegisterForEvent(this, InputEventManager.Event.Keyboard);

            // Tap also toggles checked.
            KoiLibrary.InputEventManager.RegisterForEvent(this, InputEventManager.Event.Tap);

            // If we have focus, gamepad A should toggle state.
            KoiLibrary.InputEventManager.RegisterForEvent(this, InputEventManager.Event.GamePad);

        }   // end of RegisterForInputEvents()

        #endregion

        #region InputEventHandler

        public override bool ProcessMouseLeftDownEvent(MouseInput input)
        {
            Debug.Assert(Active);

            if (KoiLibrary.InputEventManager.MouseFocusObject == null)
            {
                if (KoiLibrary.InputEventManager.MouseHitObject == this)
                {
                    // Claim mouse focus as ours.
                    // Note: we could probaly skip claiming focus and also ignore LeftUp.
                    KoiLibrary.InputEventManager.MouseFocusObject = this;

                    // Register for LeftUp event.
                    KoiLibrary.InputEventManager.RegisterForEvent(this, InputEventManager.Event.MouseLeftUp);

                    // Note that checkboxes trigger on the click rather than the release.
                    // Toggle state.
                    Checked = !Checked;
                    SetFocus();

                    return true;
                }
            }

            return false;
        }   // end of ProcessMouseLeftDownEvent()

        public override bool ProcessMouseLeftUpEvent(MouseInput input)
        {
            // Release focus.
            if (KoiLibrary.InputEventManager.MouseFocusObject == this)
            {
                KoiLibrary.InputEventManager.MouseFocusObject = null;
            }

            return base.ProcessMouseLeftUpEvent(input);
        }   // ProcessMouseLeftUpEvent()

        public override bool ProcessKeyboardEvent(KeyInput input)
        {
            Debug.Assert(Active);

            if (InFocus && input.Key == Microsoft.Xna.Framework.Input.Keys.Enter && !input.Modifier)
            {
                // If inFocus, toggle state.
                if (InFocus)
                {
                    Checked = !Checked;

                    return true;
                }
            }

            return base.ProcessKeyboardEvent(input);
        }   // end of ProcessKeyboardEvent()

        public override bool ProcessTouchTapEvent(TapGestureEventArgs gesture)
        {
            Debug.Assert(Active);

            // Did this gesture hit us?
            if (gesture.HitObject == this)
            {
                Checked = !Checked;

                return true;
            }

            return base.ProcessTouchTapEvent(gesture);
        }   // end of ProcessTouchTapEvent()

        public override bool ProcessGamePadEvent(GamePadInput pad)
        {
            Debug.Assert(Active);

            if (InFocus)
            {
                if (pad.ButtonA.WasPressed && InFocus)
                {
                    Checked = !Checked;

                    return true;
                }
            }

            return base.ProcessGamePadEvent(pad);
        }
        #endregion

        #region Internal
        #endregion

    }   // end of class CheckBox

}   // end of namespace KoiX.UI
