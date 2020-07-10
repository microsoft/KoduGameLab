// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.


/// Relocated from Common namespace

using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;

using Boku.Base;
using Boku.Common;
using Boku.Programming;
using Boku.Common.ParticleSystem;
using Boku.SimWorld.Terra;
using Boku.Audio;

namespace Boku.Fx
{
    class DistortionManager
    {
        protected static List<Distortion> lies = new List<Distortion>();

        protected static Vector4 bumpScroll = new Vector4(0.0f, 0.0f, 0.0f, 0.0f);
        protected static Vector4 bumpScale = new Vector4(0.5f, 0.5f, 0.5f, 0.5f);
        protected static Vector4 bumpRate = new Vector4(0.25f, 0.25f, -0.27f, 0.3f);
        protected static float bumpStrength = 0.1f;
        protected static float blurStrength = 1.0f;

        protected static Texture2D bump = null;
        protected static Effect partyEffect = null;

        protected static bool party = false;

        enum EffectParams
        {
            DepthTexture,
            Bump,
            BlurStrength,
            BumpScroll,
            BumpScale,
            BumpStrength,
        };
        static EffectCache effectCache = new EffectCache<EffectParams>();
        private static EffectParameter Parameter(EffectParams param)
        {
            return effectCache.Parameter((int)param);
        }

        protected static DistortFilter distFilter = null;

        protected static bool enabled = true;
        protected static bool partyEnabled = false;

        #region Accessors
        public static bool EnabledSM3
        {
            get { return enabled && BokuSettings.Settings.PostEffects; }
            set { enabled = value; }
        }
        public static bool EnabledSM2
        {
            get { return !EnabledSM3; }
        }
        public static bool PartyEnabled
        {
            get { return EnabledSM3 && partyEnabled; }
            set { partyEnabled = value; }
        }
        public static bool ParticlesActive
        {
            get { return party; }
            set { party = value; }
        }
        public static Texture2D Bump
        {
            get { return bump; }
        }
        public static Effect PartyEffect
        {
            get { return partyEffect; }
        }
        #endregion Accessors

        /// <summary>
        /// Add a distortion field to the scene, shape based on the GameThing passed in.
        /// </summary>
        /// <param name="moldOwner"></param>
        /// <param name="life"></param>
        /// <param name="maxSize"></param>
        /// <returns></returns>
        public static Distortion Add(GameThing thing, float life, Vector3 maxSize)
        {
            if (EnabledSM2 || EnabledSM3)
            {
                Distortion lie = Distortion.Acquire(thing, life, maxSize);

                lies.Add(lie);

                return lie;
            }
            return null;
        }
        /// <summary>
        /// Add distortion field with bumpy skin
        /// </summary>
        /// <param name="moldOwner"></param>
        /// <param name="life"></param>
        /// <param name="maxSize"></param>
        /// <returns></returns>
        public static Distortion AddWithBump(GameThing thing, bool doSound)
        {
            float life = 0.4f;
            Vector3 maxSize = new Vector3(1.2f, 1.2f, 1.2f);
            Distortion lie = Add(thing, life, maxSize);

            lie.EnableBump(true);

            if (doSound)
            {
                Foley.PlayPaste();
            }

            return lie;
        }
        /// <summary>
        /// Add distortion field with bumpy skin and electic glow
        /// </summary>
        /// <param name="moldOwner"></param>
        /// <param name="life"></param>
        /// <param name="maxSize"></param>
        /// <returns></returns>
        public static Distortion AddWithGlow(GameThing thing, bool doSound)
        {
            float life = (float)Brain.kStunSeconds;
            Vector3 maxSize = new Vector3(1.2f, 1.2f, 1.2f);

            Distortion lie = Add(thing, life, maxSize);
            
            lie.EnableBump(true);

            lie.EnableGlow(true);

            if (doSound)
            {
                Foley.PlayRepeatingLaser(thing);
            }

            return lie;
        }

        /// <summary>
        /// Update distortion fields for next render
        /// </summary>
        /// <param name="camera"></param>
        public static void Update()
        {
            if (EnabledSM2 || EnabledSM3)
            {
                if (EnabledSM3)
                {
                    if (distFilter == null)
                    {
                        distFilter = new DistortFilter("DistortFilter");
                        BokuGame.Load(distFilter);
                    }
                    distFilter.Update();

                    bumpScroll += Time.GameTimeFrameSeconds * bumpRate;
                    bumpScroll = MyMath.Wrap(bumpScroll);
                }

                for (int index = lies.Count - 1; index >= 0; --index)
                {
                    Distortion lie = lies[index];
                    if (!lie.Bloom && !EnabledSM3)
                    {
                        lies.RemoveAt(index);
                        lie.Release();
                    }
                    else
                    if (!lie.Update())
                    {
                        lies.RemoveAt(index);
                        lie.Release();
                    }
                }
            }
        }

