//#define IMPORT_DEBUG

#define USE_UNDO_STACK

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Net;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Storage;
#if NETFX_CORE
    using System.Threading.Tasks;
    //using Windows.Foundation;
    using Windows.Storage;
    using Windows.Storage.Pickers;
    using Windows.System;
#else
    using Microsoft.Xna.Framework.Net;
#endif


using Boku.Audio;
using Boku.Base;
using Boku.Common;
using Boku.Common.Sharing;
using Boku.Common.TutorialSystem;
using Boku.Common.Xml;
using Boku.Fx;
using Boku.Input;
using Boku.SimWorld;
using Boku.UI;
using Boku.UI2D;
using Boku.Web;

using BokuShared.Wire;

namespace Boku
{
    /// <summary>
    /// This represents the main menu of the game
    /// It is the first screen you see after the title
    /// </summary>
    public class MainMenu : GameObject, INeedsDeviceReset
    {
        public static MainMenu Instance = null;
        public static string KoduGameLabUrl
        {
            get
            {
                return   Program2.SiteOptions.KGLUrl + "?ref=client";
            }
        }

        // If set, then jump directly into this level at startup.
        public static string StartupWorldFilename;

        /// <summary>
        /// Allows the dirty flag to be set for the LiveFeed.  The reason we need this is that
        /// when we come back from the OptionsMenu, the RenderTarget used by the live feed may
        /// have been used and overwritten with other stuff.  So in the Deactivate call for the
        /// options menu we set this.
        /// The "real" problem here is that the OptionsMenu isn't a seperate scene.  Instead it
        /// is just rendered over the top of the MainMenu.  This means that the setting of the 
        /// dirty flag, which happens in the MainMenu Activate call, doesn't otherwise happen
        /// when the OptionsMenu exits.
        /// </summary>
        public bool LiveFeedDirty
        {
            set { shared.liveFeed.Dirty = value; }
        }

        public NewWorldDialog newWorldDialog;

        protected class Shared : INeedsDeviceReset
        {
            public Camera camera = new PerspectiveUICamera();
            public Camera bokuCamera = new SimCamera();

            public BokuGreeter boku = null;
            public Texture2D backgroundTexture = null;
            //public Texture2D jplTexture = null;            
            public Texture2D blueArrowTexture = null;
            public Boku.Base.GameTimer timer = null;

            public Button signOutButton = null;

            public Matrix worldMatrix = Matrix.Identity;

            public ModularMenu menu = null;

            public OptionsMenu optionsMenu = null;
            public LiveFeedDisplay liveFeed=null;

            public bool waitingForStorage = false;  // Used w/ trial mode to not display menu.

            public string screenUrl = @"www.KoduGameLab.com";
            public AABB2D urlBox = new AABB2D();
            public TextBlob textBlob = null;

            // c'tor
            public Shared(MainMenu parent)
            {
                // Set up the options menu.
                optionsMenu = new OptionsMenu();
                liveFeed = new LiveFeedDisplay();

                if (BokuGame.bMarsMode)
                    boku = ActorManager.GetActor("RoverGreeter").CreateNewInstance() as BokuGreeter;
                else
                    boku = ActorManager.GetActor("BokuGreeter").CreateNewInstance() as BokuGreeter;
                boku.SetColor(Classification.Colors.White);

                bokuCamera.NearClip = 0.1f;
                bokuCamera.FarClip = 20.0f;
                // These are the values for the model when its translation off the ground has been thrown away (and added back via constant)
                bokuCamera.From = 1.3f * new Vector3(1.5f, 0.3f, 0.5f);
                bokuCamera.At = new Vector3(0.0f, -0.5f, 0.0f);
                // These are the values for a "correct" model - that is raised off the ground in Max and whose translation is intact
                // bokuCamera.From = new Vector3(1.5f, 0.3f, 1.4f);
                // bokuCamera.At = new Vector3(0.0f, -0.5f, 0.7f);

                // Move camera to look at menu from an angle.
                //camera.From = 0.9f * camera.From;
                camera.At = new Vector3(-0.6f, 0, 0);
                Matrix foo = Matrix.CreateRotationY(-0.3f) * Matrix.CreateTranslation(new Vector3(1.0f, 0.0f, -2.0f));
                camera.At = Vector3.Transform(camera.At, foo);
                camera.From = Vector3.Transform(camera.From, foo);

                // We'll be using a 1280x720 rendertarget for all rendering.
                camera.Resolution = new Point(1280, 720);
                bokuCamera.Resolution = new Point(1280, 720);

                timer = new Boku.Base.GameTimer(Boku.Base.GameTimer.ClockType.WallClock, 3.1415927);
                timer.TimerElapsed += ChangeExpression;

                // Create text elements.
                // Start with a blob of common parameters.
                UIGridElement.ParamBlob blob = new UIGridElement.ParamBlob();
                blob.width = 3.4f;
                blob.height = 0.5f;
                blob.edgeSize = 0.06f;
                blob.Font = UI2D.Shared.GetGameFont24Bold;
                blob.textColor = Color.White;
                blob.dropShadowColor = Color.Black;
                blob.useDropShadow = true;
                blob.invertDropShadow = true;
                blob.unselectedColor = new Color(new Vector3(4, 100, 90) / 255.0f);
                blob.selectedColor = new Color(new Vector3(5, 180, 160) / 255.0f);
                blob.normalMapName = @"Slant0Smoothed5NormalMap";
                blob.justify = UIGrid2DTextElement.Justification.Left;

                menu = new ModularMenu(blob, null/*Strings.Localize("mainMenu.mainMenu")*/);
                menu.OnSelect = parent.OnSelect;
                menu.OnCancel = parent.OnCancel;
                menu.UseRtCoords = true;


                menu.AddText(Strings.Localize("mainMenu.new"));
                menu.AddText(Strings.Localize("mainMenu.play"));
#if NETFX_CORE
                menu.AddText(Strings.Localize("mainMenu.import"));
#else
                if (WinStoreHelpers.RunningAsUWP)
                {
                    menu.AddText(Strings.Localize("mainMenu.import"));
                }
#endif
                menu.AddText(Strings.Localize("mainMenu.community"));
                menu.AddText(Strings.Localize("mainMenu.options"));
                menu.AddText(Strings.Localize("mainMenu.help"));
#if !NETFX_CORE
                // Once you run an app in Win8, you are never allowed to kill it.
                // Only the system can kill it.
                menu.AddText(Strings.Localize("mainMenu.exit"));
#endif

                // And then remove what we don't want.
                if (!Program2.SiteOptions.CommunityEnabled)
                {
                    menu.DeleteText(Strings.Localize("mainMenu.community"));
                }

                menu.WorldMatrix = Matrix.CreateScale(0.9f, 1.0f, 1.0f);

                string signOutStr = Strings.Localize("textDialog.signOut");
                signOutButton = new Button( signOutStr, Color.White, null, UI2D.Shared.GetGameFont20);
                
                //Because this button has no texture and we can't set the width of the texture passed in explicitly. Just use the fixed size based on text size.
                UI2D.Shared.GetFont Font = UI2D.Shared.GetGameFont20;
                Vector2 size = (null != Font) ? Font().MeasureString( signOutStr ) : new Vector2( 60.0f, 20.0f );
                signOutButton.FixedSize = size;
                signOutButton.UseFixedSize = true;

                textBlob = new TextBlob(UI2D.Shared.GetGameFont24, "", 340);

            }   // end of Shared c'tor

