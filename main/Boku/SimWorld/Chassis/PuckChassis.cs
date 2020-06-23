
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
    /// Chassis used for the Puck.
    /// </summary>
    public class PuckChassis : BaseChassis
    {
        #region Members

        public const float DefaultMaxLinearAcceleration = 0.5f;
        public const float DefaultMaxSpeed = 2.0f;
        public const float DefaultRotationRate = 0.0f;
        public const float DefaultSlopeThreshold = 0.999f;
        public const float DefaultRadius = 1.0f;
        private const float DefaultSpeedDown = 0.0f;

        private float rotationRate = DefaultRotationRate;       // radians / sec
        private float slopeThreshold = DefaultSlopeThreshold;   // Actually sine of angle.
        private float radius = DefaultRadius;                   // How far ahead we look for collision with terrain.
        private float speedDown = DefaultSpeedDown;

        #endregion

        #region Accessors

        public override bool SupportsStrafing { get { return true; } }

        /// <summary>
        /// Radians / sec
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
        /// <summary>
        /// The distance ahead of the puck's center to look for terrain collisions.
        /// </summary>
        public float Radius
        {
            get { return radius; }
            set { radius = value; }
        }

        #endregion

        #region Public

        public PuckChassis()
            : base()
        {
            MaxLinearAcceleration = DefaultMaxLinearAcceleration;
            MaxSpeed = DefaultMaxSpeed;
            RotationRate = DefaultRotationRate;
        }

        public override void InitDefaults()
        {
            base.InitDefaults();

            rotationRate = DefaultRotationRate;
            slopeThreshold = DefaultSlopeThreshold;
            radius = DefaultRadius;
            speedDown = DefaultSpeedDown;
        }

        public override void PreCollisionTestUpdate(GameThing thing)
        {
            Movement movement = thing.Movement;
            DesiredMovement desiredMovement = thing.DesiredMovement;

            GameActor.State state = thing.CurrentState;

            float secs = Time.GameTimeFrameSeconds;

            Vector3 waterNormal = Vector3.UnitZ;
            float waterAltitude = Terrain.GetWaterHeightAndNormal(movement.Position, ref waterNormal);

            // Only move if active.
            if (state == GameActor.State.Active)
            {
                movement.RotationZRate = rotationRate;

                // Now that we've updated the rotation rate, apply this to the actual rotation.
                //movement.Rotation += movement.RotationRate * secs;

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

                // If not moving, and on a slope (or a wave), give a nudge.
                if (movement.Speed < 0.1f)
                {
                    Vector3 normal = Vector3.UnitZ;
                    if(waterAltitude > 0)
                    {
                        normal = waterNormal;
                    }
                    else
                    {
                        normal = Terrain.GetNormal(movement.Position);
                    }

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
            DesiredMovement desiredMovement = thing.DesiredMovement;
            GameActor.State state = thing.CurrentState;

            // Zero out the vertical velocity, the puck ignores it.

            Vector3 velocity = movement.Velocity;
            Vector3 position = movement.Position;
            float secs = Time.GameTimeFrameSeconds;

            // Only zero out velocity.Z if we're not trying to adjust vertical.
            if (desiredMovement.CoastingVertically)
            {
                velocity.Z = 0;
            }
            else
            {
                // If we're actively moving up/down, we need to adjust height offset.
                thing.HeightOffset += velocity.Z * secs;
            }
            
            // Adjust height for terrain.  We do this outside of the check for Active 
            // so that we stay at the right height if we're being dragged around.
            // Note that for the position we use the max Z of the current and previous
            // positions.  This way we can always detect the ground without getting
            // pushed through it.
            if (position.Z < movement.PrevPosition.Z)
            {
                position.Z = movement.PrevPosition.Z;
            }
            float terrainAltitude = 0.0f;
            Vector3 terrainNormal = Vector3.UnitZ;

            // Feelers aren't valid in edit mode.
            if (state == GameThing.State.Paused)
            {
                terrainAltitude = Terrain.GetTerrainAndPathHeight(Top(position));
            }
            else
            {
                GetTerrainAltitudeAndNormalFromFeelers(movement, ref terrainAltitude, ref terrainNormal);
            }

            Vector3 waterNormal = Vector3.UnitZ;
            float waterAltitude = Terrain.GetWaterHeightAndNormal(movement.Position, ref waterNormal);

            // Decide what the altitude and normal is under us.
            float baseAltitude = 0.0f;
            Vector3 baseNormal = Vector3.UnitZ;
            if (thing.StayAboveWater)
            {
                if (terrainAltitude > waterAltitude)
                {
                    baseAltitude = terrainAltitude;
                    baseNormal = terrainNormal;
                }
                else
                {
                    baseAltitude = waterAltitude;
                    baseNormal = waterNormal;
                }
            }
            else
            {
                baseAltitude = terrainAltitude;
                baseNormal = terrainNormal;
            }

            if (waterAltitude > 0)
            {
                CheckRipples(thing, waterAltitude);
            }

            // If nothing is there or we're already under the terrain, let the bot fall.
            if (baseAltitude == 0.0f || position.Z < 0.0f)
            {
                speedDown += Gravity * secs;
                movement.Altitude += speedDown * secs;
            }
            else
            {
                float heightGoal = EditHeight + baseAltitude;

                heightGoal = Math.Max(heightGoal, targetAltitude);

                float t = Math.Min(0.2f * 30.0f * MovementSpeedModifier * secs, 1.0f);
                movement.Altitude = MyMath.Lerp(movement.Altitude, heightGoal, t);

                // Ensure we don't go under ground.
                if (movement.Altitude < baseAltitude)
                {
                    movement.Altitude = baseAltitude;
                }
            }

            if (state == GameThing.State.Active)
            {
                // Translate any change in height to a change in velocity but only do so if actually over terrain
                // and at low altitude, and not touching the ground, and not moving upward.

                if (terrainAltitude > 0.0f && position.Z > 0.0f && !impactingFloor)
                {
                    float deltaZ = movement.Position.Z - movement.PrevPosition.Z;

                    if (deltaZ != 0.0f)
                    {
                        float deltaV = (float)Math.Sqrt(Math.Abs(2.0f * Gravity * deltaZ));

                        // Get direction apply new velocity.
                        baseNormal.Z = 0.0f;
                        velocity += deltaV * baseNormal;
                    }
                }

                // If the puck has impacted the ground, reduce lateral velocity
                if (impactingFloor)
                {
                    velocity.X *= 0.9f * secs;
                    velocity.Y *= 0.9f * secs;
                }

                // Apply the continuous rotational effect.
                // Apply this rotation to the actor.  This rotation just makes the character
                // spin.  It has no effect on the heading or facing direction.
                float angle = movement.RotationZ + (float)Time.GameTimeTotalSeconds * RotationRate;
                Matrix mat = Matrix.CreateRotationZ(angle);
                mat.Translation = movement.Position;
                // Note that we're just setting the rotation angle back to the 
                // original value.  The extra rotation only goes into LocalMatrix.
                movement.SetLocalMatrixAndRotation(mat, movement.RotationZ);
            }

            // If moving too fast, damp down a bit, but only if we have a target velocity.
            // If there isn't one, just let it move freely.
            float speed = velocity.Length();

            if (speed > 0 && desiredMovement.DesiredVelocity != null && speed > desiredMovement.MaxSpeed)
            {
                float delta = (speed - desiredMovement.MaxSpeed) * secs;
                velocity *= (1 - delta);
            }

            movement.Velocity = velocity;

            // The puck has no real "heading" which is used for Move Forward.
            // To make movement work correctly, force heading to match the 
            // current moving direction.
            // We can't set heading directly so set z rotation.
            // Note that this causes the visual rotation to glitch on bouncing
            // but at least the overall physical behaviour is correct.
            movement.RotationZ = MyMath.ZRotationFromDirection(velocity);

        }   // end of PuckChassis PostCollisionTestUpdate()

        public override void SetLoopedAnimationWeights(AnimationSet anims, Movement movement, DesiredMovement desiredMovement)
        {
            float forward = movement.Speed / MaxSpeed;

            anims.IdleWeight = 1.0f - forward;
            anims.ForwardWeight = forward;
            anims.BackwardsWeight = 0.0f;
            anims.RightWeight = 0.0f;
            anims.LeftWeight = 0.0f;
        }   // end of SetLoopedAnimationWeights()

        /// <summary>
        /// Look to see if we are intersecting a water surface, and if so
        /// kick off some ripples.
        /// </summary>
        /// <param name="thing"></param>
        /// <param name="waterHeight"></param>
        protected override void CheckRipples(GameThing thing, float waterHeight)
        {
            Vector3 collCenter = thing.WorldCollisionCenter;
            float collRad = thing.WorldCollisionRadius;
            if ((waterHeight - Terrain.WaveHeight < collCenter.Z + collRad)
                && (waterHeight > collCenter.Z - collRad))
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

            // Apply vertical movement.
            ApplyDesiredVerticalMovement(movement, desiredMovement);

        }   // end of ApplyDesiredMovement()

        #endregion

    }   // end of class PuckChassis

}   // end of namespace Boku.SimWorld.Chassis
