
//#define MF_VALIDATE


using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using System.Xml;
using System.Xml.Serialization;

using Microsoft.Xna.Framework;

using Boku.Common;
using Boku.Common.ParticleSystem;
using Boku.SimWorld;
using Boku.SimWorld.Chassis;
using Boku.SimWorld.Terra;
using Boku.Animatics;

using Boku.Programming;

namespace Boku.Base
{
    public enum ChassisType
    {
        NotApplicable,
        Unknown,
        Boat,
        Cursor,
        Cycle,
        Rover,
        DynamicProp,
        FloatInAir,
        Hover,
        HoverSwim,
        Missile,
        Puck,
        Saucer,
        SitAndSpin,
        StaticProp,
        Swim,
        Pipe
    }

    /// <summary>
    /// Base class for chassis.  A chassis encompasses the dynamics of a bot
    /// including the movement limitations of the bot, how it responds to the
    /// brain's desired movements and how it responds to collisions.
    /// </summary>
    public abstract class BaseChassis : ICloneable
    {
        // Value returned when there is no ground.  Allows object to fall.
        public static float kMinAltitude = -100.0f;

        /// <summary>
        /// A class which contains a single "probe" used so that the chassis 
        /// can sample the terrain at more than a single point.
        /// </summary>
        public class CurbFeeler
        {
            // A struct to hold the result of updateing a CurbFeeler.
            public struct FeelerData
            {
                public Vector3 position;            // World space position of this feeler.
                public float altitude;              // Terrain altitude sampled by this feeler.
                public Vector3 prevPosition;        // Previous position of this feeler in world coords.
                public float prevAltitude;          // Altitude at previous position.
            }   // end of struct FeelerData

            #region Members
            private Vector3 offset;                 // Position of this feeler relative to the bot.

            private Terrain.MaterialInfo matInfo;

            public Vector3 prevPosition = Vector3.Zero;
            public float prevAltitude = BaseChassis.kMinAltitude;
            #endregion

            #region Accessors
            /// <summary>
            /// Position of this feeler relative to the bot.  For bots with a facingDirection
            /// these are in model space and then transformed into world coords.  For bots
            /// without a facingDirection these are just offsets from the origin of the bot.
            /// </summary>
            public Vector3 Offset
            {
                get { return offset; }
                set { offset = value; }
            }
            /// <summary>
            /// Terrain and water under this feeler.
            /// </summary>
            public Terrain.MaterialInfo TerrainMaterialInfo
            {
                get { return matInfo; }
            }
            #endregion

            #region Public
            public CurbFeeler(Vector3 offset)
            {
                this.offset = offset;
            }

            public void InitDefaults()
            {
                prevPosition = Vector3.Zero;
                prevAltitude = BaseChassis.kMinAltitude;
            }

            /// <summary>
            /// Compute the altitude under this feeler when the master has given
            /// movement and position.
            /// </summary>
            /// <param name="movement"></param>
            /// <param name="position"></param>
            /// <param name="waistHeight"></param>
            /// <param name="hasFacing"></param>
            /// <returns></returns>
            public float Altitude(Movement movement, Vector3 position, float topHeight, float rescale, bool hasFacing)
            {
                position = OffsetPosition(movement, position, topHeight, rescale, hasFacing);
                return Terrain.GetTerrainAndPathHeight(position);
            }
            /// <summary>
            /// Updates the current feeler data.
            /// </summary>
            /// <param name="movement"></param>
            /// <param name="botPosition"></param>
            /// <param name="waistHeight"></param>
            /// <param name="hasFacingDirection"></param>
            /// <param name="altitude">Altitude origin of bot used to elimiate edge cases.</param>
            /// <param name="data"></param>
            /// <returns>True if the update is good, false if this feeler should be ignored.</returns>
            public bool Update(Movement movement, Vector3 botPosition, float topHeight, float rescale, bool hasFacingDirection, ref FeelerData data)
            {
                data.position = OffsetPosition(movement, botPosition, topHeight, rescale, hasFacingDirection);
                data.altitude = Terrain.GetTerrainAndPathHeight(data.position, ref matInfo);

                // If there is no terrain, put the altitude way down there.
                // This stops bots from sometimes sticking at z==0 as they fall.
                // Note at -50 bots autmatically disappear so -100 should be good.
                // Previously this was float.MinValue but that caused NaN creation
                // further into the code.
                if (data.altitude == 0)
                {
                    data.altitude = BaseChassis.kMinAltitude;
                }

                // Initialized?
                if (prevAltitude == BaseChassis.kMinAltitude)
                {
                    // First time through, use data.position
                    data.prevPosition = data.position;
                    data.prevAltitude = data.altitude;
                }
                else
                {
                    data.prevPosition = prevPosition;
                    data.prevAltitude = prevAltitude;
                }
                prevPosition = data.position;
                prevAltitude = data.altitude;

                // Check if starting from an invalid position.
                //if (data.prevAltitude != 0.0f) // && data.prevAltitude > data.prevPosition.Z)
                //{
                //    return false;
                //}

                return true;
            }   // end of CurbFeeler Update()

            public bool Validate(Movement movement, Vector3 botPosition, float waistOffset, float topOffset, float rescale, bool hasFacing)
            {
                float height = botPosition.Z + waistOffset;

                Vector3 position = OffsetPosition(movement, botPosition, topOffset, rescale, hasFacing);

                float altitude = Terrain.GetTerrainAndPathHeight(position);

                bool valid = (altitude > 0) && (altitude < height);
                if (valid)
                {
                    prevPosition = position;
                    prevAltitude = altitude;
                }
#if MF_VALIDATE
                else
                {
                    valid = false;
                }
#endif // MF_VALIDATE

                return valid;
            }

            /// <summary>
            /// Return the matInfo to a no-terrain state.
            /// </summary>
            public void ClearTerrain()
            {
                matInfo.ResetTerrain();
            }
            /// <summary>
            /// Return the matInfo to a no-water state.
            /// </summary>
            public void ClearWater()
            {
                matInfo.ResetWater();
            }

            public void ResetPrevPosition()
            {
                // An altitude of MinAltitude signifies uninitialized.
                prevAltitude = BaseChassis.kMinAltitude;
            }
            #endregion

            #region Internal
            public Vector3 OffsetPosition(Movement movement, Vector3 botPosition, float topHeight, float rescale, bool hasFacing)
            {
                if (hasFacing)
                {
                    // Current.
                    Vector3 forward = movement.Facing;
                    forward.Z = 0;
                    Vector3 left = Vector3.Cross(Vector3.UnitZ, forward);
                    Vector3 orientedOffset = Offset.X * forward + Offset.Y * left;
                    return new Vector3(
                        botPosition.X + orientedOffset.X * rescale,
                        botPosition.Y + orientedOffset.Y * rescale,
                        botPosition.Z + orientedOffset.Z * rescale + topHeight);
                }

                // For bots that don't have a facing direction 
                // we use the cardinal directions.
                return new Vector3(
                    botPosition.X + Offset.X * rescale,
                    botPosition.Y + Offset.Y * rescale,
                    botPosition.Z + Offset.Z * rescale + topHeight);
            }
            #endregion

        }   // end of class CurbFeeler


        #region Members

        private GameThing parent = null;

        public static float kGravity = -9.8f;
        protected float gravity = kGravity;

        protected bool resetHeightOffset = false;   // If true, then on the next update the chassis should reset the height offset based on its current height.
        protected float targetAltitude = 0.0f;      // A target altitude in absolute terms

        protected bool fixedPosition = false;       // Is this stuck in one place and should never be moved?
        protected bool moving = false;              // Is this thing currently moving or  
                                                    // has it settled to a static state?  

        protected bool terrainOnContact = false;    // Do we ignore terrain/water except when actually contacting (not just over)?

        protected float density = 2.0f;             // Water is 1.0.  <1 will float, >1 will sink
                                                    // Currently only used by DynamicPropChassis to control floating/sinking.

        private float maxSpeed = 2.0f;                      // meters per second
        private float maxLinearAcceleration = 2.0f;         // meters per second^2
        private float maxLinearDeceleration = -1.0f;        // meters per second^2 - negative means just use maxLinearAcceleration

        private float maxRotationRate = 1.0f;               // radians per second
        private float maxRotationalAcceleration = 1.0f;     // radians per second^2

        protected float movementSpeedModifier = 1.0f;
        protected float turningSpeedModifier = 1.0f;
        protected float linearAccelerationModifier = 1.0f;
        protected float turningAccelerationModifier = 1.0f;

        protected bool hasFacingDirection = true;   // This bot has a "front" which determines which way it is 
                                                    // moving as opposed to something like the saucer which just 
                                                    // moves any direction regardless of its rotation.

        protected bool ignoreGlassWalls = false;    // If true, this bot ignores the glass walls around the world.
        protected bool insideGlassWalls = true;     // For bots that care about glass walls, this indicates whether
                                                    // the bot is currently inside or out.

        protected bool jump = false;                // The bot has been told to "jump".  Depending on the chassis
                                                    // type this may be ignored.
        protected bool jumping = false;             // Is bot int the process of jumping?
        protected bool doubleJumping = false;       // Has a double-jump been triggered?
        protected bool startJumpAnimation = false;  // Tell animation to start.
        protected double jumpStartTime;             // Start time for jump animation.
        protected float preJumpDelay = 0.2f;        // Delay from time animation is started to time when vertical boost is added.
        protected float defaultJumpStrength = 0.0f;        // Vertical velocity boost we get when jumping.
        protected float effectiveJumpStrength = 0.0f;      // Vertical velocity boost we get when jumping.

