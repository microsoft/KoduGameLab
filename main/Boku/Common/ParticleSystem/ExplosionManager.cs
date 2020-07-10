// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.


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

using Boku.Audio;
using Boku.Base;

namespace Boku.Common.ParticleSystem
{
    /// <summary>
    /// Provides a single entry point for creating explosions anywhere in the scene.  
    /// Explosions consist of a combination of an Explosion emitter which does the 
    /// central fireball and a smoke emitter which does the lingering cloud after 
    /// the explosion.
    /// 
    /// One of the key features is that the emitters are given a fixed life span
    /// after which the manager kills them off.
    /// 
    /// Also used to manage the creation of emitter puffs, steam puffs, sparks etc.  For
    /// these we create a single, static instance of the emitter and just reuse it
    /// when needed.
    /// </summary>
    public class ExplosionManager
    {
        /// <summary>
        /// This is the base class for the elements managed by the explosion manager.
        /// TODO need a better name for this.
        /// </summary>
        abstract public class BaseFoo
        {
            public Vector3 position = new Vector3();
            public float radius = 1.0f;
            public double startTime = 0;                // When was this foo started?

            /// <summary>
            /// Update call for this foo.
            /// </summary>
            /// <param name="index">Index into the fooList to facilitate removing foo after it has timed out.</param>
            abstract public void Update(int index);
        }   // end of class BaseFoo

        public class BeamExplosion : BaseFoo
        {
            public double beamFireballDuration = 0.1f;      // How long should the fireball particles be emitted?
            public double beamSmokeDuration = 0.5f;         // How long should the smoke particles be emitted?

            public BeamSmokeEmitter beamSmoke = null;
            public BeamExplosionEmitter beamFireball = null;

            public override void Update(int index)
            {
                // Check if either emitter has outlived its usefulness,
                // if so tell it to stop putting out particles and die
                // a graceful death.  If they're both dead, get rid of
                // the explosion.
                double age = Time.GameTimeTotalSeconds - startTime;

                if (age > beamFireballDuration && beamFireball.Emitting)
                {
                    beamFireball.Emitting = false;
                    beamFireball.Dying = true;

                    // If both are dead, remove this explosion.
                    if (beamSmoke.Emitting == false)
                    {
                        fooList.RemoveAt(index);
                        beamExplosions.Add(this);
                    }
                }
                if (age > beamSmokeDuration && beamSmoke.Emitting)
                {
                    beamSmoke.Emitting = false;
                    beamSmoke.Dying = true;

                    // If both are dead, remove this explosion.
                    if (beamFireball.Emitting == false)
                    {
                        fooList.RemoveAt(index);
                        beamExplosions.Add(this);
                    }
                }
            }   // end of BeamExplosion Update()
        }

        public class ScanVFX : BaseFoo
        {
            public double scanExplosionDuration = 0.1f;      // How long should the fireball particles be emitted?
            public double scanSmokeDuration = 0.5f;         // How long should the smoke particles be emitted?

            public ScanSmokeEmitter scanSmoke = null;
            public ScanExplosionEmitter scanExplode = null;

            public override void Update(int index)
            {
                // Check if either emitter has outlived its usefulness,
                // if so tell it to stop putting out particles and die
                // a graceful death.  If they're both dead, get rid of
                // the explosion.
                double age = Time.GameTimeTotalSeconds - startTime;

                if (age > scanExplosionDuration && scanExplode.Emitting)
                {
                    scanExplode.Emitting = false;
                    scanExplode.Dying = true;

                    // If both are dead, remove this explosion.
                    if (scanSmoke.Emitting == false)
                    {
                        fooList.RemoveAt(index);
                        scanVFXs.Add(this);
                    }
                }
                if (age > scanSmokeDuration && scanSmoke.Emitting)
                {
                    scanSmoke.Emitting = false;
                    scanSmoke.Dying = true;

                    // If both are dead, remove this explosion.
                    if (scanExplode.Emitting == false)
                    {
                        fooList.RemoveAt(index);
                        scanVFXs.Add(this);
                    }
                }
            }   // end of ScanVFX Update()
        }