        /// <summary>
        /// Set renderEffects state and render SM3 versions of distortions. 
        /// </summary>
        /// <param name="camera"></param>
        /// <param name="effectsImage"></param>
        public static void RenderSM3(Camera camera, Texture2D effectsImage)
        {
            if (!EnabledSM3)
            {
                return;
            }
            if (InGame.inGame.renderEffects != InGame.RenderEffect.Normal)
            {
                return;
            }
            InGame.inGame.renderEffects = InGame.RenderEffect.DistortionPass;

            Parameter(EffectParams.DepthTexture).SetValue(effectsImage);
            Parameter(EffectParams.Bump).SetValue(Bump);
            Parameter(EffectParams.BlurStrength).SetValue(blurStrength);
            Parameter(EffectParams.BumpScroll).SetValue(bumpScroll);
            Parameter(EffectParams.BumpScale).SetValue(bumpScale);
            Parameter(EffectParams.BumpStrength).SetValue(bumpStrength);

            InGame.inGame.ParticleSystemManager.Render(camera, BaseEmitter.Use.Distort);

            // We've set the BlendFunc to Max in the effect, manually set it back
            // to add here, because that's what everyone else assumes it to be.
            BokuGame.bokuGame.GraphicsDevice.BlendState = BlendState.NonPremultiplied;

            for (int i = 0; i < lies.Count; ++i)
            {
                Distortion lie = lies[i];

                if (!lie.Bloom)
                {
                    lie.RenderSM3(camera, effectsImage);
                }
            }

            distFilter.Render(camera);

            FirstPersonEffectMgr.Render(camera);

            InGame.inGame.renderEffects = InGame.RenderEffect.Normal;
        }

        /// <summary>
        /// Render the SM2 version of glow.
        /// </summary>
        /// <param name="camera"></param>
        public static void RenderBloomSM2(Camera camera)
        {
            if (!EnabledSM2)
            {
                return;
            }
            if (InGame.inGame.renderEffects != InGame.RenderEffect.Normal)
            {
                return;
            }

            InGame.inGame.renderEffects = InGame.RenderEffect.Aura;
            for (int i = 0; i < lies.Count; ++i)
            {
                Distortion lie = lies[i];

                if (lie.Bloom)
                {
                    lie.RenderBloomSM2(camera);
                }
            }
            InGame.inGame.renderEffects = InGame.RenderEffect.Normal;
        }

        /// <summary>
        /// Render the SM3 version of glow.
        /// </summary>
        /// <param name="camera"></param>
        /// <param name="effectsImage"></param>
        public static void RenderBloomSM3(Camera camera, Texture2D effectsImage)
        {
            if (!EnabledSM3)
                return;

            if (InGame.inGame.renderEffects != InGame.RenderEffect.Normal)
            {
                return;
            }
            InGame.inGame.renderEffects = InGame.RenderEffect.BloomPass;
            
            
            // Need to jumpt through some hoops here to get the right thing to happen
            // when we're in tutorial mode.  Basically we're setting up a custom viewport
            // that has the same aspect ratio and offset that the main one has but is
            // proportionally smaller to fit the current rendertarget.
            if (BokuGame.ScreenPosition.Y > 0)
            {
                GraphicsDevice device = BokuGame.bokuGame.GraphicsDevice;
                Viewport vp = device.Viewport;

                vp.Y = (int)(vp.Height * BokuGame.ScreenPosition.Y / (BokuGame.ScreenPosition.Y + BokuGame.ScreenSize.Y));
                vp.Height = vp.Height - vp.Y;

                device.Viewport = vp;
            }

            for (int i = 0; i < lies.Count; ++i)
            {
                Distortion lie = lies[i];

                if (lie.Bloom)
                {
                    lie.RenderSM3(camera, effectsImage);
                }
            }

            // We've set the BlendFunc to Max in the effect, set it back
            // to default.
            BokuGame.bokuGame.GraphicsDevice.BlendState = BlendState.AlphaBlend;

            FirstPersonEffectMgr.Render(camera);

            InGame.inGame.renderEffects = InGame.RenderEffect.Normal;
        }


        public static void LoadContent(bool immediate)
        {
            if (EnabledSM2 || EnabledSM3)
            {
                if (bump == null)
                {
                    bump = BokuGame.Load<Texture2D>(BokuGame.Settings.MediaPath + @"Textures\DistortionWake");
                }

                if (partyEffect == null)
                {
                    partyEffect = BokuGame.Load<Effect>(BokuGame.Settings.MediaPath + @"Shaders\Particle2D");
                    effectCache.Load(PartyEffect, "");
                }

                if (distFilter == null)
                {
                    distFilter = new DistortFilter("DistortFilter");
                }
                distFilter.LoadContent(immediate);
            }

            Distortion.LoadContent(immediate);
        }

        public static void InitDeviceResources(GraphicsDevice device)
        {
            if (EnabledSM3)
            {
                distFilter.InitDeviceResources(device);
            }

            Distortion.InitDeviceResources(device);
        }

        public static void UnloadContent()
        {
            BokuGame.Release(ref bump);
            BokuGame.Release(ref partyEffect);
            
            effectCache.UnLoad();

            if (distFilter != null)
            {
                distFilter.UnloadContent();
                distFilter = null;
            }

            Distortion.UnloadContent();
        }

        public static void DeviceReset(GraphicsDevice device)
        {
            Distortion.DeviceReset(device);
        }
    }
}
