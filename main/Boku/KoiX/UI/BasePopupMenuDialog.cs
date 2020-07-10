// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.


using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

using KoiX;
using KoiX.Input;
using KoiX.Managers;
using KoiX.Text;
using KoiX.UI;

namespace KoiX.UI.Dialogs
{
    /// <summary>
    /// Base class for popup menu dialogs.
    /// </summary>
    public class BasePopupMenuDialog : BaseDialog
    {
        #region Members

        protected WidgetSet set;

        #endregion

        #region Accessors
        #endregion

        #region Public

        public BasePopupMenuDialog()
        {
            //
            // Clone the current theme and modify for these buttons.
            //
            {
                theme = Theme.CurrentThemeSet.Clone() as ThemeSet;

                // Change so that focus has bright green body.
                theme.ButtonNormalFocused.BodyColor = ThemeSet.FocusColor;
                theme.ButtonNormalFocusedHover.BodyColor = ThemeSet.FocusColor;
                theme.ButtonSelectedFocused.BodyColor = ThemeSet.FocusColor;
                theme.ButtonSelectedFocusedHover.BodyColor = ThemeSet.FocusColor;

                theme.ButtonNormal.OutlineWidth = 0;
                theme.ButtonNormalFocused.OutlineWidth = 0;
                theme.ButtonNormalFocusedHover.OutlineWidth = 0;
                theme.ButtonSelectedFocused.OutlineWidth = 0;
                theme.ButtonSelectedFocusedHover.OutlineWidth = 0;

                // Keep the text focus color white.  Otherwise the text turns green on focus.
                theme.ButtonNormalFocused.TextColor = ThemeSet.LightTextColor;
                theme.ButtonNormalFocusedHover.TextColor = ThemeSet.LightTextColor;
                theme.ButtonSelectedFocused.TextColor = ThemeSet.LightTextColor;
                theme.ButtonSelectedFocusedHover.TextColor = ThemeSet.LightTextColor;

            }

            // Should this be part of the theme?
            BackdropColor = Color.Transparent;
            RenderBaseTile = false;

            // Create set to hold buttons.
            set = new WidgetSet(this, RectangleF.EmptyRect, Orientation.Vertical, Justification.Left, Justification.Top);
            AddWidget(set);

        }   // end of c'tor

        public override void Update(SpriteCamera camera)
        {
            // Note this will not be active on the first call when shown by DialogManager.
            if (Active)
            {
                // If any button is in hover state, make that the in-focus button.
                foreach (Button b in set.Widgets)
                {
                    if (b.Hover)
                    {
                        b.SetFocus();
                    }
                }
            }

            base.Update(camera);
        }   // end of Update()

        public override void Activate(params object[] args)
        {
            // Focus on first button.
            if (set.Widgets.Count > 0)
            {
                set.Widgets[0].SetFocus(overrideInactive: true);
            }

            base.Activate(args);
        }   // end of Activate()

        #endregion

        #region InputEventHandler

        public override void RegisterForInputEvents()
        {
            Debug.Print("BasePopupMenuDialog RegisterForInputEvents");

            // Call base.Register first.  This ensures that the below registrations
            // end up in the same input set since a new set is pushed during the
            // base call.
            base.RegisterForInputEvents();

            // Events used to cancel this dialog.
            KoiLibrary.InputEventManager.RegisterForEvent(this, InputEventManager.Event.MouseLeftDown);
            KoiLibrary.InputEventManager.RegisterForEvent(this, InputEventManager.Event.MouseRightDown);
            KoiLibrary.InputEventManager.RegisterForEvent(this, InputEventManager.Event.Keyboard);
            KoiLibrary.InputEventManager.RegisterForEvent(this, InputEventManager.Event.GamePad);
            KoiLibrary.InputEventManager.RegisterForEvent(this, InputEventManager.Event.Tap);
        }   // end of RegisterForInputEvents()

        public override bool ProcessMouseLeftDownEvent(MouseInput input)
        {
            Debug.Assert(Active);

            if (KoiLibrary.InputEventManager.MouseFocusObject == null)
            {
                DialogManagerX.KillDialog(this);
                return true;
            }

            return base.ProcessMouseLeftDownEvent(input);
        }   // end of ProcessMouseLeftDownEvent()

        public override bool ProcessMouseRightDownEvent(MouseInput input)
        {
            Debug.Assert(Active);

            if (KoiLibrary.InputEventManager.MouseFocusObject == null)
            {
                DialogManagerX.KillDialog(this);
                return true;
            }

            return base.ProcessMouseRightDownEvent(input);
        }   // end of ProcessMouseRightDownEvent()

        public override bool ProcessKeyboardEvent(KeyInput input)
        {
            Debug.Assert(Active);

            // Exit out of menu.
            if (input.Key == Keys.Escape)
            {
                DialogManagerX.KillDialog(this);
                return true;
            }

            // Accept in-focus element.
            if (input.Key == Keys.Enter)
            {
                foreach (Button b in set.Widgets)
                {
                    if (b.InFocus)
                    {
                        DialogManagerX.KillDialog(this);
                        b.OnButtonSelect();
                        return true;
                    }
                }
            }

            // Use up/down arrow keys to cycle focus through buttons.
            if (input.Key == Keys.Up)
            {
                int i = (GetFocusIndex() + set.Widgets.Count - 1) % set.Widgets.Count;
                set.Widgets[i].SetFocus();
                return true;
            }
            if (input.Key == Keys.Down)
            {
                int i = (GetFocusIndex() + 1) % set.Widgets.Count;
                set.Widgets[i].SetFocus();
                return true;
            }

            return base.ProcessKeyboardEvent(input);
        }   // end of ProcessKeyboardEvent()

        public override bool ProcessGamePadEvent(GamePadInput pad)
        {
            Debug.Assert(Active);

            if (pad.ButtonB.WasPressed || pad.Back.WasPressed)
            {
                DialogManagerX.KillDialog(this);
                return true;
            }

            return base.ProcessGamePadEvent(pad);
        }   // end of ProcessGamePadEvent()

        public override bool ProcessTouchTapEvent(TapGestureEventArgs gesture)
        {
            DialogManagerX.KillDialog(this);
            return true;
        }   // end of ProcessTouchTapEvent()

        #endregion

        #region Iternal

        /// <summary>
        /// Returns the index of hte in-focus button.
        /// </summary>
        /// <returns></returns>
        int GetFocusIndex()
        {
            int index = -1;

            for (int i = 0; i < set.Widgets.Count; i++)
            {
                if (set.Widgets[i].InFocus)
                {
                    index = i;
                    break;
                }
            }

            Debug.Assert(index != -1, "Should we always have a valid focus button?");

            return index;
        }   // end of GetFocusIndex()

        #endregion

    }   // end of class BasePopupMenuDialog

}   // end of namespace KoiX.UI.Dialogs