        public class RoverScanEffect : BaseFoo
        {
            public double scanExplosionDuration = 0.1f;      // How long should the fireball particles be emitted?
            public double scanSmokeDuration = 0.5f;         // How long should the smoke particles be emitted?

            public RoverScanSmokeEmitter scanSmoke = null;
            public RoverScanExplosionEmitter scanExplode = null;

            public override void Update(int index)
            {
                // Check if either emitter has outlived its usefulness,
                // if so tell it to stop putting out particles and die
                // a graceful death.  If they're both dead, get rid of
                // the explosion.
                double age = Time.GameTimeTotalSeconds - startTime;

                if (age > scanExplosionDuration && scanExplode.Emitting)
                {
                    scanExplode.Emitting = false;
                    scanExplode.Dying = true;

                    // If both are dead, remove this explosion.
                    if (scanSmoke.Emitting == false)
                    {
                        fooList.RemoveAt(index);
                        roverEffects.Add(this);
                    }
                }
                if (age > scanSmokeDuration && scanSmoke.Emitting)
                {
                    scanSmoke.Emitting = false;
                    scanSmoke.Dying = true;

                    // If both are dead, remove this explosion.
                    if (scanExplode.Emitting == false)
                    {
                        fooList.RemoveAt(index);
                        roverEffects.Add(this);
                    }
                }
            }   // end of RoverScanEffect Update()
        }

        public class InspectVFX : BaseFoo
        {
            public double inspectExplosionDuration = 0.1f;      // How long should the fireball particles be emitted?
            public double inspectSmokeDuration = 0.5f;         // How long should the smoke particles be emitted?

            public InspectSmokeEmitter inspectSmoke = null;
            public InspectExplosionEmitter inspectExplode = null;

            public override void Update(int index)
            {
                // Check if either emitter has outlived its usefulness,
                // if so tell it to stop putting out particles and die
                // a graceful death.  If they're both dead, get rid of
                // the explosion.
                double age = Time.GameTimeTotalSeconds - startTime;

                if (age > inspectExplosionDuration && inspectExplode.Emitting)
                {
                    inspectExplode.Emitting = false;
                    inspectExplode.Dying = true;

                    // If both are dead, remove this explosion.
                    if (inspectSmoke.Emitting == false)
                    {
                        fooList.RemoveAt(index);
                        inspectVFXs.Add(this);
                    }
                }
                if (age > inspectSmokeDuration && inspectSmoke.Emitting)
                {
                    inspectSmoke.Emitting = false;
                    inspectSmoke.Dying = true;

                    // If both are dead, remove this explosion.
                    if (inspectExplode.Emitting == false)
                    {
                        fooList.RemoveAt(index);
                        inspectVFXs.Add(this);
                    }
                }
            }   // end of InspectVFX Update()
        }

        public class Explosion : BaseFoo
        {
            public double fireballDuration = 0.1f;      // How long should the fireball particles be emitted?
            public double smokeDuration = 0.5f;         // How long should the smoke particles be emitted?

            public SmokeEmitter smoke = null;
            public ExplosionEmitter fireball = null;

            public override void Update(int index)
            {
                // Check if either emitter has outlived its usefulness,
                // if so tell it to stop putting out particles and die
                // a graceful death.  If they're both dead, get rid of
                // the explosion.
                double age = Time.GameTimeTotalSeconds - startTime;

                if (age > fireballDuration && fireball.Emitting)
                {
                    fireball.Emitting = false;
                    fireball.Dying = true;

                    // If both are dead, remove this explosion.
                    if (smoke.Emitting == false)
                    {
                        fooList.RemoveAt(index);
                        explosions.Add(this);
                    }
                }
                if (age > smokeDuration && smoke.Emitting)
                {
                    smoke.Emitting = false;
                    smoke.Dying = true;

                    // If both are dead, remove this explosion.
                    if (fireball.Emitting == false)
                    {
                        fooList.RemoveAt(index);
                        explosions.Add(this);
                    }
                }
            }   // end of Explosion Update()

        }   // end of class Explosion

