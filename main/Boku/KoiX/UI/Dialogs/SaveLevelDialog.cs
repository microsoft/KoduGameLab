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
using BokuShared;

namespace KoiX.UI.Dialogs
{
    using Keys = Microsoft.Xna.Framework.Input.Keys;

    /// <summary>
    /// SaveLevelDialog is non-modal so we can also interact with Auth and other elements.
    /// </summary>
    public class SaveLevelDialog : BaseDialogWithTitle
    {
        #region Members

        SingleLineTextEditBox nameTextBox;          // Name of level.
        TextBox versionTextBox;                     // Version number.
        GraphicButton versionArrowsButton;          // Single button with both up and down arrows for version.
        MultiLineTextEditBox descriptionTextBox;    // Description for level.

        MonochromeGraphicRadioButton leftJustifyButton;       // Justification applied to description.
        MonochromeGraphicRadioButton centerJustifyButton;
        MonochromeGraphicRadioButton rightJustifyButton;
        List<MonochromeGraphicRadioButton> justifySiblings;

        TextBox warningTextBox;                     // Warning not to use personal info in name or description.

        Button tagsButton;
        TextBox tagsTextBox;                        // List of tags associated with level.

        Button okButton;
        Button cancelButton;
        int margin = 8;                             // Around button text.

        HelpButton helpButton;

        int currentVersion = 0;
        Genres currentGenres = Genres.None;
        WidgetSet nameVersionSet;                   // Contains the level name and version number widgets.
        WidgetSet warningJustificationSet;          // Contains the PII usage warning and description justification widgets.
        WidgetSet justificationSet;                 // Contains radio buttons for justification of description.
        WidgetSet tagsSet;

        BaseDialog returnDialog;                    // Dialog to activate on exit.
                                                    // TODO (****) is this something we think we might use all the time?
                                                    // If so, should be moved to BaseDialog.

        // Keep old values in case of cancel.
        string originalName;            // Includes name and version.
        string originalDescription;
        TextHelper.Justification originalDescriptionJustification;
        Genres originalGenres;

        #endregion

        #region Accessors
        #endregion

        #region Public

