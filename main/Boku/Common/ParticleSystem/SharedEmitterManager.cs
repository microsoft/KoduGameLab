
using System;
using System.Collections;
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

using Boku.Base;

namespace Boku.Common.ParticleSystem
{
    /// <summary>
    /// Provides a common entry point for the sources to add particles to the emitters.  In the 
    /// case of the smoke emitters this also allows there to be several so that we can bin the
    /// particles based on their life span.  This prevents long lived particles from "locking in"
    /// short lived ones.  We only have a sinlge distorted smoke emitter since these all tend
    /// to be short-lived.
    /// 
    /// Note that the emitters managed here are not added to the normal particle system manager.
    /// Since we know more about them we can be a bit more efficient during rendering.
    /// </summary>
    public class SharedEmitterManager : BaseEmitter
    {
        #region Members

        // Smoke emitters.
        private const int kNumSmokes = 4;
        private static float[] smokeMaxLife = new float[kNumSmokes] { 1.0f, 4.0f, 8.0f, float.MaxValue };
        private static SharedSmokeEmitter[] smoke = null;

        // Distorted smoke emitter.
        private static SharedSmokeEmitter distortedSmoke = null;
        private static BeamManager beam = null;
        private static BleepManager bleep = null;
        private static SharedSparkEmitter spark = null;
        private static SharedSplashEmitter splash = null;

        #endregion

        #region Accessors
        /// <summary>
        /// Provide access to the bleep manager, for firing off bleeps.
        /// </summary>
        public static BeamManager Beams
        {
            get { return beam; }
        } 
        public static BleepManager Bleeps
        {
            get { return bleep; }
        }
        public static SharedSparkEmitter Sparks
        {
            get { return spark; }
        }
        public static SharedSplashEmitter Splashes
        {
            get { return splash; }
        }
        #endregion

        #region Public
        
        public SharedEmitterManager(ParticleSystemManager manager)
            : base(manager)
        {
            Persistent = true;  // Tell ParticleSystemManager not to clear me.

            //
            // Create shared emitters.
            //

            // Smoke
            smoke = new SharedSmokeEmitter[kNumSmokes];
            smoke[0] = new SharedSmokeEmitter(manager, 5000);
            smoke[1] = new SharedSmokeEmitter(manager, 8000);
            smoke[2] = new SharedSmokeEmitter(manager, 2000);
            smoke[3] = new SharedSmokeEmitter(manager, 2000);

            distortedSmoke = new SharedSmokeEmitter(manager, 1000);
            distortedSmoke.Usage = Use.Distort;

            beam = new BeamManager(manager);

            bleep = new BleepManager(manager);

            spark = new SharedSparkEmitter(manager, 1000);

            splash = new SharedSplashEmitter(manager, 1000);

        }   // end of c'tor


        public static void AddSmokeParticle(ref SharedSmokeEmitter.SmokeParticle p)
        {
            // Get the index formn the lifetime.
            int i = 0;
            while (p.lifetime > smokeMaxLife[i])
            {
                ++i;
            }
            Debug.Assert(i < kNumSmokes);

            smoke[i].AddParticle(ref p);

        }   // end of AddSmokeParticle()

        public static void AddDistortedSmokeParticle(ref SharedSmokeEmitter.SmokeParticle p)
        {
            distortedSmoke.AddParticle(ref p);
        }   // end of AddDistortedSmokeParticle()

        /// <summary>
        /// Removes all particles from all shared emitters.
        /// </summary>
        public override void FlushAllParticles()
        {
            for (int i = 0; i < kNumSmokes; i++)
            {
                smoke[i].FlushAllParticles();
            }
            distortedSmoke.FlushAllParticles();
            beam.FlushAllParticles();
            bleep.FlushAllParticles();
            spark.FlushAllParticles();
            splash.FlushAllParticles();
        }   // end of SharedEmitterManager FlushAllParticles()

        public override void Update()
        {
            for (int i = 0; i < kNumSmokes; i++)
            {
                smoke[i].Update();
            }
            distortedSmoke.Update();
            beam.Update();
            bleep.Update();
            spark.Update();
            splash.Update();

        }   // end of Update()

        public override void Render(Camera camera)
        {
            if (InGame.inGame.renderEffects == InGame.RenderEffect.DistortionPass)
            {
                distortedSmoke.PreRender(camera);
                distortedSmoke.Render(camera);
                distortedSmoke.PostRender();
            }
            else
            {
                smoke[0].PreRender(camera);
                for (int i = 0; i < kNumSmokes; i++)
                {
                    smoke[i].Render(camera);
                }
                smoke[0].PostRender();

                beam.PreRender(camera);
                beam.Render(camera);
                beam.PostRender();                

                bleep.PreRender(camera);
                bleep.Render(camera);
                bleep.PostRender();

                spark.PreRender(camera);
                spark.Render(camera);
                spark.PostRender();

                splash.PreRender(camera);
                splash.Render(camera);
                splash.PostRender();
            }

        }   // end of Render()

        #endregion

        #region Internal

        public static void LoadContent(bool immediate)
        {
            for (int i = 0; i < kNumSmokes; i++)
            {
                BokuGame.Load(smoke[i], immediate);
            }
            BokuGame.Load(distortedSmoke, immediate);
            BokuGame.Load(beam, immediate);
            BokuGame.Load(bleep, immediate);
            BokuGame.Load(spark, immediate);
            BokuGame.Load(splash, immediate);

        }   // end of LoadContent()

        public static void InitDeviceResources(GraphicsDevice device)
        {
        }

        public static void UnloadContent()
        {
            for (int i = 0; i < kNumSmokes; i++)
            {
                BokuGame.Unload(smoke[i]);
            }
            BokuGame.Unload(distortedSmoke);
            BokuGame.Unload(beam);
            BokuGame.Unload(bleep);
            BokuGame.Unload(spark);
            BokuGame.Unload(splash);
        }   // end of UnloadContent()

        public static void DeviceReset(GraphicsDevice device)
        {
        }

        #endregion

    }   // end of class SharedEmitterManager

}   // end of namespace Boku.Common.ParticleSystem
