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

    public class AuthSignInDialog : BaseDialogWithTitle
    {
        #region Members

        Button okButton;
        Button cancelButton;
        int margin = 8;         // Around button text.

        Label creatorLabel;
        Label pinLabel;

        SingleLineTextEditBox creatorTextEditBox;
        SingleLineTextEditBox pinTextEditBox;

        CheckBox keepSignedInCheckBox;
        TextBox keepSignedInTextBox;

        TextBox pinErrorTextBox;

        TextBox warningTextBox; // Warning to not use real name when choosing Creator name.

        HelpButton helpButton;

        #endregion

        #region Accessors
        #endregion

        #region Public

        public AuthSignInDialog(RectangleF rect, string titleId, ThemeSet theme = null)
            : base(rect, titleId, theme: theme)
        {
#if DEBUG
            _name = "AuthSignInDialog";
#endif

            // Change ref to what base class has set.
            // Allows us to use theme rather than this.Theme.
            theme = this.ThemeSet;

            // Help button.
            {
                helpButton = new HelpButton(this, OnHelp);

                // For this dialog we also want the question mark to show up in the upper 
                // right hand corner so change the titleSet to full justification and add
                // the help button to it.
                titleSet.HorizontalJustification = Justification.Full;
                titleSet.AddWidget(helpButton);
            }

            // Body
            {
                SystemFont font = SysFont.GetSystemFont(theme.TextFontFamily, theme.TextBaseFontSize, theme.TextBaseFontStyle);
                FontWrapper wrapper = new FontWrapper(null, font);
                GetFont Font = delegate() { return wrapper; };

                creatorLabel = new Label(this, font, theme.LightTextColor, labelId: "auth.creator");
                creatorLabel.Size = creatorLabel.CalcMinSize();
                creatorTextEditBox = new SingleLineTextEditBox(this, Font, 400, Auth.DefaultCreatorName, "", theme: theme);
                bodySet.AddWidget(creatorLabel);
                bodySet.AddWidget(creatorTextEditBox);

                pinLabel = new Label(this, font, theme.LightTextColor, labelId: "auth.pin");
                pinLabel.Size = pinLabel.CalcMinSize();
                int pinWidth = (int)Math.Ceiling(font.MeasureString("0000").X + 2.0 * theme.TextEditBoxNormal.CornerRadius);
                pinTextEditBox = new SingleLineTextEditBox(this, Font, pinWidth, "0000", "", theme: theme, maskInput: true);
                bodySet.AddWidget(pinLabel);
                bodySet.AddWidget(pinTextEditBox);

                // Make both labels the same size and right justify.
                Vector2 maxLabelSize = MyMath.Max(creatorLabel.Size, pinLabel.Size);
                creatorLabel.Size = maxLabelSize;
                pinLabel.Size = maxLabelSize;
                creatorLabel.HorizontalJustification = Justification.Right;
                pinLabel.HorizontalJustification = Justification.Right;


                pinErrorTextBox = new TextBox(this, Font, AuthUI.ErrorColor, outlineColor: theme.DarkTextColor, outlineWidth: 0.75f, textId: "auth.pinError");
                bodySet.AddWidget(pinErrorTextBox);

                keepSignedInCheckBox = new CheckBox(this, theme: theme);
                keepSignedInTextBox = new TextBox(this, Font, theme.LightTextColor, textId: "auth.keepSignedIn");
                keepSignedInTextBox.Width = (int)rect.Width - 2 * 32;   // Width minus margins.

                bodySet.AddWidget(keepSignedInCheckBox);
                bodySet.AddWidget(keepSignedInTextBox);

                // Set initial state to match what's stored.
                keepSignedInCheckBox.Checked = XmlOptionsData.KeepSignedInOnExit;

                warningTextBox = new TextBox(this, Font, AuthUI.ErrorColor, outlineColor: theme.DarkTextColor, outlineWidth: 0.75f, textId: "auth.helpTextShort");
                warningTextBox.Width = (int)rect.Width - 2 * 32;   // Width minus margins.
                bodySet.AddWidget(warningTextBox);
            }

            buttonSet.Padding = new Padding(32, 16, 32, 16);

            okButton = new Button(this, new RectangleF(), labelId: "auth.ok", OnChange: OnOk, theme: this.theme);
            okButton.Size = okButton.CalcMinSize() + new Vector2(margin, 0);  // Match button size to label, with a bit of margin.
            okButton.Label.Size = okButton.Size;                              // Make label same size so it gets centered correctly.
            buttonSet.AddWidget(okButton);

            cancelButton = new Button(this, new RectangleF(), labelId: "auth.cancel", OnChange: OnCancel, theme: this.theme);
            cancelButton.Size = cancelButton.CalcMinSize() + new Vector2(margin, 0);    // Match button size to label, with a bit of margin.
            cancelButton.Label.Size = cancelButton.Size;                                // Make label same size so it gets centered correctly.

            buttonSet.AddWidget(cancelButton);

            // Call Recalc for force all the button positions and sizes to be calculated.
            // We need this in order to properly calc the links.
            Dirty = true;
            Recalc();

            // Connect navigation links.
            CreateTabList();

            // Dpad nav includes all widgets.
            CreateDPadLinks();

        }   // end of c'tor

        void OnOk(BaseWidget w)
        {
            // Copy current user name to auth if valid.
            if (creatorTextEditBox.CurrentText != Auth.DefaultCreatorName && pinTextEditBox.CurrentText != Auth.DefaultCreatorPin && Auth.IsPinValid(pinTextEditBox.CurrentText))
            {
                Auth.SetCreator(creatorTextEditBox.CurrentText, pinTextEditBox.CurrentText);
            }

            XmlOptionsData.CreatorName = Auth.CreatorName;
            XmlOptionsData.CreatorIdHash = Auth.IdHash;
            XmlOptionsData.KeepSignedInOnExit = keepSignedInCheckBox.Checked;

            DialogManagerX.KillDialog(this);
            DialogManagerX.ShowDialog(AuthUI.StatusDialog, camera);

            // Clear edit boxes.
            creatorTextEditBox.RawText = "";
            pinTextEditBox.RawText = "";

        }   // end of OnOk()

        void OnCancel(BaseWidget w)
        {
            DialogManagerX.KillDialog(this);
            DialogManagerX.ShowDialog(AuthUI.StatusDialog, camera);
        }   // end of OnCancel()

        void OnHelp(BaseWidget w)
        {
            TextDialog helpDialog = SharedX.TextDialog;
            helpDialog.TitleId = "auth.signInTitle";
            helpDialog.BodyId = "auth.helpText";
            DialogManagerX.ShowDialog(helpDialog);
        }   // end of OnHelp()

        public override void  Recalc()
        {
            // Manually set position of elements in body.

            Vector2 pos = new Vector2(32, 32);

            creatorLabel.Position = pos;
            creatorTextEditBox.Position = pos + new Vector2(creatorLabel.Size.X + 16, 0);

            pos.Y += creatorLabel.Size.Y;
            pinLabel.Position = pos;
            pinTextEditBox.Position = pos + new Vector2(pinLabel.Size.X + 16, 0);
            pinErrorTextBox.Position = pinTextEditBox.Position + new Vector2(pinTextEditBox.Size.X + 16, 0);
            pinErrorTextBox.Width = (int)(rect.Width - pinErrorTextBox.Position.X - 32);

            pos.Y += 2.7f * pinLabel.Size.Y;
            keepSignedInCheckBox.Position = pos + new Vector2(0, 3);    // The extra 3 pixels makes is line up with the text better.
            pos.X += 48;    // Checkbox width.
            keepSignedInTextBox.Position = pos;

            pos.X -= 48;
            pos.Y += keepSignedInTextBox.Size.Y + 24;
            warningTextBox.Position = pos;
            
            base.Recalc();
        }   // end of Recalc()

        public override void Update(SpriteCamera camera)
        {
            if (Active)
            {
                // Manually set position of elements in body.
                Recalc();

                // Pin warnings.
                if (pinTextEditBox.CurrentText != Auth.DefaultCreatorPin && !Auth.IsPinValid(pinTextEditBox.CurrentText))
                {
                    // Show error.
                    pinErrorTextBox.Alpha = 1.0f;

                    string str = Strings.Localize("auth.pinError");
                    if (pinTextEditBox.CurrentText.Length != 4)
                    {
                        str += "\n" + Strings.Localize("auth.pinTooShort");
                    }
                    else
                    {
                        str += "\n" + Strings.Localize("auth.pinTooSimple");
                    }
                    pinErrorTextBox.RawText = str;
                }
                else
                {
                    // Hide error.
                    pinErrorTextBox.Alpha = 0;
                }

                // Keep auth state in sync with checkbox.
                XmlOptionsData.KeepSignedInOnExit = keepSignedInCheckBox.Checked;

            }

            base.Update(camera);
        }   // end of Update()

        public override void Render(SpriteCamera camera)
        {
            base.Render(camera);
        }   // end of Render()

        public override void Activate(params object[] args)
        {
            // Set initial state to match what's stored.
            keepSignedInCheckBox.Checked = XmlOptionsData.KeepSignedInOnExit;

            base.Activate(args);
        }

        #endregion

        #region Internal
        #endregion

    }   // end of class AuthSignInDialog

}   // end of namespace KoiX.UI.Dialogs