        public SaveLevelDialog(RectangleF rect, ThemeSet theme)
            : base(rect, titleId: "saveLevelDialog.titleXbox", theme: theme)
        {
#if DEBUG
            _name = "SaveLevelDialog";
#endif
            // BaseDialogWithTitle is modal by default.  We want the SaveLevelDialog to be
            // non-modal so that the Auth dialogs work with it.
            IsModalDialog = false;

            int fullWidth = (int)(rect.Width - 2 * 32);     // Overall dialog width minus margins.

            SystemFont font = SysFont.GetSystemFont(theme.TextFontFamily, theme.TextBaseFontSize, theme.TextBaseFontStyle);
            FontWrapper wrapper = new FontWrapper(null, font);
            GetFont Font = delegate() { return wrapper; };

            // Body
            {

                bodySet.Orientation = Orientation.Vertical;
                bodySet.HorizontalJustification = Justification.Left;
                bodySet.VerticalJustification = Justification.Top;


                nameVersionSet = new WidgetSet(this, new RectangleF(0, 0, fullWidth, 0), Orientation.Horizontal, horizontalJustification: Justification.Full, verticalJustification: Justification.Center);
                nameVersionSet.Margin = new Padding(32, 0, 0, 0);
                bodySet.AddWidget(nameVersionSet);

                nameTextBox = new SingleLineTextEditBox(this, Font, fullWidth - 160, "Level Name", "EmptyWorld", theme: theme);
                nameTextBox.Size = new Vector2(nameTextBox.Size.X, 40);
                nameTextBox.Margin = new Padding(0, 32, 0, 16);
                nameVersionSet.AddWidget(nameTextBox);

                versionTextBox = new TextBox(this, Font, theme.DarkTextColor, displayText: "v00");
                versionTextBox.Width = (int)(font.MeasureString("v00").X + 1);
                nameVersionSet.AddWidget(versionTextBox);

                versionArrowsButton = new GraphicButton(this, new RectangleF(0, 0, 60, 80), @"Textures\HelpCard\UpDownArrows", OnVersion);
                versionArrowsButton.Focusable = false;  // Don't put it into the tab focus rotation.
                nameVersionSet.AddWidget(versionArrowsButton);



                RectangleF descriptionRect = new RectangleF(0, 0, fullWidth, 400);
                descriptionTextBox = new MultiLineTextEditBox(this, descriptionRect, Font, text: "Replace me with current description text", theme: theme);
                descriptionTextBox.Margin = new Padding(32, 16, 32, 16);
                bodySet.AddWidget(descriptionTextBox);

                // Warning and justification buttons.
                warningJustificationSet = new WidgetSet(this, new RectangleF(0, 0, fullWidth, 0), orientation: Orientation.Horizontal, horizontalJustification: Justification.Full, verticalJustification: Justification.Top);
                bodySet.AddWidget(warningJustificationSet);

                warningTextBox = new TextBox(this, Font, Color.Yellow, textId: "saveLevelDialog.safetyWarning");
                warningTextBox.Width = fullWidth - 192;
                warningJustificationSet.AddWidget(warningTextBox);
                warningJustificationSet.Margin = new Padding(32, 0, 32, 0);

                Vector2 buttonSize = new Vector2(48, 48);
                justificationSet = new WidgetSet(this, new RectangleF(0, 0, 3 * buttonSize.X, 0), orientation: Orientation.Horizontal, horizontalJustification: Justification.Right, verticalJustification: Justification.Center);
                warningJustificationSet.AddWidget(justificationSet);

                justifySiblings = new List<MonochromeGraphicRadioButton>();
                leftJustifyButton = new MonochromeGraphicRadioButton(this, justifySiblings, buttonSize, textureName: @"Textures\TextEditor\LeftJustify");
                justificationSet.AddWidget(leftJustifyButton);
                centerJustifyButton = new MonochromeGraphicRadioButton(this, justifySiblings, buttonSize, textureName: @"Textures\TextEditor\CenterJustify");
                justificationSet.AddWidget(centerJustifyButton);
                rightJustifyButton = new MonochromeGraphicRadioButton(this, justifySiblings, buttonSize, textureName: @"Textures\TextEditor\RightJustify");
                justificationSet.AddWidget(rightJustifyButton);
                                
            }

            // Tags
            // Note that TagsSet is free-floating so we need to set it's position explicitly.
            tagsSet = new WidgetSet(this, new RectangleF(0, 0, fullWidth - 240, 0), Orientation.Horizontal, horizontalJustification: Justification.Left, verticalJustification: Justification.Top);
            tagsSet.Margin = new Padding(32, 16, 32, 0);
            AddWidget(tagsSet);

            tagsButton = new Button(this, RectangleF.EmptyRect, labelId: "loadLevelMenu.tags", OnChange: OnTagsButton, theme: theme);
            tagsButton.Margin = new Padding(0, 0, 32, 0);
            tagsSet.AddWidget(tagsButton);

            tagsTextBox = new TextBox(this, Font, theme.LightTextColor, displayText: "replace me with\nlist of tags!");
            tagsTextBox.Width = 756;
            tagsSet.AddWidget(tagsTextBox);


            buttonSet.Padding = new Padding(32, 16, 32, 16);

            okButton = new Button(this, RectangleF.EmptyRect, labelId: "auth.ok", OnChange: OnOk, theme: this.theme);
            okButton.Size = okButton.CalcMinSize() + new Vector2(margin, 0);  // Match button size to label, with a bit of margin.
            okButton.Label.Size = okButton.Size;                              // Make label same size so it gets centered correctly.
            buttonSet.AddWidget(okButton);

            cancelButton = new Button(this, RectangleF.EmptyRect, labelId: "auth.cancel", OnChange: OnCancel, theme: this.theme);
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

            // DEBUG ONLY
            /*
            warningJustificationSet.RenderDebugOutline = true;
            justificationSet.RenderMarginDebug = true;
            
            bodySet.RenderDebugOutline = true;
            bodySet.RenderMarginDebug = true;
            

            versionTextBox.RenderMarginDebug = true;
            versionArrowsButton.RenderMarginDebug = true;

            nameVersionSet.RenderDebugOutline = true;
            nameVersionSet.RenderMarginDebug = true;

            tagsSet.RenderDebugOutline = true;
            tagsSet.RenderMarginDebug = true;
            tagsTextBox.RenderMarginDebug = true;
             */
        }   // end of c'tor

        /// <summary>
        /// Handler for version up/down arrow buttons.  Note these are represented by a single
        /// button so we have to look at the hit position to decide which button is hit.
        /// </summary>
        /// <param name="w"></param>
        void OnVersion(BaseWidget w)
        {
            Vector2 mouseHit = LowLevelMouseInput.PositionVec;
            // Move from Window coords to camera coords,
            Vector2 hit = camera.ScreenToCamera(mouseHit);
            // into dialog coords,
            hit -= w.ParentDialog.Rectangle.Position;
            // into parent container coords,
            hit -= w.ParentPosition;
            // and finally into widget/rt coords.
            hit -= w.LocalRect.Position;

            if (hit.Y < w.LocalRect.Height / 2.0f)
            {
                // Increment version.
                ++currentVersion;
            }
            else
            {
                // Decrement version.
                --currentVersion;
                if (currentVersion < 0)
                {
                    currentVersion = 0;
                }
            }

            versionTextBox.RawText = 'v' + currentVersion.ToString("00");

        }   // end of OnVersion()