        public abstract class EmitterPuff : BaseFoo
        {
            public abstract BaseEmitter Emitter { get; }

        }
        public class DustPuff : EmitterPuff
        {
            public double dustDuration = 0.1f;          // How long should the emitter particles be emitted?

            public DustEmitter emitter = null;
            public override BaseEmitter Emitter { get { return emitter; } }

            public DustPuff()
            {
                emitter = new DustEmitter(InGame.inGame.ParticleSystemManager);
            }

            public override void Update(int index)
            {
                // Check if the emitter should stop emitting particles.
                double age = Time.GameTimeTotalSeconds - startTime;

                if (age > dustDuration)
                {
                    emitter.Emitting = false;
                    emitter.Dying = true;
                }
                if (!emitter.Active)
                {
                    fooList.RemoveAt(index);
                    dustPuffs.Add(this);
                }
            }   // end of DustPuff Update()
        
        }   // end of class DustPuff

        public class SteamPuff : EmitterPuff
        {
            public double steamDuration = 0.2f;          // How long should the particles be emitted?

            public SteamEmitter emitter = null;
            public override BaseEmitter Emitter { get { return emitter; } }

            public SteamPuff()
            {
                emitter = new SteamEmitter(InGame.inGame.ParticleSystemManager);
            }
            public override void Update(int index)
            {
                // Check if the emitter should stop emitting particles.
                double age = Time.GameTimeTotalSeconds - startTime;

                if (age > steamDuration)
                {
                    emitter.Emitting = false;
                    emitter.Dying = true;
                }
                if (!emitter.Active)
                {
                    fooList.RemoveAt(index);
                    steamPuffs.Add(this);
                }
            }   // end of SteamPuff Update()

        }   // end of class SteamPuff

        public class SparkPuff : EmitterPuff
        {
            public SparkEmitter emitter = null;
            public override BaseEmitter Emitter { get { return emitter; } }

            public SparkPuff()
            {
                emitter = new SparkEmitter(InGame.inGame.ParticleSystemManager);
            }

            public override void Update(int index)
            {
                // Nothing to see here, move along.
            }   // end of SparkPuff Update()

        }   // end of class SparkPuff

        public class SplashPuff : EmitterPuff
        {
            public SplashEmitter emitter = null;
            public override BaseEmitter Emitter { get { return emitter; } }

            public SplashPuff()
            {
                emitter = new SplashEmitter(InGame.inGame.ParticleSystemManager);
            }

            public override void Update(int index)
            {
                // Nothing to see here, move along.
            }   // end of SplashPuff Update()

        }   // end of class SplashPuff

        private static List<BaseFoo> fooList = new List<BaseFoo>();    // List of BaseFoos.

        private static List<DustPuff> dustPuffs = new List<DustPuff>();
        private static List<SteamPuff> steamPuffs = new List<SteamPuff>();
        private static List<Explosion> explosions = new List<Explosion>();
        private static List<BeamExplosion> beamExplosions = new List<BeamExplosion>();
        private static List<ScanVFX> scanVFXs = new List<ScanVFX>();
        private static List<RoverScanEffect> roverEffects = new List<RoverScanEffect>();
        private static List<InspectVFX> inspectVFXs = new List<InspectVFX>();
        private static SparkPuff sparkPuff = new SparkPuff();
        private static SplashPuff splashPuff = new SplashPuff();

        // c'tor
        private ExplosionManager()
        {
        }   // end of c'tor

        private static FooType NewFoo<FooType>(List<FooType> list) where FooType : EmitterPuff, new()
        {
            FooType ret = null;
            if (list.Count > 0)
            {
                ret = list[list.Count - 1];
                list.RemoveAt(list.Count - 1);
            }
            else
            {
                ret = new FooType();
            }
            ret.Emitter.AddToManager();
            ret.Emitter.Active = true;
            ret.Emitter.Dying = false;
            ret.Emitter.Emitting = true;
            fooList.Add(ret);
            return ret;
        }
        private static Explosion NewExplosion()
        {
            Explosion ret = null;
            if (explosions.Count > 0)
            {
                ret = explosions[explosions.Count - 1];
                explosions.RemoveAt(explosions.Count - 1);
            }
            else
            {
                ret = new Explosion();
            }
            return ret;
        }

