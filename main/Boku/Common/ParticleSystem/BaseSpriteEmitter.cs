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

using Boku.Fx;

namespace Boku.Common.ParticleSystem
{
    /// <summary>
    /// Base emitter class for generic emitters that just care about
    /// rendering simple sprites.  Useful for smoke, dust, etc.
    /// </summary>
    public class BaseSpriteEmitter : BaseEmitter
    {

        public class BaseSpriteParticle
        {
            public Vector3 position;
            public Vector3 velocity;
            public float age;
            public float lifetime;
            public float rotation = 0.0f;
            public float deltaRotation;     // Radians per second.
            public float radius;
            public float alpha;
            public float tile;
            public Vector4 color;

            // c'tor
            public BaseSpriteParticle()
            {
            }
            public BaseSpriteParticle Set(Vector3 position, Vector3 velocity, float lifetime, Vector4 color, float maxRotationRate, int numTiles)
            {
                this.position = position;
                this.age = 0.0f;
                this.lifetime = lifetime;
                this.color = color;
                this.tile = (float)((int)(rnd.NextDouble() * (numTiles - 0.1f)));
                this.rotation = 0.0f;

                if (maxRotationRate != 0.0f)
                {
                    this.rotation = (float)(BaseSpriteEmitter.rnd.NextDouble() * MathHelper.TwoPi);
                }
                this.deltaRotation = (float)(-maxRotationRate + BaseSpriteEmitter.rnd.NextDouble() * 2.0f * maxRotationRate);

                this.velocity = velocity;
                this.radius = 0.0f;
                this.alpha = 0.0f;

                return this;
            }   // end of c'tor

        }   // end of class BaseSpriteParticle

        //
        //
        //  BaseSpriteEmitter
        //
        //

        #region Rendering Internals

        // Local Vertex Array.
        static Vertex[] localVerts = new Vertex[4 * maxSprites];

        // Local Index Buffer.
        static Int16[] localIndices = null; // Allocate in Init()

        public struct Vertex : IVertexType
        {
            public Vector3 position;
            public Vector3 texCoord;
            public Vector3 state;

            static VertexDeclaration decl = null;
            static VertexElement[] elements = new VertexElement[]
            {
                new VertexElement(0, VertexElementFormat.Vector3, VertexElementUsage.Position, 0),              // position
                new VertexElement(12, VertexElementFormat.Vector3, VertexElementUsage.TextureCoordinate, 0),    // texture UVs
                new VertexElement(24, VertexElementFormat.Vector3, VertexElementUsage.TextureCoordinate, 1),    // rotation, radius, alpha
                // size == 36 (oops - ***)
            };

            public Vertex(Vector3 pos, Vector3 tex, float rotation, float radius, float alpha)
            {
                position = pos;
                texCoord = tex;
                state = Vector3.Zero;
                state.X = rotation;
                state.Y = radius;
                state.Z = alpha;
            }   // end of Vertex c'tor

            public VertexDeclaration VertexDeclaration
            {
                get
                {
                    if (decl == null || decl.IsDisposed)
                    {
                        decl = new VertexDeclaration(elements);
                    }
                    return decl;
                }
            }

        }   // end of Vertex

        #endregion Rendering Internals

        protected int numTiles = 1;
        protected const int maxSprites = 1000;
        protected float positionJitter = 0.0f;    // Magnitude of random offset applied to new particles from the emitter's position.
        protected Vector3 velocity;               // Velocity of emitter.
        protected Vector4 color = new Vector4(1.0f, 1.0f, 1.0f, 1.0f);
        protected float startRadius = 0.9f;      
        protected float endRadius = 5.0f;
        protected float startAlpha = 0.4f;
        protected float endAlpha = 0.0f;
        protected float minLifetime = 0.5f;       // Particle lifetime.
        protected float maxLifetime = 3.0f;
        protected bool linearEmission = false;    // Should the particles be distributed per-meter rather than per-second?
        protected float emissionRate = 50.0f;     // Particles per second or per meter depending on linearEmission.

        protected Vector3 gravity;                // Force applied to all particles, not the emitter.
        protected Vector3 initParticleVelocity;   // Initial velocity applied to all particles, not emitter.
        protected bool nonZeroGravity = false;    // Is the above value non zero.
        protected float maxSpeed = 0.0f;          // Max speed for particles.
        protected float maxRotationRate = 0.0f;   // Particle rotation.

