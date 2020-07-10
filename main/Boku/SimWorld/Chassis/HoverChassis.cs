// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.


using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;
using System.Diagnostics;

using Boku.Base;
using Boku.Common;
using Boku.SimWorld;
using Boku.SimWorld.Terra;
using Boku.Animatics;

namespace Boku.SimWorld.Chassis
{
    /// <summary>
    /// Chassis used for things that hover, e.g. Boku, etc.
    /// </summary>
    public class HoverChassis : BaseChassis
    {
        #region Members

        float slopeAttenuation = 0;         // Controls how much slope affects movement.

        #endregion

        #region Accessors

        public override bool SupportsStrafing { get { return true; } }

        /// <summary>
        /// Controls how much slope affects movement.  Should be in range 0..1
        /// </summary>
        public float SlopeAttenuation
        {
            get { return slopeAttenuation; }
            set { slopeAttenuation = value; }
        }

        #endregion

        #region Public

        public HoverChassis()
            : base()
        {
        }

        public override void InitDefaults()
        {
            base.InitDefaults();

            slopeAttenuation = 0;
        }

        public override void PreCollisionTestUpdate(GameThing thing)
        {
            Movement movement = thing.Movement;
            DesiredMovement desiredMovement = thing.DesiredMovement;

            GameActor.State state = thing.CurrentState;

            if (thing.ActorHoldingThis != null)
                return;

            float secs = Time.GameTimeFrameSeconds;

            // Only move if active.
            if (state == GameActor.State.Active)
            {
                if (AllowBrainMovement)
                {
                    // Apply DesiredMovement values to current movement.
                    ApplyDesiredMovement(movement, desiredMovement);
                }

                // Apply external force.
                movement.Velocity += desiredMovement.ExternalForce.GetValueOrDefault() / thing.Mass * secs;

                // Apply drag to velocity.
                ApplyFriction(movement, desiredMovement, applyVertical: false);

                // Create local copies to work on.  Will be copied back to movement below.
                Vector3 velocity = movement.Velocity;
                Vector3 position = movement.Position;

                // Apply velocity to position.movement.
                position += velocity * secs;

                if (Jump && CanJump())
                {
                    // If we've already jumped but not yet landed or double-jumped, 
                    // see if we're allowed to double jump.
                    if (landing && !doubleJumping)
                    {
                        // Look for the vertical velocity to be slow as an 
                        // indicator of the top of the arc.
                        if (Math.Abs(velocity.Z) < 0.15f)
                        {
                            doubleJumping = true;
                            velocity.Z += 1.5f * effectiveJumpStrength;
                        }
                    }
                    else if (!landing && !jumping)
                    {
                        startJumpAnimation = true;      // Tell animation to start.
                        jumping = true;
                        jumpStartTime = Time.GameTimeTotalSeconds;
                    }
                }
                Jump = false;

                if (jumping)
                {
                    // If the pre delay time has passed, do the jump.
                    if (Time.GameTimeTotalSeconds > jumpStartTime + preJumpDelay)
                    {
                        velocity.Z += effectiveJumpStrength;
                        jumping = false;
                        landing = true;
                        lastJumpTime = Time.GameTimeTotalSeconds;
                    }
                }

                // Copy changes back to movement.
                movement.Position = position;
                movement.Velocity = velocity;

            }   // end of if state is active.

            //
            // Balance hover force with gravity so that chassis stays at the right height.
            // Note that we use the previous position for this since we "know" it's valid.
            //
            {            
                // Create local copies to work on.  Will be copied back to movement below.
                Vector3 velocity = movement.Velocity;
                Vector3 position = movement.Position;
                Vector3 prevPosition = movement.PrevPosition;

                // Calc the height we want to be at.
                float waterAltitude = Terrain.GetWaterBase(prevPosition);
                float terrainAltitude = 0.0f;
                Vector3 terrainNormal = Vector3.Zero;

                GetTerrainAltitudeAndNormalFromFeelers(movement, ref terrainAltitude, ref terrainNormal);

                // Heights are distance above ground, not absolute values.
                float heightGoal = EditHeight;
                float floor = 0.0f;     // Level we're hovering over.
                bool overWater = false;
                if (thing.StayAboveWater)
                {
                    if (waterAltitude > terrainAltitude)
                    {
                        floor = waterAltitude;
                        overWater = true;

                        if (!landing)
                        {
                            /// Only apply ripples if we are _not_ in the middle
                            /// of a jump.
                            CheckRipples(thing, thing.CollisionRadius);
                        }
                    }
                    else
                    {
                        floor = terrainAltitude;
                    }
                }
                else
                {
                    floor = terrainAltitude;

                    /// Look to see if we are intersecting the water surface,
                    /// and if so kick off some ripples.
                    Vector3 collCenter = thing.WorldCollisionCenter;
                    float collRad = thing.WorldCollisionRadius;
                    if ((waterAltitude - Terrain.WaveHeight < collCenter.Z + collRad)
                        && (waterAltitude > collCenter.Z - collRad))
                    {
                        CheckRipples(thing, collRad * 1.5f);
                    }
                }
                // If there's no terrain or we're already falling, just let the bot fall.
                // Compare against -1 to ensure that we can't get pushed through the ground.
                if (floor == 0.0f || position.Z - MinHeight < -1.0f)
                {
                    floor = float.MinValue;
                }
                float curHeight = position.Z - floor;

                // Are we waiting to land?
                if (landing)
                {
                    // Are we close enough?
                    float predictedHeight = curHeight + velocity.Z * preLandDelay;
                    if (predictedHeight < heightGoal)
                    {
                        startLandAnimation = true;
                        landing = false;
                        doubleJumping = false;
                    }
                }

                /*
                // Did we bounce into the ground?
                if (curHeight < MinHeight)
                {
                    position.Z = floor + MinHeight;     // Move back to above ground.
                    velocity.Z = Math.Max(velocity.Z, 0.1f);

                    // TODO (****) Bump sound and dust cloud?
                }
                else
                */
                {
                    float ratio = heightGoal / curHeight;
                    if (float.IsNaN(ratio))
                        ratio = 0.0f;

                    float hoverEffect = ratio * -Gravity;

                    float lift = hoverEffect + Gravity;

                    // Clamp lift to limits.
                    if (lift > 0.0f)
                    {
                        // Clamp to maximum acceleration up.  Allow vertical acceleration to be 10x the
                        // normal linear acceleration.  This will help keep bots out of the ground.
                        lift = 10.0f * MathHelper.Clamp(lift, 0, MaxLinearAcceleration * LinearAccelerationModifier);
                    }
                    else
                    {
                        // Clamp to gravity down.
                        lift = MathHelper.Clamp(lift, Gravity, 0);
                    }

                    float deltaZ = lift * secs;

                    velocity.Z *= (1.0f - secs);    // Damp vertical velocity.
                    velocity.Z += deltaZ;

                    // If lift > 0 clamp the upward velocity to the max allowed without overshoot.
                    if (lift > 0)
                    {
                        float d = heightGoal - curHeight;
                        float t = (float)Math.Sqrt(Math.Abs(2.0f * d / Gravity));
                        float speed = -t * Gravity;
                        velocity.Z = Math.Min(velocity.Z, speed);
                    }

                    // Did we bounce off the ground?
                    if (curHeight < 0.0f)
                    {
                        // Move position back above ground.
                        position.Z -= curHeight;
                        // Make sure we're moving up, not down.  Also apply some agressive damping.
                        // This prevents hover chassis bots from getting too bouncy if they are
                        // too close to the ground.
                        velocity.Z = (float)Math.Abs(velocity.Z) * CoefficientOfRestitution * 0.2f;
                    }

                }

                // Add in effect of slope.
                if (!overWater && Moving)
                {
                    // Attenuate SlopeAttenuation based on how high we are.  Basically, if
                    // we're way up in the air, ignore slopes.
                    float slopeFactor = SlopeAttenuation;

                    if (curHeight > EditHeight)
                    {
                        if (curHeight > EditHeight * 2.0f)
                        {
                            // Too high, ignore slope.
                            slopeFactor = 0.0f;
                        }
                        else
                        {
                            slopeFactor *= 1.0f - (curHeight - EditHeight) / EditHeight;
                        }
                    }

                    if (slopeFactor > 0)
                    {
                        // Translate any change in height to a change in velocity.
                        float deltaZ = prevPosition.Z - position.Z;
                        float deltaV = (float)Math.Sqrt(Math.Abs(2.0f * Gravity * deltaZ));

                        // Always have a minimal deltaV.  This causes things to start
                        // sliding downhill if not moving.
                        deltaV = Math.Max(deltaV, 0.01f);

                        // Get direction apply new velocity.
                        terrainNormal.Z = 0.0f;
                        velocity += slopeFactor * deltaV * terrainNormal;
                    }
                }

                // Apply vertical velocity to position.
                position.Z += velocity.Z * secs;

                // Copy changes back to movement.
                movement.Velocity = velocity;
                movement.Position = position;
            }

        }   // end of PreCollisionTestUpdate()

