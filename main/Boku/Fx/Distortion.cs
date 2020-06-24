
/// Relocated from SimWorld namespace

using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Storage;

using KoiX;

using Boku.Base;
using Boku.Common;
using Boku.SimWorld;

namespace Boku.Fx
{
    public class Distortion
    {
        #region Members
        /// <summary>
        /// Lifespan controls. If it doesn't expire, it repeats.
        /// </summary>
        protected bool expires = true;
        protected double birth = 0.0f;
        protected float lifeSpan = 1.0f;

        /// <summary>
        /// Is this an explicit bloom render or distortion field
        /// </summary>
        protected bool bloom = false;

        /// <summary>
        /// Target maximum size to reach (as scaling of underlying model)
        /// </summary>
        protected Vector3 maxScale = new Vector3(1.0f);
        protected Vector3 minScale = new Vector3(1.0f);

        /// <summary>
        /// The things to render.
        /// </summary>
        protected GameThing owner = null;

        /// <summary>
        /// Controls for ramping up and down the effect over lifespan
        /// </summary>
        protected float rampUp = 0.4f;
        protected float rampDown = 0.5f;
        /// <summary>
        ///     scales:
        ///         .x => distortion strength
        ///         .y => UNUSED
        ///         .z => tint scale
        ///         .w => blur scale
        /// </summary>
        protected Vector4 scales = new Vector4(1.0f, 0.0f, 1.0f, 0.5f);

        protected int numScaleBeats = 2;

        /// <summary>
        /// Current scaling of underlying model (age dependent)
        /// </summary>
        protected Vector3 scale = new Vector3(1.0f);
        /// <summary>
        /// Current overall strength (age dependent)
        /// </summary>
        protected float opacity = 0.0f;

        /// <summary>
        /// Additional parameter set for controlling the scrolling bump effect
        /// </summary>
        protected bool doBump = false;
        protected float bumpRadius = 1.0f;
        protected float bumpStrength = 1.0f;
        protected Vector4 bumpScale = new Vector4(3.0f, 3.0f, 3.0f, 3.0f);
        protected Vector4 bumpScroll = new Vector4(0.0f, 0.0f, 0.0f, 0.0f);
        protected Vector4 bumpRate = new Vector4(0.0f, 0.0f, 0.0f, 0.0f);
        protected Vector4 bumpTint = new Vector4(0.0f, 0.0f, 0.0f, 1.0f);

        protected static Texture2D bump = null;
        protected static Effect effect = null;

        private static List<Distortion> available = new List<Distortion>();

        #region EffectCache
        private enum EffectParams
        {
            DepthTexture,
            PreWorld,
            Opacity,
            BloomColor,
            BumpTexture,
            BumpScroll,
            BumpScale,
            BumpStrength,
            BumpTransU,
            BumpTransV,
            BumpTint,
        };
        static EffectCache effectCache = new EffectCache<EffectParams>();
        private EffectParameter Parameter(EffectParams param)
        {
            return effectCache.Parameter((int)param);
        }
        #endregion EffectCache

        #endregion Members

        #region Accessors
        public GameThing Owner
        {
            get { return owner; }
            private set { owner = value; }
        }
        public RenderObject RenderObj
        {
            get { return owner.RenderObject; }
        }
        public bool Bloom
        {
            get { return bloom; }
            set { bloom = value; }
        }
        #endregion Accessors

        #region Public
        /// <summary>
        /// Recycle or allocate a Distortion.
        /// </summary>
        /// <param name="owner"></param>
        /// <param name="inLife"></param>
        /// <param name="inMaxScale"></param>
        /// <returns></returns>
        public static Distortion Acquire(GameThing owner, float inLife, Vector3 inMaxScale)
        {
            Distortion dist = null;
            if (available.Count == 0)
            {
                dist = new Distortion(owner, inLife, inMaxScale);
            }
            else
            {
                dist = available[available.Count - 1];
                available.RemoveAt(available.Count - 1);
                dist.Setup(owner, inLife, inMaxScale);
            }
            return dist;
        }
        /// <summary>
        /// Recycle this back into the available bin.
        /// </summary>
        /// <returns></returns>
        public Distortion Release()
        {
            available.Add(this);
            return null;
        }
        /// <summary>
        /// Tell this distortion to kill itself first chance it gets.
        /// </summary>
        public void Die()
        {
            expires = true;
            lifeSpan = -1.0f;
        }

        /// <summary>
        /// Enable a rough skin on the distortion
        /// </summary>
        /// <param name="on"></param>
        public void EnableBump(bool on)
        {
            doBump = on;
            bumpScale = new Vector4(1.5f, 1.5f, -1.5f, 1.5f);
        }

        /// <summary>
        /// Enable electric glow
        /// </summary>
        /// <param name="on"></param>
        public void EnableGlow(bool on)
        {
            if (on)
            {
                bumpTint = new Vector4(0.0f, 0.2f, 0.4f, 1.0f);
            }
            else
            {
                bumpTint = new Vector4(0.0f, 0.0f, 0.0f, 1.0f);
            }
            bumpScale = new Vector4(1.0f, 1.0f, -1.0f, 1.0f);
        }