            private void ChangeExpression(Boku.Base.GameTimer timer)
            {
                int newFace = BokuGame.bokuGame.rnd.Next(6);
                switch (newFace)
                {
                    case 0:
                        boku.DisplayEmotionalState(Face.FaceState.Crazy, (float)(1.0 + BokuGame.bokuGame.rnd.NextDouble()));
                        break;
                    case 1:
                        boku.DisplayEmotionalState(Face.FaceState.Happy, (float)(1.0 + BokuGame.bokuGame.rnd.NextDouble()));
                        break;
                    case 2:
                        boku.DisplayEmotionalState(Face.FaceState.Mad, (float)(1.0 + BokuGame.bokuGame.rnd.NextDouble()));
                        break;
                    case 3:
                        boku.DisplayEmotionalState(Face.FaceState.Remember, (float)(1.0 + BokuGame.bokuGame.rnd.NextDouble()));
                        break;
                    case 4:
                        boku.DisplayEmotionalState(Face.FaceState.Sad, (float)(1.0 + BokuGame.bokuGame.rnd.NextDouble()));
                        break;
                    case 5:
                        boku.DisplayEmotionalState(Face.FaceState.Squint, (float)(1.0 + BokuGame.bokuGame.rnd.NextDouble()));
                        break;
                }

                timer.Reset(4.0 + 4.0 * BokuGame.bokuGame.rnd.NextDouble());
            }   // end of ChangeExpression()


            public void LoadContent(bool immediate)
            {
                if (backgroundTexture == null)
                {
                    if (BokuGame.bMarsMode)
                    {
                        backgroundTexture = BokuGame.Load<Texture2D>(BokuGame.Settings.MediaPath + @"Textures\MainMenuWidescreenMars");
                        //jplTexture = BokuGame.Load<Texture2D>(BokuGame.Settings.MediaPath + @"Textures\nasajpl");
                    }
                    else
                    {
#if NETFX_CORE
                        backgroundTexture = BokuGame.Load<Texture2D>(BokuGame.Settings.MediaPath + @"Textures\MainMenuWidescreenMG");
#else
                        backgroundTexture = BokuGame.Load<Texture2D>(BokuGame.Settings.MediaPath + @"Textures\MainMenuWidescreen");
#endif
                    }
                }

                if (blueArrowTexture == null)
                {
                    blueArrowTexture = BokuGame.Load<Texture2D>(BokuGame.Settings.MediaPath + @"Textures\GridElements\BlueArrow");
                }

                BokuGame.Load(boku, immediate);
                BokuGame.Load(menu, immediate);
                BokuGame.Load(optionsMenu, immediate);
                BokuGame.Load(liveFeed, immediate);
            }   // end of MainMenu Shared LoadContent()

            public void InitDeviceResources(GraphicsDevice device)
            {
                optionsMenu.InitDeviceResources(device);
            }

            public void UnloadContent()
            {
                BokuGame.Release(ref backgroundTexture);
                //BokuGame.Release(ref jplTexture);
                BokuGame.Release(ref blueArrowTexture);

                BokuGame.Unload(boku);
                BokuGame.Unload(menu);
                BokuGame.Unload(optionsMenu);
                BokuGame.Unload(liveFeed);
            }   // end of MainMenu Shared UnloadContent()

