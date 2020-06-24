
using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;
using System.Diagnostics;

using KoiX;

using Boku.Base;
using Boku.Common;
using Boku.SimWorld;
using Boku.SimWorld.Terra;
using Boku.Animatics;

namespace Boku.SimWorld.Chassis
{
    /// <summary>
    /// Chassis used for things that float in the air, e.g. ballons, clouds, etc.
    /// This chassis expects to float at a given altitude rather than at a given
    /// height.
    /// </summary>
    public class FloatInAirChassis : BaseChassis
    {
        #region Members
        public const float DefaultMaxLinearAcceleration = 0.1f;
        public const float DefaultMaxRotationalAcceleration = 0.1f;
        public const float DefaultMaxSpeed = 1.0f;
        public const float DefaultMaxRotationRate = 1.0f;
        public const float DefaultMaxAltitude = 50.0f;
        private const float DefaultStartingAltitude = Single.MinValue;
        public const float DefaultMaxVerticalSpeed = 1.0f;
        public const float DefaultMaxVerticalAcceleration = 1.0f;
        public const float DefaultVerticalSpeedMultiplier = 1.0f;

        // Limits
        private float maxAltitude = DefaultMaxAltitude;                         // Max altitude in meters.

        private float startingAltitude = DefaultStartingAltitude;               // The altitude we're trying to maintain.
        private float deltaAltitude = 0.0f;                                     // How fast we're currently moving up or down.

        //
        // TODO (scoy)
        // These speed and acceleration limits are a bit arbitrary.
        // Should we use actor.CalcMaxVerticalSpeed() and 
        // actor.CalcMaxVewrticalAcceleration() instead?
        //

        private float maxVerticalSpeed = DefaultMaxVerticalSpeed;               // How fast can we go up and down.
        private float maxVerticalAcceleration = DefaultMaxVerticalAcceleration; // How fast can we accelerate up and down.
        private float verticalSpeedMultiplier = DefaultVerticalSpeedMultiplier; // Multiplier controller by Quickly and Slowly tiles.

        #endregion 

        #region Accessors

        public override bool SupportsStrafing { get { return true; } }

        public float MaxAltitude
        {
            get { return maxAltitude; }
            set { maxAltitude = value; }
        }
        public float MaxVerticalAcceleration
        {
            get { return maxVerticalAcceleration; }
            set { maxVerticalAcceleration = value; }
        }
        public float MaxVerticalSpeed
        {
            get { return maxVerticalSpeed; }
            set { maxVerticalSpeed = value; }
        }

        /// <summary>
        /// Applied to vertical speed and acceleration as controlled by
        /// Quickly and Slowly tiles.
        /// </summary>
        public float VerticalSpeedMultiplier
        {
            get { return verticalSpeedMultiplier; }
            set { verticalSpeedMultiplier = value; }
        }

        #endregion

        #region Public

        public FloatInAirChassis()
            : base()
        {
            MaxLinearAcceleration = DefaultMaxLinearAcceleration;
            MaxRotationalAcceleration = DefaultMaxRotationalAcceleration;
            MaxSpeed = DefaultMaxSpeed;
            MaxRotationRate = DefaultMaxRotationRate;
        }

        public override void InitDefaults()
        {
            base.InitDefaults();

            maxAltitude = DefaultMaxAltitude;
            startingAltitude = DefaultStartingAltitude;
            deltaAltitude = 0.0f;
            maxVerticalSpeed = DefaultMaxVerticalSpeed;
            maxVerticalAcceleration = DefaultMaxVerticalAcceleration;
        }

        public override void PreCollisionTestUpdate(GameThing thing)
        {
            Movement movement = thing.Movement;
            DesiredMovement desiredMovement = thing.DesiredMovement;

            GameActor.State state = thing.CurrentState;

            float secs = Time.GameTimeFrameSeconds;

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
            }
        }

        public override void PostCollisionTestUpdate(GameThing thing)
        {
            Movement movement = thing.Movement;
            GameActor.State state = thing.CurrentState;

            float secs = Time.GameTimeFrameSeconds;

            if (state == GameActor.State.Active)
            {
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
                        // If we're moving up/down, assume currentl altitude is the new target.
                        startingAltitude = movement.Altitude;
                    }
                }

                //
                // From here down is "old" code (pre movement refactor) that still seems to have
                // the right feel so just leave it.
                //

