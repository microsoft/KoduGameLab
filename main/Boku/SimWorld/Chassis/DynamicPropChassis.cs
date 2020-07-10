// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.


using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;
using System.Diagnostics;

using KoiX;

using Boku.Base;
using Boku.Common;
using Boku.Common.ParticleSystem;
using Boku.SimWorld;
using Boku.SimWorld.Terra;

namespace Boku.SimWorld.Chassis
{
    /// <summary>
    /// Simple chassis used for props that can be kicked around, 
    /// e.g. fruit and rocks.
    /// </summary>
    public class DynamicPropChassis : BaseChassis
    {
        #region Members
        public const bool DefaultTumbles = false;
        public const float DefaultTumbleRadius = 1.0f;
        public const float DefaultSpinRate = 0.0f;
        private const float DefaultSpinAngle = 0.0f;
        private const float DefaultKSmallMove = 0.01f;

        private bool tumbles = DefaultTumbles;          // Should this object tumble as it moves.
        private float tumbleRadius = DefaultTumbleRadius;// The radius used to calculate the amount of tumble.
                                                        // Note, spin and tumble don't work together.

        private float spinRate = DefaultSpinRate;       // Spin around Z axis in radians per second.
        private float spinAngle = DefaultSpinAngle;     // Current spin angle.
                                                        // Note, spin and tumble don't work together.

        private float kSmallMove = DefaultKSmallMove;   // Threshold to determine if we've stopped moving.

        private bool floating = false;                  // Kind of what it says.
        private bool inWater = false;                   // In or over water

        private DustEmitter dustEmitter = null;
        private double dustEnd = 0.0f;
        #endregion

        #region Accessors

        public override bool SupportsStrafing { get { return false; } }

        /// <summary>
        /// Should this object tumble as it moves.
        /// </summary>
        public bool Tumbles
        {
            get { return tumbles; }
            set { tumbles = value; }
        }

        /// <summary>
        /// The radius used to calculate the amount of tumble.
        /// </summary>
        public float TumbleRadius
        {
            get { return tumbleRadius; }
            set { tumbleRadius = value; }
        }

        /// <summary>
        /// Spin around Z axis in radians per second.
        /// </summary>
        public float SpinRate
        {
            get { return spinRate; }
            set { spinRate = value; }
        }

        #endregion

        #region Public

        public DynamicPropChassis()
        {
            InitDustEmitter();

            terrainOnContact = true;
        }

        /// <summary>
        /// Either the scene is starting up, or we've just been added. Either way,
        /// time to start doing our thing.
        /// </summary>
        public override void Activate()
        {
            dustEmitter.AddToManager();
            dustEmitter.ResetPreviousPosition();
        }

        /// <summary>
        /// Our owner is going dormant. Tear down anything we're doing.
        /// </summary>
        public override void Deactivate()
        {
            dustEmitter.RemoveFromManager();
        }


        public override void InitDefaults()
        {
            base.InitDefaults();

            // values here copied from member initializers above.
            tumbles = DefaultTumbles;
            tumbleRadius = DefaultTumbleRadius;
            spinRate = DefaultSpinRate;
            spinAngle = DefaultSpinAngle;
            kSmallMove = DefaultKSmallMove;
            floating = false;
            inWater = false;
        }