        protected bool landing = false;             // We're waiting to land from a jump.
        protected bool startLandAnimation = false;  // Tell animation to start.
        protected float preLandDelay = 0.1f;        // How far ahead of landing we want to start the landing animation.

        protected float jumpRate = 1.0f;            // The number of times per second we may jump.
        protected double lastJumpTime = 0.0;        // In total game seconds.

        protected bool allowBrainMovement = true;   // Whether to use or ignore movement changes coming from the brain

        protected List<CurbFeeler> feelers = null;  // List of CurbFeelers for this chassis.

        protected bool terrainDataValid = false;

        protected bool overPath = false;            // Is this actor currently over a path.

        protected bool impactingFloor = false;      // Valid in PostCollisionTestUpdate only.

        protected double nextRipple = 0.0f;
        protected Vector2 lastRipple = new Vector2(float.MaxValue, float.MaxValue);

        protected ConstraintModifier.Constraints constraints = ConstraintModifier.Constraints.None;
        
        // Variables to indicate whether touch controls are attempting to rotate the chassis this frame
        // and the rotation rate/acceleration to use when this occurs.
        protected bool wasTouchRotated = false;

        #endregion

        #region Accesssors

        /// <summary>
        /// Density relative to water, determines if this object floats.  Water is 1.0.
        /// </summary>
        public float Density
        {
            get { return density; }
            set { density = value; }
        }

        /// <summary>
        /// Flag to indicate whether a touch control has caused the chassis to rotate. We use alternate
        /// values for rotation/rotationacceleration in these cases.
        /// </summary>
        public bool WasTouchRotated 
        {
            get { return wasTouchRotated; }
            set { wasTouchRotated = value; }
        }

        /// <summary>
        /// Returns true if the chassis implementation supports movement in one direction while facing another.
        /// </summary>
        public abstract bool SupportsStrafing { get; }

        /// <summary>
        /// Gravitation constant in meters per second^2.
        /// </summary>
        public float Gravity
        {
            get { return gravity; }
            set { gravity = value; }
        }

        /// <summary>
        /// Kilograms as if it matters.
        /// </summary>
        public float Mass
        {
            get { return parent.Mass; }
        }

        /// <summary>
        /// Bounciness.  Should be in range [0, 1]
        /// </summary>
        public float CoefficientOfRestitution
        {
            get { return parent.CoefficientOfRestitution; }
        }

        /// <summary>
        /// Defaults to 0, ie no friction
        /// </summary>
        public float Friction
        {
            get { return parent.Friction; }
        }

        /// <summary>
        /// This is the height of the object in the editor.  This combines
        /// the default edit height plus any user specified offset.
        /// </summary>
        public virtual float EditHeight
        {
            get { return parent.EditHeight; }
        }

        /// <summary>
        /// This is the default height of the object in the editor.
        /// </summary>
        public virtual float DefaultEditHeight
        {
            get { return parent.DefaultEditHeight; }
        }

        /// <summary>
        /// The is an offset to the height added by the user in the editor.
        /// Clamped to the object's minimum height.
        /// How this is used is object dependent.
        /// </summary>
        public float HeightOffset
        {
            get { return parent.HeightOffset; }
            set { parent.HeightOffset = value; }
        }

        /// <summary>
        /// If true, then on the next update the chassis should reset the height offset based on its current height.
        /// </summary>
        public bool ResetHeightOffset
        {
            get { return resetHeightOffset; }
            set { resetHeightOffset = value; }
        }

        /// <summary>
        /// This is the offset from the bot's height used for terrain collision testing.
        /// Defaults to 0.
        /// </summary>
        public float WaistOffset
        {
            get { return parent.WaistOffset * parent.ReScale; }
        }

        /// <summary>
        /// This is the offset in Z from the bot's origin used to play the eye when in first person mode.
        /// Defaults to 0.1.
        /// </summary>
        public float EyeOffset
        {
            get { return parent.EyeOffset * parent.ReScale; }
        }

        /// <summary>
        /// Offset in Z from bot's origin defining how much space it needs to pass under a path.
        /// Includes rescale.
        /// </summary>
        public float TopOffset
        {
            get { return parent.TopOffset; }
        }

        /// <summary>
        /// This is the minimum height of the object.  An object at 
        /// this height should appear to be sitting on the ground.
        /// </summary>
        public float MinHeight
        {
            get 
            {
                float height = parent.MinHeight;
                GameActor actor = parent as GameActor;
                if (actor != null)
                {
                    height = parent.MinHeight * actor.SquashScale.Z;
                }
                return height; 
            }
        }

        /// <summary>
        /// Target altitude in absolute terms. Actual altitude will be max of this
        /// and altitude EditHeight + TerrainHeight. This is normally reset every frame
        /// after use. Not all chassis pay attention to it.
        /// </summary>
        public float TargetAltitude
        {
            get { return targetAltitude; }
            set { targetAltitude = value; }
        }

        /// <summary>
        /// For dynamic chassis this indicates that the object is still moving.
        /// This is false if the object has settled into a fixed position.  If
        /// the object is hit or kicked this should be set to true so that the
        /// object will resume doing dynamics simulation.
        /// </summary>
        public bool Moving
        {
            get { return moving; }
            set { moving = value; }
        }

        /// <summary>
        /// Is this bot currently in the process of performing a jump.
        /// </summary>
        public bool Jumping
        {
            get { return landing; }
        }

        /// <summary>
        /// Set to true for a chassis that should not move even if bumped.
        /// </summary>
        public bool FixedPosition
        {
            get { return fixedPosition; }
            set { fixedPosition = value; }
        }


        // TODO (****) Are the following variables even needed or are they duplicates
        // of values stored elsewhere.  If they are duplicates we should remove these
        // and ensure that everything is normalized to only look in a single place.


        /// <summary>
        /// Used to tweak an individual's speed capabilities so that we can 
        /// have slightly varying performance characteristics even among 
        /// otherwise identical actors.
        /// Defaults to 1.0f.
        /// </summary>
        public float MovementSpeedModifier
        {
            get { return movementSpeedModifier; }
            set { movementSpeedModifier = value; }
        }

        /// <summary>
        /// Used to tweak an individual's turning capabilities so that we can 
        /// have slightly varying performance characteristics even among 
        /// otherwise identical actors.
        /// Defaults to 1.0f.
        /// </summary>
        public float TurningSpeedModifier
        {
            get { return turningSpeedModifier; }
            set { turningSpeedModifier = value; }
        }

        /// <summary>
        /// Speed at which we clamp velocity. Meters / second.
        /// </summary>
        public float MaxSpeed
        {
            get { return maxSpeed; }
            set { maxSpeed = value; }
        }
        /// <summary>
        /// Fastest we allow it to speed up. Meters / second^2.
        /// </summary>
        public float MaxLinearAcceleration
        {
            get { return maxLinearAcceleration; }
            set { maxLinearAcceleration = value; }
        }
        /// <summary>
        /// Fastest we allow it to slow down. Meters / second^2.
        /// </summary>
        public float MaxLinearDeceleration
        {
            get { return maxLinearDeceleration > 0 ? maxLinearDeceleration : MaxLinearAcceleration; }
            set { maxLinearDeceleration = value; }
        }
        /// <summary>
        /// Max speed of turn. Radians / second.
        /// </summary>
        public float MaxRotationRate
        {
            get { return maxRotationRate; }
            set { maxRotationRate = value; }
        }
        /// <summary>
        /// Max change in speed of turn. Radians / second^2.
        /// </summary>
        public float MaxRotationalAcceleration
        {
            get { return maxRotationalAcceleration; }
            set { maxRotationalAcceleration = value; }
        }

        /// <summary>
        /// Used to tweak an individual's speed capabilities so that we can 
        /// have slightly varying performance characteristics even among 
        /// otherwise identical actors.
        /// Defaults to 1.0f.
        /// </summary>
        public float LinearAccelerationModifier
        {
            get { return linearAccelerationModifier; }
            set { linearAccelerationModifier = value; }
        }

        /// <summary>
        /// Used to tweak an individual's turning capabilities so that we can 
        /// have slightly varying performance characteristics even among 
        /// otherwise identical actors.
        /// Defaults to 1.0f.
        /// </summary>
        public float TurningAccelerationModifier
        {
            get { return turningAccelerationModifier; }
            set { turningAccelerationModifier = value; }
        }
        
        /// <summary>
        /// This bot has a "front" which determines which way it is 
        /// moving as opposed to something like the saucer which just 
        /// moves any direction regardless of its rotation.
        /// </summary>
        public bool HasFacingDirection
        {
            get { return hasFacingDirection; }
            set { hasFacingDirection = value; }
        }

        /// <summary>
        /// This bot ignores the glass walls at the edge of the world.
        /// </summary>
        public bool IgnoreGlassWalls
        {
            get { return ignoreGlassWalls; }
            set { ignoreGlassWalls = value; }
        }

        /// <summary>
        /// For bots that care about glass walls, this indicates whether the bot is currently inside or out.
        /// </summary>
        public bool InsideGlassWalls
        {
            get { return insideGlassWalls; }
            set { insideGlassWalls = value; }
        }

        /// <summary>
        /// Used to tell the bot to perform a jump.  Note that most
        /// bots or chassis will ignore this.
        /// </summary>
        public bool Jump
        {
            get { return jump; }
            set { jump = value; }
        }