                // Calc the min height so that we don't hit the ground (or water).
                // Do some look-ahead so we better handle cliffs.
                float lookAheadHeight = Terrain.GetTerrainAndPathHeight(Top(movement.Position + movement.Facing * 3.0f));
                float terrainHeight = Terrain.GetTerrainAndPathHeight(Top(movement.Position));
                float waterHeight = Terrain.GetWaterBase(movement.Position);

                if (waterHeight > 0)
                {
                    CheckRipples(thing, waterHeight);
                }

                float minimumHeight = MathHelper.Max(terrainHeight, lookAheadHeight);
                if (thing.StayAboveWater)
                {
                    minimumHeight = MathHelper.Max(minimumHeight, waterHeight);
                }
                minimumHeight += MinHeight;
                minimumHeight += VerticalStoppingDistance();

                bool tooLow = movement.Altitude < minimumHeight;
                bool tooHigh = movement.Altitude > maxAltitude;

                float goalAltitude = Math.Max(startingAltitude, minimumHeight);
                if (targetAltitude > 0)
                {
                    goalAltitude = Math.Max(minimumHeight, targetAltitude);
                    targetAltitude = 0;
                }

                // Add a bit of a dead zone around the target altitude.
                float targetCushion = 0.5f;

                if (tooLow || movement.Altitude < goalAltitude - targetCushion)
                {
                    // have increased acceleration upward when near ground.
                    float scaleFactor = tooLow ? 2.0f : 1.0f;

                    // Accelerate upward.
                    deltaAltitude += maxVerticalAcceleration * scaleFactor * secs * VerticalSpeedMultiplier;
                    // Clamp
                    if (deltaAltitude > maxVerticalSpeed * VerticalSpeedMultiplier)
                    {
                        deltaAltitude = maxVerticalSpeed * VerticalSpeedMultiplier;
                    }
                }
                else if (tooHigh || movement.Altitude > goalAltitude + targetCushion)
                {
                    // Accelerate downward.
                    deltaAltitude -= maxVerticalAcceleration * secs * VerticalSpeedMultiplier;
                    // Clamp
                    if (deltaAltitude < -maxVerticalSpeed * 0.7f * VerticalSpeedMultiplier)
                    {
                        deltaAltitude = -maxVerticalSpeed * 0.7f * VerticalSpeedMultiplier;
                    }
                }
                else
                {
                    // If not going up or down, damp out vertical acceleration.
                    deltaAltitude *= 1.0f - secs;
                }