            /// <summary>
            /// Recreate render targets
            /// </summary>
            /// <param name="graphics"></param>
            public void DeviceReset(GraphicsDevice device)
            {
                BokuGame.DeviceReset(boku, device);
                BokuGame.DeviceReset(menu, device);
                BokuGame.DeviceReset(optionsMenu, device);
                BokuGame.DeviceReset(liveFeed, device);
            }

        }   // end of class Shared

        protected class UpdateObj : UpdateObject
        {
            private MainMenu parent = null;
            private Shared shared = null;

#if DEBUG
            private string TrialModeLevelName = "fd144670-3d81-4d42-8653-10f4a0192565.Xml";
#endif

            public UpdateObj(MainMenu parent, Shared shared)
            {
                this.parent = parent;
                this.shared = shared;

#if DEBUG
                Debug.Assert(Storage4.FileExists(BokuGame.Settings.MediaPath + BokuGame.BuiltInWorldsPath + TrialModeLevelName, StorageSource.TitleSpace), "Trial mode level does not exist");
#endif
            }

            public override void Update()
            {
                shared.timer.Update();

                // Keep Kodu animating even if a dialog is active.
                shared.boku.UpdateFace();
                shared.boku.UpdateAnimations();

                if (AuthUI.IsModalActive)
                {
                    return;
                }

                if (parent.newWorldDialog.Active)
                {
                    parent.newWorldDialog.Update();
                    return;
                }

                // If not modal, always show status.
                AuthUI.ShowStatusDialog();

                // Update the dialogs.
                parent.prevSessionCrashedMessage.Update();
                parent.noCommunityMessage.Update();
                parent.noSharingMessage.Update();

                // Don't do anything else until the user reads and dismisses the dialogs.
                if (parent.prevSessionCrashedMessage.Active 
                    || parent.exitingKodu)
                {
                    return;
                }

                // Update the options menu.  Do this first so that if it is active it can steal input.
                shared.optionsMenu.Update();

                // Set the menu's active state to always be the opposite of the feed's.  Only one should be active at a time.
                shared.menu.Active = !shared.liveFeed.Active;

                // If OptionsMenu is active, don't look at input.  This is a problem for touch input
                // which doesn't support any kind of "ClearAllWasPressedState" functionality.
                if (!shared.optionsMenu.Active)
                {

                    // Check for click on signOut tile or url.
                    if (MouseInput.Left.WasPressed || Actions.Select.WasPressed || TouchInput.WasTouched )
                    {
                        TouchContact touch = TouchInput.GetOldestTouch();
                        if (touch != null)
                        {
                            touch.position = ScreenWarp.ScreenToRT(touch.position);
                        }
                        Vector2 mouseHit = MouseInput.GetMouseInRtCoords();

                        // url is in rt coords.
                        if (shared.urlBox.Contains(mouseHit) ||
                            (null != touch && shared.urlBox.Contains(touch.position)))
                        {
#if NETFX_CORE
                            Launcher.LaunchUriAsync(new Uri(KoduGameLabUrl));
#else
                            Process.Start(KoduGameLabUrl);
#endif
                            MouseInput.Left.ClearAllWasPressedState();
                        }

                    }

                    // Enable resume option if we have something to resume to.
                    if (InGame.UnDoStack.HaveResume() && (shared.menu.Item(0) != Strings.Localize("mainMenu.resume")))
                    {
                        shared.menu.InsertText(Strings.Localize("mainMenu.resume"), 0);
                    }

                    shared.liveFeed.UpdateFeed();
                    shared.liveFeed.Update(shared.camera);

                    if (!UpdateNonMenuItems())
                    {
                        // JW - Only update the menu and process input if the interactive non-menu
                        // items didn't already handle the input.
                        int curIndex = shared.menu.CurIndex;
                        shared.menu.Update(shared.camera, ref shared.worldMatrix);
                        int newIndex = shared.menu.CurIndex;

                        // If the user made a menu change, have boku glance over.
                        if (curIndex != newIndex)
                        {
                            shared.boku.DirectGaze(new Vector3(0.2f, -0.4f, 0.08f - 0.05f * newIndex), 0.5f);
                        }
                    }
                }

                if (Actions.MiniHub.WasPressed && InGame.XmlWorldData != null)
                {
                    parent.Deactivate();
                    InGame.inGame.SwitchToMiniHub();
                    return;
                }

#if IMPORT_DEBUG
                if (!string.IsNullOrEmpty(StartupWorldFilename))
                {
                    LevelPackage.DebugPrint("MainMenu");
                    LevelPackage.DebugPrint("    StartupWorldFilename : " + StartupWorldFilename);
                }
#endif
                // Jump into the startup world, if it was specified.
                if (!String.IsNullOrEmpty(StartupWorldFilename))
                {
                    if (Storage4.FileExists(StartupWorldFilename, StorageSource.All))
                    {
#if IMPORT_DEBUG
                        LevelPackage.DebugPrint("    level exists, trying to load and run");
#endif
                        if (BokuGame.bokuGame.inGame.LoadLevelAndRun(StartupWorldFilename, keepPersistentScores: false, newWorld: false, andRun: true))
                        {
#if IMPORT_DEBUG
                            LevelPackage.DebugPrint("    success on load and run");
#endif
                            parent.Deactivate();
                        }
#if IMPORT_DEBUG
                        else
                        {
                            LevelPackage.DebugPrint("    fail to load and run");
                        }
#endif
                        shared.waitingForStorage = false;
                    }
#if IMPORT_DEBUG
                    else
                    {
                        LevelPackage.DebugPrint("    level not found");
                    }
#endif

                    StartupWorldFilename = null;
                }
                if (GamePadInput.ActiveMode == GamePadInput.InputMode.GamePad)
                {
                    GamePadInput pad = GamePadInput.GetGamePad0();
                    if (Actions.ComboLeft.WasPressed)
                    {
                        if (!shared.liveFeed.Active && (!shared.optionsMenu.Active))
                        {                       
                            shared.liveFeed.Activate();
                            shared.liveFeed.UpdateFeed();
                        }
                    }
                }
                else if (GamePadInput.ActiveMode == GamePadInput.InputMode.KeyboardMouse )
                {
                    if (Actions.ComboLeft.WasPressed && !shared.liveFeed.Active && (!shared.optionsMenu.Active))
                    {
                        shared.liveFeed.Activate();
                    }
                }
            }   // end of Update()

