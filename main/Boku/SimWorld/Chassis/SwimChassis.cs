
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
using Boku.Audio;

namespace Boku.SimWorld.Chassis
{
    /// <summary>
    /// Chassis used for things that swim.
    /// </summary>
    public class SwimChassis : BaseChassis
    {
        #region Members 
        public const float DefaultMaxRotationRate = 2.0f;           
        public const float DefaultMaxRotationalAcceleration = 3.0f;
        public const float DefaultMaxSpeed = 2.0f;                  
        public const float DefaultMaxLinearAcceleration = 2.0f;     
        public const float DefaultMaxLinearDeceleration = 2.0f;
        public const float DefaultHullDraft = 1.3f;
        public const float DefaultMinDepth = -0.6f;
        public const float DefaultMaxPitch = 0.5f;
        public const float DefaultBodyFlex = 0.0f;
        public const float DefaultFlexAmplitude = 1.0f;
        public const float DefaultKJumpVaration = 0.95f;

        private float hullDraft = DefaultHullDraft;                     // This is the distance between the origin of the model and the bottom of the body.
        private float minDepth = DefaultMinDepth;                       // Controls how close to the surface the actor can get.  Treat depth as a signed
                                                                        // value that is the bot's altitude - water altitude.  So a negative value means
                                                                        // underwater.

        // Limits
        private float maxPitch = DefaultMaxPitch;                       // radians above and below horizontal

        // Current values.
        private float pitch = 0.0f;                                     // radians, positive is nose up.

        private float waveOffset = 0.0f;                                // Height offset caused by waves.  Note this is added to the position at the end
                                                                        // ofthe update for rendering and then removed at the beginning for update.

        private Vector2 m_JumpDir = Vector2.Zero;                  // The 2D direction the bot is going when a jump happens
  
        private float bodyFlex = DefaultBodyFlex;                       // Value used to flex body.
        private float flexAmplitude = DefaultFlexAmplitude;             // Attenuation applied to constant sine wave.  Used to damp sine wave when fish is turning.
        private float flexOffset = 10.0f * MathHelper.Pi * (float)BokuGame.bokuGame.rnd.NextDouble();   // Just so all the fish aren't in sync.

        private Vector3 botNormal = Vector3.UnitZ;                      // Normal for bot's body as affected by grounding and/or waves.

        #endregion

        #region Accessors

        public override bool SupportsStrafing { get { return false; } }

        public float HullDraft
        {
            get { return hullDraft; }
            set { hullDraft = value; }
        }
        public float MinDepth
        {
            get { return minDepth; }
            set { minDepth = value; }
        }
        public float MaxPitch
        {
            get { return maxPitch; }
            set { maxPitch = value; }
        }

        /// <summary>
        /// This is the flex in hte bodyu due to turning.
        /// </summary>
        public float BodyFlex
        {
            get { return bodyFlex; }
        }

        /// <summary>
        /// This is the flex int he body due to normal swimming.
        /// </summary>
        public float FlexAmplitude
        {
            get { return flexAmplitude; }
            set { flexAmplitude = value; }
        }

        #endregion

        #region Public

        public SwimChassis()
            : base()
        {
            MaxRotationRate = DefaultMaxRotationRate;                     // radians per second
            MaxRotationalAcceleration = DefaultMaxRotationalAcceleration; // radians per second^2

            MaxSpeed = DefaultMaxSpeed;                                   // meters per second
            MaxLinearAcceleration = DefaultMaxLinearAcceleration;         // meters per second^2
            MaxLinearDeceleration = DefaultMaxLinearDeceleration;         // meters per second^2

        }

        public override void InitDefaults()
        {
            base.InitDefaults();

            hullDraft = DefaultHullDraft;
            minDepth = DefaultMinDepth;
            maxPitch = DefaultMaxPitch;
            pitch = 0.0f;
            waveOffset = 0.0f;
            bodyFlex = DefaultBodyFlex;
            flexAmplitude = DefaultFlexAmplitude;
            flexOffset = 10.0f * MathHelper.Pi * (float)BokuGame.bokuGame.rnd.NextDouble();
            botNormal = Vector3.UnitZ;
        }

