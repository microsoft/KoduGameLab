
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
using Boku.Common;
using Boku.SimWorld.Terra;

namespace Boku.Fx
{
    /// <summary>
    /// This blits a distortion effect full screen
    /// </summary>
    public class DistortFilter : BaseFilter
    {
        protected string effectName = null;

        protected static Texture2D filter = null;
        protected static Vector4 filterScroll = new Vector4(0.0f, 0.0f, 0.0f, 0.0f);
        protected static Vector4 filterScale = new Vector4(0.5f, 0.5f, 0.5f, 0.5f);
        protected static Vector4 filterRate = new Vector4(0.025f, -0.025f, 0.027f, 0.03f);
        protected static float filterBlur = 1.0f;
        protected static Vector3 filterColor = new Vector3(0.2f, 0.5f, 0.6f);


        // c'tor
        public DistortFilter(string effName)
            :
            base()
        {
            effectName = effName;
        }   // end of DistortFilter c'tor

        #region Accessors
        public Effect Effect
        {
            get { return effect; }
        }

        public static Texture2D BumpTexture
        {
            get { return filter; }
        }
        #endregion

        public void Update()
        {
            filterScroll += Time.GameTimeFrameSeconds * filterRate;
            filterScroll = MyMath.Wrap(filterScroll);
        }

        public void Render(Camera camera)
        {
            Vector3 actualFrom = camera.ActualFrom;
            float baseHeight = Terrain.GetWaterBase(actualFrom);
            float waveMax = baseHeight + Terrain.WaveHeight;
            float dh = camera.NearClip / camera.ProjectionMatrix.M22;
            bool underWater = (baseHeight > 0) && (waveMax > actualFrom.Z - dh);

            float filterStrength = Terrain.WaterStrength;

            if (underWater && (filterStrength > 0.0f))
            {
                effect.Parameters["Bump"].SetValue(filter);
                effect.Parameters["BumpScroll"].SetValue(filterScroll);
                effect.Parameters["BumpScale"].SetValue(filterScale);
                effect.Parameters["BumpStrength"].SetValue(filterStrength);
                effect.Parameters["WaterColor"].SetValue(filterColor);

                float waveCycle = (float)Time.GameTimeTotalSeconds;
                effect.Parameters["WaveCycle"].SetValue(waveCycle);

                effect.Parameters["WaveCenter"].SetValue(new Vector2(127.0f, 600.0f));

                const double WaveLength = 15.0;
                effect.Parameters["InverseWaveLength"].SetValue((float)(2.0 * Math.PI / WaveLength));

                effect.Parameters["WaveHeight"].SetValue(Terrain.WaveHeight);

                effect.Parameters["BaseHeight"].SetValue(baseHeight);
                effect.Parameters["CubeSize"].SetValue(
                    new Vector3(
                        Terrain.Current.CubeSize, 
                        1.0f / Terrain.Current.CubeSize,
                        Terrain.Current.CubeSize * 0.5f)
                );

                float dw = camera.NearClip / camera.ProjectionMatrix.M11;
                Vector3 nearPlaneToCamera = new Vector3(dw, dh, -camera.NearClip);
                effect.Parameters["NearPlaneToCamera"].SetValue(nearPlaneToCamera);

                effect.Parameters["CameraToWorld"].SetValue(Matrix.Invert(camera.ViewMatrix));

                Render("Distort");
            }
        }
        protected void Render(string technique)
        {
            GraphicsDevice device = BokuGame.bokuGame.GraphicsDevice;

            SetUvToPos();


            effect.CurrentTechnique = effect.Techniques[technique];

            device.SetVertexBuffer(vbuf);

            // Render all passes.
            for (int i = 0; i < effect.CurrentTechnique.Passes.Count; i++)
            {
                EffectPass pass = effect.CurrentTechnique.Passes[i];
                pass.Apply();
                device.DrawPrimitives(PrimitiveType.TriangleStrip, 0, 2);
            }

        }   // end of DistortFilter Render()


        public override void LoadContent(bool immediate)
        {
            // Init the effect.
            if (effect == null)
            {
                effect = BokuGame.Load<Effect>(BokuGame.Settings.MediaPath + @"Shaders\" + effectName);
            }
            if (filter == null)
            {
                filter = BokuGame.Load<Texture2D>(BokuGame.Settings.MediaPath + @"Textures\DistortionField");
            }

            base.LoadContent(immediate);
        }   // end of DistortFilter LoadContent()

        public override void UnloadContent()
        {
            BokuGame.Release(ref filter);

            base.UnloadContent();
        }   // end of DistortFilter UnloadContent()

    }   // end of class DistortFilter

}   // end of Boku.Common



