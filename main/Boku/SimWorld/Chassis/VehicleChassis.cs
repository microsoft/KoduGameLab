
using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;
using System.Diagnostics;

using Boku.Audio;
using Boku.Base;
using Boku.Common;
using Boku.Common.ParticleSystem;
using Boku.SimWorld;
using Boku.SimWorld.Terra;
using Boku.Animatics;

namespace Boku.SimWorld.Chassis
{
    /// <summary>
    /// Chassis used for single wheeled bots, e.g. the FastBot.
    /// </summary>
    public abstract class VehicleChassis : BaseChassis
    {
        #region Members
        public const float DefaultMaxSpeed = 1.0f;
        public const float DefaultMaxLinearAcceleration = 1.0f;
        public const float DefaultMaxRotationRate = 1.0f;
        public const float DefaultMaxRotationalAcceleration = 1.0f;
        private const float DefaultDamping = 0.4f;
        private const float DefaultMaxLean = 1.0f;
        private const float DefaultCloseToGround = 0.1f;
        private const float DefaultKJumpVaration = 0.95f;

        private float damping = DefaultDamping;             // When we bounce.
        private float CloseToGround = DefaultCloseToGround; // How close we need to be to the ground to be considered "on" it.

        private bool onGround = true;                       // Are we on the ground.

        private Vector3 forward = Vector3.UnitX;            // Direction to apply forward movement.  If we're up in the air this
                                                            // may not be the same as the direction we're facing.  We can turn
                                                            // while in the air but it doesn't have any affect until we land.
        private DustEmitter dustEmitter = null;             // For dust kicked up by the wheel.

        private Vector2 jumpVelocity;                       // The 2D direction the bot is going when a jump happens
        //private float kJumpVaration = DefaultKJumpVaration; // Limit how far from the jumpVelocity the bot can go while jumping.  The closer
                                                            // to 1.0 this is the tighter the constraint.  Actual value is cosine
                                                            // of angle between vectors.

        #endregion

        #region Accessors

        public override bool SupportsStrafing { get { return false; } }

        /// <summary>
        /// Is this bot on the ground?
        /// </summary>
        public bool OnGround
        {
            get { return onGround; }
        }
        public DustEmitter DustEmitter
        {
            get { return dustEmitter; }
        }

        #endregion


        #region Public

        public VehicleChassis()
            : base()
        {
            MaxSpeed = DefaultMaxSpeed;
            MaxLinearAcceleration = DefaultMaxLinearAcceleration;
            MaxRotationRate = DefaultMaxRotationRate;
            MaxRotationalAcceleration = DefaultMaxRotationalAcceleration;

            dustEmitter = new DustEmitter(InGame.inGame.ParticleSystemManager);     // Starts in inactive state.
            dustEmitter.Active = true;
            dustEmitter.Emitting = false;
            dustEmitter.PositionJitter = 0.2f;
            dustEmitter.EmissionRate = 2.5f;
            dustEmitter.LinearEmission = true;
            dustEmitter.StartRadius = 0.45f;
            dustEmitter.EndRadius = 2.5f;
            dustEmitter.StartAlpha = 0.4f;
            dustEmitter.EndAlpha = 0.0f;
            dustEmitter.MinLifetime = 0.5f;       // Particle lifetime.
            dustEmitter.MaxLifetime = 3.0f;
        }

