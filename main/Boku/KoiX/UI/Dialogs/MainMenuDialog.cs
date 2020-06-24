
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
    /// MainMenu is non-modal so we can also interact with news feed and other elements.
    /// </summary>
    public class MainMenuDialog : BaseDialogNonModal
    {
        #region Members

        WidgetSet set;

        MainMenuButton resumeButton;
        MainMenuButton newWorldButton;
        MainMenuButton loadWorldButton;
        MainMenuButton communityButton;
        MainMenuButton optionsButton;
        MainMenuButton helpButton;
        MainMenuButton quitButton;

        MainMenuButton webSiteButton;

        #endregion

        #region Accessors
        #endregion

        #region Public

        public MainMenuDialog(float width)
        {
#if DEBUG
            _name = "MainMenuDialog";
#endif

            // For this dialog, just have the buttons free-floating.
            //RenderBaseTile = false;

            //
            // Clone the current theme and modify for these buttons.
            //
            theme = GetButtonTheme(theme);

            set = new WidgetSet(this, new RectangleF(), Orientation.Vertical);
            //set.Padding = new Padding(8, 16, 8, 16);
            AddWidget(set);

            resumeButton = new MainMenuButton(this, new RectangleF(0, 0, width, 60), labelId: "mainMenu.resume", onSelect: OnResume, theme: theme);
            set.AddWidget(resumeButton);

            newWorldButton = new MainMenuButton(this, new RectangleF(0, 0, width, 60), labelId: "mainMenu.new", onSelect: OnNewWorld, theme: theme);
            set.AddWidget(newWorldButton);

            loadWorldButton = new MainMenuButton(this, new RectangleF(0, 0, width, 60), labelId: "mainMenu.play", onSelect: OnLoadWorld, theme: theme);
            set.AddWidget(loadWorldButton);

            communityButton = new MainMenuButton(this, new RectangleF(0, 0, width, 60), labelId: "mainMenu.community", onSelect: OnCommunity, theme: theme);
            set.AddWidget(communityButton);

            optionsButton = new MainMenuButton(this, new RectangleF(0, 0, width, 60), labelId: "mainMenu.options", onSelect: OnOptions, theme: theme);
            set.AddWidget(optionsButton);

            helpButton = new MainMenuButton(this, new RectangleF(0, 0, width, 60), labelId: "mainMenu.help", onSelect: OnHelp, theme: theme);
            set.AddWidget(helpButton);

            quitButton = new MainMenuButton(this, new RectangleF(0, 0, width, 60), labelId: "mainMenu.exit", onSelect: OnQuit, theme: theme);
            set.AddWidget(quitButton);

            webSiteButton = new MainMenuButton(this, new RectangleF(0, 0, 400, 60), labelText: "www.KoduGameLab.com", onSelect: OnWebSite, theme: theme);
            // Center label on this button.
            webSiteButton.Padding = new Padding();
            webSiteButton.Label.Size = webSiteButton.Size;
            webSiteButton.Label.HorizontalJustification = Justification.Center;
            // Leave a gap between the main buttons and this one.
            webSiteButton.Margin = new Padding(0, 40, 0, 0);
            set.AddWidget(webSiteButton);

            // Call Recalc for force all the button positions and sizes to be calculated.
            // We need this in order to properly calc the links.
            Dirty = true;
            Recalc();

            // Connect navigation links.
            CreateTabList();

            // Dpad nav includes all widgets so webSiteButton will be there.
            CreateDPadLinks();

        }   // end of c'tor

        void OnResume(BaseWidget w)
        {
            // If there is no current world, try and get one from Undo.
            if (InGame.CurrentWorldId == Guid.Empty)
            {
                if (InGame.UnDoStack.Resume())
                {
                    SceneManager.SwitchToScene("EditWorldScene");
                }
                else
                {
                    // We had some error in trying to resume.  So, remove the resume
                    // option from the menu and soldier on.
                    resumeButton.Disable();
                }
            }
            else
            {
                // Just jump back into the already loaded world.
                SceneManager.SwitchToScene("EditWorldScene");
            }

        }   // end of OnResume()

        void OnNewWorld(BaseWidget w)
        {
            // Load the NewWorld level and switch to EditWorldScene.
            string levelFilename = BokuGame.Settings.MediaPath + BokuGame.BuiltInWorldsPath + @"03a1b038-fd3f-492f-b18c-2a197fe68701.Xml";
            if (BokuGame.bokuGame.inGame.LoadLevelAndRun(levelFilename, keepPersistentScores: false, newWorld: true, andRun: false))
            {
                SceneManager.SwitchToScene("EditWorldScene");
            }
        }   // end of OnNewWorld()

        void OnLoadWorld(BaseWidget w)
        {
            SceneManager.SwitchToScene("LoadLevelLocalScene");
        }   // end of OnLoadWorld()
        
        void OnCommunity(BaseWidget w)
        {
            SceneManager.SwitchToScene("LoadLevelCommunityScene");
        }   // end of OnCommunity()
        
        void OnOptions(BaseWidget w)
        {
            SceneManager.SwitchToScene("OptionsMenuScene");
        }   // end of OnOptions()
        
        void OnHelp(BaseWidget w)
        {
            SceneManager.SwitchToScene("HelpMenuScene");
        }   // end of OnHelp()

        void OnQuit(BaseWidget w)
        {
            BokuGame.bokuGame.Exit();
        }   // end of OnQuit()

        void OnWebSite(BaseWidget w)
        {
            Process.Start(Boku.Program2.SiteOptions.KGLUrl + "?ref=client");
        }

        public override void Update(SpriteCamera camera)
        {
            // Disable resume option if we don't have something to resume to.
            if (!Boku.InGame.UnDoStack.HaveResume())
            {
                resumeButton.Disable();
            }

            base.Update(camera);
        }   // end of Update()

        static public ThemeSet GetButtonTheme(ThemeSet themeSet)
        {
            ThemeSet theme = Theme.CurrentThemeSet.Clone() as ThemeSet;

            // Change so that focus has dark text and bright green body.
            theme.ButtonNormalFocused.BodyColor = themeSet.FocusColor;
            theme.ButtonNormalFocusedHover.BodyColor = themeSet.FocusColor;
            theme.ButtonSelectedFocused.BodyColor = themeSet.FocusColor;
            theme.ButtonSelectedFocusedHover.BodyColor = themeSet.FocusColor;

            theme.ButtonNormalFocused.TextColor = themeSet.DarkTextColor;
            theme.ButtonNormalFocusedHover.TextColor = themeSet.DarkTextColor;
            theme.ButtonSelectedFocused.TextColor = themeSet.DarkTextColor;
            theme.ButtonSelectedFocusedHover.TextColor = themeSet.DarkTextColor;

            theme.ButtonNormalFocused.TextOutlineColor = themeSet.LightTextColor;
            theme.ButtonNormalFocusedHover.TextOutlineColor = themeSet.LightTextColor;
            theme.ButtonSelectedFocused.TextOutlineColor = themeSet.LightTextColor;
            theme.ButtonSelectedFocusedHover.TextOutlineColor = themeSet.LightTextColor;

            return theme;
        }   // end of GetButtonTheme()

        #endregion

        #region Internal
        #endregion

    }   // end of class MainMenuDialog

}   // end of namespace KoiX.UI.Dialogs