            private bool UpdateNonMenuItems()
            {
                bool inputHandled = false;

                // Check for click on signOut tile or url.
                if (MouseInput.Left.WasPressed || Actions.Select.WasPressed || TouchInput.WasTouched || TouchInput.WasLastReleased)
                {
                    TouchContact touch = TouchInput.GetOldestTouch();
                    Vector2 touchHit = new Vector2(-1, -1); // touch off screen if no touch was found

                    // url is in rt coords.
                    if (touch != null)
                    {
                        touchHit = ScreenWarp.ScreenToRT(touch.position);
                    }
                    Vector2 mouseHit = MouseInput.GetMouseInRtCoords();

                    if (shared.urlBox.Contains(mouseHit) || (shared.urlBox.Contains(touchHit) && TouchInput.WasLastReleased))
                    {
#if NETFX_CORE
                        Launcher.LaunchUriAsync(new Uri(KoduGameLabUrl));
#else
                        Process.Start(KoduGameLabUrl);
#endif
                        MouseInput.Left.ClearAllWasPressedState();
                        inputHandled = true;
                    }

                    //check for touch over scroll window (doesn't need to be released)
                    if (!inputHandled  && GamePadInput.ActiveMode == GamePadInput.InputMode.Touch)
                    {
                        if (TouchInput.WasTouched && touch != null && shared.liveFeed.IsInScrollwindow(touch.position))
                        {
                            //touch in window? if so, activate the live feed
                            shared.liveFeed.Activate();
                            inputHandled = true;
                        }
                        else
                        {
                            //deactivate the feed and don't consider the input handled
                            shared.liveFeed.Deactivate();
                        }
                    }
                }
                else if (GamePadInput.ActiveMode == GamePadInput.InputMode.KeyboardMouse)
                {
                    Vector2 pureMouseHit = new Vector2(MouseInput.Position.X, MouseInput.Position.Y);
                  //  mouseHit = new Vector2(MouseInput.Position.X, MouseInput.Position.Y);

                    if (shared.liveFeed.IsInScrollwindow(pureMouseHit)) // mouseHit))
                    {
                        shared.liveFeed.Activate();
                    }
                }

                return inputHandled;
            }

            public override void Activate()
            {
                // If we have a level to resume, check for the crashed cookie.  If we find the cookie
                // delete it and activate the dialog letting the user know they can recover the level.
                if (InGame.UnDoStack.HaveResume())
                {
                    if (Storage4.FileExists(MainMenu.CrashCookieFilename, StorageSource.UserSpace))
                    {
                        Storage4.Delete(MainMenu.CrashCookieFilename);

                        parent.prevSessionCrashedMessage.Activate();
                    }
                }

                // Force feed to refresh rendering.
                shared.liveFeed.Dirty = true;

                // Start showing the current, signed-in creator.
                AuthUI.ShowStatusDialog();
            }
            
            public override void Deactivate()
            {
                AuthUI.HideAllDialogs();
            }

        }   // end of class MainMenu UpdateObj  
        protected class RenderObj : RenderObject
        {
            private MainMenu parent;
            private Shared shared;
            private int skipFrames = 3; // Hack to allow us to skip a few frames to prevent ugliness for trial mode.

            public RenderObj(MainMenu parent, Shared shared)
            {
                this.parent = parent;
                this.shared = shared;
            }

