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
using KoiX.Text;


namespace KoiX.UI
{
    /// <summary>
    /// Thin wrapper around GraphicsButton since I expect this one to be used a lot.
    /// Allows for an optional label.
    /// </summary>
    public class PrevButton : BaseButton
    {
        #region Members

        static string textureName = @"KoiXContent\Textures\LeftArrow";
        static Texture2D texture;

        static float outlineWidth = 6.0f;

        // Alpha blend used between normal and gamepad textures 
        // depending on current input mode.
        Twitchable<float> gamepadAlpha;

        Label label;
        SystemFont font;

        #endregion

        #region Accessors
        #endregion

        #region Public

        public PrevButton(BaseDialog parentDialog, RectangleF rect, string labelId = null, string labelText = null, Callback onSelect = null, ThemeSet theme = null)
            : base(parentDialog, onSelect, theme: theme)
        {
#if DEBUG
            _name = "PrevButton";
#endif

            // Use padding to adjust the texture size.
            // Arrow texture itself ends up displayed at 48, 48.
            // Size - Padding = textureSize.
            Padding = new Padding(80, 8, 16, 8);
            Size = new Vector2(144, 64);
            // Position so that end hangs off side of screen.
            Margin = new Padding(-56, 8, 8, 8);

            if (labelId != null || labelText != null)
            {
                font = new SystemFont(Theme.CurrentThemeSet.TextFontFamily, Theme.CurrentThemeSet.TextTitleFontSize * 1.2f, System.Drawing.FontStyle.Regular);
                label = new Label(parentDialog, font,
                    color: Theme.CurrentThemeSet.DarkTextColor,
                    //outlineColor: Theme.CurrentThemeSet.DarkTextColor, outlineWidth: Theme.CurrentThemeSet.BaseOutlineWidth,
                    labelId: labelId, labelText: labelText);
                label.Size = label.CalcMinSize().Round();
                parentDialog.AddWidget(label);
            }

            gamepadAlpha = new Twitchable<float>(Theme.TwitchTime, TwitchCurve.Shape.EaseOut);
        }   // end of c'tor

        public override void Update(SpriteCamera camera, Vector2 parentPosition)
        {
            gamepadAlpha.Value = KoiLibrary.LastTouchedDeviceIsGamepad ? 1.0f : 0.0f;

            base.Update(camera, parentPosition);
        }   // end of Update()

        public override void Render(SpriteCamera camera, Vector2 parentPosition)
        {
            if (Active && !Disabled)
            {
                RectangleF rect = localRect;
                rect.Position += parentPosition;
                float radius = localRect.Height / 2.0f;

                float labelWidth = 0;
                if (label != null)
                {
                    labelWidth = label.CalcMinSize().X;
                }

                if (gamepadAlpha.Value < 1)
                {
                    // Render with arrow.
                    RoundedRect.Render(camera, rect, radius, Color.White,
                                outlineColor: Color.Black, outlineWidth: outlineWidth,
                                texture: texture, texturePadding: Padding);
                }
                if (gamepadAlpha.Value > 0)
                {
                    // Render with right bumper.
                    Padding padding = new UI.Padding(64, 16, 16, 16);
                    RoundedRect.Render(camera, rect, radius, Theme.CurrentThemeSet.DisabledLightColor * gamepadAlpha.Value,
                                outlineColor: Theme.CurrentThemeSet.DisabledDarkColor * gamepadAlpha.Value, outlineWidth: outlineWidth,
                                texture: Textures.Get("GamePad LeftBumper"), texturePadding: padding);
                }
                if (label != null)
                {
                    label.Position = new Vector2(localRect.Right, localRect.Top);
                }

                // For debugging...
                base.Render(camera, parentPosition);
            }
        }   // end of Render()

        #endregion

        #region InputEventHandler

        public override InputEventHandler HitTest(Vector2 hitLocation)
        {
            InputEventHandler result = null;

            // Test button and label for hits.
            if (localRect.Contains(hitLocation) || (label != null && label.LocalRect.Contains(hitLocation)))
            {
                result = this;
            }

            return result;
        }   // end of HitTest()

        public override bool ProcessGamePadEvent(GamePadInput pad)
        {
            Debug.Assert(Active);

            if (pad.LeftShoulder.WasPressed)
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
    }   // end of class PrevButton

}   // end of namespace KoiX.UI