        public override void PreCollisionTestUpdate(GameThing thing)
        {
            Movement movement = thing.Movement;
            DesiredMovement desiredMovement = thing.DesiredMovement;

            GameActor.State state = thing.CurrentState;

            UpdateDustEmitter(state);

            // Only do the update if this thing is active and moving.
            if (((state == GameActor.State.Active) || (state == GameThing.State.Dead || state == GameThing.State.Squashed))
                && Moving)
            {
                float dt = (float)Time.GameTimeFrameSeconds;


                floating = false;
                inWater = false;

                // Start with existing values.
                Vector3 position = movement.Position;
                Vector3 prevPosition = position;
                Vector3 velocity = movement.Velocity;

                // Are we in water?
                if (Terrain.GetWaterBase(position) > 0.0f)
                {
                    Vector3 surfaceNormal = Vector3.UnitZ;
                    float waterAlt = Terrain.GetWaterHeightAndNormal(position, ref surfaceNormal);

                    if (waterAlt > 0)
                    {
                        inWater = true;
                        CheckRipples(thing, velocity, waterAlt);
                    }

                    // Are we in the water?
                    if (position.Z < waterAlt)
                    {
                        // TODO (****) This could be made better by also having
                        // the density control how deep an object floats.

                        // Should we sink or float?
                        if (Density > 1.0f)
                        {
                            // Sink.
                            // Add attenuated gravity.
                            float attenuation = 1.0f - 1.0f / Density;
                            velocity.Z += attenuation * Gravity * dt;
                        }
                        else
                        {
                            // Float.
                            // Add attenuated, inverted gravity.
                            float attenuation = 1.0f - Density;
                            velocity.Z -= attenuation * Gravity * dt;

                            // If we're floating near the surface, have the waves move us a bit.
                            float depth = waterAlt - position.Z;
                            if (depth < 0.5f)
                            {
                                float amp = (0.5f - depth) * 20.0f;
                                velocity.X += surfaceNormal.X * dt * amp;
                                velocity.Y += surfaceNormal.Y * dt * amp;
                                floating = true;
                            }
                        }

                        // Now attenuate velocity to simulate drag from water.
                        velocity = MyMath.Lerp(velocity, Vector3.Zero, 2.0f * dt);
                    }
                    else
                    {
                        // Not in water.
                        // Add in effect of full gravity.
                        velocity.Z += Gravity * dt;
                    }
                }
                else
                {
                    // No water.
                    // Add in effect of full gravity.
                    velocity.Z += Gravity * dt;
                }

                // Apply external force.
                velocity += desiredMovement.ExternalForce.GetValueOrDefault() / thing.Mass * dt;

                // Update location.
                position.X += velocity.X * dt;
                position.Y += velocity.Y * dt;

                // Push changes back to movement.
                movement.Velocity = velocity;
                movement.Position = position;

                // If we're version 1 or later also do rotation based on brain.
                if (Parent.Version >= 1)
                {
                    ApplyDesiredMovement(movement, desiredMovement);
                }
            }   // end of if moving
            else if (state != GameActor.State.Active 
                && thing.ActorHoldingThis == null
                && state != GameThing.State.Dead
                && state != GameThing.State.Squashed
                && !Moving)
            {
                // We're not active but we still want to be able to have our height adjusted.
                movement.Altitude = Terrain.GetTerrainAndPathHeight(Top(movement.Position)) + EditHeight;
            }

        }   // end of DynamicPropChassis PreCollisionTestUpdate()

