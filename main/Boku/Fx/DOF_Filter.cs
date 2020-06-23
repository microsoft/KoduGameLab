
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
using Boku.Fx;

namespace Boku.Fx
{
    public class DOF_Filter : BaseFilter
    {
        #region Accessors
        #endregion

        // c'tor
        public DOF_Filter()
            :
            base()
        {
        }   // end of DOF_Filter c'tor

        public void Render(Texture2D fullImage,
            Texture2D blurImage,
            Texture2D bloomImage,
            Texture2D glowImage,
            Texture2D effectsImage,
            Texture2D distortImage0,
            Texture2D distortImage1)
        {
            GraphicsDevice device = BokuGame.bokuGame.GraphicsDevice;

            SetUvToPos(new Vector2(fullImage.Width, fullImage.Height));

            // This is the width and height of a pixel in texture space.
            Vector2 pixelSize = new Vector2(
                1.0f / device.Viewport.Width,
                1.0f / device.Viewport.Height);
            effect.Parameters["PixelSize"].SetValue(pixelSize);

            const float pixelOffset = 0.5f;
            //const float pixelOffset = 0.0f;            
            Vector4 fullOffset = new Vector4(
                pixelOffset / device.Viewport.Width,
                pixelOffset / device.Viewport.Height,
                -pixelOffset / device.Viewport.Width,
                -pixelOffset / device.Viewport.Height);
            effect.Parameters["FullOffset"].SetValue(fullOffset);

            effect.Parameters["FullTexture"].SetValue(fullImage);
            effect.Parameters["BlurTexture"].SetValue(blurImage);
            effect.Parameters["BloomTexture"].SetValue(bloomImage);
            effect.Parameters["GlowTexture"].SetValue(glowImage);
            effect.Parameters["DepthTexture"].SetValue(effectsImage);
            effect.Parameters["DistortTexture0"].SetValue(distortImage0);
            effect.Parameters["DistortTexture1"].SetValue(distortImage1);

            // Experimental minimum blur amount for Matt.
            float maxBlur = effect.Parameters["DOF_MaxBlur"].GetValueSingle();
            float minBlur = 0.2f;
            //float minBlur = 0.0f;
            float blurScale = (maxBlur - minBlur) / (maxBlur - 0.0f);
            effect.Parameters["DOF_MinBlur"].SetValue(new Vector2(minBlur, blurScale));

            // Adjustement needed to deal with the confusion that
            // arises when 
            Vector2 screenScale = new Vector2(fullImage.Width / BokuGame.ScreenSize.X, fullImage.Height / BokuGame.ScreenSize.Y);
            effect.Parameters["ScreenScale"].SetValue(screenScale);

            Vector2 fullScreen = BokuGame.ScreenPosition + BokuGame.ScreenSize;
            Vector2 depthSampleOffset = -BokuGame.ScreenPosition / fullScreen;
            Vector2 depthSampleScale = fullScreen / BokuGame.ScreenSize;

            effect.Parameters["DepthSampleOffset"].SetValue(depthSampleOffset);
            effect.Parameters["DepthSampleScale"].SetValue(depthSampleScale);

            if (bloomImage != null)
            {
                effect.CurrentTechnique = effect.Techniques["DOF_CompositeBloomDistort_Single_Sample"];
            }
            else if (DistortionManager.EnabledSM3)
            {
                effect.CurrentTechnique = effect.Techniques["DOF_CompositeDistort_Single_Sample"];
            }
            else
            {
                effect.CurrentTechnique = effect.Techniques["DOF_Composite_Single_Sample"];
            }

            device.SetVertexBuffer(vbuf);

            // Render all passes.
            for (int i = 0; i < effect.CurrentTechnique.Passes.Count; i++)
            {
                EffectPass pass = effect.CurrentTechnique.Passes[i];
                pass.Apply();
                device.DrawPrimitives(PrimitiveType.TriangleStrip, 0, 2);
            }

        }   // end of DOF_Filter Render()

        void SetUvToPos(Vector2 backbufferSize)
        {
            GraphicsDevice device = BokuGame.bokuGame.GraphicsDevice;

            int width = device.Viewport.Width;
            int height = device.Viewport.Height;

            float pixelWidth = 1.0f / width;
            float pixelHeight = 1.0f / height;

            Vector2 scale = backbufferSize / BokuGame.ScreenSize;

            Vector4 uvToPos = new Vector4(
                2.0f * scale.X,         // x scale
                -2.0f * scale.Y,        // y scale
                -1.0f - pixelWidth,     // x offset
                1.0f + pixelHeight);    // y offset

            effect.Parameters["UvToPos"].SetValue(uvToPos);

        }   // end of BaseFilter SetUvToPos()

        public override void LoadContent(bool immediate)
        {
            // Init the effect.
            if (effect == null)
            {
                effect = BokuGame.Load<Effect>(BokuGame.Settings.MediaPath + @"Shaders\DOF_Filter");
                ShaderGlobals.RegisterEffect("DOF_Filter", effect);
            }

            base.LoadContent(immediate);
        }   // end of DOF_Filter LoadContent()

    }   // end of class DOF_Filter

}   // end of Boku.Common