        void OnTagsButton(BaseWidget w)
        {
            DialogCenter.TagsDialog.Genres = currentGenres;
            DialogCenter.TagsDialog.OnDeactivate = OnTagsDone;
            DialogManagerX.ShowDialog(DialogCenter.TagsDialog, camera);
        }   // end of OnTagsButton()

        void OnTagsDone(BaseDialog d)
        {
            // Copy changes back.
            currentGenres = DialogCenter.TagsDialog.Genres;

            // Clear this just in case TagsDialog is launched 
            // from elsewhere without an OnDeactivate callback.
            DialogCenter.TagsDialog.OnDeactivate = null;
        }   // end of OnTagsDone()

        void OnOk(BaseWidget w)
        {
            // Copy current values back to world.
            InGame.XmlWorldData.name = nameTextBox.CurrentText + " " + versionTextBox.RawText;
            InGame.XmlWorldData.description = descriptionTextBox.BodyText;

            InGame.XmlWorldData.descJustification = TextHelper.Justification.Left;
            if (centerJustifyButton.Selected)
            {
                InGame.XmlWorldData.descJustification = TextHelper.Justification.Center;
            }
            else if (rightJustifyButton.Selected)
            {
                InGame.XmlWorldData.descJustification = TextHelper.Justification.Right;
            }
            
            InGame.XmlWorldData.genres = (int)currentGenres;


            // Do the save.
            bool newName = NewName();
            bool needsCheck = CheckPreserveLinks();

            if (!newName)
            {
                DialogCenter.OverwriteWarningDialog.OverwriteButton.SetOnChange(OnOverwrite);
                DialogCenter.OverwriteWarningDialog.CancelButton.SetOnChange(OnOverwriteCancel);
                DialogCenter.OverwriteWarningDialog.IncrementAndSaveButton.SetOnChange(OnIncrementAndSave);
                DialogManagerX.ShowDialog(DialogCenter.OverwriteWarningDialog, camera);
            }
            else if (needsCheck)
            {
                // Level has links and saved with new name - ask if we should preserve links.
                DialogCenter.PreserveLinksDialog.YesButton.SetOnChange(OnPreserveLinksYes);
                DialogCenter.PreserveLinksDialog.NoButton.SetOnChange(OnPreserveLinksNo);
                DialogManagerX.ShowDialog(DialogCenter.PreserveLinksDialog, camera);
            }
            else
            {
                InGame.inGame.SaveLevel(newName: newName, preserveLinks: true);
                // If just saving, kill this (SaveLevelDialog).  If showing other
                // dialogs, have thier hanlders kill this.
                DialogManagerX.KillDialog(this);
            }

        }   // end of OnOk()

        void OnCancel(BaseWidget w)
        {
            // Restore original values.  (Do we really need to do this?)
            InGame.XmlWorldData.name = originalName;    // Includes name and version.
            InGame.XmlWorldData.description = originalDescription;
            InGame.XmlWorldData.descJustification = originalDescriptionJustification;
            InGame.XmlWorldData.genres = (int)originalGenres;

            DialogManagerX.KillDialog(this);
        }   // end of OnCancel()

        void OnOverwrite(BaseWidget w)
        {
            DialogManagerX.KillDialog(DialogCenter.OverwriteWarningDialog);
            DialogManagerX.KillDialog(this);

            InGame.inGame.SaveLevel(newName: false, preserveLinks: true);
        }   // end of OnOverwrite()

        void OnOverwriteCancel(BaseWidget w)
        {
            DialogManagerX.KillDialog(DialogCenter.OverwriteWarningDialog);

            // Just go back to SaveLevelDialog.

        }   // end of OnOverwriteCancel()

        void OnIncrementAndSave(BaseWidget w)
        {
            DialogManagerX.KillDialog(DialogCenter.OverwriteWarningDialog);
            DialogManagerX.KillDialog(this);

            ++currentVersion;
            versionTextBox.RawText = 'v' + currentVersion.ToString("00");
            InGame.XmlWorldData.name = nameTextBox.CurrentText + " " + versionTextBox.RawText;

            InGame.inGame.SaveLevel(newName: true, preserveLinks: false);
        }   // end of OnIncrementAndSave()

