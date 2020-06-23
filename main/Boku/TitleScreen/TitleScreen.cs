
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Storage;

using Boku.Base;
using Boku.Common;
using Boku.Audio;
using Boku.Fx;

namespace Boku
{
    public class TitleScreen : GameObject, INeedsDeviceReset
    {
        protected class Shared
        {
            public bool waitMode = false;
        }

        protected class UpdateObj : UpdateObject
        {
            private Shared shared = null;

            public UpdateObj(ref Shared shared)
            {
                this.shared = shared;
            }   // end of UpdateObj c'tor

            public override void Update()
            {
            }   // end of UpdateObj Update()

            public override void Activate()
            {
            }
            
            public override void Deactivate()
            {
            }
        }   // end of class UpdateObj

        protected class RenderObj : RenderObject, INeedsDeviceReset
        {
            private Shared shared = null;

            private Texture2D backgroundTexture = null;
            private Texture2D logoTexture = null;
            private Texture2D dotTexture = null;
            private Texture2D waitTexture = null;

            private float kMaxRadius = 32.0f;

            public class Dot
            {
                public Vector2 position;         // Center of dot, screen coords.
                public float radius = 32.0f;     // In pixels.
                public float alpha = 1.0f;
            }

            private Dot[] dots = null;

            public RenderObj(ref Shared shared)
            {
                this.shared = shared;

                dots = new Dot[4];
                for (int i = 0; i < 4; i++)
                {
                    dots[i] = new Dot();
                    dots[i].position = new Vector2(413 + 56 * i, 280);
                }

            }   // end of RenderObj c'tor

            public override void Render(Camera camera)
            {
                GraphicsDevice device = BokuGame.bokuGame.GraphicsDevice;

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

                SpriteBatch batch = UI2D.Shared.SpriteBatch;

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

                    // If in wait mode, show texture.
                    if (shared.waitMode)
                    {
                        Vector2 size = new Vector2(waitTexture.Width, waitTexture.Height);
                        Vector2 pos = screenSize * 0.5f - size * 0.5f;
                        pos.Y -= 50;
                        batch.Draw(waitTexture, new Rectangle((int)pos.X, (int)pos.Y, (int)size.X, (int)size.Y), Color.White);
                    }
                }
                batch.End();
                
            }   // end of Render()
            
            public override void Activate()
            {
            }

            public override void Deactivate()
            {
            }

            public void LoadContent(bool immediate)
            {
                // Load the textures.
                if (backgroundTexture == null)
                {
                    backgroundTexture = BokuGame.Load<Texture2D>(BokuGame.Settings.MediaPath + @"Textures\Loading");
                }
                if (logoTexture == null)
                {
                    logoTexture = BokuGame.Load<Texture2D>(BokuGame.Settings.MediaPath + @"Textures\MicrosoftLogo");
                }
                if (dotTexture == null)
                {
                    dotTexture = BokuGame.Load<Texture2D>(BokuGame.Settings.MediaPath + @"Textures\LoadingDot");
                }
                if (waitTexture == null)
                {
                    waitTexture = BokuGame.Load<Texture2D>(BokuGame.Settings.MediaPath + @"Textures\Terrain\WaitPicture");
                }

            }   // end of TitleScreen RenderObj LoadContent()

            public void InitDeviceResources(GraphicsDevice device)
            {
            }

            public void UnloadContent()
            {
                BokuGame.Release(ref backgroundTexture);
                BokuGame.Release(ref logoTexture);
                BokuGame.Release(ref dotTexture);
                BokuGame.Release(ref waitTexture);
            }   // end of TitleScreen RenderObj UnloadContent()

            /// <summary>
            /// Recreate render targets
            /// </summary>
            /// <param name="graphics"></param>
            public void DeviceReset(GraphicsDevice device)
            {
            }

        }   // end of class RenderObj

        //
        //  TitleScreen
        //

        private RenderObj renderObj = null;
        private UpdateObj updateObj = null;
        private Shared shared = null;

        private enum States
        {
            Inactive,
            Active,
        }
        private States state = States.Inactive;
        private States pendingState = States.Inactive;

        public bool WaitMode
        {
            set { shared.waitMode = value; }
        }

        // c'tor
        public TitleScreen()
        {
            shared = new Shared();

            renderObj = new RenderObj(ref shared);
            updateObj = new UpdateObj(ref shared);
            
        }   // end of TitleScreen c'tor

        private void ContentLoadComplete()
        {
            WaitMode = true;
        }

#if NETFX_CORE
        /// <summary>
        /// Hacked render call to help make the WinRT startup look better.
        /// </summary>
        public void Render()
        {
            renderObj.UnloadContent();
            renderObj.LoadContent(true);
            renderObj.Render(null);
        }
#endif

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

                    BokuGame.Audio.PlayStartupSound();
                }
                else
                {
                    updateObj.Deactivate();
                    updateList.Remove(updateObj);
                    renderObj.Deactivate();
                    renderList.Remove(renderObj);

                    // Since we never come back to this object, flush the device dependent textures.
                    UnloadContent();
                }

                state = pendingState;
            }

            return result;
        }   // end of TitleScreen Refresh()
        override public void Activate()
        {
            if (state != States.Active)
            {
                pendingState = States.Active;
                BokuGame.objectListDirty = true;

#if !NETFX_CORE
                // Bring window to top.
                bool prevTopMost = MainForm.Instance.TopMost;
                MainForm.Instance.TopMost = true;
                MainForm.Instance.TopMost = prevTopMost;
#endif
            }
        }
        override public void Deactivate()
        {
            if (state != States.Inactive)
            {
                pendingState = States.Inactive;
                BokuGame.objectListDirty = true;

#if !NETFX_CORE
                // Bring window to top.
                bool prevTopMost = MainForm.Instance.TopMost;
                MainForm.Instance.TopMost = true;
                MainForm.Instance.TopMost = prevTopMost;
#endif
            }
        }

        #region INeedsDeviceReset Members

        public void LoadContent(bool immediate)
        {
            BokuGame.Load(renderObj, immediate);
        }   // end of TitleScreen LoadContent()

        public void InitDeviceResources(GraphicsDevice device)
        {
        }

        public void UnloadContent()
        {
            BokuGame.Unload(renderObj);
        }   // end of TitleScreen UnloadContent()

        /// <summary>
        /// Recreate render targets
        /// </summary>
        /// <param name="graphics"></param>
        public void DeviceReset(GraphicsDevice device)
        {
            BokuGame.DeviceReset(renderObj, device);
        }

        #endregion
    }   // end of class TitleScreen

}   // end of namespace Boku
