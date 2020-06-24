using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;
using System.Diagnostics;

using KoiX;

using Boku.Audio;
using Boku.Base;
using Boku.Common;
using Boku.Common.ParticleSystem;
using Boku.SimWorld;
using Boku.SimWorld.Path;
using Boku.SimWorld.Terra;
using Boku.SimWorld.Collision;
using Boku.Fx;

namespace Boku.SimWorld.Chassis
{
    /// <summary>
    /// Chassis used for things that fly like missiles, eg the CruiseMissile.
    /// </summary>
    public class MissileChassis : BaseChassis
    {
        [Flags]
        public enum BehaviorFlags
        {
            TerrainFollowing = 1 << 0,  // Attempt to avoid terrain
            Homing = 1 << 1,            // Follow a moving target
            StayAboveWater = 1 << 2,    // Don't go underwater
            None = 0,
            All = TerrainFollowing | Homing | StayAboveWater
        }

        #region Members
        public const float DefaultMaxRotationRate = 2f;
        public const float DefaultMaxPitchRate = 1.5f;
        public const float DefaultDesiredSpeed = 8f;

        // Limits
        private float maxPitchRate = DefaultMaxPitchRate;   // radians / sec around Y axis
        private float actualSpeed = DefaultDesiredSpeed;    // meters / sec
        private float desiredSpeed = 8f;                    // meters / sec

        private double deathTime;                           // The time when we die even if we've hit nothing.

        private GameActor launcher;                         // Who launched us.
        private GameThing targetObject;                     // What we're shooting at (optional).
        private GameActor missile;                          // The parent missile that owns this chassis.
        private Vector3 targetPosition;                     // Location missile is flying toward.
        private int damage = 0;                             // The number of hit-points this missile takes from its target
        private bool checkedForLand = false;                // Whether we've checked that there is more land to come.

        private BehaviorFlags behavior;             // Missile behavior flags
        private GameThing.Verbs verbPayload = GameThing.Verbs.Vaporize;

        float turnAngle = 0f;
        float rollAngle = 0f;
        float pitchAngle = 0f;
        #endregion

        #region Accessors

        public override bool SupportsStrafing { get { return false; } }

        /// <summary>
        /// PitchAngle, used for initializing to be pointed in the right direction.
        /// Normally don't call this except on setup.
        /// </summary>
        public float PitchAngle
        {
            get { return pitchAngle; }
            set { pitchAngle = value; }
        }
        public float Speed
        {
            get { return actualSpeed; }
            set { actualSpeed = value; }
        }
        public float DesiredSpeed
        {
            get { return desiredSpeed; }
            set { desiredSpeed = value; }
        }
        public float MaxPitchRate
        {
            get { return maxPitchRate; }
            set { maxPitchRate = value; }
        }
        public double DeathTime
        {
            get { return deathTime; }
            set { deathTime = value; }
        }
        public GameActor Launcher
        {
            get { return launcher; }
            set { launcher = value; }
        }
        public GameActor Missile
        {
            get { return missile; }
            set { missile = value; }
        }
        public BehaviorFlags Behavior
        {
            get { return behavior; }
            set { behavior = value; }
        }
        public Vector3 TargetPosition
        {
            get { return targetPosition; }
            set { targetPosition = value; }
        }
        public GameThing TargetObject
        {
            get { return targetObject; }
            set { targetObject = value; }
        }
        public GameThing.Verbs VerbPayload
        {
            get { return verbPayload; }
            set { verbPayload = value; }
        }
        public int Damage
        {
            get { return damage; }
            set { damage = value; }
        }
        public float Rotation
        {
            get { return turnAngle; }
            set { turnAngle = value; }
        }
        private bool TerrainFollowing
        {
            get { return (0 != (behavior & BehaviorFlags.TerrainFollowing)); }
        }
        private bool Homing
        {
            get { return ((targetObject != null) && (0 != (behavior & BehaviorFlags.Homing))); }
        }
        private bool StayOverWater
        {
            get { return !Homing && (0 != (behavior & BehaviorFlags.StayAboveWater)); }
        }
        #endregion

        #region Public

        public MissileChassis()
            : base()
        {
            MaxRotationRate = DefaultMaxRotationRate;         // radians / sec around Z axis
        }

        public override void InitDefaults()
        {
            base.InitDefaults();

            // values here copied from member initializers above.
            maxPitchRate = DefaultMaxPitchRate;
            actualSpeed = 0f;
            desiredSpeed = DefaultDesiredSpeed;
            damage = 0;
            checkedForLand = false;
            verbPayload = GameThing.Verbs.Vaporize;
            turnAngle = 0f;
            rollAngle = 0f;
            pitchAngle = 0f;
        }

