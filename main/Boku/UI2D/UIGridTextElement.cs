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

namespace Boku.UI2D
{
    /// <summary>
    /// An instance of UIElement that is designed to just hold a text string.  The rendering
    /// is a hack that just draws the texture and then draws the string over the top of it.
    /// This means that you can't tip/tilt/turn it and get the text to line up.
    /// </summary>
    public class UIGridTextElement : UIGridElement
    {
        private SimpleTexturedQuad quad = null;
        private Texture texture = null;
        private String textureName = null;
        private Vector2 size = new Vector2(1.0f, 1.0f);

        private String label = null;

        // TODO I don't think this class is actually used except for a quick hack so this stuff isn't realy hooked up...
        private Vector4 baseColor;          // Color that shows through where the texture is transparent.
        private Vector4 selectedColor = new Vector4(1f,1f,1f,1f);
        private Vector4 unselectedColor = new Vector4(1f, 1f, 1f, 1f);
        private bool selected = false;

        #region Accessors
        public override Vector2 Size
        {
            get { return size; }
            set { size = value; }
        }
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
                        TwitchManager.GetVector4 get = delegate(Object param) { return baseColor; };
                        TwitchManager.SetVector4 set = delegate(Vector4 val, Object param) { baseColor = val; };
                        TwitchManager.Vector4Twitch twitch = new TwitchManager.Vector4Twitch(get, set, selectedColor, 0.15f, TwitchCurve.Shape.EaseInOut);
                        twitch.Start();
                    }
                    else
                    {
                        // Create a twitch to change to unselected color.
                        TwitchManager.GetVector4 get = delegate(Object param) { return baseColor; };
                        TwitchManager.SetVector4 set = delegate(Vector4 val, Object param) { baseColor = val; };
                        TwitchManager.Vector4Twitch twitch = new TwitchManager.Vector4Twitch(get, set, unselectedColor, 0.15f, TwitchCurve.Shape.EaseInOut);
                        twitch.Start();
                    }
                    selected = value;
                }
            }
        }
        public String Label
        {
            get { return label; }
        }
        #endregion

        // c'tor
        public UIGridTextElement(String textureName, Vector2 size, String label)
        {
            this.size = size;
            this.label = label;
            this.textureName = textureName;

            quad = SimpleTexturedQuad.GetInstance();

        }   // end of UIGridTextElement c'tor

        public void Update(Camera camera)
        {
            Matrix parentMatrix = Matrix.Identity;

            base.Update(ref parentMatrix);
        }   // end of UIGridTextElement Update()

        public override void Render(Camera camera)
        {
            // Add scaling to our world.
            Matrix world = worldMatrix;
            world.M11 *= size.X;
            world.M22 *= size.Y;

            quad.Render(camera, texture, ref world, baseColor.W);

            // Render the label on top.  The font rendering location is
            // the top left corner of the box the text is rendering into.
            // Note as long as we're rendering text directly to the screen
            // instead of to a texture we can rotate it or tip it in 3D.
            Vector3 position = Position;
            position.X -= size.X / 2.0f;
            position.Y -= size.Y / 2.0f;
            Matrix textMatrix = worldMatrix;
            textMatrix.Translation -= Position; // Get back the parent matrix.
            position = Vector3.Transform(position, textMatrix);
            Point pos = camera.ToScreenCoords(position);

            if (pos.Y > -BokuGame.fontBerlinSansFBDemiBold20.Baseline && pos.Y < BokuGame.bokuGame.GraphicsDevice.Viewport.Height + BokuGame.fontBerlinSansFBDemiBold20.Baseline)
            {
                BokuGame.fontBerlinSansFBDemiBold20.DrawString(20 + pos.X, pos.Y - BokuGame.fontBerlinSansFBDemiBold20.Baseline - 5, Color.Black, label);
            }

        }   // end of UIGridTextElement Render()

        public override void LoadGraphicsContent(GraphicsDeviceManager graphics)
        {
            // Load the texture which acts as a frame/backdrop for the label.
            if (textureName != null && texture == null)
            {
                texture = BokuGame.ContentManager.Load<Texture2D>(BokuGame.Settings.MediaPath + @"Textures\" + textureName);
            }
        }   // end of UIGridTextElement LoadGraphicsContent()

        public override void UnloadGraphicsContent()
        {
            BokuGame.Release(ref texture);
        }   // end of UIGridTextElement UnloadGraphicsContent()

    }   // end of class UIGridTextElement

}   // end of namespace Boku.UI2D






