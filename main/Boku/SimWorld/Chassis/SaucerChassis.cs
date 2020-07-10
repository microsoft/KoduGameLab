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

namespace Boku.SimWorld.Chassis
{
    /// <summary>
    /// Chassis used for Saucer, wisp, etc.
    /// </summary>
    public class SaucerChassis : BaseChassis
    {
        #region Members
        public const float DefaultMaxLinearAcceleration = 0.5f;
        public const float DefaultMaxSpeed = 2.0f;
        public const float DefaultRotationRate = 0.0f;
        public const float DefaultSlopeThreshold = 0.0f;

        private float rotationRate = DefaultRotationRate;       // radians / sec.  Note this is just added to the local matrix.  This does not affect heading.
        private float slopeThreshold = DefaultSlopeThreshold;   // Actually sine of angle.

        private const float DefaultStartingAltitude = Single.MinValue;
        private float startingAltitude = DefaultStartingAltitude;   // This is the altitude we are trying to cruise at.
                                                                    // We may be higher or lower depending on terrain or
                                                                    // getting bumped.

        #endregion

        #region Accessors

        public override bool SupportsStrafing { get { return true; } }

        /// <summary>
        /// This rotation is purely cosmetic.  It shoudl have no effect on the 
        /// heading or facing directions of the actor.
        /// </summary>
        public float RotationRate
        {
            get { return rotationRate; }
            set { rotationRate = value; }
        }
        /// <summary>
        /// This is the sine of the max angle the bot can navigate without sliding.
        /// </summary>
        public float SlopeThreshold
        {
            get { return slopeThreshold; }
            set { slopeThreshold = value; }
        }
        #endregion

        #region Public

        public SaucerChassis()
            : base()
        {
            MaxLinearAcceleration = DefaultMaxLinearAcceleration;
            MaxSpeed = DefaultMaxSpeed;
        }

        public override void InitDefaults()
        {
            base.InitDefaults();

            // Values here copied from member initializers above.
            startingAltitude = DefaultStartingAltitude;
            rotationRate = DefaultRotationRate;
            slopeThreshold = DefaultSlopeThreshold;
        }

        public override void PreCollisionTestUpdate(GameThing thing)
        {
            Movement movement = thing.Movement;
            DesiredMovement desiredMovement = thing.DesiredMovement;

            GameActor.State state = thing.CurrentState;

            float secs = Time.GameTimeFrameSeconds;

            float waterAltitude = Terrain.GetWaterBase(movement.Position);

            // Only move if active.
            if (state == GameActor.State.Active)
            {
                // Apply DesiredMovement values to current movement.
                ApplyDesiredMovement(movement, desiredMovement);

                float height = 0;
                bool bounce = CollideWithGround(movement, ref height);

                // Apply external force.
                movement.Velocity += desiredMovement.ExternalForce.GetValueOrDefault() / thing.Mass * secs;

                // Apply drag to velocity.
                ApplyFriction(movement, desiredMovement, applyVertical: true);

                // Apply velocity to position.
                movement.Position += movement.Velocity * secs;

                // Prevent going up steep slopes and have bots slide down slopes.
                if (waterAltitude > 0.0f || !thing.StayAboveWater)
                {
                    Vector3 normal = Terrain.GetNormal(movement.Position);
                    if (normal.Z < slopeThreshold)
                    {
                        Vector3 velocity = movement.Velocity;
                        velocity.X += normal.X * 10.0f * secs;
                        velocity.Y += normal.Y * 10.0f * secs;
                        movement.Velocity = velocity;
                    }
                }
                if (waterAltitude > 0.0f)
                {
                    Vector3 collCenter = thing.WorldCollisionCenter;
                    float collRad = thing.WorldCollisionRadius;
                    if ((waterAltitude - Terrain.WaveHeight < collCenter.Z + collRad)
                        && (waterAltitude > collCenter.Z - collRad))
                    {
                        CheckRipples(thing, collRad);
                    }
                }
            }   // end of if state is active.

        }   // end of PreCollisionTestUpdate()