        public override void PreCollisionTestUpdate(GameThing thing)
        {
            Movement movement = thing.Movement;
            GameActor.State state = thing.CurrentState;

            float secs = Time.GameTimeFrameSeconds;

            Vector3 collisionCenter = Vector3.Transform(CruiseMissile.XmlActor.CollisionCenter, movement.LocalMatrix);

            // No need to update if we're paused.
            if ((secs > 0.0f) && (state == GameThing.State.Active))
            {

                // Have we outlived our life?
                if (Time.GameTimeTotalSeconds >= DeathTime)
                {
                    VanishWithoutHittingAnything();
                }
                else
                {

                    // If the target has been killed, then have the missile go straight off the world's edge and explode. 
                    if (targetObject != null && targetObject.CurrentState == GameThing.State.Inactive)
                    {
                        targetObject = null;
                        behavior &= ~BehaviorFlags.Homing;
                        // Create new target position since we're no longer following a target.
                        float time = (float)(DeathTime - Time.GameTimeTotalSeconds);
                        float shotRange = time * Speed;
                        targetPosition = collisionCenter + movement.Facing * shotRange * 2.0f;
                    }

                    UpdateMovement(thing, secs);
                    CheckCollisions(movement);
                }
            }
        }   // end of PreCollisionTestUpdate()

        public override void CollideWithTerrainWalls(GameThing thing)
        {
        }

        #endregion

        #region Internal