                movement.Altitude += deltaAltitude * secs;

            }   // end of if active
            else
            {
                // Still need to adjust the height if in edit mode.
                float terrainHeight = Terrain.GetTerrainAndPathHeight(Top(movement.Position));
                if (thing.StayAboveWater)
                {
                    float waterHeight = Terrain.GetWaterBase(movement.Position);
                    terrainHeight = MathHelper.Max(terrainHeight, waterHeight);
                }
                terrainHeight += EditHeight;
                movement.Altitude = terrainHeight;
            }

        }   // end of PreCollisionTestUpdate()

        public override void SetLoopedAnimationWeights(AnimationSet anims, Movement movement, DesiredMovement desiredMovement)
        {
            float idleWeight = 0.0f;
            float forwardWeight = 0.0f;
            float backwardWeight = 0.0f;
            float leftWeight = 0.0f;
            float rightWeight = 0.0f;

            // Blend animations based only on relationship between 
            // actual facing direction and desired heading.  This
            // ignores current speed or rotation rate.
            if (desiredMovement.DesiredVelocity.HasValue || desiredMovement.DesiredTargetLocation.HasValue)
            {
                Vector3 facing = movement.Facing;
                Vector3 desiredVelocity = facing;  // Value will be overwritten.

                if (desiredMovement.DesiredVelocity.HasValue)
                {
                    // Trying to move in explicit direction.
                    desiredVelocity = desiredMovement.DesiredVelocity.Value;
                }

                if (desiredMovement.DesiredTargetLocation.HasValue)
                {
                    // Trying to move toward target.
                    desiredVelocity = desiredMovement.DesiredTargetLocation.Value - movement.Position;
                }

                desiredVelocity.Normalize();
                if (!float.IsNaN(desiredVelocity.X))
                {
                    forwardWeight = Math.Max(0.0f, Vector3.Dot(desiredVelocity, facing));

                    Vector3 right = Vector3.Cross(facing, Vector3.UnitZ);
                    right.Normalize();

                    rightWeight = Math.Max(0.0f, Vector3.Dot(right, desiredVelocity));
                    leftWeight = Math.Max(0.0f, Vector3.Dot(-right, desiredVelocity));
                }
            }
            else if (desiredMovement.DesiredRotationAngle.HasValue || desiredMovement.DesiredRotationRate.HasValue)
            {
                // If we aren't setting values based on velocity changes, maybe set them based on turning.
                float curHeading = movement.RotationZ;
                float desiredHeading = curHeading;  // Value will be overwritten.

                if (desiredMovement.DesiredRotationAngle.HasValue)
                {
                    desiredHeading = desiredMovement.DesiredRotationAngle.Value;
                }

                if (desiredMovement.DesiredRotationRate.HasValue)
                {
                    desiredHeading = curHeading + Math.Sign(desiredMovement.DesiredRotationRate.Value);
                }

                float delta = curHeading - desiredHeading;
                delta = MathHelper.WrapAngle(delta);
                if (delta > 0.0f)
                {
                    rightWeight = Math.Min(delta / MathHelper.PiOver2, 1.0f);
                }
                else
                {
                    leftWeight = Math.Min(-delta / MathHelper.PiOver2, 1.0f);
                }
            }

            // Bias weights toward either full or off with a 10% flat area.
            forwardWeight = MyMath.SmoothStep(0.1f, 0.9f, forwardWeight);
            rightWeight = MyMath.SmoothStep(0.1f, 0.9f, rightWeight);
            leftWeight = MyMath.SmoothStep(0.1f, 0.9f, leftWeight);

            // Adjust weights to sum to 1.0.
            float total = forwardWeight + rightWeight + leftWeight;
            if (total > 1.0f)
            {
                forwardWeight /= total;
                leftWeight /= total;
                rightWeight /= total;
            }
            else
            {
                // Fill in with idle.
                idleWeight = 1.0f - total;

                // If there's any idle, see how much of it should be wind.
                if(idleWeight > 0.0f)
                {
                    // For this chassis we assume that the backwards animation represents wind.
                    float wind = BokuGame.bokuGame.shaderGlobals.WindAt(movement.Position);

                    if (wind > idleWeight)
                    {
                        backwardWeight = idleWeight;
                        idleWeight = 0.0f;
                    }
                    else
                    {
                        backwardWeight = wind;
                        idleWeight = idleWeight - wind;
                    }
                }
            }

            // Set resulting weights on animation set.
            anims.IdleWeight = idleWeight;
            anims.ForwardWeight = forwardWeight;
            anims.BackwardsWeight = backwardWeight;
            anims.RightWeight = rightWeight;
            anims.LeftWeight = leftWeight;

        }   // end of SetLoopedAnimationWeights()

        #endregion

        #region Internal
        /// <summary>
        /// Compute how long it would take to stop moving down if we hit the brakes now.
        /// We use this to determine when to stop moving down to avoid hitting the terrian.
        /// </summary>
        /// <returns></returns>
        private float VerticalStoppingDistance()
        {
            if (deltaAltitude < 0)
            {
                float currentSpeed = deltaAltitude;
                float stopTime = -currentSpeed / maxVerticalAcceleration;

                float stopDist = -currentSpeed * stopTime - 0.5f * maxVerticalAcceleration * stopTime * stopTime;

                return stopDist;
            }
            return 0.0f;
        }

        /// <summary>
        /// Look to see if we're intersecting the water surface and if so kick off
        /// some ripples. Note that we look farther below than above, so if we're
        /// flying low over water we make a wake.
        /// </summary>
        /// <param name="thing"></param>
        /// <param name="waterHeight"></param>
        protected override void CheckRipples(GameThing thing, float waterHeight)
        {
            Vector3 collCenter = thing.WorldCollisionCenter;
            float collRad = thing.WorldCollisionRadius;

            if ((waterHeight - Terrain.WaveHeight < collCenter.Z + collRad)
                && (waterHeight > collCenter.Z - collRad * 1.5f))
            {
                base.CheckRipples(thing, collRad);
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

            // Apply rotation changes.
            ApplyDesiredRotation(movement, desiredMovement);

            // Apply vertical movement.
            ApplyDesiredVerticalMovement(movement, desiredMovement);

        }   // end of ApplyDesiredMovement()

        #endregion Internal

    }   // end of class FloatInAirChassis

}   // end of namespace Boku.SimWorld.Chassis
