
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
    /// This downsamples an image using a 4x4 box filter.  The source texture's dimensions
    /// should be 4 times larger than the current render target's ie a 1024x1024 source
    /// will get downsampled to a 256x256 rendertarget.
    /// </summary>
    public class Box4x4BlurFilter : BaseFilter
    {
        // c'tor
        public Box4x4BlurFilter()
            :
            base()
        {
        }   // end of Box4x4BlurFilter c'tor

        public void Render(Texture2D source, float attenuation)
        {
            GraphicsDevice device = BokuGame.bokuGame.GraphicsDevice;

            SetUvToPos();
            CheckUvToSource(source, device);

            // This is the width and height of a pixel in texture space.
            Vector2 pixelSize = new Vector2(1.0f / device.Viewport.Width, 1.0f / device.Viewport.Height);

            effect.Parameters["SourceTexture"].SetValue(source);
            effect.Parameters["PixelSize"].SetValue(pixelSize);

            effect.CurrentTechnique = effect.Techniques["Box4x4Blur"];

            device.SetVertexBuffer(vbuf);

            // Render all passes.
            for (int i = 0; i < effect.CurrentTechnique.Passes.Count; i++)
            {
                EffectPass pass = effect.CurrentTechnique.Passes[i];
                pass.Apply();
                device.DrawPrimitives(PrimitiveType.TriangleStrip, 0, 2);
            }

            // Release the vertex buffer from the device so it may be updated, if needed.
            device.SetVertexBuffer(null);

        }   // end of Box4x4BlurFilter Render()

        public void Render(Texture2D source)
        {
            Render(source, 1.0f);
        }   // end of Box4x4BlurFilter Render()


        public override void LoadContent(bool immediate)
        {
            // Init the effect.
            if (effect == null)
            {
                effect = BokuGame.Load<Effect>(BokuGame.Settings.MediaPath + @"Shaders\Box4x4BlurFilter");
            }

            base.LoadContent(immediate);
        }   // end of Box4x4BlurFilter LoadContent()

        protected void CheckUvToSource(Texture2D source, GraphicsDevice device)
        {
            Vector4 uvToSource = new Vector4(1.0f, 1.0f, 0.0f, 0.0f);
            if (source.Width * device.Viewport.Height != source.Height * device.Viewport.Width)
            {
                float srcWidth = ((float)source.Height) / device.Viewport.Height * device.Viewport.Width;
                float left = (source.Width - srcWidth) * 0.5f;
                float right = (left + srcWidth);

                /// Remap 0=>left, and 1.0 => right
                /// So u' = left + u * (right - left)
                uvToSource.X = (right - left) / source.Width;
                uvToSource.Y = 1.0f;
                uvToSource.Z = left / source.Width;
                uvToSource.W = 0.0f;
            }
            effect.Parameters["UvToSource"].SetValue(uvToSource);

        }

    }   // end of class Box4x4BlurFilter

}   // end of Boku.Common