        void OnPreserveLinksYes(BaseWidget w)
        {
            DialogManagerX.KillDialog(DialogCenter.PreserveLinksDialog);
            DialogManagerX.KillDialog(this);

            InGame.inGame.SaveLevel(newName: true, preserveLinks: true);
        }   // end of OnPreserveLinksYes()

        void OnPreserveLinksNo(BaseWidget w)
        {
            DialogManagerX.KillDialog(DialogCenter.PreserveLinksDialog);
            DialogManagerX.KillDialog(this);

            InGame.inGame.SaveLevel(newName: true, preserveLinks: false);
        }   // end of OnPreserveLinksNo()

        public void SetParams(BaseDialog returnDialog)
        {
            this.returnDialog = returnDialog;
        }   // end of SetParams()

        public override void Update(SpriteCamera camera)
        {
            //Dirty = true;
            //nameVersionSet.Dirty = true;
            //bodySet.Dirty = true;

            if (Active)
            {
                tagsSet.Position = new Vector2(32, buttonSet.Position.Y - 32);
                tagsSet.Dirty = true;

                Recalc();
            }

            base.Update(camera);
        }   // end of Update()

        public override void Activate(params object[] args)
        {
            // Get current values from world.
            originalName = InGame.XmlWorldData.name;
            string name = StripVersionNumber(originalName, ref currentVersion);
            nameTextBox.RawText = name;
            versionTextBox.RawText = 'v' + currentVersion.ToString("00");

            originalDescription = InGame.XmlWorldData.description;
            descriptionTextBox.BodyText = originalDescription;

            originalDescriptionJustification = InGame.XmlWorldData.descJustification;
            switch (originalDescriptionJustification)
            {
                case TextHelper.Justification.Left:
                    leftJustifyButton.Selected = true;
                    break;
                case TextHelper.Justification.Center:
                    centerJustifyButton.Selected = true;
                    break;
                case TextHelper.Justification.Right:
                    rightJustifyButton.Selected = true;
                    break;
            }
            
            originalGenres =  (Genres)InGame.XmlWorldData.genres;
            currentGenres = originalGenres;

            // Tell InGame we're using the thumbnail so no need to do full render.
            InGame.inGame.RenderWorldAsThumbnail = true;

            nameTextBox.SetFocus(overrideInactive: true);

            base.Activate(args);
        }   // end of Activate()

        public override void Deactivate()
        {
            base.Deactivate();

            if (returnDialog != null)
            {
                DialogManagerX.ShowDialog(returnDialog, camera);
            }

            InGame.inGame.RenderWorldAsThumbnail = false;

        }   // end of Deactivate()

        #endregion

        #region Internal

        /// <summary>
        /// Strips the version number off the name string and returns the string without it.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="version">Version number found in original string, 0 if none found.</param>
        /// <returns></returns>
        string StripVersionNumber(string name, ref int version)
        {
            int pos = name.LastIndexOf(@" v");
            if (pos == -1)
            {
                // No version number found.
                version = 0;
            }
            else
            {
                try
                {
                    version = int.Parse(name.Substring(pos + 2));
                }
                catch
                {
                    version = 0;
                }
                if (version != 0)
                {
                    // Got a valid version number, trim it off the name.
                    name = name.Substring(0, pos);
                }
            }

            return name;
        }   // end of StripVersionNumber()

        /// <summary>
        /// Determines if the user is saving the file with a new name and/or version number.
        /// Note this always returns true if saving a built-in or imported world since those
        /// will now be new to MyWorlds.
        /// </summary>
        /// <returns></returns>
        bool NewName()
        {
            string name = nameTextBox.CurrentText + " " + versionTextBox.ScrubbedText;
            bool newName = originalName != name;

            if ((InGame.XmlWorldData.genres & (int)Genres.MyWorlds) == 0)
            {
                newName = true;
            }

            return newName;
        }   // end of NewName()

        /// <summary>
        /// Determines if the user needs to be asked if level links should be preserved during the save.
        /// </summary>
        /// <returns></returns>
        bool CheckPreserveLinks()
        {
            bool needsCheck = InGame.XmlWorldData.LinkedFromLevel != null || InGame.XmlWorldData.LinkedToLevel != null;

            // Check if the level has been saved before (or has been saved but not as a local world).
            if (!XmlDataHelper.CheckWorldExistsByGenre(InGame.XmlWorldData.id, Genres.MyWorlds))
            {
                // Hasn't been saved or is non-local - either way, we'll be making a new copy and links will be preserved 
                // automatically - don't have to ask the user
                needsCheck = false;
            }

            return needsCheck;
        }   // end of CheckPreserveLinks()

        #endregion

    }   // end of class SaveLevelDialog
}   // end of namespace KoiX.UI.Dialogs
