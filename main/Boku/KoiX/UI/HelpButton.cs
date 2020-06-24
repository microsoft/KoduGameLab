
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.Serialization.Formatters.Binary;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using KoiX;
using KoiX.Input;

namespace KoiX.UI
{
    /// <summary>
    /// Specialization of GraphicButton.
    /// </summary>
    public class HelpButton : GraphicButton
    {
        #region Members

        string gamepadTextureName;
        Texture2D gamepadTexture;

        // Alpha blend used between normal and gamepad textures 
        // depending on current input mode.
        Twitchable<float> gamepadAlpha;

        #endregion

        #region Accessors
        #endregion

        #region Public

        public HelpButton(BaseDialog parentDialog, Callback OnHelp, object data = null)
            : base(parentDialog, RectangleF.EmptyRect, "", OnHelp, data: data)
        {
            textureName = @"KoiXContent\Textures\QuestionMarkCircle64";
            gamepadTextureName = @"KoiXContent\Textures\QuestionMarkCircleYButton64";
            Focusable = false;
            Size = new Vector2(48, 48);
            Margin = new Padding(8, 8, 32, 8);

            gamepadAlpha = new Twitchable<float>(Theme.TwitchTime, TwitchCurve.Shape.EaseOut);
        }

        public override void Update(SpriteCamera camera, Vector2 parentPosition)
        {
            gamepadAlpha.Value = KoiLibrary.LastTouchedDeviceIsGamepad ? 1.0f : 0.0f;

            base.Update(camera, parentPosition);
        }

        public override void Render(SpriteCamera camera, Vector2 parentPosition)
        {
            SpriteBatch batch = KoiLibrary.SpriteBatch;
            bool gamepad = KoiLibrary.LastTouchedDeviceIsGamepad;

            RectangleF renderRect = localRect;
            renderRect.Position += parentPosition;
            Matrix viewMatrix = camera != null ? camera.ViewMatrix : Matrix.Identity;
            batch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, samplerState: null, depthStencilState: null, rasterizerState: null, effect: null, transformMatrix: viewMatrix);
            {
                if (gamepadAlpha.Value < 1)
                {
                    batch.Draw(texture, renderRect.ToRectangle(), Color.White);
                }
                if (gamepadAlpha.Value > 0)
                {
                    batch.Draw(gamepadTexture, renderRect.ToRectangle(), Color.White * gamepadAlpha.Value);
                }
            }
            batch.End();
        }

        #endregion

        #region Internal

        public override void LoadContent()
        {
            if (gamepadTexture == null || gamepadTexture.IsDisposed || gamepadTexture.GraphicsDevice.IsDisposed)
            {
                gamepadTexture = KoiLibrary.LoadTexture2D(gamepadTextureName);
            }

            base.LoadContent();
        }

        public override void UnloadContent()
        {
            DeviceResetX.Release(ref gamepadTexture);

            base.UnloadContent();
        }

        #endregion

    }   // end of class HelpButton
}   // end of namespace KoiX.UI
