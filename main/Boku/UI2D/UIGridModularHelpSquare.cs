
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

using Boku.Audio;
using Boku.Common;
using Boku.Fx;

namespace Boku.UI2D
{
    public class UIGridModularHelpSquare
    {
        #region Members

        private Texture2D texture;
        private Vector2 position;
        private float size;
        private float alpha;

        private double showTime = 0.0;  // Time when Show was called.
        private float delayTime = 0.6f;
        private float fadeTime = 0.5f;
        private bool hide = false;

        #endregion

        #region Accessors

        public Vector2 Position
        {
            get { return position; }
            set { position = value; }
        }
        public float Size
        {
            get { return size; }
            set { size = value; }
        }
        public bool Hidden
        {
            get { return hide; }
        }

        #endregion

        #region Public

        /// <summary>
        /// To be called to show the help square.  Should not be call every frame.
        /// </summary>
        public void Show()
        {
            showTime = Time.WallClockTotalSeconds;
            hide = false;
        }   // end of Show()

        public void Hide()
        {
            hide = true;
        }   // end of Hide()

        public void Update()
        {
            if (texture == null)
            {
                GraphicsDevice device = BokuGame.bokuGame.GraphicsDevice;
                RenderTarget2D rt = UI2D.Shared.RenderTarget128_128;
                InGame.SetRenderTarget(rt);

                InGame.Clear(Color.Transparent);

                // Background.
                ScreenSpaceQuad quad = ScreenSpaceQuad.GetInstance();
                Texture2D background = BokuGame.Load<Texture2D>(BokuGame.Settings.MediaPath + @"Textures\GridElements\BlackSquare");
                quad.Render(background, Vector2.Zero, new Vector2(rt.Width, rt.Height), "TexturedRegularAlpha");

                // Y button
                Vector2 size = new Vector2(64, 64);
                quad.Render(ButtonTextures.YButton, new Vector2(44, 24), size, "TexturedRegularAlpha");

                // Text.
                Color color = Color.Yellow;
                SpriteBatch batch = UI2D.Shared.SpriteBatch;
                UI2D.Shared.GetFont Font = UI2D.Shared.GetGameFont24Bold;
                Vector2 position = new Vector2(0, 120 - Font().LineSpacing);
                position.X = 64 - 0.5f * Font().MeasureString(Strings.Localize("editObjectParams.help")).X;
                batch.Begin();
                TextHelper.DrawString(Font, Strings.Localize("editObjectParams.help"), position, color);
                batch.End();
                
                InGame.RestoreRenderTarget();

                texture = new Texture2D(device, 128, 128, false, SurfaceFormat.Color);

                // Copy rendertarget result into texture.
                int[] data = new int[128 * 128];
                rt.GetData<int>(data);
                texture.SetData<int>(data);
            }

            double now = Time.WallClockTotalSeconds;

            if (now - showTime < delayTime || hide)
            {
                // Still in delay.
                alpha = 0.0f;
            }
            else
            {
                // Either in fade or in full view.
                float t = (float)(now - showTime - delayTime) / fadeTime;
                alpha = Math.Min(t, 1.0f);
            }

        }   // end of Update()

        public void Render(Camera camera)
        {
            if (alpha > 0.0f)
            {
                CameraSpaceQuad quad = CameraSpaceQuad.GetInstance();
                Vector2 s = new Vector2(size);
                quad.Render(camera, texture, alpha, position, s, "TexturedRegularAlpha");
            }
        }   // end of Render()

        #endregion

    }   // end of class UIGridModularHelpSquare


}   // end of namespace Boku.UI2D