        protected float explicitBloom = 0.0f;

        private static List<BaseSpriteParticle> unused = new List<BaseSpriteParticle>(maxSprites);

        #region Accessors
        /// <summary>
        /// Does this system allow use of explicit bloom?
        /// </summary>
        public float ExplicitBloom
        {
            get { return explicitBloom; }
            protected set { explicitBloom = value; }
        }
        /// <summary>
        /// If true, emission is per meter.  If false it's per second.
        /// </summary>
        public bool LinearEmission
        {
            get { return linearEmission; }
            set { linearEmission = value; }
        }
        /// <summary>
        /// Max number of particles at once.
        /// </summary>
        protected int MaxSprites
        {
            get { return maxSprites; }
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
        /// </summary>
        public float StartAlpha
        {
            get { return startAlpha; }
            set { startAlpha = value; }
        }
        // Alpha value for particles at the end of their lifetime.
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
        /// <summary>
        /// How many particles per-second or per-meter are created depending on the value of LinearEmission.
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
        public EffectTechnique Technique
        {
            get {
                return (InGame.inGame.renderEffects < InGame.RenderEffect.Normal)
                    ? manager.Technique2d((int)InGame.inGame.renderEffects)
                    : manager.Technique(TechniqueName);
            }
        }
        public virtual ParticleSystemManager.EffectTech2d TechniqueName
        {
            get { return ParticleSystemManager.EffectTech2d.TexturedColorPassNormalAlpha; }
        }

        /// <summary>
        /// Override this to true to avoid the lighting effecting your color. Like with a dark
        /// light rig you don't want your explosions in black.
        /// </summary>
        public virtual bool IsEmissive
        {
            get { return false; }
        }

        protected virtual Texture2D Texture
        {
            get { return null; }
            set { }
        }

        /// <summary>
        /// Force applied to particles, not the emitter.
        /// </summary>
        public Vector3 Gravity
        {
            get { return gravity; }
            set { gravity = value; nonZeroGravity = gravity != Vector3.Zero;  }
        }

        /// <summary>
        /// Initial velocity applied to particles, not the emitter.
        /// </summary>
        public Vector3 InitParticleVelocity
        {
            get { return initParticleVelocity; }
            set { initParticleVelocity = value; }
        }

        protected bool NonZeroGravity
        {
            get { return nonZeroGravity; }
        }
        /// <summary>
        /// Max speed for particles, not the emitter.
        /// </summary>
        public float MaxSpeed
        {
            get { return maxSpeed; }
            set { maxSpeed = value; }
        }
        /// <summary>
        /// Max rotation rate for particles.
        /// </summary>
        public float MaxRotationRate
        {
            get { return maxRotationRate; }
            set { maxRotationRate = value; }
        }
        /// <summary>
        /// Number of images available on the texture
        /// </summary>
        public int NumTiles
        {
            get { return numTiles; }
            set { numTiles = value; }
        }
        #endregion

        // c'tor
        public BaseSpriteEmitter(ParticleSystemManager manager)
            : base(manager)
        {
        }   // end of c'tor

        /// <summary>
        /// Immediate hard stop on all particles
        /// </summary>
        public override void FlushAllParticles()
        {
            if (unused.Capacity < unused.Count + particleList.Count)
            {
                unused.Capacity = unused.Count + particleList.Count;
            }
            for (int i = 0; i < particleList.Count; ++i)
            {
                unused.Add((BaseSpriteParticle)particleList[i]);
            }
            particleList.Clear();
        }


        private static void Init()
        {
            localIndices = new Int16[6 * maxSprites];

            // Pre-fill UV coords for local vertices and set index buffer.
            int v = 0;
            int i = 0;
            for (int s = 0; s < maxSprites; s++)
            {
                localIndices[i++] = (Int16)(v + 0);
                localIndices[i++] = (Int16)(v + 1);
                localIndices[i++] = (Int16)(v + 2);
                localIndices[i++] = (Int16)(v + 0);
                localIndices[i++] = (Int16)(v + 2);
                localIndices[i++] = (Int16)(v + 3);

                localVerts[v++] = new Vertex(new Vector3(), new Vector3(0, 0, 0), 0.0f, 1.0f, 1.0f);
                localVerts[v++] = new Vertex(new Vector3(), new Vector3(0, 1, 0), 0.0f, 1.0f, 1.0f);
                localVerts[v++] = new Vertex(new Vector3(), new Vector3(1, 1, 0), 0.0f, 1.0f, 1.0f);
                localVerts[v++] = new Vertex(new Vector3(), new Vector3(1, 0, 0), 0.0f, 1.0f, 1.0f);
            }

        }   // end of BaseSpriteEmitter Init()

        protected float partial = 0.0f;   // Number of particles that need to be emitted.
        static protected Random rnd = new Random();

        protected virtual void Emit(float dt)
        {
            if (emitting && manager.AllowEmission())
            {
                // Adjust emission rate according to active particle count
                float adjustedEmissionRate = manager.AdjustEmissionRate(emissionRate);
                if (LinearEmission)
                {
                    Vector3 deltaPosition = position - PreviousPosition;
                    float dist = deltaPosition.Length();

                    partial += dist * adjustedEmissionRate;
                }
                else
                {
                    partial += dt * adjustedEmissionRate;
                }

                // Emit as many particles as needed this 
                // frame to keep up with the emission rate.
                while (partial >= 1.0f)
                {
                    if (particleList.Count < maxSprites)
                    {
                        // Pick a random position somewhere along the path covered this frame.
                        Vector3 pos = position - (position - PreviousPosition) * (float)rnd.NextDouble();
                        if (PositionJitter > 0.0f)
                        {
                            pos += Scale * PositionJitter * new Vector3((float)rnd.NextDouble() - (float)rnd.NextDouble(), (float)rnd.NextDouble() - (float)rnd.NextDouble(), (float)rnd.NextDouble() - (float)rnd.NextDouble());
                        }
                        // adjust lifetime according to active particle count
                        Vector2 adjLifeTime = manager.AdjustLifetime(minLifetime, maxLifetime);
                        float lifetime = adjLifeTime.X + (float)rnd.NextDouble() * (adjLifeTime.Y - adjLifeTime.X);
                        BaseSpriteParticle particle = NewParticle(pos, initParticleVelocity, lifetime, color, maxRotationRate, NumTiles);
                        particleList.Add(particle);
                    }

                    partial -= 1.0f;
                }
            }
            PreviousPosition = position;
        }

        public virtual void UpdateParticles(float dt)
        {
            for (int i = 0; i < particleList.Count; )
            {
                BaseSpriteParticle particle = (BaseSpriteParticle)particleList[i];

                particle.age += dt;

                Debug.Assert(particle.age >= 0.0f);

                float t = particle.age / particle.lifetime;
                if (t > 1.0f)
                {
                    // Dead particle.
                    ReleaseParticle(i);
                }
                else
                {
                    particle.radius = MyMath.Lerp(startRadius, endRadius, t) * scale;
                    particle.alpha = MyMath.Lerp(startAlpha, endAlpha, t);
                    particle.rotation += particle.deltaRotation * dt;

                    // Change the linear fade to a curve.
                    particle.alpha *= particle.alpha;

                    // Add in gravity effect.
                    if (NonZeroGravity)
                    {
                        particle.velocity += Gravity * dt;
                        float speed2 = particle.velocity.LengthSquared();
                        if (speed2 > MaxSpeed * MaxSpeed)
                        {
                            particle.velocity.Normalize();
                            particle.velocity *= MaxSpeed;
                        }
                    }
                    particle.position += particle.velocity * dt;

                    i++;
                }

            }   // end loop update particles.
        }

        public override void Update()
        {
            if (active)
            {
                // Ensure index buffer it created.
                if (localIndices == null)
                {
                    Init();
                }

                // Emit new particles if needed.
                float dt = Time.GameTimeFrameSeconds;
                dt = MathHelper.Clamp(dt, 0.0f, 1.0f);  // Limit to reasonable values.

                position += velocity * dt;

                // Spawn any new little sprouts
                Emit(dt);

                // Update any existing particles.  For more heavyweight particles we could
                // have an Update call per particle.  These are lightweight enough that we
                // can just update them directly.
                UpdateParticles(dt);
                
                // See if we've died.
                if (Dying && particleList.Count == 0)
                {
                    Active = false;
                    RemoveFromManager();
                }

                // Update the previous position to match the current one for the next frame.
                ResetPreviousPosition();

            }   // end of if active
        }   // end of BaseSpriteEmitter Update()

        private void UpdateVerts()
        {
            for (int i = 0; i < particleList.Count; i++)
            {
                BaseSpriteParticle particle = (BaseSpriteParticle)particleList[particleList.Count - i - 1];

                Vector3 newState = new Vector3(particle.rotation, particle.radius, particle.alpha);

                for (int j = 0; j < 4; j++)
                {
                    localVerts[i * 4 + j].position = particle.position;
                    localVerts[i * 4 + j].texCoord.Z = particle.tile;
                    localVerts[i * 4 + j].state = newState;
                }
            }
        }

        public override void Render(Camera camera)
        {
            if (active && particleList.Count > 0 && (! Texture.IsDisposed))
            {
                // We're getting rendered, set up the shared vertex buffer to suit us.
                UpdateVerts();

                GraphicsDevice device = KoiLibrary.GraphicsDevice;


                // Get the effect we need.
                Effect effect = manager.Effect2d;

                // Set up common rendering values.
                effect.CurrentTechnique = Technique;

                Vector4 diffuseColor = ShaderGlobals.ParticleTint(IsEmissive);
                diffuseColor *= Color;
                manager.Parameter(ParticleSystemManager.EffectParams2d.DiffuseColor).SetValue(diffuseColor);

                // Set up world matrix.
                Matrix worldMatrix = Matrix.Identity;
                Matrix worldViewProjMatrix = worldMatrix * camera.ViewProjectionMatrix;

                manager.Parameter(ParticleSystemManager.EffectParams2d.DiffuseTexture).SetValue(Texture);

                manager.Parameter(ParticleSystemManager.EffectParams2d.EyeLocation).SetValue(new Vector4(camera.ActualFrom, 1.0f));
                manager.Parameter(ParticleSystemManager.EffectParams2d.CameraUp).SetValue(new Vector4(camera.ViewUp, 1.0f));

                manager.Parameter(ParticleSystemManager.EffectParams2d.WorldMatrix).SetValue(worldMatrix);
                manager.Parameter(ParticleSystemManager.EffectParams2d.WorldViewProjMatrix).SetValue(worldViewProjMatrix);
                manager.Parameter(ParticleSystemManager.EffectParams2d.TileOffset).SetValue(NumTiles > 0 ? 1.0f / (float)NumTiles : 1.0f);
                manager.Parameter(ParticleSystemManager.EffectParams2d.ParticleRadius).SetValue(
                    ShaderGlobals.MakeParticleSizeLimit(1.0f, 0.0f, 100.0f * endRadius));

                ShaderGlobals.FixExplicitBloom(ExplicitBloom);

                // Render all passes.
                for (int i = 0; i < effect.CurrentTechnique.Passes.Count; i++)
                {
                    EffectPass pass = effect.CurrentTechnique.Passes[i];
                    pass.Apply();
                    device.DrawUserIndexedPrimitives(PrimitiveType.TriangleList, localVerts, 0, particleList.Count * 4, localIndices, 0, particleList.Count * 2);
                }

                ShaderGlobals.ReleaseExplicitBloom();

            }   // end of if active

        }   // end of Emitter Render()


        public static void LoadContent(bool immediate)
        {
        }   // end of BaseSpriteEmitter LoadContent()

        public static void InitDeviceResources(GraphicsDevice device)
        {
        }

        public static void UnloadContent()
        {
        }   // end of BaseSpriteEmitter UnloadContent()

        public static void DeviceReset(GraphicsDevice device)
        {
        }

        public override void RemoveFromManager()
        {
            base.RemoveFromManager();
        }

        protected void ReleaseParticle(int which)
        {
            unused.Add((BaseSpriteParticle)particleList[which]);
            particleList.RemoveAt(which);
        }

        protected BaseSpriteParticle NewParticle(
            Vector3 pos,
            Vector3 velocity,
            float lifeTime,
            Vector4 color,
            float maxRotation,
            int numTiles)
        {
            BaseSpriteParticle part = null;

            if (particleList.Count < MaxSprites)
            {
                if (unused.Count > 0)
                {
                    part = unused[unused.Count - 1];
                    unused.RemoveAt(unused.Count - 1);
                    part.Set(pos, velocity, lifeTime, color, maxRotation, numTiles);
                }
                else
                {
                    part = new BaseSpriteParticle();
                    part.Set(pos, velocity, lifeTime, color, maxRotation, numTiles);
                }
            }
            
            return part;
        }


    }   // end of class BaseSpriteEmitter

}   // end of namespace Boku.Common