        public override void PostCollisionTestUpdate(GameThing thing)
        {
            Movement movement = thing.Movement;
            GameActor.State state = thing.CurrentState;

            float dt = Time.GameTimeFrameSeconds;

            // Only do the update if this thing is active and moving and not immobile.
            // The test on immobile prevents immobile objects from tumbling.
            if (((state == GameActor.State.Active) || (state == GameThing.State.Dead || state == GameThing.State.Squashed))
                && Moving
                && !((GameActor)Parent).TweakImmobile)
            {
                //
                // Bounce off ground (as opposed to the walls).
                //

                // Start with existing values.
                Vector3 position = movement.Position;
                Vector3 velocity = movement.Velocity;

                // Get terrain/path height at the current point.
                Vector3 normal = Vector3.Zero;
                float terrainPathAlt = 0.0f;

                GetTerrainAltitudeAndNormalFromFeelers(movement, ref terrainPathAlt, ref normal);

                // Is our starting position over terrain?
                // The +2 allows object to be slightly pushed into the ground
                // without easily getting stuck.
                float height = position.Z - MinHeight + 2;
                bool preOverTerrain = terrainPathAlt > 0.0f && height >= terrainPathAlt;

                // Apply Z velocity to position.
                float deltaZ = velocity.Z * dt;
                position.Z += deltaZ;

                // Only test for bounce if we start over terrain.
                if (preOverTerrain)
                {
                    // See if we're still over terrain.  The -0.5 check is so that things under the terrain don't just pop up to the surface.
                    height = position.Z - MinHeight;
                    bool postOverTerrain = terrainPathAlt >= 0.0f && height >= terrainPathAlt && height > -0.5f;

                    // If we're no longer over terrain, we must have gone through
                    // so we should bounce instead.
                    if (!postOverTerrain)
                    {
                        position.Z -= deltaZ;

                        movement.Position = position;
                        movement.Velocity = velocity;

                        BounceOffGround(thing, terrainPathAlt, normal);

                        position = movement.Position;
                        velocity = movement.Velocity;

                        // Make sure to push actor just above 0.
                        height = position.Z - MinHeight - terrainPathAlt;
                        if (height < 0)
                        {
                            position.Z -= height - 0.0001f;
                        }
                    }
                }
                else
                {
                }

                movement.Position = position;

                if (Tumbles && (dt > 0))
                {
                    // Base tumble on how much we actually moved rather than
                    // on velocity.
                    Vector3 vel = (movement.Position - movement.PrevPosition)/dt;
                    vel.Z = 0.0f;
                    float speed = vel.Length();

                    if (Math.Abs(speed) > 0.001)
                    {
                        float tumbleAngle = -speed * dt / (tumbleRadius * thing.ReScale);
                        // Roll less if floating.
                        if (floating)
                        {
                            tumbleAngle *= 0.2f;
                        }
                        Vector3 axis = Vector3.Cross(vel, Vector3.UnitZ);
                        axis.Normalize();
                        Matrix mat = movement.LocalMatrix;
                        mat.Translation = Vector3.Zero;
                        mat *= Matrix.CreateFromAxisAngle(axis, tumbleAngle);
                        mat.Translation = position;
                        movement.SetLocalMatrixAndRotation(mat, movement.RotationZ);
                    }
                }

            }   // end of if moving
            else if(state == GameThing.State.Active)
            {
                // Get terrain/path height at the current point.
                float terrainPathAlt = Terrain.GetTerrainAndPathHeight(Top(movement.Position));

                // Check if pushed into the ground.
                if (terrainPathAlt > 0.0f && movement.Altitude - MinHeight < terrainPathAlt)
                {
                    float newHeight = terrainPathAlt + MinHeight;
                    //float dZ = newHeight - movement.Altitude;
                    //movement.Altitude += dZ * dt;
                    // FOrce all the way up.  Add a bit of cushion too.
                    movement.Altitude = newHeight + 0.0001f;
                }

            }

            // Only spin if version < 1.
            if (Parent.Version < 1)
            {
                if (SpinRate != 0.0f)
                {
                    if (state == GameThing.State.Paused)
                    {
                        spinAngle = 0.0f;
                        Matrix mat = Matrix.Identity;
                        mat.Translation = movement.Position;
                        movement.SetLocalMatrixAndRotation(mat, movement.RotationZ);
                    }
                    else if (thing.ActorHoldingThis == null)
                    {
                        spinAngle += SpinRate * Time.GameTimeFrameSeconds;
                        spinAngle %= MathHelper.TwoPi;
                        Matrix mat = Matrix.CreateRotationZ(spinAngle);
                        mat.Translation = movement.Position;
                        movement.SetLocalMatrixAndRotation(mat, spinAngle);
                    }
                }
            }
        }   // end of DynamicPropChassis PostCollisionTestUpdate()

        // Counts how many frames this bot has not moved.  Moving is set to false after 6.
        // This forces an object to settle before being treated as static.
        int stopMovingCount = 0;

        protected override void BounceOffGround(GameThing thing, float terrainHeight, Vector3 terraNormal)
        {
            Movement movement = thing.Movement;

            float secs = Time.GameTimeFrameSeconds;

            Vector3 position = movement.Position;
            Vector3 velocity = movement.Velocity;

            // Move the thing back above ground level, but only a bit at a time to keep it smooth.
            float deltaZ = terrainHeight + MinHeight - position.Z;

            //deltaZ *= secs;

            position.Z += deltaZ;   // Move position back to just contacting terrain.

            // Remove effect of gravity for ths frame.
            // This helps the jitters that occur when almost standing still.
            if (velocity.Z < 0.0f)
            {
                velocity.Z -= Gravity * secs;
            }

            // Translate any change in height to a change in velocity.
            // This causes things rolling downhill to speed up.
            // Note that this only affects the XY axes, not the vertical.
            float deltaV = (float)Math.Sqrt(Math.Abs(2.0f * Gravity * deltaZ));

            // Get direction apply new velocity.  The 0.5 factor is not "correct" 
            // but the movement looks better with it in.
            Vector3 slopeNormal = Terrain.GetNormal(position);
            slopeNormal.Z = 0.0f;
            velocity += 0.5f * deltaV * slopeNormal;

            float velDotNorm = Vector3.Dot(velocity, terraNormal);
            if (velDotNorm < 0)
            {
                // For tumbling objects ignore the friction value and just
                // use the CoR on all components of the velocity.  For back
                // compat we now set the friction for tumbling objects to
                // 0.0f.  This way, previously saved levels with different
                // settings will still act as they used to.
                if (Tumbles && Friction == 0.0f)
                {
                    // Dampen velocity.
                    // Use a higher CoR for XY so things roll better.
                    float xyCoR = CoefficientOfRestitution + (1 - CoefficientOfRestitution) * 0.8f;
                    velocity.X *= xyCoR;
                    velocity.Y *= xyCoR;
                    velocity.Z *= CoefficientOfRestitution;

                    // Bounce
                    velocity = Vector3.Reflect(velocity, terraNormal);
                }
                else
                {
                    Vector3 velNormal = velDotNorm * terraNormal;
                    Vector3 velTangent = velocity - velNormal;

                    velNormal *= CoefficientOfRestitution;
                    // The default friction of 0.0 causes knocked out objects
                    // to slide endlessly across the ground.  So, bump up the
                    // friction so it looks reasonable.
                    velTangent *= GameActor.FrictionDecay(0.99f, secs);

                    // Really damp down squashed objects.
                    if (Parent.CurrentState == GameThing.State.Squashed)
                    {
                        velNormal *= CoefficientOfRestitution;
                        velTangent *= GameActor.FrictionDecay(0.99f, secs);
                    }

                    velocity = velTangent - velNormal;
                }

                // Check if we've stopped moving.
                if (velocity.LengthSquared() < kSmallMove)
                {
                    if (stopMovingCount >= 12)
                    {
                        Moving = false;
                    }
                    else
                    {
                        ++stopMovingCount;
                    }
                }
                else
                {
                    stopMovingCount = 0;
                }

                // Trigger a dust emitter puff.
                if (!inWater && !thing.Invisible)
                {
                    MakeDustPuff(position, terrainHeight, velocity.Length());
                }

                // Apply changes to movement object.
                movement.Velocity = velocity;
                movement.Position = position;
            }
        }

