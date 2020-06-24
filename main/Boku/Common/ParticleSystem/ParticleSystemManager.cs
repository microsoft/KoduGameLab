
//#define UPDATE_TIMERS

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Xml.Serialization;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Storage;

using KoiX;

using Boku.Base;
using Boku.Fx;

namespace Boku.Common.ParticleSystem
{
    /// <summary>
    /// Keeps a list of emitters and passes on Update and Render calls to them.
    /// </summary>
    public class ParticleSystemManager : INeedsDeviceReset
    {
        private List<BaseEmitter> emitterList = null;

        private Effect effect2d = null;
        private Effect effect3d = null;

        private int numParticles = 0;
        private static int maxNumParticles = 200000;    // Was 2000.

        /// <summary>
        /// Keep track of the number of distortion particles we have. This is strictly
        /// an optimization, so the distortion manager can skip out of some work when
        /// we don't have any.
        /// </summary>
        private int numPartyDistort = 0;

        #region EFFECT_CACHE
        public enum EffectParams2d
        {
            DiffuseColor,
            DiffuseTexture,
            EyeLocation,
            CameraUp,
            WorldMatrix,
            WorldViewProjMatrix,
            TileOffset,
            CurrentTime,
            MaxAge,
            Gravity,
            ParticleRadius,
        };
        public enum EffectTech2d
        {
            TexturedColorPassNormalAlpha = InGame.RenderEffect.Normal,
            TexturedColorPassPremultipliedAlpha,
            TexturedColorPassOneOneBlend,
            TexturedColorPassSimpleParticleOneOneBlend,
        };

        private EffectCache effectCache2d = new EffectCache<EffectParams2d, EffectTech2d>();

        public enum EffectParams3d
        {
            WorldViewProjMatrix,
            WorldMatrix,
            DiffuseColor,
            EmissiveColor,
            SpecularColor,
            SpecularPower,
            LightWrap,
            Shininess,
            GlowFactor,
            Alpha,
            EnvironmentMap,
            Radius,
        };
        public enum EffectTech3d
        {
            BasicColorPass = InGame.RenderEffect.Normal,
            OpaqueColorPass,
            TransparentColorPass,
            TransparentColorPassNoZ,
            PremultAlphaGlowColorPass,
        };

        private EffectCache effectCache3d = new EffectCache<EffectParams3d, EffectTech3d>();
        #endregion EFFECT_CACHE

        #region Accessors
        public EffectParameter Parameter(EffectParams2d param)
        {
            return effectCache2d.Parameter((int)param);
        }
        public EffectTechnique Technique(EffectTech2d tech)
        {
            return effectCache2d.Technique((int)tech);
        }
        public EffectTechnique Technique2d(int tech)
        {
            return effectCache2d.Technique(tech);
        }
        public Effect Effect2d
        {
            get { return effect2d; }
        }
        public EffectParameter Parameter(EffectParams3d param)
        {
            return effectCache3d.Parameter((int)param);
        }
        public EffectTechnique Technique(EffectTech3d tech)
        {
            return effectCache3d.Technique((int)tech);
        }
        public EffectTechnique Technique3d(int tech)
        {
            return effectCache3d.Technique(tech);
        }
        public Effect Effect3d
        {
            get { return effect3d; }
        }

        public int NumActiveParticles
        {
            get { return numParticles; }
        }
        #endregion

        // c'tor
        public ParticleSystemManager()
        {
            emitterList = new List<BaseEmitter>();

            SharedEmitterManager sharedEmitterManager = new SharedEmitterManager(this);
            sharedEmitterManager.Usage = BaseEmitter.Use.Distort | BaseEmitter.Use.Regular;
            sharedEmitterManager.AddToManager();

            BokuGame.Load(this);
        }   // end of c'tor

#if UPDATE_TIMERS
        PerfTimer updateTimer = new PerfTimer("ParticleSystemManager");     // Will update every 1 second (the default).
#endif
        public void Update()
        {
#if UPDATE_TIMERS
            updateTimer.Start();
#endif
            
            ExplosionManager.Update();

            int newNumParticles = 0;
            for (int i = 0; i < emitterList.Count; i++)
            {
                BaseEmitter emitter = emitterList[i];
                if (emitter != null)
                {
                    emitter.Update();

                    newNumParticles += emitter.ParticleCount();
                }
            }
            numParticles = newNumParticles;

#if UPDATE_TIMERS
            updateTimer.Stop();
#endif
        }   // end of ParticleSystemManager Update()