        public override void PreCollisionTestUpdate(GameThing thing)
        {
            Movement movement = thing.Movement;
            DesiredMovement desiredMovement = thing.DesiredMovement;

            GameActor.State state = thing.CurrentState;

            //string DebugString = "------------- Debug Fish: "+ thing.CreatableName +" --------------";

            float secs = Time.GameTimeFrameSeconds;

            // Only do movement if we are not being held and active.
            if (state == GameActor.State.Active && thing.ActorHoldingThis == null)
            {
                // Test for grounding.  
                float terrainAltitude = Terrain.GetHeight(movement.Position);
                float waterBaseAltitude = Terrain.GetWaterBase(movement.Position);
                    
                Vector3 waterNormal = Vector3.UnitZ;
                float waterAltitude = (waterBaseAltitude > 0) ? Terrain.GetWaterHeightAndNormal(movement.Position, ref waterNormal) : 0.0f;

                // Note that grounded tells us if there's too little water to float.
                // grounded should be false if there's no terrain under the bot.
                float groundedAmount = terrainAltitude - waterAltitude - (ReScale * HullDraft);
                bool grounded = terrainAltitude != 0 && (waterAltitude == 0.0f || groundedAmount > 0.0f);
                Vector3 groundedNormal = grounded ? Terrain.GetNormal(movement.Position) : Vector3.UnitZ;

                // Undo wave offset.
                movement.Position -= new Vector3(0, 0, waveOffset);

                if (!grounded)
                {
                    // Apply DesiredMovement values to current movement.
                    ApplyDesiredMovement(movement, desiredMovement);

                    bodyFlex = movement.RotationZRate / TurningSpeedModifier * 0.5f;
                    float newFlexAmp = MaxRotationRate - Math.Abs(movement.RotationZRate / TurningSpeedModifier);
                    flexAmplitude = MyMath.Lerp(flexAmplitude, newFlexAmp * newFlexAmp, secs);     // Wiggle less when turning.

                    // Apply drag to velocity.
                    ApplyFriction(movement, desiredMovement, applyVertical: true);

                }   // end if not grounded.


                // Now see if we're stuck or hitting shore.
                if (grounded && !(jumping || landing))
                {
                    Vector3 terrainNormal = groundedNormal;

                    // Grounded tells us there's too little water to float, now check if we're 
                    // actually touching the ground.
                    float closeToGround = 0.1f;
                    if (movement.Position.Z - hullDraft - terrainAltitude < closeToGround)
                    {
                        // Slide if shallow.
                        if (groundedAmount < 1.0f)
                        {
                            // Slide down hill a bit.
                            terrainNormal.Z = 0.0f;
                            movement.Position += 10.0f * terrainNormal * secs;
                            movement.Velocity = 10.0f * terrainNormal * secs;
                        }
                        else
                        {
                            // High and dry
                            movement.Velocity = Vector3.Zero;
                        }
                    }
                    else
                    {
                        // Decay any horizontal velocity.
                        float friction = GameActor.FrictionDecay(Friction, secs);
                        movement.Velocity *= new Vector3(friction, friction, 1);
                    }
                }

                // Move.
                if (state != GameThing.State.Paused)
                {

                    // Apply external force.
                    movement.Velocity += desiredMovement.ExternalForce.GetValueOrDefault() / thing.Mass * secs;

                    movement.Position += movement.Velocity * secs;
                }

                // Adjust pitch based on vertical movement.
                float deltaVZ = movement.Altitude - movement.PrevPosition.Z;
                if (deltaVZ > 0.01f)
                {
                    pitch = MyMath.Lerp(pitch, MaxPitch, secs);
                }
                else if (deltaVZ < -0.01f)
                {
                    pitch = MyMath.Lerp(pitch, -MaxPitch, secs);
                }
                else
                {
                    pitch = MyMath.Lerp(pitch, 0, secs);
                }

                // At the new position,
                //  push bot down due to being above the surface.
                //  push bot up to keep out of mud.
                //  calc waveOffset
                //  calc waveEffect based on how close to surface we are combined with how grounded we are.

                waterBaseAltitude = Terrain.GetWaterBase(movement.Position);
                terrainAltitude = Terrain.GetHeight(movement.Position);
                waterAltitude = (waterBaseAltitude > 0) ? Terrain.GetWaterHeightAndNormal(movement.Position, ref waterNormal) : 0.0f;

                // Keep bot out of mud.
                // Calc how deep we are and push us back up.
                // Need to zero out velocity.Z when we do this.
                float inMud = (movement.Position.Z + waveOffset - ReScale * HullDraft) - terrainAltitude;
                if (inMud < 0.0f && terrainAltitude > 0)
                {
                    movement.Position -= new Vector3(0, 0, inMud);
                    movement.Velocity *= new Vector3(1, 1, 0);
                }
                else
                {
                    // Push bot down if too high at surface (or even in the air).
                    // Only apply this if not inMud.
                    if (movement.Position.Z > waterAltitude - MinDepth || terrainAltitude == 0.0f)
                    {
                        movement.Velocity += new Vector3(0, 0, Gravity * secs);
                    }
                }

                // Calc wave effect.  We only want the waves to have an effect when the bot is near
                // the surface.  If grounded, then we want the bot to have a chance to slide back
                // into the water.  If just near the surface, the bot should move with the waves.
                float waveEffect = 0.0f;
                if (grounded)
                {
                    waveEffect = 1.0f - groundedAmount;
                }
                else
                {
                    // Make the waveEffect strongest at the surface and fading to 0 as you go deeper.
                    float maxWaveEffectDepth = 3.0f;
                    float depth = waterBaseAltitude - movement.Position.Z;
                    if (depth > maxWaveEffectDepth)
                    {
                        waveEffect = 0.0f;
                    }
                    else
                    {
                        waveEffect = (maxWaveEffectDepth - depth) / maxWaveEffectDepth;
                    }
                }
                waveEffect = MathHelper.Clamp(waveEffect, 0.0f, 1.0f);
                waveOffset = waveEffect * (waterAltitude - waterBaseAltitude);

                if (grounded)
                {
                    // Blend between ground normal and wave normal.  This allows some minimal
                    // rocking until we're fully grounded.
                    groundedNormal = MyMath.Lerp(groundedNormal, waterNormal, waveEffect);
                    botNormal = MyMath.Lerp(botNormal, groundedNormal, secs);
                }
                else
                {
                    // Blend between vertical and wave normal.  This allows the waves to 
                    // affect the bot more when it's close to the surface.
                    waterNormal = MyMath.Lerp(Vector3.UnitZ, waterNormal, waveEffect);
                    botNormal = MyMath.Lerp(botNormal, waterNormal, secs);

                    CheckRipples(thing, waveEffect);
                }

                // If in edit mode kill any velocity.
                if (state == GameActor.State.Paused)
                {
                    movement.Velocity = Vector3.Zero;
                }

                // Create a matrix orienting the actor with the surface it's on.
                botNormal.Normalize();
                Vector3 forward = new Vector3(1.0f, 0.0f, 0.0f);
                Vector3 left = Vector3.Cross(botNormal, forward);
                forward = Vector3.Cross(left, botNormal);
                Matrix local = Matrix.Identity;

                // Ok, this looks strange.  This is because XNA GS assumes Y is up and -Z is forward.  Argh.
                local.Backward = botNormal;
                local.Up = left;
                local.Right = forward;

                // Add in Z rotation and pitch.
                local = Matrix.CreateRotationY(-pitch) * Matrix.CreateRotationZ(movement.RotationZ) * local;

                // And translate to the right place.
                local.Translation = movement.Position;

                // Add in wave offset.
                local.M43 = movement.Position.Z + waveOffset;

                // Finally, set the local matrix explicitly
                movement.SetLocalMatrixAndRotation(local, movement.RotationZ);

            } //end of If State == Active && State != Held

            //Debug.WriteLine( DebugString );

        }   // end of PreCollisionTestUpdate()