        private void UpdateMovement(GameThing thing, float secs)
        {
            Movement movement = thing.Movement;

            // Update target position if homing on a moving object.
            if (Homing)
            {
                targetPosition = targetObject.WorldCollisionCenter;

                GameActor.AddMissileLine(thing, targetObject);
            }

            Vector3 collisionCenter = Vector3.Transform(CruiseMissile.XmlActor.CollisionCenter, movement.LocalMatrix);

            // Pick the reference point we'll use to calculate new orientation and position.
            Vector3 referencePosition = targetPosition;
            Vector3 referenceVector = referencePosition - collisionCenter;

            if (referenceVector == Vector3.Zero)
            {
                // We're at the target, just bail otherwise we end up with NaNs.
                return;
            }

            Vector3 referenceVectorUnit = Vector3.Normalize(referenceVector);
            Vector2 referenceVectorUnit2D = new Vector2(referenceVectorUnit.X, referenceVectorUnit.Y);
            if (referenceVectorUnit2D.LengthSquared() > 0.00001f)
                referenceVectorUnit2D.Normalize();

            float lookAheadTime = Homing ? 0.7f : 0.33f;
            float lookAheadDistance = lookAheadTime * Speed;
            float distanceToTarg2D = new Vector2(referenceVector.X, referenceVector.Y).Length();
            bool targetPastLookAhead = true;
            if (lookAheadDistance > distanceToTarg2D)
            {
                lookAheadDistance = distanceToTarg2D;
                targetPastLookAhead = false;
            }

            // If flying nap-of-the-earth, avoid upcoming terrain changes unless
            // the target is closer than the look ahead distance.
            if (TerrainFollowing)
            {
                Vector3 lookAheadPoint = collisionCenter
                                       + new Vector3(referenceVectorUnit2D, 0) * lookAheadDistance;
                lookAheadPoint.Z = targetPosition.Z;
                ///
                Terrain.HitBlock hitBlock = new Boku.SimWorld.Terra.Terrain.HitBlock();

                Vector2 minMaxZ = new Vector2(-1.0f, Single.MaxValue);
                Vector4 maxStep = new Vector4(
                    float.MaxValue, // max single step up
                    float.MinValue, // max step down
                    -1.0f, // water depth at which transition land to water occurs (-1 to ignore)
                    -1.0f); // water depth at which transition water to land occurs (-1 to ignore)

                SimWorld.Terra.Terrain.Blocked(
                    collisionCenter, 
                    lookAheadPoint, 
                    minMaxZ, 
                    maxStep, 
                    ref hitBlock,
                    missile.Movement.Altitude);

                float maxHeightAhead = hitBlock.Max;

                if (maxHeightAhead > 0)
                {
                    lookAheadPoint.Z = maxHeightAhead + EditHeight;
                    if (Homing)
                        lookAheadPoint.Z = Math.Max(lookAheadPoint.Z, targetPosition.Z);
                    checkedForLand = false;
                }
                else
                {
                    /// Check if any more terrain is coming up. If not, we'll 
                    /// early terminate.
                    CheckForMoreLand(collisionCenter, referenceVectorUnit2D);
                }

                // Pick the best height value
                float waterHeight = Terrain.GetWaterHeight(lookAheadPoint);
                if (StayOverWater)
                {
                    lookAheadPoint.Z = Math.Max(lookAheadPoint.Z, waterHeight + EditHeight);
                    maxHeightAhead = Math.Max(maxHeightAhead, waterHeight);
                }
                if (waterHeight > 0)
                {
                    CheckRipples(thing, collisionCenter, CruiseMissile.XmlActor.CollisionRadius, waterHeight);
                }

                // Update the look-ahead vector with the new height
                Vector3 lookAheadVectorUnit = new Vector3(
                    referenceVectorUnit2D, lookAheadPoint.Z - collisionCenter.Z);
                lookAheadVectorUnit.Normalize();

                /// We need to pitch up to follow the terrain if:
                /// a) We aren't homing
                /// b) We are homing but:
                ///  i) We are still far off from our target
                ///  ii) We are close but would smack into the terrain if we
                ///      bee-line toward it.
                bool adjustPitch = !Homing
                    || targetPastLookAhead
                    || (referencePosition.Z < maxHeightAhead);
                if (adjustPitch) 
                {
                    // Use the look-ahead vector as our desired flight direction.
                    referenceVector = lookAheadVectorUnit * 50f;
                    referencePosition = collisionCenter + referenceVector;
                    referenceVectorUnit = lookAheadVectorUnit;
                }
            }

            // Update yaw
            float deltaTurn = 0;
            if (referenceVectorUnit2D.LengthSquared() > 0.00001f)
            {
                float desiredTurn = (float)(Math.Acos(referenceVectorUnit2D.X) * Math.Sign(referenceVectorUnit2D.Y));
                deltaTurn = GetShorterAngle(desiredTurn - GetShorterAngle(turnAngle));
                deltaTurn = MathHelper.Clamp(deltaTurn, -MaxRotationRate * secs, MaxRotationRate * secs);
                turnAngle += deltaTurn;
            }
            
            // Update roll
            rollAngle = MyMath.Lerp(rollAngle, -deltaTurn / MaxRotationRate / secs, secs * 10f);

            // Update pitch
            float desiredPitch = (float)(Math.Atan2(referenceVectorUnit.Z, new Vector2(referenceVectorUnit.X, referenceVectorUnit.Y).Length()));
            float deltaPitch = GetShorterAngle(desiredPitch - GetShorterAngle(pitchAngle));
            if (Speed > 0)
            {
                float posMaxPitch = Homing ? MaxPitchRate * 2.0f : MaxPitchRate * 5.0f;
                float negMaxPitch = Homing ? MaxPitchRate * 0.5f : MaxPitchRate * 0.5f;

                /// Speed of 0 means this is our first frame and we haven't accelerated up yet.
                /// Go ahead and start off pitched toward the right direction.
                float maxPitch = ((pitchAngle < 0) && (deltaPitch < 0) ? negMaxPitch : posMaxPitch);
                maxPitch *= secs;
                deltaPitch = MathHelper.Clamp(deltaPitch, -maxPitch, maxPitch);
            }
            pitchAngle += deltaPitch;

            // Build our new local rotation matrix
            Matrix rotation = Matrix.CreateFromQuaternion(
                Quaternion.CreateFromAxisAngle(Vector3.Backward, turnAngle) *
                Quaternion.CreateFromAxisAngle(Vector3.Down, pitchAngle) *
                Quaternion.CreateFromAxisAngle(Vector3.Right, rollAngle));
            
            // Find our new position along our rotated forward axis.
            Vector3 velocity = rotation.Right * Speed;
            Vector3 position = movement.Position + velocity * secs;

            // Update the velocity.  This isn't currently used by the chassis
            // but is needed for the collision testing to work correctly.
            movement.Velocity = velocity;

            // Set our new rotation and translation.
            movement.LocalMatrix = rotation * Matrix.CreateTranslation(position);

            // Adjust from our actual to desired speed.
            if (Speed < DesiredSpeed)
                Speed = MyMath.Clamp<float>(Speed + secs * 10f, Speed, DesiredSpeed);
            else if (Speed > DesiredSpeed)
                Speed = MyMath.Clamp<float>(Speed - secs * 2f, DesiredSpeed, Speed);
        }

        /// <summary>
        /// Look to see if we are intersecting (or near enough) the water surface,
        /// and if so kick off some ripples. Note how much we have to expand our
        /// tiny collision radius here to get good results.
        /// </summary>
        /// <param name="movement"></param>
        /// <param name="collCenter"></param>
        /// <param name="collRad"></param>
        /// <param name="waterHeight"></param>
        private void CheckRipples(GameThing thing, Vector3 collCenter, float collRad, float waterHeight)
        {
            if (!thing.Invisible)
            {
                if ((waterHeight - Terrain.WaveHeight < collCenter.Z + collRad * 6.0f)
                    && (waterHeight > collCenter.Z - collRad * 8.0f))
                {
                    base.CheckRipples(thing, collRad * 3.0f);
                }
            }
        }

