// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

//#define RENDER_TIMERS
//#define UPDATE_TIMERS
#define DISPLAY_FPS
//#define DISPLAY_BUDGETS

#if DEBUG
// Log content filenames as they get loaded.
//#define LOG_CONTENT_LOADS
#endif


using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Text;
using System.Threading;

#if NETFX_CORE
    using Windows.System;
    using Windows.UI.ApplicationSettings;
    using Windows.UI.Popups;
#else
    using System.Windows.Forms;
#endif
using System.IO;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Storage;

using KoiX;
using KoiX.Input;
using KoiX.Managers;
using KoiX.Scenes;
using KoiX.Text;
using KoiX.UI;

using Boku.Base;
using Boku.Fx;
using Boku.Common;
using Boku.Common.Sharing;
using Boku.Common.ParticleSystem;
using Boku.Common.TutorialSystem;
using Boku.Common.Xml;
using Boku.Programming;
using Boku.Input;
using Boku.SimWorld;
using Boku.SimWorld.Path;
using Boku.SimWorld.Terra;
using Boku.UI2D;
using Boku.Audio;

using BokuShared;
using Boku.Common.Localization;


namespace Boku
{
    /// <summary>
    /// Boku, the story of a boy and his code.
    /// </summary>
#if NETFX_CORE
    partial class BokuGame : Microsoft.Xna.Framework.Game
#else
    partial class BokuGame
#endif
    {
        // instrumentation
        static object sessionTimerInstrument;


        public static int ThreadId;
        public static bool Running = true;

        // Note: All these paths do NOT include Settings.MediaPath.
        public static string LevelsPath = @"Xml\Levels\";
        public static string DownloadsPath = LevelsPath + @"Downloads\";
        public static string DownloadsStuffPath = LevelsPath + @"Downloads\Stuff\";
        public static string UnDoPath = LevelsPath + @"MyWorlds\AutoSave" + Storage4.UniqueMachineID + @"\";
        public static string MyWorldsPath = LevelsPath + @"MyWorlds\";
        public static string MyWorldsStuffPath = LevelsPath + @"MyWorlds\Stuff\";
        public static string BuiltInWorldsPath = LevelsPath + @"BuiltInWorlds\";
        public static string SharedStuffPath = LevelsPath + @"Stuff\";
        public static string TerrainPath = SharedStuffPath + @"TerrainHeightMaps\";

        private static Audio.Audio gameAudio;
        public static GameListManager gameListManager;   // Holds the top-level lists for all game objects.
        protected static bool listDirty = false;

        // SGI_MOD - picture support
        private static PictureManager pictureManager = new PictureManager();

#if NETFX_CORE
        // For Win8 we'll try and replace all calls to WinKeyboard
        // with ones from the KeyboardInput class.
#else
        /// <summary>
        /// Winkeyboard object for reading processed keyboard input.
        /// </summary>
        public WinKeyboard winKeyboard = null;
#endif        

        //
        // Scenes, modes, whatever you want to call them.
        //
        public ShaderGlobals shaderGlobals;
        public SurfaceDict Surfaces;
        public VideoOutput videoOutput;
        public InGame inGame;
        public LoadLevelMenu loadLevelMenu;      // The menu formerly known as BigBin.
        public LoadLevelMenu community;
        public ProgressScreen progressScreen;

        public static BokuGame bokuGame = null;             // Provide a ref to easily get back to the game object.

        public Random rnd = new Random();                   // Just because it's useful.

        private static bool logon = false;                  // should we have the user give us their username?

        public static bool bMarsMode = false;               //For special JPL/NASA main menu.

        // This is the size that we're targetting for rendering.  Normally this is the same
        // as the viewport size.  The exception is when the tutorial system in active.  In
        // that case this will be smaller in the Y direction.
        private Vector2 screenSize;
        private Vector2 screenPosition = Vector2.Zero;

        public static bool hwSupportsReach = false;
        public static bool hwSupportsHiDef = false;
        public static bool hidef = false;                   // Are we actually running in HiDef profile?

        #region Accessors

        public GraphicsDevice GraphicsDevice
        {
#if NETFX_CORE
            get { return graphics.GraphicsDevice; }
#else
            get { return XNAControl.Device; }
#endif
        }

        /// <summary>
        /// Is the game running in HiDef?  Based on a combination 
        /// of the HW capability and the user preference.  This
        /// should be checked in game rather than PreferReach.
        /// </summary>
        public static bool HiDefProfile
        {
            get { return hidef; }
        }

        /// <summary>
        /// This is the size that we're targetting for rendering.  Normally this is the same
        /// as the viewport size.  The exception is when the tutorial system in active.  In
        /// that case this will be smaller in the Y direction.
        /// </summary>
        public static Vector2 ScreenSize
        {
            get { return bokuGame.screenSize; }
            set
            {
                if (bokuGame.screenSize != value)
                {
                    bokuGame.screenSize = value;
                    // If in Reach mode we want to limit the screenSize 
                    // to 2048 in either direction since that's the 
                    // texture (and rendertarget) size limit.
                    // Try to keep proper aspect ratio.  In HiDef the limit
                    // is 4096.
                    if (HiDefProfile)
                    {
                        float scale = 4096.0f / bokuGame.screenSize.X;
                        scale = MathHelper.Min(scale, 4096.0f / bokuGame.screenSize.Y);
                        if (scale < 1)
                        {
                            bokuGame.screenSize.X = (int)Math.Min(bokuGame.screenSize.X * scale, 4096);
                            bokuGame.screenSize.Y = (int)Math.Min(bokuGame.screenSize.Y * scale, 4096);
                        }
                    }
                    else
                    {
                        float scale = 2048.0f / bokuGame.screenSize.X;
                        scale = MathHelper.Min(scale, 2048.0f / bokuGame.screenSize.Y);
                        if (scale < 1)
                        {
                            bokuGame.screenSize.X = (int)Math.Min(bokuGame.screenSize.X * scale, 2048);
                            bokuGame.screenSize.Y = (int)Math.Min(bokuGame.screenSize.Y * scale, 2048);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// The position to render the screen. Normally 0,0 except
        /// when tutorial mode is taking up the top of the screen.
        /// </summary>
        public static Vector2 ScreenPosition
        {
            get { return bokuGame.screenPosition; }
            set 
            {
                if (bokuGame.screenPosition != value)
                {
                    bokuGame.screenPosition = value;
                    // Changing the screen position value can cause the 
                    // adjusted mouse position to jump making it look like
                    // the user touched the mouse causing the input mode
                    // to switch.  So, tell the mouse input class to not
                    // register as touched for the next couple of frames.
                    LowLevelMouseInput.IgnoreTouched();
                }
            }
        }
        
#if !NETFX_CORE
        bool isMouseVisible = true;
        public bool IsMouseVisible
        {
            get { return isMouseVisible; }
            set
            {
                if (isMouseVisible != value)
                {
                    isMouseVisible = value;
                    if (isMouseVisible)
                    {
                        Cursor.Show();
                    }
                    else
                    {
                        Cursor.Hide();
                    }
                }
            }
        }
#endif

#if !NETFX_CORE
        public bool IsActive
        {
            get { return XNAControl.Instance.Focused; }
        }
#endif

        public static bool objectListDirty
        {
            get { return listDirty; }
            set { listDirty = value; }
        }
        public static Boku.Properties.Settings Settings
        {
            get 
            { 
                return Boku.Properties.Settings.Default; 
            }
        }
        // SGI MOD - picture support
        public static PictureManager PictureManager
        {
            get { return pictureManager; }
        }
        public static Audio.Audio Audio
        {
            get { return gameAudio; }
        }

#if LOG_CONTENT_LOADS
        static Dictionary<string, int> loadedContent = new Dictionary<string, int>();
#endif

        public static T Load<T>(string path)
        {
            LogContentFileLoaded(path);

            T resource = default(T);

            // If HiDef and an effect, try HiDef path first.
            if (!BokuSettings.Settings.PreferReach && typeof(T) == typeof(Effect))
            {
                string hiDefPath = path.Replace("Content", "ContentHiDef");
                try
                {
                    resource = ContentLoader.ContentManager.Load<T>(hiDefPath);
                }
                catch
                {
                    // Nothing to do here, shader must not be in HiDef build.
                }
            }

            if (resource == null)
            {
                resource = ContentLoader.ContentManager.Load<T>(path);
            }

            if (resource is Texture2D)
            {
                // Protect against breakage on older video cards.
                Texture2D tex = resource as Texture2D;

                if (tex.LevelCount != 1)
                {
                    if (!MyMath.IsPowerOfTwo(tex.Width) || !MyMath.IsPowerOfTwo(tex.Height))
                    {
                        throw new Exception("Mipmapped texture is non-power-2 in size: " + path);
                    }
                }
                if (tex.Format == SurfaceFormat.Dxt1)
                {
                    if (!MyMath.IsPowerOfTwo(tex.Width) || !MyMath.IsPowerOfTwo(tex.Height))
                    {
                        throw new Exception("DXT compressed texture is non-power-2 in size: " + path);
                    }
                }
            }

            return resource;
        }

        public static void LogContentFileLoaded(string path)
        {
#if LOG_CONTENT_LOADS
            if (!loadedContent.ContainsKey(path))
                loadedContent.Add(path, 0);
#endif
        }
        /*
        public static GraphicsDeviceManager Graphics
        {
            get
            {
#if NETFX_CORE
                //Debug.Assert(false, "How to get current thread id in WinRT?");
#else
                Debug.Assert(
                    ThreadId == System.Threading.Thread.CurrentThread.ManagedThreadId,
                    "The graphics device may only be accessed from the main application thread."
                );
#endif
                return graphics;
            }
        }
        */
        public static bool IsWidescreen
        {
            get { return KoiLibrary.GraphicsDevice.Viewport.AspectRatio > 1.34f; }
        }

        /// <summary>
        /// Does the current device support shader 3.0 or better?
        /// </summary>

        // TODO (****) No longer valid in Reach vs HiDef world.  Need to test Reach against x600 card.
        public static bool RequiresPowerOf2
        {
            //get { return device.GraphicsDeviceCapabilities.TextureCapabilities.RequiresPower2; }
            get { return false; }
        }

        /// <summary>
        /// Does the HW support HiDef?
        /// </summary>
        public bool HwSupportsHiDef
        {
            get { return hwSupportsHiDef; }
        }

        /// <summary>
        /// Does the HW support Reach?
        /// </summary>
        public bool HwSupportsReach
        {
            get { return hwSupportsReach; }
        }

        public static bool Logon
        {
            get { return logon; }
            set { logon = value; }
        }

        #endregion

        static BokuGame()
        {
#if NETFX_CORE
            Debug.Assert(false, "What's the WinRT approved way of getting a thread id?");
#else
            ThreadId = System.Threading.Thread.CurrentThread.ManagedThreadId;
#endif
        }

        public BokuGame()
        {
            bokuGame = this;
            Auth.Init();

            //Guide.SimulateTrialMode = true;

#if NETFX_CORE
            //Debug.Assert(false, "argh");
#else
            winKeyboard = new WinKeyboard(MainForm.Instance);
#endif
            
            // TODO (****) *** Do we need this code any more?
#if NETFX_CORE
            InitializeComponent();
#endif

#if NETFX_CORE
            SettingsPane curSettingsPane = SettingsPane.GetForCurrentView();
            curSettingsPane.CommandsRequested += OnCommandsRequested;
#endif
        }   // end of BokuGame c'tor

#if NETFX_CORE
        void OnCommandsRequested(SettingsPane sender, SettingsPaneCommandsRequestedEventArgs args)
        {
            UICommandInvokedHandler handler = new UICommandInvokedHandler(OnSettingsCommand);
                
            SettingsCommand privacyCommand = new SettingsCommand("Privacy", "Privacy", OnSettingsCommand);
            args.Request.ApplicationCommands.Add(privacyCommand);
        }

        void OnSettingsCommand(Windows.UI.Popups.IUICommand command)
        {
            if (command.Label.Equals("Privacy"))
            {
                Uri uri = new Uri(Program2.SiteOptions.KGLUrl + @"/Link/PrivacyStatement");
                Launcher.LaunchUriAsync(uri);
            }
        }
#endif

        public void Window_ClientSizeChanged(object sender, EventArgs e)
        {
#if !NETFX_CORE
            Form form = StartupForm.ActiveForm;
            if (form.WindowState == FormWindowState.Maximized)
            {
                // TODO (****) *** Need to do anything here???
                /*
                BokuGame.Graphics.ToggleFullScreen();
                BokuGame.Graphics.PreferredBackBufferWidth = 1600;
                BokuGame.Graphics.PreferredBackBufferHeight = 1200;
                BokuGame.Graphics.ApplyChanges();
                */
            }
#endif
        }

        //
        // Overrides from base game object starting with initialization.
        //

#if NETFX_CORE
        /*
        protected override void Initialize()
        {
            //base.Initialize();
        }
        */
        /// <summary>
        /// For non-graphics initialization.
        /// </summary>
        public void OldInitialize()
#else
        public void Initialize()
#endif
        {
            /// Steve, uncomment this first line to simulate having 508,313,600MB less video memory
            //InGame.DebugCheckVideoMem(508313600); // Steve's line
            //InGame.DebugCheckVideoMem(0xffffffff); // This line will check how much video memory you really have
            //InGame.DebugCheckVideoMem(742580224); // This line brings my video memory down to 128MB (I think)

            // Queue content loads for processing in the update loop.
            ContentLoader.DefaultImmediate = false;
            ContentLoader.OnLoadComplete += StartupLoadComplete;

            // Instrument the length of this Boku session.
            sessionTimerInstrument = Instrumentation.StartTimer(Instrumentation.TimerId.BokuSession);

            //start instrumentation clock
            Time.startActiveInstrumentationClock();

            // If there's already an instance of Kodu running, exit this one.
            if (bokuGame == null)
            {
#if NETFX_CORE
                Windows.UI.Xaml.Application.Current.Exit();
#else
                BokuGame.bokuGame.Exit();
#endif
            }

            // Set the window title bar text to display program name and version
#if NETFX_CORE
            Window.Title = Strings.Localize("shareHub.appName") + " (" + Program2.ThisVersion.ToString() + ", " + Program2.SiteOptions.Product + ")";
#else
            MainForm.Instance.Text = Strings.Localize("shareHub.appName") + " (" + Program2.ThisVersion.ToString() + ", " + Program2.SiteOptions.Product + ")";
#endif

            // Instrument the Boku version number.
            Instrumentation.RecordDataItem(Instrumentation.DataItemId.BokuVersion, Program2.ThisVersion.ToString());
            Instrumentation.RecordDataItem(Instrumentation.DataItemId.UpdateCode, Program2.UpdateCode.ToString());

            // Instrument the OS version string.
#if NETFX_CORE
            Instrumentation.RecordDataItem(Instrumentation.DataItemId.OperatingSystem, "WinRT");
#else
            Instrumentation.RecordDataItem(Instrumentation.DataItemId.OperatingSystem, Environment.OSVersion.VersionString);
#endif

#if NETFX_CORE
#else
            // Instrument the graphics device information.
            string gfxString = String.Format("{0}, Driver: not supported in 4.0", GraphicsAdapter.DefaultAdapter.Description);
            Instrumentation.RecordDataItem(Instrumentation.DataItemId.GraphicsAdapter, gfxString);
#endif

            BokuGame.gameAudio = new Audio.Audio(BokuSettings.Settings.Audio);

            //KeyboardInput.Init();
            //GamePadInput.Init();
            //TouchInput.Init();
            Brush2DManager.Init();
#if !NETFX_CORE
            SysFont.Init();
#endif
            Theme.Init();
            AuthUI.Init();
            GUIButtonManager.Init();

            gameListManager = new GameListManager();

#if NETFX_CORE
            GraphicsDevice.DeviceReset += DeviceResetHandler;
#else
            // TODO (****) *** Where to hook up device reset handler???
#endif

            ScreenSize = new Vector2(KoiLibrary.GraphicsDevice.Viewport.Width, KoiLibrary.GraphicsDevice.Viewport.Height);
            ScreenPosition = Vector2.Zero;

            Instrumentation.RecordDataItem(Instrumentation.DataItemId.ScreenResolution, screenSize.ToString());

            // Instrument BokuSettings
            MemoryStream stream = new MemoryStream();
            BokuSettings.Save(stream);
            stream.Position = 0;
#if NETFX_CORE
            Debug.Assert(false, "Not impl");
            string settingsStr = "";
#else
            string settingsStr = Encoding.ASCII.GetString(stream.ToArray());
#endif
            Instrumentation.RecordDataItem(Instrumentation.DataItemId.SettingsXml, settingsStr);

#if !NETFX_CORE
            // Call LoadContent()
#endif

        }   // end of BokuGame Initialize()

        public void LoadSurfaces()
        {
            Surfaces = SurfaceDict.Load(@"Content\Xml\Actors\SurfaceDict.xml", XnaStorageHelper.Instance);
        }


        // Loads static items that don't implement INeedsDeviceReset
        class StaticContent : INeedsDeviceReset
        {
            public void LoadContent(bool immediate)
            {
                Localizer.LoadContent(immediate);
                SharedX.LoadContent(immediate);
                BokuGame.gameAudio.LoadContent(immediate);
                CardSpace.LoadContent(immediate);
                Scoreboard.LoadContent(immediate);
                Boku.Programming.Help.LoadContent(immediate);
                ButtonTextures.LoadContent(immediate);
                HelpOverlay.LoadContent(immediate);
                ThoughtBalloonManager.LoadContent(immediate);
                ToolTipManager.LoadContent(immediate);
                Hints.LoadContent(immediate);
                ShowBudget.LoadContent(immediate);
                HealthBarManager.LoadContent(immediate);
                DistortionManager.LoadContent(immediate);
                FirstPersonEffectMgr.LoadContent(immediate);
                Brush2DManager.LoadContent(immediate);
                Tile.LoadContent(immediate);
                AsyncThumbnail.LoadContent(immediate);
                InGame.UnDoStack.LoadContent(immediate);
                GamePadInput.LoadContent(immediate);
                Terrain.LoadShared();
#if NETFX_CORE
                VirtualKeyboard.LoadContent(immediate);
#endif
                MouseMenu.LoadContent(immediate);
                TutorialManager.LoadContent(immediate);
                GUIButtonManager.LoadContent(immediate);
                TouchVirtualController.LoadContent(immediate);
                AuthUI.LoadContent(immediate);
            }

            public void InitDeviceResources(GraphicsDevice device)
            {
                SharedX.InitDeviceResources(device);
                BokuGame.gameAudio.InitDeviceResources(device);
                CardSpace.InitDeviceResources(device);
                Scoreboard.InitDeviceResources(device);
                Boku.Programming.Help.InitDeviceResources(device);
                ButtonTextures.InitDeviceResources(device);
                HelpOverlay.InitDeviceResources(device);
                ThoughtBalloonManager.InitDeviceResources(device);
                ToolTipManager.InitDeviceResources(device);
                Hints.InitDeviceResources(device);
                ShowBudget.InitDeviceResources(device);
                HealthBarManager.InitDeviceResources(device);
                DistortionManager.InitDeviceResources(device);
                FirstPersonEffectMgr.InitDeviceResources(device);
                Brush2DManager.InitDeviceResources(device);
                Tile.InitDeviceResources(device);
                AsyncThumbnail.InitDeviceResources(device);
                InGame.UnDoStack.InitDeviceResources(device);
                GamePadInput.InitDeviceResources(device);
                Terrain.InitSharedDeviceResources(device);
                MouseMenu.InitSharedDeviceResources(device);
                TutorialManager.InitDeviceResources(device);
                GUIButtonManager.InitDeviceResources(device);
                TouchVirtualController.InitDeviceResources(device);

                // These must load after the models, especially the trees and flowers,
                // for correct hookup of animations.
                Road.LoadContent(true);
                Road.InitDeviceResources(device);
                RoadStdRenderObj.LoadContent(true);
                RoadStdRenderObj.InitDeviceResources(device);

                // Init other stuff...

                WayPoint.Init(true);
                Foley.Init();
                AuthUI.InitDeviceResources(device);
            }

            public void UnloadContent()
            {
                Localizer.UnloadContent();
                SharedX.UnloadContent();
                BokuGame.gameAudio.UnloadContent();
                CardSpace.UnloadContent();
                Scoreboard.UnloadContent();
                Boku.Programming.Help.UnloadContent();
                ButtonTextures.UnloadContent();
                HelpOverlay.UnloadContent();
                ThoughtBalloonManager.UnloadContent();
                ToolTipManager.UnloadContent();
                Hints.UnloadContent();
                ShowBudget.UnloadContent();
                HealthBarManager.UnloadContent();
                DistortionManager.UnloadContent();
                FirstPersonEffectMgr.UnloadContent();
                Brush2DManager.UnloadContent();
                Tile.UnloadContent();
                Road.UnloadContent();
                RoadStdRenderObj.UnloadContent();
                AsyncThumbnail.UnloadContent();
                InGame.UnDoStack.UnloadContent();
                GamePadInput.UnloadContent();
                Terrain.UnloadShared();
                MouseMenu.UnloadContent();
                TutorialManager.UnloadContent();
                GUIButtonManager.UnloadContent();
                TouchVirtualController.UnloadContent();
                AuthUI.UnloadContent();

                ActorFactory.Clear();

                WayPoint.Fini();
            }

            public void DeviceReset(GraphicsDevice device)
            {
                SharedX.DeviceReset(device);
                BokuGame.gameAudio.DeviceReset(device);
                CardSpace.DeviceReset(device);
                Scoreboard.DeviceReset(device);
                Boku.Programming.Help.DeviceReset(device);
                ButtonTextures.DeviceReset(device);
                HelpOverlay.DeviceReset(device);
                ThoughtBalloonManager.DeviceReset(device);
                ToolTipManager.DeviceReset(device);
                Hints.DeviceReset(device);
                ShowBudget.DeviceReset(device);
                HealthBarManager.DeviceReset(device);
                DistortionManager.DeviceReset(device);
                FirstPersonEffectMgr.DeviceReset(device);
                Brush2DManager.DeviceReset(device);
                Tile.DeviceReset(device);
                AsyncThumbnail.DeviceReset(device);
                InGame.UnDoStack.DeviceReset(device);
                GamePadInput.DeviceReset(device);
                //Terrain.LoadShared();
                MouseMenu.DeviceReset(device);
                TutorialManager.DeviceReset(device);
                GUIButtonManager.DeviceReset(device);
                TouchVirtualController.DeviceReset(device);
                AuthUI.DeviceReset(device);
            }
        }
        StaticContent singletonContentLoader = new StaticContent();

        private void StartupLoadComplete()
        {
            // Future content loads should happen immediately, instead of
            // being queued for process in the update loop.
            ContentLoader.DefaultImmediate = true;
            ContentLoader.OnLoadComplete -= StartupLoadComplete;

            StartupScene scene = SceneManager.CurrentScene as StartupScene;
            Debug.Assert(scene != null);
            scene.OnDoneLoadingContent();
        }

        // The first time LoadContent is called by the framework, this will be true. For subsequent calls, it will be false.
        bool firstLoadContent = true;

#if NETFX_CORE
        int loadStep = 0;
        protected bool LoadContentWinRT()
        {
            bool doneLoading = false;

            switch (loadStep)
            {
                case 0:
                    // If we're here and we're not starting up then the device was lost and
                    // we must do a complete reload of all content. Flush cached textures,
                    // geometry, etc. from the content manager so they'll be reloaded from disk.
                    ContentLoader.ContentManager.Unload();
                    break;
                case 1:
                    // Moved here to clear up some order of initialization issues.
                    // Would be nice to clean this up some time.
                    HelpOverlay.Init();
                    break;
                case 2:
                    TweakScreenHelp.Init();
                    break;
                case 3:
                    // CardSpace must load before InGame so that InGame can generate the AddItem pie selector tiles.
                    CardSpace.LoadContent(true);
                    break;
                case 4:
                    // TODO (****) Figure out why this is needed.  For WinRT we're loading SSQuad early
                    // so that we can display the loading screen.  But that causes an error in CardSpace.
                    // By Unloading SSQuad here we avoid the error.  No clue what's going on.
                    ScreenSpaceQuad.GetInstance().UnloadContent();
                    CardSpace.InitDeviceResources(GraphicsDevice);
                    break;
                case 5:
                    if (firstLoadContent)
                    {
                        // Load the static actors from the actors.xml list. This must
                        // be done before the add item menus are initialized.
                        ActorManager.LoadActors();
                    }
                    break;
                case 6:
                    if (firstLoadContent)
                    {
                        // Create and activate the shader globals object.  We want
                        // this to be the first object "rendered" each frame.
                        shaderGlobals = new ShaderGlobals();
                        BokuGame.gameListManager.AddObject(shaderGlobals);
                    }
                    break;
                case 7:
                    if (firstLoadContent)
                    {
                        LoadSurfaces();
                    }
                    break;
                case 8:
                    if (firstLoadContent)
                    {
                        // Create the scenes and add them to the object list.
                        titleScreenMode = new TitleScreenMode();
                        BokuGame.gameListManager.AddObject(titleScreenMode);
                    }
                    break;
                case 9:
                    if (firstLoadContent)
                    {
                        progressScreen = new ProgressScreen();
                    }
                    break;
                case 10:
                    if (firstLoadContent)
                    {
                        ThoughtBalloonManager.Init();
                        ToolTipManager.Init();
                        Hints.Init();
                    }
                    break;
                case 11:
                    if (firstLoadContent)
                    {
                        inGame = new InGame();
                        BokuGame.gameListManager.AddObject(inGame);
                    }
                    break;
                case 12:
                    if (firstLoadContent)
                    {
                        mainMenu = new MainMenu();
                        BokuGame.gameListManager.AddObject(mainMenu);
                    }
                    break;
                case 13:
                    if (firstLoadContent)
                    {
                        videoOutput = new VideoOutput();
                        BokuGame.gameListManager.AddObject(videoOutput);
                    }
                    break;
                case 14:
                    if (firstLoadContent)
                    {
                        helpScreens = new HelpScreens();
                        BokuGame.gameListManager.AddObject(helpScreens);
                    }
                    break;
                case 15:
                    if (firstLoadContent)
                    {
                        community = new LoadLevelMenu(LevelBrowserType.Community);
                        BokuGame.gameListManager.AddObject(community);
                    }
                    break;
                case 16:
                    if (firstLoadContent)
                    {
                        loadLevelMenu = new LoadLevelMenu(LevelBrowserType.Local);
                        BokuGame.gameListManager.AddObject(loadLevelMenu);
                    }
                    break;
                case 17:
                    if (firstLoadContent)
                    {
                        miniHub = new MiniHub();
                        BokuGame.gameListManager.AddObject(miniHub);
                    }
                    break;
                case 18:
                    if (firstLoadContent)
                    {
                        sharingScreen = new LoadLevelMenu(LevelBrowserType.Sharing);
                        BokuGame.gameListManager.AddObject(sharingScreen);
                    }
                    break;
                case 19:
                    if (firstLoadContent)
                    {
                        // Activate the title sceen. It will display progress while
                        // additional assets are loaded in the background.
                        shaderGlobals.Activate();
                        titleScreenMode.Activate();

                        // Last item in firstLoadContent
                        firstLoadContent = false;
                    }
                    break;
                case 20:
                    // Init the utils object.
                    Utils.Init(GraphicsDevice);
                    break;
                case 21:
                    // Synchronously load resources necessary to display the title
                    // and progress screens while we background-load queued content.
                    BokuGame.Load(shaderGlobals, true);
                    BokuGame.Load(SimpleTexturedQuad.GetInstance(), true);
                    BokuGame.Load(CameraSpaceQuad.GetInstance(), true);
                    BokuGame.Load(ScreenSpaceQuad.GetInstance(), true);
                    BokuGame.Load(ScreenSpace3PanelQuad.GetInstance(), true);
                    BokuGame.Load(titleScreenMode, true);
                    BokuGame.Load(progressScreen, true);
                    break;
                case 22:
                    // Static items that don't implement INeedsDeviceReset
                    BokuGame.Load(singletonContentLoader);
                    break;
                case 23:
                    // Sphere object
                    BokuGame.Load(Sphere.GetInstance());

                    // Actors, load all actor models into BokuGame
                    ActorManager.LoadModels();
                    break;
                case 24:
                    // Scenes
                    BokuGame.Load(mainMenu);
                    BokuGame.Load(videoOutput);
                    BokuGame.Load(helpScreens);
                    BokuGame.Load(sharingScreen);
                    BokuGame.Load(community);
                    BokuGame.Load(loadLevelMenu);
                    BokuGame.Load(inGame);
                    BokuGame.Load(miniHub);
                    break;
                case 25:
                    base.LoadContent();
                    break;
                default:
                    doneLoading = true;
                    break;
            }

            ++loadStep;
            // If done, reset for next time.
            if(doneLoading)
            {
                loadStep = 0;
            }

            return doneLoading;
        }   // end of BokuGame LoadContentWinRT()
#else

        public void LoadContent()
        {
            //Debug.WriteLine("Begin LoadContent");

            // If we're here and we're not starting up then the device was lost and
            // we must do a complete reload of all content. Flush cached textures,
            // geometry, etc. from the content manager so they'll be reloaded from disk.
            ContentLoader.ContentManager.Unload();

            // Moved here to clear up some order of initialization issues.
            // Would be nice to clean this up some time.
            HelpOverlay.Init();
            TweakScreenHelp.Init();

            // CardSpace must load before InGame so that InGame can generate the AddItem pie selector tiles.
            CardSpace.LoadContent(true);
            CardSpace.InitDeviceResources(KoiLibrary.GraphicsDevice);

            if (firstLoadContent)
            {
                firstLoadContent = false;

                // Load the static actors from the actors.xml list. This must
                // be done before the add item menus are initialized.
                ActorManager.LoadActors();

                // Create the shader globals object.  We want this
                // to be the first object "rendered" each frame.
                shaderGlobals = new ShaderGlobals();

                LoadSurfaces();

                progressScreen = new ProgressScreen();

                ThoughtBalloonManager.Init();
                ToolTipManager.Init();
                Hints.Init();

                inGame = new InGame();
                BokuGame.gameListManager.AddObject(inGame);

                videoOutput = new VideoOutput();
                BokuGame.gameListManager.AddObject(videoOutput);

                community = new LoadLevelMenu(LevelBrowserType.Community);
                BokuGame.gameListManager.AddObject(community);

                loadLevelMenu = new LoadLevelMenu(LevelBrowserType.Local);
                BokuGame.gameListManager.AddObject(loadLevelMenu);
            }

            // Init the utils object.
            Utils.Init(GraphicsDevice);

            // Synchronously load resources necessary to display the title
            // and progress screens while we background-load queued content.
            BokuGame.Load(shaderGlobals, true);
            BokuGame.Load(SimpleTexturedQuad.GetInstance(), true);
            BokuGame.Load(CameraSpaceQuad.GetInstance(), true);
            BokuGame.Load(ScreenSpaceQuad.GetInstance(), true);
            BokuGame.Load(ScreenSpace3PanelQuad.GetInstance(), true);
            BokuGame.Load(progressScreen, true);

            // Static items that don't implement INeedsDeviceReset
            BokuGame.Load(singletonContentLoader);

            // Sphere object
            BokuGame.Load(Sphere.GetInstance());

            // Actors, load all actor models into BokuGame
            ActorManager.LoadModels();

            // Scenes
            BokuGame.Load(videoOutput);
            BokuGame.Load(community);
            BokuGame.Load(loadLevelMenu);
            BokuGame.Load(inGame);

            //Debug.WriteLine("End LoadContent");

        }   // end of BokuGame LoadContent()
#endif

        public void UnloadContent()
        {
            //Debug.WriteLine("Begin UnloadContent");

            // Static items that don't implement INeedsDeviceReset
            BokuGame.Unload(singletonContentLoader);

            // Shader globals
            BokuGame.Unload(shaderGlobals);

            // Quads
            BokuGame.Unload(SimpleTexturedQuad.GetInstance());
            BokuGame.Unload(CameraSpaceQuad.GetInstance());
            BokuGame.Unload(ScreenSpaceQuad.GetInstance());
            BokuGame.Unload(ScreenSpace3PanelQuad.GetInstance());

            // Sphere
            BokuGame.Unload(Sphere.GetInstance());

            // ActorManager
            ActorManager.UnloadModels();

            // Scenes
            BokuGame.Unload(videoOutput);
            BokuGame.Unload(community);
            BokuGame.Unload(loadLevelMenu);
            BokuGame.Unload(inGame);

            //Debug.WriteLine("End UnloadContent");

        }   // end of BokuGame UnloadContent()


        public void DeviceResetHandler(object sender, EventArgs e)
        {
            //Debug.WriteLine("Begin DeviceReset");

            // If we were in the editor when the reset happened, shut it down.  We need
            // to do this before stuff starts getting unloaded since the serialization
            // of creatable brains fails if we try it later.  No clue why.
            if (InGame.inGame.Editor.Active)
            {
                InGame.inGame.Editor.Hide(false);
                InGame.inGame.Refresh(BokuGame.gameListManager.updateList, BokuGame.gameListManager.renderList);
            }

            BokuGame.DeviceResetIfLoaded(shaderGlobals);
            BokuGame.DeviceResetIfLoaded(SimpleTexturedQuad.GetInstance());
            BokuGame.DeviceResetIfLoaded(CameraSpaceQuad.GetInstance());
            BokuGame.DeviceResetIfLoaded(ScreenSpaceQuad.GetInstance());
            BokuGame.DeviceResetIfLoaded(ScreenSpace3PanelQuad.GetInstance());
            BokuGame.DeviceResetIfLoaded(progressScreen);

            // Static items that don't implement INeedsDeviceReset
            BokuGame.DeviceResetIfLoaded(singletonContentLoader);

            // Sphere object
            BokuGame.DeviceResetIfLoaded(Sphere.GetInstance());

            // ActorManger
            ActorManager.ModelsResetIfLoaded();

            // Scenes
            BokuGame.DeviceResetIfLoaded(videoOutput);
            BokuGame.DeviceResetIfLoaded(community);
            BokuGame.DeviceResetIfLoaded(loadLevelMenu);
            BokuGame.DeviceResetIfLoaded(inGame);

            //Debug.WriteLine("End DeviceReset");
        }

        public void BeginRun()
        {
        }   // end of BeginRun()


        // Create of couple of timer objects mostly just to show how they work.
        // I've indented the Refresh output so that it's easier to distinguish
        // from the Update output.
#if RENDER_TIMERS
        PerfTimer renderTimer = new PerfTimer("Render");                    // Will update every 1 second (the default).
#endif
#if UPDATE_TIMERS
        PerfTimer updateTimer = new PerfTimer("Update");                    // Will update every 1 second (the default).
        PerfTimer refreshTimer = new PerfTimer("\t\t\t\t\tRefresh", -1.0f); // Will update immediately.

//        PerfTimer updateGameListTimer = new PerfTimer("UpdateGameList");
//        PerfTimer updateTwitchTimer = new PerfTimer("UpdateTwitch");
//        PerfTimer updateGameTimer = new PerfTimer("UpdateTimer");
#endif

        public static void CheckRefresh()
        {
            if (objectListDirty)
            {
                objectListDirty = false;
#if UPDATE_TIMERS
                //refreshTimer.Start();
#endif
                gameListManager.Refresh();
#if UPDATE_TIMERS
                //refreshTimer.Stop();
#endif
            }
        }

#if NETFX_CORE
        // For WinRT cert we need to get away from the splash screen
        // as soon as possible.  This is a hack to get the loading
        // screen up for 1 frame and then do Initialization.
        int startupFrameCount = 3;
        TitleScreen titleScreen;
#endif

#if NETFX_CORE
        /// <summary>
        /// Only used by Win8 version
        /// </summary>
        /// <param name="gameTime"></param>
        protected override void Update(GameTime gameTime)
        {
            Update();
        }

        /// <summary>
        /// Only used by Win8 version
        /// </summary>
        /// <param name="gameTime"></param>
        protected override void Draw(GameTime gameTime)
        {
            Draw();
        }
#else
        /// <summary>
        /// Only used by XNAControl desktop version.
        /// </summary>
        public void DoFrame()
        {
            Update();
            Draw();
            ScreenGrab();
        }
#endif

        //
        // These methods make up the main game loop.
        //
        public void Update()
        {
#if NETFX_CORE

            // Hack to work within Windows store guidelines for startup perf.
            // We push the Initializing and Content Loading to happen after
            // the Update/Render loop is started.  It still takes the same amount
            // of time but passes cert.
            if (startupFrameCount > 0)
            {
                Time.Update();
                if (titleScreen == null)
                {
                    titleScreen = new TitleScreen();
                    titleScreen.LoadContent(true);
                    ScreenSpaceQuad.GetInstance().LoadContent(true);
                }
                if (startupFrameCount == 2)
                {
                    BokuGame.bokuGame.OldInitialize();
                }
                if (startupFrameCount == 1)
                {
                    bool done = BokuGame.bokuGame.LoadContentWinRT();
                    // Prevent frame countdown from advancing if not done loading.
                    if (!done)
                    {
                        ++startupFrameCount;
                    }
                }
                --startupFrameCount;
                return;
            }

            /*            
            if (firstTime)
            {
                BokuGame.gameListManager.AddObject(titleScreenMode);

                BokuGame.bokuGame.OldInitialize();

                firstTime = false;
            }
            */
#endif

#if !NETFX_CORE
            System.Windows.Forms.Form form = StartupForm.ActiveForm;
#endif

#if NETFX_CORE
            // On WinRT the viewport will change sizes as the app gets snapped.  
            // Change ScreenSize to keep up.  Note we still get the wrong numbers
            // too much of the time espeically when width = 320 but it does
            // revert to proper values when full screen.
            if (KoiLibrary.GraphicsDevice.Viewport.Width != BokuGame.ScreenSize.X || KoiLibrary.GraphicsDevice.Viewport.Height != BokuGame.ScreenSize.Y)
            {
                BokuGame.ScreenSize = new Vector2(KoiLibrary.GraphicsDevice.Viewport.Width, KoiLibrary.GraphicsDevice.Viewport.Height);
                // Capture new copy of viewport.
                InGame.CaptureFullViewport();
            }
#else
            // Strangely enough, we actually see the window change sizes here before
            // we get the SizeChanged event.  So deal with it.
            // If the TutorialManager is active it may be tweaking this so don't touch.
            if (KoiLibrary.GraphicsDevice.Viewport.Width != BokuGame.ScreenSize.X || KoiLibrary.GraphicsDevice.Viewport.Height != BokuGame.ScreenSize.Y)
            {
                if (!TutorialManager.Active)
                {
                    BokuGame.ScreenSize = new Vector2(GraphicsDevice.Viewport.Width, GraphicsDevice.Viewport.Height);
                    BokuGame.ScreenPosition = Vector2.Zero;
                }
                // Capture new copy of viewport.
                InGame.CaptureFullViewport();
            }

#endif
            // We need to update the tutorial and hints first so that they can steal input if needed.
            // Also the screen size and position should be set before doing anything else.
            TutorialManager.Update();

            InGame.SetViewportToScreen();

            Point loc = MainForm.Instance.ClientLocation;
            Rectangle clientRect = new Rectangle(loc.X, loc.Y, XNAControl.Instance.ClientSize.Width, XNAControl.Instance.ClientSize.Height);
            KoiLibrary.Update(clientRect);

            SceneManager.Update();
            DialogManagerX.Update();

            AsyncOps.Update();
            AuthUI.Update();

#if UPDATE_TIMERS
            updateTimer.Start();
#endif

            // JW - The Slingshot and TouchButtons updates must be positioned before the gameListManager 
            // update, as they are intended to update their internal state before any actor brain updates.
            TouchVirtualController.Update();

            //Update GUI buttons first to see if we need to eat any input.
            GUIButtonManager.Update();

            // If we're not in runSim mode, check for F2 cycling the help level.
            /*
            if (!(InGame.inGame.State == InGame.States.Active && InGame.inGame.CurrentUpdateMode == InGame.UpdateMode.RunSim))
            {
                if(KeyboardInput.WasPressed(Microsoft.Xna.Framework.Input.Keys.F2))
                {
                    XmlOptionsData.HelpLevel = (XmlOptionsData.HelpLevel + 1) % 3;
                }
            }
            */
            // The above has been disabled because it turns out to not be all that useful.
            // In particular, it often gets hit accidentally causing confusion and frustration.
            XmlOptionsData.HelpLevel = 2;   // Set to full all the time...

            CommandStack.Update();

            ContentLoader.Update();

            CheckRefresh();

            // Don't allow new hints if there's an active GamePadInput dialog or if the Tutorial mode is active.
            Hints.Update(!GamePadInput.DialogActive && !TutorialManager.Active);

            // Update all the objects currently in the game.
//            updateGameListTimer.Start();


            // Keep camera res in sync with screen res.
            InGame.inGame.Camera.Resolution = new Point((int)BokuGame.ScreenSize.X, (int)BokuGame.ScreenSize.Y);
            InGame.inGame.Camera.Update();

            ///PV: update the touch subsystem
            // JW - NOTE: We have moved the TouchEdit update from the InGame::UpdateObjects() call
            // (where MouseEdit is updated) because the tools are updated before UpdateObjects() 
            // is called. By moving it here, the tools are now getting to-the-frame-accurate data
            // from the TouchEdit's MouseTouchHitInfo object.
            //TouchEdit.Update(InGame.inGame.Camera);

           

            gameListManager.Update();
//            updateGameListTimer.Stop();

            gameAudio.Update();

//            updateTwitchTimer.Start();
            //TwitchManager.Update();
//            updateTwitchTimer.Stop();

//            updateGameTimer.Start();
            GameTimerManager.Update();
//            updateGameTimer.Stop();

            pictureManager.Update();

            progressScreen.Update();

            //update dialog manager
            ModularMessageDialogManager.Instance.Update();

            // Pump web request callbacks.
            Web.Trans.Request.Update();

#if UPDATE_TIMERS
            updateTimer.Stop();
#endif
        }   // end of BokuGame Update()

        public static string DebugString = "";

        public void Draw()
        {

#if NETFX_CORE
            // Hack to make WinRT startup look better.
            if (startupFrameCount > 0)
            {
                titleScreen.Render();
                return;
            }
#endif

#if !NETFX_CORE
            Debug.Assert(System.Threading.Thread.CurrentThread.ManagedThreadId == ThreadId);
#endif

            // Render to the TutorialManager's render target.
            TutorialManager.PreRender();

#if RENDER_TIMERS
            renderTimer.Start();
#endif

            // TODO (****) Remove when no longer needed.
            // HACK HACK
            // If we just have the NullScene in place, render normally.
            if (SceneManager.CurrentScene.Name == "NullScene")
            {
                // Render all the active objects.
                gameListManager.Render();
            }

            // New scene rendering...
            // We always want to do this even if NullScene is active in over to support dialogs.
            InGame.SetViewportToScreen();
            SceneManager.Render();

            /*
            // Debug touch
            ScreenSpaceQuad quad = ScreenSpaceQuad.GetInstance();
            TouchContact t = TouchInput.GetOldestTouch();
            if (t != null)
                quad.Render(new Vector4(1, 0, 0, 1), t.position - new Vector2(5, 5), new Vector2(10, 10));
            */

#if RENDER_TIMERS
            renderTimer.Stop();
#endif

            // Render any active hints.
            Hints.Render();

            progressScreen.Render();

            // Render Tutorials stuff last so it's always on top.
            TutorialManager.Render();

            GamePadInput.Render();

            // Render dialogs.
            InGame.SetViewportToScreen();
            ModularMessageDialogManager.Instance.Render();
            AuthUI.Render();

            
#if DISPLAY_FPS
            if (XmlOptionsData.ShowFramerate && !BokuGame.bokuGame.videoOutput.Active)
            {
                // Draw frame rate string over the top of what's been rendered.

                string fpsString = Time.FrameRateString + DebugString;

                GetSpriteFont Font = SharedX.GetSegoeUI24;

                SpriteBatch batch = KoiLibrary.SpriteBatch;

                InGame.RestoreViewportToFull();

                batch.Begin();
                {
                    int height = (int)(BokuGame.ScreenPosition.Y + BokuGame.ScreenSize.Y);
                    int x = 120;
                    int y = height - 50;

                    batch.DrawString(Font(), fpsString, new Vector2(x + 1, y + 1), Color.DimGray);
                    if (Time.SkippingFrames)
                    {
                        batch.DrawString(Font(), fpsString, new Vector2(x, y), Color.Red);
                        //TextHelper.DrawStringWithShadow(Font, batch, x, y, fpsString, Color.Red, Color.DimGray, false);
                    }
                    else
                    {
                        batch.DrawString(Font(), fpsString, new Vector2(x, y), Color.White);
                        //TextHelper.DrawStringWithShadow(Font, batch, x, y, fpsString, Color.White, Color.DimGray, false);
                    }

#if DISPLAY_BUDGETS
                    float TotalCost = InGame.inGame.TotalCost;
                    float actorCost = TotalCost - WayPoint.TotalCost - Terrain.TotalCost;
                    TextHelper.DrawStringWithShadow(Font, batch, x, y - 60,
                        "A " + actorCost.ToString("F2")
                        + " W " + WayPoint.TotalCost.ToString("F2")
                        + " T " + Terrain.TotalCost.ToString("F2"),
                        Color.White, Color.DimGray, false);
#endif // DISPLAY_BUDGETS
                }
                batch.End();
            }
#endif

        }   // end of BokuGame Draw()

        private void ScreenGrab()
        {
// (TODO (****) BROKEN
#if !NETFX_CORE
            if (Actions.PrintScreen.WasPressed || Actions.ShiftPrintScreen.WasPressed || pictureManager.DoScreenGrab)
            {
                bool debugCapture = Actions.ShiftPrintScreen.WasPressed;

                Actions.PrintScreen.ClearAllWasPressedState();
                Actions.ShiftPrintScreen.ClearAllWasPressedState();

                GraphicsDevice device = GraphicsDevice;
                int width = device.PresentationParameters.BackBufferWidth;
                int height = device.PresentationParameters.BackBufferHeight;

                // Get the back buffer data.
                Color[] data = new Color[width * height];
                if (hidef)
                {
                    device.GetBackBufferData<Color>(data);
                }
                else
                {
                    InGame.inGame.FullRenderTarget0.GetData<Color>(data);
                }

                // Create a texture for it.
                Texture2D screenGrab = new Texture2D(device, width, height);
                screenGrab.SetData<Color>(data);

                // Save texture to Jpg.
                string fileName = ScreenGrabName();
                Storage4.TextureSaveAsJpeg(screenGrab, fileName);

                // If needed, save as PNG with transparency, if any.
                if (debugCapture)
                {
                    fileName = fileName.Replace(".Jpg", ".png");
                    Storage4.TextureSaveAsPng(screenGrab, fileName);
                }

                DeviceResetX.Release(ref screenGrab);

                // Should we print?
                bool print = KeyboardInput.IsPressed(Microsoft.Xna.Framework.Input.Keys.RightControl)
                            || KeyboardInput.IsPressed(Microsoft.Xna.Framework.Input.Keys.LeftControl);
                if (print)
                {
                    try
                    {
                        Process myProcess = new Process();
                        string fullPath = Path.Combine(Storage4.UserLocation, fileName);

                        myProcess.StartInfo.FileName = fullPath; 
                        myProcess.StartInfo.Verb = "Print";
                        myProcess.StartInfo.CreateNoWindow = true;
                        myProcess.Start();
                    }
                    catch
                    {
                    }

                }

                // SGI_MOD - picture support
                if (pictureManager.DoScreenGrab)
                {
                    pictureManager.ScreenGrabFinished();
                }
            }
#endif
        }   // end of ScreenGrab()

        private string ScreenGrabName()
        {
            int i = 0;
            string root = @"..\Screen";
            string ext = @".Jpg";
            string fmt = "D3";

            while (Storage4.FileExists(root + i.ToString(fmt) + ext, StorageSource.UserSpace))
            {
                ++i;
            }
            return root + i.ToString(fmt) + ext;
        }

        //
        // Game loop is over, exiting.
        //

#if NETFX_CORE
        protected override void EndRun()
#else
        public void EndRun()
#endif
        {
            // Adding an empty try/catch here because I'm seeing null dref exceptions
            // in the error log but I'm not sure what's causing it.
            try
            {
                GamePadInput.stopActiveInputTimer();
                //MouseInput.StopMouseWorkerThread();

                BokuGame.Running = false;
                Time.ActiveGameClock = false;
                if (InGame.XmlWorldData != null && InGame.XmlWorldData.id != null)
                {
                    Instrumentation.RecordEvent(Instrumentation.EventId.FinalLevel, InGame.XmlWorldData.id.ToString());
                }
                Instrumentation.StopTimer(sessionTimerInstrument);

                //Clean up the instrumentation by stopping all remaining timers
                Instrumentation.StopAllTimers();

#if LOG_CONTENT_LOADS
            {
                List<string> keys = new List<string>(loadedContent.Keys);
                keys.Sort(StringComparer.InvariantCultureIgnoreCase);

                StreamWriter writer = File.CreateText("loadedContent.txt");

                foreach (string key in keys)
                {
                    writer.WriteLine(key);
                }

                writer.Close();
            }
#endif
            }
            catch(NullReferenceException e)
            {
                if (e != null)
                {
                }
            }

        }   // end of BokuGame EndRun()

        protected void OnExiting(object sender, EventArgs args)
        {
            //base.OnExiting(sender, args);
        }   // end of BokuGame OnExiting()

#if NETFX_CORE
#else
        public void Exit()
        {
            Application.Exit();
        }
#endif

        //
        // End of game overrides.
        //

        public static void Load(INeedsDeviceReset foo)
        {
            ContentLoader.Load(foo);
        }

        public static void Load(INeedsDeviceReset foo, bool immediate)
        {
            ContentLoader.Load(foo, immediate);
        }

        public static void InitDeviceResources(INeedsDeviceReset foo, GraphicsDevice device)
        {
            if (foo != null)
            {
                foo.InitDeviceResources(device);
            }
        }

        public static void Loaded(INeedsDeviceReset foo)
        {
            if (!loaded.ContainsKey(foo.GetHashCode()))
                loaded.Add(foo.GetHashCode(), foo);
        }

        public static void Unload(INeedsDeviceReset foo)
        {
            if (foo != null)
            {
                ContentLoader.Unload(foo);
                loaded.Remove(foo.GetHashCode());
            }
        }

        public static void DeviceReset(INeedsDeviceReset foo)
        {
            DeviceReset(foo, KoiLibrary.GraphicsDevice);
        }

        public static void DeviceReset(INeedsDeviceReset foo, GraphicsDevice device)
        {
            if (foo != null)
            {
                foo.DeviceReset(device);
            }
        }

        public static void DeviceResetIfLoaded(INeedsDeviceReset foo)
        {
            if (foo != null && loaded.ContainsKey(foo.GetHashCode()))
            {
                foo.DeviceReset(KoiLibrary.GraphicsDevice);
            }
        }

        static Dictionary<int, INeedsDeviceReset> loaded = new Dictionary<int, INeedsDeviceReset>();

        /// Disposing of content resources can cause a problem during reload.
        /// The underlying ContentManager caches things like loaded textures
        /// but doesn't know when it gets disposed by the application, so
        /// when reloading it just returns to us the cached disposed asset.
        /// On the other hand, other things must be Disposed immediately, because
        /// they represent precious finite resources that others will be needing
        /// right away.
        /// 
        /// The generic case is to call the object's Dispose method. We have
        /// specializations that override this behavior for the types we don't
        /// want disposed.
        /// 
        /// Obviously when you add a new type, you should think very carefully for at
        /// least 10 seconds about whether its Dispose should be called or not.
        /// 
        /// Regarding the Assert that the type is derived from IDisposable, these functions
        /// are strictly for regulating whether an IDisposable's Dispose() should be
        /// called before letting go of it. If it's not an IDisposable, you are safe
        /// in just setting the ref to null. So Don't add Release for anything not
        /// derived from IDisposable.
        /// 

        public static void Release<T>(ref T foo) where T : IDisposable
        {
            if (foo != null)
            {
                foo.Dispose();
            }
            foo = default(T);
        }

        public static void Release(ref Texture foo)
        {
            // No Dispose, cached in ContentManager
            foo = null;
        }
        public static void Release(ref Texture2D foo)
        {
            // No Dispose, cached in ContentManager
            foo = null;
        }
        public static void Release(ref TextureCube foo)
        {
            // No Dispose, cached in ContentManager
            foo = null;
        }
        public static void Release(ref Effect foo)
        {
            // No Dispose, cached in ContentManager
            foo = null;
        }
        public static void Release(ref VertexDeclaration foo)
        {
            // Don't call Dispose since these are commonly shared.
            foo = null;
        }

    }   // end of class BokuGame

}   // end of namespace Boku