        /// <summary>
        /// Vertical velocity added when this chassis jumps.
        /// </summary>
        public float DefaultJumpStrength
        {
            get { return defaultJumpStrength; }
            set { defaultJumpStrength = value; }
        }

        public float EffectiveJumpStrength
        {
            get { return effectiveJumpStrength; }
            set { effectiveJumpStrength = value; }
        }

        /// <summary>
        /// Flag to be checked by the animation to know when to start jumping.
        /// Should be cleared to acknowledge that animation has started.
        /// </summary>
        public bool StartJumpAnimation
        {
            get { return startJumpAnimation; }
            set { startJumpAnimation = value; }
        }

        /// <summary>
        /// Flag to be checked by the animation to know when to start landing.
        /// Should be cleared to acknowledge that animation has started.
        /// </summary>
        public bool StartLandAnimation
        {
            get { return startLandAnimation; }
            set { startLandAnimation = value; }
        }

        /// <summary>
        /// The amount of time that the jump animation should be running before 
        /// the physical jump takes place.  Or, in animaotr terms, the 
        /// anticipation time before the jump.
        /// </summary>
        public float PreJumpDelay
        {
            get { return preJumpDelay; }
            set { preJumpDelay = value; }
        }

        /// <summary>
        /// The amount of time before the bot touches down that the landing 
        /// animation should be started.
        /// </summary>
        public float PreLandDelay
        {
            get { return preLandDelay; }
            set { preLandDelay = value; }
        }

        /// <summary>
        /// The number of times per second this chassis may jump.
        /// </summary>
        public float JumpRate
        {
            get { return jumpRate; }
            set { jumpRate = value; }
        }


        /// <summary>
        /// The list of CurbFeelers associated with this chassis.
        /// </summary>
        [XmlIgnore]
        public List<CurbFeeler> Feelers
        {
            get { return feelers; }
        }

        public bool AllowBrainMovement
        {
            get { return allowBrainMovement; }
            set { allowBrainMovement = value; }
        }

        /// <summary>
        /// Is the terrain material info in the feelers valid?
        /// </summary>
        [XmlIgnore]
        public bool TerrainDataValid
        {
            get { return terrainDataValid; }
            protected set { terrainDataValid = value; }
        }

        /// <summary>
        /// Is this actor currently over a path.
        /// </summary>
        [XmlIgnore]
        public bool OverPath
        {
            get { return overPath; }
            set { overPath = value; }
        }

        /// <summary>
        /// If we're over a path, this is its color.
        /// </summary>
        [XmlIgnore]
        public Classification.Colors PathColor
        {
            get
            {
                Classification.Colors color = Classification.Colors.NotApplicable;
                if (Feelers != null && Feelers[0] != null)
                {
                    color = Feelers[0].TerrainMaterialInfo.PathColor;
                }
                return color;
            }
        }

        /// <summary>
        /// The initial position of the chassis when we entered the simulation.
        /// Only valid if any Constraints flags are set.
        /// </summary>
        public Vector3 ConstraintInitialPosition { get; set; }

        public Vector3 ImmobilePosition { get; set; }

        /// <summary>
        /// Constraints the brain is imposing on this chassis. We clear this every frame after applying.
        /// </summary>
        public ConstraintModifier.Constraints Constraints 
        {
            get { return constraints; }
            set { constraints = value; }
        }

        /// <summary>
        /// The position at the "top" of the bot.
        /// </summary>
        /// <param name="pos"></param>
        /// <returns></returns>
        public Vector3 Top(Vector3 pos)
        {
            return new Vector3(pos.X, pos.Y, pos.Z + TopOffset);
        }

        /// <summary>
        /// Scale value how much bigger(smaller) this bot is than cannonical size.
        /// </summary>
        public float ReScale
        {
            get { return parent.ReScale; }
        }

        public GameThing Parent
        {
            get { return parent; }
        }
        
        #endregion

        #region Public

        public BaseChassis()
        {
            feelers = new List<CurbFeeler>();
            // By default, always test the center of the actor.
            feelers.Add(new BaseChassis.CurbFeeler(Vector3.Zero));
        }

        /// <summary>
        /// Let this chassis know what bot it's controlling. This should only
        /// be called (automatically) by the parent bot.
        /// </summary>
        /// <param name="thing"></param>
        public void Bind(GameThing thing)
        {
            Debug.Assert((thing == null) || (thing.Chassis == this));
            parent = thing;
        }

        /// <summary>
        /// Initialize any members that can change at runtime back to their
        /// initial values. DO NOT ALLOCATE ANYTHING IN THIS FUNCTION. This
        /// function exists so that ActorFactory can reinitialize recycled
        /// actors. ActorFactory exists so that we do not need to allocate
        /// new memory when the need for a new actor arises. Therefore
        /// allocating memory in this function would defeat the entire
        /// reason for its existence.
        /// </summary>
        public virtual void InitDefaults()
        {
            // These values copied from constructor initializers above.
            Density = 2.0f;
            Moving = false;
            jumping = false;
            doubleJumping = false;
            startJumpAnimation = false;
            landing = false;
            startLandAnimation = false;
            lastJumpTime = 0.0;
            impactingFloor = false;
            targetAltitude = 0.0f;
            allowBrainMovement = true;
            terrainDataValid = false;
            impactingFloor = false;
            Constraints = ConstraintModifier.Constraints.None;

            for (int i = 0; i < feelers.Count; ++i)
            {
                feelers[i].InitDefaults();
            }
        }

        /// <summary>
        /// Either the scene is starting up, or we've just been added. Either way,
        /// time to start doing our thing.
        /// </summary>
        public virtual void Activate()
        {
            for (int i = 0; i < feelers.Count; ++i)
            {
                feelers[i].ResetPrevPosition();
            }
        }

        /// <summary>
        /// Our owner is going dormant. Tear down anything we're doing.
        /// </summary>
        public virtual void Deactivate()
        {
        }

        /// <summary>
        /// This is the update call that should handle all the chassis dynamics.
        /// </summary>
        /// <param name="thing"></param>
        public virtual void PreCollisionTestUpdate(GameThing thing)
        {
        }   // end of BaseChassis PreCollisionTestUpdate()

        /// <summary>
        /// This function should only handle parts of the chassis updates that need to occur
        /// after the collision (other actors, terrain walls and edge of the world) testing.
        /// This update should NEVER change the position of the chassis except in Z.  Right
        /// now this is only used for chassis that need to bounce off the ground, ie the 
        /// DynamicPropChassis and the CycleChassis.  We need to do this since we want to 
        /// test the terrain as a wall before testing for a bounce.
        /// </summary>
        /// <param name="thing"></param>
        public virtual void PostCollisionTestUpdate(GameThing thing)
        {
        }   // end of BaseChassis PostCollisionTestUpdate()

        /// <summary>
        /// Return a list of terrain types we're touching. May contain duplicates.
        /// Only valid when TerrainDataValid is true.
        /// </summary>
        /// <param name="list"></param>
        public void GetTerrainMaterials(Terrain.TypeList list)
        {
            list.Clear();
            for (int i = 0; i < Feelers.Count; ++i)
            {
                CurbFeeler feeler = Feelers[i];
                list.AddType(feeler.TerrainMaterialInfo.TerrainType);
            }
        }
        /// <summary>
        /// Return a list of water types we're touching. May contain duplicates.
        /// Only valid when TerrainDataValid is true.
        /// </summary>
        /// <param name="list"></param>
        public void GetWaterMaterials(Terrain.TypeList list)
        {
            list.Clear();
            for (int i = 0; i < Feelers.Count; ++i)
            {
                CurbFeeler feeler = Feelers[i];
                list.AddType(feeler.TerrainMaterialInfo.WaterType);
            }
        }

