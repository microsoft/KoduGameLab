
using System;
using System.Collections.Generic;
using System.Diagnostics;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;

using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;
using Microsoft.Xna.Framework.Storage;

using KoiX;
using KoiX.Managers;
using KoiX.Scenes;
using KoiX.UI;

using Boku.Common;

namespace Boku
{
    /// <summary>
    /// Something to wrap around everything.
    /// </summary>
    public class Main
    {
        #region Members

        static public float SceneSwitchTime = 0.3f;

        // Template Scenes.
        BlankScene blankScene;          // Not used.
        NullScene nullScene;            // Placeholder for SceneManager when old-style scenes are in control.

        // Real Scenes.
        StartupScene startupScene;          // Shown during initial loading.
        IntroVideoScene introVideoScene;
        MainMenuScene mainMenuScene;
        HomeMenuScene homeMenuScene;

        LoadLevelScene loadLevelLocalScene;
        LoadLevelScene loadLevelCommunityScene;
        LoadLevelScene loadLevelAttachingScene;
        
        RunSimScene runSimScene;
        EditWorldScene editWorldScene;

        // These scenes are all multi-page settings scenes.
        // All the underlying page scenes are instantiated under them.
        // They are set up so that this first scene can optionally contain
        // a menu of shortcuts to the underlying pages.  This would be used
        // in the case where there are many pages.  Otherwise, for the simple
        // case, these scenes just fall through to the first page.
        HelpMenuScene helpMenuScene;
        OptionsMenuScene optionsMenuScene;
        WorldSettingsMenuScene worldSettingsMenuScene;
        ObjectSettingsMenuScene objectSettingsMenuScene;


        #endregion

        #region Accessors
        #endregion

        #region Public

        /// <summary>
        /// c'tor
        /// </summary>
        public Main()
        {
            // Init Scenes
            blankScene = new BlankScene();
            nullScene = new NullScene();

            startupScene = new StartupScene();
            introVideoScene = new IntroVideoScene();
            mainMenuScene = new MainMenuScene();
            homeMenuScene = new HomeMenuScene();

            loadLevelLocalScene = new LoadLevelScene("LoadLevelLocalScene", LevelBrowserType.Local);
            loadLevelCommunityScene = new LoadLevelScene("LoadLevelCommunityScene", LevelBrowserType.Community);
            loadLevelAttachingScene = new LoadLevelScene("LoadLevelAttachingScene", LevelBrowserType.Local, isLinking: true);

            runSimScene = new RunSimScene();
            editWorldScene = new EditWorldScene();

            helpMenuScene = new HelpMenuScene();
            optionsMenuScene = new OptionsMenuScene();
            worldSettingsMenuScene = new WorldSettingsMenuScene();
            objectSettingsMenuScene = new ObjectSettingsMenuScene();

            // Start at the beginning.
            SceneManager.SwitchToScene(startupScene);

        }   // end of c'tor


        public void Update()
        {
        }   // end of Update()

        public void Render()
        {
            GraphicsDevice device = XNAControl.Device;

            device.Clear(Color.HotPink);
        }   // end of Render

        #endregion

        #region Internal
        #endregion
    }   // end of class Main
}   // end of namespace Boku
