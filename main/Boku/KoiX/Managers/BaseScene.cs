
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Storage;

using KoiX;
using KoiX.Input;
using KoiX.UI;
using KoiX.UI.Dialogs;

using Boku;

namespace KoiX.Managers
{
    /// <summary>
    /// Base class for scenes managed by SceneManager.
    /// </summary>
    public abstract class BaseScene : InputEventHandler, IDeviceResetX
    {
        /// <summary>
        /// This is the target resolution for all our UI rendering.  So, within whatever the actual screen 
        /// resolution is, we find the maximally sized 16x10 region and set up the camera so it sees that 
        /// as 1600x1000.  This gives us a constant size to render against regardless of actual window size.
        /// 
        /// Why this size?  Steam shows 16x9 accounts for more than 60% of the market.  16x10 accounts
        /// for another 20%.  So, by choosing this we are close on over 80% of the market.  On the 16x9
        /// machines this will also allow us to stretch things a bit wider without looking odd.
        /// </summary>
        public static Vector2 TargetResolution = new Vector2(1600, 1000);

        #region Members

        bool active = false;    // Should only be changed via this base class's Activate() or Deactivate() methods.

        string name;

        BaseScene prevScene;

        // Dialog used for screen oriented widgets without a normal parent dialog.
        // The DialogManager's camera is used for this since it includes global zoom.
        // To be really useful the dialog should also be given one or more WidgetSets
        // to use in aligning the widgets as the screen size or camera zoom changes.
        protected FullScreenDialog fullScreenDialog;

        // By default, no camera is created.  If this is not null then this is the
        // camera that is passed in when this dialog is shown via DialogManager.Show().
        protected SpriteCamera fullScreenDialogCamera = null;

        #endregion

        #region Accessors

        /// <summary>
        /// Is this scene active?
        /// </summary>
        public bool Active
        {
            get { return active; }
        }

        public string Name
        {
            get { return name; }
        }

        /// <summary>
        /// Scene we came from to get to this scene.  Allows a simple amount of going back.
        /// </summary>
        public BaseScene PrevScene
        {
            get { return prevScene; }
            set { prevScene = value; }
        }

        #endregion

        #region Public

        /// <summary>
        /// c'tor
        /// </summary>
        /// <param name="name"></param>
        public BaseScene(string name)
        {
            Debug.Assert(name != null && name.Length > 0);

            this.name = name;
            SceneManager.RegisterScene(this);

            // Do we want to do this here or individually?
            // TODO This needs to be IDispose if we ever want it to go away.
            //KoiLibrary.GraphicsDevice.DeviceReset += DeviceResetHandler;

            fullScreenDialog = new FullScreenDialog();

#if DEBUG
            fullScreenDialog._name = "FullScreenDialog : " + name;
#endif


        }   // end of c'tor

        public virtual void Update()
        {
            if (Active)
            {
            }
        }   // end of Update()

        /// <summary>
        /// Render the scene.
        /// </summary>
        /// <param name="rt">The RenderTarget to render into.  May be null -> default backbuffer</param>
        public virtual void Render(RenderTarget2D rt)
        {
            /*
            if (rt != null)
            {
                KoiLibrary.GraphicsDevice.SetRenderTarget(rt);
            }

            fullScreenDialog.Render(fullScreenCamera);

            if (rt != null)
            {
                KoiLibrary.GraphicsDevice.SetRenderTarget(null);
            }
            */
        }   // end of Render()

        /// <summary>
        /// Render any bits of the scene that should appear on top the dialogs.
        /// May not want to do this if modal dialogs are active.  I leave that up
        /// to the scene though.
        /// </summary>
        /// <param name="rt">The RenderTarget to render into.  May be null -> default backbuffer</param>
        public virtual void PostDialogRender(RenderTarget2D rt)
        {
        }   // end of PostDialogRender()

        public virtual void RegisterForEvents()
        {
            
            // Should be overridden with a version that registers 
            // for any events we want this scene to respond to.

        }   // end of RegisterForEvents()

        public virtual void UnregisterForEvents()
        {
            // Unregister for all events.
            KoiLibrary.InputEventManager.UnregisterForAllEvents(this);

        }   // end of UnregisterForEvents()

