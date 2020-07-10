// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.


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
using Microsoft.Xna.Framework.Media;
using Microsoft.Xna.Framework.Storage;

using KoiX;
using KoiX.Input;
using KoiX.Managers;
using KoiX.Text;
using KoiX.UI;

using Boku;
using Boku.Common.Xml;

namespace KoiX.Scenes
{
    public class IntroVideoScene : BaseScene
    {
        #region Members

        BaseScene onExitScene;  // Which scene to go to when video is done.

#if !NETFX_CORE
        public Video video = null;
        public VideoPlayer player = null;
#endif

        #endregion

        #region Accessors
        #endregion

        #region Public

        // c'tor
        public IntroVideoScene()
            : base("IntroVideoScene")
        {
        }

        public override void Update()
        {
            if (Active)
            {
#if NETFX_CORE
                // No video for Win Store version.  Just bail.
                SceneManager.SwitchToScene(onExitScene);
#endif
                // Does user want to quit?
                if (KoiLibrary.LastTouchedDeviceIsGamepad)
                {
                    GamePadInput pad0 = GamePadInput.GetGamePad0();
                    if (pad0.ButtonB.WasPressed || pad0.Back.WasPressed)
                    {
                        SceneManager.SwitchToScene(onExitScene);
                    }
                }

                try
                {
                    if (video == null)
                    {
                        // Start video.
                        video = BokuGame.Load<Video>(BokuGame.Settings.MediaPath + @"Video\Intro");
                        player = new VideoPlayer();
                        player.IsLooped = false;
                        player.Play(video);
                    }

                    // Check if we're done with the video or the user hit escape to skip.
                    if (player.State != MediaState.Playing)
                    {
                        SceneManager.SwitchToScene(onExitScene);
                    }
                }
                catch (Exception e)
                {
                    if (e != null)
                    {
                    }

                    // Something failed with the video so just pretend
                    // we never intended to go there anyway.
                    XmlOptionsData.ShowIntroVideo = false;

                    SceneManager.SwitchToScene(onExitScene);
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

            device.Clear(Color.Black);

            Texture2D vid = player.GetTexture();
            int w = device.Viewport.Width;
            int h = device.Viewport.Height;
            float scale = (float)w / vid.Width;

            Vector2 size = new Vector2(w, vid.Height * scale);
            Vector2 pos = new Vector2(0, (h - size.Y) / 2.0f);

            batch.Begin();
            batch.Draw(vid, new Rectangle((int)pos.X, (int)pos.Y, (int)size.X, (int)size.Y), Color.White);
            batch.End();

            if (rt != null)
            {
                device.SetRenderTarget(null);
            }
        }   // end of Render()

        public override void RegisterForEvents()
        {
            KoiLibrary.InputEventManager.RegisterForEvent(this, InputEventManager.Event.Keyboard);
        }   // end of RegisterForEvents()

        public override void UnregisterForEvents()
        {
            base.UnregisterForEvents();
        }   // end of UnregisterForEvents()

        /// <summary>
        /// Activate this scene.
        /// Shouldn't be called by user code.  Is called by SceneManager when 
        /// switching scenes.
        /// </summary>
        /// <param name="args">IntroVideoScene needs to know where to return on exit.  Should be a BaseScene or string with the scene name.</param>
        public override void Activate(params object[] args)
        {
            if (!Active)
            {
                onExitScene = null;

                Debug.Assert(args.Length == 1);

                if (args[0] is BaseScene)
                {
                    onExitScene = args[0] as BaseScene;
                }
                else if (args[0] is string)
                {
                    onExitScene = SceneManager.GetSceneFromName(args[0] as string);
                }

                Debug.Assert(onExitScene != null, "This scene needs to know where to return to on exit.");

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
                if (player != null)
                {
                    player.Stop();
                    player.Dispose();
                }
                video = null;

                XmlOptionsData.ShowIntroVideo = false;

                base.Deactivate();
            }
        }   // end of default Deactivate()

        #endregion

        #region InputEventHandler

        public override bool ProcessKeyboardEvent(KeyInput input)
        {
            Debug.Assert(Active);

            if (input.Key == Keys.Escape)
            {
                SceneManager.SwitchToScene(onExitScene);
                return true;
            }
            
            return base.ProcessKeyboardEvent(input);
        }

        #endregion

        #region Internal

        public override void LoadContent()
        {
            base.LoadContent();
        }

        public override void UnloadContent()
        {
            base.UnloadContent();
        }

        #endregion


    }   // end of class IntroVideoScene

}   // end of namespace KoiX.Scenes