        /// <summary>
        /// Does collision testing for falling off the edge  
        /// of the world and running into terrain walls.
        /// </summary>
        /// <param name="actor"></param>
        public virtual void CollideWithTerrainWalls(GameThing thing)
        {
            Movement movement = thing.Movement;

            bool soundPlayed = false;   // Only ever play a single sound even if there are multiple collisions.

            // Reset per-frame state.
            impactingFloor = false;

            // Structs we'll use.
            Terrain.HitBlock hitBlock = new Terrain.HitBlock();
            CurbFeeler.FeelerData data = new CurbFeeler.FeelerData();

            // Start with current values;
            Vector3 position = movement.Position;
            Vector3 prevPosition = movement.PrevPosition;
            Vector3 velocity = movement.Velocity;
            Vector3 accumVelocity = Vector3.Zero;
            float totalWgt = 0.0f;
            float dt = Time.GameTimeFrameSeconds;

            float collisionBottom = thing.WorldCollisionCenter.Z - thing.WorldCollisionRadius;

            float actorWaist = thing.ActorHoldingThis != null
                ? float.MaxValue
                : WaistOffset;

            bool feelersValid = true;

#if MF_VALIDATE
            bool debugValid = true;
#endif // MF_VALIDATE

            // If we haven't moved then we can be fairly certain we haven't hit any walls.
            Vector3 deltaPosition = position - prevPosition;
            bool moved = !(deltaPosition == Vector3.Zero && velocity == Vector3.Zero);
            if (!TerrainDataValid || moved)
            {

                // Are we testing against glass walls?
                bool glass = Terrain.Current.GlassWalls && !IgnoreGlassWalls && (thing.ActorHoldingThis == null);

                /// Record whether we need to call a bounce correction at the end.
                float bounceHeight = 0.0f;

                // 
                // Test feelers against terrain and glass walls.
                //
                for (int i = 0; i < Feelers.Count; ++i)
                {
                    CurbFeeler feeler = Feelers[i];

#if MF_VALIDATE
                    float feelerHeight = Terrain.GetTerrainAndPathHeight(Top(feeler.prevPosition));
                    if ((feeler.prevAltitude >= 0) && (Math.Abs(feelerHeight - feeler.prevAltitude) > 0.001f))
                    {
                        debugValid = false;
                    }
                    if (feelerHeight > position.Z + actorWaist)
                    {
                        debugValid = false;
                    }
#endif // MF_VALIDATE

                    if (feeler.Update(movement, position, TopOffset, thing.ReScale, HasFacingDirection, ref data))
                    {
                        float height = position.Z + actorWaist;

                        if (height <= 0.0f)
                        {
                            continue;
                        }

                        Vector2 minMax = new Vector2(-1.0f, height);
                        Vector4 maxStep = new Vector4(
                            Single.MaxValue, // max single step up
                            Single.MinValue, // max step down
                            -1.0f, // water depth at which transition land to water occurs (-1 to ignore)
                            -1.0f); // water depth at which transition water to land occurs (-1 to ignore)

                        if (landing && data.altitude < height)
                        {
                            /// If we hit a wall, that always counts as a terrain touch,
                            /// but if we didn't hit a wall, and we're jumping, then we'll
                            /// call that over and _not_ touching.
                            feeler.ClearTerrain();
                        }
                        if (terrainOnContact)
                        {
                            // Need a bit of a cushion here for objects that are right on the ground.
                            float eps = 0.1f;
                            if (data.altitude + eps < collisionBottom)
                            {
                                feeler.ClearTerrain();
                            }
                            if (!feeler.TerrainMaterialInfo.NoWater)
                            {
                                float waterHeight = Terrain.GetWaterHeight(data.position);
                                if (collisionBottom > waterHeight)
                                {
                                    feeler.ClearWater();
                                }
                            }
                        }

                        // TODO (****) This code which looks like it's checking for feelers getting embedded
                        // in the terrain is actually where we do all (?) of the bouncing off the terrain.
                        // Is this really what we want?
                        // Part of the problem is that the feelers are better poisitioned for side to side 
                        // collisions rather than vertical.  So, for chassis that can bounce off the terrain
                        // we should always put in an explicit test.

                        // Don't test center feeler for embedding.
                        if (i != 0)
                        {
                            // Feelers can have Z offsets which may leave them embedded in the terrain.
                            float feelerHeight = feeler.prevPosition.Z - TopOffset + feeler.Offset.Z * thing.ReScale;
                            if (feelerHeight < data.prevAltitude)
                            {
                                if (parent.CurrentState == GameThing.State.Active)
                                {
                                    // Give a slight impulse up and out.
                                    Vector3 impulse = position - data.position;
                                    impulse.Normalize();
                                    // Add upward bias.
                                    impulse.Z += 1.0f;
                                    impulse.Normalize();
                                    // Just in case either normalize caused a NaN.
                                    if (float.IsNaN(impulse.X))
                                    {
                                        impulse = Vector3.UnitZ;
                                    }
                                    accumVelocity += impulse;
                                    totalWgt += 1.0f;
                                }
                            }
                        }

                        if (data.prevAltitude == BaseChassis.kMinAltitude)
                        {
                            // Starting outside the world.

                            // Not entering.
                            if (data.altitude == BaseChassis.kMinAltitude)
                                continue;
                           
                        }
                        else
                        {
                            // Starting inside the world.
                            if (glass)
                                minMax.X = 0.0f;

                            // Not exiting and we didn't hit a wall.
                            if (data.altitude > 0 && data.altitude < height)
                                continue;
                        }

                        hitBlock.Normal = Vector3.Zero;

                        if (Terrain.Blocked(data.prevPosition, data.position, minMax, maxStep, ref hitBlock, parent.Movement.Altitude))
                        {
                            // If the normal comes back all zeros then don't consider this a valid hit.
                            // This seems to only occur when objects are moving nearly straight down and
                            // so will get handled by the chassis bounce code.
                            if (hitBlock.Normal == Vector3.Zero)
                            {
                                impactingFloor = true;
                                continue;
                            }

                            Vector3 vel = velocity;
                            // We've hit a wall so bounce but only bounce if
                            // we're not already heading away from the wall.
                            if (Vector3.Dot(vel, hitBlock.Normal) < 0.0f)
                            {
                                DynamicPropChassis dynChassis = this as DynamicPropChassis;

                                // For tumbling objects ignore the friction value and just
                                // use the CoR on all components of the velocity.  For back
                                // compat we now set the friction for tumbling objects to
                                // 0.0f.  This way, previously saved levels with different
                                // settings will still act as they used to.
                                if (dynChassis != null && dynChassis.Tumbles && Friction == 0.0f)
                                {
                                    vel *= thing.CoefficientOfRestitution;
                                    vel = Vector3.Reflect(vel, hitBlock.Normal);
                                }
                                else
                                {

                                    // Coefficient of Restitution should only apply to fraction of velocity that is aligned with 
                                    // the wall normal.  Velocity tangent to the wall normal should be left alone.
                                    Vector3 normalVelocity = Vector3.Dot(hitBlock.Normal, vel) * hitBlock.Normal;
                                    Vector3 tangentVelocity = vel - normalVelocity;

                                    vel = tangentVelocity * GameActor.FrictionDecay(Friction, dt)
                                        - CoefficientOfRestitution * normalVelocity;
                                }

                                GameActor actor = thing as GameActor;
                                if (actor != null)
                                {
                                    actor.PlayBounceEffect(hitBlock.Position, hitBlock.BlockHeight <= 0);
                                }
                            }

                            position += hitBlock.Position - data.position;

                            //position += 0.5f * dt * vel;   // Assume bounce in middle of frame.  This has the benefit
                            //                                    // of pushing the object away from the wall.
                            //                                    // Damp velocity from bounce.  Assumes blocks/walls are 1.0.
                            position += 0.01f * hitBlock.Normal;    // Push off the wall a tiny bit to help avoid tunneling issues.

                            accumVelocity += vel;
                            totalWgt += 1.0f;

                            // Collision sound.
                            if (!soundPlayed)
                            {
                                float kHardHitSpeed = 2.0f;
                                bool hard = velocity.Length() > kHardHitSpeed;
                                ushort t = feeler.TerrainMaterialInfo.TerrainType;
                                Audio.Foley.PlayCollision(thing, t);
                                soundPlayed = true;
                            }

                            /// We've messed with the position on this one trying to get it to a valid
                            /// position. See if we succeeded.
                            if (!feeler.Validate(movement, position, actorWaist, TopOffset, thing.ReScale, hasFacingDirection))
                                feelersValid = false;

                        }


                    }   // end if valid feeler.

#if MF_VALIDATE
                    feelerHeight = Terrain.GetTerrainAndPathHeight(Top(feeler.prevPosition));
                    if (Math.Abs(feelerHeight - feeler.prevAltitude) > 0.001f)
                    {
                        debugValid = false;
                    }
                    if (feelerHeight > position.Z + actorWaist)
                    {
                        debugValid = false;
                    }
#endif // MF_VALIDATE

                }   // end of loop over feelers.

                if (totalWgt > 0)
                {
                    // Note, when bouncing off the terrain, this is where the velocity is changed.
                    velocity = accumVelocity / totalWgt;
                }

                TerrainDataValid = true;
                OverPath = false;

                // Apply positional constraints imposed by the brain.
                ApplyConstraints(movement, ref position, ref velocity);

                // Adjust for brain-driven up/down movement.
                ApplyHeightOffsetReset(movement, position, thing.ReScale, thing.StayAboveWater);

                // Ensure that we're still in a valid location.
                if (feelersValid)
                {
                    // In some cases the bounce can cause the center of the actor to move to an invalid spot.
                    // So check if this happens and just use the previous position.
                    float newHeight = Terrain.GetTerrainAndPathHeight(Top(position), ref overPath);

                    // Check for bouncing into a terrain wall.
                    float height = position.Z + actorWaist;
                    // First clause notes that it can't be a wall if there's no terrain or path there.
                    bool hitWall = (newHeight > 0) && (newHeight > height);

                    // Check for crossing glass.
                    bool glassCrossed = glass && ((InsideGlassWalls && newHeight == 0) || (!InsideGlassWalls && newHeight != 0));

                    if (hitWall || glassCrossed)
                    {
                        if (height > 0)
                        {
                            bounceHeight = newHeight;
                            position = prevPosition;
                        }
                        else
                        {
                            position.X = prevPosition.X;
                            position.Y = prevPosition.Y;
                        }
                    }
                }
                else
                {
                    // We've already adjusted the velocity above.
                    position = prevPosition;
                }

                // Apply the changes.
                movement.Position = position;
                movement.Velocity = velocity;

                if (bounceHeight > 0)
                {
                    BounceOffGround(thing, bounceHeight, Vector3.UnitZ);
                }
            }
#if MF_VALIDATE
            else
            {
                debugValid = false; // breakpoint
            }
            if (!debugValid)
            {
                debugValid = false;
            }
#endif // MF_VALIDATE

        }   // end of BaseChassis CollideWithTerrainWalls()


        public virtual void CollisionResponse(Movement movement)
        {
        }   // end of BaseChassis CollisionResponse()

