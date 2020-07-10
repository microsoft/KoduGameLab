// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.


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

using KoiX;

using Boku.Base;

namespace Boku.Common.ParticleSystem
{
    /// <summary>
    /// This is the class which is actually instantiated for each smoke source in the scene.
    /// Upon update, it will add new particles to the SharedSmokeEmitter.
    /// </summary>
    public class SharedSmokeSource : BaseEmitter
    {
        #region Members

        protected Vector3 velocity = Vector3.Zero;// Initial velocity of particle.
        protected Vector3 acceleration = Vector3.Zero;  // Acceleration applied to particle over lifetime.  May be used to act like gravity or cause smoke to rise.
        protected float positionJitter = 0.0f;    // Magnitude of random offset applied to new particles from the emitter's position.
        protected Vector4 color = new Vector4(1.0f, 1.0f, 1.0f, 1.0f);
        protected float startRadius = 0.9f;
        protected float endRadius = 5.0f;
        protected float startAlpha = 0.4f;
        protected float minLifetime = 0.5f;       // Particle lifetime.
        protected float maxLifetime = 3.0f;
        protected bool linearEmission = false;    // Should the particles be distributed per-meter rather than per-second?
        protected float emissionRate = 50.0f;     // Particles per second or per meter depending on linearEmission.
        protected Vector3 flashTime = Vector3.Zero; // when to start and how long to flash emissive after that
                                                    // and how long the tail glows

        protected float maxRotationRate = 0.0f;   // Particle rotation.

        #endregion

        #region Accessors
        
        /// <summary>
        /// If true, emission is per meter.  If false it's per second.
        /// </summary>
        public bool LinearEmission
        {
            get { return linearEmission; }
            set { linearEmission = value; }
        }
        
        /// <summary>
        /// Magnitude of random offset given to new particles.
        /// </summary>
        public float PositionJitter
        {
            get { return positionJitter; }
            set { positionJitter = value; }
        }

        /// <summary>
        /// This is the initial velocity of the particle.
        /// </summary>
        public Vector3 Velocity
        {
            get { return velocity; }
            set { velocity = value; }
        }
        
        /// <summary>
        /// Color used to attenuate particles when they're rendered.
        /// </summary>
        public Vector4 Color
        {
            get { return color; }
            set { color = value; }
        }
        
        /// <summary>
        /// Radius for particles at the beginning of their lifetime.
        /// </summary>
        public float StartRadius
        {
            get { return startRadius; }
            set { startRadius = value; }
        }
        
        /// <summary>
        /// Radius for particles at the end of their lifetime.
        /// </summary>
        public float EndRadius
        {
            get { return endRadius; }
            set { endRadius = value; }
        }

        /// <summary>
        /// Alpha value for particles at the beginning of their lifetime.
        /// Always goes to 0 at end of lifetime.
        /// </summary>
        public float StartAlpha
        {
            get { return startAlpha; }
            set { startAlpha = value; }
        }

        /// <summary>
        /// Minimum time the particles live.
        /// </summary>
        public float MinLifetime
        {
            get { return minLifetime; }
            set { minLifetime = value; }
        }
        
        /// <summary>
        /// Maximum  time the particles live.
        /// </summary>
        public float MaxLifetime
        {
            get { return maxLifetime; }
            set { maxLifetime = value; }
        }

        /// <summary>
        /// How many particles per-second or per-meter are created depending on the value of LinearEmission.
        /// </summary>
        public float EmissionRate
        {
            get { return emissionRate; }
            set { emissionRate = value; }
        }

        /// <summary>
        /// Acceleration applied to particle over lifetime.  May be used to act like gravity or cause smoke to rise.
        /// </summary>
        public Vector3 Acceleration
        {
            get { return acceleration; }
            set { acceleration = value; }
        }

        /// <summary>
        /// Max rate of rotation for particles in radians per second.
        /// </summary>
        public float MaxRotationRate
        {
            get { return maxRotationRate; }
            set { maxRotationRate = value; }
        }

        #endregion

        #region Public
        
        public SharedSmokeSource(ParticleSystemManager manager)
            : base(manager)
        {
        }   // end of c'tor

        /// <summary>
        /// Initialize an emissive burst from now (presumably the birth of the system)
        /// for a fixed length of time. Tail is how long (secs) the particles glow after
        /// coming out the tail (after particle birth)
        /// </summary>
        /// <param name="length"></param>
        public void InitFlash(float length, float tail)
        {
            flashTime.X = (float)Time.GameTimeTotalSeconds;
            flashTime.Y = length;
            flashTime.Z = tail;
        }

        protected float partial = 0.0f;   // Number of particles that need to be emitted.
        public override void Update()
        {
            // See if we've died.
            if (Dying)
            {
                // We don't need to wait for any particles to 
                // go away since the shared emitter owns them.
                Active = false;
                RemoveFromManager();
            }

            if (emitting)
            {

                float dt = Time.GameTimeFrameSeconds;
                Random rnd = BokuGame.bokuGame.rnd;

                if (LinearEmission)
                {
                    Vector3 deltaPosition = position - PreviousPosition;
                    float dist = deltaPosition.Length();

                    partial += dist * EmissionRate;
                }
                else
                {
                    partial += dt * EmissionRate;
                }

                // Emit as many particles as needed this 
                // frame to keep up with the emission rate.
                SharedSmokeEmitter.SmokeParticle particle = new SharedSmokeEmitter.SmokeParticle();
                while (partial >= 1.0f)
                {
                    // Pick a random position somewhere along the path covered this frame.
                    Vector3 pos = position - (position - PreviousPosition) * (float)rnd.NextDouble();
                    if (PositionJitter > 0.0f)
                    {
                        pos += PositionJitter * new Vector3((float)rnd.NextDouble() - (float)rnd.NextDouble(), (float)rnd.NextDouble() - (float)rnd.NextDouble(), (float)rnd.NextDouble() - (float)rnd.NextDouble());
                    }
                    float lifetime = minLifetime + (float)rnd.NextDouble() * (maxLifetime - minLifetime);

                    // Fill in the particle data.
                    particle.position = pos;
                    particle.velocity = velocity;
                    particle.acceleration = acceleration;
                    particle.color = color;
                    particle.startRadius = StartRadius * Scale;
                    particle.endRadius = EndRadius * Scale;
                    float rotationRate = maxRotationRate * (float)(rnd.NextDouble() - rnd.NextDouble());
                    particle.rotationRate = rotationRate;
                    particle.lifetime = lifetime;
                    particle.flash = flashTime;

                    if ((usage & Use.Regular) != 0)
                    {
                        SharedEmitterManager.AddSmokeParticle(ref particle);
                    }

                    if ((usage & Use.Distort) != 0)
                    {
                        SharedEmitterManager.AddDistortedSmokeParticle(ref particle);
                    }

                    partial -= 1.0f;
                }
            }

            PreviousPosition = position;

        }   // end of Update()

        public override int ParticleCount()
        {
            return 0;
        }

        #endregion

        #region Internal
        #endregion

    }   // end of class SharedSmokeSource


}   // end of namespace Boku.Common.ParticleSystem