        private static BeamExplosion NewBeamExplosion()
        {
            BeamExplosion ret = null;
            if (beamExplosions.Count > 0)
            {
                ret = beamExplosions[beamExplosions.Count - 1];
                beamExplosions.RemoveAt(beamExplosions.Count - 1);
            }
            else
            {
                ret = new BeamExplosion();
            }
            return ret;
        }

        private static ScanVFX NewScanEffect()
        {
            ScanVFX ret = null;
            if (scanVFXs.Count > 0)
            {
                ret = scanVFXs[scanVFXs.Count - 1];
                scanVFXs.RemoveAt(scanVFXs.Count - 1);
            }
            else
            {
                ret = new ScanVFX();
            }
            return ret;
        }

        private static RoverScanEffect NewRoverScanEffect()
        {
            RoverScanEffect ret = null;
            if (roverEffects.Count > 0)
            {
                ret = roverEffects[roverEffects.Count - 1];
                roverEffects.RemoveAt(roverEffects.Count - 1);
            }
            else
            {
                ret = new RoverScanEffect();
            }
            return ret;
        }

        private static InspectVFX NewInspectEffect()
        {
            InspectVFX ret = null;
            if (inspectVFXs.Count > 0)
            {
                ret = inspectVFXs[inspectVFXs.Count - 1];
                inspectVFXs.RemoveAt(inspectVFXs.Count - 1);
            }
            else
            {
                ret = new InspectVFX();
            }
            return ret;
        }

        private static void Nuke(BaseFoo foo)
        {
            if (foo is DustPuff)
            {
                DustPuff puff = foo as DustPuff;
                puff.Emitter.RemoveFromManager();
                dustPuffs.Add(puff);
            }
            else if (foo is SteamPuff)
            {
                SteamPuff puff = foo as SteamPuff;
                puff.Emitter.RemoveFromManager();
                steamPuffs.Add(puff);
            }
            else if (foo is Explosion)
            {
                Explosion explosion = foo as Explosion;
                explosion.fireball.RemoveFromManager();
                explosion.smoke.RemoveFromManager();
                explosions.Add(explosion);
            }
            else if (foo is BeamExplosion)
            {
                BeamExplosion beamExplosion = foo as BeamExplosion;
                beamExplosion.beamFireball.RemoveFromManager();
                beamExplosion.beamSmoke.RemoveFromManager();
                beamExplosions.Add(beamExplosion);
            }
            else if (foo is ScanVFX)
            {
                ScanVFX scanVFX = foo as ScanVFX;
                scanVFX.scanExplode.RemoveFromManager();
                scanVFX.scanSmoke.RemoveFromManager();
                scanVFXs.Add(scanVFX);
            }
            else if (foo is RoverScanEffect)
            {
                RoverScanEffect roverEffect = foo as RoverScanEffect;
                roverEffect.scanExplode.RemoveFromManager();
                roverEffect.scanSmoke.RemoveFromManager();
                roverEffects.Add(roverEffect);
            }
            else if (foo is InspectVFX)
            {
                InspectVFX inspect = foo as InspectVFX;
                inspect.inspectExplode.RemoveFromManager();
                inspect.inspectSmoke.RemoveFromManager();
                inspectVFXs.Add(inspect);
            }
        }
        public static void Init()
        {
            sparkPuff.emitter = new SparkEmitter(InGame.inGame.ParticleSystemManager);
            sparkPuff.Emitter.AddToManager();
            sparkPuff.Emitter.Active = true;
            fooList.Add(sparkPuff);

            splashPuff.emitter = new SplashEmitter(InGame.inGame.ParticleSystemManager);
            splashPuff.Emitter.AddToManager();
            splashPuff.Emitter.Active = true;
            fooList.Add(splashPuff);
        }   // end of Init()

