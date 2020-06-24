
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

using Boku.Base;
using Boku.Common;

namespace KoiX.UI
{
    /// <summary>
    /// Class for the buttons used to select an actor's color.
    /// </summary>
    public class ColorRadioButton : BaseWidget
    {
        #region Members

        float bevelWidth = 8;   // Rounded bevel on buttons.

        Classification.Colors classColor = Classification.Colors.None;

        Color color;        
        float radius;
        float focusWidth = 6.0f;    // Width of focus highlight around selected button.

        UIState prevCombinedState = UIState.Inactive;

        List<ColorRadioButton> siblings;      // List of all radio buttons in this set.  Used for clearing
                                                // selection of others when this one is set.

        RadioButtonTheme curTheme;              // Colors and sizes for current state.

        #endregion

        #region Accessors

        public new bool Selected
        {
            get { return base.Selected; }
            set
            {
                if (base.Selected != value)
                {
                    base.Selected = value;

                    // If we're setting this ColorRadioButton "on" then
                    // all its siblings need to be "off".
                    if (value == true)
                    {
                        foreach (ColorRadioButton rb in siblings)
                        {
                            if (rb != this)
                            {
                                rb.Selected = false;
                            }
                        }
                    }

                    // TODO (scoy) Should we only call OnChange when selected or
                    // should be call any time the state changes?
                    if (Selected)
                    {
                        SetFocus(overrideInactive: true);
                        OnChange();
                    }
                }   // end if value changes.

            }
        }

        public Classification.Colors ClassColor
        {
            get { return classColor; }
        }

        #endregion

        #region Public

        /// <summary>
        /// 
        /// </summary>
        /// <param name="parentDialog"></param>
        /// <param name="siblings"></param>
        /// <param name="radius"></param>
        /// <param name="classColor">Classification color for this option.</param>
        /// <param name="OnChange">Callback which is called when this button is selected.  Note if button is already selected then callback is still called.</param>
        /// <param name="theme"></param>
        /// <param name="data"></param>
        public ColorRadioButton(BaseDialog parentDialog, List<ColorRadioButton> siblings, float radius, Classification.Colors classColor, Callback OnChange = null, ThemeSet theme = null, string id = null, object data = null)
            : base(parentDialog, OnChange: OnChange, theme: theme, id: id, data: data)
        {
            this.siblings = siblings;
            this.radius = radius;
            this.classColor = classColor;
            color = Classification.XnaColor(classColor);

            // Add self.
            siblings.Add(this);

            if (theme == null)
            {
                theme = Theme.CurrentThemeSet;
            }

            // Default to Inactive state.
            prevCombinedState = UIState.Inactive;
            curTheme = theme.RadioButtonNormal.Clone() as RadioButtonTheme;

            localRect.Size = new Vector2(2.0f * radius, 2.0f * radius);
        }   // end of c'tor

        public override void Update(SpriteCamera camera, Vector2 parentPosition)
        {
            // Needed to handle focus changes.
            base.Update(camera, parentPosition);

            if (Hover)
            {
                SetFocus();
            }

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
                        curTheme = theme.RadioButtonDisabled;
                        break;

                    case UIState.Active:
                    case UIState.ActiveHover:
                        curTheme = theme.RadioButtonNormal;
                        break;

                    case UIState.ActiveFocused:
                    case UIState.ActiveFocusedHover:
                        curTheme = theme.RadioButtonNormalFocused;
                        break;

                    case UIState.ActiveSelected:
                    case UIState.ActiveSelectedHover:
                        curTheme = theme.RadioButtonSelected;
                        break;

                    // For ColorRadioButton selected == checked.
                    case UIState.ActiveSelectedFocused:
                    case UIState.ActiveSelectedFocusedHover:
                        curTheme = theme.RadioButtonSelectedFocused;
                        break;

                    default:
                        // Should only happen on state.None
                        break;

                }   // end of switch

                prevCombinedState = combinedState;

            }   // end if state changed.

        }   // end of Update()

        public override void Render(SpriteCamera camera, Vector2 parentPosition)
        {
            Vector2 pos = Position + parentPosition;
            Vector2 center = pos + new Vector2(radius, radius);

            if (InFocus)
            {
                // Foucs color highlight under full disc.
                Disc.Render(camera, center, radius + focusWidth, theme.FocusColor);
            }

            // Render normal disc.
            Disc.Render(camera, center, radius, color,
                        bevelStyle: BevelStyle.Round, bevelWidth: bevelWidth);

            // Render radio button centered at bottom of larger disc.
            pos = center + new Vector2(0, radius - curTheme.CornerRadius);
            Disc.Render(camera, pos, curTheme.CornerRadius, curTheme.BodyColor,
                        outlineWidth: curTheme.OutlineWidth, outlineColor: curTheme.OutlineColor,
                        bevelStyle: BevelStyle.Round, bevelWidth: curTheme.CornerRadius);

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

                // Stop getting move and up events.
                KoiLibrary.InputEventManager.UnregisterForEvent(this, InputEventManager.Event.MouseMove);
                KoiLibrary.InputEventManager.UnregisterForEvent(this, InputEventManager.Event.MouseLeftUp);

                // If mouse up happens over box, fine.  If not, ignore.
                if (KoiLibrary.InputEventManager.MouseHitObject == this)
                {
                    // Set to "on".
                    Selected = true;
                }

                return true;
            }
            return false;
        }   // end of ProcessMouseLeftUpEvent()

        public override bool ProcessKeyboardEvent(KeyInput input)
        {
            Debug.Assert(Active);

            if (InFocus && input.Key == Microsoft.Xna.Framework.Input.Keys.Enter && !input.Modifier)
            {
                // If inFocus, toggle state.
                if (InFocus)
                {
                    // Set to "on".
                    Selected = true;

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
                // Set to "on".
                Selected = true;

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
                    // Set to "on".
                    Selected = true;

                    return true;
                }
            }

            return base.ProcessGamePadEvent(pad);
        }
        #endregion

        #region Internal

        public override void LoadContent()
        {
            base.LoadContent();
        }   // end of LoadContent()

        public override void UnloadContent()
        {
            base.UnloadContent();
        }   // end of UnloadContent()

        #endregion


    }   // end of class ColorRadioButton
}   // end of namespace KoiX.UI