        public bool AllowEmission()
        {
            return numParticles < maxNumParticles;
        }
        public float AdjustEmissionRate(float emissionRate)
        {
            float throttleEnd = (float)maxNumParticles;
            float throttleBegin = throttleEnd * 0.5f;
            float throttle = MathHelper.Clamp(
                ((float)numParticles - throttleBegin) / (throttleEnd - throttleBegin),
                0.0f,
                1.0f);
            return emissionRate * (1.0f - throttle);
        }
        public Vector2 AdjustLifetime(float minLife, float maxLife)
        {
            float throttleEnd = (float)maxNumParticles;
            float throttleBegin = throttleEnd * 0.5f;
            float throttle = MathHelper.Clamp(
                ((float)numParticles - throttleBegin) / (throttleEnd - throttleBegin),
                0.0f,
                1.0f);
            maxLife *= (1.0f - throttle);

            Vector2 newLife = new Vector2(minLife, maxLife);
            return newLife;
        }

        public void Render(Camera camera)
        {
            for (int i = 0; i < emitterList.Count; i++)
            {
                BaseEmitter emitter = emitterList[i];
                if ((emitter != null) && emitter.HasUsage(BaseEmitter.Use.Regular))
                {
                    emitter.Render(camera);
                }
            }

        }   // end of ParticleSystemManager Render()

        public void Render(Camera camera, BaseEmitter.Use usage)
        {
            for (int i = 0; i < emitterList.Count; i++)
            {
                BaseEmitter emitter = emitterList[i];
                if ((emitter != null) && emitter.HasUsage(usage))
                {
                    emitter.Render(camera);
                }
            }

        }   // end of ParticleSystemManager Render()

        public void AddEmitter(BaseEmitter emitter)
        {
            if (!emitter.InManager)
            {
                if (emitter.HasUsage(BaseEmitter.Use.Distort))
                {
                    ++numPartyDistort;
                }
                emitter.InManager = true;
                emitterList.Add(emitter);
            }
        }   // end of ParticleSystemManager AddEmitter()

        public void RemoveEmitter(BaseEmitter emitter)
        {
            if (emitter.InManager)
            {
                emitterList.Remove(emitter);
                emitter.FlushAllParticles();
                emitter.InManager = false;
                if (emitter.HasUsage(BaseEmitter.Use.Distort))
                {
                    --numPartyDistort;
                }
            }
        }   // end of ParticleSystemManager RemoveEmitter()

        /// <summary>
        /// Removes all emitters from the manager except those that
        /// are persistent.  These are flushed.
        /// </summary>
        public void ClearAllEmitters()
        {
            int cnt = emitterList.Count;
            for (int i = cnt - 1; i >= 0; --i)
            {
                BaseEmitter e = emitterList[i];
                e.FlushAllParticles();
                if (!e.Persistent)
                {
                    emitterList.RemoveAt(i);

                    e.InManager = false;

                    if (e.HasUsage(BaseEmitter.Use.Distort))
                    {
                        --numPartyDistort;
                    }
                }
            }
            numParticles = 0;
        }   // end of ParticleSystemManager ClearAllEmitters()

        public void LoadContent(bool immediate)
        {
            if (effect2d == null)
            {
                effect2d = KoiLibrary.LoadEffect(@"Shaders\Particle2D");
                effectCache2d.Load(effect2d);
            }

            if (effect3d == null)
            {
                effect3d = KoiLibrary.LoadEffect(@"Shaders\Particle3D");
                ShaderGlobals.RegisterEffect("Particel3D", effect3d);
                effectCache3d.Load(effect3d);
            }

            SharedEmitterManager.LoadContent(immediate);

            BaseSpriteEmitter.LoadContent(immediate);

            ExplosionEmitter.LoadContent(immediate);
            FlowerEmitter.LoadContent(immediate);
            HeartEmitter.LoadContent(immediate);
            SmokeEmitter.LoadContent(immediate);
            StarEmitter.LoadContent(immediate);
            SteamEmitter.LoadContent(immediate);
            SwearEmitter.LoadContent(immediate);
            WreathEmitter.LoadContent(immediate);
            BeamExplosionEmitter.LoadContent(immediate);
            BeamSmokeEmitter.LoadContent(immediate);
            ScanExplosionEmitter.LoadContent(immediate);
            ScanSmokeEmitter.LoadContent(immediate);
            RoverScanExplosionEmitter.LoadContent(immediate);
            RoverScanSmokeEmitter.LoadContent(immediate);
            InspectExplosionEmitter.LoadContent(immediate);
            InspectSmokeEmitter.LoadContent(immediate);
            FanEmitter.LoadContent(immediate);
            InkEmitter.LoadContent(immediate);


        }   // end of ParticleSystemManager LoadContent()