        public static void Resume()
        {
            sparkPuff.emitter.AddToManager();
            splashPuff.emitter.AddToManager();
        }

        public static void Suspend()
        {
            for (int i = fooList.Count - 1; i >= 0; --i)
            {
                Nuke(fooList[i]);
            }
            fooList.Clear();

            sparkPuff.Emitter.RemoveFromManager();
            splashPuff.Emitter.RemoveFromManager();
        }

        public static void Update()
        {
            // Loop through the list backwards in case we remove one.
            for (int i = fooList.Count - 1; i >= 0;  i--)
            {
                BaseFoo foo = (BaseFoo)fooList[i];
                foo.Update(i);
            }   // end of loop over list.

        }   // end of ExplosionManager Update()

        /// <summary>
        /// Public call which allows the creation of an explosion from anywhere in game.
        /// </summary>
        /// <param name="position">Where the explosion occurs.</param>
        /// <param name="radius">Approximate size of the explosion.</param>
        public static void CreateExplosion(Vector3 position, float radius)
        {
            Explosion e = NewExplosion();

            e.position = position;
            e.radius = radius;

            e.smokeDuration = 0.5f;
            e.smoke = new SmokeEmitter(InGame.inGame.ParticleSystemManager);
            e.smoke.Position = position;
            e.smoke.ResetPreviousPosition();
            e.smoke.PositionJitter = 0.2f;  // Random offset for each particle.
            e.smoke.StartRadius = radius * 0.5f;
            e.smoke.EndRadius = radius;
            e.smoke.EmissionRate = 20.0f;
            e.smoke.Color = new Vector4(0.4f, 0.4f, 0.4f, 1.0f);    // Dark grey.
            e.smoke.Active = true;

            e.fireballDuration = 0.05f;
            e.fireball = new ExplosionEmitter(InGame.inGame.ParticleSystemManager);
            e.fireball.Position = position;
            e.fireball.ResetPreviousPosition();
            e.fireball.PositionJitter = 0.8f;   // Random offset for each particle.
            e.fireball.StartRadius = radius * 0.2f;
            e.fireball.EndRadius = radius * 0.5f;
            e.fireball.EmissionRate = 100.0f;
            e.fireball.MinLifetime = 0.5f;
            e.fireball.MaxLifetime = 1.0f;
            e.fireball.Active = true;

            e.startTime = Time.GameTimeTotalSeconds;

            e.smoke.AddToManager();
            e.fireball.AddToManager();

            fooList.Add(e);

        }   // end of ExplosionManager CreateExplosion()

        /// <summary>
        /// Public call which allows the creation of an beam explosion from anywhere in game.
        /// </summary>
        /// <param name="position">Where the explosion occurs.</param>
        /// <param name="radius">Approximate size of the explosion.</param>
        public static void CreateBeamExplosion(Vector3 position, float radius)
        {
            BeamExplosion e = NewBeamExplosion();

            e.position = position;
            e.radius = radius;

            e.beamSmokeDuration = 0.85f;
            e.beamSmoke = new BeamSmokeEmitter(InGame.inGame.ParticleSystemManager);
            e.beamSmoke.Position = position;
            e.beamSmoke.ResetPreviousPosition();
            e.beamSmoke.PositionJitter = 0.0f;  // Random offset for each particle.
            e.beamSmoke.StartRadius = radius * 0.2f;
            e.beamSmoke.EndRadius = radius * 1.4f;
            e.beamSmoke.EmissionRate = 15.0f;
            e.beamSmoke.Color = new Vector4(0.4f, 0.8f, 1.0f, 0.5f);    
            e.beamSmoke.Active = true;

            e.beamFireballDuration = 0.6f;
            e.beamFireball = new BeamExplosionEmitter(InGame.inGame.ParticleSystemManager);
            e.beamFireball.Position = position;
            e.beamFireball.ResetPreviousPosition();
            e.beamFireball.PositionJitter = 0.0f;   // Random offset for each particle.
            e.beamFireball.StartRadius = radius * 0.2f;
            e.beamFireball.EndRadius = radius * 1.4f;
            e.beamFireball.EmissionRate = 15.0f;
            e.beamFireball.MinLifetime = 0.1f;
            e.beamFireball.MaxLifetime = 0.4f;
            e.beamFireball.MaxRotationRate = 0.1f;
            e.beamFireball.Active = true;

            e.startTime = Time.GameTimeTotalSeconds;

            e.beamSmoke.AddToManager();
            e.beamFireball.AddToManager();

            fooList.Add(e);

        }   // end of ExplosionManager CreateBeamExplosion()

