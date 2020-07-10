// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.


/// Relocated from Boku.Common namespace

using System;
using System.Collections;
using System.Diagnostics;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Storage;

using Boku.Base;

namespace Boku.Fx
{
    /// <summary>
    /// Simple filters designed to support copying one texture into another allowing for
    /// positioning and scaling of the source texture.  Also allows for targeting specific
    /// channels in the destination texture for use when editing terrain select textures.
    /// This is a little different from the other filters in that this one doesn't always
    /// cover the full rendertarget.
    /// </summary>
    public class EditFilter : BaseFilter
    {
        // c'tor
        public EditFilter()
            :
            base()
        {
        }   // end of EditFilter c'tor

        /// <summary>
        /// Updates the vertex positions based on the position and size of the
        /// quad we're trying to edit into the rendertarget.
        /// </summary>
        /// <param name="position">The center of the quad.</param>
        /// <param name="size">The radius in X and Y directions relative to the quad size.</param>
        private void SetUvToPos(Vector2 position, Vector2 size)
        {
            GraphicsDevice device = BokuGame.bokuGame.GraphicsDevice;

            // Remap position from 0,1 range to -1,1.
            position = position * 2.0f - new Vector2(1.0f, 1.0f);

            // Now check the dimensions of the destination
            int width = device.Viewport.Width;
            int height = device.Viewport.Height;

            Vector2 min = position - size;
            Vector2 max = position + size;

            Vector4 uvToPos = new Vector4(
                max.X - min.X, // x scale
                max.Y - min.Y, // y scale
                -max.X, // x offset
                min.Y); // y offset

            effect.Parameters["UvToPos"].SetValue(uvToPos);

        }   // end of BaseFilter SetUvToPos()


        /// <summary>
        /// 
        /// </summary>
        /// <param name="source">Our brush texture.</param>
        /// <param name="index">Which channel to paint into.  0 is default(just erase from others), 1 is red, 2 is green, 3 is blue.</param>
        /// <param name="position">Center of brush.</param>
        /// <param name="radius">Radius of brush, may be non-circular.</param>
        /// <param name="alpha">Transparency assigned to brush.</param>
        public void Render(Texture2D source, int index, Vector2 position, Vector2 radius, float alpha)
        {
            GraphicsDevice device = BokuGame.bokuGame.GraphicsDevice;

            SetUvToPos(position, radius);

            Vector4 color = new Vector4(0.0f, 0.0f, 0.0f, 1.0f);
            switch (index)
            {
                case 0:
                    break;
                case 1:
                    color.X = 1.0f;
                    break;
                case 2:
                    color.Y = 1.0f;
                    break;
                case 3:
                    color.Z = 1.0f;
                    break;
            }

            // Store away the current channels.


            effect.Parameters["SourceTexture"].SetValue(source);
            effect.Parameters["Color"].SetValue(color);
            effect.Parameters["Alpha"].SetValue(alpha);

            effect.CurrentTechnique = effect.Techniques["Edit"];

            device.SetVertexBuffer(vbuf);

            // Render all passes.
            for (int i = 0; i < effect.CurrentTechnique.Passes.Count; i++)
            {
                EffectPass pass = effect.CurrentTechnique.Passes[i];
                pass.Apply();
                device.DrawPrimitives(PrimitiveType.TriangleStrip, 0, 2);
            }

        }   // end of EditFilter Render()


        public override void LoadContent(bool immediate)
        {
            // Init the effect.
            if (effect == null)
            {
                effect = BokuGame.Load<Effect>(BokuGame.Settings.MediaPath + @"Shaders\EditFilter");
            }

            base.LoadContent(immediate);
        }   // end of EditFilter LoadContent()

    }   // end of class EditFilter

}   // end of Boku.Common



