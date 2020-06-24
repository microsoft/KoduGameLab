
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

using KoiX;

using Boku.Base;

namespace Boku.Fx
{
    /// <summary>
    /// This performs a 7 pixel wide Gaussian filter either horizontally or vertically.  Since
    /// Gaussian filters are separable doing one then the other yields the same results as doing 
    /// a 7x7 filter but runs a lot faster.
    /// </summary>
    class GaussianFilter : BaseFilter
    {
        float[] weights;
        float[] offsets;

        // c'tor
        public GaussianFilter()
            :
            base()
        {
            weights = new float[4];
            offsets = new float[4];

            weights[0] =  0.108f;
            weights[1] =  0.392f;
            weights[2] =  0.392f;
            weights[3] =  0.108f;

            offsets[0] = -2.168f;
            offsets[1] = -0.592f;
            offsets[2] =  0.592f;
            offsets[3] =  2.168f;

        }   // end of GaussianFilter c'tor

        public void Render(Texture2D source, bool horizontal)
        {
            GraphicsDevice device = KoiLibrary.GraphicsDevice;

            SetUvToPos();

            // This is the width and height of a pixel in texture space.
            Vector2 pixelSize = new Vector2(1.0f / device.Viewport.Width, 1.0f / device.Viewport.Height);

            effect.Parameters["SourceTexture"].SetValue(source);
            effect.Parameters["PixelSize"].SetValue(pixelSize);
            effect.Parameters["Weights"].SetValue(weights);
            effect.Parameters["Offsets"].SetValue(offsets);

            effect.CurrentTechnique = effect.Techniques[horizontal ? "GaussianHorizontal" : "GaussianVertical"];

            device.SetVertexBuffer(vbuf);

            // Render all passes.
            for (int i = 0; i < effect.CurrentTechnique.Passes.Count; i++)
            {
                EffectPass pass = effect.CurrentTechnique.Passes[i];
                pass.Apply();
                device.DrawPrimitives(PrimitiveType.TriangleStrip, 0, 2);
            }

        }   // end of GaussianFilter Render()

        public void RenderHorizontal(Texture2D source)
        {
            Render(source, true);
        }   // end of GaussianFilter RenderHorizontal()

        public void RenderVertical(Texture2D source)
        {
            Render(source, false);
        }   // end of GaussianFilter RenderVertical()


        public override void LoadContent(bool immediate)
        {
            // Init the effect.
            if (effect == null)
            {
                effect = KoiLibrary.LoadEffect(@"Shaders\GaussianFilter");
            }

            base.LoadContent(immediate);
        }   // end of GaussianFilter LoadContent()

    }

}   // end of namespace Boku.Common