        /// <summary>
        /// Calcs the terrain altitude and normal based on the current feelers.
        /// </summary>
        /// <param name="movement"></param>
        /// <param name="terrainAltitude"></param>
        /// <param name="terrainNormal"></param>
        public void GetTerrainAltitudeAndNormalFromFeelers(Movement movement, ref float terrainAltitude, ref Vector3 terrainNormal)
        {
            Vector3 prevPosition = movement.PrevPosition;

            // On the first frame or if dead, the feeler data is invalid so ignore.
            if (Feelers[0].prevAltitude == BaseChassis.kMinAltitude 
                || Feelers[0].prevPosition == Vector3.Zero 
                || parent.CurrentState == GameThing.State.Dead 
                || parent.CurrentState == GameThing.State.Squashed)
            {
                terrainAltitude = Terrain.GetHeight(Top(prevPosition));
                terrainNormal = Vector3.UnitZ;
            }
            else
            {
                terrainAltitude = Feelers[0].prevAltitude;

                if (Feelers.Count == 1)
                {
                    terrainNormal = Terrain.GetNormal(Feelers[0].prevPosition);
                }
                else
                {
                    // Skip the first feeler since we've already got it.
                    for (int i = 1; i < Feelers.Count; i++)
                    {
                        terrainAltitude = Math.Max(terrainAltitude, Feelers[i].prevAltitude);

                        // Also calc the slope of the terrain based on the feelers.
                        Vector3 feelerDir = Feelers[i].prevPosition - Feelers[0].prevPosition;
                        feelerDir.Z += Feelers[i].prevAltitude - Feelers[0].prevAltitude;
                        Vector3 right = Vector3.Cross(feelerDir, Vector3.UnitZ);
                        Vector3 normal = Vector3.Cross(right, feelerDir);
                        normal.Normalize();
                        terrainNormal += normal;
                    }

                    terrainNormal.Normalize();
                }
            }
        }   // end of GetTerrainAltitudeAndNormalFromFeelers()

        public Object Clone()
        {
            BaseChassis clone = MemberwiseClone() as BaseChassis;
            clone.feelers = new List<CurbFeeler>(Feelers.Count);
            for (int i = 0; i < Feelers.Count; ++i)
            {
                clone.Feelers.Add(new CurbFeeler(Feelers[i].Offset));
            }
            clone.TerrainDataValid = false;
            return clone;
        }

        /// <summary>
        /// Based on the chassis' internal values, sets the blend values for the 
        /// four standard looping animations.  Should be overridden for each chassis
        /// type.  The default implementation just idles.  Note, the sum of the
        /// weights must be 1.0f.
        /// </summary>
        /// <param name="anims"></param>
        /// <param name="movement"></param>
        public virtual void SetLoopedAnimationWeights(AnimationSet anims, Movement movement, DesiredMovement desiredMovement)
        {
            anims.IdleWeight = 1.0f;
            anims.ForwardWeight = 0.0f;
            anims.BackwardsWeight = 0.0f;
            anims.RightWeight = 0.0f;
            anims.LeftWeight = 0.0f;
        }   // end of SetLoopedAnimationWeights()

        /// <summary>
        /// A standard implementation of SetLoopedAnimationWeights() that works for most moving bots.
        /// It's not right for everything but at least 4 different chassis were using it so it 
        /// seemed right to make the code common.
        /// </summary>
        /// <param name="anims"></param>
        /// <param name="movement"></param>
        public virtual void StandardSetLoopedAnimationWeights(AnimationSet anims, Movement movement, DesiredMovement desiredMovement)
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
            else if(desiredMovement.DesiredRotationAngle.HasValue || desiredMovement.DesiredRotationRate.HasValue)
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

            // Bias weights toward either full on or off with a 10% flat area.
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
            }

