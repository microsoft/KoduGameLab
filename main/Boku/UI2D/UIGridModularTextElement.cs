// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.


using System;
using System.Collections;

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
    /// An instance of UIElement that uses a 9-grid element for its geometry
    /// and creates a texture on the fly into which the text string is rendered.
    /// </summary>
    public class UIGridModularTextElement : UIGridElement
    {
        private Texture2D tile = null;

        private float width;    // Width in screen units (~inches @ 96dpi)
        private float height;   // Height in screen units (~inches @ 96dpi)

        private bool selected = false;

        private string label = null;
        private Vector4 textColor = Vector4.One;    // The color we're rendering with.
        private Color selectedTextColor;
        private Color unselectedTextColor;
        private Justification justify = Justification.Center;

        private RenderTarget2D rt = null;       // Pre-render the button contents here.

        #region Accessors
        public override bool Selected
        {
            get { return selected; }
            set
            {
                if (selected != value)
                {
                    if (value)
                    {
                        // Create a twitch to change to selected color
                        TwitchManager.Set<Vector4> set = delegate(Vector4 val, Object param) { textColor = val; RefreshRT(); };
                        TwitchManager.CreateTwitch<Vector4>(textColor, selectedTextColor.ToVector4(), set, 0.15, TwitchCurve.Shape.OvershootOut);
                    }
                    else
                    {
                        // Create a twitch to change to unselected color.
                        TwitchManager.Set<Vector4> set = delegate(Vector4 val, Object param) { textColor = val; RefreshRT(); };
                        TwitchManager.CreateTwitch<Vector4>(textColor, unselectedTextColor.ToVector4(), set, 0.15, TwitchCurve.Shape.EaseOut);
                    }
                    selected = value;
                }
            }
        }
        public override string Label
        {
            get { return label; }
            set { label = value; }
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
        /// <summary>
        /// Simple c'tor using a blob to hold the common data.
        /// </summary>
        /// <param name="blob"></param>
        /// <param name="label"></param>
        public UIGridModularTextElement(ParamBlob blob, string label)
        {
            this.label = TextHelper.FilterInvalidCharacters(label);

            // blob
            this.width = blob.width;
            this.height = blob.height;

            this.Font = blob.Font;
            this.selectedTextColor = blob.selectedTextColor;
            this.unselectedTextColor = blob.unselectedTextColor;
            this.justify = blob.justify;

            this.textColor = unselectedTextColor.ToVector4();
        }

        public override void HandleMouseInput(Vector2 hitUV)
        {
        }   // end of HandleMouseInput()

        public override void HandleTouchInput(TouchContact touch, Vector2 hitUV)
        {
        }   // end of HandleTouchInput()

        public override void Update(ref Matrix parentMatrix)
        {
            if (rt.IsContentLost)
            {
                RefreshRT();
            }
            
            base.Update(ref parentMatrix);
        }

        public override void Render(Camera camera)
        {
            Vector2 pos = new Vector2(worldMatrix.Translation.X, worldMatrix.Translation.Y);

            // Get position of center of tile.
            pos = camera.WorldToScreenCoordsVector2(new Vector3(pos.X, pos.Y, 0.0f));

            // Calc screen space position for underlying tile.
            Vector2 tileSize = camera.WorldToScreenCoordsVector2(new Vector3(width, -height, 0.0f)) - new Vector2(camera.Resolution.X, camera.Resolution.Y) / 2.0f;
            Vector2 tilePos = pos - tileSize / 2.0f;

            // Render tile.
            ScreenSpaceQuad ssquad = ScreenSpaceQuad.GetInstance();
            ssquad.Render(rt, tilePos, tileSize, "TexturedRegularAlpha");

            /*
            // Calc position of text and render.
            pos -= Font().MeasureString(label) / 2.0f;

            TextHelper.DrawStringNoBatch(Font, label, pos, new Color(textColor));
            */
        }   // end of UIGridModularTextElement Render()

        private void RefreshRT()
        {
            GraphicsDevice device = BokuGame.bokuGame.GraphicsDevice;

            Vector2 textSize = Font().MeasureString(label);

            RenderTarget2D textRT = UI2D.Shared.RenderTarget512_302;

            // If label is too wide for button, render it to an extra RT and shrink it down.
            int margin = 4;
            float compressionFactor = (rt.Width - 2 * margin) / textSize.X; // If this is <1 we need to compress.
            if (compressionFactor < 1.0f)
            {
                // Text is too wide...
                InGame.SetRenderTarget(textRT);
                device.Clear(Color.Transparent);
                TextHelper.DrawStringNoBatch(Font, label, new Vector2(1, 1), new Color(textColor));
                InGame.RestoreRenderTarget();
            }

            InGame.SetRenderTarget(rt);
            device.Clear(Color.Transparent);

            // Button background.
            Vector2 buttonSize = new Vector2(rt.Width, rt.Height);
            ScreenSpaceQuad ssquad = ScreenSpaceQuad.GetInstance();
            ssquad.Render(tile, Vector2.Zero, buttonSize, "TexturedRegularAlpha");


            if (compressionFactor < 1.0f)
            {
                // Compress text to fit button.
                Vector2 pos = new Vector2(margin, (buttonSize.Y - textSize.Y) / 2.0f - 1);
                Vector2 size = new Vector2(textRT.Width * compressionFactor, textRT.Height);
                ssquad.Render(textRT, pos, size, "TexturedRegularAlpha");
            }
            else
            {
                // Center text onto button.
                Vector2 pos = (buttonSize - textSize) / 2.0f;
                TextHelper.DrawStringNoBatch(Font, label, pos, new Color(textColor));
            }

            InGame.RestoreRenderTarget();
        }   // end of RefreshRT()

        public override void LoadContent(bool immediate)
        {
            const int dpi = 96;
            int w = (int)(dpi * width);
            int h = (int)(dpi * height);

            if (tile == null)
            {
                tile = BokuGame.Load<Texture2D>(BokuGame.Settings.MediaPath + @"Textures\GridElements\BlackTextTile");
            }

            if (rt == null)
            {
                rt = new RenderTarget2D(BokuGame.bokuGame.GraphicsDevice, w, h);
            }
        }   // end of UIGridModularTextElement LoadContent()

        public override void InitDeviceResources(GraphicsDevice device)
        {
            RefreshRT();
        }

        public override void UnloadContent()
        {
            base.UnloadContent();

            BokuGame.Release(ref tile);
            BokuGame.Release(ref rt);
        }   // end of UIGridModularTextElement UnloadContent()

        /// <summary>
        /// Recreate render targets
        /// </summary>
        /// <param name="graphics"></param>
        public override void DeviceReset(GraphicsDevice device)
        {
            // If there's a problem with the rendertarget, get rid of it.
            if (rt == null || rt.IsDisposed || rt.GraphicsDevice.IsDisposed)
            {
                BokuGame.Release(ref rt);
            }

            LoadContent(true);
            InitDeviceResources(BokuGame.bokuGame.GraphicsDevice);
        }

    }   // end of class UIGridModularTextElement

}   // end of namespace Boku.UI2D