            public float fVal = -10.0f;
            public override void Render(Camera camera)
            {
                GraphicsDevice device = BokuGame.bokuGame.GraphicsDevice;

                HelpOverlay.RefreshTexture();

                RenderTarget2D rt = UI2D.Shared.RenderTargetDepthStencil1280_720;
                Vector2 screenSize = BokuGame.ScreenSize;
                Vector2 rtSize = new Vector2(rt.Width, rt.Height);

                if (skipFrames > 0 || shared.waitingForStorage)
                {
                    InGame.Clear(Color.Black);
                    --skipFrames;
                    return;
                }

                shared.liveFeed.FeedSize = shared.liveFeed.ResetScrollBoxSize;
                InGame.SetRenderTarget(rt);

                // Clear the screen & z-buffer.
                InGame.Clear(Color.Black);

                // Apply the background.
                ScreenSpaceQuad quad = ScreenSpaceQuad.GetInstance();

                Vector2 position = Vector2.Zero;
                quad.Render(shared.backgroundTexture, position, rtSize, @"TexturedNoAlpha");

                Color textColor = new Color(21, 125, 178);

                if (parent.newWorldDialog.Active)
                {
                    // Hide the dialog if auth UI is active.  Just keeps things cleaner.
                    if (!AuthUI.IsModalActive)
                    {
                        // If options menu is active, render instead of main menu.
                        parent.newWorldDialog.Render(new Vector2(rt.Width, rt.Height));
                    }
                }
                else if (shared.optionsMenu.Active)
                {
                    // Hide the menu if auth UI is active.  Just keeps things cleaner.
                    if (!AuthUI.IsModalActive)
                    {
                        // If options menu is active, render instead of main menu.
                        shared.optionsMenu.Render();
                    }
                }
                else
                {
                    // Render url
                    SpriteBatch batch = UI2D.Shared.SpriteBatch;
                    UI2D.Shared.GetFont Font = UI2D.Shared.GetGameFont24;
                    Vector2 size = Font().MeasureString(shared.screenUrl);
                    Vector2 pos = new Vector2(rt.Width / 2 - size.X / 2, 586);
                    batch.Begin();
                    TextHelper.DrawString(Font, shared.screenUrl, pos, textColor);
                    batch.End();
                    shared.urlBox.Set(pos, pos + size);

                    // Hide the menu if auth UI is active.  Just keeps things cleaner.
                    if (!AuthUI.IsModalActive)
                    {
                        // Render menu using local cameras.
                        ShaderGlobals.SetCamera(shared.camera);
                        shared.menu.WorldMatrix = Matrix.CreateTranslation(0.0f, -0.3f, 0.0f);
                        shared.menu.Render(shared.camera);
                    }

                    // Render Boku.
                    ShaderGlobals.SetCamera(shared.bokuCamera);
                    string oldRig = BokuGame.bokuGame.shaderGlobals.PushLightRig(ShaderGlobals.GreeterRigName);

                    // TODO (****) How to temporarily disable point lights???
                    // Do we really need to?
                    //Luz.SetToEffect(true); // disable scene point lights

                    if (BokuGame.bMarsMode)
                    {
                        shared.boku.Movement.Position = new Vector3(-0.0f, 0.25f, -0.5f);
                        shared.boku.ReScale = 0.50f;

                        //quad = ScreenSpaceQuad.GetInstance();
                        //float wid=shared.jplTexture.Width/2;
                        //position = new Vector2(1250-(wid), 20);
                        //quad.Render(shared.jplTexture, position, new Vector2(wid, shared.jplTexture.Height/2), @"TexturedRegularAlpha");
                    }
                    else
                    {
                        shared.boku.Movement.Position = new Vector3(0.0f, 0.0f, 0.0f);
                    }
                    fVal += 0.01f;

                    // Be sure to set the right camera so the env map looks correct.
                    ShaderGlobals.SetCamera(shared.bokuCamera);

                    shared.boku.RenderObject.Render(shared.bokuCamera);

                    // TODO (****) How to temporarily disable point lights???
                    //Luz.SetToEffect(false); // re-enable scene point lights
                    BokuGame.bokuGame.shaderGlobals.PopLightRig(oldRig);

                }

                InGame.RestoreRenderTarget();

                InGame.Clear(new Color(20, 20, 20));
                InGame.SetViewportToScreen();

                // Copy the rendered scene to the backbuffer.
                {
                    ScreenWarp.FitRtToScreen(rtSize);

                    quad.Render(rt, ScreenWarp.RenderPosition, ScreenWarp.RenderSize, @"TexturedNoAlpha");
                }

                // Render news feed.
                if (!shared.optionsMenu.Active)
                {
                    shared.liveFeed.Render();
                }

                // Hide overlay if auth UI is active.
                if (!AuthUI.IsModalActive)
                {
                    HelpOverlay.Render();
                }

                // Render text dialogs if being shown by OptionsMenu.
                // TODO (****) Need to get rid of rendering to RTs where possible.
                // TODO (****) Need to split OptionsMenu from MainMenu.
                if (shared.optionsMenu.Active)
                {
                    InGame.inGame.shared.smallTextDisplay.Render();
                    InGame.inGame.shared.scrollableTextDisplay.Render();
                }

                MainMenu.Instance.prevSessionCrashedMessage.Render();
                MainMenu.Instance.noCommunityMessage.Render();
                MainMenu.Instance.noSharingMessage.Render();

            }   // end of Render()  
            
            public override void Activate()
            {
            }
            
            public override void Deactivate()
            {
            }

        }   // end of class MainMenu RenderObj     


        // List objects.
        protected Shared shared = null;
        protected RenderObj renderObj = null;
        protected UpdateObj updateObj = null;

        private enum States
        {
            Inactive,
            Active,
        }


        private States state = States.Inactive;
        private States pendingState = States.Inactive;

        private CommandMap commandMap = new CommandMap("App.TitleMenu");   // Placeholder for stack.

        private ModularMessageDialog noCommunityMessage = null;
        private ModularMessageDialog noSharingMessage = null;
        private ModularMessageDialog prevSessionCrashedMessage = null;