        /// <summary>
        /// Make this into a glowing aura distortion.
        /// </summary>
        public void MakeAura()
        {
            TintAura(0.0f, 1.0f, 0.0f);
            bumpStrength = 0.0f;
            scales = new Vector4(0.0f, 0.0f, 1.0f, 1.0f);
            expires = false;
            minScale = maxScale * 0.9f;
            lifeSpan = 2.0f;
            rampUp = 0.0f;
            rampDown = 1.0f;
            bloom = true;
        }
        /// <summary>
        /// Tint the aura we project, values [0..1].
        /// </summary>
        /// <param name="r"></param>
        /// <param name="g"></param>
        /// <param name="b"></param>
        public void TintAura(float r, float g, float b)
        {
            bumpTint = new Vector4(r, g, b, 1.0f);
        }

        /// <summary>
        /// Update this distortion body
        /// </summary>
        /// <param name="camera"></param>
        /// <returns></returns>
        public bool Update()
        {
            double curTime = Time.GameTimeTotalSeconds;

            if (birth <= 0.0f)
            {
                birth = Time.GameTimeTotalSeconds;
            }

            float age = (float)(curTime - birth);
            if (expires && (age >= lifeSpan))
            {
                return false;
            }

            age /= lifeSpan;
            if (age > 1.0f)
            {
                age -= (int)age;
            }

            if (age <= rampUp)
            {
                opacity = MyMath.SmoothStep(0.0f, rampUp, age);
            }
            else if (age >= rampDown)
            {
                // MyMath.SmoothStep doesn't handle a > b.
                float t = (age - 1.0f) / (rampDown - 1.0f);
                opacity = t * t; //  MyMath.SmoothStep(0.0f, 1.0f, t);
            }
            else
            {
                opacity = 1.0f;
            }
            //Debug.Print("opacity - {0}", opacity);
            //opacity = 1.0f - age;
            //opacity *= opacity;

            float tScale = 0.0f;
            for (int iscale = 0; iscale < numScaleBeats; ++iscale)
            {
                double beatScale = 1.0f - Math.Cos(age * MathHelper.Pi * (2 << iscale));
                tScale += (float)beatScale;
            }
            scale = minScale + (maxScale - minScale) * (tScale / numScaleBeats);

            return true;
        }

        /// <summary>
        /// Render this object's SM2 glow to the screen.
        /// </summary>
        /// <param name="camera"></param>
        public void RenderBloomSM2(Camera camera)
        {
            if (effect != null)
            {
                Matrix preWorld = Matrix.CreateScale(scale);

                Parameter(EffectParams.PreWorld).SetValue(preWorld);
                Parameter(EffectParams.Opacity).SetValue(scales * opacity);

                Parameter(EffectParams.BloomColor).SetValue(bumpTint);

                Debug.Assert(Bloom, "No other reason for rendering in SM2");

                RenderObj.Render(camera);

                Parameter(EffectParams.PreWorld).SetValue(Matrix.Identity);
            }
        }

        /// <summary>
        /// Render this distortion pulse to the current offscreen
        /// Assumes the proper InGame.inGame.renderEffects is setup
        /// </summary>
        /// <param name="camera"></param>
        /// <param name="effectsImage"></param>
        public void RenderSM3(Camera camera, Texture2D effectsImage)
        {
            if (effect != null)
            {
                Matrix preWorld = Matrix.CreateScale(scale);

                Parameter(EffectParams.DepthTexture).SetValue(effectsImage);
                Parameter(EffectParams.PreWorld).SetValue(preWorld);
                Parameter(EffectParams.Opacity).SetValue(scales * opacity);

                if (bump != null)
                {
                    SetupBump(camera);
                }

                if (Bloom)
                {
                    Parameter(EffectParams.BloomColor).SetValue(bumpTint);
                }

                RenderObj.Render(camera);

                Parameter(EffectParams.PreWorld).SetValue(Matrix.Identity);
            }
        }
        #endregion Public

        #region Internal
        /// <summary>
        /// Load up resource needed for rendering, all shared statics.
        /// </summary>
        /// <param name="immediate"></param>
        public static void LoadContent(bool immediate)
        {
            if (bump == null)
            {
                bump = KoiLibrary.LoadTexture2D(@"Textures\DistortionWake");
            }
            if (effect == null)
            {
                effect = KoiLibrary.LoadEffect(@"Shaders\Standard");
                ShaderGlobals.RegisterEffect("Standard", effect);
                effectCache.Load(effect, "");
            }
        }

        /// <summary>
        /// Anything device specific (nothing).
        /// </summary>
        /// <param name="graphics"></param>
        public static void InitDeviceResources(GraphicsDevice device)
        {
        }

