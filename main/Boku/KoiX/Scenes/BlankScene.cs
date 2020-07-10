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
using Microsoft.Xna.Framework.Storage;

using KoiX;
using KoiX.Input;
using KoiX.Managers;
using KoiX.Text;
using KoiX.UI;

using Boku;

namespace KoiX.Scenes
{
    public class BlankScene : BaseScene
    {
        #region Members

        Texture2D bkgTexture;

        #endregion

        #region Accessors
        #endregion

        #region Public

        // c'tor
        public BlankScene()
            : base("BlankScene")
        {
        }

        public override void Update()
        {
            if (Active)
            {
                // Get raw touch input.
                TouchCollection state = TouchPanel.GetState();

                foreach (TouchLocation loc in state)
                {
                    if (loc.State != TouchLocationState.Invalid)
                    {
                    }
                }

                if (KoiLibrary.LastTouchedDeviceIsGamepad)
                {
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

            string message = "this space intentionally left blank";
            Vector2 size = SharedX.GetGameFont30Bold().MeasureString(message);
            Vector2 pos = (new Vector2(KoiLibrary.ViewportSize.X, KoiLibrary.ViewportSize.Y) - size) / 2.0f;
            pos.X = (int)pos.X;
            pos.Y = (int)pos.Y;
            KoiX.Text.TextHelper.DrawStringNoBatch(SharedX.GetGameFont30Bold, message, pos, Color.LightGray, outlineColor: Color.Black, outlineWidth: 1.2f);

            if (rt != null)
            {
                device.SetRenderTarget(null);
            }
        }   // end of Render()

        public override void RegisterForEvents()
        {
            // Register to get mouse events.  
            KoiLibrary.InputEventManager.RegisterForEvent(this, InputEventManager.Event.MouseLeftDown);
            KoiLibrary.InputEventManager.RegisterForEvent(this, InputEventManager.Event.Keyboard);

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
                base.Deactivate();
            }
        }   // end of default Deactivate()

        #endregion

        #region InputEventHandler

        public override bool ProcessMouseLeftDownEvent(MouseInput input)
        {
            Debug.Assert(Active);

            // On left down, return prev scene.
            SceneManager.SwitchToScene(PrevScene, SceneManager.Transition.Fade, Color.Black, Main.SceneSwitchTime, TwitchCurve.Shape.EaseOut);

            return true;
        }   // end of ProcessMouseLeftDownEvent()

        public override bool ProcessKeyboardEvent(KeyInput input)
        {
            Debug.Assert(Active);

            // On any keyboard event, return to prev scene.
            SceneManager.SwitchToScene(PrevScene, SceneManager.Transition.Fade, Color.Black, Main.SceneSwitchTime, TwitchCurve.Shape.EaseOut);

            return true;
        }

        #endregion

        #region Internal

        public override void LoadContent()
        {
            /*
            if (bkgTexture == null)
            {
                bkgTexture = KoiLibrary.LoadTexture2D(@"Textures\GenericBkg");
            }
            */

            base.LoadContent();
        }

        public override void UnloadContent()
        {
            DeviceResetX.Release(ref bkgTexture);

            base.UnloadContent();
        }

        #endregion


    }   // end of class BlankScene

}   // end of namespace KoiX.Scenes
