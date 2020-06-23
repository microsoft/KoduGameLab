/*
 * PaletteEffectReader.cs
 * Copyright (c) 2006 David Astle
 * 
 * Permission is hereby granted, free of charge, to any person obtaining a
 * copy of this software and associated documentation files (the
 * "Software"), to deal in the Software without restriction, including
 * without limitation the rights to use, copy, modify, merge, publish,
 * distribute, sublicense, and/or sell copies of the Software, and to
 * permit persons to whom the Software is furnished to do so, subject to
 * the following conditions:
 *
 * The above copyright notice and this permission notice shall be included
 * in all copies or substantial portions of the Software.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS
 * OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
 * MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.
 * IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY
 * CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT,
 * TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE
 * SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
 */

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace Xclna.Xna.Animation.Content
{
    /// <summary>
    /// Reads a BasicPaletteEffect from the content pipeline.
    /// </summary>
    public class PaletteEffectReader : ContentTypeReader<BasicPaletteEffect>
    {
        /// <summary>
        /// Reads a BasicPaletteEffect.
        /// </summary>
        /// <param name="input">The input stream.</param>
        /// <param name="existingInstance">N/A.</param>
        /// <returns>A new instance of BasicPaletteEffec.t</returns>
        protected override BasicPaletteEffect Read(ContentReader input, BasicPaletteEffect existingInstance)
        {
            // Read in the parameters, including the byte code, and create the effect.
            ContentManager manager = input.ContentManager;
            IGraphicsDeviceService graphics =
                (IGraphicsDeviceService)manager.ServiceProvider.GetService(typeof(IGraphicsDeviceService));
            byte[] effectCode = input.ReadRawObject<byte[]>();
            int paletteSize = input.ReadInt32();
            BasicPaletteEffect effect = new BasicPaletteEffect(graphics.GraphicsDevice,
                effectCode,paletteSize);
            if (input.ReadBoolean())
            {
                effect.Texture = input.ReadExternalReference<Texture2D>();
                effect.TextureEnabled = true;
            }
            if (input.ReadBoolean())
                effect.SpecularPower = input.ReadSingle();
            else
                effect.SpecularPower = 8.0f;
            if (input.ReadBoolean())
                effect.SpecularColor = input.ReadVector3();
            else
                effect.SpecularColor = Color.Black.ToVector3();
            if (input.ReadBoolean())
                effect.EmissiveColor = input.ReadVector3();
            else
                effect.EmissiveColor = Color.Black.ToVector3();
            if (input.ReadBoolean())
                effect.DiffuseColor = input.ReadVector3();
            else
                effect.DiffuseColor = Color.Black.ToVector3();
            if (input.ReadBoolean())
                effect.Alpha = input.ReadSingle();
            else
                effect.Alpha = 1.0f;

            return effect;

        }
    }
}