        public override void PostCollisionTestUpdate(GameThing thing)
        {
            Movement movement = thing.Movement;

            float secs = Time.GameTimeFrameSeconds;

            // Init the target Altitude.  This should only happen the very first time 
            // after a reset.  We want to use the bot's initial height as the original 
            // target height but at the time the c'tor is called we don't yet know 
            // where the bot will be positioned.
            if (startingAltitude == DefaultStartingAltitude)
            {
                startingAltitude = Terrain.GetTerrainAndPathHeight(Top(movement.Position)) + EditHeight;
                if (Parent.StayAboveWater)
                {
                    startingAltitude = Math.Max(startingAltitude, Terrain.GetWaterHeight(movement.Position) + EditHeight);
                }
            }

            //
            // Now handle "automatic" vertical movement.  We want the charactors to try and stay at their
            // startingAltitude.
            //

            // Look at desiredMovement to see if user is actively changing altitude.  If
            // so then adjust startingAltitude to match.
            DesiredMovement desiredMovement = Parent.DesiredMovement;
            bool coastingVertically = desiredMovement.CoastingVertically;

            if (!coastingVertically)
            {
                if (desiredMovement.DesiredAltitude.HasValue)
                {
                    startingAltitude = desiredMovement.DesiredAltitude.Value;
                }
                if (desiredMovement.DesiredVerticalSpeed.HasValue)
                {
                    // If we're moving up/down, assume current altitude is the new target.
                    startingAltitude = movement.Altitude;
                }
            }

            // Adjust height for terrain.
            float waterAltitude = Terrain.GetWaterBase(movement.Position);
            float terrainAltitude = MaxFeelerAltitude(movement, movement.Position, thing.ReScale);
            // altitudeBase is the ground/water we're flying over and basing our height on.
            float altitudeBase = thing.StayAboveWater ? MathHelper.Max(terrainAltitude, waterAltitude) : terrainAltitude;

            float altitudeGoal = altitudeBase + EditHeight;

            altitudeGoal = Math.Max(altitudeGoal, startingAltitude);
            targetAltitude = 0;

            // Bounce off ground?
            if (movement.Altitude < terrainAltitude)
            {
                BounceOffGround(thing, terrainAltitude, Vector3.UnitZ);
            }

            movement.Altitude = MyMath.Lerp(movement.Altitude, altitudeGoal, 0.1f * 30.0f * secs);

            // Apply this rotation to the actor.  This rotation just makes the character
            // spin.  It has no effect on the heading or facing direction.
            float angle = movement.RotationZ + (float)Time.GameTimeTotalSeconds * RotationRate;
            Matrix mat = Matrix.CreateRotationZ(angle);
            mat.Translation = movement.Position;
            // Note that we're just setting the rotation angle back to the 
            // original value.  The extra rotation only goes into LocalMatrix.
            movement.SetLocalMatrixAndRotation(mat, movement.RotationZ);

        }   // end of PostCollisionTestUpdate()

        #endregion

        #region Internal
        protected override void BounceOffGround(GameThing thing, float terrainHeight, Vector3 terraNormal)
        {
            Movement movement = thing.Movement;
            base.BounceOffGround(thing, terrainHeight, terraNormal);

            if (movement.Altitude < terrainHeight)
            {
                movement.Altitude = 2.0f * terrainHeight - movement.Altitude;
            }
        }

        /// <summary>
        /// Applies the values set by the brain in DesiredMovement to this chassis.
        /// </summary>
        /// <param name="movement"></param>
        /// <param name="desiredMovement"></param>
        public override void ApplyDesiredMovement(Movement movement, DesiredMovement desiredMovement)
        {
            // Apply velocity changes.
            ApplyDesiredVelocityForHover(movement, desiredMovement);

            // Apply vertical movement.
            ApplyDesiredVerticalMovement(movement, desiredMovement);

        }   // end of ApplyDesiredMovement()

        #endregion Internal

    }   // end of class SaucerChassis

}   // end of namespace Boku.SimWorld.Chassis