        /// <summary>
        /// Activate this scene.
        /// Shouldn't be called by user code.  Is called by SceneManager when 
        /// switching scenes.
        /// </summary>
        /// <param name="args">optional argument list.  Most Scenes will not use one but for those cases where it's needed this is here.</param>
        public virtual void Activate(params object[] args)
        {
            if (!active)
            {
                if (args != null)
                {
                    foreach (object arg in args)
                    {
                        // Do something with each arg...
                    }
                }

                RegisterForEvents();

                DialogManagerX.ShowDialog(fullScreenDialog, fullScreenDialogCamera);

                active = true;
            }
        }   // end of default Activate()

        /// <summary>
        /// Shouldn't be called by user code.  Is called by SceneManager when 
        /// switching scenes.
        /// </summary>
        public virtual void Deactivate()
        {
            if (active)
            {
                DialogManagerX.KillDialog(fullScreenDialog);

                UnregisterForEvents();

                active = false;
            }
        }   // end of default Deactivate()

        /// <summary>
        /// Adjust the given camera to fit our UI standard scaling.  This
        /// set the camera to be centered on 0, 0 and be zoomed so that we
        /// can see a 1600x1000 (TargetResolution) size block in the screen.
        /// </summary>
        /// <param name="camera"></param>
        public static void SetCameraToTargetResolution(SpriteCamera camera)
        {
            // Calc camera settings.
            Vector2 screenSize = BokuGame.ScreenSize;
            camera.Position = Vector2.Zero;

            float screenAspect = screenSize.X / screenSize.Y;
            float targetAspect = TargetResolution.X / TargetResolution.Y;

            if (screenAspect > targetAspect)
            {
                // Screen aspect is wider than target so set up camera based on vertical resolution.
                camera.Zoom = screenSize.Y / TargetResolution.Y;
            }
            else
            {
                // Screen aspect is taller than target so set up camera based on horizontal resolution.
                camera.Zoom = screenSize.X / TargetResolution.X;
            }

            // Apply camera changes.
            camera.Update();

        }   // end of SetCameraToTargetResolution()

        /// <summary>
        /// Renders the given texture across the full screen.  Maintains
        /// the aspect ratio of the texture while enlarge/shrinking it
        /// in order to best fit the screen's aspect ratio.  The full
        /// screen is covered while some of the texture may be clipped.
        /// </summary>
        /// <param name="bkgTexture"></param>
        protected void RenderBackgroundStretched(Texture2D bkgTexture)
        {
            SpriteBatch batch = KoiLibrary.SpriteBatch;

            Vector2 screenSize = BokuGame.ScreenSize;
            float screenAspect = screenSize.X / screenSize.Y;
            float bkgAspect = bkgTexture.Width / (float)bkgTexture.Height;
            float ratio = screenAspect / bkgAspect;
            Rectangle dstRect = new Rectangle(0, 0, (int)screenSize.X, (int)screenSize.Y);
            Rectangle srcRect;
            if (ratio > 1.0f)
            {
                // Clip top and bottom of bkg.
                float expectedHeight = bkgTexture.Height / ratio;
                int clip = (int)((bkgTexture.Height - expectedHeight) / 2.0f);
                srcRect = new Rectangle(0, clip, bkgTexture.Width, bkgTexture.Height - 2 * clip);
            }
            else
            {
                // Clip left/right of bkg.
                float expectedWidth = bkgTexture.Width * ratio;
                int clip = (int)((bkgTexture.Width - expectedWidth) / 2.0f);
                srcRect = new Rectangle(clip, 0, bkgTexture.Width - 2 * clip, bkgTexture.Height);
            }
            batch.Begin();
            batch.Draw(bkgTexture, dstRect, srcRect, Color.White);
            batch.End();
        }   // end of RenderBackgroundStretched()

        #endregion

        #region Internal

        public virtual void LoadContent()
        {
            fullScreenDialog.LoadContent();
        }

        public virtual void UnloadContent()
        {
            fullScreenDialog.UnloadContent();
        }

        public virtual void DeviceResetHandler(object sender, EventArgs e)
        {
            fullScreenDialog.DeviceResetHandler(sender, e);
        }

        #endregion

    }   // end of class BaseScene

}   // end of namespace KoiX.Managers