        private bool exitingKodu = false;   // Flag set when the user chooses to exit Kodu 
                                            // from the above dialogs.  This flags allows us
                                            // to exit more cleanly.  Without it we flash the
                                            // storage selection dialog as we exit.

        // Only show this notification once.


        static public string CrashCookieFilename = "Crash.txt";

        #region Accessors
        public bool Active
        {
            get { return (state == States.Active); }
        }

        public bool OptionsActive
        {
            get { return shared.optionsMenu.Active; }
        }

        /// <summary>
        /// Are any of the MainMenu dialogs active?
        /// </summary>
        public bool DialogActive
        {
            get { return noCommunityMessage.Active || noSharingMessage.Active; }
        }

        public Texture2D BackgroundTexture
        {
            get { return shared.backgroundTexture; }
        }

        public Button SignOutButton
        {
            get { return shared.signOutButton; }
        }

        #endregion

        // c'tor
        public MainMenu()
        {
            MainMenu.Instance = this;

            shared = new Shared(this);

            // Create the RenderObject and UpdateObject parts of this mode.
            updateObj = new UpdateObj(this, shared);
            renderObj = new RenderObj(this, shared);

            NewWorldDialog.OnAction OnSelectWorld = delegate(string level) 
            {
                // Deactivate main menu and go into editor with empty level.
                string levelFilename = Path.Combine(BokuGame.Settings.MediaPath, BokuGame.BuiltInWorldsPath, level + ".Xml");
                if (BokuGame.bokuGame.inGame.LoadLevelAndRun(levelFilename, keepPersistentScores: false, newWorld: true, andRun: false))
                {
                    Deactivate();
                    InGame.inGame.Activate();
                    InGame.inGame.CurrentUpdateMode = InGame.UpdateMode.ToolMenu;
                }
                else
                {
                    shared.menu.Active = true;
                }
            };
            NewWorldDialog.OnAction OnCancel = delegate(string level) 
            {
                shared.menu.Active = true;
            };
            newWorldDialog = new NewWorldDialog(OnSelectWorld, OnCancel);

            // Set up the NoCommunity, NoSharing and PrevCrash dialogs.
            ModularMessageDialog.ButtonHandler handlerA = delegate(ModularMessageDialog dialog)
            {
                // User chose "resume"

                // Deactivate dialog.
                dialog.Deactivate();

                if (InGame.CurrentWorldId == Guid.Empty)
                {
                    if (InGame.UnDoStack.Resume())
                    {
                        // Deactivate MainMenu.
                        Deactivate();
                    }
                    else
                    {
                        //Debug.Assert(false, "Resume should not be enabled unless there is something to resume from");

                        // We had some error in trying to resume.  So, remove the resume
                        // option from the menu and soldier on.
                        shared.menu.DeleteText(Strings.Localize("mainMenu.resume"));
                        shared.menu.Active = true;
                        XmlOptionsData.LastAutoSave = -1;
                    }
                }
                else
                {
                    // Deactivate MainMenu.
                    Deactivate();

                    // Just reactivate the existing game.
                    BokuGame.bokuGame.inGame.Activate();
                }

            };

            ModularMessageDialog.ButtonHandler handlerB = delegate(ModularMessageDialog dialog)
            {
                // User chose "back"

                // Deactivate dialog.
                dialog.Deactivate();

                // Only needed for corruptStorageMessage but shouldn't hurt for all.
            };

            ModularMessageDialog.ButtonHandler handlerX = delegate(ModularMessageDialog dialog)
            {
                // User chose to quit Kodu

                // Deactivate dialog.
                dialog.Deactivate();

                // Wave bye, bye.
#if NETFX_CORE
                Windows.UI.Xaml.Application.Current.Exit();
#else
                BokuGame.bokuGame.Exit();
#endif

                exitingKodu = true;
            };

            noCommunityMessage = new ModularMessageDialog(Strings.Localize("miniHub.noCommunityMessage"),
                                                            null, null,
                                                            handlerB, Strings.Localize("textDialog.back"),
                                                            null, null,
                                                            null, null
                                                            );
            noSharingMessage = new ModularMessageDialog(Strings.Localize("miniHub.noSharingMessage"),
                                                            null, null,
                                                            handlerB, Strings.Localize("textDialog.back"),
                                                            null, null,
                                                            null, null
                                                            );
            prevSessionCrashedMessage = new ModularMessageDialog(Strings.Localize("mainMenu.prevSessionCrashedMessage"),
                                                            handlerA, Strings.Localize("textDialog.resume"),
                                                            handlerB, Strings.Localize("textDialog.back"),
                                                            null, null,
                                                            null, null
                                                            );

        }   // end of MainMenu c'tor

#if NETFX_CORE
        public async void OnSelect(ModularMenu menu)
#else
        public void OnSelect(ModularMenu menu)
#endif
        {
            menu.Active = false;
            string cur = menu.CurString;

            // RESUME
            if (cur == Strings.Localize("mainMenu.resume"))
            {
                if (InGame.CurrentWorldId == Guid.Empty)
                {
                    if (InGame.UnDoStack.Resume())
                    {
                        Deactivate();

                        // Force resume to go into Edit mode.
                        InGame.inGame.CurrentUpdateMode = InGame.UpdateMode.MouseEdit;
                    }
                    else
                    {
                        //Debug.Assert(false, "Resume should not be enabled unless there is something to resume from");

                        // We had some error in trying to resume.  So, remove the resume
                        // option from the menu and soldier on.
                        shared.menu.DeleteText(Strings.Localize("mainMenu.resume"));
                        shared.menu.Active = true;
                        XmlOptionsData.LastAutoSave = -1;
                    }
                }
                else
                {
                    Deactivate();
                    // Just reactivate the existing game.
                    BokuGame.bokuGame.inGame.Activate();
                }
            }

            // NEW WORLD
            if (cur == Strings.Localize("mainMenu.new") || cur == "CREATE")
            {
                newWorldDialog.Active = true;
            }

            // PLAY
            if (cur == Strings.Localize("mainMenu.play") )
            {
                Deactivate();
                BokuGame.bokuGame.loadLevelMenu.LocalLevelMode = LoadLevelMenu.LocalLevelModes.General;
                BokuGame.bokuGame.loadLevelMenu.ReturnToMenu = LoadLevelMenu.ReturnTo.MainMenu;
                BokuGame.bokuGame.loadLevelMenu.Activate();
            }

#if NETFX_CORE
            // IMPORT
            if (cur == Strings.Localize("mainMenu.import"))
            {
                // Note this also switches to the LoadLevelMenu if any worlds are imported.
                bool levelImported = await PickImportFilesAsync();
                if (levelImported)
                {
                    Deactivate();
                    // Switch to LoadLevelMenu which should also trigger a loading of the files in the Imports dir.
                    BokuGame.bokuGame.loadLevelMenu.LocalLevelMode = LoadLevelMenu.LocalLevelModes.General;
                    BokuGame.bokuGame.loadLevelMenu.ReturnToMenu = LoadLevelMenu.ReturnTo.MainMenu;
                    BokuGame.bokuGame.loadLevelMenu.Activate();
                }
                else
                {
                    menu.Active = true;
                }
            }
#endif
            if (WinStoreHelpers.RunningAsUWP)
            {
                // IMPORT
                if (cur == Strings.Localize("mainMenu.import"))
                {
                    // Note this also switches to the LoadLevelMenu if any worlds are imported.
                    bool levelImported = PickImportFiles();
                    if (levelImported)
                    {
                        Deactivate();
                        // Switch to LoadLevelMenu which should also trigger a loading of the files in the Imports dir.
                        BokuGame.bokuGame.loadLevelMenu.LocalLevelMode = LoadLevelMenu.LocalLevelModes.General;
                        BokuGame.bokuGame.loadLevelMenu.ReturnToMenu = LoadLevelMenu.ReturnTo.MainMenu;
                        BokuGame.bokuGame.loadLevelMenu.Activate();
                    }
                    else
                    {
                        menu.Active = true;
                    }
                }
            }

            // COMMUNITY
            if (cur == Strings.Localize("mainMenu.community") || cur == "GALLERY")
            {
                // Check to see if the community server is reachable before switching screens.
                if (!Web.Community.Async_Ping(Callback_Ping, null))
                {
                    noCommunityMessage.Activate();
                    menu.Active = true;
                }
            }

            // OPTIONS
            if (cur == Strings.Localize("mainMenu.options"))
            {
                // Reactivate the menu since we want it alive when the options menu comes back.
                // Need to do this before activating the options menu in order to keep the stacks correct.
                menu.Active = true;
                shared.optionsMenu.Activate();
            }

            // HELP
            if (cur == Strings.Localize("mainMenu.help"))
            {
                Deactivate();
                BokuGame.bokuGame.helpScreens.Activate();
            }

            // QUIT
            if (cur == Strings.Localize("mainMenu.exit"))
            {

                //GamePadInput.stopActiveInputTimer();

                //deactivate the menu on exit to stop the timer
                Deactivate();

                // Wave bye, bye.
#if NETFX_CORE
                Windows.UI.Xaml.Application.Current.Exit();
#else
                BokuGame.bokuGame.Exit();
#endif
            }
        }   // end of OnSelect

#if NETFX_CORE