        /// <summary>
        /// Public call which allows the creation of an scan effect from anywhere in game.
        /// </summary>
        /// <param name="position">Where the explosion occurs.</param>
        /// <param name="radius">Approximate size of the explosion.</param>
        public static void CreateScanEffect(Vector3 position, float radius)
        {
            ScanVFX e = NewScanEffect();

            e.position = position;
            e.radius = radius;

            e.scanSmokeDuration = 0.7f;
            e.scanSmoke = new ScanSmokeEmitter(InGame.inGame.ParticleSystemManager);
            e.scanSmoke.Position = position;
            e.scanSmoke.ResetPreviousPosition();
            e.scanSmoke.PositionJitter = 0.0f;  // Random offset for each particle.
            e.scanSmoke.StartRadius = radius * 0.5f;
            e.scanSmoke.EndRadius = radius;
            e.scanSmoke.EmissionRate = 20.0f;
            e.scanSmoke.Gravity = new Vector3(0.0f, 0.0f, 100.0f);
            e.scanSmoke.Color = new Vector4(0.2f, 0.6f, 0.3f, 0.5f);
            e.scanSmoke.MaxRotationRate = 0.0f;
            e.scanSmoke.Active = true;

            e.scanExplosionDuration = 0.05f;
            e.scanExplode = new ScanExplosionEmitter(InGame.inGame.ParticleSystemManager);
            e.scanExplode.Position = position;
            e.scanExplode.ResetPreviousPosition();
            e.scanExplode.PositionJitter = 0.0f;   // Random offset for each particle.
            e.scanExplode.StartRadius = radius * 0.2f;
            e.scanExplode.EndRadius = radius * 0.8f;
            e.scanExplode.EmissionRate = 80.0f;
            e.scanExplode.Gravity = new Vector3(0.0f, 0.0f, 100.0f);
            e.scanExplode.MinLifetime = 0.5f;
            e.scanExplode.MaxLifetime = 1.0f;
            e.scanExplode.MaxRotationRate = 0.0f;
            e.scanExplode.Color = new Vector4(1.0f, 1.0f, 1.0f, 1.0f);   
            e.scanExplode.Active = true;

            e.startTime = Time.GameTimeTotalSeconds;

            e.scanSmoke.AddToManager();
            e.scanExplode.AddToManager();

            fooList.Add(e);

        }   // end of ExplosionManager CreateScanEffect()

