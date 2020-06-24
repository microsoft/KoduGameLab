
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
using Boku.UI2D;

using BokuShared;

namespace KoiX.UI.Dialogs
{
    using Keys = Microsoft.Xna.Framework.Input.Keys;

    /// <summary>
    /// AuthStatus is non-modal since it just sits up in the corner.
    /// </summary>
    public class AuthStatusDialog : BaseDialogNonModal
    {
        #region Members

        Button button;
        int margin = 8;         // Around button text.

        SpriteCamera camera;    // Local ref.

        #endregion

        #region Accessors
        #endregion

        #region Public

        public AuthStatusDialog(ThemeSet theme = null)
            : base(theme: theme)
        {
#if DEBUG
            _name = "AuthStatusDialog";
#endif

            // Change ref to what base class has set.
            // Allows us to use theme rather than this.Theme.
            theme = this.ThemeSet;

            // For this dialog, just have the button free-floating.
            RenderBaseTile = false;
            focusable = false;

            string labelText = Strings.Localize("auth.creator") + " : " + Auth.CreatorName;

            button = new Button(this, new RectangleF(), labelText: labelText, OnChange: OnSelect, theme: theme);
            button.Size = button.CalcMinSize() + new Vector2(margin, 0);    // Match button size to label, with a bit of margin.
            button.Label.Size = button.Size;                                // Make label same size so it gets centered correctly.
            AddWidget(button);

            // Call Recalc for force all the button positions and sizes to be calculated.
            // We need this in order to properly calc the links.
            Dirty = true;
            Recalc();

            // Note we don't have any navigation links for this button/dialog.

        }   // end of c'tor

        void OnSelect(BaseWidget w)
        {
            DialogManagerX.KillDialog(this);

            if (Auth.IsSignedIn)
            {
                DialogManagerX.ShowDialog(AuthUI.SignOutDialog, camera);
            }
            else
            {
                DialogManagerX.ShowDialog(AuthUI.SignInDialog, camera);
            }
        }   // end of OnSelect()

        public override void Update(SpriteCamera camera)
        {
            // Test against Inactive instead of Active.  This allows the position to 
            // be properly updated even when suspended (still rendering but not accepting input).
            // This happens when a modal dialog is active.
            // TODO (****) Look through all dialogs and see if this should be changed.
            if (!Inactive)
            {
                this.camera = camera;

                Vector2 pos = new Vector2(camera.ScreenSize.X / 2.0f / camera.Zoom - button.Size.X, -camera.ScreenSize.Y / 2.0f / camera.Zoom);
                Rectangle = new RectangleF(pos, button.Size);
            }

            base.Update(camera);
        }   // end of Update()

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
            // Ensure label is up to date.  Since this string is a mix of localized and explicit
            // we need to handle it here and pass it in as is.
            string labelString = "";
            labelString += Strings.Localize("auth.creator") + " : " + Auth.CreatorName;
            button.Label.LabelText = labelString;

            button.Size = button.CalcMinSize() + new Vector2(margin, 0);    // Match button size to label, with a bit of margin.
            button.Label.Size = button.Size;                                // Make label same size so it gets centered correctly.
            
            base.Activate(args);
        }   // end of Activate()

        #endregion

        #region Internal
        #endregion

    }   // end of class AuthStatusDialog

}   // end of namespace KoiX.UI.Dialogs
