
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
    /// This filter looks at the RGB values of the source image.  If any of them are
    /// above the threshold the pixel is passed unchanged.  If all are below the
    /// threshold then the output pixel is set to 0.
    /// </summary>
    public class ThresholdFilter : BaseFilter
    {
        // c'tor
        public ThresholdFilter()
            :
            base()
        {
        }   // end of ThresholdFilter c'tor

        public void Render(Texture2D source, float threshold)
        {
            GraphicsDevice device = BokuGame.bokuGame.GraphicsDevice;

            SetUvToPos();

            effect.Parameters["ThresholdValue"].SetValue(threshold);
            effect.Parameters["SourceTexture"].SetValue(source);

            effect.CurrentTechnique = effect.Techniques["Threshold"];

            device.SetVertexBuffer(vbuf);

            // Render all passes.
            for (int i = 0; i < effect.CurrentTechnique.Passes.Count; i++)
            {
                EffectPass pass = effect.CurrentTechnique.Passes[i];
                pass.Apply();
                device.DrawPrimitives(PrimitiveType.TriangleStrip, 0, 2);
            }

        }   // end of ThresholdFilter Render()


        public override void LoadContent(bool immediate)
        {
            // Init the effect.
            if (effect == null)
            {
                effect = BokuGame.Load<Effect>(BokuGame.Settings.MediaPath + @"Shaders\ThresholdFilter");
            }

            base.LoadContent(immediate);
        }   // end of ThresholdFilter LoadContent()

    }   // end of class ThresholdFilter

}   // end of Boku.Common



