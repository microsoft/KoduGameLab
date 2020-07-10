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
    // Emits a Smoke as a series of expanding, concentric shells which fade as they grow.
    public class SmokeEmitter3D : BaseEmitter
    {

        public class SmokeParticle
        {
            public Vector3 position;
            public float age;
            public float lifetime;
            public float radius;
            public float alpha;
            public Color color;

            // c'tor
            public SmokeParticle(Vector3 position, float lifetime, Color color)
            {
                this.position = position;
                this.age = 0.0f;
                this.lifetime = lifetime;
                this.color = color;
            }   // end of c'tor

        }   // end of class SmokeParticle

        //
        //
        //  SmokeEmitter3D
        //
        //

        private Vector3 velocity;
        private Color color = Color.Gray;
        private float startRadius = 0.1f;
        private float endRadius = 2.0f;
        private float startAlpha = 0.5f;
        private float endAlpha = 0.0f;
        private float minLifetime = 2.0f;       // Particle lifetime.
        private float maxLifetime = 3.0f;
        private float emissionRate = 100.0f;    // Particles per second.

        #region Accessors
        public Vector3 Velocity
        {
            get { return velocity; }
            set { velocity = value; }
        }
        public Color Color
        {
            get { return color; }
            set { color = value; }
        }
        public float StartRadius
        {
            get { return startRadius; }
            set { startRadius = value; }
        }
        public float EndRadius
        {
            get { return endRadius; }
            set { endRadius = value; }
        }
        public float StartAlpha
        {
            get { return startAlpha; }
            set { startAlpha = value; }
        }
        public float EndAlpha
        {
            get { return endAlpha; }
            set { endAlpha = value; }
        }
        /// <summary>
        /// How long the particles live.
        /// </summary>
        public float MinLifetime
        {
            get { return minLifetime; }
            set { minLifetime = value; }
        }
        public float MaxLifetime
        {
            get { return maxLifetime; }
            set { maxLifetime = value; }
        }
        /// <summary>
        /// How many particles per second are created.
        /// </summary>
        public float EmissionRate
        {
            get { return emissionRate; }
            set { emissionRate = value; }
        }
        public bool ListIsEmpty
        {
            get { return particleList.Count == 0; }
        }
        #endregion

        // c'tor
        public SmokeEmitter3D(ParticleSystemManager manager)
            : base(manager)
        {
        }   // end of c'tor

        private float partial = 0.0f;   // Number of particles that need to be emitted.
        private Random rnd = new Random();

        public override void Update()
        {
            if (active)
            {
                // Emit new particles if needed.
                float dt = Time.GameTimeFrameSeconds;
                dt = MathHelper.Clamp(dt, 0.0f, 1.0f);  // Limit to reasonable values.

                if (emitting)
                {
                    partial += dt * emissionRate;

                    // Emit as many particles as needed this 
                    // frame to keep up with the emission rate.
                    while (partial >= 1.0f)
                    {
                        // Pick a random position somewhere along the path covered this frame.
                        Vector3 pos = position + (float)rnd.NextDouble() * dt * velocity;
                        float lifetime = minLifetime + (float)rnd.NextDouble() * (maxLifetime - minLifetime);
                        SmokeParticle particle = new SmokeParticle(pos, lifetime, color);
                        particleList.Add(particle);

                        partial -= 1.0f;
                    }
                }

                // Update any existing particles.  For more heavyweight particles we could
                // have an Update call per particle.  These are lightweight enough that we
                // can just update them directly.
                for (int i = 0; i < particleList.Count; )
                {
                    SmokeParticle particle = (SmokeParticle)particleList[i];

                    particle.age += dt;

                    Debug.Assert(particle.age >= 0.0f);

                    float t = particle.age / particle.lifetime;
                    if (t > 1.0f)
                    {
                        // Dead particle.
                        particleList.RemoveAt(i);
                    }
                    else
                    {
                        particle.radius = MyMath.Lerp(startRadius, endRadius, t) * scale;
                        particle.alpha = MyMath.Lerp(startAlpha, endAlpha, t) * scale;
                        particle.alpha *= particle.alpha;

                        i++;
                    }
                }

            }   // end of if active
        }   // end of SmokeEmitter3D Update()

        public override void Render(Camera camera)
        {
            if (active)
            {
                Sphere sphere = Sphere.GetInstance();

                // Get the effect we need.
                Effect effect = manager.Effect3d;

                // Set up common rendering values.
                effect.CurrentTechnique = manager.Technique(ParticleSystemManager.EffectTech3d.PremultAlphaGlowColorPass);

                manager.Parameter(ParticleSystemManager.EffectParams3d.DiffuseColor).SetValue(color.ToVector4());

                // Set up world matrix.
                Matrix worldMatrix = Matrix.Identity;

                manager.Parameter(ParticleSystemManager.EffectParams3d.GlowFactor).SetValue(0.5f);

                for (int i = 0; i < particleList.Count; i++)
                {
                    SmokeParticle particle = (SmokeParticle)particleList[i];

                    // Set translation and radius.
                    worldMatrix.Translation = particle.position;
                    Matrix worldViewProjMatrix = worldMatrix * camera.ViewProjectionMatrix;

                    manager.Parameter(ParticleSystemManager.EffectParams3d.Radius).SetValue(particle.radius);

                    // Set alpha.
                    manager.Parameter(ParticleSystemManager.EffectParams3d.Alpha).SetValue(particle.alpha);

                    // Set color
                    manager.Parameter(ParticleSystemManager.EffectParams3d.DiffuseColor).SetValue(particle.color.ToVector4());

                    sphere.Render(camera, ref worldMatrix, effect);

                }
            }   // end of if active

        }   // end of Emitter Render()

    }   // end of class SmokeEmitter3D

}   // end of namespace Boku.Common
