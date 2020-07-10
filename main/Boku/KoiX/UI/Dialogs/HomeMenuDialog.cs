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

namespace KoiX.UI.Dialogs
{
    using Keys = Microsoft.Xna.Framework.Input.Keys;

    /// <summary>
    /// HomeMenu is non-modal so we can also interact with Auth and other elements.
    /// </summary>
    public class HomeMenuDialog : BaseDialogWithTitle
    {
        #region Members

        MainMenuButton playWorldButton;
        MainMenuButton editWorldButton;
        MainMenuButton saveWorldButton;
        MainMenuButton shareWorldButton;
        MainMenuButton loadWorldButton;
        MainMenuButton newWorldButton;
        MainMenuButton printKodeButton;
        MainMenuButton exitToMainMenuButton;

        GraphicButton house;

        SaveLevelDialog saveLevelDialog;

        #endregion

        #region Accessors
        #endregion

        #region Public

        public HomeMenuDialog()
            : base(RectangleF.EmptyRect, titleId:"minihub.miniHub")
        {
#if DEBUG
            _name = "HomeMenuDialog";
#endif

            // BaseDialogWithTitle is modal by default.  We want the HomeMenu to be
            // non-modal so that the Auth dialogs work with it.
            IsModalDialog = false;

            float width = 600;

            theme.DialogBodyTileNormal.TileColor = ThemeSet.BaseColorPlus10;
            theme.DialogBodyTileFocused.TileColor = ThemeSet.BaseColorPlus10;

            // Create the SaveLevelDialog first since we want to use the 
            // unmodified theme on it.
            Vector2 saveLevelDialogSize = new Vector2(1200, 800);
            saveLevelDialog = new SaveLevelDialog(new RectangleF(-saveLevelDialogSize / 2.0f, saveLevelDialogSize), theme: theme);

            //
            // Clone the current theme and modify for these buttons.
            //
            {
                theme = Theme.CurrentThemeSet.Clone() as ThemeSet;

                // Change so that focus has dark text and bright green body.
                theme.ButtonNormalFocused.BodyColor = ThemeSet.FocusColor;
                theme.ButtonNormalFocusedHover.BodyColor = ThemeSet.FocusColor;
                theme.ButtonSelectedFocused.BodyColor = ThemeSet.FocusColor;
                theme.ButtonSelectedFocusedHover.BodyColor = ThemeSet.FocusColor;

                theme.ButtonNormal.OutlineWidth = 0;
                theme.ButtonNormalFocused.OutlineWidth = 0;
                theme.ButtonNormalFocusedHover.OutlineWidth = 0;
                theme.ButtonSelectedFocused.OutlineWidth = 0;
                theme.ButtonSelectedFocusedHover.OutlineWidth = 0;

                // Keep the focus color white.  Otherwise the text turns green on focus.
                theme.ButtonNormalFocused.TextColor = ThemeSet.LightTextColor;
                theme.ButtonNormalFocusedHover.TextColor = ThemeSet.LightTextColor;
                theme.ButtonSelectedFocused.TextColor = ThemeSet.LightTextColor;
                theme.ButtonSelectedFocusedHover.TextColor = ThemeSet.LightTextColor;

                // Used oversized font.  This works since we're setting these buttons up
                // to not have any outlines.
                theme.ButtonNormal.FontSize *= 1.5f;

                /*
                theme.ButtonNormalFocused.TextColor = ThemeSet.DarkTextColor;
                theme.ButtonNormalFocusedHover.TextColor = ThemeSet.DarkTextColor;
                theme.ButtonSelectedFocused.TextColor = ThemeSet.DarkTextColor;
                theme.ButtonSelectedFocusedHover.TextColor = ThemeSet.DarkTextColor;

                theme.ButtonNormalFocused.TextOutlineColor = ThemeSet.LightTextColor;
                theme.ButtonNormalFocusedHover.TextOutlineColor = ThemeSet.LightTextColor;
                theme.ButtonSelectedFocused.TextOutlineColor = ThemeSet.LightTextColor;
                theme.ButtonSelectedFocusedHover.TextOutlineColor = ThemeSet.LightTextColor;
                */
            }

            // Use center justification for this menu.
            this.titleSet.HorizontalJustification = Justification.Center;

            bodySet.Orientation = Orientation.Vertical;

            playWorldButton = new MainMenuButton(this, new RectangleF(0, 0, width, 60), labelId: "miniHub.reset", onSelect: OnPlayWorld, theme: theme);
            playWorldButton.Margin = new Padding(32, 32, 32, 0);
            playWorldButton.Label.HorizontalJustification = Justification.Center;
            bodySet.AddWidget(playWorldButton);

            editWorldButton = new MainMenuButton(this, new RectangleF(0, 0, width, 60), labelId: "miniHub.edit", onSelect: OnEditWorld, theme: theme);
            editWorldButton.Margin = new Padding(32, 0, 32, 0);
            editWorldButton.Label.HorizontalJustification = Justification.Center;
            bodySet.AddWidget(editWorldButton);

            saveWorldButton = new MainMenuButton(this, new RectangleF(0, 0, width, 60), labelId: "miniHub.save", onSelect: OnSaveWorld, theme: theme);
            saveWorldButton.Margin = new Padding(32, 0, 32, 0);
            saveWorldButton.Label.HorizontalJustification = Justification.Center;
            bodySet.AddWidget(saveWorldButton);

            shareWorldButton = new MainMenuButton(this, new RectangleF(0, 0, width, 60), labelId: "miniHub.publish", onSelect: OnShareWorld, theme: theme);
            shareWorldButton.Margin = new Padding(32, 0, 32, 0);
            shareWorldButton.Label.HorizontalJustification = Justification.Center;
            bodySet.AddWidget(shareWorldButton);

            loadWorldButton = new MainMenuButton(this, new RectangleF(0, 0, width, 60), labelId: "miniHub.load", onSelect: OnLoadWorld, theme: theme);
            loadWorldButton.Margin = new Padding(32, 0, 32, 0);
            loadWorldButton.Label.HorizontalJustification = Justification.Center;
            bodySet.AddWidget(loadWorldButton);

            newWorldButton = new MainMenuButton(this, new RectangleF(0, 0, width, 60), labelId: "miniHub.emptyLevel", onSelect: OnNewWorld, theme: theme);
            newWorldButton.Margin = new Padding(32, 0, 32, 0);
            newWorldButton.Label.HorizontalJustification = Justification.Center;
            bodySet.AddWidget(newWorldButton);

            printKodeButton = new MainMenuButton(this, new RectangleF(0, 0, width, 60), labelId: "miniHub.print", onSelect: OnPrintKode, theme: theme);
            printKodeButton.Margin = new Padding(32, 0, 32, 0);
            printKodeButton.Label.HorizontalJustification = Justification.Center;
            bodySet.AddWidget(printKodeButton);

            exitToMainMenuButton = new MainMenuButton(this, new RectangleF(0, 0, width, 60), labelId: "miniHub.quit", onSelect: OnExitToMainMenu, theme: theme);
            exitToMainMenuButton.Margin = new Padding(32, 0, 32, 32);
            exitToMainMenuButton.Label.HorizontalJustification = Justification.Center;
            bodySet.AddWidget(exitToMainMenuButton);

            house = new GraphicButton(this, new RectangleF(32, -64, 128, 128), @"Textures\ToolMenu\Home", onSelect: null);
            house.Focusable = false;
            AddWidget(house);



            // Call Recalc for force all the button positions and sizes to be calculated.
            // We need this in order to properly calc the links.
            Dirty = true;
            Recalc();

            // Connect navigation links.
            CreateTabList();

            // Dpad nav includes all widgets so webSiteButton will be there.
            CreateDPadLinks();

        }   // end of c'tor

