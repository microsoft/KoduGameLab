
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
    /// This just does a straight pixel copy.  Not too useful in general but
    /// it comes in handy for debugging and as a template for creating new filters.
    /// </summary>
    public class CopyFilter : BaseFilter
    {
        // c'tor
        public CopyFilter()
            :
            base()
        {
        }   // end of CopyFilter c'tor

        public void Render(string technique, Texture2D source, float attenuation)
        {
            GraphicsDevice device = KoiLibrary.GraphicsDevice;

            SetUvToPos();

            effect.Parameters["Attenuation"].SetValue(attenuation);
            effect.Parameters["SourceTexture"].SetValue(source);

            effect.CurrentTechnique = effect.Techniques[technique];

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

        }   // end of CopyFilter Render()

        public void Render(Texture2D source)
        {
            Render("Copy", source, 1.0f);
        }   // end of CopyFilter Render()

        public void RenderAdd(Texture2D source)
        {
            Render("Add", source, 1.0f);
        }   // end of CopyFilter Render()


        public override void LoadContent(bool immediate)
        {
            // Init the effect.
            if (effect == null)
            {
                effect = KoiLibrary.LoadEffect(@"Shaders\CopyFilter");
            }

            base.LoadContent(immediate);
        }   // end of CopyFilter LoadContent()

    }   // end of class CopyFilter

}   // end of Boku.Common