        /// <summary>
        /// Look ahead on non-guided missiles to see if any more land is coming up.
        /// If not, then just die.
        /// </summary>
        /// <param name="from"></param>
        /// <param name="dir2D"></param>
        /// <returns></returns>
        private bool CheckForMoreLand(Vector3 from, Vector2 dir2D)
        {
            if (!checkedForLand && !Homing)
            {
                float distance = DesiredSpeed * (float)(DeathTime - Time.GameTimeTotalSeconds);
                Vector3 end = new Vector3(from.X + dir2D.X * distance,
                                          from.Y + dir2D.Y * distance,
                                          from.Z);


                Terrain.HitBlock hitBlock = new Boku.SimWorld.Terra.Terrain.HitBlock();

                Vector2 minMaxZ = new Vector2(-1.0f, 0.1f);
                Vector4 maxStep = new Vector4(
                    float.MaxValue, // max single step up
                    float.MinValue, // max step down
                    -1.0f, // water depth at which transition land to water occurs (-1 to ignore)
                    -1.0f); // water depth at which transition water to land occurs (-1 to ignore)

                SimWorld.Terra.Terrain.Blocked(
                    from,
                    end,
                    minMaxZ,
                    maxStep,
                    ref hitBlock,
                    missile.Movement.Altitude);

                checkedForLand = true;

                if (hitBlock.Max <= 0)
                {
                    DeathTime = Time.GameTimeTotalSeconds + 0.25f;
                }

                return hitBlock.Max > 0;
            }
            return true;
        }

        private void CheckCollisions(Movement movement)
        {
            Vector3 collisionCenter = Vector3.Transform(CruiseMissile.XmlActor.CollisionCenter, movement.LocalMatrix);

            // Check for collision with terrain.
            float terrainHeight = Terrain.GetTerrainAndPathHeight(collisionCenter);
            if (movement.Altitude < terrainHeight)
            {
                ExplodeWithoutHittingAnything(collisionCenter);
                return;
            }

            // Check for collisions against walls.
            List<Road.CollisionInfo> collisions = new List<Road.CollisionInfo>();
            if (WayPoint.GetCollisions(collisionCenter, 1.0f, collisions))
            {
                ExplodeWithoutHittingAnything(collisionCenter);
                return;
            }

        }   // end of CheckCollisions()

        public void HitTarget(GameThing hitThing, MouseTouchHitInfo MouseTouchHitInfo)
        {
            if (hitThing != null)
            {
                // Tell the launcher what we hit.
                launcher.OnMissileHit(hitThing, MouseTouchHitInfo.Center, verbPayload, damage);

                // Apply the damage to the target.
                // We only need to do this if the launcher is dead.  Otherwise the above
                // notification will get processed and the damage will be taken into
                // account there.  Yes, this is kind of confusing and should be rethought
                // and cleaned up.  
                // TODO (scoy)
                if (Launcher.CurrentState == GameThing.State.Inactive)
                {
                    bool targetDied = false;
                    hitThing.DoDamage(-damage, verbPayload, true, false, Launcher, out targetDied);
                }

                // Kill off the missile.
                missile.Deactivate();

                // Gratuitous effects.
                PlayExplodeEffect(MouseTouchHitInfo.Center);
            }
        }   // end of HitTarget()

        private void VanishWithoutHittingAnything()
        {
            // Tell the launcher that we didn't hit anything.
            launcher.OnMissileExpire();

            // Kill off the missile.
            missile.Deactivate();
        }
        private void ExplodeWithoutHittingAnything(Vector3 position)
        {
            // Tell the launcher that we didn't hit anything.
            launcher.OnMissileExpire();

            // Kill off the missile.
            missile.Deactivate();

            // Gratuitous effects.
            PlayExplodeEffect(position);
        }

        private void PlayExplodeEffect(Vector3 position)
        {
            if (this.verbPayload != GameThing.Verbs.Vanish)
            {
                ExplosionManager.CreateExplosion(position, 2.0f);
                if (!Launcher.Mute)
                {
                    Foley.PlayBoom(missile);
                }
            }
        }   // end of PlayExplodeEffect()

        /// <summary>
        /// Returns true if this thing should be excluded from being hit.
        /// </summary>
        /// <param name="thing"></param>
        /// <returns></returns>
        private bool ExcludedHitThing(GameThing thing)
        {
            return (thing == null)
                || (thing == launcher)
                || (thing.ActorHoldingThis == launcher);
        }

        /// <summary>
        /// Returns equivalent angle in [-pi, -pi]
        /// </summary>
        /// <param name="angle"></param>
        /// <returns></returns>
        private float GetShorterAngle(float angle)
        {
            while (angle > MathHelper.Pi)
                angle -= MathHelper.TwoPi;
            while (angle < -MathHelper.Pi)
                angle += MathHelper.TwoPi;
            return angle;
        }

        #endregion

    }   // end of class MissileChassis

}   // end of namespace Boku.SimWorld.Chassis
