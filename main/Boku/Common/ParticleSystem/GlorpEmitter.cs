
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

namespace Boku.Common
{
    // Emits "Distortion Glorp" as a series of expanding, rotating 
    // sprites which fade and grow as they age.  The sprites
    // are rendered using the DistortionManager.
    public class GlorpEmitter : BaseSpriteEmitter
    {
        private static Texture2D texture = null;
        private float targetRadius = 1.0f;

        #region accessors
        protected override Texture2D Texture
        {
            get { return texture; }
        }
        public override ParticleSystemManager.EffectTech2d TechniqueName
        {
            get { return ParticleSystemManager.EffectTech2d.TexturedColorPassOneOneBlend; }
        }
        #endregion

        // c'tor
        public GlorpEmitter(ParticleSystemManager manager)
            : base(manager)
        {
            base.Init();
            LoadGraphicsContent(BokuGame.Graphics);

            Color = new Vector4(1.0f, 1.0f, 1.0f, 1.0f);
            StartRadius = 0.0f;
            EndRadius = 1.0f;
            StartAlpha = 1.0f;
            EndAlpha = 0.0f;
            MinLifetime = 0.2f;     // Particle lifetime.
            MaxLifetime = 1.0f;
            EmissionRate = 10.0f;  // Particles per second.

            MaxRotationRate = 2.0f;

        }   // end of c'tor

        // Cut and pasted from BaseSpriteEmitter, just to override
        // how the position is computed. Might like to refactor the
        // Update into common particle tasks (spawn, kill, move, etc.)
        public override void Update(Camera camera)
        {
            if (Active)
            {
                // Emit new particles if needed.
                float dt = Time.GameTimeFrameSeconds;
                dt = MathHelper.Clamp(dt, 0.0f, 1.0f);  // Limit to reasonable values.

                Position += Velocity * dt;

                if (Emitting)
                {
                    if (LinearEmission)
                    {
                        Vector3 deltaPosition = Position - PreviousPosition;
                        float dist = deltaPosition.Length();

                        partial += dist * EmissionRate;
                    }
                    else
                    {
                        partial += dt * EmissionRate;
                    }

                    // Emit as many particles as needed this 
                    // frame to keep up with the emission rate.
                    while (partial >= 1.0f)
                    {
                        if (particleList.Count < MaxSprites)
                        {
                            // Pick a random position on the sphere.
                            Vector3 rndVec = new Vector3((float)(rnd.NextDouble() - rnd.NextDouble()),
                                                        (float)(rnd.NextDouble() - rnd.NextDouble()),
                                                        (float)(rnd.NextDouble() - rnd.NextDouble()));
                            rndVec.Normalize();
                            rndVec *= targetRadius;
                            Vector3 pos = Position + rndVec;

                            // Pick a random position somewhere along the path covered this frame.
                            if (PositionJitter > 0.0f)
                            {
                                pos += PositionJitter * new Vector3((float)rnd.NextDouble() - (float)rnd.NextDouble(), (float)rnd.NextDouble() - (float)rnd.NextDouble(), (float)rnd.NextDouble() - (float)rnd.NextDouble());
                            }
                            float lifetime = MinLifetime + (float)rnd.NextDouble() * (MaxLifetime - MinLifetime);
                            BaseSpriteParticle particle = new BaseSpriteParticle(pos, lifetime, Color, MaxRotationRate, NumTiles);
                            particleList.Add(particle);
                        }

                        partial -= 1.0f;
                    }
                }

                // Update the previous position to match the current one for the next frame.
                ResetPreviousPosition();

                // Update any existing particles.  For more heavyweight particles we could
                // have an Update call per particle.  These are lightweight enough that we
                // can just update them directly.
                for (int i = 0; i < particleList.Count; )
                {
                    BaseSpriteParticle particle = (BaseSpriteParticle)particleList[i];

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
                        particle.radius = MyMath.Lerp(StartRadius, EndRadius, t);
                        particle.alpha = MyMath.Lerp(StartAlpha, EndAlpha, t);
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
                // Now that we've updated all the particles, create/update the vertex buffer.
                UpdateVerts();

                // See if we've died.
                if (Dying && particleList.Count == 0)
                {
                    Active = false;
                    RemoveFromManager();
                }

            }   // end of if active
        } // end Update


        public override void Render(Camera camera)
        {
            if (InGame.inGame.renderEffects == InGame.RenderEffect.DistortionPass)
            {
                base.Render(camera);
            }
        }


        public override void LoadGraphicsContent(GraphicsDeviceManager graphics)
        {
            // Load the texture.
            if (GlorpEmitter.texture == null)
            {
                GlorpEmitter.texture = BokuGame.ContentManager.Load<Texture2D>(BokuGame.Settings.MediaPath + @"Textures/Fire01");
            }

            base.LoadGraphicsContent(graphics);
        }   // end of GlorpEmitter LoadGraphicsContent()

        public override void UnloadGraphicsContent()
        {
            GlorpEmitter.texture = null;
            base.UnloadGraphicsContent();
        }   // end of GlorpEmitter UnloadGraphicsContent()

        public static void Unload()
        {
            GlorpEmitter.texture = null;
        }

    }   // end of class GlorpEmitter

}   // end of namespace Boku.Common

