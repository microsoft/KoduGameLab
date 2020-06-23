
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
    /// Emitter class for ultra-simple, one-shot, spark effects that just care about
    /// rendering simple sprites with a fixed lifetime.  Assumes that all particles are
    /// emitted at creation time and when the particles die the emitter goes away.
    /// </summary>
    public class SparkEmitter : BaseEmitter
    {

        //
        //
        //  SparkEmitter
        //
        //

        private float maxAge = 0.6f;        // How long a particle lasts.  Constant for all sparks.
        private Vector3 gravity = new Vector3(0.0f, 0.0f, -3.0f);   // Damped version of gravity.

        #region Accessors
        #endregion

        // c'tor
        public SparkEmitter(ParticleSystemManager manager)
            : base(manager)
        {
        }   // end of c'tor

        /// <summary>
        /// Create a new burst of sparks.
        /// </summary>
        /// <param name="numSparks">How many to create.</param>
        /// <param name="origin">Where the sparks should originate from.</param>
        /// <param name="minRadius">Starting size.</param>
        /// <param name="maxRadius">Ending size.</param>
        /// <param name="speed">Initial speed of particles.  If this is bigger, a more spread burst is created.</param>
        /// <param name="color">Color used to tint the particles.  Generaly this should just be Vector4.One.</param>
        public void AddSparks(int numSparks, Vector3 origin, float minRadius, float maxRadius, float speed, Vector4 color)
        {
            Random rnd = BokuGame.bokuGame.rnd;

            for (int i = 0; i < numSparks; i++)
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
                party.flash = Vector3.Zero;
                party.color = color;

                SharedEmitterManager.Sparks.AddParticle(ref party);
            }
        }   // end of SparkEmitter AddSparks()

        /// <summary>
        /// Add in some explicit sparks.
        /// </summary>
        /// <param name="numSparks">How many</param>
        /// <param name="sparks">List of positions</param>
        /// <param name="vels">List of velocities (including speed)</param>
        /// <param name="minRadius">Starting size</param>
        /// <param name="maxRadius">Ending size</param>
        public void AddSparks(int numSparks, Vector3[] pos, Vector3[] vel, float minRadius, float maxRadius)
        {
            for (int i = 0; i < numSparks; i++)
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

                SharedEmitterManager.Sparks.AddParticle(ref party);
            }
        }

    }   // end of class SparkEmitter

}   // end of namespace Boku.Common