        private async Task<bool> PickImportFilesAsync()
        {
            bool levelsImported = false;

            // Ask user for files to import.
            FileOpenPicker filePicker = new FileOpenPicker();
            filePicker.FileTypeFilter.Add(".kodu");
            filePicker.FileTypeFilter.Add(".kodu2");
            filePicker.ViewMode = PickerViewMode.List;
            filePicker.SuggestedStartLocation = PickerLocationId.Desktop;
            filePicker.SettingsIdentifier = "ImportPicker";
            filePicker.CommitButtonText = Strings.Localize("mainMenu.importWorldsCommitButtonText");

            IReadOnlyList<StorageFile> files = await filePicker.PickMultipleFilesAsync();

            // Copy resulting files into Imports directory.
            if (files != null && files.Count > 0)
            {
                foreach (StorageFile file in files)
                {
                    StorageFolder importsFolder = await Storage4.UserSpaceFolder.CreateFolderAsync(LevelPackage.importsPath, CreationCollisionOption.OpenIfExists);
                    StorageFile fileCopy = await file.CopyAsync(importsFolder);
                }

                levelsImported = true;
            }

            return levelsImported;
        }   // end of PickImportFile()

#endif

        private bool PickImportFiles()
        {
            bool levelsImported = false;

            // Create the dialog.
            System.Windows.Forms.OpenFileDialog dlg = new System.Windows.Forms.OpenFileDialog();
            dlg.Multiselect = true;
            dlg.DefaultExt = ".Kodu2";
            dlg.Filter = "Kodu Levels|*.Kodu2;*.Kodu";

            // Activate dialog.
            System.Windows.Forms.DialogResult result = dlg.ShowDialog();
            
            if(result == System.Windows.Forms.DialogResult.OK)
            {
                // See if any files were selected and copy them to the imports folder.
                string[] fullPaths = dlg.FileNames;
                string[] filenames = dlg.SafeFileNames;

                for(int i=0; i<fullPaths.Length; i++)
                {
                    if(File.Exists(fullPaths[i]))
                    {
                        string destFilename = Path.Combine(Storage4.UserLocation, LevelPackage.importsPath, filenames[i]);
                        File.Copy(fullPaths[i], destFilename);

                        levelsImported = true;
                    }
                }
            }

            return levelsImported;
        }   // end of PickImportFiles()

