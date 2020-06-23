using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;
using System.Diagnostics;

using Boku.Base;
using Boku.Common;
using Boku.Common.ParticleSystem;
using Boku.SimWorld;
using Boku.SimWorld.Terra;
using Boku.Animatics;

namespace Boku.SimWorld.Chassis
{
    /// <summary>
    /// Chassis used for things that hover above land but should swim under water, e.g. Octopus, etc.
    /// 
    /// Most of the movement logic taken from the HoverChassis, with the vertical velocity/pitch adjustment from SwimChassis
    /// </summary>
    public class HoverSwimChassis : BaseChassis
    {
        #region Members
        #endregion

        #region Accessors

        public override bool SupportsStrafing { get { return true; } }


        #endregion

        #region Public
        
        public HoverSwimChassis()
            : base()
        {
        }

        public override void InitDefaults()
        {
            base.InitDefaults();
        }

        //basic movement logic from hover chassis, added velocity using logic from swim chassis for when under water
        public override void PreCollisionTestUpdate(GameThing thing)
        {
            Movement movement = thing.Movement;
            DesiredMovement desiredMovement = thing.DesiredMovement;

            GameActor.State state = thing.CurrentState;

            // Are we being held?
            if (thing.ActorHoldingThis != null)
            {
                return;
            }

            float secs = Time.GameTimeFrameSeconds;

            // Calc the height we want to be at.
            float waterAltitude = Terrain.GetWaterBase(movement.PrevPosition);
            float terrainAltitude = 0.0f;
            Vector3 terrainNormal = Vector3.Zero;

            GetTerrainAltitudeAndNormalFromFeelers(movement, ref terrainAltitude, ref terrainNormal);

            // Heights are distance above ground, not absolute values.
            float heightGoal = EditHeight;
            float floor = terrainAltitude;

            Vector3 collCenter = thing.WorldCollisionCenter;

            bool underWater = false;

            // Only move if active.
            if (state == GameActor.State.Active)
            {
                // Bounce off ground?
                bool bounce = false;

                // Under water?
                if (waterAltitude > 0 && (waterAltitude - Terrain.WaveHeight) > (collCenter.Z + thing.WorldCollisionRadius))
                {
                    underWater = true;
                }

                float height = movement.Position.Z - Parent.CollisionCenter.Z - Parent.CollisionRadius - floor;

                if (allowBrainMovement)
                {
                    // Apply DesiredMovement values to current movement.
                    ApplyDesiredMovement(movement, desiredMovement);

                    // If not underwater we should ignore any velocity changes.  We still
                    // need to process them so that we get turning correct.
                    if (!underWater)
                    {
                        // Undo velocity changes.
                        movement.Velocity = movement.PrevVelocity;
                    }

                    bounce = CollideWithGround(movement, ref height);
                    
                    // Create a dust puff when bouncing off dry ground.
                    // TODO (****) This doesn't seem to actually do anything.  As far as I can
                    // tell the CreateDustPuff() call is never called by anything else so it may
                    // not work at all.  May be worth looking into if you're bored some time.
                    /*
                    if (bounce && waterAltitude == 0)
                    {
                        // Must have bounced on dry ground.  Give a puff
                        // of dust if moving fast enough.
                        if (Math.Abs(movement.Velocity.Z) > 1.0f)
                        {
                            ExplosionManager.CreateDustPuff(movement.Position, Parent.CollisionRadius, 1.0f);
                        }
                    }
                    */

                }   // end if BrainMovement

                // Apply external force.
                movement.Velocity += desiredMovement.ExternalForce.GetValueOrDefault() / thing.Mass * secs;

                // Apply drag to velocity.
                if (underWater)
                {
                    ApplyFriction(movement, desiredMovement, applyVertical: true);
                }

                // Lerp pitch based on vertical rate.
                /*
                if (desiredMovement.Coasting)
                {
                    pitch = MyMath.Lerp(pitch, 0, secs);
                }
                else
                {
                    float deltaZ = movement.Velocity.Z;
                    pitch = MyMath.Lerp(pitch, deltaZ, secs);
                }
                */

                // If not underwater then we should fall to the ground.  If we just hit the ground, skip
                // adding gravity since that will force us too deep into the ground.
                if (!underWater && !bounce)
                {
                    float s0 = (-movement.Velocity.Z + (float)Math.Sqrt(movement.Velocity.Z * movement.Velocity.Z - 4 * Gravity * height)) / (2.0f * Gravity);
                    float s1 = (-movement.Velocity.Z - (float)Math.Sqrt(movement.Velocity.Z * movement.Velocity.Z - 4 * Gravity * height)) / (2.0f * Gravity);
                    float s = MathHelper.Max(s0, s1);
                    // s will be negative when falling off the edge of the world.
                    if(float.IsNaN(s) || s < 0)
                    {
                        s = secs;
                    }
                    else
                    {
                        s = MathHelper.Min(s, secs);
                    }

                    // Calc affect of gravity using minimum of frame secs or time until we hit the ground.
                    // By using the shorter of the two we help prevent things from getting driven into the ground.
                    Vector3 velocity = movement.Velocity;
                    velocity.Z += Gravity * s;
                    movement.Velocity = velocity;
                }

                // Apply velocity to position.  We don't do this on the frame where a bounce occurs.  By
                // skipping this frame we help stabilize the behavior on the ground.  The actor sits without bouncing.
                if (!bounce)
                {
                    movement.Position += movement.Velocity * secs;
                }

                // If body is intersecting with water surface, add some splashes/ripples.
                if (waterAltitude != 0)
                {
                    // Test if the collision sphere is breaking the surface of the water.  If we falling in, create a splash.
                    // If we're just cruising along, create ripples.  Note that the extra 1.5f scaling is to make the ripples
                    // appear even if we just get close to the surface.
                    if (movement.Position.Z - Parent.CollisionRadius < waterAltitude && movement.Position.Z + Parent.CollisionRadius * 1.5f > waterAltitude)
                    {
                        CheckSplash(Parent, movement.Position, movement.Velocity);
                        CheckRipples(Parent, Parent.CollisionRadius);
                    }
                }

            }   // end of if state is active.

            // If paused we still want to apply vertical motion
            // so that in edit mode we maintain the correct height.
            if (state == GameThing.State.Paused)
            {
                Vector3 position = movement.Position;
                position.Z = floor + EditHeight;
                movement.Position = position;
            }

        }   // end of PreCollisionTestUpdate()


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

    }   // end of class HoverSwimChassis
}   // end of namespace Boku.SimWorld.Chassis
