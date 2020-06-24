
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Input.Touch;
using Microsoft.Xna.Framework.Storage;

using KoiX;
using KoiX.Input;
using KoiX.Geometry;
using KoiX.Managers;
using KoiX.Text;
using KoiX.UI;
using KoiX.UI.Dialogs;

using Boku;
using Boku.Audio;
using Boku.Base;
using Boku.Common;
using Boku.Common.Xml;
using Boku.Fx;
using Boku.SimWorld;
using Boku.UI2D;

using BokuShared;

namespace KoiX.Scenes
{
    public class MainMenuScene : BaseScene
    {
        float mainMenuWidth = 600;

        #region Members

        SpriteCamera camera;
        Texture2D bkgTexture;
        Texture2D logoTexture;

        BokuGreeter boku;
        Camera bokuCamera = new SimCamera();
        GameTimer timer;

        MainMenuDialog mainMenu;
        NewsFeedDialog newsFeed;

        double timeout = 0;

        BaseWidget prevGlanceWidget;    // Previous widget the greeter glanced at.

        bool firstActivation = true;    // Is this the first ime this scene has been activated.
                                        // Currently used to decide whether or not to show the 
                                        // AuthSignInDialog.

        #endregion

        #region Accessors
        #endregion

        #region Public

        // c'tor
        public MainMenuScene()
            : base("MainMenuScene")
        {
            camera = new SpriteCamera();

            mainMenu = new MainMenuDialog(mainMenuWidth);
            mainMenu.RenderBaseTile = false;
            mainMenu.Rectangle = new RectangleF(-mainMenuWidth / 2.0f, -150, mainMenuWidth, 400);

            newsFeed = new NewsFeedDialog();
            newsFeed.Rectangle = new RectangleF(-800, -370, 450, 300);

            // Greeter 
            if (BokuGame.bMarsMode)
                boku = ActorManager.GetActor("RoverGreeter").CreateNewInstance() as BokuGreeter;
            else
                boku = ActorManager.GetActor("BokuGreeter").CreateNewInstance() as BokuGreeter;
            boku.SetColor(Classification.Colors.White);

            bokuCamera.NearClip = 0.1f;
            bokuCamera.FarClip = 20.0f;
            // These are the values for the model when its translation off the ground has been thrown away (and added back via constant)
            bokuCamera.From = 1.3f * new Vector3(1.5f, 0.3f, 0.5f);
            bokuCamera.At = new Vector3(0.5f, -0.5f, 0.0f);
            bokuCamera.Fov = 0.7f;
            bokuCamera.Resolution = KoiLibrary.ClientRect.GetSize();
            bokuCamera.Update();

            timer = new Boku.Base.GameTimer(Boku.Base.GameTimer.ClockType.WallClock, 3.1415927);
            timer.TimerElapsed += ChangeExpression;

        }   // end of c'tor

        public override void Update()
        {
            if (Active)
            {
                // Needed for greeter.
                BokuGame.bokuGame.shaderGlobals.Update();

                // Main menu needs to be removed when modal dialog is active and
                // restarted when not.
                if (DialogManagerX.ModalDialogIsActive)
                {
                    if (mainMenu.Active)
                    {
                        DialogManagerX.KillDialog(mainMenu);
                        DialogManagerX.KillDialog(newsFeed);
                    }
                }
                else
                {
                    if (!mainMenu.Active)
                    {
                        DialogManagerX.ShowDialog(mainMenu, camera);
                        DialogManagerX.ShowDialog(newsFeed, camera);
                    }
                }

                bokuCamera.From = 1.3f * new Vector3(1.5f, 0.3f, 0.5f);
                bokuCamera.At = new Vector3(0.5f, -0.5f, 0.0f);
                bokuCamera.Fov = 0.7f;
                bokuCamera.Resolution = KoiLibrary.ClientRect.GetSize();
                bokuCamera.Update();

                // Keep Kodu animating even if a dialog is active.
                timer.Update();
                boku.UpdateFace();
                boku.UpdateAnimations();

                // Position dialogs.
                {
                    BaseWidget w = mainMenu.GetWidget(0);
                    if (w != null)
                    {
                        float buttonHeight = (w as WidgetSet).Widgets[0].LocalRect.Height;

                        // Size of mainMenu based on extent of buttons.
                        Vector2 size = w.LocalRect.Size;
                        mainMenu.Rectangle = new RectangleF(-mainMenuWidth / 2.0f, -150, size.X, size.Y);
                        newsFeed.Rectangle = new RectangleF(-800, -370, 450, 740);
                    }
                }

                if (Time.WallClockTotalSeconds > timeout)
                {
                    // Let the old UI show through.
                    //SceneManager.SwitchToScene("NullScene");

                    //BokuGame.bokuGame.mainMenu.Activate();
                }

                // Set UI camera to standard position/zoom to match current resolution.
                SetCameraToTargetResolution(camera);

                // If the user made a menu change, have boku glance over.
                if(DialogManagerX.CurrentFocusDialog == mainMenu && mainMenu.CurrentFocusWidget != prevGlanceWidget)
                {
                    prevGlanceWidget = mainMenu.CurrentFocusWidget;
                    Debug.Assert(prevGlanceWidget != null, "Should never be null.  Protect against it anyway.");
                    if (prevGlanceWidget != null)
                    {
                        float y = prevGlanceWidget.Position.Y;
                        boku.DirectGaze(new Vector3(0.2f, -0.4f, 0.08f - 0.05f * y / 80.0f), 0.5f);
                    }
                }

            }
        }   // end of Update()

