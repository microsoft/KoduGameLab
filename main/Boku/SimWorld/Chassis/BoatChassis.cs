
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

using KoiX;

namespace Boku.SimWorld.Chassis
{
    /// <summary>
    /// Chassis used for boats that float.
    /// </summary>
    public class BoatChassis : BaseChassis
    {
        #region Members
        public const float DefaultHullDraft = 0.3f;

        // Limits
        private float hullDraft = DefaultHullDraft;

        private float speed = 0.0f;
        private bool grounded = false;                      // Are we high and dry?  This should be set when the level 
                                                            // is reset and stay constant throughout the run unless the
                                                            // bot is picked up and carried.
        private float velocityZ = 0.0f;                     // Temp storage for velocity.Z while terrain and glass wall hits
                                                            // are being done since we don't want them to affect this.

        #endregion

        #region Accessors

        public override bool SupportsStrafing { get { return false; } }

        public float HullDraft
        {
            get { return hullDraft; }
            set { hullDraft = value; }
        }

        #endregion

        #region Public

        public override void InitDefaults()
        {
            base.InitDefaults();

            hullDraft = DefaultHullDraft;
            speed = 0.0f;
            grounded = false;
            velocityZ = 0.0f;
        }

        public override void PreCollisionTestUpdate(GameThing thing)
        {
            Movement movement = thing.Movement;
            DesiredMovement desiredMovement = thing.DesiredMovement;

            GameActor.State state = thing.CurrentState;

            // Are we being held?
            if (thing.ActorHoldingThis != null)
            {
                grounded = false;
                return;
            }


            float secs = Time.GameTimeFrameSeconds;
            Vector3 position = movement.Position;

            Vector3 bow = position + 0.9f * movement.Facing;

            // Test for grounding.
            {
                float waterAltitude = Terrain.GetWaterHeight(position);
                float terrainAltitude = Terrain.GetTerrainAndPathHeight(Top(position));
                grounded = (terrainAltitude > 0) && (terrainAltitude > waterAltitude - HullDraft);
            }


            if (state == GameActor.State.Active)
            {
                if (!grounded)
                {
                    // Apply DesiredMovement values to current movement.
                    ApplyDesiredMovement(movement, desiredMovement);

                    if (Jump && CanJump() && !jumping && !landing)
                    {
                        startJumpAnimation = true;      // Tell animation to start.
                        jumping = true;
                        jumpStartTime = Time.GameTimeTotalSeconds;
                    }
                    Jump = false;

                    if (jumping)
                    {
                        // If the pre delay time has passed, do the jump.
                        if (Time.GameTimeTotalSeconds > jumpStartTime + preJumpDelay)
                        {
                            movement.Velocity += new Vector3(0, 0, effectiveJumpStrength);
                            jumping = false;
                            landing = true;
                            lastJumpTime = Time.GameTimeTotalSeconds;
                        }
                    }

                    // Apply external force.
                    movement.Velocity += desiredMovement.ExternalForce.GetValueOrDefault() / thing.Mass * secs;

                    // Apply drag to velocity.
                    ApplyFriction(movement, desiredMovement, applyVertical: true);

                    // Apply velocity to position.
                    movement.Position += movement.Velocity * secs;

                }   // end if not grounded.
                else
                {
                    // We're grounded but we may be high enough to stil be falling.
                    // Apply velocity to position.
                    movement.Position += movement.Velocity * secs;
                }

            }   // End of if active.
            else
            {
                // Not active but we still want to be able to move vertically
                // if the water level is changing in the editor.
                //movement.Altitude += movement.Velocity.Z * Time.WallClockFrameSeconds;
                //grounded = false;
            }

            // Check for hitting the ground.  Only do this if we're active and not fully grounded.
            if (!grounded && (state != GameActor.State.Dead || state != GameActor.State.Squashed))
            {
                bow = position + 0.9f * movement.Facing;
                float waterBase = Terrain.GetWaterBase(bow);
                float terrainAltitude = Terrain.GetHeight(bow);
                if (terrainAltitude < waterBase)
                {
                    // Only slide if active.
                    if (state == GameActor.State.Active)
                    {
                        float waterAltitude = Terrain.GetWaterHeight(bow);

                        // Are we hitting ground?  If so, reduce speed and slide back a bit.
                        if (position.Z - hullDraft < terrainAltitude)
                        {
                            // Stuck.
                            speed *= 1.0f - secs;
                            speed = 0.0f;

                            // Slide down hill a bit.
                            Vector3 terrainNormal = Terrain.GetNormal(movement.Position);
                            terrainNormal.Z = 0.0f;
                            movement.Position += terrainNormal * 2.0f * secs;
                            movement.Velocity = terrainNormal;
                        }
                    }
                }
            }   // End of if not grounded or dead.

            velocityZ = movement.Velocity.Z;
        }   // end of PreCollisionTestUpdate()