        /// <summary>
        /// Public call which allows the creation of an scan effect from anywhere in game.
        /// </summary>
        /// <param name="position">Where the explosion occurs.</param>
        /// <param name="radius">Approximate size of the explosion.</param>
        public static void CreateRoverScanEffect(Vector3 position, float radius)
        {
            RoverScanEffect e = NewRoverScanEffect();

            e.position = position;
            e.radius = radius;

            e.scanSmokeDuration = 0.7f;
            e.scanSmoke = new RoverScanSmokeEmitter(InGame.inGame.ParticleSystemManager);
            e.scanSmoke.Position = position;
            e.scanSmoke.ResetPreviousPosition();
            e.scanSmoke.PositionJitter = 0.0f;  // Random offset for each particle.
            e.scanSmoke.StartRadius = radius * 0.02f;
            e.scanSmoke.EndRadius = radius * 1.333f;
            e.scanSmoke.EmissionRate = 6.0f;
            e.scanSmoke.Gravity = new Vector3(0.0f, 0.0f, 100.0f);
            e.scanSmoke.MinLifetime = 4.0f;
            e.scanSmoke.MaxLifetime = 4.0f;
            e.scanSmoke.Color = new Vector4(0.3f, 0.6f, 0.5f, 1.0f);
            e.scanSmoke.MaxRotationRate = 0.0f;
            e.scanSmoke.Active = true;

            e.scanExplosionDuration = 0.7f;
            e.scanExplode = new RoverScanExplosionEmitter(InGame.inGame.ParticleSystemManager);
            e.scanExplode.Position = position;
            e.scanExplode.ResetPreviousPosition();
            e.scanExplode.PositionJitter = 0.0f;   // Random offset for each particle.
            e.scanExplode.StartRadius = radius * 0.02f;
            e.scanExplode.EndRadius = radius;
            e.scanExplode.EmissionRate = 4.0f;
            e.scanExplode.Gravity = new Vector3(0.0f, 0.0f, 100.0f);
            e.scanExplode.MinLifetime = 3.0f;
            e.scanExplode.MaxLifetime = 3.0f;
            e.scanExplode.MaxRotationRate = 0.0f;
            e.scanExplode.Color = new Vector4(0.3f, 0.6f, 0.5f, 1.0f);
            e.scanExplode.Active = true;

            e.startTime = Time.GameTimeTotalSeconds;

            e.scanSmoke.AddToManager();
            e.scanExplode.AddToManager();

            fooList.Add(e);

        }   // end of ExplosionManager CreateRoverScanEffect()

        /// <summary>
        /// Public call which allows the creation of an scan effect from anywhere in game.
        /// </summary>
        /// <param name="position">Where the explosion occurs.</param>
        /// <param name="radius">Approximate size of the explosion.</param>
        public static void CreateInspectEffect(Vector3 position, float radius)
        {
            InspectVFX e = NewInspectEffect();

            e.position = position;
            e.radius = radius;

            e.inspectSmokeDuration = 0.9f;
            e.inspectSmoke = new InspectSmokeEmitter(InGame.inGame.ParticleSystemManager);
            e.inspectSmoke.Position = position;
            e.inspectSmoke.ResetPreviousPosition();
            e.inspectSmoke.PositionJitter = 0.2f;  // Random offset for each particle.
            e.inspectSmoke.StartRadius = radius * 0.5f;
            e.inspectSmoke.EndRadius = radius * 1.3f;
            e.inspectSmoke.EmissionRate = 20.0f;
            e.inspectSmoke.MaxRotationRate = 0.1f;
            e.inspectSmoke.Color = new Vector4(1.0f, 0.8f, 0.6f, 0.4f);   
            e.inspectSmoke.Active = true;

            e.inspectExplosionDuration = 0.4f;
            e.inspectExplode = new InspectExplosionEmitter(InGame.inGame.ParticleSystemManager);
            e.inspectExplode.Position = position;
            e.inspectExplode.ResetPreviousPosition();
            e.inspectExplode.PositionJitter = 0.0f;   // Random offset for each particle.
            e.inspectExplode.StartRadius = radius * 0.2f;
            e.inspectExplode.EndRadius = radius * 1.3f;
            e.inspectExplode.MaxRotationRate = 0.1f;
            e.inspectExplode.EmissionRate = 30.0f;
            e.inspectExplode.MinLifetime = 0.1f;
            e.inspectExplode.MaxLifetime = 0.3f;
            e.inspectExplode.Color = new Vector4(1.0f, 1.0f, 1.0f, 1.0f);   
            e.inspectExplode.Active = true;

            e.startTime = Time.GameTimeTotalSeconds;

            e.inspectSmoke.AddToManager();
            e.inspectExplode.AddToManager();

            fooList.Add(e);

        }   // end of ExplosionManager CreateScanEffect()

