// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.


using System;
using System.Collections;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.Serialization.Formatters.Binary;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using KoiX.UI;

namespace KoiX
{
    /// <summary>
    /// Helper glass for rendering UI using SpriteBatch.
    /// </summary>
    public static class NineGrid
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="batch"></param>
        /// <param name="texture"></param>
        /// <param name="position">Upper left, adjusted for size of corner radius.</param>
        /// <param name="size">Size of solid part, not counting shadow.</param>
        /// <param name="cornerRadius"></param>
        /// <param name="color"></param>
        public static void Render(SpriteBatch batch, Texture2D texture, RectangleF rect, float cornerRadius, Color color)
        {
            int interiorWidth = (int)Math.Floor(rect.Size.X - cornerRadius);
            int interiorHeight = (int)Math.Floor(rect.Size.Y - cornerRadius);
            Point pos = new Point((int)Math.Floor(rect.Position.X - cornerRadius / 2.0f), (int)Math.Floor(rect.Position.Y - cornerRadius / 2.0f));
            Render(batch, texture, (int)Math.Floor(cornerRadius), interiorWidth, interiorHeight, pos, color);
        }

        public static void Render(SpriteCamera camera, Texture2D texture, RectangleF rect, float cornerRadius, Color color)
        {
            int interiorWidth = (int)Math.Floor(rect.Size.X - cornerRadius);
            int interiorHeight = (int)Math.Floor(rect.Size.Y - cornerRadius);
            Point pos = new Point((int)Math.Floor(rect.Position.X - cornerRadius / 2.0f), (int)Math.Floor(rect.Position.Y - cornerRadius / 2.0f));
            SpriteBatch batch = KoiLibrary.SpriteBatch;

            batch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, samplerState: null, depthStencilState: null, rasterizerState: null, effect: null, transformMatrix: camera.ViewMatrix);
            {
                Render(batch, texture, (int)(cornerRadius), interiorWidth, interiorHeight, pos, color);
            }
            batch.End();
        }

        /// <summary>
        /// 
        /// Note, overall width = 2 * cornerSize + interiorWidth
        ///       overall height = 2 * cornerSize + interiorHeight  
        /// </summary>
        /// <param name="batch"></param>
        /// <param name="texture">Corner texture.  Should be square.</param>
        /// <param name="cornerSize">Size of corner blocks.  The corner radius will be half this since the textures only go half way across the texture to allow for the shadows.</param>
        /// <param name="interiorWidth">Width of 'box' inside of corners.</param>
        /// <param name="interiorHeight">Height of 'box' inside of corners.</param>
        /// <param name="position">Upper, left hand corner.</param>
        /// <param name="color"></param>
        public static void Render(SpriteBatch batch, Texture2D texture, int cornerSize, int interiorWidth, int interiorHeight, Point position, Color color)
        {
            Rectangle dstRect;
            Rectangle srcRect;

            // Note assumes texture is square.
            int half = texture.Width / 2;

            srcRect = new Rectangle(0, 0, half, half);
            dstRect = new Rectangle(position.X, position.Y, cornerSize, cornerSize);
            batch.Draw(texture, dstRect, srcRect, color);

            srcRect = new Rectangle(half, 0, 0, half);
            dstRect = new Rectangle(position.X + cornerSize, position.Y, interiorWidth, cornerSize);
            batch.Draw(texture, dstRect, srcRect, color);

            srcRect = new Rectangle(half, 0, half, half);
            dstRect = new Rectangle(position.X + cornerSize + interiorWidth, position.Y, cornerSize, cornerSize);
            batch.Draw(texture, dstRect, srcRect, color);


            srcRect = new Rectangle(0, half, half, 0);
            dstRect = new Rectangle(position.X, position.Y + cornerSize, cornerSize, interiorHeight);
            batch.Draw(texture, dstRect, srcRect, color);

            srcRect = new Rectangle(half, half, 0, 0);
            dstRect = new Rectangle(position.X + cornerSize, position.Y + cornerSize, interiorWidth, interiorHeight);
            batch.Draw(texture, dstRect, srcRect, color);

            srcRect = new Rectangle(half, half, half, 0);
            dstRect = new Rectangle(position.X + cornerSize + interiorWidth, position.Y + cornerSize, cornerSize, interiorHeight);
            batch.Draw(texture, dstRect, srcRect, color);


            srcRect = new Rectangle(0, half, half, half);
            dstRect = new Rectangle(position.X, position.Y + cornerSize + interiorHeight, cornerSize, cornerSize);
            batch.Draw(texture, dstRect, srcRect, color);

            srcRect = new Rectangle(half, half, 0, half);
            dstRect = new Rectangle(position.X + cornerSize, position.Y + cornerSize + interiorHeight, interiorWidth, cornerSize);
            batch.Draw(texture, dstRect, srcRect, color);

            srcRect = new Rectangle(half, half, half, half);
            dstRect = new Rectangle(position.X + cornerSize + interiorWidth, position.Y + cornerSize + interiorHeight, cornerSize, cornerSize);
            batch.Draw(texture, dstRect, srcRect, color);

        }   // end of Render()

        public static void Render(Texture2D texture, int cornerSize, int interiorWidth, int interiorHeight, Point position)
        {
            SpriteBatch batch = KoiLibrary.SpriteBatch;

            batch.Begin(SpriteSortMode.Deferred, BlendState.NonPremultiplied);

            Render(texture, cornerSize, interiorWidth, interiorHeight, position);

            batch.End();
        }   // end of Render()

    }   // end of class NineGrid

}   // end of namespace KoiX
