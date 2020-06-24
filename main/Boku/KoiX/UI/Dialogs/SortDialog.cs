
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

using KoiX;
using KoiX.Input;
using KoiX.Managers;
using KoiX.Text;
using KoiX.UI;

using BokuShared;

namespace KoiX.UI.Dialogs
{
    public class SortDialog : BaseDialogWithTitle
    {
        #region Members

        SystemFont font;

        RadioButtonLabelHelp dateButton;
        RadioButtonLabelHelp creatorButton;
        RadioButtonLabelHelp titleButton;

        List<RadioButton> buttons;

        SortBy sortBy = SortBy.Date;

        bool cancelled = true;  // Set when we leave the dialog via cancelling.

        #endregion

        #region Accessors

        public SortBy SortBy
        {
            get { return sortBy; }
        }

        /// <summary>
        /// When the dialog closed, was it because the user cancelled?
        /// </summary>
        public bool Cancelled
        {
            get { return cancelled; }
        }


        #endregion

        #region Public

        public SortDialog(RectangleF rect, string titleId, ThemeSet theme = null)
            : base(rect, titleId, theme: theme)
        {
#if DEBUG
            _name = "SortDialog";
#endif

            // Don't want backdrop for this menu.
            BackdropColor = Color.Transparent;

            font = new SystemFont(theme.TextFontFamily, theme.TextBaseFontSize * 1.2f, System.Drawing.FontStyle.Regular);
            FontWrapper wrapper = new FontWrapper(null, font);
            GetFont Font = delegate() { return wrapper; };

            buttons = new List<RadioButton>();

            int margin = 8;

            dateButton = new RadioButtonLabelHelp(this, Font, null, rect.Width - 9 * margin, buttons, labelId: "loadLevelMenu.sortDate", OnChange: OnDate, theme: theme);
            dateButton.Selected = true;
            creatorButton = new RadioButtonLabelHelp(this, Font, null, rect.Width - 9 * margin, buttons, labelId: "loadLevelMenu.sortCreator", OnChange: OnCreator, theme: theme);
            titleButton = new RadioButtonLabelHelp(this, Font, null, rect.Width - 9 * margin, buttons, labelId: "loadLevelMenu.sortTitle", OnChange: OnTitle, theme: theme);

            bodySet.Orientation = Orientation.Vertical;
            bodySet.VerticalJustification = Justification.Top;
            bodySet.HorizontalJustification = Justification.Left;
            bodySet.Padding = new Padding(4 * margin, 0, 0, 0);

            bodySet.AddWidget(dateButton);
            bodySet.AddWidget(creatorButton);
            bodySet.AddWidget(titleButton);

            // Call Recalc for force all the button positions and sizes to be calculated.
            // We need this in order to properly calc the links.
            Dirty = true;
            Recalc();

            // Connect navigation links.
            CreateTabList();

            // Dpad nav includes all widgets.
            CreateDPadLinks();

        }   // end of c'tor

        void OnCancel(BaseWidget w)
        {
            cancelled = true;
            DialogManagerX.KillDialog(this);
        }

        void OnDate(BaseWidget w)
        {
            sortBy = SortBy.Date;
            cancelled = false;
            // Need to check is active here since we might 
            // just be setting the state during Activate.
            if (Active)
            {
                DialogManagerX.KillDialog(this);
            }
        }   // end of OnDate()

        void OnCreator(BaseWidget w)
        {
            sortBy = SortBy.Creator;
            cancelled = false;
            // Need to check is active here since we might 
            // just be setting the state during Activate.
            if (Active)
            {
                DialogManagerX.KillDialog(this);
            }
        }   // end of OnCreator()

        void OnTitle(BaseWidget w)
        {
            sortBy = SortBy.Name;
            cancelled = false;
            // Need to check is active here since we might 
            // just be setting the state during Activate.
            if (Active)
            {
                DialogManagerX.KillDialog(this);
            }
        }   // end of OnTitle()

        public override void Activate(params object[] args)
        {
            Debug.Assert(!Active, "Why are we activating something that's already active?");

            // Update selection and focus based on current setting.
            // Note we do this after base activation so that the buttons are active.
            switch (sortBy)
            {
                case BokuShared.SortBy.Date:
                    dateButton.Selected = true;
                    dateButton.SetFocus(overrrideInactive: true);
                    break;
                case BokuShared.SortBy.Creator:
                    creatorButton.Selected = true;
                    creatorButton.SetFocus(overrrideInactive: true);
                    break;
                case BokuShared.SortBy.Name:
                    titleButton.Selected = true;
                    titleButton.SetFocus(overrrideInactive: true);
                    break;
            }

            cancelled = true;

            base.Activate(args);

        }   // end of Activate()

        public override void Deactivate()
        {
            base.Deactivate();
        }

        public override void RegisterForInputEvents()
        {
            // Call base register first.  By putting the child widgets on the input stacks
            // first we can then put oursleves on and have priority.
            base.RegisterForInputEvents();

            // Allow dialog to be cancelled with taps or clicks outside of its rect.
            KoiLibrary.InputEventManager.RegisterForEvent(this, InputEventManager.Event.MouseLeftDown);
            KoiLibrary.InputEventManager.RegisterForEvent(this, InputEventManager.Event.Tap);

        }   // end of RegisterForInputEvents()

        public override bool ProcessMouseLeftDownEvent(MouseInput input)
        {
            Debug.Assert(Active);

            // If user clicks outside of flyout, treat that as a cancel.
            if (!Rectangle.Contains(input.Position))
            {
                cancelled = true;
                DialogManagerX.KillDialog(this);
                return true;
            }

            return base.ProcessMouseLeftDownEvent(input);
        }   // end of ProcessMouseLeftDownEvent()

        public override bool ProcessTouchTapEvent(TapGestureEventArgs gesture)
        {
            Debug.Assert(Active);

            // If user taps outside of flyout, treat that as a cancel.
            if (!Rectangle.Contains(gesture.Position))
            {
                cancelled = true;
                DialogManagerX.KillDialog(this);
                return true;
            }

            return base.ProcessTouchTapEvent(gesture);
        }   // end of ProcessTouchTapEvent()

        public override bool ProcessKeyboardEvent(KeyInput input)
        {
            Debug.Assert(Active);

            switch (input.Key)
            {
                // Allow ESC to close dialog.
                case Keys.Escape:
                    DialogManagerX.KillDialog(this);
                    return true;
            }

            return base.ProcessKeyboardEvent(input);
        }   // end of ProcessKeyboardEvent()

        #endregion

        #region Internal
        #endregion

    }   // end of class SortDialog

}   // end of namespace KoiX.UI.Dialogs
