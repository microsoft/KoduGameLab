
/// Relocated from Common namespace

using System;
using System.Collections.Generic;
using System.Diagnostics;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using KoiX;

using Boku.Base;
using Boku.Common;

namespace Boku.Fx
{
    public partial class FirstPersonEffectMgr
    {
        public class FPEGlow : FirstPersonEffect
        {
            #region Members
            private static Texture2D mask = null;

            private float secsToNext = -1.0f;
            private float counter = 0.0f;
            /// <summary>
            /// xyz => (current, prev, next)
            /// </summary>
            private Vector3 intensity = Vector3.One;
            /// <summary>
            /// xyz => (current, prev, next)
            /// </summary>
            private Vector3 size = Vector3.One;

            private enum EffectParams
            {
                GlowColor,
                PosToUV,
                Mask,
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
            #endregion


            #region Public

            /// <summary>
            /// Add a little dynamics to the effect.
            /// </summary>
            /// <returns></returns>
            public override bool Update()
            {
                counter += Time.GameTimeFrameSeconds;
                if (secsToNext - counter <= 0.0f)
                {
                    intensity.X = intensity.Z;
                    intensity.Y = intensity.Z;
                    
                    size.X = size.Z;
                    size.Y = size.Z;

                    float kBaseIntensity = 0.2f;
                    float kMaxIntensity = 0.25f;
                    intensity.Z = kBaseIntensity + (float)(rnd.NextDouble() * (kMaxIntensity - kBaseIntensity));

                    float kBaseSize = 0.9f;
                    float kMaxSize = 1.0f;
                    size.Z = kBaseSize + (float)(rnd.NextDouble() * (kMaxSize - kBaseSize));

                    counter = 0.0f;

                    float kBaseWait = 0.3f;
                    float kMaxWait = 0.5f;
                    secsToNext = kBaseWait + (float)(rnd.NextDouble() * (kMaxWait - kBaseWait));
                }
                else
                {
                    float t = counter / secsToNext;
                    intensity.X = intensity.Y + t * (intensity.Z - intensity.Y);
                    size.X = size.Y + t * (size.Z - size.Y);
                }

                return base.Update();
            }

            /// <summary>
            /// Blit it out there if we're active.
            /// </summary>
            /// <param name="camera"></param>
            public override void Render(Camera camera)
            {
                if (InGame.inGame.renderEffects == InGame.RenderEffect.Normal)
                {
                    GameActor firstPerson = CameraInfo.FirstPersonActor;
                    if (firstPerson != null)
                    {
                        if (firstPerson.Glowing)
                        {
                            base.Render(camera);
                        }
                    }
                }
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
                    mask = KoiLibrary.LoadTexture2D(@"Textures\FPEMask");
                }

                if (effect == null)
                {
                    effect = KoiLibrary.LoadEffect(@"Shaders\FPEGlow");
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
                DeviceResetX.Release(ref mask);
                DeviceResetX.Release(ref effect);

                effectCache.UnLoad();

                base.UnloadContent();
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

                Vector4 glowColor = firstPerson.GlowEmitter.Color;
                glowColor.W *= intensity.X;

                Parameter(EffectParams.GlowColor).SetValue(glowColor);
                // uv.xy = pos * PosToUV.xz + PosToUV.yw;
                Parameter(EffectParams.PosToUV).SetValue(
                    new Vector4(
                        0.5f * size.X,
                        0.5f,
                        0.5f / camera.AspectRatio * size.X,
                        0.5f));

                Parameter(EffectParams.Mask).SetValue(Mask);

                return true;
            }
            #endregion Abstracts
            #endregion Internal
        }
    }
}
