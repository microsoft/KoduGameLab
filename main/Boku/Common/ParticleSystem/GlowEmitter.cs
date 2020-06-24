
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

namespace Boku.Common.ParticleSystem
{
    // Emits a glow as a series of expanding, concentric shells which fade as they grow.
    public class GlowEmitter : BaseEmitter
    {

        public class GlowParticle
        {
            public float age;
            public float radius;
            public float alpha;
            public Vector4 color;

            // c'tor
            public GlowParticle()
            {
                this.age = 0.0f;
            }   // end of c'tor

        }   // end of class GlowParticle

        //
        //
        //  GlowEmitter
        //
        //

        private bool showSolidCore = false;
        private Vector4 color = new Vector4(1.0f, 0.0f, 0.0f, 1.0f);
        private float startRadius = 0.1f;
        private float endRadius = 0.8f;
        private float startAlpha = 1.0f;
        private float endAlpha = 0.0f;
        private float lifetime = 2.5f;      // Particle lifetime.
        private float emissionRate = 2.0f;  // Particles per second.

        private static List<GlowParticle> unused = new List<GlowParticle>();

        #region Accessors
        public Vector4 Color
        {
            get { return color; }
            set { color = value; }
        }
        /// <summary>
        /// Puts a solid core at the center with
        /// radius equal to the start radius.
        /// </summary>
        public bool ShowSolidCore
        {
            get { return showSolidCore; }
            set { showSolidCore = value; }
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
        public float Lifetime
        {
            get { return lifetime; }
            set { lifetime = value; }
        }
        /// <summary>
        /// How many particles per second are created.
        /// </summary>
        public float EmissionRate
        {
            get { return emissionRate; }
            set { emissionRate = value; }
        }
        #endregion

        // c'tor
        public GlowEmitter(ParticleSystemManager manager) 
            : base(manager)
        {
            const int numInitParticles = 5;
            for (int i = 0; i < numInitParticles; ++i)
            {
                unused.Add(new GlowParticle());
            }
        }   // end of c'tor

        private float partial = 0.0f;   // Number of particles that need to be emitted.

        public override void Update()
        {
            if (active)
            {
                // Emit new particles if needed.
                float dt = Time.GameTimeFrameSeconds;
                dt = MathHelper.Clamp(dt, 0.0f, 1.0f);  // Limit to reasonable values.

                partial += dt * emissionRate;

                // Note this is set up so that at most a single particle will be
                // emitted per Update call.  This works for the glow but may
                // not be right for other particle effects.
                if (partial >= 1.0f)
                {
                    GlowParticle particle = NewParticle(color);
                    particleList.Add(particle);

                    partial -= 1.0f;
                }

                // Update any existing particles.  For more heavyweight particles we could
                // have an Update call per particle.  These are lightweight enough that we
                // can just update them directly.
                for (int i = 0; i < particleList.Count; )
                {
                    GlowParticle particle = (GlowParticle)particleList[i];

                    particle.age += dt;

                    Debug.Assert(particle.age >= 0.0f);

                    float t = particle.age / lifetime;
                    if (t > 1.0f)
                    {
                        // Dead particle.
                        ReleaseParticle(i);
                    }
                    else
                    {
                        particle.radius = MyMath.Lerp(startRadius, endRadius, t) * scale;
                        particle.alpha = MyMath.Lerp(startAlpha, endAlpha, t);
                        particle.alpha *= particle.alpha;

                        i++;
                    }
                }

            }   // end of if active
        }   // end of GlowEmitter Update()

        public override void Render(Camera camera)
        {
            if (active)
            {
                GraphicsDevice device = KoiLibrary.GraphicsDevice;
                Sphere sphere = Sphere.GetInstance();

                // Get the effect we need.
                Effect effect = manager.Effect3d;

                // Set up common rendering values.
                effect.CurrentTechnique = manager.Technique(ParticleSystemManager.EffectTech3d.PremultAlphaGlowColorPass);

                // Set up world matrix.
                Matrix worldMatrix = Matrix.Identity;
                worldMatrix.Translation = position;

                device.Indices = sphere.Ibuf;
                device.SetVertexBuffer(sphere.Vbuf);

                if (showSolidCore)
                {
                    Matrix worldViewProjMatrix = worldMatrix * camera.ViewProjectionMatrix;

                    // Set radius.
                    manager.Parameter(ParticleSystemManager.EffectParams3d.Radius).SetValue(startRadius * scale);
                    manager.Parameter(ParticleSystemManager.EffectParams3d.WorldMatrix).SetValue(worldMatrix);
                    manager.Parameter(ParticleSystemManager.EffectParams3d.WorldViewProjMatrix).SetValue(worldViewProjMatrix);

                    // Set alpha.
                    manager.Parameter(ParticleSystemManager.EffectParams3d.Alpha).SetValue(1.0f);

                    // Set color.
                    manager.Parameter(ParticleSystemManager.EffectParams3d.DiffuseColor).SetValue(color);
                    manager.Parameter(ParticleSystemManager.EffectParams3d.EmissiveColor).SetValue(Vector4.Zero);

                    // Set glow factor.
                    manager.Parameter(ParticleSystemManager.EffectParams3d.GlowFactor).SetValue(0.1f);

                    effect.CurrentTechnique.Passes[0].Apply();

                    sphere.DrawPrim(effect);
                }

                // Set glow factor for shells.
                manager.Parameter(ParticleSystemManager.EffectParams3d.GlowFactor).SetValue(1.5f);

                for (int i = 0; i < particleList.Count; i++)
                {
                    GlowParticle particle = (GlowParticle)particleList[i];

                    Matrix worldViewProjMatrix = worldMatrix * camera.ViewProjectionMatrix;

                    // Set radius.
                    manager.Parameter(ParticleSystemManager.EffectParams3d.Radius).SetValue(particle.radius);
                    manager.Parameter(ParticleSystemManager.EffectParams3d.WorldMatrix).SetValue(worldMatrix);
                    manager.Parameter(ParticleSystemManager.EffectParams3d.WorldViewProjMatrix).SetValue(worldViewProjMatrix);

                    // Set alpha.
                    manager.Parameter(ParticleSystemManager.EffectParams3d.Alpha).SetValue(particle.alpha);

                    // Set color
                    manager.Parameter(ParticleSystemManager.EffectParams3d.DiffuseColor).SetValue(particle.color);
                    manager.Parameter(ParticleSystemManager.EffectParams3d.EmissiveColor).SetValue(Vector4.Zero);

                    effect.CurrentTechnique.Passes[0].Apply();

                    sphere.DrawPrim(effect);

                }

            }   // end of if active

        }   // end of Emitter Render()

        private GlowParticle NewParticle(Vector4 color)
        {
            GlowParticle part = null;
            if (unused.Count > 0)
            {
                part = unused[unused.Count - 1];
                unused.RemoveAt(unused.Count - 1);
            }
            else
            {
                part = new GlowParticle();
            }
            part.color = color;
            part.age = 0.0f;
            return part;
        }

        private void ReleaseParticle(int which)
        {
            unused.Add((GlowParticle)particleList[which]);
            particleList.RemoveAt(which);
        }


    }   // end of class GlowEmitter

}   // end of namespace Boku.Common
