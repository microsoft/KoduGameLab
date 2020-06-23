
using System;
using System.Collections;
using System.Collections.Generic;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Storage;

using Boku.Common;
using Boku.Fx;

namespace Boku.UI2D
{
    /// <summary>
    /// An instance of UIElement that uses ScreenSpaceQuad to render a
    /// texture into which the background and text has been rendered.
    /// </summary>
    public class MessageBoxElement : UIGridElement
    {
        #region Members

        private static Effect effect = null;

        private RenderTarget2D diffuse = null;
        private Texture background = null;

        private float width = 512;
        private float height = 302;

        private String label = null;
        private Color textColor = Color.White;
        private Color dropShadowColor = new Color(30, 30, 30);
        private bool useDropShadow = true;
        private bool invertDropShadow = false;  // Puts the drop shadow above the regular letter instead of below.
        private Justification justify = Justification.Center;

        private Vector2 pos;

        #endregion

        #region Accessors

        public override bool Selected
        {
            get { return true; }
            set { }
        }

        public override string Label
        {
            get { return label; }
        }
        public Color TextColor
        {
            get { return textColor; }
        }
        public Color DropShadowColor
        {
            get { return dropShadowColor; }
        }
        public bool UseDropShadow
        {
            get { return useDropShadow; }
        }
        public bool InvertDropShadow
        {
            get { return invertDropShadow; }
        }
        public float Width
        {
            get { return width; }
        }
        public float Height
        {
            get { return height; }
        }

        public override Vector2 Size
        {
            get { return new Vector2(width, height); }
            set { /* do nothing, should be removed from base class */ }
        }

        #endregion

        // c'tor
        public MessageBoxElement(String label)
        {
            this.label = label;

            Font = UI2D.Shared.GetGameFont24;

            // Center box on screen.
            pos = new Vector2(BokuGame.bokuGame.GraphicsDevice.Viewport.Width, BokuGame.bokuGame.GraphicsDevice.Viewport.Height);
            pos = (pos - Size) * 0.5f;
        }

        public void Update()
        {
            Matrix parentMatrix = Matrix.Identity;

            base.Update(ref parentMatrix);
        }   // end of MessageBoxElement Update()

        public override void HandleMouseInput(Vector2 hitUV)
        {
        }   // end of HandleMouseInput()

        public override void HandleTouchInput(TouchContact touch, Vector2 hitUV)
        {
        }   // end of HandleTouchInput()

        public override void Render(Camera camera)
        {
            ScreenSpaceQuad ssquad = ScreenSpaceQuad.GetInstance();

            if (BokuGame.RequiresPowerOf2)
            {
                ssquad.Render(diffuse, pos, new Vector2(512, 512), @"TexturedRegularAlpha");
            }
            else
            {
                ssquad.Render(diffuse, pos, new Vector2(width, height), @"TexturedRegularAlpha");
            }

        }   // end of MessageBoxElement Render()

        public override void LoadContent(bool immediate)
        {
            // Load the background texture.
            if (background == null)
            {
                background = BokuGame.Load<Texture2D>(BokuGame.Settings.MediaPath + @"Textures\MessageBox\MessageBoxBackground");
            }

        }   // end of UIGridMessageBoxElement LoadContent()

        public override void InitDeviceResources(GraphicsDeviceManager graphics)
        {
            CreateRenderTargets(graphics);
        }

        public override void UnloadContent()
        {
            base.UnloadContent();

            ReleaseRenderTargets();

            BokuGame.Release(ref effect);
            BokuGame.Release(ref background);
        }   // end of UIGridMessageBoxElement UnloadContent()

        public override void DeviceReset(GraphicsDeviceManager graphics)
        {
            ReleaseRenderTargets();
            CreateRenderTargets(graphics);
        }

        private void CreateRenderTargets(GraphicsDeviceManager graphics)
        {
            GraphicsDevice device = graphics.GraphicsDevice;

            const int margin = 24;
            int w = (int)width;
            int h = (int)height;

            int backgroundWidth = w;
            int backgroundHeight = h;

            // Create the diffuse texture.
            if (BokuGame.RequiresPowerOf2)
            {
                w = MyMath.GetNextPowerOfTwo(w);
                h = MyMath.GetNextPowerOfTwo(h);
            }

            diffuse = new RenderTarget2D(
                device,
                w, h,
                1,
                SurfaceFormat.Color,
                MultiSampleType.None, 0,
                RenderTargetUsage.PlatformContents);
            InGame.GetRT("MessageBoxElement", diffuse);

            // Save off the current depth buffer.
            InGame.SetRenderTarget(diffuse);
            InGame.Clear(Color.Transparent);

            // Render the backdrop.
            ScreenSpaceQuad ssquad = ScreenSpaceQuad.GetInstance();
            ssquad.Render(background, Vector2.Zero, new Vector2(512, 512), @"TexturedPreMultAlpha");

            SpriteFont font = Font();

            List<string> lineList = new List<string>();
            TextHelper.SplitMessage(label, backgroundWidth - margin * 2, font, false, lineList);

            // Calc center of display.
            int y = (int)((backgroundHeight - font.LineSpacing) / 2.0f) - 2;
            int dy = font.LineSpacing;
            // Offset based on number of lines.
            y -= (int)(dy * (lineList.Count - 1) / 2.0f);

            SpriteBatch batch = UI2D.Shared.SpriteBatch;
            batch.Begin();

            for (int i = 0; i < lineList.Count; i++)
            {
                string line = lineList[i];

                // Render the label text into the texture.
                int x = 0;
                Vector2 textSize = font.MeasureString(line);

                x = TextHelper.CalcJustificationOffset(margin, backgroundWidth, (int)textSize.X, justify);

                if (useDropShadow)
                {
                    TextHelper.DrawStringWithShadow(font, batch, x, y, line, textColor, dropShadowColor, invertDropShadow);
                }
                else
                {
                    batch.DrawString(font, line, new Vector2(x, y), textColor);
                }

                y += dy;
            }   // end of i loop over lines in list.

            // Load button textures.
            Texture BButton = ButtonTextures.BButton;

            // Render the 'B' button.
            Vector2 size = new Vector2(56.0f, 56.0f);
            Vector2 position = new Vector2(w - 150, h - 40 - margin);
            // Hack for X600 compat.
            if (BokuGame.RequiresPowerOf2)
            {
                position = new Vector2(backgroundWidth - 350, backgroundHeight - size.Y - margin);
            }
            ssquad.Render(BButton, position, size, @"TexturedRegularAlpha");

            // And the text with it.
            {
                int x = (int)(position.X + 40);
                y = (int)(position.Y);
                String buttonLabel = @"Back";
                if (useDropShadow)
                {
                    TextHelper.DrawStringWithShadow(font, batch, x, y, buttonLabel, textColor, dropShadowColor, false);
                }
                else
                {
                    batch.DrawString(font, buttonLabel, new Vector2(x, y), textColor);
                }
            }

            batch.End();


            // Restore backbuffer and depth buffer.
            InGame.RestoreRenderTarget();
        }

        private void ReleaseRenderTargets()
        {
            InGame.RelRT("MessageBoxElement", diffuse);
            BokuGame.Release(ref diffuse);
        }

    }   // end of class MessageBoxElement

}   // end of namespace Boku.UI2D






