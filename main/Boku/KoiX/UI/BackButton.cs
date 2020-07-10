// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.


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
using KoiX.Geometry;


namespace KoiX.UI
{
    /// <summary>
    /// Thin wrapper around GraphicsButton since I expect this one to be used a lot.
    /// </summary>
    public class BackButton : BaseButton
    {
        #region Members

        static string textureName = @"KoiXContent\Textures\LeftArrow";
        static Texture2D texture;

        static float outlineWidth = 6.0f;

        // Alpha blend used between normal and gamepad textures 
        // depending on current input mode.
        Twitchable<float> gamepadAlpha;

        #endregion

        #region Accessors
        #endregion

        #region Public

        public BackButton(BaseDialog parentDialog, RectangleF rect, Callback onSelect = null, ThemeSet theme = null)
            : base(parentDialog, onSelect, theme:theme)
        {
#if DEBUG
            _name = "BackButton";
#endif

            // Use padding to adjust the texture size.
            Padding = new Padding(24, 24, 24, 24);
            Margin = new Padding(8, 8, 8, 8);
            Size = new Vector2(96, 96);

            gamepadAlpha = new Twitchable<float>(Theme.TwitchTime, TwitchCurve.Shape.EaseOut);
        }   // end of c'tor

        public override void Update(SpriteCamera camera, Vector2 parentPosition)
        {
            gamepadAlpha.Value = KoiLibrary.LastTouchedDeviceIsGamepad ? 1.0f : 0.0f;

            base.Update(camera, parentPosition);
        }   // end of Update()

        public override void Render(SpriteCamera camera, Vector2 parentPosition)
        {
            // Scale the outline and padding to work with the current screen size.
            Vector2 center = localRect.Center + parentPosition;

            if (gamepadAlpha.Value < 1)
            {
                // Render with arrow.
                Disc.Render(camera, center, localRect.Width / 2.0f, Color.White,
                            outlineColor: Color.Black, outlineWidth: outlineWidth,
                            texture: texture, texturePadding: Padding);
            }
            if (gamepadAlpha.Value > 0)
            {
                // Render with back button.
                Padding padding = new UI.Padding(24, 72, 24, 72);
                Disc.Render(camera, center, localRect.Width / 2.0f, Theme.CurrentThemeSet.DisabledLightColor * gamepadAlpha.Value,
                            outlineColor: Theme.CurrentThemeSet.DisabledDarkColor * gamepadAlpha.Value, outlineWidth: outlineWidth,
                            texture: Textures.Get("GamePad Back"), texturePadding: padding);
            }

            // For debugging...
            base.Render(camera, parentPosition);
        }   // end of Render()

        #endregion

        #region InputEventHandler

        public override bool ProcessGamePadEvent(GamePadInput pad)
        {
            Debug.Assert(Active);

            if (pad.Back.WasPressed)
            {
                OnButtonSelect();
                pad.Back.ClearAllWasPressedState();
                return true;
            }

            return base.ProcessGamePadEvent(pad);
        }

        #endregion

        #region Internal

        public override void LoadContent()
        {
            texture = KoiLibrary.LoadTexture2D(textureName);

            base.LoadContent();
        }

        public override void UnloadContent()
        {
            DeviceResetX.Release(ref texture);

            base.UnloadContent();
        }

        #endregion
    }   // end of class BackButton

}   // end of namespace KoiX.UI
