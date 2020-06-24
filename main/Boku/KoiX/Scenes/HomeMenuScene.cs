
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
using Boku.Fx;
using Boku.SimWorld;
using Boku.UI2D;

namespace KoiX.Scenes
{
    public class HomeMenuScene : BaseScene
    {
        float HomeMenuWidth = 600;

        #region Members

        SpriteCamera camera;
        Texture2D bkgTexture;   // Texture used for background.  Will be thumbnail, if available.  Else the same as Community.
        Texture2D communityTexture;

        HomeMenuDialog homeMenu;

        #endregion

        #region Accessors
        #endregion

        #region Public

        // c'tor
        public HomeMenuScene()
            : base("HomeMenuScene")
        {
            camera = new SpriteCamera();

            homeMenu = new HomeMenuDialog();
            homeMenu.Rectangle = new RectangleF(-HomeMenuWidth / 2.0f, -150, HomeMenuWidth, 600);
        }   // end of c'tor

        public override void Update()
        {
            if (Active)
            {

                // Position dialogs.
                {
                    BaseWidget w = homeMenu.GetWidget(0);
                    if (w != null)
                    {
                        float buttonHeight = (w as WidgetSet).Widgets[0].LocalRect.Height;

                        // Size of HomeMenu based on extent of buttons.
                        Vector2 size = w.LocalRect.Size;
                        homeMenu.Rectangle = new RectangleF(-homeMenu.Rectangle.Width / 2.0f, -homeMenu.Rectangle.Height / 2.0f, size.X, size.Y);
                    }
                }

                // Set UI camera to standard position/zoom to match current resolution.
                SetCameraToTargetResolution(camera);
            }
        }   // end of Update()

        public override void Render(RenderTarget2D rt)
        {
            GraphicsDevice device = KoiLibrary.GraphicsDevice;

            if (rt != null)
            {
                device.SetRenderTarget(rt);
            }

            RenderBackgroundStretched(bkgTexture);

            // Overlay "safe" region.
            //RoundedRect.Render(camera, new RectangleF(-targetResolution / 2.0f, targetResolution), 32.0f, Color.Red * 0.2f);

            if (rt != null)
            {
                device.SetRenderTarget(null);
            }
        }   // end of Render()

        public override void PostDialogRender(RenderTarget2D rt)
        {
            // Put the house graphic 

            base.PostDialogRender(rt);
        }

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

                DialogManagerX.ShowDialog(homeMenu, camera);
                DialogManagerX.ShowDialog(AuthUI.StatusDialog, camera);

                // Do we have a thumbnail?
                bkgTexture = communityTexture;
                /*
                if (InGame.inGame.ThumbNail != null)
                {
                    bkgTexture = InGame.inGame.ThumbNail;
                }
                */

                Foley.PlayMenuLoop();

                base.Activate(args);
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
                DialogManagerX.KillDialog(homeMenu);
                DialogManagerX.KillDialog(AuthUI.StatusDialog);

                Foley.StopMenuLoop();

                base.Deactivate();
            }
        }   // end of default Deactivate()

        #endregion

        #region InputEventHandler
        #endregion

        #region Internal

        public override void LoadContent()
        {
            if (DeviceResetX.NeedsLoad(bkgTexture))
            {
                communityTexture = KoiLibrary.LoadTexture2D(@"Textures\LoadLevel\CommunityBackground");
            }

            homeMenu.LoadContent();

            base.LoadContent();
        }

        public override void UnloadContent()
        {
            DeviceResetX.Release(ref bkgTexture);

            homeMenu.UnloadContent();

            base.UnloadContent();
        }

        #endregion


    }   // end of class HomeMenuScene

}   // end of namespace KoiX.Scenes