        /// <summary>
        /// Public call which allows the creation of a dust puff from anywhere in game.
        /// </summary>
        /// <param name="position">Where the emitter puff occurs.</param>
        /// <param name="radius">Approximate size of the puff.</param>
        /// <param name="density">Acts as a multiplier on the emission rate to create more or less dense puffs.</param>
        public static void CreateDustPuff(Vector3 position, float radius, float density)
        {
            DustPuff dustPuff = NewFoo<DustPuff>(dustPuffs);
            dustPuff.position = position;
            dustPuff.radius = radius;

            dustPuff.dustDuration = 0.1f;

            dustPuff.emitter.Position = position;
            dustPuff.emitter.ResetPreviousPosition();
            dustPuff.emitter.PositionJitter = radius * 0.7f;      // Random offset for each particle.
            dustPuff.emitter.StartRadius = radius * 0.4f;
            dustPuff.emitter.EndRadius = radius;
            dustPuff.emitter.EmissionRate = density;
            dustPuff.emitter.Active = true;
            dustPuff.emitter.Emitting = true;

            dustPuff.startTime = Time.GameTimeTotalSeconds;

        }   // end of ExplosionManager CreateDustPuff()

        /// <summary>
        /// Public call which allows the creation of a steam puff from anywhere in game.
        /// </summary>
        /// <param name="position">Where the steam puff occurs.</param>
        /// <param name="radius">Approximate size of the puff.</param>
        /// <param name="density">Acts as a multiplier on the emission rate to create more or less dense puffs.</param>
        public static void CreateSteamPuff(Vector3 position, float density, float scale)
        {
            SteamPuff steamPuff = NewFoo<SteamPuff>(steamPuffs);

            steamPuff.position = position;

            steamPuff.emitter.Position = position;
            steamPuff.emitter.ResetPreviousPosition();
            steamPuff.emitter.Scale = scale;
            steamPuff.emitter.PositionJitter = steamPuff.emitter.StartRadius * scale * 0.5f;    // Random offset for each particle.
            steamPuff.emitter.EmissionRate = 100.0f * density;
            steamPuff.emitter.Active = true;
            steamPuff.emitter.Emitting = true;

            steamPuff.startTime = Time.GameTimeTotalSeconds;

        }   // end of ExplosionManager CreateSteamPuff()

        /// <summary>
        /// Sparks are simple particle systems which emit all their 
        /// particle at the same time upon creation.  This causes the 
        /// emitter to immediately be put into the "dying" state so it
        /// doesn't need to be put into the fooList.
        /// </summary>
        /// <param name="position"></param>
        /// <param name="numParticles"></param>
        public static void CreateSpark(Vector3 position, int numParticles, float radius, float speed)
        {
            sparkPuff.emitter.AddSparks(numParticles, position, 0.0f, radius, speed, Vector4.One);
        }   // end of ExplosionManager CreateDustPuff()

        public static void CreateSpark(Vector3 position, int numParticles, float radius, float speed, Vector4 color)
        {
            sparkPuff.emitter.AddSparks(numParticles, position, 0.0f, radius, speed, color);
        }   // end of ExplosionManager CreateDustPuff()

        /// <summary>
        /// Create sparks from an explicit list of positions and velocities.
        /// </summary>
        /// <param name="numSparks">How many</param>
        /// <param name="positions">Where</param>
        /// <param name="velocities">Velocity includes speed</param>
        /// <param name="radius">Max size at death</param>
        public static void CreateSparks(int numSparks, Vector3[] positions, Vector3[] velocities, float radius)
        {
            sparkPuff.emitter.AddSparks(numSparks, positions, velocities, 0.0f, radius);
        }

        /// <summary>
        /// Create a splash around the given position and velocity axis.
        /// </summary>
        /// <param name="numSplashes">How many</param>
        /// <param name="pos">Center of spray</param>
        /// <param name="vel">Central axis of spray</param>
        /// <param name="radius">Largest the particles get</param>
        /// <param name="tint">Tint</param>
        public static void CreateSplashes(int numSplashes, Vector3 pos, Vector3 vel, float radius, Vector4 tint)
        {
            splashPuff.emitter.AddSplashes(numSplashes, pos, vel, radius, tint);
            Foley.PlaySplash(pos, 0.3f);
        }
    }   // end of class ExplosionManager

}   // end of namespace Boku.Common