        public override void Render(RenderTarget2D rt)
        {
            GraphicsDevice device = KoiLibrary.GraphicsDevice;

            if (rt != null)
            {
                device.SetRenderTarget(rt);
            }

            // Needed for greeter.
            BokuGame.bokuGame.shaderGlobals.Render(bokuCamera);

            RenderBackgroundStretched(bkgTexture);

            // Overlay "safe" region.
            //RoundedRect.Render(camera, new RectangleF(-targetResolution / 2.0f, targetResolution), 32.0f, Color.Red * 0.2f);

            // Logo
            {
                SpriteBatch batch = KoiLibrary.SpriteBatch;

                Rectangle dstRect = new Rectangle((int)(-mainMenuWidth / 2), -450, (int)mainMenuWidth, (int)(mainMenuWidth / 2));
                batch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, null, null, null, null, camera.ViewMatrix);
                batch.Draw(logoTexture, dstRect, Color.White);
                batch.End();
            }


            if (rt != null)
            {
                device.SetRenderTarget(null);
            }
        }   // end of Render()

        /// <summary>
        /// Use the PostDialogRender to layer the greeter Kodu over the top
        /// of the main menu.  If another modal dialog is active, don't 
        /// render the Kodu at all, otherwise the user may not be able to 
        /// get to it.
        /// </summary>
        /// <param name="rt"></param>
        public override void PostDialogRender(RenderTarget2D rt)
        {
            if (!DialogManagerX.ModalDialogIsActive)
            {
                GraphicsDevice device = KoiLibrary.GraphicsDevice;

                if (rt != null)
                {
                    device.SetRenderTarget(rt);
                }

                //
                // Render Boku.
                //
                {
                    BokuGame.bokuGame.shaderGlobals.SetCamera(bokuCamera);
                    string oldRig = BokuGame.bokuGame.shaderGlobals.PushLightRig(ShaderGlobals.GreeterRigName);

                    boku.Movement.Position = new Vector3(0.0f, 0.0f, 0.0f);

                    // Be sure to set the right camera so the env map looks correct.
                    BokuGame.bokuGame.shaderGlobals.SetCamera(bokuCamera);

                    boku.RenderObject.Render(bokuCamera);

                    // TODO (****) How to temporarily disable point lights???
                    //Luz.SetToEffect(false); // re-enable scene point lights
                    BokuGame.bokuGame.shaderGlobals.PopLightRig(oldRig);
                }

                if (rt != null)
                {
                    device.SetRenderTarget(null);
                }
            }
        }   // end of PostDialogRnder()

        public override void RegisterForEvents()
        {
        }   // end of RegisterForEvents()

        public override void UnregisterForEvents()
        {
            base.UnregisterForEvents();
        }   // end of UnregisterForEvents()

        /// <summary>
        /// Activate this scene.
        /// </summary>
        /// <param name="args">optional argument list.  Most Scenes will not use one but for those cases where it's needed this is here.</param>
        /// <summary>
        /// Shouldn't be called by user code.  Is called by SceneManager when 
        /// switching scenes.
        /// </summary>
        public override void Activate(params object[] args)
        {
            if (!Active)
            {
                if (args != null)
                {
                    foreach (object arg in args)
                    {
                        // Do something with each arg...
                    }
                }

                // 2 seconds after we start...
                timeout = Time.WallClockTotalSeconds + 2.0f;

                DialogManagerX.ShowDialog(newsFeed, camera);
                DialogManagerX.ShowDialog(mainMenu, camera);

                // If no one is signed in and the user didn't select 
                // to keep signed in, launch the sign in dialog.
                if (Auth.CreatorName == Auth.DefaultCreatorName && !XmlOptionsData.KeepSignedInOnExit && firstActivation)
                {
                    AuthUI.ShowSignInDialog(camera);
                }
                else
                {
                    AuthUI.ShowStatusDialog(camera);
                }

                Foley.PlayMenuLoop();

                base.Activate(args);

                firstActivation = false;
            }
        }   // end of default Activate()

        /// <summary>
        /// Shouldn't be called by user code.  Is called by SceneManager when 
        /// switching scenes.
        /// </summary>
        public override void Deactivate()
        {
            if (Active)
            {
                DialogManagerX.KillDialog(mainMenu);
                DialogManagerX.KillDialog(newsFeed);
                DialogManagerX.KillDialog(AuthUI.StatusDialog);

                Foley.StopMenuLoop();

                base.Deactivate();
            }
        }   // end of default Deactivate()

        #endregion

        #region InputEventHandler
        #endregion

        #region Internal

        void ChangeExpression(Boku.Base.GameTimer timer)
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


        public override void LoadContent()
        {
            if (bkgTexture == null)
            {
                bkgTexture = KoiLibrary.LoadTexture2D(@"Textures\LoadLevel\CommunityBackground");
                logoTexture = KoiLibrary.LoadTexture2D(@"Textures\KoduLogoBW");
            }

            BokuGame.Load(boku);

            base.LoadContent();
        }

        public override void UnloadContent()
        {
            DeviceResetX.Release(ref bkgTexture);
            DeviceResetX.Release(ref logoTexture);

            BokuGame.Unload(boku);

            base.UnloadContent();
        }

        #endregion


    }   // end of class MainMenuScene

}   // end of namespace KoiX.Scenes
