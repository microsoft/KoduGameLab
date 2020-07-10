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

namespace Boku.Common.ParticleSystem
{
    // Emits "emitter puffs" as a series of expanding, rotating 
    // sprites which fade and grow as they age.  These are to
    // be used when an object is being dragged or when it 
    // bounces on the ground.
    public class DustEmitter : BaseEmitter
    {
        #region Members
        private float emissionRate = 20.0f;
        private Vector3 velocity;               // Velocity of emitter.
        private Vector4 color = new Vector4(0.72f, 0.7f, 0.55f, 1.0f);
        private float startRadius = 0.1f;
        private float endRadius = 5.0f;
        private float startAlpha = 0.4f;
        private float endAlpha = 0.0f;
        private float minLifetime = 0.5f;
        private float maxLifetime = 3.0f;
        private float maxRotationRate = 2.0f;
        protected float positionJitter = 0.0f;    // Magnitude of random offset applied to new particles from the emitter's position.

        private bool linearEmission = false;
        #endregion Members

        #region Accessors
        /// <summary>
        /// How many particles per-second or per-meter are created depending on the value of LinearEmission.
        /// </summary>
        public float EmissionRate
        {
            get { return emissionRate; }
            set { emissionRate = value; }
        }
        /// <summary>
        /// This is the velocity applied to the emitters source.  This
        /// is in addition to any other movement that may be caused by
        /// the emitters position being changed.
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
        /// Radius for particles at the beginning of their Lifetime.
        /// </summary>
        public float StartRadius
        {
            get { return startRadius; }
            set { startRadius = value; }
        }
        /// <summary>
        /// Radius for particles at the end of their Lifetime.
        /// </summary>
        public float EndRadius
        {
            get { return endRadius; }
            set { endRadius = value; }
        }
        /// <summary>
        /// Alpha value for particles at the beginning of their Lifetime.
        /// </summary>
        public float StartAlpha
        {
            get { return startAlpha; }
            set { startAlpha = value; }
        }
        /// <summary>
        /// Alpha value for particles at the end of their Lifetime.
        /// </summary>
        public float EndAlpha
        {
            get { return endAlpha; }
            set { endAlpha = value; }
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
        public float MaxRotationRate
        {
            get { return maxRotationRate; }
            set { maxRotationRate = value; }
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
        /// If true, emission is per meter.  If false it's per second.
        /// </summary>
        public bool LinearEmission
        {
            get { return linearEmission; }
            set { linearEmission = value; }
        }
        #endregion

        #region Public
        // c'tor
        public DustEmitter(ParticleSystemManager manager)
            : base(manager)
        {
        }   // end of c'tor

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

                    partial += dist * EmissionRate / Scale;
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
                    float lifetime = MinLifetime + (float)rnd.NextDouble() * (MaxLifetime - MinLifetime);

                    // Fill in the particle data.
                    particle.position = pos;
                    particle.velocity = velocity;
                    particle.acceleration = Vector3.Zero;
                    particle.color = Color;
                    particle.startRadius = StartRadius * Scale;
                    particle.endRadius = EndRadius * Scale;
                    float rotationRate = MaxRotationRate * (float)(rnd.NextDouble() - rnd.NextDouble());
                    particle.rotationRate = rotationRate;
                    particle.lifetime = lifetime;
                    particle.flash = Vector3.Zero;

                    if ((usage & Use.Regular) != 0)
                    {
                        SharedEmitterManager.AddSmokeParticle(ref particle);
                    }

                    partial -= 1.0f;
                }
            }

            PreviousPosition = position;

        }   // end of Update()


        #endregion Public

    }   // end of class DustEmitter

}   // end of namespace Boku.Common