        void OnPlayWorld(BaseWidget w)
        {
            SceneManager.SwitchToScene("RunSimScene");
        }   // end of OnPlayWorld()

        void OnEditWorld(BaseWidget w)
        {
            SceneManager.SwitchToScene("EditWorldScene");
        }   // end of OnEditWorld()

        void OnSaveWorld(BaseWidget w)
        {
            DialogManagerX.KillDialog(this);
            saveLevelDialog.SetParams(this);
            DialogManagerX.ShowDialog(saveLevelDialog, camera);
        }   // end of OnSaveWorld()

        void OnShareWorld(BaseWidget w)
        {
        }   // end of OnShareWorld()

        void OnLoadWorld(BaseWidget w)
        {
            SceneManager.SwitchToScene("LoadLevelLocalScene");
        }   // end of OnLoadWorld()

        void OnNewWorld(BaseWidget w)
        {
            // TODO (****) Need to implement the multi-choice, new-world dialog from Master.
            // Also, this code should be shared with MainMenu.  Can we just make it public
            // and reference it?

            // Load the NewWorld level and switch to EditWorldScene.
            string levelFilename = BokuGame.Settings.MediaPath + BokuGame.BuiltInWorldsPath + @"03a1b038-fd3f-492f-b18c-2a197fe68701.Xml";
            if (BokuGame.bokuGame.inGame.LoadLevelAndRun(levelFilename, keepPersistentScores: false, newWorld: true, andRun: false))
            {
                SceneManager.SwitchToScene("EditWorldScene");
            }
        }   // end of OnNewWorld()

        void OnPrintKode(BaseWidget w)
        {
        }   // end of OnPrintKode()

        void OnExitToMainMenu(BaseWidget w)
        {
            SceneManager.SwitchToScene("MainMenuScene");
        }   // end of OnExitToMainMenu()

        public override void Deactivate()
        {
            DialogManagerX.KillDialog(saveLevelDialog);

            base.Deactivate();
        }   // end of Deactivate()

        #endregion

        #region Internal

        public void LoadContent()
        {
            saveLevelDialog.LoadContent();

            base.LoadContent();
        }

        public void UnloadContent()
        {
            saveLevelDialog.UnloadContent();

            base.UnloadContent();
        }

        #endregion

    }   // end of class HomeMenuDialog

}   // end of namespace KoiX.UI.Dialogs