        public void InitDeviceResources(GraphicsDevice device)
        {
            SharedEmitterManager.InitDeviceResources(device);

            BaseSpriteEmitter.InitDeviceResources(device);

            ExplosionEmitter.InitDeviceResources(device);
            FlowerEmitter.InitDeviceResources(device);
            HeartEmitter.InitDeviceResources(device);
            SmokeEmitter.InitDeviceResources(device);
            StarEmitter.InitDeviceResources(device);
            SteamEmitter.InitDeviceResources(device);
            SwearEmitter.InitDeviceResources(device);
            WreathEmitter.InitDeviceResources(device);

            BeamExplosionEmitter.InitDeviceResources(device);
            BeamSmokeEmitter.InitDeviceResources(device);
            ScanExplosionEmitter.InitDeviceResources(device);
            ScanSmokeEmitter.InitDeviceResources(device);
            RoverScanExplosionEmitter.InitDeviceResources(device);
            RoverScanSmokeEmitter.InitDeviceResources(device);
            InspectExplosionEmitter.InitDeviceResources(device);
            InspectSmokeEmitter.InitDeviceResources(device);
            FanEmitter.InitDeviceResources(device);
            InkEmitter.InitDeviceResources(device);
        }

        public void UnloadContent()
        {
            effectCache2d.UnLoad();
            DeviceResetX.Release(ref effect2d);
            DeviceResetX.Release(ref effect3d);

            SharedEmitterManager.UnloadContent();

            BaseSpriteEmitter.UnloadContent();

            ExplosionEmitter.UnloadContent();  // Remove the static texture instance.
            FlowerEmitter.UnloadContent();     // Remove the static texture instance.
            HeartEmitter.UnloadContent();      // Remove the static texture instance.
            SmokeEmitter.UnloadContent();      // Remove the static texture instance.
            StarEmitter.UnloadContent();
            SteamEmitter.UnloadContent();
            SwearEmitter.UnloadContent();
            WreathEmitter.UnloadContent();

            BeamExplosionEmitter.UnloadContent();
            BeamSmokeEmitter.UnloadContent();
            ScanExplosionEmitter.UnloadContent();
            ScanSmokeEmitter.UnloadContent();
            RoverScanExplosionEmitter.UnloadContent();
            RoverScanSmokeEmitter.UnloadContent();
            InspectExplosionEmitter.UnloadContent();
            InspectSmokeEmitter.UnloadContent();
            FanEmitter.UnloadContent();
            InkEmitter.UnloadContent();
        }   // end of ParticleSystemManager UnloadContent()

        public void DeviceReset(GraphicsDevice device)
        {
            SharedEmitterManager.DeviceReset(device);

            BaseSpriteEmitter.DeviceReset(device);

            ExplosionEmitter.DeviceReset(device);
            FlowerEmitter.DeviceReset(device);
            HeartEmitter.DeviceReset(device);
            SmokeEmitter.DeviceReset(device);
            StarEmitter.DeviceReset(device);
            SteamEmitter.DeviceReset(device);
            SwearEmitter.DeviceReset(device);
            WreathEmitter.DeviceReset(device);

            BeamExplosionEmitter.DeviceReset(device);
            BeamSmokeEmitter.DeviceReset(device);
            ScanExplosionEmitter.DeviceReset(device);
            ScanSmokeEmitter.DeviceReset(device);
            RoverScanExplosionEmitter.DeviceReset(device);
            RoverScanSmokeEmitter.DeviceReset(device);
            InspectExplosionEmitter.DeviceReset(device);
            InspectSmokeEmitter.DeviceReset(device);
            FanEmitter.DeviceReset(device);
            InkEmitter.DeviceReset(device);
        }

    }   // end of class ParticleSystemManager

}   // end of namespace Boku.Common
