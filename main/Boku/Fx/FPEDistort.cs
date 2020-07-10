// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.


/// Relocated from Common namespace

using System;
using System.Collections.Generic;
using System.Diagnostics;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using Boku.Base;
using Boku.Common;
using Boku.Programming;
using Boku.Audio;

namespace Boku.Fx
{
    public partial class FirstPersonEffectMgr
    {
        public class FPEDistort : FirstPersonEffect
        {
            #region Members
            private static Texture2D mask = null;
            private static Texture2D bump = null;

            private static float kLife = (float)Programming.Brain.kStunSeconds;
            private float age = kLife;
            private float intensity = 0.0f;
            private Vector4 color = new Vector4(0.0f, 0.2f, 0.4f, 1.0f);

            protected Vector4 bumpScale = new Vector4(1.0f, -1.0f, -1.0f, 1.0f);
            protected Vector4 bumpScroll = new Vector4(0.0f, 0.0f, 0.0f, 0.0f);
            protected Vector4 bumpRate = new Vector4(0.17f, 1.1f, 0.19f, -0.9f);

            private static FPEDistort instance = null;

            private enum EffectParams
            {
                PosToUV,
                BumpTint,
                BumpScroll,
                BumpScale,
                Mask,
                BumpTexture,
            };
            private static EffectCache effectCache = new EffectCache<EffectParams>();
            private static EffectParameter Parameter(EffectParams param)
            {
                return effectCache.Parameter((int)param);
            }
            #endregion

            #region Accessors
            public static Texture2D Mask
            {
                get { return mask; }
            }
            public static Texture2D Bump
            {
                get { return bump; }
            }
            #endregion

            #region Public

            public FPEDistort()
                : base()
            {
                Debug.Assert(instance == null, "There can be only one");
                instance = this;
            }

            /// <summary>
            ///  Quick implementation of smoothstep, because it's unclear what the XNA version does.
            /// This one does a Hermite interpolation from [0..1] as val=>[min..max].
            /// </summary>
            /// <param name="min"></param>
            /// <param name="max"></param>
            /// <param name="val"></param>
            /// <returns></returns>
            private float SmoothStep(float min, float max, float val)
            {
                Debug.Assert(max != min, "Degenerate input to SmoothStep");
                val = (val - min) / (max - min);
                if (val <= 0.0f)
                    return 0.0f;
                if (val >= 1.0f)
                    return 1.0f;

                return (3.0f - 2.0f * val) * val * val;
            }
            /// <summary>
            /// Add a little dynamics to the effect.
            /// </summary>
            /// <returns></returns>
            public override bool Update()
            {
                if (age < kLife)
                    age += Time.GameTimeFrameSeconds;

                if (age < kLife)
                {
                    float kRampUp = (float)(Brain.kStunSeconds * 0.1);
                    float kRampDown = (float)(Brain.kStunSeconds * 0.5);
                    if (age < kRampUp)
                    {
                        intensity = SmoothStep(0.0f, kRampUp, age);
                    }
                    else if (age > kRampDown)
                    {
                        intensity = SmoothStep(kLife, kRampDown, age);
                    }
                    else
                    {
                        intensity = 1.0f;
                    }
                    intensity *= intensity;
                    float kMaxIntensity = 1.0f;
                    intensity *= kMaxIntensity;
                    color.W = intensity;
                }
                else
                {
                    intensity = 0.0f;
                }

                return base.Update();
            }

            /// <summary>
            /// Blit it out there if we're active.
            /// </summary>
            /// <param name="camera"></param>
            public override void Render(Camera camera)
            {
                if (intensity > 0.0f)
                {
                    if (InGame.inGame.renderEffects == InGame.RenderEffect.DistortionPass)
                    {
                        GameActor firstPerson = CameraInfo.FirstPersonActor;
                        if (firstPerson != null)
                        {
                            base.Render(camera);
                        }
                    }
                }
            }

            public static void Pulse(GameThing firstPerson)
            {
                instance.age = 0.0f;
                Foley.PlayRepeatingLaser(firstPerson);
            }
            #endregion Public

            #region Internal
            #region Abstracts
            /// <summary>
            /// Called to tell an object to load any device dependent parts of itself.
            /// </summary>
            public override void LoadContent(bool immediate)
            {
                if (mask == null)
                {
                    mask = BokuGame.Load<Texture2D>(BokuGame.Settings.MediaPath + @"Textures\FPEMask");
                }
                if (bump == null)
                {
                    bump = BokuGame.Load<Texture2D>(BokuGame.Settings.MediaPath + @"Textures\DistortionWake");
                }

                if (effect == null)
                {
                    effect = BokuGame.Load<Effect>(BokuGame.Settings.MediaPath + @"Shaders\FPEDistort");
                    effectCache.Load(Effect, "");
                }

                base.LoadContent(immediate);
            }

            public override void InitDeviceResources(GraphicsDevice device)
            {
                base.InitDeviceResources(device);
            }

            /// <summary>
            /// Called to tell an object to remove/delete any device dependent parts.
            /// </summary>
            public override void UnloadContent()
            {
                BokuGame.Release(ref mask);
                BokuGame.Release(ref bump);
                BokuGame.Release(ref effect);
                effectCache.UnLoad();
            }

            /// <summary>
            /// Load parameters into the effect for rendereing.
            /// </summary>
            /// <param name="camera"></param>
            /// <returns></returns>
            protected override bool SetupEffect(Camera camera)
            {
                GameActor firstPerson = CameraInfo.FirstPersonActor;
                Debug.Assert(firstPerson != null, "Shouldn't get this far into render when not first person");

                Parameter(EffectParams.BumpTint).SetValue(color);

                // uv.xy = pos * PosToUV.xz + PosToUV.yw;
                Parameter(EffectParams.PosToUV).SetValue(
                    new Vector4(
                        0.5f,
                        0.5f,
                        0.5f / camera.AspectRatio,
                        0.5f));

                Parameter(EffectParams.Mask).SetValue(Mask);
                Parameter(EffectParams.BumpTexture).SetValue(Bump);

                bumpScroll += Time.GameTimeFrameSeconds * bumpRate;
                bumpScroll = MyMath.Wrap(bumpScroll);
                Parameter(EffectParams.BumpScroll).SetValue(bumpScroll);
                Parameter(EffectParams.BumpScale).SetValue(bumpScale);

                return true;
            }
            #endregion Abstracts
            #endregion Internal
        }
    }
}