        private bool CanJump()
        {
            double currTime = Time.GameTimeTotalSeconds;
            return JumpRate != 0 && (currTime - lastJumpTime) >= 1.0 / JumpRate;
        }

        /// <summary>
        /// Based on the chassis' internal values, sets the blend values for the 
        /// four standard looping animations.
        /// </summary>
        /// <param name="anims"></param>
        public override void SetLoopedAnimationWeights(AnimationSet anims, Movement movement, DesiredMovement desiredMovement)
        {
            StandardSetLoopedAnimationWeights(anims, movement, desiredMovement);
        }   // end of SetLoopedAnimationWeights()

        #endregion

        #region Internal

        /// <summary>
        /// Applies the values set by the brain in DesiredMovement to this chassis.
        /// </summary>
        /// <param name="movement"></param>
        /// <param name="desiredMovement"></param>
        public override void ApplyDesiredMovement(Movement movement, DesiredMovement desiredMovement)
        {
            // Apply velocity changes.
            ApplyDesiredVelocityForHover(movement, desiredMovement);

            // Apply rotation changes.
            ApplyDesiredRotation(movement, desiredMovement);

        }   // end of ApplyDesiredMovement()

        #endregion

    }   // end of class HoverChassis

}   // end of namespace Boku.SimWorld.Chassis
