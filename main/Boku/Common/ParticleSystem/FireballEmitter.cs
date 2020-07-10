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

using Boku.Base;

namespace Boku.Common.ParticleSystem
{
    // Shoots a fireball.
    public class FireballEmitter : BaseEmitter
    {

        public class FireballParticle
        {
            public float age;
            public float lifetime;
            public Vector3 position;
            public Vector3 velocity;
            public float radius;
            public float alpha;
            public Color color;

            public Fireball fireball = null;    // The GameThing associated with this particle so
                                                // that actors in the scene can respond to this particle.

            // Each fireball has its own smoke emitter.  We may have to change this to
            // a shared static if we need to better control the overall number of 
            // particles in the scene.
            public SmokeEmitter smokeEmitter = null;

            // c'tor
            public FireballParticle(Color color, ParticleSystemManager manager)
            {
                this.age = 0.0f;
                this.color = color;

                smokeEmitter = new SmokeEmitter(manager);
                smokeEmitter.Active = true;

                fireball = new Fireball(Classification.Colors.Red);
                InGame.inGame.gameThingList.Add(fireball);
                fireball.Activate();
            }   // end of c'tor

        }   // end of class FireballParticle

        //
        //
        //  FireballEmitter
        //
        //

        private Color color = Color.Yellow;
        private float startRadius = 0.1f;
        private float endRadius = 0.1f;
        private float startAlpha = 1.0f;
        private float endAlpha = 1.0f;
        private float speed = 15.0f;
        // private float gravity = 0.0f;   // Effect due to gravity.  Ignore for now.

        private float fireRate = 0.5f;  // Fireballs per second.

        #region Accessors
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
        public float FireRate
        {
            get { return fireRate; }
            set { fireRate = value; }
        }
        #endregion

        // c'tor
        public FireballEmitter(ParticleSystemManager manager)
            : base(manager)
        {
        }   // end of c'tor

        private Vector3 from;
        private Vector3 at;
        private bool shotPending = false;
        private float partial = 1.0f;   // Start loaded and ready to shoot.

        public override void Update(Camera camera)
        {
            if (active)
            {
                float dt = Time.GameTimeFrameSeconds;
                dt = MathHelper.Clamp(dt, 0.0f, 1.0f);  // Limit to reasonable values.

                partial += dt * fireRate;

                // Note this is set up so that at most a single particle will be
                // emitted per Update call.  This works for the fireballs but may
                // not be right for other particle effects.
                if (shotPending && partial >= 1.0f)
                {
                    FireballParticle ball = new FireballParticle(Color.Red, manager);
                    ball.velocity = at - from;
                    ball.lifetime = 2.5f * ball.velocity.Length() / speed;
                    ball.velocity.Normalize();
                    ball.velocity *= speed;
                    ball.position = from;

                    ball.fireball.Movement.Position = ball.position;

                    particleList.Add(ball);

                    partial = 0.0f;
                    shotPending = false;
                }

                // Update any existing particles.  For more heavyweight particles we could
                // have an Update call per particle.  These are lightweight enough that we
                // can just update them directly.
                for (int i = 0; i < particleList.Count; )
                {
                    FireballParticle particle = (FireballParticle)particleList[i];

                    particle.age += dt;

                    Debug.Assert(particle.age >= 0.0f);

                    float t = particle.age / particle.lifetime;
                    if (t > 1.0f)
                    {
                        // Dead particle but we can't remove it until all the smoke
                        // it generated has gone.  Turn off the emission of the smoke
                        // particles and just let the exisitng ones fade out.
                        particle.smokeEmitter.Emitting = false;
                        if (particle.smokeEmitter.ListIsEmpty)
                        {
                            particleList.RemoveAt(i);
                        }
                        else
                        {
                            particle.smokeEmitter.Update(camera);
                            i++;
                        }

                        // Go ahead and remove the GameThing right away.
                        if (particle.fireball != null)
                        {
                            particle.fireball.Deactivate();
                            InGame.inGame.gameThingList.Remove(particle.fireball);
                            particle.fireball = null;
                        }
                    }
                    else
                    {
                        particle.radius = MyMath.Lerp(startRadius, endRadius, t);
                        particle.alpha = MyMath.Lerp(startAlpha, endAlpha, t);
                        particle.position += particle.velocity * dt;
                        particle.fireball.Movement.Position = particle.position;

                        particle.smokeEmitter.Position = particle.position;
                        particle.smokeEmitter.Velocity = particle.velocity;

                        particle.smokeEmitter.Update(camera);

                        i++;
                    }
                }

            }   // end of if active
        }   // end of FireballEmitter Update()

        public override void Render(Camera camera)
        {
            if (active)
            {
                Sphere sphere = Sphere.GetInstance();

                // Get the effect we need.
                Effect effect = manager.Effect3d;

                // Set up common rendering values.
                effect.CurrentTechnique = manager.Technique3d((int)ParticleSystemManager.EffectTech3d.PremultAlphaGlowColorPass);

                manager.Parameter3d(ParticleSystemManager.EffectParams3d.DiffuseColor).SetValue(color.ToVector4());

                // Set up world matrix.
                Matrix worldMatrix = Matrix.Identity;
                //worldMatrix.Translation = position;

                manager.Parameter3d(ParticleSystemManager.EffectParams3d.GlowFactor).SetValue(1.0f);

                // Render all the fireballs first.
                for (int i = 0; i < particleList.Count; i++)
                {
                    FireballParticle particle = (FireballParticle)particleList[i];

                    // Need to check age since we may still have expired 
                    // particles in the list while their smoke fades.
                    if (particle.age < particle.lifetime)
                    {
                        // Set radius and translation.
                        worldMatrix.Translation = particle.position;
                        Matrix worldViewProjMatrix = worldMatrix * camera.ViewProjectionMatrix;

                        // Set radius.
                        manager.Parameter3d(ParticleSystemManager.EffectParams3d.Radius).SetValue(particle.radius);

                        // Set alpha.
                        manager.Parameter3d(ParticleSystemManager.EffectParams3d.Alpha).SetValue(particle.alpha);

                        // Set color
                        manager.Parameter3d(ParticleSystemManager.EffectParams3d.DiffuseColor).SetValue(particle.color.ToVector4());

                        sphere.Render(camera, ref worldMatrix, effect);
                    }

                }

                // Then render the smoke for all the fireballs.
                for (int i = 0; i < particleList.Count; i++)
                {
                    FireballParticle particle = (FireballParticle)particleList[i];
                    particle.smokeEmitter.Render(camera);
                }

            }   // end of if active

        }   // end of Emitter Render()

        // private float lastShotTime = 0.0f;

        public void Shoot(Vector3 from, Vector3 at)
        {
            if (partial >= 1.0f)
            {
                this.from = from;
                this.at = at;
                shotPending = true;
            }
        }   // end of FireballEmitter Shoot()


    }   // end of class FireballEmitter

}   // end of namespace Boku.Common