            // Set resulting weights on animation set.
            anims.IdleWeight = idleWeight;
            anims.ForwardWeight = forwardWeight;
            anims.BackwardsWeight = backwardWeight;
            anims.RightWeight = rightWeight;
            anims.LeftWeight = leftWeight;
        }   // StandardSetLoopedAnimationWeights()

        /// <summary>
        /// How much room (worst case) do we need to come to a stop.
        /// </summary>
        /// <returns></returns>
        public float SafeStoppingDistance()
        {
            float distance = 0.0f;
            float accel = Math.Min(MaxLinearAcceleration, MaxLinearDeceleration);
            if (accel > 0.0f)
            {
                /// Don't know which of the acceleration values the derived chassis
                /// will use for slowing down. We'll take worst case here.
                float speed = MaxSpeed * MovementSpeedModifier;
                distance = speed * speed * 0.5f / accel;
            }
            return distance;
        }
        /// <summary>
        /// What's a reasonable guess at our turning radius.
        /// </summary>
        /// <returns></returns>
        public float SafeTurningRadius()
        {
            float radius = 0.0f;
            if (MaxRotationalAcceleration > 0)
            {
                radius = MaxSpeed * MovementSpeedModifier / MaxRotationalAcceleration;
            }
            return radius;
        }
        /// <summary>
        /// How much time would it take to stop now.
        /// </summary>
        /// <returns></returns>
        public float SafeStoppingTime(Vector3 vel)
        {
            float accel = Math.Min(MaxLinearAcceleration, MaxLinearDeceleration);
            if (accel > 0)
            {
                return vel.Length() / accel;
            }
            return 0.0f;
        }
        /// <summary>
        /// How much time would it take to come to a stop, ignoring vertical movement.
        /// </summary>
        /// <param name="vel"></param>
        /// <returns></returns>
        public float SafeStoppingTime(Vector2 vel)
        {
            float accel = Math.Min(MaxLinearAcceleration, MaxLinearDeceleration);
            if (accel > 0)
            {
                return vel.Length() / accel;
            }
            return 0.0f;
        }

        public float SafeAversionDistance()
        {
            float stop = SafeStoppingDistance();
            float turn = SafeTurningRadius();
            return Math.Max(stop, turn);
        }

        #endregion

        #region Internal

        //
        // Helper functions which may be used by any chassis.
        //

        /// <summary>
        /// See if it's time to make some more ripples, either because
        /// enough time has passed or because we've moved from the last
        /// spot.
        /// </summary>
        /// <param name="movement"></param>
        /// <param name="radius"></param>
        protected virtual void CheckRipples(GameThing gameThing, float radius)
        {
            if (!gameThing.Invisible)
            {
                Movement movement = gameThing.Movement;
                Vector2 pos2 = new Vector2(movement.Position.X, movement.Position.Y);
                double t = Time.GameTimeTotalSeconds;
                float movingRippleRadius = radius;
                float rippleWait = radius / Fx.Ripple.WaveSpeed * 9.0f / 16.0f;
                float stillRippleRadius = movingRippleRadius * 2.0f;
                float rippleDistSq = movingRippleRadius * movingRippleRadius * 0.5f * 0.5f;
                if (Vector2.DistanceSquared(pos2, lastRipple) > rippleDistSq)
                {
                    Fx.Ripple.Add(movement.Position, movingRippleRadius);
                    lastRipple = pos2;
                    nextRipple = t + rippleWait;
                }
                else if (t >= nextRipple)
                {
                    Fx.Ripple.Add(movement.Position, stillRippleRadius);
                    nextRipple = t + rippleWait;
                }
            }
        }

        /// <summary>
        /// Check the velocity and if it's high enough, make a splash.
        /// </summary>
        /// <param name="pos"></param>
        /// <param name="vel"></param>
        /// <returns></returns>
        protected bool CheckSplash(GameThing gameThing, Vector3 pos, Vector3 vel)
        {
            if (!gameThing.Invisible)
            {
                float speed = vel.Length();
                float kMinSplashSpeed = 5.0f;
                if (speed > kMinSplashSpeed)
                {
                    vel.Z = vel.Z > 0 ? vel.Z : -vel.Z;

                    float kMaxSplashSpeed = 20.0f;
                    float strength = (speed - kMinSplashSpeed) / (kMaxSplashSpeed - kMinSplashSpeed);

                    float kMinCount = 3;
                    float kMaxCount = 15;
                    int count = (int)(kMinCount + strength * (kMaxCount - kMinCount) + 1.0f);

                    ExplosionManager.CreateSplashes(count, pos, vel, 0.1f, Vector4.One);

                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Compute the maximum terrain altitude under all feelers.
        /// </summary>
        /// <param name="movement"></param>
        /// <param name="position"></param>
        /// <returns></returns>
        protected float MaxFeelerAltitude(Movement movement, Vector3 position, float rescale)
        {
            float maxHeight = 0.0f;
            for (int i = 0; i < feelers.Count; ++i)
            {
                float height = feelers[i].Altitude(movement, position, TopOffset, rescale, HasFacingDirection);
                maxHeight = Math.Max(maxHeight, height);
            }
            return maxHeight;
        }

        /// <summary>
        /// Do whatever you do when you've smacked downward into the terrain.
        /// This is for vertical hits only. Default is to bounce the vertical
        /// component of the velocity.
        /// </summary>
        /// <param name="movement"></param>
        protected virtual void BounceOffGround(GameThing thing, float terrainHeight, Vector3 terraNormal)
        {
            Movement movement = thing.Movement;
            if (movement.Altitude > 0)
            {
                Vector3 velocity = movement.Velocity;

                if (velocity.Z < 0)
                {
                    velocity.Z = -velocity.Z * CoefficientOfRestitution;
                }

                movement.Velocity = velocity;
            }
        }

        /// <summary>
        /// Filters out the list of avoid locations to remove those
        /// behind the object or too far away.
        /// </summary>
        /// <param name="movement"></param>
        /// <param name="desiredMovement"></param>
        protected void FilterAvoidLocations(Movement movement, DesiredMovement desiredMovement)
        {
            GameActor actor = Parent as GameActor;

            // If we have a target we're moving toward, ignore anything
            // beyond the target distance.
            float targetLocationDist = float.MaxValue;
            if (desiredMovement.DesiredTargetLocation.HasValue)
            {
                Vector3 loc = desiredMovement.DesiredTargetLocation.Value;
                targetLocationDist = (loc - movement.Position).Length();
            }

            // Pre-filter avoid locations.  Get rid of ones
            // that are behind us or too far away to matter.
            for (int i = desiredMovement.AvoidTargets.Count - 1; i >= 0; --i)
            {
                GameActor target = desiredMovement.AvoidTargets[i];

                // minDist is the minimum distance we want to be.
                // We can adjust this scalar as  needed.
                float minDist = 1.0f * actor.TotalGrabRange(target);

                // maxDist is the outer limit of what we consider.  This
                // is an arbitrary multiplier on the minDist but also
                // limited iff we're moving toward a target location. If the 
                // avoid target is further away than this, we'll ignore it.
                float maxDist = minDist * 5.0f;
                maxDist = Math.Min(maxDist, targetLocationDist);

                Vector3 toTarget = target.Movement.Position - movement.Position;
                float dist = toTarget.Length();
                toTarget /= dist;   // Normalize.

                // Remove if behind us.
                if (Vector3.Dot(toTarget, movement.Facing) < 0.0f)
                {
                    desiredMovement.AvoidTargets.RemoveAt(i);
                    continue;
                }

                // Too far away?
                if (dist > maxDist)
                {
                    desiredMovement.AvoidTargets.RemoveAt(i);
                    continue;
                }

            }   // end of loop over locations.

            // Sort any remaining targets by distance.
            for (int i = 0; i < desiredMovement.AvoidTargets.Count - 2; i++)
            {
                for (int j = desiredMovement.AvoidTargets.Count - 2; j >= i; --j)
                {
                    Vector3 toTarget = desiredMovement.AvoidTargets[j].Movement.Position - movement.Position;
                    float dist0 = toTarget.LengthSquared();
                    toTarget = desiredMovement.AvoidTargets[j+1].Movement.Position - movement.Position;
                    float dist1 = toTarget.LengthSquared();
                    if (dist0 > dist1)
                    {
                        // Swap.
                        GameActor tmp = desiredMovement.AvoidTargets[j];
                        desiredMovement.AvoidTargets[j] = desiredMovement.AvoidTargets[j + 1];
                        desiredMovement.AvoidTargets[j + 1] = tmp;
                    }
                }
            }

        }   // end of FilterAvoidLocations

        /// <summary>
        /// Assumes the heading is the exisitng desired heading.  Adjust this
        /// to avoid and avoid targets and returns a new heading.
        /// Assumes that all non relevant avoid targets have already been 
        /// filtered out.  Maybe even using the function above this one. :-)
        /// </summary>
        /// <param name="desiredMovement"></param>
        /// <param name="heading"></param>
        /// <returns></returns>
        protected Vector3 ApplyAvoidTargets(DesiredMovement desiredMovement, Vector3 heading)
        {
            GameActor actor = Parent as GameActor;
            Vector3 newHeading = heading;

            // Loop over targets, far to near and adjust direction we want to
            // go to avoid them.
            // TODO (****) For future tweak, may want to consider the distance
            // to the avoid target and attenuate the effect of targets which
            // are futher away.
            for (int i = desiredMovement.AvoidTargets.Count - 1; i >= 0; --i)
            {
                GameActor target = desiredMovement.AvoidTargets[i];
                float minDist = 1.0f * actor.TotalGrabRange(target);
                Vector3 toTarget = desiredMovement.AvoidTargets[i].Movement.Position - actor.Movement.Position;
                toTarget.Z = 0;
                float dist = toTarget.Length();

                // Calc point on heading vector that's at same distance as avoid target.
                Vector3 pointAlongHeading = actor.Movement.Position + dist * newHeading;
                
                // delta is difference between avoid target position and nearest point in heading direction.
                Vector3 delta = pointAlongHeading - target.Movement.Position;
                delta.Z = 0;
                float deltaLen = delta.Length();

                if (deltaLen > minDist)
                {
                    // Clearly missing this avoid target so don't do anything.
                    continue;
                }

                // Project point onto new heading path and update new heading.
                Vector3 newPoint = desiredMovement.AvoidTargets[i].Movement.Position + delta * minDist / deltaLen;
                newHeading = newPoint - actor.Movement.Position;
                newHeading.Normalize();
            }

            return newHeading;
        }   // end of ApplyAvoidTargets()

        /// <summary>
        /// Applies the DesiredMovement values from the brain to the chassis.
        /// This is public since some chassis may share the same code with each other
        /// and that beats cloning the code.
        /// </summary>
        /// <param name="movement"></param>
        /// <param name="desiredMovement"></param>
        public virtual void ApplyDesiredMovement(Movement movement, DesiredMovement desiredMovement) 
        {
            Debug.Assert(false, "NotImpl");
        }


        /// <summary>
        /// Return the angle normalized to the range -pi..pi
        /// 
        /// TODO (****) Should probably be in MathHelper.
        /// </summary>
        /// <param name="angle"></param>
        /// <returns></returns>
        protected static float NormalizeAngle(float angle)
        {
            if (angle > MathHelper.Pi)
            {
                angle -= MathHelper.TwoPi;
            }
            else if (angle < -MathHelper.Pi)
            {
                angle += MathHelper.TwoPi;
            }

            return angle;
        }   // end of NormalizeAngle()

        protected float AngleBetweenVectors(Vector3 v0, Vector3 v1)
        {
            v0.Normalize();
            v1.Normalize();
            float dot = Vector3.Dot(v0, v1);
            dot = MathHelper.Clamp(dot, -1, 1);
            // Get absolute angle
            float angle = (float)Math.Acos(dot);

            // Figure out sign.
            Vector3 cross = Vector3.Cross(v0, v1);
            if (cross.Z < 0)
                angle = -angle;

            return angle;
        }


        /// <summary>
        /// Apply any position constraints put on the chassis by the brain.
        /// Note we don't clear the contraints here since this gets applied
        /// more than once per frame.
        /// </summary>
        /// <param name="position"></param>
        /// <returns></returns>
        public void ApplyConstraints(Movement movement, ref Vector3 position, ref Vector3 velocity)
        {
            if ((Constraints & ConstraintModifier.Constraints.NorthSouth) != 0)
            {
                float z = position.Z;
                position = MyMath.NearestPointOnLine(position, ConstraintInitialPosition, ConstraintInitialPosition + Vector3.UnitY);
                position.Z = z;
                //Constraints &= ~(ConstraintModifier.Constraints.NorthSouth);
            }

            if ((Constraints & ConstraintModifier.Constraints.EastWest) != 0)
            {
                float z = position.Z;
                position = MyMath.NearestPointOnLine(position, ConstraintInitialPosition, ConstraintInitialPosition + Vector3.UnitX);
                position.Z = z;
                //Constraints &= ~(ConstraintModifier.Constraints.EastWest);
            }

            if ((Constraints & ConstraintModifier.Constraints.UpDown) != 0)
            {
                position = MyMath.NearestPointOnLine(position, ConstraintInitialPosition, ConstraintInitialPosition + Vector3.UnitZ);
                //Constraints &= ~(ConstraintModifier.Constraints.UpDown);
            }

            if ((Constraints & ConstraintModifier.Constraints.Immobile) != 0)
            {
                position = new Vector3(ImmobilePosition.X, ImmobilePosition.Y, ImmobilePosition.Z);
                velocity = Vector3.Zero;
                //Constraints &= ~(ConstraintModifier.Constraints.Immobile);
            }

        } // end of ApplyConstraints()

        private void ApplyHeightOffsetReset(Movement movement, Vector3 position, float rescale, bool stayAboveWater)
        {
            // If the brain has told us to fly up or down then it has also set the ResetHeightOffset flag.
            // This flag tell us to update our HeightOffset value to match our new altitude.

            if (ResetHeightOffset)
            {
                // Adjust height for terrain and water.
                float terrainAltitude = MaxFeelerAltitude(movement, position, rescale);
                float waterAltitude = Terrain.GetWaterBase(position);
                // altitudeBase is the ground/water we're flying over and basing our height on.
                float altitudeBase = stayAboveWater ? MathHelper.Max(terrainAltitude, waterAltitude) : terrainAltitude;

                HeightOffset = position.Z - altitudeBase - DefaultEditHeight;

                ResetHeightOffset = false;  // Clear flag.
            }
        } // end of ApplyHeightOffsetReset

        /// <summary>
        /// Apply the brain's desired velocity changes.
        /// HoverChassis version.
        /// </summary>
        /// <param name="movement"></param>
        /// <param name="desiredMovement"></param>
        public void ApplyDesiredVelocityForHover(Movement movement, DesiredMovement desiredMovement)
        {
            float tics = Time.GameTimeFrameSeconds;
            GameActor actor = Parent as GameActor;

            // If AutoTurn is true, then either our acceleration or target should determine
            // which way we want to face.
            Vector3 desiredFacingDir = movement.Facing;

            // First, start with things that affect our velocity.

            if (desiredMovement.DesiredVelocity.HasValue || desiredMovement.DesiredTargetLocation.HasValue)
            {
                // Remove any avoid targets that are behind the actor or too far away.
                // Sort the rest by distance.
                FilterAvoidLocations(movement, desiredMovement);

                Vector3 velocity = movement.Velocity;

                // Moving in a direction?
                if (desiredMovement.DesiredVelocity.HasValue)
                {
                    // Accelerate in the desired direction.  Note that we're clamping the movement
                    // to the facing direction unless we are also actively turning.  In that case
                    // we're probably doing something like mouse-look so allow the bot to move in
                    // a direction it's not facing.
                    if (desiredMovement.DesiredRotationAngle.HasValue)
                    {
                        velocity += desiredMovement.DesiredVelocity.Value * desiredMovement.MaxLinearAcceleration * tics;
                    }
                    else
                    {
                        if (HasFacingDirection)
                        {
                            float dot = Vector3.Dot(desiredMovement.DesiredVelocity.Value, movement.Facing);
                            velocity += dot * movement.Facing * desiredMovement.MaxLinearAcceleration * tics;
                        }
                        else
                        {
                            // For actors that don't have a facing direction...
                            velocity += desiredMovement.DesiredVelocity.Value * desiredMovement.MaxLinearAcceleration * tics;
                        }
                    }

                    // Turn to face the direction we want to go.
                    desiredFacingDir = desiredMovement.DesiredVelocity.Value;
                }

                // Moving toward a target?
                if (desiredMovement.DesiredTargetLocation.HasValue)
                {
                    Vector3 toObject = desiredMovement.DesiredTargetLocation.Value - movement.Position;

                    // Ignore the Z component.  This makes the distance 2D which works better since
                    // hover chassis stuff is mostly fixed for height.  Without this, the distance also
                    // takes the vertical into account and when we decide whether or not we're close enough 
                    // to slow down we underestimate and end up overshooting.
                    // Calc dist as a 2D distance.
                    float distToObject = (float)Math.Sqrt(toObject.X * toObject.X + toObject.Y * toObject.Y);
                    // Normalize the toObject vector relative to the XY components.
                    if (distToObject != 0)
                    {
                        toObject /= distToObject;
                    }

                    // Adjust for avoid targets.
                    toObject = ApplyAvoidTargets(desiredMovement, toObject);

                    // Face object we're moving toward.
                    desiredFacingDir = toObject;

                    float speed = velocity.Length();

                    // Do we need to start slowing down because we're getting close?
                    float distToDecelerateToZero = 0.5f * speed * speed / desiredMovement.MaxLinearAcceleration;

                    if (distToObject > distToDecelerateToZero)
                    {
                        // We're still far enough away, apply the acceleration.
                        // If we accelerate direclty toward target, this leads to orbiting.

                        if (speed > 0.1f)
                        {
                            // Split velocity into orthogonal components.
                            Vector3 right = Vector3.Cross(toObject, Vector3.UnitZ);
                            float speedRight = Vector3.Dot(right, velocity);
                            float maxDeltaSpeed = desiredMovement.MaxLinearAcceleration * tics;

                            // Fraction of acceleration we should apply to right vs toObject.
                            float fraction = Math.Min(Math.Abs(speedRight) / maxDeltaSpeed, 1.0f);

                            // Apply fraction of our acceleration toward right.
                            // This flattens out the trajectory and prevents orbiting.
                            velocity -= Math.Sign(speedRight) * fraction * right * desiredMovement.MaxLinearAcceleration * tics;

                            // Apply remaining acceleration toward object.
                            fraction = 1.0f - fraction;
                            fraction = 1.0f;    // Or just apply all?
                            if (fraction > 0)
                            {
                                velocity += fraction * toObject * desiredMovement.MaxLinearAcceleration * tics;
                            }

                            speed = velocity.Length();
                            if (speed > desiredMovement.MaxSpeed)
                            {
                                velocity *= desiredMovement.MaxSpeed / speed;
                            }
                        }
                        else
                        {
                            // Barely moving so just go straight to target object.
                            velocity += toObject * desiredMovement.MaxLinearAcceleration * tics;
                        }
                    }
                    else
                    {
                        // Too fast, start slowing down now.
                        float deltaSpeed = desiredMovement.MaxLinearAcceleration * tics;
                        if (speed > deltaSpeed)
                        {
                            velocity *= (speed - deltaSpeed) / speed;
                        }
                        else
                        {
                            velocity = Vector3.Zero;
                        }
                    }
                }

                // If we don't have any active turning, and autoTurn is set, turn in the direction we're accelerating.
                if (desiredMovement.AutoTurn && !desiredMovement.DesiredRotationAngle.HasValue && !desiredMovement.DesiredRotationRate.HasValue)
                {
                    if (actor != null)
                    {
                        desiredFacingDir.Z = 0;
                        desiredFacingDir.Normalize();
                        // If this vector was 0 then Normalize will result
                        // in NaNs so just replace with current facing dir.
                        if (float.IsNaN(desiredFacingDir.X))
                        {
                            desiredFacingDir = movement.Facing;
                        }
                        // Allow moving backwards but only if user controlled.  
                        if (movement.UserControlled)
                        {
                            // If dot product between facing direction
                            // and acceleration is negative then we're trying to move backwards.
                            float dot = desiredFacingDir.X * movement.Facing.X + desiredFacingDir.Y * movement.Facing.Y;
                            if (dot < 0)
                            {
                                //desiredFacingDir = -desiredFacingDir;
                                //desiredFacingDir = Vector3.Reflect(desiredFacingDir, movement.Facing);
                            }
                        }
                        float desiredHeading = MyMath.ZRotationFromDirection(desiredFacingDir);
                        //float strength = 1.01f - Math.Abs(Vector3.Dot(desiredFacingDir, movement.Facing));
                        float strength = 1.0f;    // Force to turn quickly.
                        if (strength > 0.000001f)
                        {
                            // If we're already facing the correct direction, just clamp rotation to 0.
                            // This helps prevent overshooting at low frame rates.
                            if (Math.Abs(desiredHeading - movement.RotationZ) < 0.01)
                            {
                                movement.RotationZRate = 0;
                            }
                            else
                            {
                                desiredMovement.SetDesiredRotationAngle(desiredHeading, strength * actor.CalcTurnSpeed(), strength * actor.CalcTurnAcceleration());
                            }
                        }
                    }
                }

                // Clamp to max speed but only in 2d so falling still works.
                {
                    Vector2 velocity2D = new Vector2(velocity.X, velocity.Y);
                    float speed = velocity2D.Length();
                    if (speed > desiredMovement.MaxSpeed)
                    {
                        // Instead of hard clamping, we just decelerate 
                        // very fast.  This ends up looking smoother.
                        // Lerp toward target velocity.
                        Vector2 targetVelocity = velocity2D * (desiredMovement.MaxSpeed / speed);

                        // This depends on Time never allowing frame time to be longer than 0.2 seconds.
                        // If this throws, that means that the max frame time has been increased and
                        // we need to change the equation for t since it should always be in the 0..1 range.
                        Debug.Assert(tics <= 0.2f);
                        float t = tics * 4.0f;
                        
                        velocity2D = MyMath.Lerp(velocity2D, targetVelocity, t);
                    }
                }

                movement.Velocity = velocity;
            }   // end if changing velocity.

        }   // end of ApplyDesiredVelocityForHover()

        /// <summary>
        /// Apply the brain's desired rotation changes. 
        /// This is standard turning for Hover chassis and may be applied to others.
        /// </summary>
        /// <param name="movement"></param>
        /// <param name="desiredMovement"></param>
        public void ApplyDesiredRotation(Movement movement, DesiredMovement desiredMovement)
        {
            float tics = Time.GameTimeFrameSeconds;
            GameActor actor = Parent as GameActor;

            // Handle rotation.
            float rotationRate = movement.RotationZRate;

            // Are we actively turning?
            if (desiredMovement.DesiredRotationAngle.HasValue || desiredMovement.DesiredRotationRate.HasValue)
            {

                if (desiredMovement.DesiredRotationAngle.HasValue)
                {
                    float heading = movement.RotationZ;
                    float desiredHeading = desiredMovement.DesiredRotationAngle.Value;
                    float rotationalAcceleration = desiredMovement.MaxRotationalAcceleration;

                    // Ok, need to figure out which way to try and turn. If we're not already turning 
                    // it's totally simple BUT if we're already rotating, the short way may 
                    // not be the quickest.  Do we really care about that case? No. Just do
                    // what's simple.

                    float delta = desiredHeading - heading;
                    // Map into range (-pi, pi).
                    delta = NormalizeAngle(delta);

                    // From the current rotation rate calc how many radians will pass if 
                    // we try and stop at full deceleration.
                    float radiansToStop = movement.RotationZRate * movement.RotationZRate / rotationalAcceleration;

                    if (radiansToStop >= Math.Abs(delta))
                    {
                        // We're already going too fast (or exactly right) to stop in time so slow as fast as possible.
                        float deltaRotationRate = Math.Sign(rotationRate) * rotationalAcceleration * tics;
                        if (Math.Abs(deltaRotationRate) >= Math.Abs(rotationRate))
                        {
                            // We're going to stop this frame so just short-cut;
                            rotationRate = 0;
                        }
                        else
                        {
                            // Slow down.
                            rotationRate -= Math.Sign(rotationRate) * rotationalAcceleration * tics;
                        }
                    }
                    else
                    {
                        // We're not maxed out yet so accelerate in the right direction.
                        float dRate = rotationalAcceleration * tics;
                        // Clamp based on how far we need to go to prevent overshoot.
                        dRate = Math.Min(dRate, Math.Abs(delta) / 2.0f);
                        rotationRate += Math.Sign(delta) * dRate;
                    }
                }

                if (desiredMovement.DesiredRotationRate.HasValue)
                {
                    float desiredRate = desiredMovement.DesiredRotationRate.Value;
                    // Accelerate toward desiredRate.  Need to know whether to go positive or negative.
                    if (desiredRate > rotationRate)
                    {
                        rotationRate += desiredMovement.MaxRotationalAcceleration * tics;
                    }
                    else
                    {
                        rotationRate -= desiredMovement.MaxRotationalAcceleration * tics;
                    }
                }

                // Clamp rotation rate to chassis limits and apply to rotation.
                rotationRate = MathHelper.Clamp(rotationRate, -desiredMovement.MaxRotationRate, desiredMovement.MaxRotationRate);

                movement.RotationZRate = rotationRate;
                movement.RotationZ += rotationRate * tics;
            }
            else
            {
                // Not moving or actively turning so just damp rotation rate to 0.
                float turnAcceleration = actor.CalcTurnAcceleration();
                float deltaRate = turnAcceleration * tics;
                if (rotationRate > 0)
                {
                    rotationRate = MathHelper.Max(rotationRate - deltaRate, 0);
                }
                else
                {
                    rotationRate = MathHelper.Min(rotationRate + deltaRate, 0);
                }
            }

            // Clamp rotation rate to chassis limits and apply to rotation.
            rotationRate = MathHelper.Clamp(rotationRate, -desiredMovement.MaxRotationRate, desiredMovement.MaxRotationRate);

            movement.RotationZRate = rotationRate;
            movement.RotationZ += rotationRate * tics;

        }   // end of ApplyDesiredRotation()

        /// <summary>
        /// Apply the brain's desired vertical movement changes.
        /// 
        /// This is just simple float up/down.
        /// Note that this also affects the pitch of actor if appropriate.
        /// </summary>
        /// <param name="movement"></param>
        /// <param name="desiredMovement"></param>
        /// <param name="affectPitch"></param>
        public void ApplyDesiredVerticalMovement(Movement movement, DesiredMovement desiredMovement, bool affectPitch = true)
        {
            float tics = Time.GameTimeFrameSeconds;
            GameActor actor = Parent as GameActor;

            // Are we actively trying to change vertical speed/position?
            // Also need to take into account the case where we're following a
            // path that is at a different altitude.
            bool activeControl = !desiredMovement.CoastingVertically;

            if (activeControl)
            {
                Vector3 velocity = movement.Velocity;

                // Moving in a direction?
                if (desiredMovement.DesiredVerticalSpeed.HasValue)
                {
                    // Accelerate in the desired direction.
                    velocity.Z += Math.Sign(desiredMovement.DesiredVerticalSpeed.Value) * desiredMovement.MaxVerticalAcceleration * tics;
                }

                // Moving toward a target altitude?
                if(desiredMovement.DesiredAltitude.HasValue)
                {
                    float targetAltitude = desiredMovement.DesiredAltitude.Value;
                    float maxVerticalAcceleration = desiredMovement.MaxVerticalAcceleration;
                    float maxVertialSpeed = desiredMovement.MaxVerticalSpeed;

                    float deltaAltitude = targetAltitude - movement.Altitude;
                    float speed = velocity.Z;

                    // Do we need to start slowing down because we're getting close?
                    float distToDecelerateToZero = 0.5f * speed * speed / maxVerticalAcceleration;

                    if (Math.Abs(deltaAltitude) > distToDecelerateToZero)
                    {
                        // We're still far enough away, apply the acceleration.
                        velocity.Z += Math.Sign(deltaAltitude) * maxVerticalAcceleration * tics;
                    }
                    else
                    {
                        // Too fast, start slowing down now.
                        float deltaSpeed = maxVerticalAcceleration * tics;
                        if (speed > deltaSpeed)
                        {
                            velocity.Z *= (speed - deltaSpeed) / speed;
                        }
                        else
                        {
                            velocity.Z = 0;
                        }
                    }
                }

                // Copy result back to movement.
                movement.Velocity = velocity;
            }   // end if changing velocity.

        }   // end of ApplyDesiredVerticalMovement() 

        /// <summary>
        /// Apply drag to velocity.  We're actually modeling friction here.  This works 
        /// better than trying to model actual drag since drag is relative to v^2 which 
        /// causes it to be near 0 when the velocity is near 0 which means that nothing ever stops.
        /// We ignore friction while actively driving chassis.  Only using friction when 
        /// all Desired* members of DesiredMovement have no values.  ie only apply friction 
        /// when we're not actively trying to move.
        /// 
        /// By default, applyVertical is true so we apply friction to all 3 axes. If false
        /// the friction is only applied to the horizontal axes (X and Y).
        /// </summary>
        /// <param name="movement"></param>
        /// <param name="applyVertical"></param>
        public void ApplyFriction(Movement movement, DesiredMovement desiredMovement, bool applyVertical = true)
        {
            if (Friction > 0.0f)
            {
                float secs = Time.GameTimeFrameSeconds;
                Vector3 velocity = movement.Velocity;
                float damping = 1.0f - Friction * secs;
                damping *= damping * damping;

                if (desiredMovement.CoastingHorizontally)
                {
                    // Apply friction to horizontal movement.
                    velocity.X *= damping;
                    velocity.Y *= damping;

                    // Since we use the facing direction to control the friction
                    // we need to be able to handle bots with no facing direction.
                    // In this case, just pretent that the bot is facing the 
                    // direction it is currently moving.  This will cause it to
                    // slow in a stright line instead of curving.
                    Vector3 facing = movement.Facing;
                    if (!HasFacingDirection)
                    {
                        facing = velocity;
                        if (facing != Vector3.Zero)
                        {
                            facing.Normalize();
                        }
                    }

                    // Do additional damping to velocity perpendicular to facing direction.
                    // This stops bots from feeling to slippery when going sideways.
                    // We do this by splitting the velocity into orthogonal components using
                    // the facing direction and a right vector.  The damping is only applied
                    // to the contribution from the right vector.
                    Vector3 right = new Vector3(facing.Y, -facing.X, 0);

                    float facingDot = Vector3.Dot(velocity, facing);
                    float rightDot = Vector3.Dot(velocity, right);

                    Vector3 vFacing = facingDot * facing;
                    Vector3 vRight = rightDot * right;

                    velocity.X = vFacing.X + damping * damping * vRight.X;
                    velocity.Y = vFacing.Y + damping * damping * vRight.Y;

                    // If trying to go backwards, damp even further.
                    if (facingDot < 0)
                    {
                        velocity.X = damping * damping * vFacing.X + damping * damping * vRight.X;
                        velocity.Y = damping * damping * vFacing.Y + damping * damping * vRight.Y;
                    }
                }
                else
                {
                    if (!applyVertical && desiredMovement.MaxSpeed > 0)
                    {
                        // Special case hack here.  If we're moving fast and we now have
                        // a slower top speed, apply some friction.
                        if (movement.Speed > desiredMovement.MaxSpeed)
                        {
                            // Apply friction to horizontal movement.
                            velocity.X *= damping;
                            velocity.Y *= damping;
                        }
                    }
                }

                if (applyVertical && desiredMovement.CoastingVertically)
                {
                    // Apply friction to vertical movement.
                    velocity.Z *= damping;
                }

                movement.Velocity = velocity;
            }
        }   // end of ApplyFriction()

        /// <summary>
        /// Tests current bot for vertical collision with ground.  This does not
        /// use feelers, just the collision sphere.
        /// </summary>
        /// <param name="movement"></param>
        /// <returns>True if we collide, false otherwise.</returns>
        public bool CollideWithGround(Movement movement, ref float height)
        {
            bool bounce = false;

            Vector3 pos = movement.Position;

            float terrain = Terrain.GetTerrainAndPathHeight(pos);
            float bottom = pos.Z - Parent.CollisionCenter.Z - Parent.CollisionRadius;

            height = bottom - terrain;

            // Only bounce if there is ground and if we're below the surface, but not if we're fully under. 
            if (terrain > 0 && height <= 0 && height > -2)
            {
                // Move back to surface.
                pos.Z -= height;
                movement.Position = pos;
                height = 0;

                Vector3 normal = Terrain.GetTerrainNormal(pos);

                // If already moving in same direction as terrain normal, don't
                // do anything.  If moving into ground, then reflect velocity vector
                // across normal.

                if (Vector3.Dot(movement.Velocity, normal) < 0)
                {

                    // Reflect velocity across normal.
                    Vector3 velocity = movement.Velocity;
                    velocity = Vector3.Reflect(velocity, normal);

                    // Damp.
                    velocity *= Parent.CoefficientOfRestitution;

                    // Ensure velocity is upward.  Yes, this is not always correct but it will help
                    // prevent burrowing into the ground.
                    //velocity.Z = Math.Abs(velocity.Z);
                    movement.Velocity = velocity;
                }

                bounce = true;
            }

            return bounce;
        }   // end of CollideWithGround()

        #endregion

    }   // end of class BaseChassis

}   // end of namespace Boku.Base
