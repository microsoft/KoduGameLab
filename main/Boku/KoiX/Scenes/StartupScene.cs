
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
using KoiX.Managers;
using KoiX.Text;
using KoiX.UI;
using KoiX.UI.Dialogs;

using Boku;
using Boku.Common;
using Boku.Common.Xml;
using Boku.UI2D;

using BokuShared;

namespace KoiX.Scenes
{
    /// <summary>
    /// This is the scene that apppear while Kodu is initially loading.
    /// </summary>
    public class StartupScene : BaseScene
    {
        #region Members

        Texture2D backgroundTexture;
        Texture2D logoTexture;
        Texture2D dotTexture;

        float kMaxRadius = 32.0f;

        bool doneLoading = false;

        protected struct Dot
        {
            public Vector2 position;    // Center of dot, screen coords.
            public float radius;        // In pixels.
            public float alpha;
        }

        Dot[] dots = null;

        #endregion

        #region Accessors
        #endregion

        #region Public

        // c'tor
        public StartupScene()
            : base("StartupScene")
        {
            dots = new Dot[4];
            for (int i = 0; i < 4; i++)
            {
                dots[i] = new Dot();
                dots[i].position = new Vector2(413 + 56 * i, 280);
                dots[i].radius = 32.0f;
                dots[i].alpha = 1.0f;
            }
        }

        public override void Update()
        {
            if (Active)
            {
                if (doneLoading)
                {
                    // If we've successfully imported a new level, jump
                    // immediately to there.  Do not go to main menu, 
                    // do not watch the video.
                    if (!String.IsNullOrEmpty(Program2.StartupWorldFilename))
                    {
                        if (Storage4.FileExists(Program2.StartupWorldFilename, StorageSource.All))
                        {
                            // TODO (scoy) Should this SwitchToScene be here or
                            // should hte switch be embedded in LoadLevelAndRun?
                            if (BokuGame.bokuGame.inGame.LoadLevelAndRun(Program2.StartupWorldFilename, keepPersistentScores: false, newWorld: false, andRun: true))
                            {
                                SceneManager.SwitchToScene("RunSimScene");
                            }
                        }
                        Program2.StartupWorldFilename = null;
                    }

                    if (XmlOptionsData.ShowIntroVideo)
                    {
                        // Show the video and tell it to go to the MainMenu when done.
                        object[] args = { "MainMenuScene" };
                        SceneManager.SwitchToScene("IntroVideoScene", args: args);
                    }
                    else
                    {
                        SceneManager.SwitchToScene("MainMenuScene");
                    }

                }   // end of if doneLoading

                // Animate the dots...
                double tic = Time.WallClockTotalSeconds;

                tic *= 2.0;     // Speed up time?
                for (int i = 0; i < 4; i++)
                {
                    float t = (float)tic + 5.0f - 0.5f * i;
                    t %= 6.0f;
                    if (t > 4.0f)
                    {
                        dots[i].radius = 0.0f;
                        dots[i].alpha = 0.0f;
                    }
                    else
                    {
                        t *= 0.5f;
                        if (t > 1.0f)
                            t = 2.0f - t;
                        t = TwitchCurve.Apply(t, TwitchCurve.Shape.EaseOut);

                        dots[i].radius = t * kMaxRadius;
                        dots[i].alpha = t;
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

            SpriteBatch batch = KoiLibrary.SpriteBatch;

            Vector2 screenSize = BokuGame.ScreenSize;

#if NETFX_CORE
                // For some reason, right at the start, this shows up as 0, 0.
                if (screenSize == Vector2.Zero)
                {
                    screenSize = new Vector2(device.Viewport.Width, device.Viewport.Height);
                }
#endif

            Vector2 backgroundSize = new Vector2(backgroundTexture.Width, backgroundTexture.Height);
            Vector2 logoSize = new Vector2(logoTexture.Width, logoTexture.Height);
            Vector2 position = (screenSize - backgroundSize) / 2.0f;
            // Clamp to pixels.
            position.X = (int)position.X;
            position.Y = (int)position.Y;

            // Clear the screen & z-buffer.
            InGame.Clear(Color.Black);

            batch.Begin(SpriteSortMode.Deferred, BlendState.NonPremultiplied);
            {
                // Apply the background.
                batch.Draw(backgroundTexture, position, Color.White);

                // Render dots.                
                for (int i = 0; i < 4; i++)
                {
                    Vector2 size = new Vector2(dots[i].radius);
                    Vector2 pos = position + dots[i].position - size;
                    size *= 2;
                    Color color = new Color(1, 1, 1, dots[i].alpha);
                    batch.Draw(dotTexture, new Rectangle((int)pos.X, (int)pos.Y, (int)size.X, (int)size.Y), color);
                    // Reflection 
                    color = new Color(1, 1, 1, dots[i].alpha * 0.15f);
                    batch.Draw(dotTexture, new Rectangle((int)pos.X, (int)pos.Y + 150, (int)size.X, (int)size.Y), color);
                }

                // MS logo.
                position = (screenSize - logoSize) / 2.0f + new Vector2(0, screenSize.Y / 4.0f);
                // Clamp to pixels.
                position.X = (int)position.X;
                position.Y = (int)position.Y;
                batch.Draw(logoTexture, position, Color.White);

            }
            batch.End();
          
            if (rt != null)
            {
                device.SetRenderTarget(null);
            }
        }   // end of Render()

        /// <summary>
        /// Activate this scene.
        /// </summary>
        /// <param name="args">optional argument list.  Most Scenes will not use one but for those cases where it's needed this is here.</param>
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

                base.Activate(args);
            }
        }   // end of default Activate()

        public override void Deactivate()
        {
            if (Active)
            {
                base.Deactivate();
            }
        }   // end of default Deactivate()

        /// <summary>
        /// Called by BokuGame when all the delay-loaded content
        /// is done.  At this point we need to either show the
        /// intro video or go straight to the MainMenu.
        /// </summary>
        public void OnDoneLoadingContent()
        {
            DialogCenter.Init();

            if (Program2.bShowVersionWarning)
            {
                Program2.bShowVersionWarning = false;
                DialogManagerX.ShowDialog(DialogCenter.ImportNeedsNewerVersionDialog);
            }

            // Did we save the previous user?  If so, restore.
            if (XmlOptionsData.KeepSignedInOnExit)
            {
                Auth.SetCreator(XmlOptionsData.CreatorName, XmlOptionsData.CreatorIdHash);
            }

            doneLoading = true;

        }   // end of OnDoneLoadingContent()

        #endregion

        #region InputEventHandler
        #endregion

        #region Internal

        public override void LoadContent()
        {
            if (DeviceResetX.NeedsLoad(backgroundTexture))
            {
                backgroundTexture = KoiLibrary.LoadTexture2D(@"Textures\Loading");
            }
            if (DeviceResetX.NeedsLoad(logoTexture))
            {
                logoTexture = KoiLibrary.LoadTexture2D(@"Textures\MicrosoftLogo");
            }
            if (DeviceResetX.NeedsLoad(dotTexture))
            {
                dotTexture = KoiLibrary.LoadTexture2D(@"Textures\LoadingDot");
            }

            base.LoadContent();
        }

        public override void UnloadContent()
        {
            DeviceResetX.Release(ref backgroundTexture);
            DeviceResetX.Release(ref logoTexture);
            DeviceResetX.Release(ref dotTexture);

            base.UnloadContent();
        }

        #endregion


    }   // end of class StartupScene

}   // end of namespace KoiX.Scenes