        private void MakeDustPuff(Vector3 position, float terrainHeight, float density)
        {
            float kMinDensity = 1.25f;
            density -= kMinDensity;
            if (density > 0)
            {
                float kDustLife = 0.25f;
                dustEnd = Time.GameTimeTotalSeconds + kDustLife;
                dustEmitter.Position = new Vector3(position.X, position.Y, terrainHeight + dustEmitter.StartRadius);
                dustEmitter.ResetPreviousPosition();

                density *= 5.0f;
                dustEmitter.EmissionRate = density;
                dustEmitter.Emitting = true;
            }
        }
        private void UpdateDustEmitter(GameActor.State state)
        {
            switch (state)
            {
                case GameThing.State.Active:
                case GameThing.State.Dead:
                case GameThing.State.Squashed:
                    if (dustEnd < Time.GameTimeTotalSeconds)
                    {
                        dustEmitter.Emitting = false;
                    }
                    break;

                case GameThing.State.Inactive:
                case GameThing.State.Paused:
                    dustEmitter.Emitting = false;
                    break;

            }
        }
        private void InitDustEmitter()
        {
            float radius = 0.5f;
            dustEmitter = new DustEmitter(InGame.inGame.ParticleSystemManager);
            dustEmitter.PositionJitter = radius * 0.7f;      // Random offset for each particle.
            dustEmitter.StartRadius = radius * 0.4f;
            dustEmitter.EndRadius = radius;
            dustEmitter.EmissionRate = 0.0f;
            dustEmitter.Active = true;
            dustEmitter.Emitting = false;
        }

        /// <summary>
        /// Check to see if we are intersecting the surface of the water, and
        /// if we are, make some ripples and check for the need for a splash.
        /// </summary>
        /// <param name="thing"></param>
        /// <param name="vel"></param>
        /// <param name="waterHeight"></param>
        private void CheckRipples(GameThing thing, Vector3 vel, float waterHeight)
        {
            Vector3 collCenter = thing.WorldCollisionCenter;
            float collRad = thing.WorldCollisionRadius;
            if ((waterHeight - Terrain.WaveHeight < collCenter.Z + collRad)
                && (waterHeight > collCenter.Z - collRad))
            {
                base.CheckRipples(thing, collRad);

                CheckSplash(thing, collCenter, vel);
            }
        }


        public override void CollisionResponse(Movement movement)
        {
        }   // end of DynamicChassis CollisionResponse()

        /// <summary>
        /// Applies the values set by the brain in DesiredMovement to this chassis.
        /// </summary>
        /// <param name="movement"></param>
        /// <param name="desiredMovement"></param>
        public override void ApplyDesiredMovement(Movement movement, DesiredMovement desiredMovement)
        {
            GameActor actor = Parent as GameActor;

            ApplyDesiredRotation(movement, desiredMovement);

        }   // end of ApplyDesiredMovement()

        #endregion

    }   // end of class DynamicPropChassis

}   // end of namespace Boku.SimWorld.Chassis
