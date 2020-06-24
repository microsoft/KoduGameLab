
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
using KoiX.Input;
using KoiX.Managers;
using KoiX.Text;
using KoiX.UI;

using Boku;
using Boku.Common;
using Boku.Common.Xml;
using Boku.UI2D;

using BokuShared;

namespace KoiX.UI.Dialogs
{
    using Keys = Microsoft.Xna.Framework.Input.Keys;

    /// <summary>
    /// AuthSignout.
    /// </summary>
    public class AuthSignOutDialog : BaseDialogWithTitle
    {
        #region Members

        Button signOutButton;
        Button cancelButton;
        int margin = 8;         // Around button text.

        TextBox keepSignedInTextBox;
        TextBox curUserTextBox;
        CheckBox keepSignedInCheckBox;

        bool prevCheckboxState; // In case user cancels we can restore prev state.

        #endregion

        #region Accessors
        #endregion

        #region Public

        public AuthSignOutDialog(RectangleF rect, string titleId, ThemeSet theme = null, Color backdropColor = default(Color))
            : base(rect, titleId, theme: theme)
        {
#if DEBUG
            _name = "AuthSignOutDialog";
#endif

            // Change ref to what base class has set.
            // Allows us to use theme rather than this.Theme.
            theme = this.ThemeSet;

            // Body
            {
                SystemFont font = SysFont.GetSystemFont(theme.TextFontFamily, theme.TextBaseFontSize, theme.TextBaseFontStyle);
                FontWrapper wrapper = new FontWrapper(null, font);
                GetFont Font = delegate() { return wrapper; };

                keepSignedInCheckBox = new CheckBox(this, theme:theme);

                keepSignedInTextBox = new TextBox(this, Font, theme.LightTextColor, textId: "auth.keepSignedIn");
                keepSignedInTextBox.Width = (int)rect.Width - 2 * 32;   // Width minus margins.

                string curUserString = Strings.Localize("auth.currentlySignedInAs") + Auth.CreatorName;
                curUserTextBox = new TextBox(this, Font, theme.LightTextColor, displayText: curUserString);
                curUserTextBox.Width = (int)rect.Width - 2 * 32 - 64;   // Widht minus margins minus checkbox space.

                bodySet.AddWidget(curUserTextBox);
                bodySet.AddWidget(keepSignedInCheckBox);
                bodySet.AddWidget(keepSignedInTextBox);

                // Set initial state to match what's stored.
                keepSignedInCheckBox.Checked = XmlOptionsData.KeepSignedInOnExit;
            }

            buttonSet.Padding = new Padding(32, 16, 32, 16);

            signOutButton = new Button(this, new RectangleF(), OnChange: OnSignOut, theme: this.theme, labelId: "auth.signOut");
            signOutButton.Size = signOutButton.CalcMinSize() + new Vector2(margin, 0);  // Match button size to label, with a bit of margin.
            signOutButton.Label.Size = signOutButton.Size;                              // Make label same size so it gets centered correctly.
            buttonSet.AddWidget(signOutButton);

            cancelButton = new Button(this, new RectangleF(), OnChange: OnCancel, theme: this.theme, labelId: "auth.cancel");
            cancelButton.Size = cancelButton.CalcMinSize() + new Vector2(margin, 0);    // Match button size to label, with a bit of margin.
            cancelButton.Label.Size = cancelButton.Size;                                // Make label same size so it gets centered correctly.
            buttonSet.AddWidget(cancelButton);

            // Call Recalc for force all the button positions and sizes to be calculated.
            // We need this in order to properly calc the links.
            Dirty = true;
            Recalc();

            // Connect navigation links.
            CreateTabList();

            // Dpad nav includes all widgets so webSiteButton will be there.
            CreateDPadLinks();

        }   // end of c'tor

        void OnSignOut(BaseWidget w)
        {
            // On sign-out we want to display the sign-in dialog.
            Auth.SetCreator(Auth.DefaultCreatorName, Auth.DefaultCreatorHash);

            DialogManagerX.KillDialog(this);
            DialogManagerX.ShowDialog(AuthUI.SignInDialog, camera);
        }   // end of OnSignOut()

        void OnCancel(BaseWidget w)
        {
            // Restore original state.
            XmlOptionsData.KeepSignedInOnExit = prevCheckboxState;
            keepSignedInCheckBox.Checked = prevCheckboxState;

            // Return to displaying status dialog.
            DialogManagerX.KillDialog(this);
            DialogManagerX.ShowDialog(AuthUI.StatusDialog, camera);
        }   // end of OnCancel()

        public override void  Recalc()
        {
 	        base.Recalc();
        }   // end of Recalc()

        public override void Update(SpriteCamera camera)
        {
            if (Active)
            {
                // Manually set position of elements in body.
                // Figure out how many lines of text we have and adjust accordingly.
                // Add 3 extra, 1 above, 1 in the middle, and 1 below.
                int numLines = curUserTextBox.NumLines + keepSignedInTextBox.NumLines + 3;
                int top = (int)((numLines * curUserTextBox.TotalSpacing) / 2.0f - 32);
                Vector2 pos = new Vector2(32, top);
                curUserTextBox.Position = pos;

                pos.Y += (curUserTextBox.NumLines + 1) * curUserTextBox.TotalSpacing;

                keepSignedInCheckBox.Position = pos;
                keepSignedInCheckBox.Render(camera, bodySet.Position);

                pos.X += 48;    // Checkbox width.
                keepSignedInTextBox.Position = pos;

                // Keep auth state in sync with checkbox.
                XmlOptionsData.KeepSignedInOnExit = keepSignedInCheckBox.Checked;

            }

            base.Update(camera);
        }   // end of Update()

        public override void Render(SpriteCamera camera)
        {
            base.Render(camera);
        }   // end of Render()

        public override void RegisterForInputEvents()
        {
            // All of the input handling is done in the button.

            base.RegisterForInputEvents();
        }

        public override void UnregisterForInputEvents()
        {
            base.UnregisterForInputEvents();
        }

        public override void Activate(params object[] args)
        {
            // Update 
            if (Auth.IsSignedIn)
            {
                // Ensure label is up to date.
                string curUserString = Strings.Localize("auth.currentlySignedInAs") + Auth.CreatorName;
                curUserTextBox.RawText = curUserString;
            }
            else
            {
                Debug.Assert(false, "If we're not signed in, why are we trying to sign out?");
                // Ensure label is up to date.
                string curUserString = Strings.Localize("auth.currentlySignedInAs") + Auth.CreatorName;
                curUserTextBox.RawText = curUserString;
            }
            
            // Set initial state to match what's stored.
            keepSignedInCheckBox.Checked = XmlOptionsData.KeepSignedInOnExit;
            prevCheckboxState = keepSignedInCheckBox.Checked;

            base.Activate(args);
        }   // end of Activate()

        #endregion

        #region Internal
        #endregion

    }   // end of class AuthSignOutDialog

}   // end of namespace KoiX.UI.Dialogs