        /// <summary>
        /// Resets member field that change during runtime back to their initial default values
        /// as they were before customizations applied by a specific actor.
        /// </summary>
        public override void InitDefaults()
        {
            base.InitDefaults();

            // values here copied from member initializers above.
            onGround = true;
            forward = Vector3.UnitX;
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


        public override void PreCollisionTestUpdate(GameThing thing)
        {
            Movement movement = thing.Movement;
            DesiredMovement desiredMovement = thing.DesiredMovement;

            GameActor.State state = thing.CurrentState;

            float secs = Time.GameTimeFrameSeconds;

            if (state != GameActor.State.Dead || state != GameThing.State.Squashed)
            {
                if (state == GameActor.State.Active)
                {
                    // Only do movement if we are not being held.
                    if (thing.ActorHoldingThis == null)
                    {
                        // Check if we're up in the air.  If so, then we don't kick up 
                        // any dust.  We can still turn while in the air but it doesn't
                        // affect the direction we're moving until we hit the ground.
                        float terrainHeight = Terrain.GetTerrainAndPathHeight(Top(movement.Position));
                        bool falling = terrainHeight == 0.0f;
                        float heightAboveGround = movement.Altitude - terrainHeight - MinHeight;

                        if (landing)
                        {
                            // This version doesn't clamp movement to only work forward and backward.
                            ApplyDesiredMovementWhenFalling(movement, desiredMovement);
                        }
                        else
                        {
                            ApplyDesiredMovement(movement, desiredMovement);
                        }

                        // Are we waiting to land?
                        if (landing && movement.Velocity.Z < 0.0f)
                        {
                            // Are we close enough?
                            float predictedHeight = heightAboveGround + movement.Velocity.Z * preLandDelay;
                            if (predictedHeight < CloseToGround)
                            {
                                startLandAnimation = true;
                                landing = false;
                                if (TerrainDataValid && Feelers != null)
                                {
                                    Foley.PlayCollision(thing, Feelers[0].TerrainMaterialInfo.TerrainType);
                                }
                                else
                                {
                                    Foley.PlayCollision(thing, 0);
                                }
                            }
                        }

                        onGround = heightAboveGround < CloseToGround;
                        onGround = onGround && !falling;
                        if (onGround)
                        {
                            // The bot is on the ground.

                            // Only jump if on the ground and not already jumping.
                            if (Jump && !jumping && !landing)
                            {
                                startJumpAnimation = true;      // Tell animation to start.
                                jumping = true;
                                jumpStartTime = Time.GameTimeTotalSeconds;

                                jumpVelocity = new Vector2(movement.Velocity.X, movement.Velocity.Y);
                                float len = jumpVelocity.Length();
                                if (len > 0.1f)
                                {
                                    jumpVelocity /= len;
                                }
                                else
                                {
                                    jumpVelocity = new Vector2(movement.Facing.X, movement.Facing.Y);
                                    jumpVelocity.Normalize();
                                }
                            }
                        }

                        // Always clear jump flag.
                        Jump = false;

                        if (jumping && !landing)
                        {
                            // If the pre delay time has passed, do the jump.
                            if (Time.GameTimeTotalSeconds > jumpStartTime + preJumpDelay)
                            {
                                movement.Velocity += new Vector3(0, 0, effectiveJumpStrength);
                                jumping = false;
                                landing = true;
                            }
                        }

                        HandleMovement(movement);

                        // Apply drag to velocity.
                        ApplyFriction(movement, desiredMovement, applyVertical: false);

                        // Apply external force.
                        movement.Velocity += desiredMovement.ExternalForce.GetValueOrDefault() / thing.Mass * secs;

                        // Move due to velocity.
                        movement.Position += movement.Velocity * secs;

                    }   // end of if not held.

                }   // end of if active.


            }   // end of if not dead

        }   // end of PreCollisionTestUpdate()

        public override void PostCollisionTestUpdate(GameThing thing)
        {
            Movement movement = thing.Movement;
            GameActor.State state = thing.CurrentState;

            if (state != GameActor.State.Dead && state != GameThing.State.Squashed && thing.ActorHoldingThis == null)
            {
                // Start with existing values.
                Vector3 velocity = movement.Velocity;
                Vector3 position = movement.Position;
                float secs = Time.GameTimeFrameSeconds;

                // Adjust Actor's height to follow the terrain.
                // Add in effect of gravity.
                if (state != GameThing.State.Paused)
                {
                    velocity.Z += Gravity * secs;
                }
                position.Z += velocity.Z * secs;

                // See if we bounce.
                float terrainHeight = Terrain.GetTerrainAndPathHeight(Top(position));
                if (terrainHeight > 0.0f)
                {
                    float desiredHeight = terrainHeight;
                    if (desiredHeight > position.Z)
                    {
                        position.Z = desiredHeight;
                        velocity.Z = -velocity.Z * damping;
                    }
                }
                else
                {
                    // There is no terrain or path below the bot.  Check if we fell through a path.

                    // Only test if we're not already below the world.
                    if (movement.PrevPosition.Z > 0.0f)
                    {
                        // Were we previously over a path?
                        terrainHeight = Terrain.GetTerrainAndPathHeight(Top(movement.PrevPosition));
                        if (terrainHeight > 0.0f)
                        {
                            // Bounce if below height of path.  This effectively extends the 
                            // width of the path by 1 frame of movement which allows this to work
                            // independent of the thickness of the path.
                            float desiredHeight = terrainHeight;
                            if (desiredHeight > position.Z)
                            {
                                position.Z = desiredHeight;
                                velocity.Z = -velocity.Z * damping;
                            }
                        }
                    }
                }
                Matrix local = GenerateRotationMatrix(movement);
                local.Translation = position;

                // Copy results back to movement.
                movement.Position = position;
                movement.Velocity = velocity;

                //movement.LocalMatrix = local;
                movement.SetLocalMatrixAndRotation(local, movement.RotationZ);

                float waterHeight = Terrain.GetWaterBase(position);
                if (waterHeight > 0)
                {
                    CheckRipples(thing, waterHeight);
                }

                dustEmitter.Scale = thing.ReScale;
                dustEmitter.PreviousPosition = movement.PrevPosition;
                dustEmitter.Position = position;
                dustEmitter.Emitting = !thing.Invisible && OnGround && (waterHeight <= 0);
            }   // end if not dead or held.

        }   // end of PostCollisionTestUpdate()

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
        /// Allow subclasses to handle special logic for adjusting based on terrain
        /// </summary>
        /// <param name="movement"></param>
        protected virtual void HandleMovement(Movement movement)
        {        
        }

        /// <summary>
        /// Allow subclasses to handle special logic for generating rotation of the object
        /// </summary>
        /// <param name="movement"></param>
        protected virtual Matrix GenerateRotationMatrix(Movement movement)
        {
            //base behaviour to just rotate in the direction we're looking
            return Matrix.CreateRotationZ(movement.RotationZ);
        }

        /// <summary>
        /// Look to see whether we are intersecting the surface of the water.
        /// Also look if it makes sense to kick up a splash.
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

                if (Math.Abs(waterHeight - Terrain.WaveHeight * 0.5f - thing.Movement.Position.Z) < 0.4f)
                {
                    Vector3 vel = thing.Movement.Velocity;
                    vel = -vel;
                    vel.Z = vel.Length();
                    CheckSplash(thing, thing.Movement.Position, vel);
                }
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

            // Clamp velocity to forward/backward.  Don't mess with vertical movement.
            // Calc facing dir without any Z component.
            Vector3 forward = movement.Facing * new Vector3(1, 1, 0);
            forward.Normalize();
            float dot = Vector3.Dot(forward, movement.Velocity);
            Vector3 vel = dot * forward;
            movement.Velocity = new Vector3(vel.X, vel.Y, movement.Velocity.Z);

        }   // end of ApplyDesiredMovement()

        /// <summary>
        /// Applies the values set by the brain in DesiredMovement to this chassis.
        /// Assume the bot is falling so only rotation changes apply.
        /// </summary>
        /// <param name="movement"></param>
        /// <param name="desiredMovement"></param>
        public void ApplyDesiredMovementWhenFalling(Movement movement, DesiredMovement desiredMovement)
        {
            // Apply velocity changes.  Sort of.  We need this to affect the
            // rotation of the bot but, since we're falling, the velocity shouldn't change.
            Vector3 velocity = movement.Velocity;
            ApplyDesiredVelocityForHover(movement, desiredMovement);
            movement.Velocity = velocity;   // Restore saved value.

            // Apply rotation changes.
            ApplyDesiredRotation(movement, desiredMovement);

            // Note that we don't clamp velocity to facing direction here.
            // This means that we can spin in the air without changing our velocity.

        }   // end of ApplyDesiredMovementWhenFalling()

        #endregion Internal

    }   // end of class CycleChassis

}   // end of namespace Boku.SimWorld.Chassis
