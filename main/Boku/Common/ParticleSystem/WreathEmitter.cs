using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;

using KoiX;

namespace Boku.Common.ParticleSystem
{
    class WreathEmitter : BaseSpriteEmitter
    {
        protected static Texture2D wreathTexture = null;

        protected float wreathRadius = 0.75f;
        protected float wreathRate = 1.0f;
        protected float radialJitter = 0.1f;
        protected float keepUp = 1.0f;

        #region accessors
        protected override Texture2D Texture
        {
            get { return wreathTexture; }
            set { wreathTexture = value; }
        }
        public float WreathRadius
        {
            get { return wreathRadius; }
            set { wreathRadius = value; }
        }
        public float WreathRate
        {
            get { return wreathRate; }
            set { wreathRate = value; }
        }
        public float RadialJitter
        {
            get { return radialJitter; }
            set { radialJitter = value; }
        }
        public float KeepUp
        {
            get { return keepUp; }
            set { keepUp = value; }
        }
        /// <summary>
        /// Avoid the lighting effecting your color. These are arcade effect
        /// that don't want or need realistic lighting.
        /// </summary>
        public override bool IsEmissive
        {
            get { return true; }
        }

        #endregion

        // c'tor
        public WreathEmitter(ParticleSystemManager manager)
            : base(manager)
        {
            EmissionRate = 10.0f;
            Color = new Vector4(1.0f, 1.0f, 1.0f, 1.0f);
            StartRadius = 0.1f;
            EndRadius = 0.2f;
            PositionJitter = 0.1f;
            StartAlpha = 1.0f;
            EndAlpha = 0.0f;
            MinLifetime = 2.5f;       // Particle lifetime.
            MaxLifetime = 5.0f;

            MaxRotationRate = 2.0f;

            NumTiles = 4;

            // Have wreaths float up a bit.
            Gravity = new Vector3(0.0f, 0.0f, 0.05f);
            MaxSpeed = 10.0f;
        }   // end of c'tor

        protected override void Emit(float dt)
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
                            pos += PositionJitter * new Vector3((float)rnd.NextDouble() - (float)rnd.NextDouble(), (float)rnd.NextDouble() - (float)rnd.NextDouble(), (float)rnd.NextDouble() - (float)rnd.NextDouble());
                        }
                        double rads = rnd.NextDouble() * MathHelper.TwoPi;
                        Vector3 radial = scale * new Vector3((float)Math.Cos(rads), (float)Math.Sin(rads), 0.0f)
                            * (wreathRadius + RadialJitter * ((float)rnd.NextDouble() * 2.0f - 1.0f));
                        pos += radial;

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

        public override void UpdateParticles(float dt)
        {
            // Let the particles keep up with us as we move
            Vector3 advance = (Position - PreviousPosition) * KeepUp;
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
                    particle.position += advance;
                    particle.position += particle.velocity * dt;

                    // Now make the spin about the source
                    Vector2 fromCenter = new Vector2(particle.position.X - Position.X, particle.position.Y - Position.Y);
                    fromCenter.Normalize();
                    double theta = dt * wreathRate;
                    float sinTheta = (float)Math.Sin(theta);
                    float cosTheta = (float)Math.Cos(theta);
                    Vector2 delPos = scale * wreathRadius * ((float)theta)
                        * (sinTheta * fromCenter
                            + cosTheta * new Vector2(fromCenter.Y, -fromCenter.X));
                    particle.position.X += delPos.X;
                    particle.position.Y += delPos.Y;
                                                                
                    i++;
                }

            }   // end loop update particles.
        }

        new public static void LoadContent(bool immediate)
        {
            // Load the texture.
            if (wreathTexture == null)
            {
                wreathTexture = KoiLibrary.LoadTexture2D(@"Textures/Daisy01");
            }
        }   // end of WreathEmitter LoadContent()

        new public static void InitDeviceResources(GraphicsDevice device)
        {
        }

        new public static void UnloadContent()
        {
            wreathTexture = null;
        }   // end of WreathEmitter UnloadContent()

        new public static void DeviceReset(GraphicsDevice device)
        {
        }

    }
}