        public override void PostCollisionTestUpdate(GameThing thing)
        {
            Movement movement = thing.Movement;
            GameActor.State state = thing.CurrentState;

            // Restore Z velocity.
            //Vector3 velocity = movement.Velocity;
            //velocity.Z = velocityZ;

            Vector3 surfaceNormal = Vector3.UnitZ;
            if (state == GameActor.State.Active && thing.ActorHoldingThis == null)
            {

                // Now that we have the updated position information, create the actor's local transform.
                float secs = Time.GameTimeFrameSeconds;
                float terrainHeight = Terrain.GetTerrainAndPathHeight(Top(movement.Position));

                if (!grounded)
                {
                    if (terrainHeight > 0.0f)
                    {
                        // Adjust Actor's height to follow the water surface.
                        Vector3 waterSurfaceNormal = Vector3.UnitZ;
                        float waterHeight = Terrain.GetWaterHeightAndNormal(movement.Position, ref waterSurfaceNormal);

                        float z = Math.Max(waterHeight, terrainHeight);

                        // Falling?
                        if (movement.Altitude > z)
                        {
                            movement.Velocity += new Vector3(0, 0, Gravity * secs);

                            // Are we waiting to land?
                            if (landing)
                            {
                                // Are we close enough?
                                float predictedAlt = movement.Position.Z + movement.Velocity.Z * preLandDelay;
                                if (predictedAlt < z)
                                {
                                    startLandAnimation = true;
                                    landing = false;
                                }
                            }

                        }
                        else
                        {
                            movement.Altitude = z;
                            // Zero out vertical velocity.
                            movement.Velocity *= new Vector3(1, 1, 0);
                        }

                        surfaceNormal = waterSurfaceNormal;
                    }
                    else
                    {
                        // Falling off the edge of the world.
                        movement.Velocity += new Vector3(0, 0, Gravity * secs);
                    }

                    // Only do ripples if on the suface.
                    // TODO (****) Should we also add a splash effect upon landing?
                    if (!landing)
                    {
                        CheckRipples(thing, thing.CollisionRadius * 0.75f);
                    }
                }
                else
                {
                    // Fall to ground.
                    if (movement.Altitude > terrainHeight)
                    {
                        movement.Velocity += new Vector3(0, 0, Gravity * secs);
                    }
                    else
                    {
                        // Pop up to level ground.
                        movement.Altitude = terrainHeight;
                    }

                    surfaceNormal = Terrain.GetNormal(movement.Position);
                }

                if (grounded)
                {
                    // Create a matrix orienting the actor with the surface it's on.
                    // TODO (****) This would be nicer if it lerped into place so
                    // that sliding from steep terrain onto water didn't pop.
                    Vector3 forward = new Vector3(1.0f, 0.0f, 0.0f);
                    Vector3 left = Vector3.Cross(surfaceNormal, forward);
                    left.Normalize();
                    forward = Vector3.Cross(left, surfaceNormal);
                    Matrix local = Matrix.Identity;

                    // Ok, this looks strange.  This is because XNA GS assumes Y is up and -Z is forward.  Argh.
                    local.Backward = surfaceNormal;
                    local.Up = left;
                    local.Right = forward;

                    // Add in Z rotation.
                    local = Matrix.CreateRotationZ(movement.RotationZ) * local;

                    // And tranlate to the right place.
                    local.Translation = movement.Position;

                    // Finally, set the local matrix explicitly
                    //movement.LocalMatrix = local;
                    Matrix oldLocal = movement.LocalMatrix;
                    movement.SetLocalMatrixAndRotation(MyMath.Lerp(ref oldLocal, ref local, secs), movement.RotationZ);
                }
                else
                {
                    // Start with Z rotation.
                    Matrix local = Matrix.CreateRotationZ(movement.RotationZ);

                    // And tranlate to the right place.
                    local.Translation = movement.Position;

                    // Finally, set the local matrix explicitly
                    movement.SetLocalMatrixAndRotation(local, movement.RotationZ);
                }
            }

        }   // end of PostCollisionTestUpdate()

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

        /// <summary>
        /// Applies the values set by the brain in DesiredMovement to this chassis.
        /// </summary>
        /// <param name="movement"></param>
        /// <param name="desiredMovement"></param>
        public override void ApplyDesiredMovement(Movement movement, DesiredMovement desiredMovement)
        {
            // For boats we want them to be able to turn BUT they shouldn't be able
            // to move sideways.  So, we need to calc the rotation as usual but
            // the velocity should only be applied based on the direction the bot
            // is facing.

            // Apply velocity changes.
            ApplyDesiredVelocityForHover(movement, desiredMovement);

            // Apply rotation changes.
            ApplyDesiredRotation(movement, desiredMovement);

            // Clamp velocity to forward/backward.  Don't mess with vertical movement.
            // Calc facing dir without any Z component.
            Vector3 forward = movement.Facing * new Vector3(1, 1, 0);
            forward.Normalize();
            float dot = Vector3.Dot(forward, movement.Velocity);
            Vector3 vel = dot * forward;
            movement.Velocity = new Vector3(vel.X, vel.Y, movement.Velocity.Z);

        }   // end of ApplyDesiredMovement()

        #endregion

    }   // end of class BoatChassis

}   // end of namespace Boku.SimWorld.Chassis