        /// <summary>
        /// Dump all resources.
        /// </summary>
        public static void UnloadContent()
        {
            DeviceResetX.Release(ref bump);
            DeviceResetX.Release(ref effect);
            effectCache.UnLoad();
        }

        /// <summary>
        /// Recreate render targets
        /// </summary>
        public static void DeviceReset(GraphicsDevice device)
        {
        }

        /// <summary>
        /// Constructor, only accessed through Acquire
        /// </summary>
        /// <param name="owner"></param>
        /// <param name="inLife"></param>
        /// <param name="inMaxScale"></param>
        private Distortion(GameThing owner, float inLife, Vector3 inMaxScale)
        {
            Setup(owner, inLife, inMaxScale);
        }

        /// <summary>
        /// Set up a distortion like new.
        /// </summary>
        /// <param name="owner"></param>
        /// <param name="inLife"></param>
        /// <param name="inMaxScale"></param>
        private void Setup(GameThing owner, float inLife, Vector3 inMaxScale)
        {
            this.owner = owner;
            lifeSpan = inLife;
            maxScale = inMaxScale;

            expires = true;

            rampUp = 0.2f;
            rampDown = 0.8f;

            bumpRadius = 1.0f;
            bumpStrength = 1.0f;
            bumpScale = new Vector4(3.0f, 3.0f, -3.0f, 3.0f);
            //bumpScale = new Vector4(1.0f, 1.0f, -1.0f, 1.0f);
            bumpScroll = new Vector4(0.0f, 0.0f, 0.0f, 0.0f);
            bumpRate = new Vector4(0.017f, 0.11f, 0.019f, -0.09f);
            //bumpRate = new Vector4(0.00f, 0.0f, 0.0f, -0.0f);


            birth = 0.0f;
            bloom = false;
            minScale = new Vector3(1.0f);
            scales = new Vector4(1.0f, 0.0f, 1.0f, 0.5f);
            numScaleBeats = 2;
            scale = new Vector3(1.0f);
            opacity = 0.0f;
            doBump = false;
            bumpTint = new Vector4(0.0f, 0.0f, 0.0f, 1.0f);
        }


        /// <summary>
        /// project a bumpy skin onto the target.
        /// </summary>
        /// <param name="camera"></param>
        protected void SetupBump(Camera camera)
        {
            Vector3 currPos = Owner.Movement.Position;

            Vector3 posToCam = camera.ActualFrom - currPos;

            Vector3 axU = new Vector3(-posToCam.Y, posToCam.X, 0.0f);
            float len = axU.Length();
            if (len > 0.0f)
            {
                axU /= len;
            }
            else
            {
                axU = Vector3.Right;
            }
            Vector3 axV = Vector3.Normalize(Vector3.Cross(posToCam, axU));
            Vector3 axVCrossZ = new Vector3(axV.Y, -axV.X, 0.0f);

            // Find theta, angle between axU and x-axis
            // The cross product and dot products with (1,0,0) could obviously be optimized,
            // but it's clearer what's happening leaving it in general form.
            float theta = (float)Math.Atan2(
                Vector3.Dot(Vector3.Cross(axU, Vector3.UnitX), Vector3.UnitZ), 
                Vector3.Dot(axU, Vector3.UnitX)
                );

            float phi = (float)Math.Atan2(
                Vector3.Cross(axV, Vector3.UnitZ).Length(), 
                Vector3.Dot(axV, Vector3.UnitZ)
                );
            if (posToCam.Z > 0.0f)
            {
                phi = -phi;
            }

            // Now roll the scale into the axes. Just shorthand for:
            // u' = (dot(P - P0, axU)/radius * 0.5f + 0.5f)*tiling - theta/PI * tiling
            axU *= 0.5f / bumpRadius;
            float dU = -Vector3.Dot(axU, currPos) + 0.5f - theta / MathHelper.Pi;
            Vector4 transU = new Vector4(axU, dU);

            axV *= 0.5f / bumpRadius;
            float dV = -Vector3.Dot(axV, currPos) + 0.5f - phi / MathHelper.Pi;
            Vector4 transV = new Vector4(axV, dV);

//            Debug.Print("theta/phi {0:F} / {1:F}", theta, phi);

            bumpScroll += Time.GameTimeFrameSeconds * bumpRate;
            bumpScroll = MyMath.Wrap(bumpScroll);

            Parameter(EffectParams.BumpTexture).SetValue(bump);
            Parameter(EffectParams.BumpScroll).SetValue(bumpScroll);
            Parameter(EffectParams.BumpScale).SetValue(bumpScale);
            Parameter(EffectParams.BumpStrength).SetValue(bumpStrength);
            Parameter(EffectParams.BumpTransU).SetValue(transU);
            Parameter(EffectParams.BumpTransV).SetValue(transV);
            Parameter(EffectParams.BumpTint).SetValue(bumpTint);
        }
        #endregion Internal
    }
}