        public override void SetLoopedAnimationWeights(AnimationSet anims, Movement movement, DesiredMovement desiredMovement)
        {
            float idleWeight = 0.0f;
            float forwardWeight = 0.0f;
            float backwardsWeight = 0.0f;
            float rightWeight = 0.0f;
            float leftWeight = 0.0f;

            // For the sub and fish the forward and backward animations 
            // are used for dive and surface so look at the pitch value 
            // and assign weights from that.

            if (pitch > 0)
            {
                // Surfacing
                backwardsWeight = Math.Min(pitch / maxPitch, 1.0f); ;

            }
            else
            {
                forwardWeight = Math.Max(-pitch / maxPitch, -1.0f);
            }

            // Blend left/right animations based only soley on current rotation rate.
            {
                // Both the current rotation and the desired rotation are
                // in the range 0..2pi so shift into -pi..pi range to make
                // the comparison easier.
                float delta = movement.RotationZRate;
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
            float total = forwardWeight + backwardsWeight + rightWeight + leftWeight;
            if (total > 1.0f)
            {
                forwardWeight /= total;
                backwardsWeight /= total;
                leftWeight /= total;
                rightWeight /= total;
            }
            else
            {
                // Fill in with idle.
                idleWeight = 1.0f - total;
            }

            // Set resulting weights on animation set.
            anims.IdleWeight = idleWeight;
            anims.ForwardWeight = forwardWeight;
            anims.BackwardsWeight = backwardsWeight;
            anims.RightWeight = rightWeight;
            anims.LeftWeight = leftWeight;

        }   // end of SetLoopedAnimationWeights()

        #endregion

        #region Internal
        /// <summary>
        /// Check if we're close enough to the surface to cause some ripples.
        /// WaveEffect goes from 0=>1 as we approach the surface.
        /// </summary>
        /// <param name="thing"></param>
        /// <param name="waveEffect"></param>
        protected override void CheckRipples(GameThing thing, float waveEffect)
        {
            if (waveEffect > 0.75f)
            {
                float scale = (waveEffect - 0.75f) / (0.9f - 0.75f);
                scale = MyMath.Clamp<float>(scale, 0.0f, 1.0f);
                scale = 0.3f + scale * (1.0f - 0.3f);
                base.CheckRipples(thing, thing.CollisionRadius * scale);
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
            
            /*
            // Clamp velocity to prevent side to side motion.
            if (movement.Velocity.LengthSquared() > 5.0f)
            {
                Vector3 right = Vector3.Cross(movement.Facing, Vector3.UnitZ);
                right.Normalize();
                float dot = Vector3.Dot(right, movement.Velocity);
                // The 0.8 factor just prevents this from clamping down absolutely.
                // It give the motion a bit more fluid feel.
                movement.Velocity -= 0.8f * dot * right;
            }            
            */
            // Clamp velocity to forward/backward.  Don't mess with vertical movement.
            // Calc facing dir without any Z component.
            /*
            if (movement.Velocity.LengthSquared() > 5.0f)
            {
                Vector3 forward = movement.Facing * new Vector3(1, 1, 0);
                forward.Normalize();
                float dot = Vector3.Dot(forward, movement.Velocity);
                Vector3 vel = dot * forward;
                movement.Velocity = new Vector3(vel.X, vel.Y, movement.Velocity.Z);
            }
            */
        }   // end of ApplyDesiredMovement()

        #endregion Internal

    }   // end of class SwimChassis

}   // end of namespace Boku.SimWorld.Chassis
