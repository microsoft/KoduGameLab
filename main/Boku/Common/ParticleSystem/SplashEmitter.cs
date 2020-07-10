// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.


using System;
using System.Collections;
using System.Diagnostics;
using System.IO;
using System.Xml.Serialization;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Storage;

using Boku.Fx;

namespace Boku.Common.ParticleSystem
{
    /// <summary>
    /// Emitter class for ultra-simple, one-shot, splash effects that just care about
    /// rendering simple sprites with a fixed lifetime.  Assumes that all particles are
    /// emitted at creation time and when the particles die the emitter goes away.
    /// </summary>
    public class SplashEmitter : BaseEmitter
    {

        //
        //
        //  SplashEmitter
        //
        //

        private float maxAge = 1.5f;        // How long a particle lasts.  Constant for all Splashes.
        private Vector3 gravity = new Vector3(0.0f, 0.0f, -9.0f);   // Damped version of gravity.

        #region Accessors
        #endregion

        // c'tor
        public SplashEmitter(ParticleSystemManager manager)
            : base(manager)
        {
        }   // end of c'tor

        /// <summary>
        /// Create a new burst of Splashes.
        /// </summary>
        /// <param name="numSplashes">How many to create.</param>
        /// <param name="origin">Where the Splashes should originate from.</param>
        /// <param name="minRadius">Starting size.</param>
        /// <param name="maxRadius">Ending size.</param>
        /// <param name="speed">Initial speed of particles.  If this is bigger, a more spread burst is created.</param>
        /// <param name="color">Color used to tint the particles.  Generaly this should just be Vector4.One.</param>
        public void AddSplashes(int numSplashes, Vector3 origin, float minRadius, float maxRadius, float speed, Vector4 color)
        {
            Random rnd = BokuGame.bokuGame.rnd;

            for (int i = 0; i < numSplashes; i++)
            {
                Vector3 velocity = new Vector3((float)rnd.NextDouble() - (float)rnd.NextDouble(), (float)rnd.NextDouble() - (float)rnd.NextDouble(), (float)rnd.NextDouble() - (float)rnd.NextDouble());
                velocity *= speed;

                SharedSmokeEmitter.SmokeParticle party = new SharedSmokeEmitter.SmokeParticle();
                party.position = origin;
                party.velocity = velocity;
                party.acceleration = gravity;
                party.startRadius = minRadius;
                party.endRadius = maxRadius;
                party.rotationRate = 0.0f;
                party.lifetime = maxAge;
                party.color = color;

                SharedEmitterManager.Splashes.AddParticle(ref party);
            }
        }   

        /// <summary>
        /// Add in some explicit splashes
        /// </summary>
        /// <param name="numSplashes">how many</param>
        /// <param name="pos">list of where</param>
        /// <param name="vel">list of speeds</param>
        /// <param name="minRadius">starting size</param>
        /// <param name="maxRadius">ending size</param>
        public void AddSplashes(int numSplashes, Vector3[] pos, Vector3[] vel, float minRadius, float maxRadius)
        {
            for (int i = 0; i < numSplashes; i++)
            {

                Vector4 color = Vector4.One;

                SharedSmokeEmitter.SmokeParticle party = new SharedSmokeEmitter.SmokeParticle();
                party.position = pos[i];
                party.velocity = vel[i];
                party.acceleration = gravity;
                party.startRadius = minRadius;
                party.endRadius = maxRadius;
                party.rotationRate = 0.0f;
                party.lifetime = maxAge;
                party.color = color;

                SharedEmitterManager.Splashes.AddParticle(ref party);
            }
        }

        public void AddSplashes(int numSplashes, Vector3 pos, Vector3 vel, float radius, Vector4 tint)
        {
            Vector3 ax0 = new Vector3(-vel.Y, vel.X, 0.0f);
            float lenSq = ax0.LengthSquared();
            if (lenSq > 0)
            {
                ax0 *= (float)(1.0 / Math.Sqrt(lenSq));
            }
            else
            {
                ax0 = Vector3.UnitY;
            }
            Vector3 ax1 = Vector3.Normalize(Vector3.Cross(vel, ax0));
            float speed = vel.Length();
            Vector3 ax2 = vel / speed;

            Random rnd = BokuGame.bokuGame.rnd;
            for(int i = 0; i < numSplashes; ++i)
            {
                SharedSmokeEmitter.SmokeParticle party = new SharedSmokeEmitter.SmokeParticle();

                float rnd0 = (float)(rnd.NextDouble() * 2.0 - 1.0);
                float rnd1 = (float)(rnd.NextDouble() * 2.0 - 1.0);
                float rnd2 = (float)(rnd.NextDouble() * 2.0 - 1.0) * 0.2f + 1.0f;

                Vector3 deviate = (ax0 * rnd0 + ax1 * rnd1);

                Vector3 pPos = pos + deviate * radius * 0.2f;

                Vector3 pVel = vel * rnd2 + deviate * speed * 0.2f;

                party.position = pPos;
                party.velocity = pVel;
                party.acceleration = gravity;
                party.startRadius = radius * 0.2f;
                party.endRadius = radius;
                party.rotationRate = 0.0f;
                party.lifetime = maxAge;
                party.color = tint;

                SharedEmitterManager.Splashes.AddParticle(ref party);
            }
        }

    }   // end of class SplashEmitter

}   // end of namespace Boku.Common