        void Callback_Ping(object param)
        {
            AsyncResult result = (AsyncResult)param;

            if (result.Success)
            {
                // Open the community UI
                Deactivate();
                BokuGame.bokuGame.community.Activate();
            }
            else
            {
                noCommunityMessage.Activate();

                // since we aren't leaving this screen, we need to reactivate the menu.
                shared.menu.Active = true;
            }
        }

        public void OnCancel(ModularMenu menu)
        {
            // Nothing to see here, move along.  Just be sure the menu remains active.
            menu.Active = true;

            // Make "Quit" the selected menu item.
            int quitIdx = menu.Index(Strings.Localize("mainMenu.exit"));
            if (quitIdx >= 0)
            {
                menu.CurIndex = quitIdx;
            }

        }   // end of MainMenu OnCancel()


        public override bool Refresh(List<UpdateObject> updateList, List<RenderObject> renderList)
        {
            bool result = false;

            if (state != pendingState)
            {
                if (pendingState == States.Active)
                {
                    updateList.Add(updateObj);
                    updateObj.Activate();
                    renderList.Add(renderObj);
                    renderObj.Activate();

                    shared.menu.Active = true;
                    shared.timer.Reset(BokuGame.bokuGame.rnd.NextDouble());
                }
                else
                {
                    shared.menu.Active = false;

                    renderObj.Deactivate();
                    renderList.Remove(renderObj);
                    updateObj.Deactivate();
                    updateList.Remove(updateObj);
                }

                state = pendingState;
            }

            return result;
        }   // end of MainMenu Refresh()

        private object timerInstrument = null;

        override public void Activate()
        {
            if (state != States.Active)
            {
                // If we're in a tutorial and we get to the MainMenu, kill the tutorial.
                TutorialManager.Deactivate();

                // Do stack handling here.  If we do it in the update object we have no
                // clue which order things get pushed and popped and madness ensues.
                CommandStack.Push(commandMap);
                shared.menu.Active = true;

                pendingState = States.Active;
                BokuGame.objectListDirty = true;

                // Restore the default texture map for UI use.
                BokuGame.bokuGame.shaderGlobals.EnvTextureName = null;

                // Not persisting, make sure it's empty.
                XmlOptionsData.WebUserSecret = String.Empty;

                timerInstrument = Instrumentation.StartTimer(Instrumentation.TimerId.MainMenuTime);

                Foley.PlayMenuLoop();
            }
        }   // end of MainMenu Activate()

        override public void Deactivate()
        {
            if (state != States.Inactive)
            {
                // Do stack handling here.  If we do it in the update object we have no
                // clue which order things get pushed and popped and madness ensues.
                shared.menu.Active = false;
                CommandStack.Pop(commandMap);

                shared.menu.DeleteText(Strings.Localize("mainMenu.resume"));
                shared.menu.Active = false;

                // Just to be sure.
                shared.optionsMenu.Deactivate();

                pendingState = States.Inactive;
                BokuGame.objectListDirty = true;

                GamePadInput.IgnoreUntilReleased(Buttons.A);

                Instrumentation.StopTimer(timerInstrument);

                Foley.StopMenuLoop();
            }
        }   // end of MainMenu Deactivate()

        public void LoadContent(bool immediate)
        {
            BokuGame.Load(shared, immediate);
            BokuGame.Load(noCommunityMessage, immediate);
            BokuGame.Load(noSharingMessage, immediate);
            BokuGame.Load(newWorldDialog, immediate);
        }   // end of MainMenu LoadContent()

        public void InitDeviceResources(GraphicsDevice device)
        {
        }

        public void UnloadContent()
        {
            BokuGame.Unload(shared);
            BokuGame.Unload(noCommunityMessage);
            BokuGame.Unload(noSharingMessage);
            BokuGame.Unload(newWorldDialog);
        }   // end of MainMenu UnloadContent()

        /// <summary>
        /// Recreate render targets
        /// </summary>
        /// <param name="graphics"></param>
        public void DeviceReset(GraphicsDevice device)
        {
            BokuGame.DeviceReset(shared, device);
            BokuGame.DeviceReset(noCommunityMessage, device);
            BokuGame.DeviceReset(noSharingMessage, device);
        }


    }   // end of class MainMenu

}   // end of namespace Boku
    

