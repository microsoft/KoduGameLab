
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

using Boku.Base;
using Boku.Common;

using BokuShared;


namespace KoiX.UI.Dialogs
{
    public class ScoreDisplayDialog : BaseDialogWithTitle
    {
        // TODO (scoy)  This is obviously the same as the SortDialog from the LoadLevelMenu.
        // Should these be generic and reusable.

        #region Members

        SystemFont font;

        RadioButtonLabelHelp loudButton;
        RadioButtonLabelHelp quietButton;
        RadioButtonLabelHelp offButton;

        List<RadioButton> buttons;

        ScoreVisibility visibility = ScoreVisibility.Loud;

        Classification.Colors classColor = Classification.Colors.NotApplicable;

        bool cancelled = true;  // Set when we leave the dialog via cancelling.

        #endregion

        #region Accessors

        public ScoreVisibility Visibility
        {
            get { return visibility; }
            set { visibility = value; }
        }

        /// <summary>
        /// When the dialog closed, was it because the user cancelled?
        /// </summary>
        public bool Cancelled
        {
            get { return cancelled; }
        }

        /// <summary>
        /// Needs to be set before activating.  This color is then used to 
        /// set the user choice on the actual colors.
        /// TODO (scoy) Should this be done via ShowDialog params?
        /// </summary>
        public Classification.Colors ClassificationColor
        {
            set { classColor = value; }
        }

        #endregion

        #region Public

        public ScoreDisplayDialog(RectangleF rect, string titleId, ThemeSet theme = null)
            : base(rect, titleId, theme: theme)
        {
#if DEBUG
            _name = "ScoreDisplayDialog";
#endif

            // Don't want backdrop for this menu.
            BackdropColor = Color.Transparent;

            font = new SystemFont(theme.TextFontFamily, theme.TextBaseFontSize * 1.2f, System.Drawing.FontStyle.Regular);
            FontWrapper wrapper = new FontWrapper(null, font);
            GetFont Font = delegate() { return wrapper; };

            buttons = new List<RadioButton>();

            int margin = 8;

            loudButton = new RadioButtonLabelHelp(this, Font, null, rect.Width - 9 * margin, buttons, labelId: "editWorldParams.loud", OnChange: OnLoud, theme: theme);
            loudButton.Selected = true;
            quietButton = new RadioButtonLabelHelp(this, Font, null, rect.Width - 9 * margin, buttons, labelId: "editWorldParams.quiet", OnChange: OnQuiet, theme: theme);
            offButton = new RadioButtonLabelHelp(this, Font, null, rect.Width - 9 * margin, buttons, labelId: "editWorldParams.off", OnChange: OnOff, theme: theme);

            bodySet.Orientation = Orientation.Vertical;
            bodySet.VerticalJustification = Justification.Top;
            bodySet.HorizontalJustification = Justification.Left;
            bodySet.Padding = new Padding(4 * margin, 0, 0, 0);

            bodySet.AddWidget(loudButton);
            bodySet.AddWidget(quietButton);
            bodySet.AddWidget(offButton);

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

        void OnLoud(BaseWidget w)
        {
            visibility = ScoreVisibility.Loud;
            cancelled = false;

            Scoreboard.Score scoreObj = Scoreboard.GetScoreboardScore(classColor);
            if (scoreObj != null)
            {
                scoreObj.Visibility = ScoreVisibility.Loud;
            }

            // Need to check is active here since we might 
            // just be setting the state during Activate.
            if (Active)
            {
                DialogManagerX.KillDialog(this);
            }
        }   // end of OnLoud()

        void OnQuiet(BaseWidget w)
        {
            visibility = ScoreVisibility.Quiet;
            cancelled = false;

            Scoreboard.Score scoreObj = Scoreboard.GetScoreboardScore(classColor);
            if (scoreObj != null)
            {
                scoreObj.Visibility = ScoreVisibility.Quiet;
            }
            
            // Need to check is active here since we might 
            // just be setting the state during Activate.
            if (Active)
            {
                DialogManagerX.KillDialog(this);
            }
        }   // end of OnQuiet()

        /// <summary>
        /// I just love it when a naming convention comes together.
        /// </summary>
        /// <param name="w"></param>
        void OnOff(BaseWidget w)
        {
            visibility = ScoreVisibility.Off;
            cancelled = false;

            Scoreboard.Score scoreObj = Scoreboard.GetScoreboardScore(classColor);
            if (scoreObj != null)
            {
                scoreObj.Visibility = ScoreVisibility.Off;
            }

            // Need to check is active here since we might 
            // just be setting the state during Activate.
            if (Active)
            {
                DialogManagerX.KillDialog(this);
            }
        }   // end of OnOff()

        public override void Activate(params object[] args)
        {
            Debug.Assert(!Active, "Why are we activating something that's already active?");

            Debug.Assert(classColor != Classification.Colors.None);
            Debug.Assert(classColor != Classification.Colors.NotApplicable);

            Scoreboard.Score scoreObj = Scoreboard.GetScoreboardScore(classColor);
            if (scoreObj != null)
            {
                visibility = scoreObj.Visibility;
            }

#if DEBUG
            _name = "ScoreDisplayDialog " + classColor.ToString();
#endif

            // Update selection and focus based on current setting.
            // Note we do this after base activation so that the buttons are active.
            switch (visibility)
            {
                case ScoreVisibility.Loud:
                    loudButton.Selected = true;
                    loudButton.SetFocus(overrrideInactive: true);
                    break;
                case ScoreVisibility.Quiet:
                    quietButton.Selected = true;
                    quietButton.SetFocus(overrrideInactive: true);
                    break;
                case ScoreVisibility.Off:
                    offButton.Selected = true;
                    offButton.SetFocus(overrrideInactive: true);
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

    }   // end of class ScoreDisplayDialog

}   // end of namespace KoiX.UI.Dialogs
