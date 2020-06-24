
// If you uncomment this then the randomness will be taken out of
// the initial position and velocity of created/droppped items.
#define NO_RANDOM

using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Diagnostics;
using System.IO;
using System.Xml.Serialization;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

using KoiX;

using Boku;
using Boku.Fx;
using Boku.Common;
using Boku.Common.ParticleSystem;
using Boku.SimWorld;
using Boku.SimWorld.Chassis;
using Boku.SimWorld.Terra;
using Boku.SimWorld.Collision;
using Boku.SimWorld.Path;
using Boku.Scenes;
using Boku.Programming;
using Boku.Audio;
using Boku.Common.Xml;

using Boku.Animatics;

namespace Boku.Base
{
    public delegate FBXModel GetModelInstance();

    public class GameActor : GameThing
    {
        /// <summary>
        /// These used to be Rover specific but are being renamed
        /// so they can be used by any character.  No, the renaming
        /// doesn't change any functionality but it seems like 
        /// having "rover" in the name no longer makes sense.
        /// </summary>
        public enum ScienceAction
        {
            None,
            Beam,
            Inspect,
            Scan
        }

        public readonly StaticActor StaticActor;

        /// <summary>
        /// ActorFactory bookkeeping: Whether this actor is in the factory's free bin.
        /// </summary>
        [XmlIgnore]
        public bool InRecycleBin;

        /// <summary>
        /// ActorFactory bookkeeping: Whether this actor was created by the factory.
        /// </summary>
        [XmlIgnore]
        public bool FactoryCreated;


        protected Brain brain;
        protected AnimationSet animationSet = null;

        [XmlIgnore]
        public FollowWaypointsSelector.State followPathState;

        private bool invulnerable;

        private int currHitPoints;
        private int prevHitPoints;
        private int healthbar = HealthBarManager.kInvalidHandle;

        [XmlIgnore]
        public ScoreSet localScores = new ScoreSet();

        private string displayName;     // Actor name.  By default, this is the type of actor.
        private int displayNumber;      // Number appended to name to ensure uniqueness.

        private Guid creatableId = Guid.Empty;
        private Distortion creatableAura;
        private LuzMgr.GlowLuz glowLuz = null;

        private SensorTargetSet touchSpoofs = new SensorTargetSet();
        private SensorTargetSet.Enumerator touchSpoofsIter;
        private SensorTargetSet touchedSet = new SensorTargetSet();
        private SensorTargetSet.Enumerator touchedSetIter;
        private SensorTargetSet givenSet = new SensorTargetSet();
        private SensorTargetSet eatenSet = new SensorTargetSet();
        private SensorTargetSet missileHitSet = new SensorTargetSet();  // Targets my shots have hit this frame.
        private SensorTargetSet shooterHitSet = new SensorTargetSet();  // Shooters that have hit me this frame.

        // The following modifiers are set based on the execution of the 
        // Settings->MoveSpeed and Settings->TurnSpeed tiles during runtime.
        // They must be reset at the start of each run and should not be persisted.
        private float moveSpeedTileModifier = 1.0f;
        private float turnSpeedTileModifier = 1.0f;

        // SGI_MOD beam related
        private SensorTargetSet beamedSet = new SensorTargetSet();
        private SensorTargetSet.Enumerator beamedSetIter;

        private SensorTargetSet inspectedSet = new SensorTargetSet();
        private SensorTargetSet.Enumerator inspectedSetIter;

        private SensorTargetSet scannedSet = new SensorTargetSet();
        private SensorTargetSet.Enumerator scannedSetIter;

        private BleepManager.BleepData _bleeps = null;
        // SGI_MOD - Beam related
        private BeamManager.BeamData _beams = null;

        // SGI_MOD - One time beam/inspect only
        private bool _beamed;
        private bool _beamedSoundPlayed;
        private Vector3 _beamTargetPosition;
        private Classification.Colors _beamColor;
        private bool _inspected;


        // SGI_MOD - if the object has reached end of path
        private Classification.Colors _reachedEOP;
        private bool _readyToProcessEOP;

        // If the actor has performed a science specific action.
        private bool scienceActionPerformed;
        // Which action we're waiting for.
        private ScienceAction currentScienceAction;
        // How long to delay the actor from moving.
        private float scienceActionDelay;
        // How much time left before delay is over.
        private float scienceActionTimer;
        private GameActor scienceActorTarget;

        // When performing science actions, we first rotate to face the target.
        private bool scienceActorRotating;
        private Vector3 scienceActorDirection;
        private bool scienceActorMuted;
        private float PrevMaxRotationAcceleration;
        private float PrevMaxRotationRate;

        // SGI_MOD - Scan related
        // Track whether or not this object has been revealed.
        private bool _revealed;
		// Track whether we need to update the render object we're using due to a reveal.
        private bool _needsRevealUpdate;

        // tracks whether the rover has initiated a scan or not
        private bool _scanned;
        // how long it takes to reveal each ring
        private float _scanRevealDelay;
        // how much time before next ring is revealed
        private float _scanRevealTimer;
        // Rng that we are scanning
        private int _scanningRing;

        /// <summary>
        /// Scale value used when actor is squashed.
        /// </summary>
        private Vector3 squashScale = Vector3.One;      // Actual (current) value.
        private Vector3 _squashScale = Vector3.One;     // Twitch target value.

        public Vector3 SquashScaleTarget = new Vector3(1.2f, 1.2f, 0.1f);
        /// <summary>
        /// Scale value used when actor is squashed.
        /// </summary>
        [XmlIgnore]
        public Vector3 SquashScale
        {
            get { return squashScale; }
            set
            {
                if (_squashScale != value)
                {
                    _squashScale = value;
                    TwitchManager.Set<Vector3> set = delegate(Vector3 val, Object param) { squashScale = val; };
                    TwitchManager.CreateTwitch<Vector3>(squashScale, _squashScale, set, 0.4f, TwitchCurve.Shape.OvershootOut);
                }
            }
        }
        /// <summary>
        /// Scale value used when actor is squashed.
        /// Twitch target version.  Setting this also sets the current value.
        /// </summary>
        public Vector3 _SquashScale
        {
            get { return _squashScale; }
            set { _squashScale = value; squashScale = value; }
        }

        [XmlIgnore]
        public SensorTargetSet TouchedSet
        {
            get { return touchedSet; }
        }

        [XmlIgnore]
        public SensorTargetSet.Enumerator TouchedSetIter
        {
            get { return touchedSetIter; }
        }

        [XmlIgnore]
        public SensorTargetSet GivenSet
        {
            get { return givenSet; }
        }

        [XmlIgnore]
        public SensorTargetSet EatenSet
        {
            get { return eatenSet; }
        }

        [XmlIgnore]
        public SensorTargetSet MissileHitSet
        {
            get { return missileHitSet; }
        }

        [XmlIgnore]
        public SensorTargetSet ShooterHitSet
        {
            get { return shooterHitSet; }
        }

        // SGI_MOD beam related
        [XmlIgnore]
        public SensorTargetSet BeamedSet
        {
            get { return beamedSet; }
        }

        [XmlIgnore]
        public SensorTargetSet.Enumerator BeamedSetIter
        {
            get { return beamedSetIter; }
        }

        [XmlIgnore]
        public SensorTargetSet InspectedSet
        {
            get { return inspectedSet; }
        }

        [XmlIgnore]
        public SensorTargetSet.Enumerator InspectedSetIter
        {
            get { return inspectedSetIter; }
        }

        [XmlIgnore]
        public SensorTargetSet ScannedSet
        {
            get { return scannedSet; }
        }

        [XmlIgnore]
        public SensorTargetSet.Enumerator ScannedSetIter
        {
            get { return scannedSetIter; }
        }


        [XmlIgnore]
        public BleepManager.BleepData ActiveBleeps
        {
            get { return _bleeps ?? (_bleeps = new BleepManager.BleepData()); }
        }

        public void ClearBleeps()
        {
            if (_bleeps != null)
            {
                _bleeps.Clear();
            }
        }

        [XmlIgnore]
        public BeamManager.BeamData ActiveBeams
        {
            get { return _beams ?? (_beams = new BeamManager.BeamData()); }
        }

        public override bool Revealed
        {
            get { return _revealed; }
            set
            {
                //early out
                if (_revealed == value) return;

                //mark as revealed - this will have RenderObject and SRO returning the new model
                _revealed = value;

                // If we have an actor that doesn't have a "revealed" state, then we 
                // can just stop here.  Currently, this is only for "rock".
                if (StaticActor.XmlGameActorRevealed == null)
                {
                    return;
                }

                //indicate we need to update the render object we're using
				//this can only happen at certain points in the update loop, so flag it for now, handle it later
                _needsRevealUpdate = true;

                //reset the animation set
                animationSet = new AnimationSet(this, SRO);

				//make sure we get a refresh call so we can update the render object
                BokuGame.objectListDirty = true;
            }
        }

        public bool Scanned
        {
            get { return _scanned; }
            set { _scanned = value; }
        }

        public float ScanRevealDelay
        {
            get { return _scanRevealDelay; }
            set { _scanRevealDelay = value; }
        }

        public float ScanRevealTimer
        {
            get { return _scanRevealTimer; }
            set { _scanRevealTimer = value; }
        }

        public int ScanningRing
        {
            get { return _scanningRing; }
            set { _scanningRing = value; }
        }

        public bool Beamed
        {
            get { return _beamed; }
            set { _beamed = value;  }
        }

        public bool BeamedSoundPlayed
        {
            get { return _beamedSoundPlayed; }
            set { _beamedSoundPlayed = value; }
        }

        public Vector3 BeamTargetPosition
        {
            get { return _beamTargetPosition; }
            set { _beamTargetPosition= value; }
        }

        public Classification.Colors BeamColor
        {
            get { return _beamColor; }
            set { _beamColor = value; }
        }

        public bool Inspected
        {
            get { return _inspected; }
            set { _inspected = value; }
        }

        public bool ScienceActionPerformed
        {
            get { return scienceActionPerformed; }
            set 
            { 
                scienceActionPerformed = value;
                TweakImmobileNoRot = value;
            }
        }

        public ScienceAction CurrentScienceAction
        {
            get { return currentScienceAction; }
            set { currentScienceAction= value; }
        }

        public float ScienceActionDelay
        {
            get { return scienceActionDelay; }
            set { scienceActionDelay = value; }
        }

        public float ScienceActionTimer
        {
            get { return scienceActionTimer; }
            set { scienceActionTimer = value; }
        }

        public GameActor ScienceActorTarget
        {
            get { return scienceActorTarget; }
            set { scienceActorTarget = value; }
        }

        public bool ScienceActorRotating
        {
            get { return scienceActorRotating; }
            set { scienceActorRotating = value; }
        }

        public bool ScienceActorMuted
        {
            get { return scienceActorMuted; }
            set { scienceActorMuted = value; }
        }

        public Vector3 ScienceActorDirection
        {
            get { return scienceActorDirection; }
            set { scienceActorDirection = value; }
        }
        
        public override Classification.Colors ReachedEOP
        {
            get { return _reachedEOP; }
            set { _reachedEOP = value; }
        }

        public override bool ReadyToProcessEOP
        {
            get { return _readyToProcessEOP; }
            set { _readyToProcessEOP = value; }
        }

        public void ClearBeams()
        {
            if (_beams != null)
            {
                _beams.Clear();
            }
        }

        // We intentionally do not use a Dictionary<> here because they are
        // low performance, and devices are accessed every update.
        public List<VisualDevice> VisualDevices = new List<VisualDevice>();

        public enum CameraFollowModes
        {
            UserControlled,     // Camera follows the actor only when user controlled.  (default)
            Always,             // Camera always follows the actor.
            Never               // Camera ignores the actor.
        }

        /// <summary>
        /// These are grouped into a class to make them easier to save/restore/clone.
        /// </summary>
        public class TweakableParameters : ArbitraryComparable, ICloneable
        {
            public bool immobile;                   // Doesn't move in XYZ but can still rotate.
            public bool immobileNoRot;              // Doesn't move or rotate.
            public bool invulnerable;               // Like Superman.
            public bool showSensors;                // Render sensor cones/ranges.
            public bool showHitPoints;              // Render health bar.
            public bool creatable;                  // Include in 'create' menu.
            public bool mute;
            public int maxHitPoints;                // Max number of hit points on the actor.
            public int blipDamage;                  // Amount of damage a blip inflicts. Negative values heal targets. N/A for non-damage payloads (e.g., stun).
            public int missileDamage;               // Amount of damage a vaporize or kill missile inflicts. Negative values heal targets.

            // This group of values should default to 1.0 but
            // may be changed by the user via actor Settings.
            public float movementSpeedModifier;         // Simply multiplied by the current chassis limits.
            public float turningSpeedModifier;          // ibid but for turning instead of forward movement.
            public float linearAccelerationModifier;    // Simply multiplied by the current chassis limits.
            public float turningAccelerationModifier;   // ibid but for turning instead of forward movement.
            public float verticalSpeedModifier;         // Simply multiplied by the current chassis limits.
            public float verticalAccelerationModifier;  // Simply multiplied by the current chassis limits.

            public float blipReloadTime;         // Seconds.
            public int blipSpeed;                // m/s
            public float blipRange;              // meters
            public int blipsInAir;               // max # missiles at once

            // SGI_MOD - Beam related values
            public float beamReloadTime;         // Seconds.
            public int beamSpeed;
            public float beamDist;
            public int beamsInAir;

            //SGI_MOD - Scan variables
            public bool revealed;

            public float missileReloadTime;         // Seconds.
            public int missileSpeed;                // m/s
            public float missileRange;              // meters
            public int missilesInAir;               // max # missiles at once
            public bool missileTrails;              // Show contrails?

            public bool shieldEffects;              // Display shield animation on collision or damage?
            public bool invisible;                  // Don't draw me.
            public bool ignored;                    // Don't sense me

            //SGI_MOD - Camouflaged
            public bool camouflaged;

            //SGI_MOD - Push
            public float pushRange;
            public float pushStrength;

            public float kickStrength;              // Strength of kick.
            public float kickRate;                  // Kicks per second.
            public float hearing;                   // Hearing range. Normalized to 0..1 range.  No clue why.  In use will remap to 0..100 range.
                                                    // During run, 100 is treated like infinity.
            public float loudness;                  // Obsolete and unused.
            public float glowAmt;                   // The amount of glow, when glowing.
            public float glowLights;                // How strong the point light is when this character glows
            public float glowEmission;              // How much light the glow casts onto the bot.
            public bool displayLOS;                 // Draw lines showing what we are looking at.
            public bool displayLOP;                 // Draw lines showing what we see and hear.
            public bool displayCurrentPage;         // Display currently active programming page.
            public CameraFollowModes cameraMode;    // How the camera deals with this character.
            public float coefficientOfRestitution;  // Bounciness.
            public float friction;                  // Friction, 0=frictionless, 1=grabbing
            public float lightRange;                // Only used for lights, the range of the light.
            public bool stayAboveWater;             // Do we ride the water surface or follow the terrain down.
            public int maxCreated;                  // How many runtime copies of me can you make?
            public float rescale;                   // Scale multiplier for this bot's size.
            public float holdDistance;              // Multiplier for holding distance.
            public float _holdDistance;             // Twitch target for holdDistance.

            public float mass;                      // Kilograms.

            /// <summary>
            /// Invalid value to mark this heightOffset as unset.
            /// </summary>
            public const float kInvalidHeightOffset = -666;
            /// <summary>
            /// Obsolete value, don't use this, use GameThing.HeightOffset. This is strictly
            /// for backward compatibility.
            /// </summary>
            public float heightOffset = kInvalidHeightOffset;

            public float nearByDistance;            // Distance to be used in this bot's "near by" filter.
            public float farAwayDistance;           // Distance to be used in this bot's "far away" filter.

            public TweakableParameters()
            {
                InitDefaults();
            }

            public void InitDefaults()
            {
                immobile = false;
                immobileNoRot = false;
                invulnerable = false;
                creatable = false;
                mute = false;
                showSensors = false;
                showHitPoints = false;
                maxHitPoints = 50;
                blipDamage = 5;
                missileDamage = 50;
                movementSpeedModifier = 1.0f;
                turningSpeedModifier = 1.0f;
                linearAccelerationModifier = 1.0f;
                turningAccelerationModifier = 1.0f;
                verticalSpeedModifier = 1.0f;
                verticalAccelerationModifier = 1.0f;

                blipReloadTime = 0.05f;
                blipSpeed = 10;
                blipRange = 35.0f;
                blipsInAir = 40;

                beamReloadTime = 0.025f;
                beamSpeed = 10;
                beamDist = 35.0f;
                beamsInAir = 2000;

                revealed = false;

                missileReloadTime = 3.0f;
                missileSpeed = 6;
                missileRange = 50.0f;
                missilesInAir = 4;
                missileTrails = true;

                shieldEffects = true;
                invisible = false;
                ignored = false;

                camouflaged = false;

                pushRange = 15.0f;
                pushStrength = 50.0f;

                kickStrength = 10.0f;
                kickRate = 2.0f;
                hearing = 1.0f;
                loudness = 1.0f;
                glowAmt = 1f;
                glowLights = 0.0f;
                glowEmission = 0.0f;
                lightRange = Light.DefaultRange;
                displayLOS = false;
                displayLOP = false;
                cameraMode = CameraFollowModes.UserControlled;

                stayAboveWater = true;
                maxCreated = 1000;
                rescale = 1.0f;
                holdDistance = _holdDistance = 1.0f;
                

                mass = 1.0f;

                nearByDistance = 5;
                farAwayDistance = 15;

                coefficientOfRestitution = 0.8f;
                friction = 0.5f;
            }   // end of TweakableParameters InitDefaults()

            public void CopyTo(TweakableParameters dst)
            {
                dst.immobile = this.immobile;
                dst.immobileNoRot = this.immobileNoRot;
                dst.invulnerable = this.invulnerable;

                // Don't copy the creatable flag
                //dst.creatable = this.creatable;

                dst.mute = this.mute;
                dst.showSensors = this.showSensors;
                dst.showHitPoints = this.showHitPoints;
                dst.maxHitPoints = this.maxHitPoints;
                dst.blipDamage = this.blipDamage;
                dst.missileDamage = this.missileDamage;
                dst.movementSpeedModifier = this.movementSpeedModifier;
                dst.turningSpeedModifier = this.turningSpeedModifier;
                dst.linearAccelerationModifier = this.linearAccelerationModifier;
                dst.turningAccelerationModifier = this.turningAccelerationModifier;
                dst.verticalSpeedModifier = this.verticalSpeedModifier;
                dst.verticalAccelerationModifier = this.verticalAccelerationModifier;

                dst.blipReloadTime = this.blipReloadTime;
                dst.blipSpeed = this.blipSpeed;
                dst.blipRange = this.blipRange;
                dst.blipsInAir = this.blipsInAir;

                dst.beamReloadTime = this.beamReloadTime;
                dst.beamSpeed = this.beamSpeed;
                dst.beamDist = this.beamDist;
                dst.beamsInAir = this.beamsInAir;

                dst.revealed = this.revealed;

                dst.missileReloadTime = this.missileReloadTime;
                dst.missileSpeed = this.missileSpeed;
                dst.missileRange = this.missileRange;
                dst.missilesInAir = this.missilesInAir;
                dst.missileTrails = this.missileTrails;

                dst.shieldEffects = this.shieldEffects;
                dst.invisible = this.invisible;
                dst.ignored = this.ignored;

                dst.camouflaged = this.camouflaged;

                dst.pushRange = this.pushRange;
                dst.pushStrength = this.pushStrength;

                dst.kickStrength = this.kickStrength;
                dst.kickRate = this.kickRate;
                dst.hearing = this.hearing;
                dst.loudness = this.loudness;
                dst.glowAmt = this.glowAmt;
                dst.glowLights = this.glowLights;
                dst.glowEmission = this.glowEmission;
                dst.lightRange = this.lightRange;
                dst.displayLOS = this.displayLOS;
                dst.displayLOP = this.displayLOP;
                dst.displayCurrentPage = this.displayCurrentPage;
                dst.cameraMode = this.cameraMode;

                dst.stayAboveWater = this.stayAboveWater;
                dst.maxCreated = this.maxCreated;
                dst.rescale = this.rescale;
                dst.holdDistance = this.holdDistance;

                dst.mass = this.mass;

                dst.nearByDistance = this.nearByDistance;
                dst.farAwayDistance = this.farAwayDistance;

                dst.coefficientOfRestitution = this.coefficientOfRestitution;
                dst.friction = this.friction;
            }

            public Object Clone()
            {
                return MemberwiseClone();
            }


        }   // end of struct TweakableParameters

        /// <summary>
        /// Gets or sets the actor's current hit points.  If actor is
        /// invulnerable, attempts to set hit points are ignored.
        /// Value is clamped to [0, MaxHitPoints].
        /// </summary>
        public int HitPoints
        {
            get { return currHitPoints; }
            set
            {
                prevHitPoints = currHitPoints;
                currHitPoints = MyMath.Clamp(value, 0, MaxHitPoints);
            }
        }

        /// <summary>
        /// The HealthSensor compares current and previous health values.
        /// </summary>
        public int PrevHitPoints
        {
            get { return prevHitPoints; }
        }

        /// <summary>
        /// Gets or sets the actor's health bar handle.  Intended for
        /// use only by the HealthBarManager.
        /// </summary>
        public int HealthBarHandle
        {
            get { return healthbar; }
            set { healthbar = value; }
        }

        /// <summary>
        /// This is the minimum height of the object.  An object at 
        /// this height should appear to be sitting on the ground.
        /// </summary>
        public override float MinHeight
        {
            get { return XmlActorParams.MinHeight * ReScale; }
        }

        /// <summary>
        /// This is the default height of the object in the editor.
        /// </summary>
        public override float DefaultEditHeight
        {
            get { return XmlActorParams.DefaultEditHeight * ReScale; }
            set { XmlActorParams.DefaultEditHeight = value; }
        }

        /// <summary>
        /// This is the offset from the bot's height used for terrain collision testing.
        /// Defaults to 0.
        /// </summary>
        public override float WaistOffset
        {
            get { return XmlActorParams.WaistOffset; }
        }

        /// <summary>
        /// This is the offset in Z from the bot's origin used to play the eye when in first person mode.
        /// Defaults to 0.1.
        /// </summary>
        public override float EyeOffset
        {
            get { return XmlActorParams.EyeOffset; }
        }

        /// <summary>
        /// The local space height of the top of this bot.
        /// </summary>
        public override float TopOffset
        {
            get { return CollisionRadius + CollisionCenter.Z; }
        }


        /// <summary>
        /// Gets the offset in model space where the healthbar should render.
        /// This is a reasonable default for most models, but should be overridden
        /// for models where it looks wrong. Intended for consumption only by the
        /// HealthBarManager.
        /// </summary>
        public override Vector3 HealthBarOffset
        {
            get { return GlowPosition + XmlActorParams.HealthBarOffset * ReScale; }
        }

        public override Vector3 WorldHealthBarOffset
        {
            get { return Vector3.Transform(HealthBarOffset, Movement.LocalMatrix); }
        }

        public override Vector3 ThoughtBalloonOffset
        {
            get { return HealthBarOffset; }
        }

        /// <summary>
        /// Offset position for thought balloon.  Note that for 
        /// tumbling objects we want to ignore the tumble otherwise
        /// the balloon gets spun into the ground.
        /// </summary>
        public override Vector3 WorldThoughtBalloonOffset
        {
            get 
            {
                Vector3 result = Vector3.Zero;
                DynamicPropChassis chassis = Chassis as DynamicPropChassis;
                if(chassis != null && chassis.Tumbles)
                {
                    result = HealthBarOffset + Movement.Position - Vector3.UnitZ;
                }
                else
                {
                    result = WorldHealthBarOffset - Vector3.UnitZ;
                }

                return result; 
            }
        }

        // Tweak screen settings
        private TweakableParameters localParameters = null;

        // Runtime copy of local parameters (for clones)
        private TweakableParameters runtimeLocalParameters = null;


        /// <summary>
        /// If this bot is an individual or a creatable master, return its local parameters.
        /// Otherwise it is a clone, so return parameters of the creatable master.
        /// </summary>
        public TweakableParameters SharedParameters
        {
            get
            {
                GameActor source = null;

                if (IsClone)
                {
                    // Find our associated creatable master and return its local
                    // set of parameters.
                    source = InGame.inGame.GetCreatable(CreatableId);

                    // It is possible for a creatable master to be missing if
                    // a world is being loaded and the creatables have not yet
                    // been registered. This is dangerous, if the value is read
                    // once, but okay if the incorrect value is re-read next frame
                    // with the correct value. Will probably take some extreme
                    // ajustment to get them correct.
                    if (source == null)
                    {
                        source = this;
                        runtimeLocalParameters = null;
                    }
                    else
                    {
                        //first time in run mode each time, grab a copy of the source creatable's settings
                        //this allows clones to have their own values that can change at run time
                        if (InGame.inGame.CurrentUpdateMode == InGame.UpdateMode.RunSim)
                        {
                            if (runtimeLocalParameters == null)
                            {
                                runtimeLocalParameters = new TweakableParameters();
                                source.LocalParameters.CopyTo(runtimeLocalParameters);
                            }

                            return runtimeLocalParameters;
                        }
                        else
                        {
                            //reset whenever not in sim mode
                            runtimeLocalParameters = null;
                        }
                    }
                }
                else
                {
                    //non-clones don't need special copies
                    source = this;
                    runtimeLocalParameters = null;
                }

                return source.LocalParameters;
            }
        }

        /// <summary>
        // Used when loading a actor from file.
        /// </summary>
        internal TweakableParameters LocalParameters
        {
            get { return localParameters; }
            set { localParameters = value; }
        }

        // Missile
        protected double nextMissileFire = 0;
        protected double nextBleepFire = 0;
        protected int numMissilesInAir = 0;
        protected double nextBeamFire = 0;

        /// <summary>
        /// How many missiles launched from this actor are currently active.
        /// </summary>
        [XmlIgnore]
        public int NumMissilesInAir
        {
            get { return numMissilesInAir; }
            set { numMissilesInAir = value; }
        }

        // Kick
        protected double lastKickedAt;
        protected GameThing lastKickedThing;

        public enum BalloonExpressions
        {
            TastyEats,
            SourEats,
            Why,
            SeeRedPlumpkin,
            RememberedRedPlumpkin,
            Searching,
        }

        /// <summary>
        /// Where this actor is allowed to move.
        /// </summary>
        public enum MovementDomain
        {
            Air,        // Actor can fly over land, water or holes in terrain.
            Land,       // Actor is contrained to land.
            Water,      // Actor is constrained to water.
        };
        public MovementDomain Domain
        {
            get { return XmlActorParams.Domain; }
        }

        public enum AltitudeOptions
        {
            None = 0,
            IgnoreTerrainFeatures = 1,
            IgnoreWaves = 2
        };

        protected AltitudeOptions altitudeOptions;

        public override float GetPreferredAltitude()
        {
            float worldHeight = 0.0f;

            // Get terrain height
            float terrainHeight;
            if ((altitudeOptions & AltitudeOptions.IgnoreTerrainFeatures) != 0)
                terrainHeight = Terrain.GetHeight(Movement.Position);
            else
                terrainHeight = Terrain.GetTerrainAndPathHeight(Movement.Position);
            worldHeight = terrainHeight; // Math.Max(worldHeight, terrainHeight);

            // Get water height (if applicable)
            if (StayAboveWater)
            {
                float waterHeight;
                if ((altitudeOptions & AltitudeOptions.IgnoreWaves) != 0)
                    waterHeight = Terrain.GetWaterBase(Movement.Position);
                else
                    waterHeight = Terrain.GetWaterHeight(Movement.Position);

                worldHeight = Math.Max(worldHeight, waterHeight);
            }

            return worldHeight + Chassis.EditHeight;
        }

        protected bool brainRegistered = false;

        public override void Activate()
        {
            Debug.Assert(FactoryCreated);
            Debug.Assert(!InRecycleBin);

            if (CurrentState != State.Active)
            {
                BokuGame.objectListDirty = true;
            }

            PendingState = State.Active;
        }

        public override void Deactivate()
        {
            Debug.Assert(FactoryCreated);
            Debug.Assert(!InRecycleBin);

            if (CurrentState != State.Inactive && PendingState != State.Inactive)
            {
                DoLastBrainPass(State.Inactive);
            }
        }

        /// <summary>
        /// Allow the brain to do one final cycle.  This is used when
        /// actors are killed since they can detect health==0.  This
        /// is also used for knocked out and squashed actors.  In either
        /// case they need to look at themselves eg WHEN See Me Squashed
        /// or WHEN See Me KnockedOut
        /// <param name="pendingState">The brain state we're moving into.  Inactive for blown up, Dead for knockedOut, Squashed for squashed.</param>
        /// </summary>
        public void DoLastBrainPass(State pendingState)
        {
            // Clear queued sensor inputs
            touchedSet.Clear();
            touchSpoofs.Clear();
            givenSet.Clear();
            eatenSet.Clear();
            missileHitSet.Clear();
            shooterHitSet.Clear();
            beamedSet.Clear();
            inspectedSet.Clear();
            scannedSet.Clear();

            // This may not be the right place to do this, but it worked.
            if (ThingBeingHeldByThisActor != null)
            {
                ThingBeingHeldByThisActor.ActorHoldingThis = null;
                ThingBeingHeldByThisActor = null;
            }

            if (ActorHoldingThis != null)
            {
                ActorHoldingThis.ThingBeingHeldByThisActor = null;
                ActorHoldingThis = null;
            }

            // Set pending state before the brain update to avoid recursion into this function.
            PendingState = pendingState;
            BokuGame.objectListDirty = true;

            // If we're running the sim, run a final brain update to detect death.
            if (!Creatable && BrainRegistered && CurrentState == State.Active)
            {
                // Make it look like the actor just lost its last hit point and then pump the health sensors.
                prevHitPoints = 1;
                currHitPoints = 0;

                // The overall goal of this code is to allow 1 final pass through the kode when a bot dies.  
                // This allows things like WHEN Health 0 DO Score Points.  (score when I die)
                // The filter is applied to the root level reflexes (indent==0) and so only allowed things like 
                // the above example to work when at indent==0.  If nested, even if the parent executes, the filter 
                // prevents this running.
                // Removing the filter altogether causes bugs.  For instance:
                //          WHEN OnLand Type DO +Score Green 1Point
                //              WHEN DO Vanish Me
                // This will end up scoring twice.  Once when executed and then again in DoLastBrainPass().
                // So, go back to filtering on the health sensor and just dealing with the fact that it is required to be indent == 0.
                UpdateSensors(BrainCategories.WhenHealth);
                UpdateActuators();
            }
        }   // end of DoLastBrainPass()

        public override void Pause()
        {
            if (CurrentState != State.Paused)
            {
                BokuGame.objectListDirty = true;
            }
            PendingState = State.Paused;
        }

        /// <summary>
        /// Keep track of how many of us there are, start failing when too many.
        /// </summary>
        /// <returns></returns>
        public override bool EnterScene()
        {
            if (!InGame.InReset && IsClone && (InGame.inGame.CurrentUpdateMode == InGame.UpdateMode.RunSim))
            {
                Debug.Assert(numCopiesOfMe == 0);

                GameActor master = InGame.inGame.GetCreatable(creatableId);
                if (master == this)
                {
                    /// If this is really a master, it's not actually in the scene in the
                    /// normal sense, it's only there for show during edit and reset. Always
                    /// allow, and don't affect the counts.
                    return true;
                }
                int maxCount = master.MaxCreated;
                int count = master.numCopiesOfMe;
                if ((maxCount >= 0) && (count >= maxCount))
                {
                    return false;
                }

                ++(master.numCopiesOfMe);
                ++numCopiesOfMe;
            }

            return true;
        }

        /// <summary>
        /// Keep track of how many of us are left.
        /// </summary>
        public override void ExitScene()
        {
            if (numCopiesOfMe > 0)
            {
                /// Masters don't affect the counts.
                GameActor master = InGame.inGame.GetCreatable(creatableId);
                
                // This only occurs in levels with PreGrame and creatables. GamePad must be used.
                // To get the error to happen need to run the game, during pregame hit Start to 
                // go to HomeMenu, Save game, it should return to run mode, now hit Back to go
                // into Edit mode.  During the unload we end up here because the creatableId is Guid.Empty.
                // Duel temp fixes: check for null here and disable Start button during RunSim.
                Debug.Assert(master != null, "Why");

                if (master != null && master != this)
                {
                    --numCopiesOfMe;
                    Debug.Assert(master.numCopiesOfMe > 0);
                    --(master.numCopiesOfMe);
                }
            }
        }

        /// <summary>
        /// Explicitly clear the count.
        /// </summary>
        public void ClearCount()
        {
            numCopiesOfMe = 0;
        }

        private int numCopiesOfMe = 0;

        public override bool Refresh(List<UpdateObject> updateList, List<RenderObject> renderList)
        {
            Debug.Assert(FactoryCreated);
            Debug.Assert(!InRecycleBin);

            bool result = false;

            //check if we need to update which render object is used
            if (_needsRevealUpdate)
            {
                _needsRevealUpdate = false;
                if (_revealed)
                {
                    Classification.Colors color = renderObjRevealed.Parent.ClassColor;

					//were we just revealed?  deactivate normal model, activate reveal model
                    renderList.Remove(renderObj);
                    renderObj.Deactivate();
                    UnRegister();

                    //reapply params from new xml

                    // BUG Revealing a rock loses its color.

                    //TODO: glow emitter + face?
                    SetupBaseXmlParams();
                    ResetXmlParams(revivingDeadActor: false);

                    renderList.Add(renderObjRevealed);
                    renderObjRevealed.Activate();
                    Register();

                    // Note that we set ClassColor here because it avoids the twitch
                    // when using GameActor.SetColor().
                    ClassColor = color;
                }
                else
                {
					//were we just reverted?  deactivate reveal model, activate normal model
                    renderList.Add(renderObj);
                    renderObj.Activate();
                    UnRegister();

                    renderList.Remove(renderObjRevealed);
                    renderObjRevealed.Deactivate();
                    Register();
                }
            }

            if (CurrentState != PendingState)
            {
                //Debug.Print("change state " + this.uniqueNum.ToString() + " from " + CurrentState.ToString() + " to " + PendingState.ToString());

                switch (CurrentState)
                {
                    case State.Dead:
                    case State.Active:
                    case State.Squashed:

                        // If we're coming from a Dead state we need to reset the
                        // LocalMatrix to get rid of any tilt added to the corpse.
                        if (CurrentState == State.Dead)
                        {
                            Matrix mat = Matrix.CreateRotationZ(Movement.RotationZ);
                            mat.Translation = Movement.Position;
                            Movement.SetLocalMatrixAndRotation(mat, Movement.RotationZ);
                        }

                        switch (PendingState)
                        {
                            case State.Inactive:
                                renderList.Remove(RenderObject);
                                RenderObject.Deactivate();
                                ShutdownSubsystems(revivingTheDead: false);
                                UnRegister();
                                result = true;
                                break;

                            case State.Paused:
                                ThoughtBalloonManager.RemoveThoughts(this);
                                StopSound();
                                break;

                            case State.Active:
                                State cacheCurrentState = CurrentState;
                                State cachePendingState = PendingState;

                                // Reset chassis, etc.
                                InitDefaults(true);

                                // ...except don't reset state vars
                                CurrentState = cacheCurrentState;
                                PendingState = cachePendingState;

                                // Move to inactive state.
                                renderList.Remove(RenderObject);
                                RenderObject.Deactivate();

                                // ShutdownSubsystems() also reset the number of hitpoints.  So copy
                                // the value and restore it on the other side.  This is used when
                                // reviving a dead character via healing.  Otherwise the character
                                // would come back fully healed.
                                int hitPoints = HitPoints;
                                ShutdownSubsystems(revivingTheDead: true);
                                HitPoints = hitPoints;
                                
                                UnRegister();

                                // Move to active state.
                                renderList.Add(RenderObject);
                                RenderObject.Activate();
                                StartupSubsystems();
                                Register();
                                // Set Moving to true for DynamicPropChassis.  This ensure that things start rolling downhill.
                                // BUT set to fasle for others.  This stops Kodu, etc from sliding downhill without moving first.
                                Chassis.Moving = Chassis is DynamicPropChassis;
                                // Reset squashScale if it was squashed.
                                SquashScale = Vector3.One;

                                break;
                        }
                        break;

                    case State.Inactive:
                        switch (PendingState)
                        {
                            case State.Active:
                                renderList.Remove(RenderObject);
                                if (visible)
                                    renderList.Add(RenderObject);
                                RenderObject.Activate();
                                StartupSubsystems();
                                Register();
                                // Set Moving to true for DynamicPropChassis.  This ensure that things start rolling downhill.
                                // BUT set to fasle for others.  This stops Kodu, etc from sliding downhill without moving first.
                                Chassis.Moving = Chassis is DynamicPropChassis;
                                break;

                            case State.Paused:
                                renderList.Remove(RenderObject);
                                if (visible)
                                    renderList.Add(RenderObject);
                                RenderObject.Activate();
                                EnableAttachments(false);
                                Register();
                                break;
                        }
                        break;

                    case State.Paused:
                        switch (PendingState)
                        {
                            case State.Active:
                                EnableAttachments(true);
                                StartSound();
                                // Set Moving to true for DynamicPropChassis.  This ensure that things start rolling downhill.
                                // BUT set to false for others.  This stops Kodu, etc from sliding downhill without moving first.
                                Chassis.Moving = Chassis is DynamicPropChassis;
                                if (ActorHoldingThis != null)
                                {
                                    Debug.Assert(ActorHoldingThis.ThingBeingHeldByThisActor == this);
                                }
                                break;

                            case State.Inactive:
                                renderList.Remove(RenderObject);
                                RenderObject.Deactivate();
                                ThoughtBalloonManager.RemoveThoughts(this);
                                DisableAttachments(false);
                                StopSound();
                                UnRegister();
                                result = true;
                                break;
                        }
                        break;
                }
                Movement.SetPreviousPositionVelocity();

                CurrentState = PendingState;

                // If this actor has been removed from the scene, put it in the recycle bin.
                if (CurrentState == State.Inactive)
                {
                    ActorFactory.Recycle(this);
                }
            }

            return result;
        }   // end of GameActor Refresh()

        protected override void Register()
        {
            /// CollideUpdates get registered for every frame that we hit something.
            InGame.inGame.RegisterCollide(this);

            InGame.inGame.RegisterBrain(this);

            // No need to register creatables with some subsystems.
            if (!Creatable)
            {
                HealthBarManager.RegisterActor(this);
            }

            base.Register();
        }

        protected override void UnRegister()
        {
            HealthBarManager.UnregisterActor(this);

            InGame.inGame.UnRegisterCollide(this);
            InGame.inGame.UnRegisterBrain(this);

            /// Let the waypoint system know it can forget about caching this guy's whereabouts
            WayPoint.Node.PrevNode.ReleasePrevNode(this);

            base.UnRegister();
        }

        public bool IsTree
        {
            get { return Classification.name == "tree"; }
        }

        /// <summary>
        /// Is this actor's brain registered for updates?
        /// 
        /// TODO (scoy) remove this.  Do we really need this or
        /// is it just redundant information that could get out
        /// of sync?
        /// </summary>
        public bool BrainRegistered
        {
            get { return brainRegistered; }
            set { brainRegistered = value; }
        }

        /// <summary>
        /// Gets or sets the ID of this actor's creatable, if it has (or is) one.
        /// </summary>
        public Guid CreatableId
        {
            get { return creatableId; }
            set
            {
                if (creatableId != value)
                {
                    // If we are becoming an individual from being a clone, copy our creatable master's
                    // tweak parameters to our own, since we're disconnecting.
                    if (creatableId != Guid.Empty && value == Guid.Empty)
                    {
                        GameActor master = InGame.inGame.GetCreatable(CreatableId);
                        if (master != null)
                            master.localParameters.CopyTo(this.localParameters);
                    }

                    creatableId = value;
                }
            }
        }

        [XmlIgnore]
        public Distortion CreatableAura
        {
            get { return creatableAura; }
            set
            {
                if (creatableAura != null)
                    creatableAura.Die();
                if (value != null)
                    value.TintAura(0.0f, 0.4f, 0.0f);
                creatableAura = value;
            }
        }

        /// <summary>
        /// The DisplayNameNumber is a combination of a string name and number.  The 
        /// number cannot be set by the user and is used to ensure uniqueness even 
        /// if the same string name is used.
        /// 
        /// The only time this should ever be set is when reading in from a level.
        /// </summary>
        public override string DisplayNameNumber
        {
            get
            {
                Debug.Assert(!String.IsNullOrWhiteSpace(displayName), "We should alwasy have a valid name.");
                Debug.Assert(displayNumber > 0, "Number should also be at least 1");
                
                return displayName + " " + displayNumber.ToString();
            }
            set
            {
                // The only place that should be setting this value is when we are reading in a level.
                // If user is changing the name, we need to just set the name, not the number.  The
                // number will be calculated automatically.
                if (!String.IsNullOrEmpty(value))
                {
                    displayNumber = 0;
                    displayName = value;

                    // Attempt to parse the number from the new name.
                    int index = value.LastIndexOf(' ');

                    if (index == -1)
                    {
                        // No space found so assume we just have a name.
                        displayNumber = FindNextDisplayNumber(displayName);
                    }
                    else
                    {
                        try
                        {
                            displayName = value.Substring(0, index);
                            displayNumber = Int32.Parse(value.Substring(index + 1));
                        }
                        catch
                        {
                            // If Parse throws, just get the next available number.
                            displayNumber = FindNextDisplayNumber(displayName); ;
                        }
                    }

                    // If this actor is a creatable, we need to re-register it with
                    // CardSpace in order to get the name right on modifier tiles.
                    // Note, Creatable may be false while CreatableId is valid.  This
                    // happens when a clone of a creatable is made.  Yes, it's not
                    // very well defined but by checking both we prevent the creatable
                    // glow from appearing on the clones.
                    if (Creatable && CreatableId != Guid.Empty)
                    {
                        InGame.inGame.RegisterCreatable(this);
                    }
                }
                else
                {
                    Debug.Assert(false, "Why don't we have a name?");
                }
            }
        }

        /// <summary>
        /// Display name for the bot.  Will be combined with a number to
        /// ensure uniqueness.
        /// </summary>
        [XmlIgnore]
        public string DisplayName
        {
            get { return displayName; }
            set
            {
                if (!string.IsNullOrWhiteSpace(value))
                {
                    displayName = value;
                    displayNumber = FindNextDisplayNumber(displayName);

                    // If this actor is a creatable, we need to re-register it with
                    // CardSpace in order to get the name right on modifier tiles.
                    if (CreatableId != Guid.Empty)
                    {
                        InGame.inGame.RegisterCreatable(this);
                    }
                }
                else
                {
                    Debug.Assert(false, "Invalid name!");
                }
            }
        }

        /// <summary>
        /// This field is used internally when auto-generating unique actor names.
        /// Do not use it for anything. It is not guaranteed to remain valid or unchanged.
        /// </summary>
        [XmlIgnore]
        public int DisplayNumber
        {
            get { return displayNumber; }
        }

        /// <summary>
        /// Has the user renamed this character?
        /// Note that this will not work correctly on levels that were created
        /// using one language and then edited in another.  All names will 
        /// appear to be user defined.
        /// </summary>
        [XmlIgnore]
        public bool IsUserNamed
        {
            get { return DisplayName != StaticActor.LocalizedName; }
        }

        /// <summary>
        /// called to check if the object is NOT deactivated and NOT about to be removed
        /// </summary>
        /// <returns></returns>
        public override bool IsAlive()
        {
            // with this you can still see dead actors
            return (PendingState != State.Inactive);
        }

        /// <summary>
        /// Doesn't do anything except act as a placholder for 
        /// bot specific functions that need to happen before
        /// the brain is updated.
        /// </summary>
        public virtual void PreBrainUpdate()
        {
        }   // end of PreBrainUpdate()

        /// <summary>
        /// Doesn't do anything except act as a placholder for 
        /// bot specific functions that need to happen after
        /// the brain is updated.
        /// </summary>
        public virtual void PostBrainUpdate()
        {
        }   // end of PostBrainUpdate()

        /// <summary>
        /// Update the brain (AI)
        /// </summary>
        /// 
        public void UpdateSensors(BrainCategories category)
        {
            // If we're entering edit mode, don't execute.
            if (PendingState == State.Paused)
                return;

            if (CurrentState == State.Active)
            {
                ProcessTouchSpoofs();

                Brain.UpdateSensors(category);

                ClearEatenSet();
                ClearShooterSet();
                ClearBeamedSet();
                ClearInspectedSet();
                ClearScannedSet();
                
                prevHitPoints = currHitPoints;
            }
        }

        public void UpdateActuators()
        {
            // If we're entering edit mode, don't execute.
            if (PendingState == State.Paused && CurrentState != State.Paused)
                return;

            // Reset per-frame settings. The brain actuators will update these as necessary.
            Movement.UserControlled = false;

            if (CurrentState == State.Active)
            {
                Brain.UpdateActuators();

                // Updating actuators may have deactivated this actor.
            }

            if (!(PendingState == State.Inactive || PendingState == State.Dead || PendingState == State.Squashed))
            {
                // Process the list of things being given to us.  This list will
                // have been filtered by any applicable reflexes during brain update.
                ProcessGivenSet();

                ClearTouchedSet();

                ProcessMissileHitSet();

                // Process queued movements (applies rotations and directions to chassis).
                ProcessMovementSets();
            }


            bool quiet = Mute;


            // GameActor doesn't have a general update call, so tasks we'd normally perform
            // in a general update have started accumulating in this function. The code
            // block below is a good place to consolidate them so that if we do implement
            // a general update call, we know what needs to be moved over to it.

            {
                if (CurrentState != State.Dead && CurrentState != State.Squashed)
                {
                    UpdateAnimations();
                }

                // If I'm carrying something, force its orientation to match mine.
                if (ThingBeingHeldByThisActor != null)
                {
                    ThingBeingHeldByThisActor.Movement.RotationZ = this.Movement.RotationZ;
                }

                float deltaTime = Time.GameTimeFrameSeconds;
                if (ScienceActionPerformed && !ScienceActorRotating)
                {
                    ScienceActionTimer += deltaTime;
                    if (ScienceActionTimer > ScienceActionDelay)
                    {
                        // free the over
                        Chassis.MaxRotationalAcceleration = PrevMaxRotationAcceleration;
                        Chassis.MaxRotationRate = PrevMaxRotationRate;
                        ScienceActionPerformed = false;
                        if (ScienceActorTarget != null)
                        {
                            if (CurrentScienceAction == ScienceAction.Beam)
                            {
                                Foley.PlayRockSizzle(ScienceActorTarget);
                            }
                            else
                            {
                                Foley.PlayRockBreak(ScienceActorTarget);
                            }
                            ScienceActorTarget = null;
                        }
                        CurrentScienceAction = ScienceAction.None;
                    }
                    else
                    {
                        if (CurrentScienceAction == ScienceAction.Beam)
                        {
                            FireBeamInternal(ScienceActorMuted);
                        }
                        else if (CurrentScienceAction == ScienceAction.Inspect && !ScienceActorTarget.Inspected)
                        {
                            ScienceActorTarget.Revealed = true;
                            ExplosionManager.CreateInspectEffect(ScienceActorTarget.WorldCollisionCenter, 1.2f);

                            Vector3 actorCenter = Vector3.Transform(BoundingSphere.Center, Movement.LocalMatrix);
                            Vector3 thingCenter = Vector3.Transform(ScienceActorTarget.BoundingSphere.Center, ScienceActorTarget.Movement.LocalMatrix);
                            Vector3 direction = thingCenter - actorCenter;
                            float range = direction.Length();

                            SensorTarget target = SensorTargetSpares.Alloc();
                            target.Init(ScienceActorTarget, direction, range);

                            inspectedSet.AddOrFree(target);

                            target = SensorTargetSpares.Alloc();
                            target.Init(this, -direction, range);
                            ScienceActorTarget.inspectedSet.AddOrFree(target);
                            ScienceActorTarget.Inspected = true;

                            if (!ScienceActorMuted)
                            {
                                Foley.PlayInspect(this);
                            }
                        }
                    }

                }

                // When a Scan or Beam action is taken, the Rover first turns
                // to face the object.  Only after the rotation is done does
                // the actual action start which is triggered by setting
                // RoverActorRotating to false.
                if (ScienceActorRotating)
                {
                    Chassis.MaxRotationalAcceleration = roverRotationAcc;
                    Chassis.MaxRotationRate = roverRotationRate;
                    float targetRot = MyMath.ZRotationFromDirection(ScienceActorDirection);
                    // Calc short way around to turn.
                    float delta = targetRot - Movement.RotationZ;
                    if (delta > MathHelper.Pi)
                    {
                        delta = delta - MathHelper.TwoPi;
                    }
                    else if (delta < -MathHelper.Pi)
                    {
                        delta = delta + MathHelper.TwoPi;
                    }
                    targetRot = Movement.RotationZ + delta;

                    // Lerp rotation toward rock.
                    Movement.RotationZ = MyMath.Lerp(Movement.RotationZ, targetRot, 5.0f * deltaTime);
                    // Are we close enough to facing the correct direction?
                    if (Math.Abs(targetRot - Movement.RotationZ) < 0.1f)
                    {
                        ScienceActorRotating = false;
                    }
                }

                if (Scanned)
                {
                    // Reveal the rocks 1 ring at a time.
                    ScanRevealTimer += deltaTime;
                    if (ScanRevealTimer > ScanRevealDelay * ScanningRing)
                    {
                        scanRings = scanRings <= 0 ? 1 : scanRings;
                        ++ScanningRing;
                        float range = scanRange / scanRings * ScanningRing;

                        List<GameActor> results = RevealableActors(range);

                        foreach (GameActor result in results)
                        {
                            result.Revealed = true;
                            ExplosionManager.CreateScanEffect(result.WorldCollisionCenter, 1.2f);
                            if (!result.Mute)
                            {
                                Foley.PlayRockScanned(result);
                            }
                            Vector3 actorCenter = Vector3.Transform(BoundingSphere.Center, Movement.LocalMatrix);
                            Vector3 thingCenter = Vector3.Transform(result.BoundingSphere.Center, result.Movement.LocalMatrix);
                            Vector3 direction = thingCenter - actorCenter;
                            range = direction.Length();

                            SensorTarget target = SensorTargetSpares.Alloc();
                            target.Init(result, direction, range);
                            scannedSet.AddOrFree(target);

                            target = SensorTargetSpares.Alloc();
                            target.Init(this, -direction, range);
                            result.scannedSet.AddOrFree(target);
                        }
                    }
                    if (ScanningRing >= scanRings)
                    {
                        Scanned = false;
                    }
                }
            }
        }   // end of UpdateActuators()

        /// <summary>
        /// Updates the first person state of the current bot.  Note, that in order for this 
        /// to work the CameraInfo state must be valid which means that ALL the bots need 
        /// to have had UpdateActuators() already called on them.
        /// </summary>
        public void UpdateFirstPersonState()
        {
            // If this actor's first person status has changed, update the bot's state.
            // By triggering this off of CameraInfo we ensure that only one bot ever
            // thinks that it is in first person mode.
            if (this == CameraInfo.FirstPersonActor && !firstPersonLastFrame)
            {
                SetFirstPerson(true);
                firstPersonLastFrame = true;
            }
            else if (this != CameraInfo.FirstPersonActor && firstPersonLastFrame)
            {
                SetFirstPerson(false);
                firstPersonLastFrame = false;
            }

        }   // end of UpdateFirstPersonState()

        /// <summary>
        /// After finally settling on a position, notify anyone that needs notifying
        /// </summary>
        public virtual void PostCollide()
        {
            UpdateAttachments();
            UpdateFace();
        }

        public class RenderObj : FBXRenderObj
        {
            public RenderObj(GameActor parent, GetModelInstance getModelInstance)
                : base(parent, getModelInstance)
            {
            }
        }

        protected BaseController _currentAnim = null;
        protected AnimatorList _animators = AnimatorList.EmptyList;

        protected int glowBoneIndex = -1;
        protected Vector3 holdingPosition;
        protected float holdDistance = 1.0f;    // Multiplier used to increase the distance of the held object.
        /// <summary>
        /// The UNSCALED grab range. Should be combined with the grab range of
        /// the other bot involved.
        /// </summary>
        protected float grabRange;
        /// <summary>
        /// The UNSCALED kick range. Should be combined with kickrange of other bot.
        /// </summary>
        protected float kickRange;
        /// <summary>
        /// The UNSCALED scan range.
        /// </summary>
         

        /// TODO (scoy) From here down this looks like it's all Rover
        /// specific.  If so, why is it here?  Would it be better in

        protected float scanRange;
        /// <summary>
        /// The UNSCALED beam range. Should be combined with beamRange of other bot.
        /// </summary>
        protected float beamRange;
        /// <summary>
        /// The UNSCALED inspect range. 
        /// </summary>
        protected float inspectRange;
        /// <summary>
        /// The UNSCALED cluster range. 
        /// </summary>
        protected float clusterRange;
        /// <summary>
        /// The UNSCALED beam delay
        /// </summary>
        protected float beamDelay;
        /// <summary>
        /// The UNSCALED inspect delay. 
        /// </summary>
        protected float inspectDelay;
        /// <summary>
        /// The UNSCALED scan delay. 
        /// </summary>
        protected float scanDelay;
        /// <summary>
        /// The UNSCALED scan rings. 
        /// </summary>
        protected int scanRings;
        /// <summary>
        /// The UNSCALED rover rotation acc when beaming/inspecting 
        /// </summary>
        protected float roverRotationAcc;
        /// <summary>
        /// The UNSCALED rover rotation rate when beaming/inspecting 
        /// </summary>
        protected float roverRotationRate;
        /// <summary>
        /// The UNSCALED beam range. the FOV of the rover [-1.0, 1.0]
        /// </summary>
        protected float roverFOV;
        /// <summary>
        /// The UNSCALED min pitch of the rover [-PI, maxPitch]
        /// </summary>
        protected double minPitch;
        /// <summary>
        /// The UNSCALED max pitch of the rover [minPitch, PI]
        /// </summary>
        protected double maxPitch;
        /// <summary>
        /// The UNSCALED min roll of the rover [-PI, maxRoll]
        /// </summary>
        protected double minRoll;
        /// <summary>
        /// The UNSCALED max roll of the rover [minRoll, PI]
        /// </summary>
        protected double maxRoll;

        public double LastImpact
        {
            get;
            set;
        }

        /// <summary>
        /// Sounds associated with actions and/or animations.
        /// </summary>
        public string IdleSoundName
        {
            get { return XmlActorParams.IdleSoundName; }
        }
        public string MoveSoundName
        {
            get { return XmlActorParams.MoveSoundName; }
        }
        public string CursorSoundName
        {
            get { return XmlActorParams.CursorSoundName; }
        }
        public string JumpSoundName
        {
            get { return XmlActorParams.JumpSoundName; }
        }
        public string ShootSoundName
        {
            get { return XmlActorParams.ShootSoundName; }
        }
        public string RapidFireSoundName
        {
            get { return XmlActorParams.RapidFireSoundName; }
        }
        public string TwirlSoundName
        {
            get { return XmlActorParams.TwirlSoundName; }
        }
        public string AnimateSoundName
        {
            get { return XmlActorParams.AnimateSoundName; }
        }
        public string OpenSoundName
        {
            get { return XmlActorParams.OpenSoundName; }
        }
        public string CloseSoundName
        {
            get { return XmlActorParams.CloseSoundName; }
        }

        public string Ent1SoundName
        {
            get { return XmlActorParams.Ent1SoundName; }
        }
        public string Ent2SoundName
        {
            get { return XmlActorParams.Ent2SoundName; }
        }
        public string Ent3SoundName
        {
            get { return XmlActorParams.Ent3SoundName; }
        }
        public string Ent4SoundName
        {
            get { return XmlActorParams.Ent4SoundName; }
        }
        public string Ent5SoundName
        {
            get { return XmlActorParams.Ent5SoundName; }
        }
        public string Ent6SoundName
        {
            get { return XmlActorParams.Ent6SoundName; }
        }
        public string Ent7SoundName
        {
            get { return XmlActorParams.Ent7SoundName; }
        }
        public string Ent8SoundName
        {
            get { return XmlActorParams.Ent8SoundName; }
        }
        public string Ent9SoundName
        {
            get { return XmlActorParams.Ent9SoundName; }
        }
        public string Ent10SoundName
        {
            get { return XmlActorParams.Ent10SoundName; }
        }
        public string Ent11SoundName
        {
            get { return XmlActorParams.Ent11SoundName; }
        }
        public string Ent12SoundName
        {
            get { return XmlActorParams.Ent12SoundName; }
        }
        public float ClusterRange
        {
            get { return clusterRange;  }
        }

        public AudioCue idleCue = null;
        public AudioCue moveCue = null;

        // Provides glow around actor's head.
        protected bool glowing = false;
        private float glowEmissivity = 0;
        protected GlowEmitter glowEmitter = null;
        protected GameTimer gameTimerNoise = new GameTimer(GameTimer.ClockType.GameClock);

        // The glow-aura
        protected Distortion glowAura = null;

        public abstract class Attachment
        {
            #region Members
            private Matrix offset = Matrix.Identity;
            private bool tossOnDeath = false;

            string boneName = null;
            int boneIdx = -1;
            AnimationInstance anim = null;
            #endregion Members

            #region Accessors
            /// <summary>
            /// Transform from local (or bone space) to attachment.
            /// </summary>
            public Matrix Offset
            {
                get { return offset; }
                set { offset = value; }
            }
            public bool TossOnDeath
            {
                get { return tossOnDeath; }
                set { tossOnDeath = value; }
            }
            public int BoneIdx
            {
                get { return boneIdx; }
            }
            public string BoneName
            {
                get { return boneName; }
                set { boneName = value; boneIdx = -1; }
            }
            #endregion Accessors

            #region Public
            /// <summary>
            /// Constructor with simple vector (local space) offset
            /// </summary>
            /// <param name="offset"></param>
            public Attachment(Vector3 offset)
            {
                this.offset = Matrix.CreateTranslation(offset);
            }
            /// <summary>
            /// Constructor with full transform offset
            /// </summary>
            /// <param name="offset"></param>
            public Attachment(Matrix offset)
            {
                this.offset = offset;
            }

            /// <summary>
            /// Constructor for attachment following a bone.
            /// </summary>
            /// <param name="bone"></param>
            /// <param name="offset"></param>
            public Attachment(string boneName, Matrix offset)
            {
                this.boneName = boneName;
                this.boneIdx = -1;
                this.offset = offset;
            }

            public void HookupBone(AnimatorList animList)
            {
                boneIdx = -1;
                if ((animList != null) && animList.NotEmpty && !string.IsNullOrEmpty(boneName))
                {
                    anim = animList.Sample;
                    boneIdx = anim.BoneIndex(boneName);
                    if (boneIdx < 0)
                        anim = null;
                }
            }

            /// <summary>
            /// Do any once a frame update. Return false to indicate you're done and
            /// can be disposed.
            /// </summary>
            /// <param name="local"></param>
            /// <returns></returns>
            public abstract bool Update(Matrix local, float scale);
            /// <summary>
            /// Get ready to go. If start is true, then actually begin animating,
            /// else go into paused but ready state.
            /// </summary>
            /// <param name="start"></param>
            public abstract void Enable(bool start);
            /// <summary>
            /// Stop doing whatever you do. If hard is false, finish out your animation,
            /// but if hard is true, remove all signs of your existence from the scene.
            /// </summary>
            /// <param name="hard"></param>
            public abstract void Disable(bool hard);
            /// <summary>
            /// We're about to let you go. Recycle yourself if you can.
            /// </summary>
            public abstract void Release();
            /// <summary>
            /// Whatever you're attached to has just teleported, so adjust accordingly.
            /// </summary>
            /// <param name="pos"></param>
            public abstract void ResetPosition(Vector3 pos);

            /// <summary>
            /// Notify the system that its owner is going to/from first person.
            /// </summary>
            /// <param name="on"></param>
            public virtual void FirstPerson(bool on)
            {
            }
            #endregion Public

            #region Internal
            protected Matrix Concat(Matrix local, float scale)
            {
                Matrix accum = offset;
                if (BoneIdx >= 0)
                {
                    accum = anim.LocalToWorld(BoneIdx) * accum;
                }
                accum = accum * Matrix.CreateScale(scale) * local;
                return accum;
            }
            #endregion Internal
        }

        protected class OffsetEmitter : Attachment
        {
            #region Members
            private BaseEmitter emitter = null;
            private bool alwaysEmitting = true;
            private bool softDeath = true;
            #endregion Members

            #region Accessors
            public bool AlwaysEmitting
            {
                get { return alwaysEmitting; }
                set { alwaysEmitting = value; }
            }
            public bool Dead
            {
                get { return TossOnDeath && !emitter.Active; }
            }
            /// <summary>
            /// True if after death the effect fades out.
            /// </summary>
            public bool SoftDeath
            {
                get { return softDeath; }
                set { softDeath = value; }
            }

            public BaseEmitter Emitter
            {
                get { return emitter; }
            }

            #endregion Accessors

            #region Public
            public OffsetEmitter(BaseEmitter emitter, Vector3 offset)
                : base(offset)
            {
                this.emitter = emitter;
            }
            public OffsetEmitter(BaseEmitter emitter, Matrix offset)
                : base(offset)
            {
                this.emitter = emitter;
            }
            public OffsetEmitter(BaseEmitter emitter, string boneName, Matrix offset)
                : base(boneName, offset)
            {
                this.emitter = emitter;
            }

            public bool Matches(BaseEmitter em)
            {
                return emitter == em;
            }

            public override bool Update(Matrix local, float scale)
            {
                if (Dead)
                {
                    return false;
                }

                if (emitter is WreathEmitter)
                {
                    // Needed for wreath emitters.
                    emitter.Position = Concat(local, 1.0f).Translation;
                }
                else
                {
                    // Needed for smoke emitters.
                    emitter.Position = Concat(local, scale).Translation;
                }

                emitter.Scale = scale;
                return true;
            }
            public override void Enable(bool start)
            {
                emitter.Dying = false;
                emitter.Active = true;
                emitter.AddToManager();
                if (start && alwaysEmitting && !emitter.Emitting)
                {
                    emitter.Emitting = true;
                }
            }
            public override void Disable(bool hard)
            {
                if (hard || !SoftDeath)
                {
                    // Immediate kill
                    emitter.Active = false;
                    emitter.Emitting = false;
                    emitter.RemoveFromManager();
                }
                else
                {
                    // Start it dying and let nature take its course
                    emitter.Dying = true;
                    emitter.Emitting = false;
                }
            }
            public override void Release()
            {
            }
            public override void ResetPosition(Vector3 pos)
            {
                emitter.Position = pos;
                emitter.ResetPreviousPosition();
            }
            public override void FirstPerson(bool on)
            {
                if (AlwaysEmitting)
                {
                    if (on)
                    {
                        emitter.Dying = true;
                        emitter.Emitting = false;
                    }
                    else
                    {
                        emitter.Dying = false;
                        emitter.Active = true;
                        emitter.AddToManager();
                        if (!emitter.Emitting)
                            emitter.Emitting = true;
                    }
                }
            }
            #endregion Public
        }

        protected List<Attachment> attachments = new List<Attachment>();

        protected BaseSpriteEmitter emoteEmitter = null;

        protected RenderObj renderObj = null;
        protected RenderObj renderObjRevealed = null;   // Revealed version of RenderObj.  Note this causes problems since there
                                                        // is state encapsulated in the RenderObj and it now needs to be kept in
                                                        // sync between these two.  (color...)

        #region Accessors

        public override RenderObject RenderObject
        {
            get { return _revealed ? renderObjRevealed : renderObj; }
        }

        public virtual FBXModel SRO
        {
            get { return _revealed ? renderObjRevealed.SRO : renderObj.SRO; }
        }

        public override BoundingSphere BoundingSphere
        {
            get { return new BoundingSphere(SRO.BoundingSphere.Center * ReScale, SRO.BoundingSphere.Radius * ReScale); }
        }

        public override Vector3 CollisionCenter
        {
            get { return XmlActorParams.CollisionCenter * ReScale; }
        }

        public override float CollisionRadius
        {
            get { return XmlActorParams.CollisionRadius * ReScale; }
        }

        /// <summary>
        /// The thickness of the virtual cushion under a hover bot which
        /// it can touch as it glides over without invoking dynamics.
        /// </summary>
        public float TouchCushion
        {
            get
            {
                return XmlActorParams.HasCushion
                        ? Chassis.EditHeight + XmlActorParams.TouchOffset * ReScale
                        : 0;
            }
        }

        public override AnimatorList Animators
        {
            get { return _animators; }
        }

        public AnimationSet AnimationSet
        {
            get { return animationSet; }
        }

        /// <summary>
        /// Did the user specify this character as immobile in the tweak screen?
        /// </summary>
        public bool TweakImmobile
        {
            get { return SharedParameters.immobile; }
            set { SharedParameters.immobile = value; }
        }

        /// <summary>
        /// This appears to be a version of TweakImmobile that also is to prevent
        /// rotation.  It is only used by the Rover when performing a "rover action",
        /// eg scanning, drilling, etc.  On starting the action, the Rover first turns
        /// to face the target and then freezes while performing the action.
        /// I suspect that this requires a seocnd immobile flag since it's a temporary
        /// freeze rather than one set at edit time.
        /// </summary>
        public bool TweakImmobileNoRot
        {
            get { return SharedParameters.immobileNoRot; }
            set { SharedParameters.immobileNoRot = value; }
        }

        /// <summary>
        /// Did the user specify this character as invulnerable in the tweak screen?
        /// </summary>
        public bool TweakInvulnerable
        {
            get { return SharedParameters.invulnerable; }
            set { SharedParameters.invulnerable = value; }
        }

        /// <summary>
        /// Is character currently invulnerable? This value can be set at runtime from the "open" and "close" verbs.
        /// </summary>
        public override bool Invulnerable
        {
            get { return invulnerable | TweakInvulnerable; }
            set { invulnerable = value; }
        }

        /// <summary>
        /// Gets or sets whether this actor should be included on the 'create' menu.
        /// </summary>
        public bool Creatable
        {
            // Access this field directly instead of proxying thru the creatable master
            get
            {
                return localParameters.creatable;
            }
            set
            {
                if (localParameters.creatable != value)
                {
                    localParameters.creatable = value;
                    InitAsCreatableOrNot();
                }
            }
        }

        /// <summary>
        /// Scale multiplier for the size of this bot.
        /// </summary>
        public override float ReScale
        {
            get { return SharedParameters.rescale; }
            set
            {
                SharedParameters.rescale = value;
                /// Reset the height offset, because changing the scale might
                /// have changed the min height.
                HeightOffset = HeightOffset;
            }
        }

        /// <summary>
        /// Transition scaling from value (what we're lerping from)
        /// </summary>
        public float ReScaleFromScale
        {
            get; 
            set;
        }

        /// <summary>
        /// Transition scaling to value (what we're lerping to)
        /// </summary>
        public float ReScaleToScale
        {
            get;
            set;
        }


        //these are only changed at runtime, always local
        public bool ReScaling
        {
            get;
            set;
        }

        //these are only changed at runtime, always local
        public int ReScaleTwitchID
        {
            get;
            set;
        }

        /// <summary>
        /// Scale multiplier for holding distance
        /// </summary>
        public override float HoldDistance
        {
            get { return SharedParameters.holdDistance; }
            set
            {
                SharedParameters.holdDistance = value;
            }
        }

        public override bool Mute
        {
            get { return SharedParameters.mute; }
            set { SharedParameters.mute = value; }
        }

        /// <summary>
        /// Render visual representation of sensor cones/ranges.
        /// </summary>
        public bool ShowSensors
        {
            get { return SharedParameters.showSensors; }
            set { SharedParameters.showSensors = value; }
        }

        /// <summary>
        /// Render visual representation of actor's hit points.
        /// </summary>
        public bool ShowHitPoints
        {
            get { return SharedParameters.showHitPoints; }
            set
            {
                SharedParameters.showHitPoints = value;
                HealthBarManager.SetRegistered(this, value);

                // Propagate setting to linked actors
                GameActor source = null;

                if (Creatable)
                {
                    source = this;
                }
                else if (IsClone)
                {
                    source = InGame.inGame.GetCreatable(creatableId);
                }

                if (source != null)
                {
                    // Copy to source
                    HealthBarManager.SetRegistered(source, value);

                    // Copy to clones
                    List<GameActor> clones = new List<GameActor>();
                    InGame.inGame.GetClones(creatableId, clones);

                    foreach (GameActor clone in clones)
                    {
                        HealthBarManager.SetRegistered(clone, value);
                    }
                }
            }
        }

        /// <summary>
        /// Returns true if the actor was cloned from a creatable.
        /// </summary>
        public bool IsClone
        {
            get { return !Creatable && CreatableId != Guid.Empty; }
        }

        /// <summary>
        /// Gets or sets the maximum number of hit points on this actor.
        /// Setting this value resets current hit points.
        /// </summary>
        public int MaxHitPoints
        {
            get { return SharedParameters.maxHitPoints; }
            set
            {
                SharedParameters.maxHitPoints = value;

                // Don't propagate change to clones during runSim.  A change in
                // this value should only be propagate during edit mode.
                if (InGame.inGame.CurrentUpdateMode != InGame.UpdateMode.RunSim)
                {
                    // Match current hitpoints to new max.  Note that we're not doing this during runtime.
                    currHitPoints = prevHitPoints = value;

                    // Propagate setting to linked actors
                    GameActor source = null;

                    if (Creatable)
                    {
                        source = this;
                    }
                    else if (IsClone)
                    {
                        source = InGame.inGame.GetCreatable(creatableId);
                    }

                    if (source != null)
                    {
                        // Copy to source
                        source.currHitPoints = source.prevHitPoints = value;

                        // Copy to clones
                        List<GameActor> clones = new List<GameActor>();
                        InGame.inGame.GetClones(creatableId, clones);

                        foreach (GameActor clone in clones)
                        {
                            clone.currHitPoints = clone.prevHitPoints = value;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Health-bar damage inflicted by a blip projectile. Negative values heal. N/A for non-damage payloads. Can be overridden by damage/heal verbs on shot-hit sensor.
        /// </summary>
        public int BlipDamage
        {
            get { return SharedParameters.blipDamage; }
            set { SharedParameters.blipDamage = value; }
        }

        /// <summary>
        /// Health-bar damage inflicted by a cruise missile. Negative values heal. N/A for non-damage payloads. Can be overridden by damage/heal verbs on shot-hit sensor.
        /// </summary>
        public int MissileDamage
        {
            get { return SharedParameters.missileDamage; }
            set { SharedParameters.missileDamage = value; }
        }

        /// <summary>
        /// Value used to modify chassis movement limits.  Acts as a multiplier to the chassis limits
        /// so that 1.0 is the default, 0.5 will create a character that moves at half speed and 2.0
        /// will move at double the normal speed.
        /// </summary>
        public float MovementSpeedModifier
        {
            get { return SharedParameters.movementSpeedModifier; }
            set { SharedParameters.movementSpeedModifier = value; }
        }

        /// <summary>
        /// Value used to modify chassis movement limits.  Acts as a multiplier to the chassis limits
        /// so that 1.0 is the default, 0.5 will create a character that moves at half speed and 2.0
        /// will move at double the normal speed.
        /// </summary>
        public float TurningSpeedModifier
        {
            get { return SharedParameters.turningSpeedModifier; }
            set { SharedParameters.turningSpeedModifier = value; }
        }

        /// <summary>
        /// Value used to modify chassis movement limits.  Acts as a multiplier to the chassis limits
        /// so that 1.0 is the default, 0.5 will create a character that accelerates at half speed and 2.0
        /// will accelerate at double the normal rate.
        /// </summary>
        public float LinearAccelerationModifier
        {
            get { return SharedParameters.linearAccelerationModifier; }
            set { SharedParameters.linearAccelerationModifier = value; }
        }

        /// <summary>
        /// Value used to modify chassis movement limits.  Acts as a multiplier to the chassis limits
        /// so that 1.0 is the default, 0.5 will create a character that accelerates at half speed and 2.0
        /// will accelerate at double the normal rate.
        /// </summary>
        public float TurningAccelerationModifier
        {
            get { return SharedParameters.turningAccelerationModifier; }
            set { SharedParameters.turningAccelerationModifier = value; }
        }

        /// <summary>
        /// Value used to modify vertical speed for bots whose chassis allows them
        /// to move up and down.  Acts as a multiplier to the chassis limits
        /// so that 1.0 is the default, 0.5 will create a character that moves 
        /// at half speed and 2.0 will move at double the normal rate.
        /// </summary>
        public float VerticalSpeedModifier
        {
            get { return SharedParameters.verticalSpeedModifier; }
            set { SharedParameters.verticalSpeedModifier = value; }
        }

        /// <summary>
        /// Value used to modify vertical acceleration for bots whose chassis allows them
        /// to move up and down.  Acts as a multiplier to the chassis limits
        /// so that 1.0 is the default, 0.5 will create a character that accelerates 
        /// at half speed and 2.0 will accelerate at double the normal rate.
        /// </summary>
        public float VerticalAccelerationModifier
        {
            get { return SharedParameters.verticalAccelerationModifier; }
            set { SharedParameters.verticalAccelerationModifier = value; }
        }

        /// <summary>
        /// The time this character requires between missile shots.
        /// </summary>
        public float BlipReloadTime
        {
            get { return SharedParameters.blipReloadTime; }
            set { SharedParameters.blipReloadTime = value; }
        }

        public int BlipSpeed
        {
            get { return SharedParameters.blipSpeed; }
            set { SharedParameters.blipSpeed = value; }
        }

        public float BlipRange
        {
            get { return SharedParameters.blipRange; }
            set { SharedParameters.blipRange = value; }
        }

        public int BlipsInAir
        {
            get { return SharedParameters.blipsInAir; }
            set { SharedParameters.blipsInAir = value; }
        }

        // SGI_MOD Beam related

        public float BeamReloadTime
        {
            get { return SharedParameters.beamReloadTime; }
            set { SharedParameters.beamReloadTime = value; }
        }

        public int BeamSpeed
        {
            get { return SharedParameters.beamSpeed; }
            set { SharedParameters.beamSpeed = value; }
        }

        public float BeamDist
        {
            get { return SharedParameters.beamDist; }
            set { SharedParameters.beamDist= value; }
        }

        public int BeamsInAir
        {
            get { return SharedParameters.beamsInAir; }
            set { SharedParameters.beamsInAir = value; }
        }

        /// <summary>
        /// The time this character requires between missile shots.
        /// </summary>
        public float MissileReloadTime
        {
            get { return SharedParameters.missileReloadTime; }
            set { SharedParameters.missileReloadTime = value; }
        }

        public int MissileSpeed
        {
            get { return SharedParameters.missileSpeed; }
            set { SharedParameters.missileSpeed = value; }
        }

        public float MissileRange
        {
            get { return SharedParameters.missileRange; }
            set { SharedParameters.missileRange = value; }
        }

        public int MissilesInAir
        {
            get { return SharedParameters.missilesInAir; }
            set { SharedParameters.missilesInAir = value; }
        }

        public bool MissileTrails
        {
            get { return SharedParameters.missileTrails; }
            set { SharedParameters.missileTrails = value; }
        }

        /// <summary>
        /// Whether to play the shield effect on collisions and damage.
        /// </summary>
        public bool ShieldEffects
        {
            get { return SharedParameters.shieldEffects; }
            set { SharedParameters.shieldEffects = value; }
        }

        /// <summary>
        /// Override for making this bot not visible.
        /// </summary>
        public override bool Invisible
        {
            get { return SharedParameters.invisible; }
            set { SharedParameters.invisible = value; }
        }

        /// <summary>
        /// True if other bots can't detect or interact with this bot.
        /// </summary>
        public override bool Ignored
        {
            get { return SharedParameters.ignored; }
            set { SharedParameters.ignored = value; }
        }

        /// <summary>
        /// True if other bots can't detect or interact with this bot but bot can collide
        /// </summary>
        public override bool Camouflaged
        {
            get { return SharedParameters.camouflaged; }
            set { SharedParameters.camouflaged = value; }
        }

        /// <summary>
        /// The base strength of an actor's push.  
        /// Actors will be push by this amount of force
        /// Default is 5.0.
        /// </summary>
        public float PushStrength
        {
            get { return SharedParameters.pushStrength; }
            set { SharedParameters.pushStrength = value; }
        }

        /// <summary>
        /// The base range of an actor's push.  
        /// Actors within this range will be affected by the push
        /// Default is 30.0.
        /// </summary>
        public float PushRange
        {
            get { return SharedParameters.pushRange; }
            set { SharedParameters.pushRange = value; }
        }

        /// <summary>
        /// Actual push range used by actor which is multiple of the object size scale
        /// </summary>
        public float FinalPushRange
        {
            get { return SharedParameters.pushRange * ReScale; }
        }

        /// <summary>
        /// Actual push width which is multiple of the object size scale
        /// </summary>
        public float FinalPushWidth
        {
            get { return 2.0f * ReScale; } // Might need to make tunable
        }

        /// <summary>
        /// The strength of an actor's kick.  Used as the 
        /// base velocity imparted to the kicked object.
        /// Default is 10.0.
        /// </summary>
        public float KickStrength
        {
            get { return SharedParameters.kickStrength; }
            set { SharedParameters.kickStrength = value; }
        }

        /// <summary>
        /// The number of kicks per second this character can perform.
        /// Default is 2.0.
        /// </summary>
        public float KickRate
        {
            get { return SharedParameters.kickRate; }
            set { SharedParameters.kickRate = value; }
        }

        /// <summary>
        /// How well do I hear, normalized to range [0..1], with 0=>bad, 1=>great.
        /// </summary>
        public float Hearing
        {
            get { return SharedParameters.hearing; }
            set { SharedParameters.hearing = value; }
        }

        /// <summary>
        /// How far in meters can I hear.
        /// </summary>
        public float HearingRange
        {
            /// Remap [0..1] to a plausible range 0..100.
            get
            {
                float hearing = SharedParameters.hearing * 100.0f;
                // Treat 100 as infinity.
                if (hearing == 100.0f)
                {
                    hearing = 1e10f;
                }
                return hearing;
            }
        }

        public float GlowAmt
        {
            get { return SharedParameters.glowAmt; }
            set { SharedParameters.glowAmt = value; }
        }

        /// <summary>
        /// Use up a point light on this guy all the time.
        /// </summary>
        public float GlowLights
        {
            get { return SharedParameters.glowLights; }
            set { SharedParameters.glowLights = value; }
        }

        /// <summary>
        /// Readonly access to the glow emitter.
        /// </summary>
        public GlowEmitter GlowEmitter
        {
            get { return glowEmitter; }
        }

        /// <summary>
        /// Are we currently glowing?
        /// </summary>
        public bool Glowing
        {
            get { return glowing; }
            protected set { glowing = value; }
        }

        /// <summary>
        /// How much light the glow casts onto this bot.
        /// </summary>
        public float GlowEmission
        {
            get { return SharedParameters.glowEmission; }
            set { SharedParameters.glowEmission = value; }
        }

        /// <summary>
        /// How strong is the self lighting term now.
        /// </summary>
        public float GlowEmissivity
        {
            get { return glowEmissivity; }
            set { glowEmissivity = value; }
        }

        /// <summary>
        /// The color we would glow if we were glowing.
        /// </summary>
        public Vector3 GlowColor
        {
            get { return new Vector3(glowEmitter.Color.X, glowEmitter.Color.Y, glowEmitter.Color.Z); }
        }

        /// <summary>
        /// Light range for this object, only valid if it's a light.
        /// </summary>
        public virtual float LightRange
        {
            get { return SharedParameters.lightRange; }
            set { SharedParameters.lightRange = value; }
        }

        /// <summary>
        /// Draw lines showing what this guy sees.
        /// </summary>
        public bool DisplayLOS
        {
            get { return SharedParameters.displayLOS; }
            set { SharedParameters.displayLOS = value; }
        }

        /// <summary>
        /// Draw lines showing what this guy sees and hears.
        /// </summary>
        public bool DisplayLOP
        {
            get { return SharedParameters.displayLOP; }
            set { SharedParameters.displayLOP = value; }
        }

        /// <summary>
        /// If actor has any programming, display currently acitve page.
        /// </summary>
        public bool DisplayCurrentPage
        {
            get { return SharedParameters.displayCurrentPage; }
            set { SharedParameters.displayCurrentPage = value; }
        }

        /// <summary>
        /// Determines whether or not the camera tries to keep this character in view.
        /// </summary>
        public CameraFollowModes CameraFollowMode
        {
            get { return SharedParameters.cameraMode; }
            set { SharedParameters.cameraMode = value; }
        }

        /// <summary>
        /// Controls the bounciness of the object during collisions.
        /// </summary>
        public override float CoefficientOfRestitution
        {
            get { return SharedParameters.coefficientOfRestitution; }
            set { SharedParameters.coefficientOfRestitution = value; }
        }

        /// <summary>
        /// Kinetic Friction constant, 0=frictionless, 1=sandpaper
        /// </summary>
        public override float Friction
        {
            get { return SharedParameters.friction; }
            set { SharedParameters.friction = value; }
        }

        /// <summary>
        /// Kilograms as if it matters.  Mass is scaled by the cube of the ReScale
        /// size.
        /// </summary>
        public override float Mass
        {
            get { return SharedParameters.mass * ReScale * ReScale * ReScale; }
            set { SharedParameters.mass = value; }
        }

        /// <summary>
        /// Whether this bot stays above water or can fly down into it.
        /// </summary>
        public override bool StayAboveWater
        {
            get { return SharedParameters.stayAboveWater; }
            set { SharedParameters.stayAboveWater = value; }
        }

        /// <summary>
        /// For Creatables only, how many runtime copies of me allowed?
        /// </summary>
        public int MaxCreated
        {
            get { return SharedParameters.maxCreated; }
            set { SharedParameters.maxCreated = value; }
        }

        public float NearByDistance
        {
            get { return SharedParameters.nearByDistance; }
            set { SharedParameters.nearByDistance = value; }
        }

        public float FarAwayDistance
        {
            get { return SharedParameters.farAwayDistance; }
            set { SharedParameters.farAwayDistance = value; }
        }

        /// <summary>
        /// Returns the altitude (absolute Z position) for this object 
        /// when in edit mode.  For some bots (boats) this should be 
        /// overridden to also take into account the water level.
        /// </summary>
        public virtual float EditAltitude
        {
            get { return Chassis.EditHeight + Terrain.GetTerrainAndPathHeight(Movement.Position); }
        }

        public override float Cost
        {
            get { return XmlActorParams.Cost; }
        }

        public override Foley.CollisionSound CollisionSound
        {
            get { return XmlActorParams.CollisionSound; }
        }

        /// <summary>
        /// Is this actor classified as a bot?
        /// </summary>
        public bool IsBot
        {
            get { return XmlActorParams.IsBot; }
        }

        /// <summary>
        /// Is this actor classified as a prop?
        /// </summary>
        public bool IsProp
        {
            get { return XmlActorParams.IsProp; }
        }

        /// <summary>
        /// Is this actor classified as a building?
        /// </summary>
        public bool IsBuilding
        {
            get { return XmlActorParams.IsBuilding; }
        }

        /// <summary>
        /// Is this thing visible ie being rendered?
        /// </summary>
        public override bool Visible
        {
            get { return visible && (!SharedParameters.invisible || (InGame.inGame.CurrentUpdateMode != InGame.UpdateMode.RunSim)); }
            set { visible = value; }
        }

        /// <summary>
        /// Return the shared paramaterization for this type of actor.
        /// </summary>
        public XmlGameActor XmlActorParams
        {
            get
            {
                if (StaticActor != null)
                {
                    XmlGameActor result = _revealed ? StaticActor.XmlGameActorRevealed : StaticActor.XmlGameActor;
                    // Most actors don't have a "revealed" state so just return the normal one.
                    if (result == null)
                    {
                        result = StaticActor.XmlGameActor;
                    }

                    return result;
                }
                else
                {
                    return null;
                }
            }
        }

        #endregion Accessors

        public GameActor(
            string classificationName,
            string classificationRevealedName,
            BaseChassis chassis,
            GetModelInstance getModelInstance,
            GetModelInstance getRevealModelInstance,
            StaticActor staticActor)
            : base(classificationName, classificationRevealedName, chassis)
        {
            StaticActor = staticActor;

            renderObj = new RenderObj(this, getModelInstance);
            renderObjRevealed = new RenderObj(this, getRevealModelInstance);

            followPathState = new FollowWaypointsSelector.State(this);

            touchSpoofsIter = (SensorTargetSet.Enumerator)touchSpoofs.GetEnumerator();
            touchedSetIter = (SensorTargetSet.Enumerator)touchedSet.GetEnumerator();
            beamedSetIter = (SensorTargetSet.Enumerator)beamedSet.GetEnumerator();
            inspectedSetIter = (SensorTargetSet.Enumerator)inspectedSet.GetEnumerator();
            scannedSetIter = (SensorTargetSet.Enumerator)scannedSet.GetEnumerator();

            #region Create brain
            brain = new Brain();
            brain.GameActor = this;
            brain.InitEmpty();
            #endregion

            localParameters = new TweakableParameters();

            // If we can't get our XmlGameActor, there isn't
            // much of a point in continuing initialization
            if (XmlActorParams == null)
                return;

            #region Create glow emitter
            glowEmitter = new GlowEmitter(InGame.inGame.ParticleSystemManager);     // Starts in inactive state.
            glowEmitter.ShowSolidCore = true;
            if (XmlActorParams.GlowEmitterData != null)
            {
                glowEmitter.StartRadius = XmlActorParams.GlowEmitterData.StartRadius;
                glowEmitter.EndRadius = XmlActorParams.GlowEmitterData.EndRadius;
            }
            #endregion

            #region Create face
            if (XmlActorParams.FaceParams != null)
            {
                face = Face.MakeFace(getModelInstance, XmlActorParams);
                face.FaceChange += OnFaceChanged;
                preRender += face.SetupForRender;
            }
            #endregion

            BokuGame.Load(this);

            CreateDevices();

            SetupBaseXmlParams();
            
            XmlConstruct();
            InitDefaults(false);

        }   // end of c'tor

        /// <summary>
        /// Reinitialize members that can change at runtime back to their initial
        /// values. DO NOT ALLOCATE ANYTHING IN THIS FUNCTION. This function exists
        /// so that ActorFactory can reinitialize recycled actors. ActorFactory
        /// exists so that we can avoid allocating new memory when the need for a
        /// new actor arises. Allocating memory in this function would defeat the
        /// entire reason for ActorFactory's existence.
        /// </summary>
        public override void InitDefaults(bool revivingDeadActor)
        {
            // WARNING: NO CODE MAY APPEAR BEFORE CALL TO BASE. PUT YOUR ADDITIONS
            //          AT THE END OF THIS FUNCTION.

            // base must be called BEFORE any changes are made at this call-level.
            // This is because the chassis may be reset to default values. We need
            // to apply our bot-specific chassis customizations after this happens.
            base.InitDefaults(revivingDeadActor);

            CreatableId = Guid.Empty;

            if (!revivingDeadActor)
            {
                localParameters.InitDefaults();
                localScores.Reset(ScoreResetFlags.All);
            }

            // Set stayAboveWater option to false for chassis that can't stay above the water
            if ((Chassis is CursorChassis)
                || (Chassis is CycleChassis)
                || (Chassis is DynamicPropChassis)
                || (Chassis is StaticPropChassis)
                || (Chassis is SwimChassis))
            {
                localParameters.stayAboveWater = false;
            }

            followPathState.Reset();

            invulnerable = false;

            // Reset timeouts for shooting.
            nextMissileFire = 0;
            nextBleepFire = 0;
            nextBeamFire = 0;

            Beamed = false;
            Inspected = false;
            Revealed = false;
            Scanned = false;
            ScienceActionPerformed = false;
            if (ScienceActorRotating)
            {
                Chassis.MaxRotationalAcceleration = PrevMaxRotationAcceleration;
                Chassis.MaxRotationRate = PrevMaxRotationRate;
            }
            ScienceActorRotating = false;

            CurrentScienceAction = ScienceAction.None;

            if (animationSet != null)
            {
                animationSet.InitDefaults();
            }

            ReachedEOP = Classification.Colors.None;
            ReadyToProcessEOP = false;

            ResetXmlParams(revivingDeadActor);

            StopScalingTransition(this);

            PrevMaxRotationAcceleration = Chassis.MaxRotationalAcceleration;
            PrevMaxRotationRate = Chassis.MaxRotationRate;

            // For actors that can open/close, set to default state.
            if (XmlActorParams.SpecialActions.OpenCloseData != null)
            {
                openCloseMode = XmlActorParams.SpecialActions.OpenCloseData.DefaultPosition;
            }

            SquashScale = Vector3.One;

            ResetHitPoints();

            // Reset tile based modifier to default value of 1.
            moveSpeedTileModifier = 1.0f;
            turnSpeedTileModifier = 1.0f;

        }   // end of InitDefaults()

        protected virtual void SetupBaseXmlParams()
        {
            if (SRO != null) // Make sure our model is loaded so that we have access to our BoundingSphere
            {
                #region Set grab, kick and scan range
                // If we aren't provided with a kick/grab range,
                // set our range to be just out side our
                // BoundingSphere (Note: this is the desired
                // behavior of most game props.)

                if (XmlActorParams.KickRange == null)
                    kickRange = BoundingSphere.Radius * 1.5f;
                else
                    kickRange = XmlActorParams.KickRange.Value;

                if (XmlActorParams.GrabRange == null)
                    grabRange = BoundingSphere.Radius * 1.5f;
                else
                    grabRange = XmlActorParams.GrabRange.Value;

                if (XmlActorParams.ScanRange == null)
                    scanRange = BoundingSphere.Radius * 1.5f;
                else
                    scanRange = XmlActorParams.ScanRange.Value;

                if (XmlActorParams.BeamRange == null)
                    beamRange = BoundingSphere.Radius * 1.5f;
                else
                    beamRange = XmlActorParams.BeamRange.Value;

                if (XmlActorParams.InspectRange == null)
                    inspectRange = BoundingSphere.Radius * 1.5f;
                else
                    inspectRange = XmlActorParams.InspectRange.Value;

                if (XmlActorParams.ClusterRange == null)
                    clusterRange = BoundingSphere.Radius * 1.5f;
                else
                    clusterRange = XmlActorParams.ClusterRange.Value;

                if (XmlActorParams.BeamDelay== null)
                    beamDelay= 3.0f;
                else
                    beamDelay= XmlActorParams.BeamDelay.Value;

                if (XmlActorParams.InspectDelay == null)
                    inspectDelay = 3.0f;
                else
                    inspectDelay = XmlActorParams.InspectDelay.Value;

                if (XmlActorParams.ScanDelay == null)
                    scanDelay = 3.0f;
                else
                    scanDelay = XmlActorParams.ScanDelay.Value;

                if (XmlActorParams.ScanRings == null)
                    scanRings = 4;
                else
                    scanRings  = XmlActorParams.ScanRings.Value;

                if (XmlActorParams.RoverRotationAcc == null)
                    roverRotationAcc = 50.0f;
                else
                    roverRotationAcc = XmlActorParams.RoverRotationAcc.Value;

                if (XmlActorParams.RoverRotationRate == null)
                    roverRotationRate = 50.0f;
                else
                    roverRotationRate = XmlActorParams.RoverRotationRate.Value;

                if (XmlActorParams.RoverFOV == null)
                    roverFOV = 0.5f;
                else
                    roverFOV = XmlActorParams.RoverFOV.Value;

                if (XmlActorParams.MinPitch == null)
                    minPitch = -Math.PI;
                else
                    minPitch = XmlActorParams.MinPitch.Value;

                if (XmlActorParams.MaxPitch == null)
                    maxPitch = Math.PI;
                else
                    maxPitch = XmlActorParams.MaxPitch.Value;

                if (XmlActorParams.MinRoll == null)
                    minRoll = -Math.PI;
                else
                    minRoll = XmlActorParams.MinRoll.Value;

                if (XmlActorParams.MaxRoll == null)
                    maxRoll = Math.PI;
                else
                    maxRoll = XmlActorParams.MaxRoll.Value;

                // Get rover specific values.
                RoverChassis rc = Chassis as RoverChassis;
                if (rc != null)
                {
                    if (XmlActorParams.RoverHillClimbSpeed == null)
                        rc.RoverHillClimbSpeed = 1.0f;
                    else
                        rc.RoverHillClimbSpeed = XmlActorParams.RoverHillClimbSpeed.Value;

                    if (XmlActorParams.RoverHillStartPitch == null)
                        rc.RoverHillStartPitch = 1.0f;
                    else
                        rc.RoverHillStartPitch = XmlActorParams.RoverHillStartPitch.Value;

                    if (XmlActorParams.RoverHillEndPitch == null)
                        rc.RoverHillEndPitch = 1.0f;
                    else
                        rc.RoverHillEndPitch = XmlActorParams.RoverHillEndPitch.Value;
                }

                #endregion

                #region Set holdingPosition
                if (XmlActorParams.HoldingPositionData != null)
                {
                    float x = XmlActorParams.HoldingPositionData.X + (XmlActorParams.HoldingPositionData.XOffsetByBoundingSphereRadius ? BoundingSphere.Radius : 0);
                    float y = XmlActorParams.HoldingPositionData.Y + (XmlActorParams.HoldingPositionData.YOffsetByBoundingSphereRadius ? BoundingSphere.Radius : 0);
                    float z = XmlActorParams.HoldingPositionData.Z + (XmlActorParams.HoldingPositionData.ZOffsetByBoundingSphereRadius ? BoundingSphere.Radius : 0);
                    holdingPosition = new Vector3(x, y, z);
                }
                else
                {
                    // If no holding position is provided, place then holding position
                    // ontop of our object. (Note: this is the desired behavior of
                    // most game props.)
                    holdingPosition = new Vector3(0, 0, BoundingSphere.Radius);
                }
                #endregion

                #region Set altitude options
                altitudeOptions =
                      (XmlActorParams.AltitudeOptions.IgnoreTerrainFeatures ? AltitudeOptions.IgnoreTerrainFeatures : AltitudeOptions.None)
                    | (XmlActorParams.AltitudeOptions.IgnoreWaves ? AltitudeOptions.IgnoreWaves : AltitudeOptions.None);
                #endregion
            }

            #region Initialize CurbFeelers
            if (XmlActorParams.CurbFeelersData != null)
            {
                float nose = XmlActorParams.CurbFeelersData.Nose;
                float width = XmlActorParams.CurbFeelersData.Width;
                float height = XmlActorParams.CurbFeelersData.Height;

                Chassis.Feelers.Add(new BaseChassis.CurbFeeler(new Vector3(nose, 0.0f, height)));    // forward
                Chassis.Feelers.Add(new BaseChassis.CurbFeeler(new Vector3(0.0f, width, height)));   // left
                Chassis.Feelers.Add(new BaseChassis.CurbFeeler(new Vector3(0.0f, -width, height)));  // right

                if (!XmlActorParams.CurbFeelersData.ExcludeBackwardFeeler)
                    Chassis.Feelers.Add(new BaseChassis.CurbFeeler(new Vector3(-nose, 0.0f, height)));   // backward
            }
            #endregion

            #region Add SmokeSources
            if (XmlActorParams.SmokeSources != null)
            {
                for (int i = 0; i < XmlActorParams.SmokeSources.Count; ++i)
                {
                    XmlSmokeSource xmlSource = XmlActorParams.SmokeSources[i];
                    AddSmoke(xmlSource);
                }
            }
            #endregion      
        }

        protected virtual void ResetXmlParams(bool revivingDeadActor)
        {
            if (XmlActorParams.Friction != null)
                Friction = XmlActorParams.Friction.Value;
            if (XmlActorParams.CoefficientOfRestitution != null)
                CoefficientOfRestitution = XmlActorParams.CoefficientOfRestitution.Value;
            if (XmlActorParams.Mass != null)
                Mass = XmlActorParams.Mass.Value;
            if (XmlActorParams.StayAboveWater != null)
                StayAboveWater = XmlActorParams.StayAboveWater.Value;

            if (XmlActorParams.ClassificationData != null)
            {
                if (!revivingDeadActor)
                {
                    // Ugly hack here:
                    // Normally we reset an actor's color to the default here when recycling.
                    // But, with missiles, this can be a problem.  For instance:
                    // WHEN Bump Missile Green DO ...
                    // The missile ends up being recycled (and its color reset to white) before
                    // the Bump can test the color and trigger.  
                    // So, the hack is to just not reset the color.  This only works for
                    // missiles because whenever they are created, a new color is set.  This
                    // isn't true for most other things.
                    if (!(this is CruiseMissile))
                    {
                        ClassColor = XmlActorParams.ClassificationData.Color;
                    }
                }
                classification.audioImpression = XmlActorParams.ClassificationData.AudioImpression;
                classification.audioVolume = XmlActorParams.ClassificationData.AudioVolume;
                classification.physicality = XmlActorParams.ClassificationData.Physicality;
                classification.emitter = XmlActorParams.ClassificationData.Emitter;
                classification.expression = XmlActorParams.ClassificationData.Expression;

                classificationRevealed.audioImpression = XmlActorParams.ClassificationData.AudioImpression;
                classificationRevealed.audioVolume = XmlActorParams.ClassificationData.AudioVolume;
                classificationRevealed.physicality = XmlActorParams.ClassificationData.Physicality;
                classificationRevealed.emitter = XmlActorParams.ClassificationData.Emitter;
                classificationRevealed.expression = XmlActorParams.ClassificationData.Expression;
            }

            if (XmlActorParams.ChassisData != null)
            {
                XmlActorParams.ChassisData.CopyTo(Chassis);
            }

            if (XmlActorParams.SpecialActions.OpenCloseData != null)
            {
                AnimationSet.IsOpen = (XmlActorParams.SpecialActions.OpenCloseData.DefaultPosition == OpenCloseModes.Open);
                // If invulnerable when closed and currently closed, set invulnerable.
                if (XmlActorParams.SpecialActions.OpenCloseData.DefaultPosition == OpenCloseModes.Closed)
                {
                    TweakInvulnerable = XmlActorParams.SpecialActions.OpenCloseData.Invulnerable;
                }
            }    
        }

        protected virtual void XmlConstruct()
        {
        }

        /// <summary>
        /// Note this must be called after the renderObj is created.  For now this is
        /// called in the base LoadContent call.
        /// </summary>
        protected virtual void InitAnimationSet()
        {
            animationSet = new AnimationSet(this, renderObj.SRO);
        }   // end of InitAnimationSet()

        /// <summary>
        /// React to the cursor moving under this character in edit mode.
        /// </summary>
        public override void ReactToCursor()
        {
            animationSet.StartCursorReactAnimation();

            // Display the selected thing's creatable name in a thought balloon above it.
            Vector4 darkGrey = new Vector4(0.1f, 0.1f, 0.1f, 1.0f); // It's the new black.
            string balloonString = DisplayNameNumber;
            if (XmlOptionsData.HelpLevel > 1)
            {
                if (InGame.inGame.CurrentUpdateMode == InGame.UpdateMode.TouchEdit)
                {
                    balloonString += Strings.Localize("tools.balloonTextTouch");
                }
                else if (InGame.inGame.CurrentUpdateMode == InGame.UpdateMode.MouseEdit)
                {
                    balloonString += Strings.Localize("tools.balloonTextMouse");
                }
                else
                {
                    balloonString += Strings.Localize("tools.balloonText");
                }
            }
            ThoughtBalloonManager.CreateThoughtBalloon(this, balloonString, darkGrey, true);

        }   // end of ReactToCursor()

        public Vector3 GlowPosition
        {
            get
            {
                Vector3 pos = XmlActorParams.GlowPosition.Offset;
                if ((glowBoneIndex >= 0) && (Animators != null))
                {
                    pos = Vector3.Transform(pos, Animators.Sample.LocalToWorld(glowBoneIndex));
                }
                return pos * ReScale;
            }
        }

        public Vector3 WorldGlowPosition
        {
            get { return Vector3.Transform(GlowPosition, Movement.LocalMatrix); }
        }

        /// <summary>
        /// Vector offset from the bot's position where "held" objects are positioned.  This
        /// is automatically scaled by the bot's scale.
        /// </summary>
        public Vector3 HoldingPosition
        {
            // Returns the offset in model space where we want to be holding our object.
            get { return holdingPosition * HoldDistance * ReScale; }
        }

        public Vector3 WorldHoldingPosition
        {
            get { return Vector3.Transform(HoldingPosition, Movement.LocalMatrix); }
        }

        public Vector3 SparkPosition(Vector3 direction)
        {
            Vector3 center = WorldCollisionCenter;

            return center - direction * CollisionRadius;
        }

        // The public interface is always recriprical.
        public void AddTouched(GameThing thing, Vector3 direction, float range)
        {
            AddTouched(thing, direction, range, true);
        }

        public void AddTouched(GameThing thing, Vector2 direction, float range)
        {
            AddTouched(thing, new Vector3(direction, 0.0f), range, true);
        }

        // The private interface makes sure the recursion terminates.
        private void AddTouched(GameThing thing, Vector3 direction, float range, bool reciprocate)
        {
            // Don't collide with the thing holding us.
            if (ActorHoldingThis == thing)
                return;

            // Don't collide with the thing we are holding.
            if (ThingBeingHeldByThisActor == thing)
                return;

            GameActor targetActor = thing as GameActor;
            if (targetActor != null && !targetActor.CanBeSensed())
                return;
            
            SensorTarget target = SensorTargetSpares.Alloc();
            target.Init(thing, direction, range);

            // If the target is already in the touch set, then we're in a dangerous but valid
            // condition where a chain of more than two bots are in a holding circle and all
            // colliding, that is, A is holding B, B is holding C, and C is holding A.
            if (!touchSpoofs.Add(target))
            {
                SensorTargetSpares.Free(target);
                return;
            }

            // If we're being carried by another actor, propagate the bump
            if (ActorHoldingThis != null && ActorHoldingThis is GameActor && ActorHoldingThis != thing)
                (ActorHoldingThis as GameActor).AddTouched(thing, direction, range, reciprocate);

            if (reciprocate)
            {
                GameActor otherActor = thing as GameActor;
                if (otherActor != null)
                {
                    // Make sure we pass reciprocate==false, to avoid
                    // infinite reciprocation/recursion.
                    otherActor.AddTouched(this, -direction, range, false);
                }
            }
        }

        /// <summary>
        /// Spoof a collision event to cause a bump (reciprical) to get processed next brain update.
        /// </summary>
        /// <param name="otherActor"></param>
        public void AddTouchSpoof(GameActor otherActor)
        {
            Vector3 direction = otherActor.Movement.Position - Movement.Position;
            float range = direction.Length();

            if (otherActor.CanBeSensed())
            {
                SensorTarget target = SensorTargetSpares.Alloc();
                target.Init(otherActor, direction, range);
                touchSpoofs.AddOrFree(target);

                target = SensorTargetSpares.Alloc();
                target.Init(this, -direction, range);
                otherActor.touchSpoofs.AddOrFree(target);
            }
        }

        private void ProcessTouchSpoofs()
        {
            touchSpoofsIter.Reset();

            while (touchSpoofsIter.MoveNext())
            {
                SensorTarget target = (SensorTarget)touchSpoofsIter.Current;
                touchedSet.Add(target);
            }

            touchSpoofs.Clear();
        }

        private void ClearTouchedSet()
        {
            touchedSet.Clear();
        }

        private void ClearEatenSet()
        {
            eatenSet.Clear();
        }

        private void ClearShooterSet()
        {
            shooterHitSet.Clear();
        }

        private void ClearBeamedSet()
        {
            beamedSet.Clear();
        }

        private void ClearInspectedSet()
        {
            inspectedSet.Clear();
        }

        private void ClearScannedSet()
        {
            scannedSet.Clear();
        }

        private void ProcessMissileHitSet()
        {
            if (missileHitSet.Count > 0)
            {
                // The following code can throw under rare circumstances.  If you program
                // a Kodu with WHEN See Cycle DO Shoot and cycle with WHEN Health 0 DO Reset World
                // you get a situation where the missileHitSet gets reset in the middle of the 
                // foreach loop causing an exception.  The try/catch lets it work as expected.
                // A "proper" fix might be a bit more involved...
                try
                {
                    foreach (SensorTarget target in missileHitSet)
                    {
                        MissileHitTargetParam param = target.Tag as MissileHitTargetParam;

                        bool died = false;
                        target.GameThing.DoDamage(-param.defaultDamage, param.defaultPayload, false, false, this, out died);

                        // Special case for the Stun verb.  We always want to perform this action on the target.
                        if (!died && param.defaultPayload == GameThing.Verbs.Stun)
                            target.GameThing.ExecuteVerb(param.defaultPayload, null, null, Mute);

                        if (!died)
                        {
                            PlayShieldEffect(
                                param.defaultPayload,
                                param.hitStrength,
                                target.GameThing as GameActor,
                                param.hitPosition);
                        }

                        param.Release();

                        if (died)
                            break;
                    }
                }
                catch(Exception e)
                {
                    if (e != null)
                    {
                    }
                }

                missileHitSet.Clear();
            }
        }

        /// <summary>
        /// Play the shield effect from when a missile has caused us damage (or healing).
        /// Passes the missile impact position on to the DamageEffect.
        /// </summary>
        /// <param name="verbPayload"></param>
        /// <param name="damage"></param>
        /// <param name="actor"></param>
        /// <param name="hitPos"></param>
        private void PlayShieldEffect(GameThing.Verbs verbPayload, int damage, GameActor actor, Vector3 hitPos)
        {
            if (actor != null)
            {
                if ((verbPayload != GameThing.Verbs.Vanish)
                    && (verbPayload != GameThing.Verbs.Stun)
                    && (damage != 0))
                {
                    actor.PlayDamageEffect(damage, hitPos);
                }
            }
        }

        /// <summary>
        /// Play a damage effect for specified damage at specified "contact" point.
        /// </summary>
        /// <param name="damage"></param>
        /// <param name="hitPos"></param>
        private void PlayDamageEffect(int damage, Vector3 hitPos)
        {
            if (ShieldEffects)
            {
                float hitRadius = CollisionRadius;

                Vector3 hitColor = damage < 0
                    ? new Vector3(0.3f, 1.0f, 0.5f)
                    : new Vector3(1.0f, 0.5f, 0.3f);

                Shield.AddImpact(this, hitPos, hitRadius, hitColor);
            }
        }

        /// <summary>
        /// Put up a shield effect for bounces off terrain and glass walls.
        /// </summary>
        /// <param name="hitPos"></param>
        /// <param name="glass"></param>
        public void PlayBounceEffect(Vector3 hitPos, bool glass)
        {
            if (ShieldEffects)
            {
                hitPos.Z = WorldCollisionCenter.Z;

                float hitRadius = WorldCollisionRadius;

                Vector3 hitColor = glass
                    ? new Vector3(0.1f, 0.15f, 0.3f)
                    : new Vector3(0.2f, 0.2f, 0.1f);

                if (Chassis as DynamicPropChassis == null)
                {
                    Shield.AddImpact(this, hitPos, hitRadius, hitColor);
                }
            }
        }

        /// <summary>
        /// Queue a thing for potential "give" to this actor.
        /// </summary>
        /// <param name="thing"></param>
        private void AddGiven(GameThing thing)
        {
            if (thing == ThingBeingHeldByThisActor)
                return;

            SensorTarget target = SensorTargetSpares.Alloc();
            target.Init(thing, Vector3.Zero, 0f);
            givenSet.AddOrFree(target);
        }

        /// <summary>
        /// Process set of potential "gives".  List will have been filtered by "Got" reflexes, leaving only compatible list entries.
        /// </summary>
        private void ProcessGivenSet()
        {
            if (givenSet.Count > 0)
            {
                Foley.PlayGive(givenSet.Nearest.GameThing);
                DoGrabObject(givenSet.Nearest.GameThing, true, this.Mute);
            }

            givenSet.Clear();
        }

        public void ClearHits()
        {
            ClearLines();
        }

        /// <summary>
        /// Allow derived actor types to override the physical collision bounce with
        /// some other deranged behavior.
        /// </summary>
        /// <param name="other"></param>
        /// <param name="MouseTouchHitInfo"></param>
        /// <returns></returns>
        protected virtual bool SpecialBounce(GameActor other, ref MouseTouchHitInfo MouseTouchHitInfo)
        {
            return false;
        }

        public virtual void ApplyCollisions(ref MouseTouchHitInfo MouseTouchHitInfo)
        {
            if (Chassis.FixedPosition)
                return;

            if (SRO.DisplayCollisions)
            {
                AddLine(MouseTouchHitInfo);
            }
            if (ActorHoldingThis != null)
            {
                GameActor actorHolding = ActorHoldingThis as GameActor;
                if (actorHolding != null && actorHolding != MouseTouchHitInfo.Other)
                {
                    actorHolding.ApplyCollisions(ref MouseTouchHitInfo);
                }
                return;
            }

            GameActor actor = MouseTouchHitInfo.Other;
            Debug.Assert(actor != null, "Received a null other from collision system");
            if ((actor.ThingBeingHeldByThisActor != this)   // It's not holding us.
                && (ThingBeingHeldByThisActor != actor))    // We're not holding it.
            {
                if (!MouseTouchHitInfo.Handled)
                {
                    Vector3 delta = actor.Movement.Position - Movement.Position;
                    AddTouched(actor, delta, delta.Length());

                    // Seagrass is a special case.  We want actors to be able to swim through it without getting blocked
                    // but we still want bumps to be detectable.  So, after adding the touch, exit before bouncing.
                    // TODO (scoy) Need to add this as a setting in a generic way for all actors.  Should be able to
                    // independently turn off physical collisions and bump detection.
                    if (actor.Classification.name == "seagrass" || (MouseTouchHitInfo.OtherMover != null && MouseTouchHitInfo.OtherMover.Owner.Classification.name == "seagrass"))
                    {
                        return;
                    }
                }

                // If actors are already moving apart, ignore the collision.
                Vector3 deltaPos = actor.Movement.Position - Movement.Position;
                Vector3 deltaVel = actor.Movement.Velocity - Movement.Velocity;
                if (Vector3.Dot(deltaPos, deltaVel) > 0.0f)
                {
                    return;
                }

                if (!SpecialBounce(actor, ref MouseTouchHitInfo) && !actor.SpecialBounce(this, ref MouseTouchHitInfo))
                {
                    if (!actor.Chassis.FixedPosition)
                    {
                        // Do a normal collision.
                        // Move them apart so they're no longer colliding.
                        if (!MouseTouchHitInfo.Handled)
                        {
                            Vector3 normal = PerturbNormal(MouseTouchHitInfo.Normal);
                            CollisionBounce(this, actor, normal);
                            actor.Chassis.Moving = true;
                        }
                    }
                    else
                    {
                        // Do a "soft" collision.

                        // Reflect velocity vector to simulate bounce.
                        Vector3 normal = PerturbNormal(MouseTouchHitInfo.Normal);
                        float velDotNorm = Vector3.Dot(Movement.Velocity, normal);
                        if (velDotNorm <= 0)
                        {
                            // Movement.Velocity = Vector3.Reflect(Movement.Velocity, normal);

                            Vector3 velNormal = velDotNorm * normal;
                            Vector3 velTangent = Movement.Velocity - velNormal;

                            float bounce = CoefficientOfRestitution * actor.CoefficientOfRestitution;
                            velNormal *= bounce;

                            velTangent *= FrictionDecay();

                            Movement.Velocity = velTangent - velNormal;

                        }
                    }
                }

                // If either entity in the collision is a DynamicPropChassis
                // switch their gravity form 0 to normal so they fall after the bump.
                if (actor.Chassis is DynamicPropChassis)
                {
                    actor.Chassis.Gravity = BaseChassis.kGravity;
                }
                if (Chassis is DynamicPropChassis)
                {
                    Chassis.Gravity = BaseChassis.kGravity;
                }

                // TODO (scoy) Encapsulate this in some "collision effect" manager so we 
                // can have different sounds/visuals for different types of collisions.
                // For now, just don't do effects for prop/prop collisions.
                if (Chassis as DynamicPropChassis == null && actor.Chassis as DynamicPropChassis == null)
                {
                    ExplosionManager.CreateSpark(MouseTouchHitInfo.Contact, 2, 0.02f, 1.0f);

                    if (!MouseTouchHitInfo.Touching)
                    {
                        // Collision sound.
                        Audio.Foley.PlayCollision(actor, MouseTouchHitInfo.Other);
                    }
                }
                // No shield or sparks for apples or rocks.
                if (ShieldEffects && Chassis as DynamicPropChassis == null)
                    Shield.AddImpact(this, MouseTouchHitInfo.Contact, WorldCollisionRadius, new Vector3(0.01f, 0.1f, 0.1f));
                if (actor.ShieldEffects && actor.Chassis as DynamicPropChassis == null)
                    Shield.AddImpact(actor, MouseTouchHitInfo.Contact, actor.WorldCollisionRadius, new Vector3(0.01f, 0.1f, 0.1f));

                Chassis.Moving = true;

                MouseTouchHitInfo.Handled = true;
            }
        }

        public Vector3 PerturbNormal(Vector3 normal)
        {
            Random rnd = BokuGame.bokuGame.rnd;

            float perturb = 0.05f;
            Vector3 ret = new Vector3(
                (float)(normal.X + perturb * (rnd.NextDouble() - rnd.NextDouble())),
                (float)(normal.Y + perturb * (rnd.NextDouble() - rnd.NextDouble())),
                (float)(normal.Z + perturb * (rnd.NextDouble() - rnd.NextDouble())));

            return Vector3.Normalize(ret);
        }

        /// <summary>
        /// Compute the right amount to dampen the tangent velocity for this frame interval.
        /// </summary>
        /// <returns></returns>
        public float FrictionDecay()
        {
            return FrictionDecay(Time.GameTimeFrameSeconds);
        }

        /// <summary>
        /// Compute the right amount to dampen the tangent velocity for the input time interval.
        /// </summary>
        /// <returns></returns>
        public float FrictionDecay(float dt)
        {
            return FrictionDecay(Friction, dt);
        }

        /// <summary>
        /// Compute the right amount to dampen a tangent velocity for the input time interval
        /// and friction parameter.
        /// 
        /// TODO (scoy) This is totally useless if it doesn't even document the range
        /// of values and which end of the range aligns with slick vs sticky.
        /// </summary>
        /// <param name="friction"></param>
        /// <param name="dt"></param>
        /// <returns></returns>
        public static float FrictionDecay(float friction, float dt)
        {
            Debug.Assert((friction >= 0.0f) && (friction <= 1.0f));
            if (dt <= 0)
                return 1.0f;

            if (friction >= 1)
                return 0.0f;

            float fricK = kNoFriction + friction * (kBigFriction - kNoFriction);
            float fric = (float)Math.Pow(fricK, dt);

            Debug.Assert((fric >= 0.0f) && (fric <= 1.0f));

            return fric;
        }

        /// <summary>
        /// kBigFriction can be calculated as pow(Z, 1/T), where:
        /// Z is the fraction of starting velocity left after T seconds.
        /// For example, k = pow(0.1f, 1/0.5f) gives a frictional constant
        /// that will reduce the speed to 10% after half a second.
        /// </summary>
        private const float kBigFriction = 0.0000000001f; // pow(0.1f, 1 / 0.1f)
        private const float kNoFriction = 1.0f; // frictionless, T=infinity

        /// <summary>
        /// We've detected that the two actors have collided so now 
        /// calc the bounce and apply this to the actor's velocities.
        /// </summary>
        /// <param name="actor0"></param>
        /// <param name="actor1"></param>
        protected void CollisionBounce(GameActor actor0, GameActor actor1, Vector3 normal)
        {
            // Calc combined coefficient of restitution.
            float combinedCOR = actor0.CoefficientOfRestitution * actor1.CoefficientOfRestitution;

            bool swapped = false;
            if (actor0.TweakImmobile || actor0.TweakImmobileNoRot || actor0.Chassis.Constraints == ConstraintModifier.Constraints.Immobile)
            {
                // Swap to make actor1 the immobile one.
                GameActor tmp = actor0;
                actor0 = actor1;
                actor1 = tmp;
                swapped = true;
            }
            if (swapped || actor1.TweakImmobile || actor1.TweakImmobileNoRot || actor1.Chassis.Constraints == ConstraintModifier.Constraints.Immobile)
            {
                // Actor1 is immobile so just make actor0 bounce.
                actor0.Movement.Velocity = combinedCOR * Vector3.Reflect(actor0.Movement.Velocity, normal);
                // Restore the position so that we don't "tunnel" into the stationary object.
                actor0.Movement.Position = actor0.Movement.PrevPosition;
                // Apply a little of the bounce.  Note that if CombinedCOR is 0 then the objects will still stick together.
                actor0.Movement.Position += actor0.Movement.Velocity * Time.GameTimeFrameSeconds;

                return;
            }

            float mass0 = actor0.Chassis.Mass;
            float mass1 = actor1.Chassis.Mass;

            // If a bot is user controlled, act like it has a large amount of mass.
            // This lets the user retain a decent amount of control rather than
            // getting pushed around a lot.
            if (actor0.Brain.ActiveTask != null && actor0.Brain.ActiveTask.IsUserControlled)
            {
                mass0 *= 100000.0f;
            }

            if (actor1.Brain.ActiveTask != null && actor1.Brain.ActiveTask.IsUserControlled)
            {
                mass1 *= 100000.0f;
            }

            float totalMass = mass0 + mass1;

            Vector3 velocity0 = actor0.Movement.Velocity;
            Vector3 velocity1 = actor1.Movement.Velocity;

            // Calc speed along normal.
            float normSpeed0 = Vector3.Dot(velocity0, normal);
            float normSpeed1 = Vector3.Dot(velocity1, normal);

            // Apply coefficient of restitution ONLY to velocity along normal.

            // Calc speed of center of mass along normal.
            float normSpeedCM = (mass0 * normSpeed0 + mass1 * normSpeed1) / totalMass;

            // Calc the tangent velocities before messing with speeds.
            Vector3 tangentVel0 = velocity0 - normSpeed0 * normal;
            Vector3 tangentVel1 = velocity1 - normSpeed1 * normal;

            // Used the combined COR to lerp between a elastic and inelastic collision results.
            normSpeed0 = MyMath.Lerp(normSpeedCM, normSpeed0, combinedCOR);
            normSpeed1 = MyMath.Lerp(normSpeedCM, normSpeed1, combinedCOR);

            // Calc new normal speeds.
            float ns0 = (normSpeed0 * (mass0 - mass1) + 2.0f * mass1 * normSpeed1) / totalMass;
            float ns1 = (normSpeed1 * (mass1 - mass0) + 2.0f * mass0 * normSpeed0) / totalMass;

            // Calc new combined velocities.
            actor0.Movement.Velocity = ns0 * normal + tangentVel0 * actor0.FrictionDecay();
            actor1.Movement.Velocity = ns1 * normal + tangentVel1 * actor1.FrictionDecay();

        }   // end of GameActor CollisionBounce()

        /// <summary>
        /// Details on what is blocking us.
        /// </summary>
        public struct BlockedInfo
        {
            /// <summary>
            /// Where I will be when I make contact
            /// </summary>
            public Vector3 Center;
            /// <summary>
            /// Where on my collision hull will there be contact
            /// </summary>
            public Vector3 Contact;
            /// <summary>
            /// What will be the normal (pointing out from me) of the contact.
            /// </summary>
            public Vector3 Normal;
            /// <summary>
            /// What is blocking us. Will be null if it is terrain.
            /// </summary>
            public GameActor Blocker;
        }

        /// <summary>
        /// Internal workhorse for the public blocked members. Check terrain and LOS
        /// systems to see if there's any obstruction, besides what's listed in excluded,
        /// between this actor and the target toward.
        /// </summary>
        /// <param name="toward"></param>
        /// <param name="excluded"></param>
        /// <param name="blockInfo"></param>
        /// <returns></returns>
        private bool Blocked(Vector3 toward, List<GameThing> excluded, ref BlockedInfo blockInfo)
        {
            excluded.Add(this);
            if (ThingBeingHeldByThisActor != null)
            {
                excluded.Add(ThingBeingHeldByThisActor);
            }
            if (ActorHoldingThis != null)
            {
                excluded.Add(ActorHoldingThis);
            }
            SimWorld.Terra.Terrain.HitBlock hitBlock = new Boku.SimWorld.Terra.Terrain.HitBlock();

            Vector2 minMaxZ = new Vector2(0.0f, Single.MaxValue);

            Vector4 maxStep = new Vector4(
                Chassis.EditHeight + Chassis.WaistOffset, // max single step up
                Single.MinValue, // max step down
                -1.0f, // water depth at which transition land to water occurs (-1 to ignore)
                -1.0f); // water depth at which transition water to land occurs (-1 to ignore)

            switch (Domain)
            {
                case GameActor.MovementDomain.Air:
                    minMaxZ.X = -1.0f;
                    break;
                case GameActor.MovementDomain.Land:
                    maxStep.Z = 1.0f;
                    break;
                case GameActor.MovementDomain.Water:
                    maxStep.X = Single.MaxValue;
                    maxStep.W = 1.0f;
                    break;
            }
            if (!Chassis.IgnoreGlassWalls)
                minMaxZ.X = 0;

            Vector3 from = WorldCollisionCenter;

            bool terrainBlocked = false;
            if (SimWorld.Terra.Terrain.Blocked(from, toward, minMaxZ, maxStep, ref hitBlock, Movement.Altitude))
            {
                terrainBlocked = true;
            }


            bool objectBlocked = false;
            if (CollSys.TestAll(from, toward, CollisionRadius, _scratchPreHits))
            {
                objectBlocked = true;
                ClearExcluded(_scratchPreHits, _scratchPostHits, _scratchExcluded);
            }
            objectBlocked = _scratchPostHits.Count > 0;

            if (terrainBlocked && objectBlocked)
            {
                /// Which was closer?
                if (Vector3.DistanceSquared(from, hitBlock.Position)
                    < Vector3.DistanceSquared(from, _scratchPostHits[0].Contact))
                {
                    objectBlocked = false;
                }
                else
                {
                    terrainBlocked = false;
                }
            }
            if (terrainBlocked)
            {
                /// Start with the useful bits.
                blockInfo.Contact = hitBlock.Position;
                blockInfo.Center = blockInfo.Contact - Vector3.Normalize(toward - from) * CollisionRadius;
                blockInfo.Normal = hitBlock.Normal;
                blockInfo.Blocker = null;
            }
            else if (objectBlocked)
            {
                blockInfo.Center = _scratchPostHits[0].Center;
                blockInfo.Contact = _scratchPostHits[0].Contact;
                blockInfo.Normal = _scratchPostHits[0].Normal;
                blockInfo.Blocker = _scratchPostHits[0].Other;
            }
            _scratchPostHits.Clear();
            _scratchPreHits.Clear();

            return terrainBlocked || objectBlocked;
        }

        /// <summary>
        /// Remove all excluding things from hitsIn, and put the unexcluded ones in hitsOut.
        /// hitsIn and excluded unchanged.
        /// </summary>
        /// <param name="hitsIn"></param>
        /// <param name="hitsOut"></param>
        /// <param name="excluded"></param>
        private void ClearExcluded(List<MouseTouchHitInfo> hitsIn, List<MouseTouchHitInfo> hitsOut, List<GameThing> excluded)
        {
            hitsOut.Clear();

            if (hitsIn.Count > 1)
                hitsIn.Sort(_CompareMouseTouchHitInfo);
            if (excluded.Count > 1)
                excluded.Sort(_CompareThing);

            int iHit = 0;
            int iExcl = 0;
            while ((iHit < hitsIn.Count) && (iExcl < excluded.Count))
            {
                int compare = hitsIn[iHit].Other.CompareTo(excluded[iExcl]);
                if (compare < 0)
                {
                    hitsOut.Add(hitsIn[iHit]);
                    ++iHit;
                }
                else if (compare > 0)
                {
                    ++iExcl;
                }
                else
                {
                    ++iHit;
                    ++iExcl;
                }
            }
            while (iHit < hitsIn.Count)
            {
                hitsOut.Add(hitsIn[iHit]);
                ++iHit;
            }

        }

        /// <summary>
        /// See if there's anything blocking us from getting to the target position
        /// excluding the input actor (which is presumably at the target position).
        /// </summary>
        /// <param name="actor"></param>
        /// <param name="target"></param>
        /// <param name="blockInfo"></param>
        /// <returns></returns>
        public bool Blocked(GameThing thing, Vector3 target, ref BlockedInfo blockInfo)
        {
            _scratchExcluded.Clear();
            _scratchExcluded.Add(thing);
            return Blocked(target, _scratchExcluded, ref blockInfo);
        }

        /// <summary>
        /// Return true if I'm blocked from reaching the actor, including details in blockInfo.
        /// False if no blockage, and blockInfo remains untouched.
        /// </summary>
        /// <param name="actor"></param>
        /// <param name="MouseTouchHitInfo"></param>
        /// <returns></returns>
        public bool Blocked(GameActor actor, ref BlockedInfo blockInfo)
        {
            return Blocked(actor, actor.WorldCollisionCenter, ref blockInfo);
        }

        /// <summary>
        /// Return true if I'm blocked from reaching the target position, with details in blockInfo.
        /// False if no obstruction, and blockInfo remains untouched.
        /// </summary>
        /// <param name="target"></param>
        /// <param name="blockInfo"></param>
        /// <returns></returns>
        public bool Blocked(Vector3 target, ref BlockedInfo blockInfo)
        {
            _scratchExcluded.Clear();
            return Blocked(target, _scratchExcluded, ref blockInfo);
        }

        /// <summary>
        /// Internal for avoiding GC, don't use.
        /// </summary>
        private static List<MouseTouchHitInfo> _scratchPreHits = new List<MouseTouchHitInfo>();
        /// <summary>
        /// Internal for avoiding GC, don't use.
        /// </summary>
        private static List<MouseTouchHitInfo> _scratchPostHits = new List<MouseTouchHitInfo>();
        /// <summary>
        /// Internal for avoiding GC, don't use.
        /// </summary>
        private static List<GameThing> _scratchExcluded = new List<GameThing>();

        /// <summary>
        /// Internal for sorting actors in exclusion lists.
        /// </summary>
        private class CompareThing : IComparer<GameThing>
        {
            public int Compare(GameThing lhs, GameThing rhs)
            {
                if (lhs.UniqueNum < rhs.UniqueNum)
                    return -1;
                if (lhs.UniqueNum > rhs.UniqueNum)
                    return 1;
                return 0;
            }
        }

        /// <summary>
        /// Internal for sorting hits by the actor within.
        /// </summary>
        private class CompareMouseTouchHitInfo : IComparer<MouseTouchHitInfo>
        {
            public int Compare(MouseTouchHitInfo lhs, MouseTouchHitInfo rhs)
            {
                if (lhs.Other.UniqueNum < rhs.Other.UniqueNum)
                    return -1;
                if (lhs.Other.UniqueNum > rhs.Other.UniqueNum)
                    return 1;
                return 0;
            }
        }

        /// <summary>
        /// Internal comparator for avoiding GC.
        /// </summary>
        private static CompareThing _CompareThing = new CompareThing();
        /// <summary>
        /// Internal comparator for avoiding GC.
        /// </summary>
        private static CompareMouseTouchHitInfo _CompareMouseTouchHitInfo = new CompareMouseTouchHitInfo();

        protected void AddEmitter(BaseEmitter em, string boneName, Vector3 offset)
        {
            AddAttachment(new OffsetEmitter(em, boneName, Matrix.CreateTranslation(offset)));
        }

        protected void AddEmitter(BaseEmitter em, Vector3 offset)
        {
            AddAttachment(new OffsetEmitter(em, offset));
        }

        protected void AddAttachment(Attachment att)
        {
            attachments.Add(att);
            if (!att.Update(Movement.LocalMatrix, ReScale))
            {
                // Already over age, remove.
                attachments.Remove(att);
            }
        }

        protected void RemoveAttachment(Attachment att)
        {
            RemoveAttachment(attachments.IndexOf(att));
        }

        protected void RemoveAttachment(int idx)
        {
            Attachment att = attachments[idx];
            // A hard disable now that it's dead.
            att.Disable(true);
            att.Release();
            attachments.Remove(att);
        }

        protected virtual void UpdateAttachments()
        {
            CheckGlowLuz();
            Matrix local = Movement.LocalMatrix;
            glowEmitter.Position = WorldGlowPosition;
            glowEmitter.Scale = ReScale;
            for (int idx = attachments.Count - 1; idx >= 0; --idx)
            {
                Attachment oe = attachments[idx];

                if (!oe.Update(local, ReScale))
                {
                    RemoveAttachment(idx);
                }
            }
        }

        public void ResetAttachmentPositions()
        {
            for (int i = 0; i < attachments.Count; ++i)
            {
                Attachment oe = attachments[i];
                oe.ResetPosition(Movement.Position);
            }
        }

        /// We want a glowLuz iff GlowLights && Glowing.
        /// We need to keep checking, because we might not get one
        /// <summary>
        /// Check that we have a glowLuz if we need one and we don't if we don't.
        /// </summary>
        protected void CheckGlowLuz()
        {
            if ((GlowLights > 0) && Glowing)
            {
                /// Want one
                if (glowLuz == null)
                {
                    string glowBone = XmlActorParams.GlowPosition.Bone;
                    glowLuz = LuzMgr.GlowLuz.Acquire(glowBone, Matrix.Identity, GlowLights);
                    if (glowLuz != null)
                    {
                        AddAttachment(glowLuz);
                        glowLuz.HookupBone(Animators);
                        glowLuz.Tint = new Vector3(
                            glowEmitter.Color.X,
                            glowEmitter.Color.Y,
                            glowEmitter.Color.Z);
                    }
                }
            }
            else
            {
                /// Don't want one
                ReleaseGlowLuz();
            }
        }

        protected void ReleaseGlowLuz()
        {
            if (glowLuz != null)
            {
                /// Remove will release
                RemoveAttachment(glowLuz);
                glowLuz = null;
            }
        }

        public virtual void EnableEmitters()
        {
            if (!Invisible)
            {
                for (int i = 0; i < attachments.Count; ++i)
                {
                    OffsetEmitter oe = attachments[i] as OffsetEmitter;
                    if (oe != null)
                    {
                        oe.Enable(true);
                        oe.ResetPosition(Movement.Position);
                    }
                }
            }
        }

        public virtual void DisableEmitters()
        {
            for (int i = 0; i < attachments.Count; ++i)
            {
                OffsetEmitter oe = attachments[i] as OffsetEmitter;
                if (oe != null)
                {
                    oe.Disable(false);
                }
            }
        }

        protected virtual void EnableAttachments(bool start)
        {
            glowEmitter.AddToManager();
            if ((XmlActorParams.GlowPosition.Bone != "") && (Animators != null))
            {
                glowBoneIndex = Animators.Sample.BoneIndex(XmlActorParams.GlowPosition.Bone);
            }
            else
            {
                glowBoneIndex = -1;
            }
            Chassis.Activate();
            for (int i = 0; i < attachments.Count; ++i)
            {
                Attachment oe = attachments[i];
                OffsetEmitter offsetEmitter = oe as OffsetEmitter;
                if ((offsetEmitter == null) || !offsetEmitter.AlwaysEmitting)
                {
                    oe.Enable(start);
                }
                oe.HookupBone(Animators);
            }
        }

        protected virtual void DisableAttachments(bool hard)
        {
            ReleaseGlowLuz();

            TwitchEmissivity(false);

            glowing = false;
            glowEmitter.Active = false;
            glowEmitter.RemoveFromManager();
            for (int i = 0; i < attachments.Count; ++i)
            {
                Attachment oe = attachments[i];
                oe.Disable(hard);
            }
            DisposeEmote(hard);

            if (glowAura != null)
            {
                glowAura.Die();
                glowAura = null;
            }

            Chassis.Deactivate();

            UpdateAttachments();
        }

        protected virtual void AttachmentsFirstPerson(bool on)
        {
            for (int i = 0; i < attachments.Count; ++i)
            {
                Attachment oe = attachments[i];
                oe.FirstPerson(on);
            }
        }

        protected void AddSmoke(XmlSmokeSource xmlSource)
        {
            if (xmlSource.AutoGen)
            {
                SharedSmokeSource source = new SharedSmokeSource(InGame.inGame.ParticleSystemManager);

                if (DistortionManager.PartyEnabled || (xmlSource.Usage != BaseEmitter.Use.Distort))
                {
                    source.Active = xmlSource.Active;
                    source.Emitting = xmlSource.Emitting;
                    source.Color = xmlSource.Color;
                    source.PositionJitter = xmlSource.PositionJitter;
                    source.StartRadius = xmlSource.StartRadius;
                    source.EndRadius = xmlSource.EndRadius;
                    source.StartAlpha = xmlSource.StartAlpha;
                    source.MinLifetime = xmlSource.MinLifetime;
                    source.MaxLifetime = xmlSource.MaxLifetime;
                    source.MaxRotationRate = xmlSource.MaxRotationRate;
                    source.EmissionRate = xmlSource.EmissionRate;
                    source.Velocity = xmlSource.Velocity;
                    source.Acceleration = xmlSource.Acceleration;
                    source.Usage = xmlSource.Usage;

                    AddEmitter(source, xmlSource.Bone, xmlSource.Offset);
                }
            }
        }

        protected virtual void StartSound()
        {
            //Debug.Print("starting " + uniqueNum.ToString());

#if NETFX_CORE
            // TODO (scoy) Restore this when MG does audio.
            return;
#else

            // Set up looping sounds.
            if (!Mute)
            {
                if (IdleSoundName != null)
                {
                    if (idleCue == null)
                    {
                        idleCue = BokuGame.Audio.GetCue(IdleSoundName, this);
                        idleCue.Spatial = true;
                    }
                    idleCue.SetVolume(1.0f);
                    idleCue.Play();
                }
                if (MoveSoundName != null)
                {
                    if (moveCue == null)
                    {
                        moveCue = BokuGame.Audio.GetCue(MoveSoundName, this);
                        moveCue.Spatial = true;
                    }
                    // If we have an idle sound, turn down the moving sound.
                    if (idleCue != null)
                    {
                        moveCue.SetVolume(0.0f);
                    }
                    moveCue.Play();
                }
            }
#endif
        }

        protected virtual void StopSound()
        {
            //Debug.Print("stopping " + uniqueNum.ToString());

            // Halt looping sounds.
            if (idleCue != null)
            {
                idleCue.StopImmediate();
                idleCue.Reset();
                idleCue = null;
            }
            if (moveCue != null)
            {
                moveCue.StopImmediate();
                moveCue.Reset();
                moveCue = null;
            }
            Foley.PlayEmoNone(this);
        }

        protected virtual void DisposeEmote(bool hard)
        {
            if (emoteEmitter != null)
            {
                emoteEmitter.Dying = true;
                emoteEmitter.Emitting = false;
                if (hard)
                {
                    emoteEmitter.Active = false;
                    emoteEmitter.RemoveFromManager();
                }
                emoteEmitter = null;
            }
            this.classification.emitter = ExpressModifier.Emitters.None;
        }
        protected virtual bool MakeEmote(ExpressModifier.Emitters emitter)
        {
            if (this is NullActor)
            {
                return false;
            }

            if (emitter != this.classification.emitter)
            {
                DisposeEmote(false);
                switch (emitter)
                {
                    case ExpressModifier.Emitters.Hearts:
                        emoteEmitter = new HeartEmitter(InGame.inGame.ParticleSystemManager);
                        Foley.PlayEmoLove(this);
                        break;
                    case ExpressModifier.Emitters.Flowers:
                        emoteEmitter = new FlowerEmitter(InGame.inGame.ParticleSystemManager);
                        Foley.PlayEmoFlowers(this);
                        break;
                    case ExpressModifier.Emitters.Stars:
                        emoteEmitter = new StarEmitter(InGame.inGame.ParticleSystemManager);
                        Foley.PlayEmoStars(this);
                        break;
                    case ExpressModifier.Emitters.Swears:
                        emoteEmitter = new SwearEmitter(InGame.inGame.ParticleSystemManager);
                        Foley.PlayEmoSwearing(this);
                        break;
                    case ExpressModifier.Emitters.None:
                        Foley.PlayEmoNone(this);
                        break;
                    default:
                        break;
                }
                if (emoteEmitter != null)
                {
                    emoteEmitter.AddToManager();
                    emoteEmitter.Active = true;

                    OffsetEmitter oe = new OffsetEmitter(emoteEmitter, GlowPosition);
                    oe.TossOnDeath = true;
                    oe.SoftDeath = true;
                    oe.AlwaysEmitting = false;
                    AddAttachment(oe);

                }
                this.classification.emitter = emitter;
            }
            return true;
        }


        // TODO (scoy) Try and move more of this into the AnimationSet class.

        /// <summary>
        /// The animation system was designed to self-update, but we update 
        /// the animations manually so that we have more control over 
        /// suspending them, etc.  If you want animations to run "on their 
        /// own," then pass a valid "Game" object into the constructor (we 
        /// pass null from our constructor, above.)
        /// 
        /// Note: This function may be overridden to start new bot-specific
        /// animations.
        /// </summary>
        public virtual void UpdateAnimations()
        {
            if (_animators != null)
            {
                // If animation is turned off and the "open" state has changed...
                bool once = false;
                if (BokuSettings.Settings.Animation == false && open != animationSet.IsOpen)
                {
                    open = animationSet.IsOpen;

                    animationSet.SetOpenCloseToEnd(animationSet.IsOpen);
                    once = true;
                }

                // If animation is turned off we only need to do the following 
                // whenever "once" is reset which currently only happens when
                // the open/close state changes.
                if (BokuSettings.Settings.Animation || once)
                {
                    animationSet.Update();

                    once = false;
                }
            }
        }   // end of GameActor UpdateAnimations()

        /// <summary>
        /// Used by the UpdateAnimations method (above) to allow the noAnimations option to work.
        /// </summary>
        private bool open = true;

        public void SetAnimators(AnimatorList animList)
        {
            _animators = animList;
        }
        public void SetAnimation(BaseController controller)
        {
            if (_animators != null)
            {
                if (_currentAnim != controller)
                {
                    _currentAnim = controller;
                    _animators.ApplyController(_currentAnim);
                    //ApplyController(_animators, _currentAnim);
                }
            }
        }

        /// <summary>
        /// Face interface
        /// </summary>
        /// 

        public bool DirectGaze(Vector3 target)
        {
            return DirectGaze(target, Face.DefaultGazeDuration);
        }
        public bool DirectGaze(Vector3 target, float duration)
        {
            if (face != null)
            {
                face.DirectGaze(target, duration);
                return true;
            }
            return false;
        }
        public bool DisplayEmotionalState(Face.FaceState facial, float duration)
        {
            if (face != null)
            {
                face.DisplayEmotionalState(facial, duration);
                return true;
            }
            return false;
        }
        public void UpdateFace()
        {
            if (face != null)
            {
                face.Update(Movement);
            }
        }
        protected void OnFaceChanged(Face.FaceState newFace)
        {
            this.classification.expression = newFace;
        }

        protected bool MakeFace(Face.FaceState facial)
        {
            if (face != null)
            {
                if (this.face.EmotionalState != facial)
                {
                    this.face.DisplayEmotionalState(facial, float.MaxValue);

                    switch (facial)
                    {
                        case Face.FaceState.Mad:
                            Foley.PlayEmoAngry(this);
                            break;
                        case Face.FaceState.Crazy:
                            Foley.PlayEmoCrazy(this);
                            break;
                        case Face.FaceState.Happy:
                            Foley.PlayEmoHappy(this);
                            break;
                        case Face.FaceState.Sad:
                            Foley.PlayEmoSad(this);
                            break;
                    }
                }
                return true;
            }
            return false;
        }

        private bool FindExpressions(BaseAction effector, ref Face.FaceState facial, ref ExpressModifier.Emitters emitter)
        {
            Face.FaceState tmpFacial = facial;

            if (effector.Reflex.ModifierParams.HasFacial)
                tmpFacial = effector.Reflex.ModifierParams.Facial;

            if (tmpFacial != Face.FaceState.None)
                facial = tmpFacial;

            if (effector.Reflex.ModifierParams.HasExpressEmitter)
                emitter = effector.Reflex.ModifierParams.ExpressEmitter;

            return (facial != Face.FaceState.None && facial != Face.FaceState.NotApplicable) || (emitter != ExpressModifier.Emitters.NotApplicable);
        }

        public bool DoExpress(BaseAction effector)
        {
            Face.FaceState facial = Face.FaceState.None;
            ExpressModifier.Emitters emitter = ExpressModifier.Emitters.NotApplicable;

            if (FindExpressions(effector, ref facial, ref emitter))
            {
                MakeFace(facial);
                MakeEmote(emitter);
            }

            return true;
        }

        public bool DoGlow(BaseAction effector)
        {
            bool supports = true;

            if (this is NullActor)
            {
                return false;
            }

            // Turning glow on or off?
            bool glowOn = !effector.Reflex.HasModifier("modifier.glowoff");

            if (glowOn)
            {
                // Default to glowing the current bot's color.
                Classification.Colors color = ClassColor;
                if (effector.Reflex.ModifierParams.HasColor)
                {
                    color = effector.Reflex.ModifierParams.Color;
                    if (color == Classification.Colors.NotApplicable)
                    {
                        color = Classification.RandomColor();
                    }
                }

                // Only play sound if changing color.
                if (classification.GlowColor != color)
                    Foley.PlayGlow(this);

                glowEmitter.Color = Classification.ColorVector4(color);
                MakeGlow(glowing = true);

                classification.GlowColor = color;
            }
            else
            {
                MakeGlow(glowing = false);
                classification.GlowColor = Classification.Colors.None;
            }

            return supports;
        }

        public bool DoReset(BaseAction effector, bool quiet)
        {
            bool playResetSound = false;

            ResetModifier.Resets resetFlags = effector.Reflex.ModifierParams.Reset;

            bool isKnockedOutOrSquashed = ((CurrentState == GameThing.State.Dead || CurrentState == GameThing.State.Squashed) && PendingState != GameThing.State.Active);

            if (!isKnockedOutOrSquashed && (resetFlags & ResetModifier.Resets.Expression) != 0)
            {
                KillEmote();
                playResetSound = true;
            }
            if (!isKnockedOutOrSquashed && (resetFlags & ResetModifier.Resets.Glow) != 0)
            {
                KillGlow();
                playResetSound = true;
            }
            if ((resetFlags & ResetModifier.Resets.Health) != 0)
            {
                if (isKnockedOutOrSquashed)
                {
                    KillEmote();
                    Activate();
                }

                ResetHitPoints();
                playResetSound = true;
            }
            if (!quiet && playResetSound)
            {
                Foley.PlayReset(this);
            }

            if ((resetFlags & ResetModifier.Resets.Score) != 0)
            {
                Scoreboard.ResetScore(effector.Reflex.ModifierParams.ScoreBucket, ScoreResetFlags.Score);
            }

            // Reset world last, since it immediately terminates all brain execution.
            if ((resetFlags & ResetModifier.Resets.World) != 0)
            {
                InGame.inGame.ResetSim(preserveScores: true, removeCreatablesFromScene: true, keepPersistentScores: false);
                // Resetting the Sim causes everything to be reloaded so we need to
                // re-apply the inlining.  In this case we're 100% we're going back
                // into run mode.
                InGame.ApplyInlining();
            }

            return true;
        }

        /// <summary>
        /// This should be called when in the actors DoDirectObjectVerb if it supports being given things
        /// </summary>
        /// <param name="givenThing"></param>
        /// <returns></returns>
        public bool QueueGivenObject(GameThing givenThing, bool quiet)
        {
            quiet |= this.Mute;

            // Another actor wants to give us something.  Queue it in or "given set"
            // so that we can process it according to our "got" reflexes upon next
            // brain update.
            AddGiven(givenThing);

            // Play the sound here rather than at the point of giving since giving may be refused.
            if (!quiet)
            {
                Foley.PlayGive(this);
            }

            return true;
        }

        public override bool DoDropObject(GameThing dropThing, bool quiet)
        {
            quiet |= this.Mute;

            // If the thing to be dropped is in the set of things being given to
            // us in this evaluation frame, remove it from the set so we won't
            // accept it when we process given things at the end of this frame.
            if (dropThing != null)
                givenSet.Remove(dropThing);

            if (dropThing == null)
                dropThing = ThingBeingHeldByThisActor;

            bool supported = false;
            for (; ; )
            {
                if (dropThing == null)
                    break;

                if (dropThing != ThingBeingHeldByThisActor)
                    break;

                animationSet.StartDropAnimation();

                DynamicPropChassis dynChassis = ThingBeingHeldByThisActor.Chassis as DynamicPropChassis;
                if (dynChassis != null)
                {
                    // For things using the DynamicPropChassis actually throw them down and let them bounce around.
                    dynChassis.Moving = true;
                    // Init the object's velocity to match the bot that's carying it with a little randomness thrown in.
                    Random rnd = BokuGame.bokuGame.rnd;
                    ThingBeingHeldByThisActor.Movement.Velocity = movement.Velocity;
                    // Add in some randomness.
#if !NO_RANDOM
                    ThingBeingHeldByThisActor.Movement.Velocity += 0.1f * new Vector3((float)(rnd.NextDouble() - rnd.NextDouble()), (float)(rnd.NextDouble() - rnd.NextDouble()), (float)(-rnd.NextDouble()));
#endif
                }
                else
                {
                    GameActor heldActor = ThingBeingHeldByThisActor as GameActor;

                    /// Drop it outside our combined collision radius, and nudge its
                    /// velocity so that we're moving apart.
                    Vector3 locationAir = heldActor.Movement.Position;
                    Vector3 locationGround = locationAir;
                    Vector3 meToIt = locationGround - Movement.Position;
                    meToIt.Z = 0;
                    const float kClearance = 0.1f;
                    float combinedRadius = CollisionRadius + heldActor.CollisionRadius + kClearance;
                    if (meToIt.LengthSquared() > 0)
                    {
                        meToIt.Normalize();
                        meToIt *= combinedRadius;
                        locationGround = Movement.Position + meToIt;
                    }
                    else
                    {
                        meToIt.Z = combinedRadius;
                        locationGround.Z += combinedRadius;
                    }
                    locationGround.Z = ThingBeingHeldByThisActor.GetPreferredAltitude();

                    Debug.Assert(heldActor != null);

                    heldActor.Movement.Velocity = Movement.Velocity;
                    heldActor.Movement.Velocity += meToIt * (1.0f / combinedRadius);

                    locationAir = new Vector3(locationGround.X, locationGround.Y, locationAir.Z);

                    heldActor.Movement.Position = locationAir;
                }

                Foley.PlayDrop(ThingBeingHeldByThisActor);

                // disconnect it
                ThingBeingHeldByThisActor.ActorHoldingThis = null;
                ThingBeingHeldByThisActor = null;

                if (!quiet)
                {
                    Foley.PlayDrop(this);
                }

                dropThing.DoDropped(quiet);

                supported = true;

                break;
            }
            return supported;
        }

        private static SensorTargetSet _grabSet = new SensorTargetSet();
        public bool DoGrabObject(GameThing grabThing, bool ignoreDistance, bool quiet)
        {
            // ActuatorUpdate will set the direct object to ourselves if the "it" pronoun wasn't
            // explicitly set on the reflex by the user. We choose to interpret this as "grab
            // something nearby".
            if (grabThing == this)
                grabThing = null;

            quiet |= this.Mute;

            bool supported = false;

            // Held objects may not also hold objects.
            if (ActorHoldingThis != null)
                return false;

            // HACK:  The below auto target with no subject is hacked in for usability
            //
            // It does no sensor properties checks,
            // we can grab things directly behind us etc. This is really
            // broken as it is ignoring any properties that the actor may
            // want to be applied
            //
            // Why is this a hack?

            // If we don't have the optional subject, lets find a nearby one.
            // Ignore anything that we're already holding. 
            if (grabThing == null)
            {
                grabThing = ClosestActor(TotalGrabRange, true);
            }

            // Cast as Actor so we can access all the properties we need.
            GameActor grabActor = grabThing as GameActor;

            // Infinite loop to make it easy to break out.
            for (; ; )
            {
                if (grabActor == null)
                    break;

                // Don't grab things that are dying.
                if (grabActor.PendingState == State.Inactive)
                    break;

                // Don't grab what we're already holding on to.
                if (grabActor == ThingBeingHeldByThisActor)
                    break;

                // Someone already holding it?
                if (grabActor.ActorHoldingThis != null)
                    break;

                // Don't grab the thing holding us.
                if (ActorHoldingThis == grabActor)
                    break;

                // If thing isn't collectable then don't grab it.
                if ((grabActor.Classification.physicality & Classification.Physicalities.Collectable) == 0)
                    break;

                // If it's marked immobile, don't grab.
                if (grabActor.SharedParameters.immobile)
                    break;

                // Check if object is in grabbing range.
                if (!ignoreDistance)
                {
                    Vector3 delta = WorldCollisionCenter - grabActor.WorldCollisionCenter;
                    float maxDist = TotalGrabRange(grabActor);
                    if (delta.Length() > maxDist)
                        break;
                }

                // drop what we might already be carrying
                DoDropObject(null, quiet);

                // connect it
                ThingBeingHeldByThisActor = grabActor;
                ThingBeingHeldByThisActor.ActorHoldingThis = this;

                animationSet.StartGrabbingAnimation();

                if (!quiet)
                {
                    Foley.PlayGrab(this);
                }

                grabActor.DoGrabbed(quiet);

                supported = true;
                break;
            }
            return supported;
        }

        public bool DoGiveObject(BaseAction effector, bool quiet)
        {
            quiet |= this.Mute;

            bool supported = false;
            GameThing giveTarget = effector.GameThing;
            GameActor giveActor = giveTarget as GameActor;

            // HACK:  The below auto target with no subject is hacked in for usability
            //
            // It does no sensor properties checks,
            // we can give to things directly behind us etc. This is really
            // broken as it is ignoring any properties that the actor may
            // want to be applied
            //
            // Q: Why is this a hack?

            GameThing giveThing = ThingBeingHeldByThisActor;
            if (giveThing != null)
            {

                // If we don't have the optional subject, lets find a nearby one.
                // Since we're looking to give the object we are currently holding
                // we should ignore it in our search for a target.
                if (giveActor == null)
                {
                    giveActor = ClosestActor(TotalGrabRange, true);
                }

                if (giveActor != null)
                {
                    // Check if target is in grabbing (giving) range
                    Vector3 delta = giveActor.Movement.Position - Movement.Position;
                    if (delta.Length() < TotalGrabRange(ThingBeingHeldByThisActor))
                    {
                        // queue a potential "give"
                        giveActor.QueueGivenObject(ThingBeingHeldByThisActor, quiet);

                        // Drop it
                        DoDropObject(null, quiet);

                        animationSet.StartGiveAnimation();

                        supported = true;

                    } // end if
                }
            }
            return supported;
        }

        public bool DoSwitchTask(BaseAction effector)
        {
            if (!effector.Reflex.ModifierParams.HasTaskId)
                return false;

            int taskId = (int)effector.Reflex.ModifierParams.TaskId;

            bool supported = false;
            int countTasks = this.Brain.TaskCount;

            if (taskId >= 0 && taskId < countTasks)
            {
                supported = true;
                this.Brain.ActiveTaskId = taskId;
            }

            return supported;
        }

        private const float maxHoverChassisFloatHeightOffset = 10.0f;
        private const float minHoverChassisFloatHeightOffset = 1.0f;

        public enum OpenCloseModes
        {
            NotApplicable,
            Open,     // Opened/up
            Closed,   // Closed/down
        }

        protected OpenCloseModes openCloseMode;
        private float? maxSpeedBeforeClosed;

        public bool DoOpen(GameActor directObject, BaseAction effector, bool quiet)
        {
            if (XmlActorParams.SpecialActions.OpenCloseData != null)
            {
                if (openCloseMode != OpenCloseModes.Open)
                {
                    openCloseMode = OpenCloseModes.Open;
                    animationSet.StartOpenAnimation();

                    // Set the invulnerable state to match the Tweakable Settings.
                    Invulnerable = directObject.TweakInvulnerable;

                    // If we froze this actor on closing, restore movement if needed.
                    if (XmlActorParams.SpecialActions.OpenCloseData.Freeze)
                    {
                        Chassis.AllowBrainMovement = true;
                        if (maxSpeedBeforeClosed != null)
                        {
                            Chassis.MaxSpeed = maxSpeedBeforeClosed.Value;
                        }
                    }

                    if (XmlActorParams.SpecialActions.OpenCloseData.HeightOffset != null)
                    {
                        // Restore saved height offset.
                        HeightOffset = PrevHeightOffset;
                    }
                }
                return true;
            }
            return false;
        }   // end of DoOpen()

        public bool DoClose(GameActor directObject, BaseAction effector, bool quiet)
        {
            if (XmlActorParams.SpecialActions.OpenCloseData != null)
            {
                if (openCloseMode != OpenCloseModes.Closed)
                {
                    openCloseMode = OpenCloseModes.Closed;
                    animationSet.StartCloseAnimation();

                    // If closing makes this actor invulnerable, set that state.
                    if (XmlActorParams.SpecialActions.OpenCloseData.Invulnerable)
                    {
                        Invulnerable = true;
                    }

                    // If closing makes this actor freeze, capture info needed to restore.
                    if (XmlActorParams.SpecialActions.OpenCloseData.Freeze)
                    {
                        Chassis.AllowBrainMovement = false;
                        maxSpeedBeforeClosed = Chassis.MaxSpeed;
                        Chassis.MaxSpeed = 0.0f;
                    }

                    if (XmlActorParams.SpecialActions.OpenCloseData.HeightOffset != null)
                    {
                        // Save current height offset so we can restore it upon opening.
                        PrevHeightOffset = HeightOffset;
                        HeightOffset = 0;
                    }
                }
                return true;
            }
            return false;
        }   // end of DoClose()

        public bool DoJump(GameActor directObject, BaseAction effector, bool quiet)
        {
            if (XmlActorParams.SpecialActions.JumpData != null)
            {
                Chassis.Jump = true;
                Chassis.EffectiveJumpStrength = GetLoft(effector, Chassis.DefaultJumpStrength);

                return true;
            }
            return false;
        }

        private const float ReScaleDefaultTransitionTime = 2.0f;
        public bool DoReScale(BaseAction effector, bool instant)
        {
            // Should we be scaling Me or It?
            GameActor target = effector.Reflex.ModifierParams.Pronoun == PronounModifier.Pronouns.It ? effector.GameThing as GameActor : this;
            // Not sure if this is ever needed.
            if (target == null)
            {
                target = this;
            }

            if (effector.Reflex.Data.ReScaleEnabled)
            {
                if (instant)
                {
                    target.ReScale = effector.Reflex.Data.ReScale;
                    target.ReScaling = false;

                    CollSys.RefreshMoverCollision(target);

                    //keep it moving until we finish
                    if (target.Chassis != null)
                    {
                        target.Chassis.Moving = true;
                    }
                }
                else 
                {
                    if (target.ReScaling)
                    {
                        //already transitioning to this scale?  if so, let it finish
                        if (effector.Reflex.Data.ReScale == target.ReScaleToScale)
                        {
                            return true;
                        }
                        StopScalingTransition(target);
                    }
                    target.ReScaling = true;

                    //transition over time
                    Vector3 speed = new Vector3(1.0f, 0.0f, 0.0f);
                    effector.Reflex.ModifyHeading(target, Modifier.ReferenceFrames.All, ref speed);

                    //faster => speed will be larger, but in this case, faster => smaller transition time.  therefore, divide by speed
                    float transitionTime = ReScaleDefaultTransitionTime / speed.Length();
                    target.ReScaleFromScale = target.ReScale;
                    target.ReScaleToScale = effector.Reflex.Data.ReScale;

                    //since we're moving from 0.0 to 1.0, use the input as the lerp value (even though it won't technically be linear)
                    TwitchManager.Set<float> scaleLerp = delegate(float value, Object param)
                    {
                        //apply a scale at some point between the two values
                        target.ReScale = target.ReScaleFromScale * (1.0f - value) + target.ReScaleToScale * value;

                        CollSys.RefreshMoverCollision(target);

                        //keep it moving until we finish
                        if (target.Chassis != null)
                        {
                            target.Chassis.Moving = true;
                        }
                    };

                    TwitchCompleteEvent scaleLerpComplete = delegate(Object param)
                    {
                        //finished - set the final value and indicate we're done
                        target.ReScale = target.ReScaleToScale;
                        target.ReScaling = false;
                    };

                    TwitchCompleteEvent scaleLerpTerminated = delegate(Object param) {};

                    target.ReScaleTwitchID = TwitchManager.CreateTwitch<float>(0.0f, 1.0f, scaleLerp, transitionTime, TwitchCurve.Shape.EaseIn, null, scaleLerpComplete, scaleLerpTerminated, true);

                }
            }

            return true;
        }

        private void StopScalingTransition(GameActor target)
        {
            if (target.ReScaling && target.ReScaleTwitchID >= 0)
            {
                TwitchManager.KillTwitch<float>(target.ReScaleTwitchID);
                target.ReScaling = false;
                target.ReScaleTwitchID = -1;
            }
        }

        public bool DoHoldDistance(BaseAction effector, bool instant)
        {
            if (LocalParameters._holdDistance != effector.Reflex.Data.HoldDistance)
            {
                //HoldDistance = effector.reflex.Data.HoldDistance;

                LocalParameters._holdDistance = effector.Reflex.Data.HoldDistance;
                float twitchTime = instant ? 0.01f : 2.0f;
                TwitchManager.Set<float> set = delegate(float value, Object param) { HoldDistance = value; };
                TwitchManager.CreateTwitch<float>(HoldDistance, LocalParameters._holdDistance, set, twitchTime, TwitchCurve.Shape.EaseInOut);

                // TODO (scoy) Is this needed?
                // Keep it moving until we finish
                if (this.Chassis != null)
                {
                    this.Chassis.Moving = true;
                }
            }

            return true;
        }   // end of DoHoldDistance()

        private const float WorldSkyDefaultTransitionTime = 2.0f;
        public bool DoWorldSkyChange(BaseAction effector, bool instant)
        {
            if (effector.Reflex.Data.WorldSkyChangeEnabled)
            {
                if (instant)
                {
                    Terrain.Current.RunTimeSkyIndex = effector.Reflex.Data.WorldSkyChangeIndex;
                }
                else
                {
                    //transition over time
                    Vector3 speed = new Vector3(1.0f, 0.0f, 0.0f);
                    effector.Reflex.ModifyHeading(this, Modifier.ReferenceFrames.All, ref speed);

                    //faster => speed will be larger, but in this case, faster => smaller transition time.  therefore, divide by speed
                    float transitionTime = WorldSkyDefaultTransitionTime / speed.Length();

                    Terrain.Current.TransitionToSky(effector.Reflex.Data.WorldSkyChangeIndex, transitionTime);
                }
            }

            return true;
        }

        private const float LightRigDefaultTransitionTime = 2.0f;
        public bool DoLightRigTransition(BaseAction effector, bool instant)
        {
            if (effector.Reflex.Data.WorldLightChangeEnabled && effector.Reflex.Data.WorldLightChangeIndex >= 0 && effector.Reflex.Data.WorldLightChangeIndex < ShaderGlobals.RigNames.Length)
            {
                if (instant)
                {
                    //instantaneous light change
                    InGame.inGame.RunTimeLightRig = ShaderGlobals.RigNames[effector.Reflex.Data.WorldLightChangeIndex];
                }
                else
                {
                    //transition over time
                    Vector3 speed = new Vector3(1.0f, 0.0f, 0.0f);
                    effector.Reflex.ModifyHeading(this, Modifier.ReferenceFrames.All, ref speed);

                    //faster => speed will be larger, but in this case, faster => smaller transition time.  therefore, divide by speed
                    float transitionTime = LightRigDefaultTransitionTime / speed.Length();

                    InGame.inGame.TransitionToLightRig(ShaderGlobals.RigNames[effector.Reflex.Data.WorldLightChangeIndex], transitionTime);
                }
            }

            return true;
        }

        /// <summary>
        /// Change this characters max hitpoints due to the programming tile being activated.
        /// </summary>
        /// <param name="effector"></param>
        /// <returns></returns>
        public bool DoMaxHitpointsChange(BaseAction effector)
        {
            // Change max hitpoints but keep the same current level (except when needed to clamp to new max).
            int curHitPoints = HitPoints;
            MaxHitPoints = effector.Reflex.Data.MaxHitpoints;
            HitPoints = curHitPoints;       // Forces clamp to max.

            return true;
        }   // end of DoMaxHitpointsChange()

        public bool DoBlipDamageChange(BaseAction effector)
        {
            BlipDamage = effector.Reflex.Data.ParamInt;
            return true;
        }   // end of DoBlipDamageChange()

        public bool DoMissileDamageChange(BaseAction effector)
        {
            MissileDamage = effector.Reflex.Data.ParamInt;
            return true;
        }   // end of DoMissileDamageChange()

        public bool DoBlipReloadTimeChange(BaseAction effector)
        {
            BlipReloadTime = effector.Reflex.Data.ParamFloat;
            return true;
        }   // end of DoBlipRelaodTimeChange()

        public bool DoBlipRangeChange(BaseAction effector)
        {
            BlipRange = effector.Reflex.Data.ParamFloat;
            return true;
        }   // end of DoBlipRangeChange()

        public bool DoMissileReloadTimeChange(BaseAction effector)
        {
            MissileReloadTime = effector.Reflex.Data.ParamFloat;
            return true;
        }   // end of DoMissileRelaodTimeChange()

        public bool DoMissileRangeChange(BaseAction effector)
        {
            MissileRange = effector.Reflex.Data.ParamFloat;
            return true;
        }   // end of DoMissileRangeChange()

        public bool DoCloseByRangeChange(BaseAction effector)
        {
            NearByDistance = effector.Reflex.Data.ParamFloat;
            return true;
        }   // end of DoCloseByRangeChange()

        public bool DoFarAwayRangeChange(BaseAction effector)
        {
            FarAwayDistance = effector.Reflex.Data.ParamFloat;
            return true;
        }   // end of DoFarAwayRangeChange()

        public bool DoHearingRangeChange(BaseAction effector)
        {
            Hearing = effector.Reflex.Data.ParamFloat;
            return true;
        }   // end of DoHearingRangeChange()

        /// <summary>
        /// Create an appropriate actor from either the creatableId or modifier specified.
        /// </summary>
        /// <param name="item"></param>
        /// <param name="creatableId"></param>
        /// <param name="color"></param>
        /// <param name="startPos"></param>
        /// <returns></returns>
        protected GameActor ActorFromModifier(
            ObjectModifier.ModifierObjects item,
            Guid creatableId,
            Classification.Colors color,
            Vector3 startPos,
            float rotation)
        {
            GameActor prop = null;
            if (creatableId != Guid.Empty)
            {
                GameActor creatable = InGame.inGame.GetCreatable(creatableId);
                if (creatable != null)
                {
                    /// This is where creatables get spawned during gameplay.
                    prop = InGame.inGame.CloneInPlace(creatable, startPos, rotation) as GameActor;
                }
            }
            else
            {
                switch (item)
                {
                    case ObjectModifier.ModifierObjects.InkJet:
                        {
                            prop = InGame.inGame.AddActor(ActorFactory.Create(ActorManager.GetActor("InkJet")), startPos, rotation);
                        }
                        break;

                    case ObjectModifier.ModifierObjects.Rock:
                        {
                            prop = InGame.inGame.AddActor(ActorFactory.Create(ActorManager.GetActor("Rock")), startPos, rotation);
                        }
                        break;

                    case ObjectModifier.ModifierObjects.IceBerg:
                        {
                            prop = InGame.inGame.AddActor(ActorFactory.Create(ActorManager.GetActor("IceBerg")), startPos, rotation);
                        }
                        break;

                    case ObjectModifier.ModifierObjects.RockLowValue:
                        {
                            prop = InGame.inGame.AddActor(ActorFactory.Create(ActorManager.GetActor("RockLowValue")), startPos, rotation);
                        }
                        break;

                    case ObjectModifier.ModifierObjects.RockHighValue:
                        {
                            prop = InGame.inGame.AddActor(ActorFactory.Create(ActorManager.GetActor("RockHighValue")), startPos, rotation);
                        }
                        break;

                    case ObjectModifier.ModifierObjects.Fruit:
                        {
                            prop = InGame.inGame.AddActor(ActorFactory.Create(ActorManager.GetActor("Fruit")), startPos, rotation);
                        }
                        break;

                    case ObjectModifier.ModifierObjects.Star:
                        {
                            prop = InGame.inGame.AddActor(ActorFactory.Create(ActorManager.GetActor("Star")), startPos, rotation);
                        }
                        break;

                    case ObjectModifier.ModifierObjects.Coin:
                        {
                            prop = InGame.inGame.AddActor(ActorFactory.Create(ActorManager.GetActor("Coin")), startPos, rotation);
                        }
                        break;

                    case ObjectModifier.ModifierObjects.SoccerBall:
                        {
                            prop = InGame.inGame.AddActor(ActorFactory.Create(ActorManager.GetActor("SoccerBall")), startPos, rotation);
                        }
                        break;

                    case ObjectModifier.ModifierObjects.Heart:
                        {
                            prop = InGame.inGame.AddActor(ActorFactory.Create(ActorManager.GetActor("Heart")), startPos, rotation);
                        }
                        break;

                    case ObjectModifier.ModifierObjects.Ammo:
                        {
                            prop = InGame.inGame.AddActor(ActorFactory.Create(ActorManager.GetActor("Bullet")), startPos, rotation);
                        }
                        break;

                    default:
                        break;
                }
            }

            // Color it, if a color was given.
            if (prop != null && color != Classification.Colors.None)
            {
                if (color == Classification.Colors.NotApplicable)
                    color = Classification.RandomColor();

                prop.ClassColor = color;
            }

            return prop;
        }
        /// <summary>
        /// walk effector modifiers and set item and color if found
        /// </summary>
        /// <param name="effector"></param>
        /// <param name="item"></param>
        /// <param name="creatableId"></param>
        /// <param name="color"></param>
        protected void GetItemAndColor(
            BaseAction effector,
            ref ObjectModifier.ModifierObjects item,
            ref Guid creatableId,
            ref Classification.Colors color)
        {
            if (effector.Reflex.ModifierParams.HasItem)
                item = effector.Reflex.ModifierParams.Item;

            if (effector.Reflex.ModifierParams.HasColor)
                color = effector.Reflex.ModifierParams.Color;

            if (effector.Reflex.ModifierParams.HasCreatableId)
                creatableId = effector.Reflex.ModifierParams.CreatableId;
        }
        /// <summary>
        /// Figure out what to launch, either something to create or something we're holding.
        /// 
        /// This may return null if the resource limit is reached and we're not
        /// holding anything.
        /// </summary>
        /// <param name="effector"></param>
        /// <param name="startPos"></param>
        /// <returns></returns>
        protected GameActor ThingToLaunch(BaseAction effector, Vector3 startPos)
        {
            GameActor thingToLaunch = null;
            ObjectModifier.ModifierObjects item = ObjectModifier.ModifierObjects.None;
            Classification.Colors color = Classification.Colors.None;
            Guid creatableId = Guid.Empty;

            GetItemAndColor(effector, ref item, ref creatableId, ref color);

            thingToLaunch = ActorFromModifier(item, creatableId, color, startPos, InGame.inGame.Camera.Rotation);

            /// If we don't have anything to launch, try launching whatever (if anything)
            /// we're holding.
            if (thingToLaunch == null)
            {
                thingToLaunch = ThingBeingHeldByThisActor as GameActor;
            }

            return thingToLaunch;
        }

        /// <summary>
        /// Function to figure the total range when looking for a closest actor.
        /// For example, the grab range is the sum this.grabRange + other.grabRange.
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        protected delegate float TotalRange(GameThing other);

        /// <summary>
        /// Returns the closest GameActor in the scene within the specified range.
        /// 
        /// If I'm being held, this should never return the actor holding me.
        /// </summary>
        /// <param name="RangeCheck">Delegate to get max range to check.</param>
        /// <param name="ignoreHeldObjects">Should we ignore things we are holding?  
        /// Depends of usage.  For instance is makes sense to ignore the apple I'm
        /// holding if I'm trying to find someone to give it to.  On the other hand
        /// it does make sense to count it if I'm looking for something nearby to eat.</param>
        /// <returns>null if none found</returns>

        protected GameActor ClosestActor(TotalRange RangeCheck, bool ignoreHeldObjects)
        {
            float closestDistSq = float.MaxValue;
            GameActor closestActor = null;
            int cnt = Brain.GameThings.Count;
            for (int i = 0; i < cnt; ++i)
            {
                GameActor actor = Brain.GameThings[i] as GameActor;

                if ((actor != null)                                             // Actor must not be null.
                    && (actor != this)                                          // Actor must not be self.
                    && (actor != ActorHoldingThis)                              // Actor must not be holding me.
                    && (actor.ActorHoldingThis != this || !ignoreHeldObjects)   // Actor is not being held by me or we're not ignoring held objects.
                    )
                {
                    Vector3 toActor = -ToClosest(actor.WorldCollisionCenter);

                    float distSq = toActor.LengthSquared();

                    float totalRange = RangeCheck(actor);

                    if ((distSq <= totalRange * totalRange) && (distSq < closestDistSq))
                    {
                        closestActor = actor;
                        closestDistSq = distSq;
                    }
                }
            }
            return closestActor;
        }

        /// <summary>
        /// Return the closest game actor in the scene within specified range.
        /// </summary>
        /// <param name="startPos"></param>
        /// <param name="maxDist"></param>
        /// <returns></returns>
        protected List<GameActor> RevealableActors(float range)
        {
            var resultActors = new List<GameActor>();

            int cnt = Brain.GameThings.Count;
            for (int i = 0; i < cnt; ++i)
            {
                GameActor actor = Brain.GameThings[i] as GameActor;
                if ((actor != null)
                    && (actor != this)
                    && ((actor.ActorHoldingThis == null) || (actor.ActorHoldingThis == this))
                    && (actor.XmlActorParams.CanReveal) 
                    && (!actor.Revealed))
                {
                    //TODO: verify it's an object that can be revealed and an object that hasn't yet been revealed
                    Vector3 toActor = -ToClosest(actor.WorldCollisionCenter);

                    float distSq = toActor.LengthSquared();

                    float totalRange = range;

                    if ((distSq <= totalRange * totalRange))
                    {
                        resultActors.Add(actor);
                    }
                }
            }
            return resultActors;
        }

        /// <summary>
        /// Return the closest rock game actor in the scene within specified range.
        /// </summary>
        /// <param name="startPos"></param>
        /// <param name="maxDist"></param>
        /// <returns></returns>
        protected GameActor ClosestRockActor(TotalRange RangeCheck)
        {
            float closestDistSq = float.MaxValue;
            GameActor closestActor = null;
            int cnt = Brain.GameThings.Count;
            for (int i = 0; i < cnt; ++i)
            {
                GameActor actor = Brain.GameThings[i] as GameActor;
                if ((actor != null)
                    && (actor != this)
                    && ((actor.ActorHoldingThis == null) || (actor.ActorHoldingThis == this))
                    && (IsRoverRock(actor)
                    && actor.CanBeSensed())
                    )
                {
                    Vector3 toActor = -ToClosest(actor.WorldCollisionCenter);

                    float distSq = toActor.LengthSquared();

                    float totalRange = RangeCheck(actor);

                    if ((distSq <= totalRange * totalRange) && (distSq < closestDistSq))
                    {
                        closestActor = actor;
                        closestDistSq = distSq;
                    }
                }
            }
            return closestActor;
        }

        /// <summary>
        /// Return the closest rock game actor in the scene within specified range.
        /// </summary>
        /// <returns></returns>
        protected GameActor ClosestRockActor(TotalRange RangeCheck, bool IsBeam)
        {
            float straightestFOV = float.MinValue;
            float closestHPDistSq = float.MaxValue;
            float closestLPDistSq = float.MaxValue;
            float closestDistSq = float.MaxValue;
            GameActor closestFOVActor = null;
            // closest high priority actor
            GameActor closestHPActor = null;
            // closest low priority actor
            GameActor closestLPActor = null;
            GameActor closestActor = null;
            int cnt = Brain.GameThings.Count;
            string rockNameHP = IsBeam ? "rockLowValue" : "rockHighValue";
            string rockNameLP = IsBeam ? "rockHighValue" : "rockLowValue";

            for (int i = 0; i < cnt; ++i)
            {
                GameActor actor = Brain.GameThings[i] as GameActor;
                if ((actor != null)
                    && (actor != this)
                    && ((actor.ActorHoldingThis == null) || (actor.ActorHoldingThis == this))
                    && (IsRoverRock(actor)
                    && actor.CanBeSensed())
                    )
                {
                    Vector3 toActor = -ToClosest(actor.WorldCollisionCenter);

                    float distSq = toActor.LengthSquared();

                    float totalRange = RangeCheck(actor);

                    if (distSq <= totalRange * totalRange)
                    {
                        // if we are with in range
                        // Check if this rock is in the FOV of the rover
                        Vector3 Facing = Movement.Facing;
                        Facing.Normalize();
                        toActor.Normalize();
                        float dotProduct = Vector3.Dot(Facing, toActor);
                        if (dotProduct >= roverFOV && dotProduct > straightestFOV)
                        {
                            closestFOVActor = actor;
                            straightestFOV = dotProduct;
                        }
                        // check if this is a rock name 1
                        else if (actor.Classification.name == rockNameHP && distSq < closestHPDistSq)
                        {
                            closestHPActor = actor;
                            closestHPDistSq = distSq;                                 
                        }
                        // check for rock name 2
                        else if (actor.Classification.name == rockNameLP && distSq < closestLPDistSq)
                        {
                            closestLPActor = actor;
                            closestLPDistSq = distSq;
                        }
                        // else just find the nearest rock
                        else if (distSq < closestDistSq)
                        {
                            closestActor = actor;
                            closestDistSq = distSq;
                        }
                    }
                }
            }

            // Check the rock in our FOV if it matches the rock type we want
            if (closestFOVActor != null && closestFOVActor.Classification.name == rockNameHP)
                return closestFOVActor;
            // else find the closest high priority actor
            else if (closestHPActor != null)
                return closestHPActor;
            // return the low priority actor in view
            else if (closestFOVActor != null && closestFOVActor.Classification.name == rockNameLP)
                return closestFOVActor;
            // return the closest low priority actor 
            else if (closestLPActor != null)
                return closestLPActor;
            // return the rock that's in view
            else if (closestFOVActor != null)
                return closestFOVActor;
            // return the closest rock
            else
                return closestActor;
        }

        /// <summary>
        /// Detects if the given actor is a rock that the Rover
        /// can apply its beam and inspection capabilities on.
        /// Originally this was just the specific rocks but has
        /// been changed to include all rocks.
        /// </summary>
        /// <param name="InActor"></param>
        /// <returns></returns>
        protected bool IsRoverRock(GameActor actor)
        {
            bool result = false;

            result = actor.Classification.name.StartsWith("rock");

            return result;
        }
        /// <summary>
        /// Give the effector a shot at picking out a loft.
        /// </summary>
        /// <param name="effector"></param>
        /// <param name="dir"></param>
        /// <returns></returns>
        protected Vector3 GetLoft(BaseAction effector, Vector3 dir)
        {
            float loft = GetLoft(effector, dir.Z);
            dir.Z = loft;
            return dir;
        }

        protected float GetLoft(BaseAction effector, float val)
        {
            if (effector.Reflex.ModifierParams.HasLoft)
            {
                val *= effector.Reflex.ModifierParams.Loft * effector.Reflex.ModifierParams.Loft;
            }

            return val;
        }

        protected Vector3 GetStrength(BaseAction effector, Vector3 dir)
        {
            if (effector.Reflex.ModifierParams.HasStrength)
            {
                dir *= effector.Reflex.ModifierParams.Strength;
            }

            return dir;
        }

        protected float GetStrength(BaseAction effector, float val)
        {
            if (effector.Reflex.ModifierParams.HasStrength)
            {
                val *= effector.Reflex.ModifierParams.Strength;
            }

            return val;
        }

        /// <summary>
        /// Return the nearby actors in front of the object that can be pushed by the actor.
        /// </summary>
        /// <returns></returns>
        protected void GetPushableActors(ref List<GameActor> pushableActors)
        {
            pushableActors.Clear();

            Vector3 actorPos = Movement.Position;
            Vector3 actorFacing = Movement.Facing;
            Vector3 actorRight = Vector3.Cross(actorFacing, Vector3.UnitZ);
            actorRight.Normalize();

            for (int i = 0; i < Brain.GameThings.Count; i++)
            {
                GameActor target = Brain.GameThings[i] as GameActor;

                if ((target != null)
                    && (target != this)
                    && ((target.ActorHoldingThis == null) || (target.ActorHoldingThis == this))
                    && target.Classification.physicality != Classification.Physicalities.NotApplicable
                    && target.CanBeSensed()
                    )
                {
                    // Vector from us to the actor we're testing.
                    Vector3 toTarget = target.Movement.Position - actorPos;

                    // First, test if the forward range is good.
                    float forward = Vector3.Dot(actorFacing, toTarget);
                    if (forward >= 0 && forward <= FinalPushRange)
                    {
                        // Now, test the width.
                        float offset = Vector3.Dot(actorRight, toTarget);

                        if (offset >= -FinalPushWidth && offset <= FinalPushWidth)
                        {
                            // Yep, it's in range.  Add to list.
                            pushableActors.Add(target);
                        }

                    }

                }   // end if valid target.

            }   // endof loop over all actors.
        }   // end of GetPushableActors()

        /// <summary>
        /// Push an object in certain direction in constant velocity.
        /// Push force is set as a external force on the target actor.
        /// </summary>
        /// <param name="directObject"></param>
        /// <param name="effector"></param>
        /// <returns></returns>
        protected virtual bool DoPush(BaseAction effector, GameThing directObject, bool quiet, bool reverse)
        {
            //
            // Always ignore direct object and use own way of finding pushable actors
            // TODO (scoy) Why?  Is this because we want fans to push everything in range
            // without being able to discriminate and only push blue Kodus?

            List<GameActor> pushThings = new List<GameActor>();
            GetPushableActors(ref pushThings);

            foreach (GameActor pushThing in pushThings)
            {
                // Actor we want to push.
                GameActor pushActor = pushThing as GameActor;

                // Assuming that PushStrength is the wind speed, we want to 
                // calculate a force relative to the ideal.  In other words, if the
                // target is already moving with the wind then the wind should have
                // no additional effect.  Moving against the wind should have a 
                // stronger effect.
                float relativeSpeed = Vector3.Dot(movement.Facing, pushActor.Movement.Velocity);

                // The /10.0f is a magic number here to make it match the previous code which also
                // had this in it.  No clue why they just didn't adjust the numbers in the fans.
                // Too late now.  Argh.
                float deltaSpeed = MathHelper.Clamp(PushStrength / 10.0f - relativeSpeed, 0, float.MaxValue);

                if (reverse)
                {
                    deltaSpeed *= -1;
                }

                // Pre-multiplying by the mass here. Yes, this is wrong but it makes the fans
                // push all objects the same amount which better matches the old behaviour.
                // I guess, thechnically we should also take into account the size of the
                // when calculating the force of the wind.
                pushActor.DesiredMovement.SetExternalForce(movement.Facing * deltaSpeed * pushActor.Mass);

            }   // end of loop over things to push.

            return pushThings.Count > 0;
        }   // end of DoPush()

        /// <summary>
        /// Create an object and send it flying at specific speed, azimuth, and pitch.
        /// If no object to create is specified, and we're holding something, throw
        /// that instead.
        /// </summary>
        /// <param name="directObject"></param>
        /// <param name="effector"></param>
        /// <returns></returns>
        protected bool DoLaunch(BaseAction effector, GameThing directObject, bool quiet)
        {
            // ActuatorUpdate will set the direct object to ourselves if the "it" pronoun wasn't
            // explicitly set on the reflex by the user. We choose to interpret this as "launch
            // something nearby".
            if (directObject == this)
                directObject = null;

            /// Find the thing to launch
            GameActor thingToLaunch = directObject as GameActor;
            GameActor foundThing = ThingToLaunch(effector, Vector3.Zero);

            if (foundThing != null)
            {
                thingToLaunch = foundThing;
            }

            if (thingToLaunch != null && thingToLaunch == directObject)
            {
                return DoKick(effector, directObject, quiet);
            }

            if (thingToLaunch != null)
            {
                if (thingToLaunch.ActorHoldingThis == this)
                {
                    // Stop holding the thing as we're about to launch it.
                    thingToLaunch.ActorHoldingThis = null;
                    ThingBeingHeldByThisActor = null;
                }

                Vector3 collisionCenter = WorldCollisionCenter;
                /// Find the azimuth to launch it
                Vector3 dir = GetShootDirection(collisionCenter, directObject, 1.0f, effector);

                /// Find where to launch it from
                Vector3 startPos = Vector3.Zero;
                CreatedItemOffset(ref startPos);

                Vector2 dirXY = new Vector2(dir.X, dir.Y);
                if (dirXY.LengthSquared() > 0.001f)
                {
                    // If direction has a horizontal component, set the default loft.
                    dir.Z = 0.75f;

                    // And rotate it into the firing direction.
                    startPos = new Vector3(
                        startPos.X * dir.X + startPos.Y * dir.X,
                        startPos.X * dir.Y + startPos.Y * dir.Y,
                        startPos.Z);
                }
                else
                {
                    // Point it up or down as appropriate.
                    startPos = new Vector3(0.0f, 0.0f, dir.Z);
                }
                float scale = (WorldCollisionRadius + thingToLaunch.WorldCollisionRadius) * 1.1f;
                startPos *= scale;

                startPos += collisionCenter;
                startPos += Movement.Velocity * (float)Time.GameTimeFrameSeconds;

                /// Apply the strength to launch it
                dir = GetStrength(effector, dir);

                /// Find the loft to launch it at
                dir = GetLoft(effector, dir);

                Vector3 worldVelocity = dir * KickStrength + Movement.Velocity;
                float terrainHeight = Terrain.GetTerrainAndPathHeight(startPos);
                float minHeight = terrainHeight + thingToLaunch.MinHeight * thingToLaunch.ReScale;
                // If we have land under us and launched thing is below the minHeight, move thing up to minHeight.
                if (terrainHeight > 0 && startPos.Z < minHeight)
                {
                    startPos.Z = minHeight;
                }

                thingToLaunch.Chassis.Gravity = BaseChassis.kGravity;
                thingToLaunch.Movement.LocalMatrix = Movement.LocalMatrix;
                thingToLaunch.Movement.Position = startPos;
                thingToLaunch.Movement.RotationZ = (float)Math.Atan2(thingToLaunch.Movement.Facing.Y, thingToLaunch.Movement.Facing.X);
                thingToLaunch.Movement.Velocity = worldVelocity;
                thingToLaunch.Chassis.Moving = true;
                thingToLaunch.Chassis.InsideGlassWalls = terrainHeight != 0;
                thingToLaunch.Activate();

                Foley.PlayKick(thingToLaunch);

                animationSet.StartKickAnimation();
            }

            directObject = thingToLaunch;

            return directObject != null;
        }

        public bool DoMakeItem(BaseAction effector, bool quiet)
        {
            quiet |= this.Mute;

            bool supports = true;

            Vector3 worldVelocity = Vector3.Zero;
            Vector3 localOffset = Vector3.Zero;
            CreatedItemVelocity(ref worldVelocity);
            CreatedItemOffset(ref localOffset);

            // Transform offset into world space.
            Vector3 offset = Vector3.TransformNormal(localOffset, Movement.LocalMatrix);

            // If we've got a mouse position, put the new item there.  Else use the
            // standard offset from the creating bot.
            Vector3 startPos;
            bool useCreatorPosition = false;    // Use the position of the creator for the position of the created thing.
            if (effector.Reflex.MousePosition != null)
            {
                // Use the mouse position.
                startPos = effector.Reflex.MousePosition.Value;
            }
            else if (effector.Reflex.MouseActor != null)
            {
                // Use the position of the actor we clicked on.
                startPos = effector.Reflex.MouseActor.Movement.Position;
            }
            else if (effector.Reflex.TouchPosition != null)
            {
                // Use the touch position
                startPos = effector.Reflex.TouchPosition.Value;
            }
            else if (effector.Reflex.TouchActor != null)
            {
                // Use the position of the actor we touched
                startPos = effector.Reflex.TouchActor.Movement.Position;
            }
            else
            {
                // Start with creator's position.
                startPos = Movement.Position + offset;
                useCreatorPosition = true;
            }

            // set defaults
            //
            ObjectModifier.ModifierObjects item = ObjectModifier.ModifierObjects.None;
            Classification.Colors color = Classification.Colors.None;
            Guid creatableId = Guid.Empty;

            GetItemAndColor(effector, ref item, ref creatableId, ref color);

            // Add a slight bit of randomness to the initial velocity so things look a little more natural.
            float worldSpeed = worldVelocity.Length();
#if !NO_RANDOM
            if (worldSpeed > 0)
            {
                Random rnd = BokuGame.bokuGame.rnd;
                worldVelocity += (worldSpeed * 0.1f) * new Vector3((float)(rnd.NextDouble() - rnd.NextDouble()), (float)(rnd.NextDouble() - rnd.NextDouble()), 0.0f);
            }
#endif

            //
            // Produce item requested.
            //

            GameActor prop = ActorFromModifier(item, creatableId, color, startPos, Movement.RotationZ);

            if (prop != null)
            {
                // Set starting position so collision bounds overlap.
                float dist = 0.5f * (WorldCollisionRadius + prop.WorldCollisionRadius);
                startPos += dist * Vector3.Normalize(offset);

                float minHeight = Terrain.GetTerrainAndPathHeight(movement.Position) + prop.MinHeight;

                startPos.Z = Math.Max(minHeight, startPos.Z);

                //prop.Movement.LocalMatrix = Movement.LocalMatrix;
                prop.Movement.Position = startPos;
                prop.Movement.RotationZ = (float)Math.Atan2(prop.Movement.Facing.Y, prop.Movement.Facing.X);
                prop.Movement.Velocity = worldVelocity;
                prop.Chassis.Moving = true;
                prop.Chassis.Gravity = BaseChassis.kGravity;
                prop.Activate();

                InGame.inGame.DistortionPulse(prop, false);

                GameActor propActor = prop as GameActor;
                if (propActor != null)
                {
                    propActor.InitMuzzleFlash(Classification.ColorVector4(color), 1.0f, 4.0f);
                }

                /// Trigger a bump event
                /// but only if the object is being created at 'this' position.
                if (useCreatorPosition)
                {
                    AddTouchSpoof(prop);
                }

                /// The prop has missed the PreCollisionTestUpdate for this frame, but
                /// is about to have PostCollisionTestUpdate called on it. That can
                /// cause problems, so we'll make up for it here.
                prop.Movement.SetPreviousPositionVelocity();
                // TODO (scoy) Why are we call PreCollisionTestUpdate() on this newly
                // created actor when Refresh hasn't even brouth it into the game yet?
                prop.Chassis.PreCollisionTestUpdate(propActor);

                Foley.PlayCreate(this);
            }
            else
            {
                supports = false;
            }

            return supports;
        }   // end of DoMakeItem()

        protected void InitMuzzleFlash(Vector4 color, float life, float scale)
        {
            LuzMgr.MuzzleLuz luz = LuzMgr.MuzzleLuz.Acquire(Matrix.Identity, life, scale);
            if (luz != null)
            {
                AddAttachment(luz);
                Vector3 tint = new Vector3(color.X, color.Y, color.Z);
                tint *= 0.75f;
                tint += new Vector3(0.25f);
                luz.Tint = tint;
            }
        }

        /// <summary>
        /// Get the vector from otherCenter (in world space) to our collision center.
        /// Accounts for the TouchCushion, by returning the vector from otherCenter to
        /// the segment running from our WorldCollisionCenter down a distance TouchCushion.
        /// </summary>
        /// <param name="otherCenter"></param>
        /// <returns></returns>
        public Vector3 ToClosest(Vector3 otherCenter)
        {
            Vector3 direction = WorldCollisionCenter - otherCenter;
            if (direction.Z >= TouchCushion)
            {
                direction.Z -= TouchCushion;
            }
            else if (direction.Z > 0.0f)
            {
                direction.Z = 0.0f;
            }
            return direction;
        }

        public bool DoEat(BaseAction effector, bool quiet)
        {
            quiet |= this.Mute;

            bool supports = false;

            GameThing eatThing = effector.GameThing;

            // HACK:  The below auto target with no subject is hacked in for usability
            //
            // It does no sensor properties checks,
            // we can eat things directly behind us etc. This is really
            // broken as it is ignoring any properties that the actor may
            // want to be applied
            //

            // If we don't have the optional subject, lets find a nearby one.
            // Allow finding the object we are holding.
            if (eatThing == null)
            {
                eatThing = ClosestActor(TotalGrabRange, false);
            }

            if (eatThing != null 
                && !(eatThing is NullActor)
                && (eatThing.ActorHoldingThis == null || eatThing.ActorHoldingThis == this))
            {
                // Check if object is in eating range.

                Vector3 eatThingPos = eatThing.WorldCollisionCenter;

                float totalGrabRange = TotalGrabRange(eatThing);

                Vector3 delta = ToClosest(eatThingPos);
                float distance = delta.Length();

                bool inRange = distance <= totalGrabRange;

                if (inRange || eatThing.ActorHoldingThis == this)
                {
                    if (eatThing.ActorHoldingThis == this)
                    {
                        ThingBeingHeldByThisActor = null;
                        eatThing.ActorHoldingThis = null;
                    }
                    else
                    {
                        // Trigger a "got" event.
                        eatenSet.Add(eatThing, Vector3.Zero, 0);
                    }

                    animationSet.StartEatAnimation();

                    if (!quiet)
                    {
                        if (CollisionRadius > 1.0)
                        {
                            Foley.PlayEatBig(this);
                        }
                        else
                        {
                            Foley.PlayEatSmall(this);
                        }
                    }

                    eatThing.DoEaten(quiet);

                    supports = true;
                }
            }

            return supports;
        }

        /// <summary>
        /// temporary effect
        /// If override, call this base first then modify state as Reset may do that also 
        /// </summary>
        /// <returns></returns>
        protected override bool DoStun(BaseAction effector, bool quiet, GameThing executor)
        {
            if (this is NullActor && executor != null && executor is GameActor)
            {
                return (executor as GameActor).DoStun(effector, quiet, executor);
            }

            quiet |= this.Mute;

            if (Brain.Stunned)
            {
                return false;
            }

            bool supports = base.DoStun(effector, quiet, executor);

            if (supports)
            {
                // Stun myself

                // Drop whatever we're carrying.
                DoDropObject(null, quiet);

                Brain.Stun();
                DisplayEmotionalState(Face.FaceState.Crazy, 2.0f);
            }

            return supports;
        }

        /// <summary>
        /// Permanent effect, leave a carcas
        /// If override, call this base first then modify state as Reset may do that also.
        /// 
        /// UPDATE : Death can now be reversed by healing.
        /// </summary>
        /// <returns></returns>
        protected override bool DoKill(BaseAction effector, bool quiet, GameThing executor)
        {
            if (this is NullActor && executor !=null && executor is GameActor)
            {
                return (executor as GameActor).DoKill(effector, quiet, executor);                
            }

            quiet |= this.Mute;

            bool supports = base.DoKill(effector, quiet, executor);

            if (supports)
            {
                // Kill myself
                HealthBarManager.UnregisterActor(this);

                DoLastBrainPass(State.Dead);

                StopSound();

                MoveToDeadPose();

                ResetState(revivingTheDead: false);

                // Turn off emitters.  This must come after the Reset() since the Reset enables the emitters.
                DisableAttachments(false);

                DisplayEmotionalState(Face.FaceState.Dead, float.MaxValue);

                // Reset the effector, then stop downstream code from processing it further.
                supports = effector != null && effector.Reflex != null;
            }

            return supports;
        }

        /// <summary>
        /// Flatten character and leave in "dead" state.
        /// Can be revivied via Healing.
        /// </summary>
        /// <returns></returns>
        protected override bool DoSquash(BaseAction effector, bool quiet, GameThing executor)
        {
            if (this is NullActor && executor != null && executor is GameActor)
            {
                return (executor as GameActor).DoSquash(effector, quiet, executor);
            }

            quiet |= this.Mute;

            bool supports = base.DoSquash(effector, quiet, executor);

            if (supports)
            {
                // Squash myself
                HealthBarManager.UnregisterActor(this);

                DoLastBrainPass(State.Squashed);

                // Allow to fall.
                Chassis.Moving = true;

                StopSound();

                MoveToDeadPose(randomTilt: false);

                ResetState(revivingTheDead: false);

                // Set scaling to apply the squash.
                SquashScale = SquashScaleTarget;

                // Turn off emitters.  This must come after the Reset() since the Reset enables the emitters.
                DisableAttachments(false);

                DisplayEmotionalState(Face.FaceState.Dead, float.MaxValue);

                // Reset the effector, then stop downstream code from processing it further.
                supports = effector != null && effector.Reflex != null;
            }

            return supports;
        }   // end of DoSquash()

        protected void MoveToDeadPose(bool randomTilt = true)
        {
            Matrix mat = Matrix.CreateRotationZ(Movement.RotationZ);
            float twitchTime = 0.3f + 0.1f * (float)BokuGame.bokuGame.rnd.NextDouble();

            if (randomTilt)
            {
                float roll = 0;
                // For some characters, have them roll belly up on death.
                if (this is SwimFish)
                {
                    roll = MathHelper.Pi * 0.8f;
                    if (BokuGame.bokuGame.rnd.NextDouble() < 0.5)
                    {
                        roll = -roll;
                    }
                    twitchTime *= 10.0f;    // Slow your roll...
                }
                // Note that YawPitchRoll only works if you are using the default XNA coords.  Since we're using Z up this looks strange.
                mat = Matrix.CreateFromYawPitchRoll(0.5f - (float)BokuGame.bokuGame.rnd.NextDouble(), roll + 0.5f - (float)BokuGame.bokuGame.rnd.NextDouble(), 0.5f - (float)BokuGame.bokuGame.rnd.NextDouble()) * mat;
            }
            Vector3 loc = Movement.Position;
            loc.Z = Terrain.GetTerrainAndPathHeight(Movement.Position) + MinHeight;
            mat.Translation = loc;

            //TwitchManager.Set<Matrix> set = delegate(Matrix value, Object param) { Movement.LocalMatrix = value; };
            //TwitchManager.CreateTwitch<Matrix>(Movement.LocalMatrix, mat, set, 0.3f, TwitchCurve.Shape.EaseIn);

            TwitchManager.Set<Vector3> rightSet = delegate(Vector3 value, Object param)
            {
                value.Normalize();
                Matrix m = Movement.LocalMatrix;
                m.Right = value;
                Movement.LocalMatrix = m;
            };
            TwitchManager.CreateTwitch<Vector3>(Movement.LocalMatrix.Right, mat.Right, rightSet, twitchTime, TwitchCurve.Shape.EaseInOut);
            TwitchManager.Set<Vector3> backwardSet = delegate(Vector3 value, Object param)
            {
                value.Normalize();
                Matrix m = Movement.LocalMatrix;
                m.Backward = value;
                Movement.LocalMatrix = m;
            };
            TwitchManager.CreateTwitch<Vector3>(Movement.LocalMatrix.Backward, mat.Backward, backwardSet, twitchTime, TwitchCurve.Shape.EaseInOut);
            TwitchManager.Set<Vector3> upSet = delegate(Vector3 value, Object param)
            {
                value.Normalize();
                Matrix m = Movement.LocalMatrix;
                m.Up = value;
                Movement.LocalMatrix = m;
            };
            TwitchManager.CreateTwitch<Vector3>(Movement.LocalMatrix.Up, mat.Up, upSet, twitchTime, TwitchCurve.Shape.EaseInOut);

            if (!Chassis.FixedPosition)
            {
                // For a moving chassis, convert it to a dynamic prop chassis,
                // so that it will tumble off and what not.
                DynamicPropChassis deadChassis = new DynamicPropChassis();
                deadChassis.Tumbles = false;
                deadChassis.Moving = true;

                deadChassis.Density = Chassis.Density;

                Mass = 10.0f;
                CoefficientOfRestitution = 0.5f;
                Friction = 0.5f;

                /// We want to hang onto our movement. Alternatively, we could
                /// set our new movement based on our old, but this will work.
                Movement oldMovement = Movement;
                Chassis = deadChassis;
                Movement = oldMovement;
            }
        }   // end of MoveToDeadPose()

        /// <summary>
        /// Permanent effect, remove from game
        /// If override, call this base first then modify state as Reset may do that also
        /// </summary>
        /// <returns></returns>
        protected override bool DoVaporize(BaseAction effector, bool quiet, GameThing executor)
        {
            quiet |= this.Mute;

            bool supports = base.DoVaporize(effector, quiet, executor);

            if (supports)
            {
                // Reset the effector, then stop downstream code from processing it further.
                supports = effector != null && effector.Reflex != null;
            }

            return supports;
        }

        /// <summary>
        /// Permanent effect, remove from game.
        /// No associated visual or audio effects.
        /// </summary>
        /// <returns></returns>
        protected override bool DoVanish(BaseAction effector, bool quiet, GameThing executor)
        {
            quiet |= this.Mute;

            bool supports = base.DoVanish(effector, quiet, executor);

            if (supports)
            {
                // Reset the effector, then stop downstream code from processing it further.
                supports = effector != null && effector.Reflex != null;
            }

            return supports;
        }

        protected void ThingKicked(GameThing kickThing)
        {
            lastKickedThing = kickThing;
            lastKickedAt = Time.GameTimeTotalSeconds;
        }

        protected bool CanKickThing(GameThing kickThing)
        {
            if (kickThing != lastKickedThing)
                return true;

            return (Time.GameTimeTotalSeconds - lastKickedAt) >= (1.0f / KickRate);
        }

        /// <summary>
        /// Find the sum of my kick range and the kick range of the thing I want to kick.
        /// Handles rescaling.
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        protected float TotalKickRange(GameThing other)
        {
            float total = kickRange * ReScale;
            GameActor otherActor = other as GameActor;
            if (otherActor != null)
            {
                total += otherActor.kickRange * otherActor.ReScale;
            }
            return total;
        }

        /// <summary>
        /// Find the sum of my grab range and the grab range of the thing I want to grab.
        /// Handles rescaling.
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public float TotalGrabRange(GameThing other)
        {
            float total = grabRange * ReScale;
            GameActor otherActor = other as GameActor;
            if (otherActor != null)
            {
                total += otherActor.grabRange * otherActor.ReScale;
            }
            return total;
        }


        /// <summary>
        /// Find the sum of my grab range and the grab range of the thing I want to grab.
        /// Handles rescaling.
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        protected float TotalBeamRange(GameThing other)
        {
            float total = beamRange * ReScale;
            GameActor otherActor = other as GameActor;
            if (otherActor != null)
            {
                total += otherActor.beamRange * otherActor.ReScale;
            }
            return total;
        }

        protected float TotalInspectRange(GameThing other)
        {
            float total = inspectRange * ReScale;
            GameActor otherActor = other as GameActor;
            if (otherActor != null)
            {
                total += otherActor.inspectRange * otherActor.ReScale;
            }
            return total;
        }

        public bool DoCamouflage(BaseAction effector, GameThing directObject, bool bCamouflage)
        {
            // Camo ourselves by default
            GameActor camoThis = directObject as GameActor;
            if (camoThis == null)
            {
                camoThis = this;
            }

            if (camoThis.Camouflaged != bCamouflage)
            {
                camoThis.Camouflaged = bCamouflage;

                if (bCamouflage)
                {
                    Foley.PlayCamouflageOn(this);
                }
                else
                {
                    Foley.PlayCamouflageOff(this);
                }
            }

             
            return true;                            
        }

        public bool DoKick(BaseAction effector, GameThing directObject, bool quiet)
        {
            // ActuatorUpdate will set the direct object to ourselves if the "it" pronoun wasn't
            // explicitly set on the reflex by the user. We choose to interpret this as "kick
            // something nearby".
            if (directObject == this)
                directObject = null;

            quiet |= this.Mute;

            bool supports = false;

            GameThing kickThing = directObject;

            if (kickThing != null)
                givenSet.Remove(kickThing);

            // HACK:  The below auto target with no subject is hacked in for usability
            //
            // It does no sensor properties checks,
            // we can kick things directly behind us etc. This is really
            // broken as it is ignoring any properties that the actor may
            // want to be applied
            //

            // If we don't have the optional subject, lets find a nearby one.
            // Allow kicking the thing we are holding.
            if (kickThing == null)
            {
                kickThing = ClosestActor(TotalKickRange, false);
            }

            // Check if object is kickable and kick it
            //
            if (kickThing != null &&
                    kickThing.Classification.physicality != Classification.Physicalities.NotApplicable &&
                    (kickThing.ActorHoldingThis == null || kickThing.ActorHoldingThis == this) &&
                    CanKickThing(kickThing))
            {
                // Check if object is in kick range.  Use 3d range.
                Vector3 toThing = -ToClosest(kickThing.WorldCollisionCenter);
                float distance = toThing.Length();
                float totalKickRange = TotalKickRange(kickThing);

                if (distance <= totalKickRange || kickThing.ActorHoldingThis == this)
                {
                    if (kickThing.ActorHoldingThis == this)
                    {
                        this.ThingBeingHeldByThisActor = null;
                        kickThing.ActorHoldingThis = null;
                    }

                    Vector3 kickDir = Vector3.Zero;

                    // See if the modifiers want to assign us a kick direction.
                    effector.Reflex.ModifyHeading(this, Modifier.ReferenceFrames.All, ref kickDir);

                    // If no direction given, use bot's facing direction.
                    if (kickDir == Vector3.Zero)
                    {
                        kickDir = kickThing.WorldCollisionCenter - WorldCollisionCenter;
                        GameActor kickActor = kickThing as GameActor;
                        if ((kickActor != null) && (kickActor.Domain == MovementDomain.Land))
                        {
                            kickDir.Z = 0;
                        }
                        kickDir.Normalize();
                    }

                    // If direction has a horizontal component, set the default loft.
                    if (kickDir.Z == 0)
                        kickDir.Z = 0.75f;

                    // Apply kick strength modifiers.
                    kickDir = GetStrength(effector, kickDir);
                    kickDir *= SharedParameters.kickStrength;

                    // Give it some loft
                    kickDir = GetLoft(effector, kickDir);


                    if ((kickThing.Classification.physicality & Classification.Physicalities.Static) != 0)
                    {
                        // If I've kicked a static object, apply the kick to me instead.
                        supports = DoKicked(kickThing, -kickDir, quiet);
                    }
                    else
                    {
                        // Apply the kick to the target.
                        supports = kickThing.DoKicked(this, kickDir, quiet);
                    }

                    animationSet.StartKickAnimation();
                }

                ThingKicked(kickThing);
            }
            return supports;
        }

        public void MissileLaunched()
        {
            // Make it average out firing maxMissilesActive times over every
            // missile lifetime period, ensuring a minimum reload time.
            /*
            float delay
                = (MissileReloadTime + (float)BokuGame.bokuGame.rnd.NextDouble() * 0.1f)
                + CruiseMissile.NumActive / (float)CruiseMissile.MaxActive
                * (2.0f * (float)BokuGame.bokuGame.rnd.NextDouble())
                * CruiseMissile.Throttle;

            nextMissileFire = Time.GameTimeTotalSeconds + delay;
             */

            nextMissileFire = Time.GameTimeTotalSeconds + MissileReloadTime;
            numMissilesInAir++;
        }

        public bool CanLaunchMissile()
        {
            if (Time.GameTimeTotalSeconds < nextMissileFire)
                return false;

            if (numMissilesInAir >= MissilesInAir)
                return false;

            return true;
        }

        /// <summary>
        /// Find the color projectile to fire.
        /// </summary>
        /// <param name="reflex"></param>
        /// <param name="defaultColor"></param>
        /// <returns></returns>
        protected Classification.Colors GetShootColor(Reflex reflex, Classification.Colors defaultColor)
        {
            Classification.Colors color = defaultColor;

            if (reflex.ModifierParams.HasColor)
            {
                color = reflex.ModifierParams.Color;
            }

            // Get our random color if needed.
            if (color == Classification.Colors.NotApplicable)
            {
                color = Classification.RandomColor();
            }

            return color;
        }
        /// <summary>
        /// Find the verb payload from this reflex.
        /// </summary>
        /// <param name="reflex"></param>
        /// <param name="defaultPayload"></param>
        /// <returns></returns>
        protected GameThing.Verbs GetVerbPayload(Reflex reflex, GameThing.Verbs defaultPayload)
        {
            GameThing.Verbs verbPayload = defaultPayload;

            if (reflex.ModifierParams.HasVerb)
                verbPayload = reflex.ModifierParams.Verb;

            return verbPayload;
        }

        /// <summary>
        /// Returns this actor's default damage amount for the specified type of damage.
        /// </summary>
        /// <param name="reflex"></param>
        /// <returns></returns>
        protected int GetShootDamage(Verbs damageType, int defaultDamage)
        {
            int damage;

            if ((damageType == Verbs.Vaporize) || (damageType == Verbs.Kill) || (damageType == Verbs.Vanish) || (damageType == Verbs.Squash))
            {
                // Pop, Boom, Vanish, and Squash do damage.
                damage = defaultDamage;
            }
            else
            {
                // Stun doesn't do damage
                damage = 0;
            }

            return damage;
        }

        /// <summary>
        /// Compute the behavior flags for firing this cruise missile.
        /// </summary>
        /// <param name="targetObject"></param>
        /// <param name="effector"></param>
        /// <returns></returns>
        protected MissileChassis.BehaviorFlags GetShootBehavior(GameThing targetObject, BaseAction effector)
        {
            MissileChassis.BehaviorFlags behavior = MissileChassis.BehaviorFlags.All;
            Vector3 gun = WorldCollisionCenter;

            // If launcher is below water, then disable stay above water behavior.
            float waterHeight = Terrain.GetWaterHeight(gun);
            if (gun.Z < waterHeight)
            {
                behavior &= ~MissileChassis.BehaviorFlags.StayAboveWater;
            }

            // If not shooting at an object, then disable homing behavior.
            if (targetObject == null || targetObject is NullActor)
            {
                behavior &= ~MissileChassis.BehaviorFlags.Homing;
            }

            /// !HasMissileBehavior is implemented as MissileBehaviorFlags == None,
            /// so reworking the following code to follow that logic.
            MissileChassis.BehaviorFlags behaviorFlags = effector.Reflex.ModifierParams.MissileBehavior;
            if (behaviorFlags != MissileChassis.BehaviorFlags.TerrainFollowing)
            {
                behavior &= ~MissileChassis.BehaviorFlags.TerrainFollowing;
            }

            if (effector.Reflex.ModifierParams.HasDirection)
            {
                // Shooting in a specific direction, so disable homing and clear target thing.
                behavior &= ~MissileChassis.BehaviorFlags.Homing;

                if (0 != (effector.Reflex.ModifierParams.Direction & Programming.Directions.Vertical))
                {
                    // If shooting up or down, don't follow terrain or worry about water.
                    behavior &= ~MissileChassis.BehaviorFlags.TerrainFollowing;
                    behavior &= ~MissileChassis.BehaviorFlags.StayAboveWater;
                }
            }

            return behavior;
        }

        protected Vector3 ModifyDirection(Vector3 direction, BaseAction effector)
        {
            // See if the modifiers want to give us a direction.
            Vector3 tempDirection = Vector3.Zero;
            effector.Reflex.ModifyHeading(this, Modifier.ReferenceFrames.All, ref tempDirection);
            if (tempDirection != Vector3.Zero)
                direction = tempDirection;

            // Check for gamepad stick modifying our initial rotation, unless a direction was explicitly provided.
            if (effector.Reflex.GetModifierCountByType(typeof(DirectionModifier)) == 0)
            {
                int gamePadCount = 0;
                for (int indexFilter = 0; indexFilter < effector.Reflex.Filters.Count; ++indexFilter)
                {
                    Filter filter = effector.Reflex.Filters[indexFilter] as Filter;
                    if (filter == null)
                        continue;

                    if (filter is GamePadStickFilter)
                    {
                        ++gamePadCount;
                        GamePadStickFilter filterGameStick = filter as GamePadStickFilter;
                        if (filterGameStick.stickPosition != Vector2.Zero)
                        {
                            Vector3 stickDir = new Vector3(filterGameStick.stickPosition, 0f);
                            float stickAngle = MyMath.ZRotationFromDirection(stickDir);
                            Vector3 cameraDir = InGame.inGame.Camera.ViewDir; cameraDir.Z = 0f;
                            float cameraAngle = MyMath.ZRotationFromDirection(cameraDir);
                            float initialRotation = cameraAngle + stickAngle - MathHelper.PiOver2;
                            direction.X = (float)Math.Cos(initialRotation);
                            direction.Y = (float)Math.Sin(initialRotation);
                        }
                    }
                }
            }

            direction.Normalize();
            return direction;
        }
        /// <summary>
        /// Compute the direction in which to shoot.
        /// </summary>
        /// <param name="targetObject"></param>
        /// <param name="effector"></param>
        /// <returns></returns>
        protected Vector3 GetShootDirection(Vector3 gun, GameThing targetObject, float speed, BaseAction effector)
        {
            // Get default direction to shoot.
            Vector3 initialDirection = Vector3.UnitZ;
            if (Chassis.HasFacingDirection)
            {
                // If we get crap, just punt and using the forward vector.
                // This may happen if we do something like using the arrow keys to shoot and pressing left and right simultaniously.
                if (effector.Value == Vector3.Zero)
                {
                    initialDirection = Movement.Facing;
                }
                else
                {
                    // We need to rotate the effector's direction for some reason.  I think the brain thinks that
                    // the bot's local coordinate system has the bot facing along the Y axis while in reality it's
                    // facing along the X axis.  The proper thing would be to go in and fix this but for now I'm
                    // just going to fix it up here.
                    // TODO Fix this correctly!
                    initialDirection = effector.Value;
                    float tmp = initialDirection.X;
                    initialDirection.X = initialDirection.Y;
                    initialDirection.Y = -tmp;
                    initialDirection = Vector3.TransformNormal(initialDirection, Movement.LocalMatrix);
                }

                if (initialDirection == Vector3.Zero || float.IsNaN(initialDirection.X) || float.IsNaN(initialDirection.Y) || float.IsNaN(initialDirection.Z))
                {
                    initialDirection = movement.Facing;
                }

            }
            else
            {
                initialDirection = Movement.Velocity;
                float length = initialDirection.Length();
                if (length < 0.001f)
                {
                    // As good as any random direction...
                    initialDirection = Movement.Facing;
                }
                else
                {
                    initialDirection.Normalize();
                }
            }

            // If shooting at an object, point gun at it.
            if (targetObject != null && !(targetObject is NullActor))
            {
                initialDirection = LeadTarget(
                    gun,
                    speed,
                    targetObject.WorldCollisionCenter,
                    targetObject.Movement.Velocity);
                initialDirection -= gun;
                initialDirection.Normalize();
            }
            else
            {
                // See if we're shooting at mouse or touch position.
                if (effector.Reflex.MousePosition != null && !effector.Reflex.hasGUIButtonFilter)
                {
                    initialDirection = effector.Reflex.MousePosition.Value - Movement.Position;
                    initialDirection.Normalize();
                }
                else if (effector.Reflex.TouchPosition != null)
                {
                    initialDirection = effector.Reflex.TouchPosition.Value - Movement.Position;
                    initialDirection.Normalize();
                }
                else if (effector.Reflex.targetSet.Nearest != null && effector.Reflex.targetSet.Nearest.GameThing is CursorThing)
                {
                    // Targetting cursor.
                    initialDirection = effector.Reflex.targetSet.Nearest.GameThing.Movement.Position - Movement.Position;
                    initialDirection.Normalize();
                }
            }

            initialDirection = ModifyDirection(initialDirection, effector);

            return initialDirection;
        }

        /// <summary>
        /// Helper function to perform dead-reckoning computation of where to aim to
        /// hit a moving target.
        /// </summary>
        /// <param name="from"></param>
        /// <param name="speed"></param>
        /// <param name="targetPos"></param>
        /// <param name="targetVel"></param>
        /// <returns></returns>
        public static Vector3 LeadTarget(Vector3 from, float speed, Vector3 targetPos, Vector3 targetVel)
        {
            /// First off, find when we can hit the target (if ever).
            /// Figure that from the distance between the shooter's position to
            /// the target position at time t will be speed * t (we know how far
            /// our missle traveled in time t, we just don't know what direction.
            double C = (targetPos - from).LengthSquared();
            double B = 2.0 * Vector3.Dot(targetPos - from, targetVel) / C;
            double A = (targetVel.LengthSquared() - speed * speed) / C;
            C = 1.0;

            if (Math.Abs(A) < 0.00001)
            {
                /// B*t + C = 0
                /// t = -C / B
                /// We know C is positive, so we only have an interesting root if B less than zero
                if (B < -0.00001)
                {
                    return targetPos + targetVel / (float)-B;
                }
                return targetPos + targetVel * 0.5f;
            }

            double discrim = B * B - 4.0f * A * C;
            if (discrim < 0)
            {
                /// No real roots, return targetPos + 1/2 second, good a place as any to aim.
                return targetPos + targetVel * 0.5f;
            }
            double sqrtDiscrim = Math.Sqrt(discrim);
            double tPlus = (-B + sqrtDiscrim) / (2.0f * A);
            double tMinus = (-B - sqrtDiscrim) / (2.0f * A);

            double tHit = 0;
            /// Between the two roots, we want the smallest one that is non-negative.
            if ((tPlus >= 0) && (tMinus >= 0))
            {
                tHit = Math.Min(tPlus, tMinus);
            }
            else if (tPlus >= 0)
            {
                tHit = tPlus;
            }
            else if (tMinus >= 0)
            {
                tHit = tMinus;
            }
            else
            {
                /// Only hits are in the past.
                return targetPos + targetVel * 0.5f;
            }

            /// Then figure out where the target will be then, and aim there.
            return targetPos + targetVel * (float)tHit;
        }

        /// <summary>
        /// Fire off a bleep (a lightweight projectile) if there are any available.
        /// </summary>
        /// <param name="targetObject"></param>
        /// <param name="effector"></param>
        /// <returns></returns>
        protected virtual bool DoBleep(BaseAction effector, GameThing targetObject, bool quiet)
        {
            quiet |= this.Mute;

            if (Time.GameTimeTotalSeconds < nextBleepFire)
                return false;
            if (ActiveBleeps.Bleeps.Count >= BlipsInAir)
                return false;

            Vector3 gun = WorldCollisionCenter;

            Classification.Colors color = GetShootColor(effector.Reflex, Classification.Colors.White);

            GameThing.Verbs verbPayload = GetVerbPayload(effector.Reflex, Verbs.Vaporize);

            int damage = GetShootDamage(verbPayload, SharedParameters.blipDamage);

            float speed = BlipSpeed;
            float range = BlipRange;

            Vector3 initialDirection = Movement.Facing;

            // Get the shooting direction from the target object (which may be NullActor).
            initialDirection = GetShootDirection(gun, targetObject, speed, effector);
            Vector3 targetPosition = gun + initialDirection * range;

            bool fired = SharedEmitterManager.Bleeps.Fire(this, targetPosition, color, verbPayload, damage);
            if (fired)
            {
                nextBleepFire = Time.GameTimeTotalSeconds + BlipReloadTime;

                if (!quiet)
                {
                    Foley.PlayRapidFire(this);
                }
            }
            return fired;
        }   // end of DoBleep()

        protected virtual bool DoMissile(BaseAction effector, GameThing targetObject, bool quiet)
        {
            quiet |= this.Mute;

            if (!CanLaunchMissile())
                return false;

            bool supports = false;

            Vector3 gun = WorldCollisionCenter;

            // init missile settings to default
            Classification.Colors color = GetShootColor(effector.Reflex, Classification.Colors.Grey);

            GameThing.Verbs verbPayload = GetVerbPayload(effector.Reflex, GameThing.Verbs.Vaporize);
            MissileChassis.BehaviorFlags behavior = GetShootBehavior(targetObject, effector);
            if ((behavior & MissileChassis.BehaviorFlags.Homing) == 0)
            {
                targetObject = null;
            }

            int damage = GetShootDamage(verbPayload, SharedParameters.missileDamage);

            Vector3 initialDirection = GetShootDirection(gun, targetObject, MissileSpeed, effector);
            float initialRotation = MyMath.ZRotationFromDirection(initialDirection);


            CruiseMissile missile = null;

            float lifetime = MissileRange / MissileSpeed;
            if (targetObject == null)
            {
                float shotRange = MissileRange;
                Vector3 target = gun + initialDirection * shotRange * 1.1f;

                // We're shooting in a direction.
                missile = CruiseMissile.Create(
                    gun,
                    target,
                    this,
                    initialRotation,
                    verbPayload,
                    damage,
                    behavior,
                    color,
                    MissileSpeed,
                    lifetime,
                    MissileTrails);
            }
            else
            {
                // We're shooting at a specific target.
                missile = CruiseMissile.Create(
                    gun,
                    targetObject,
                    this,
                    initialRotation,
                    verbPayload,
                    damage,
                    behavior,
                    color,
                    MissileSpeed,
                    lifetime,
                    MissileTrails);
            }

            animationSet.StartRocketAnimation();

            missile = (CruiseMissile)InGame.inGame.AddThing(missile, true);
            if (missile != null)
            {
                missile.Activate();
                BokuGame.objectListDirty = true;
                MissileLaunched();

                if (!quiet)
                {
                    Foley.PlayShoot(this);
                }

                supports = true;
            }

            return supports;
        }   // end of DoMissile()

        public bool DoShoot(BaseAction effector, GameThing targetObject, bool quiet, ProjectileModifier.ProjectileType defaultProjectileType)
        {
            if (targetObject == this)
                targetObject = null;

            quiet |= this.Mute;

            ProjectileModifier.ProjectileType projectileType = defaultProjectileType;

            // Check to see if a projectile type was specified.
            ProjectileModifier projectileMod = effector.Reflex.GetModifierByType(typeof(ProjectileModifier)) as ProjectileModifier;

            if (projectileMod != null)
            {
                projectileType = projectileMod.projectile;
            }

            bool result = false;

            if (projectileType == ProjectileModifier.ProjectileType.Blip)
            {
                result = DoBleep(effector, targetObject, quiet);
            }
            else
            {
                result = DoMissile(effector, targetObject, quiet);
            }

            if (result && AnimationSet != null)
            {
                AnimationSet.PlayCue(ShootSoundName);
            }

            return result;

        }   // end of GameActor DoShoot()

        // SGI_MOD - adding beam action
        public bool DoBeam(BaseAction effector, GameThing targetObject, bool quiet)
        {
            if (targetObject == this)
                targetObject = null;

            quiet |= this.Mute;

            ScienceActorMuted = quiet;

            //don't allow if not on ground
            if (Chassis is VehicleChassis && ((VehicleChassis)Chassis).OnGround == false)
                return false;
            if (Time.GameTimeTotalSeconds < nextBeamFire)
                return false;
            if (ActiveBeams.Beams.Count >= BeamsInAir)
                return false;
            if (ScienceActionPerformed)
                return false;

            bool inRange = false;

            // if we dont' have a target, find the closest rock in range
            if (targetObject == null)
            {
                targetObject = ClosestRockActor(TotalBeamRange, true);
                if (targetObject == null)
                    return false;
                else
                    inRange = true;
            }

            GameActor targetActor = targetObject as GameActor;
            if (targetActor.Beamed || targetActor.Inspected || !IsRoverRock(targetActor))
                return false;

            Vector3 gun = WorldCollisionCenter;

            Classification.Colors color = GetShootColor(effector.Reflex, Classification.Colors.White);

            float speed = BeamSpeed;
            float range = BeamDist;

            Vector3 initialDirection = Movement.Facing;

            // Get the shooting direction from the target object (which may be NullActor).
            initialDirection = GetShootDirection(gun, targetObject, speed, effector);
            Vector3 targetPosition = gun + initialDirection * range;

            // If this is not an auto picked target, check to see if in range
            if (!inRange)
            {
                float totalBeamRange = TotalBeamRange(targetObject);

                Vector3 delta = ToClosest(targetObject.WorldCollisionCenter);
                float distance = delta.Length();

                inRange = distance <= totalBeamRange;

                if (!inRange)
                {
                    return false;
                }
            }

            ScienceActorRotating = true;
            ScienceActionPerformed = true;
            CurrentScienceAction = ScienceAction.Beam;
            ScienceActionDelay = beamDelay;
            ScienceActionTimer = 0.0f;
            ScienceActorTarget = targetActor;
            ScienceActorDirection = initialDirection;
            BeamTargetPosition = targetPosition;
            BeamColor = color;
            BeamedSoundPlayed = false;

            return true;

        }   // end of GameActor DoBeam()

        private void FireBeamInternal(bool quiet)
        {
            if (Time.GameTimeTotalSeconds < nextBeamFire)
                return; 

            bool result = SharedEmitterManager.Beams.Fire(this, ScienceActorTarget, BeamTargetPosition, BeamColor);
            if (result)
            {
                nextBeamFire = Time.GameTimeTotalSeconds + BeamReloadTime;

                if (!quiet && !BeamedSoundPlayed)
                {
                    Foley.PlayBeam(this);
                    BeamedSoundPlayed = true;
                }
            }
        }


        // SGI_MOD - adding inspect action
        public bool DoInspect(BaseAction effector, GameThing targetObject, bool quiet)
        {
            if (targetObject == this)
                targetObject = null;

            quiet |= this.Mute;
            ScienceActorMuted = quiet;

            //don't allow if not on ground
            if (Chassis is VehicleChassis && ((VehicleChassis)Chassis).OnGround == false)
                return false; 
            if (ScienceActionPerformed)
                return false;

            bool inRange = false;

            // if we dont' have a target, find the closest rock in range
            if (targetObject == null)
            {
                targetObject = ClosestRockActor(TotalInspectRange, false);
                if (targetObject == null)
                    return false;
                else
                    inRange = true;
            }

            // todo check of targetObject is scannable
            GameActor targetActor = targetObject as GameActor;

            if (targetActor.Inspected || targetActor.Beamed || !IsRoverRock(targetActor))
                return false;

            if (!inRange)
            {
                float totalInspectRange = TotalInspectRange(targetObject);

                Vector3 delta = ToClosest(targetObject.WorldCollisionCenter);
                float distance = delta.Length();

                inRange = distance <= totalInspectRange;

                if (!inRange)
                    return false;

            }

            Vector3 actorCenter = Vector3.Transform(BoundingSphere.Center, Movement.LocalMatrix);
            Vector3 thingCenter = Vector3.Transform(targetObject.BoundingSphere.Center, targetObject.Movement.LocalMatrix);
            Vector3 direction = thingCenter - actorCenter;

            ScienceActorRotating = true;
            ScienceActionPerformed = true;
            CurrentScienceAction = ScienceAction.Inspect;
            ScienceActionDelay = inspectDelay;
            ScienceActionTimer = 0.0f;
            ScienceActorTarget = targetActor;
            ScienceActorDirection = direction;
            
            return true;

        }   // end of GameActor DoInspect()

        // SGI_MOD - adding scan action
        public bool DoScan(BaseAction effector, bool quiet)
        {
            quiet |= this.Mute;

            //don't allow if not on ground
            if (Chassis is VehicleChassis && ((VehicleChassis)Chassis).OnGround == false)
                return false;

            if (!ScienceActionPerformed)
            {
                ExplosionManager.CreateRoverScanEffect(WorldCollisionCenter, scanRange);
                ScienceActionPerformed = true;
                CurrentScienceAction = ScienceAction.Scan;
                ScienceActionDelay = scanDelay;
                ScienceActionTimer = 0.0f;
                Scanned = true;
                ScanningRing = 0;
                scanRings = scanRings <= 0 ? 1 : scanRings;
                ScanRevealDelay = scanDelay / scanRings;
                ScanRevealTimer = 0.0f;

                if (!quiet)
                {
                    Foley.PlayScan(this);
                }
            }

            return true;

        }   // end of GameActor DoScan()

        // SGI_MOD - adding picture action
        public bool DoPicture(BaseAction effector, bool quiet)
        {
            quiet |= this.Mute;

            BokuGame.PictureManager.SetScreenGrabEnabled(true, this, quiet);

            return true;
        }   // end of GameActor DoPicture()

        private GameActor SwapActor(GameActor targetActor, string ActorType)
        {
            StaticActor replaceActorType = ActorManager.GetActor(ActorType);
            GameActor replaceActor = ActorFactory.Create(replaceActorType);
            InGame.inGame.AddActor(replaceActor, targetActor.movement.Position, targetActor.movement.RotationZ);

            GameActor brainActor = replaceActor.brain.GameActor;
            replaceActor.brain = Brain.DeepCopy(targetActor.brain);
            replaceActor.brain.GameActor = brainActor;
            //make sure we keep running the same task
            replaceActor.brain.ActiveTaskId = targetActor.brain.ActiveTaskId;

            targetActor.Deactivate();
            replaceActor.Activate();

            return replaceActor;
        }

        /// <summary>
        /// Attempts to modify actor's hit points.  If hit points go to zero, perform
        /// killVerb on the actor.  Returns true if killVerb disabled the actor.
        /// </summary>
        /// <param name="amount"></param>
        /// <param name="killVerb"></param>
        /// <returns></returns>
        public override bool DoDamage(int amount, Verbs killVerb, bool doEffect, bool quiet, GameThing executor, out bool died)
        {
            died = false;

            if (Invulnerable && executor != this)
                return false;

            quiet |= this.Mute;

            if (PendingState == State.Inactive)
                return false;

            // Reviving a dead actor?
            if ((CurrentState == State.Dead || CurrentState == State.Squashed) && amount > 0)
            {
                ResetState(revivingTheDead: true);
                HitPoints = amount;
                PendingState = State.Active;
                // Restore Health Bar display if needed.
                HealthBarManager.RegisterActor(this);
            }
            else
            {
                HitPoints += amount;
            }

            if (!quiet)
            {
                if (amount < 0)
                {
                    Foley.PlayDamage(this);
                }
                else
                {
                    Foley.PlayHeal(this);
                }
            }

            // TODO (scoy) Is this right? What about < 0?
            if (HitPoints == 0)
            {
                ExecuteVerb(killVerb, null, null, quiet);
            }
            else if (doEffect)
            {
                Vector3 hitPos = WorldCollisionCenter;
                hitPos.Z -= WorldCollisionRadius;
                PlayDamageEffect(-amount, hitPos);
            }

            // TODO (scoy) Is this enough?  Or should we assert hitpoints <= 0
            died = PendingState == State.Inactive;

            return true;
        }

        /// <summary>
        /// Return created items velocity in WORLD SPACE
        /// </summary>
        /// <param name="velocity"></param>
        public virtual void CreatedItemVelocity(ref Vector3 velocity)
        {
            // Default to matching the actor's velocity plus a little forward.
            velocity = Movement.Velocity;
        }

        /// <summary>
        /// Return offset from me to created item in LOCAL SPACE
        /// </summary>
        /// <param name="offset"></param>
        public virtual void CreatedItemOffset(ref Vector3 offset)
        {
            if (XmlActorParams.CreatedItemOffset != null)
            {
                offset = XmlActorParams.CreatedItemOffset.Value;
            }
            else
            {
                // Default to just in front of the actor.
                // Throw in about 22.5 degrees randomness each way.
                Random rnd = BokuGame.bokuGame.rnd;
                offset = new Vector3(1.0f, 0.0f, 0.0f);
#if !NO_RANDOM
                offset.Y += (float)(rnd.NextDouble() * 2.0 - 1.0) * 0.38f;
#endif
            }
        }

        /// <summary>
        /// Turn off any emote emitter and facial expression on this actor.
        /// </summary>
        public void KillEmote()
        {
            if (Face != null)
                Face.DisplayEmotionalState(Face.FaceState.None);
            DisposeEmote(false);
        }

        /// <summary>
        /// Turns off any glowing effect active on this actor.
        /// </summary>
        public void KillGlow()
        {
            MakeGlow(glowing = false);
        }

        /// <summary>
        /// Resets hitpoints to the maximum value.
        /// </summary>
        public void ResetHitPoints()
        {
            currHitPoints = prevHitPoints = MaxHitPoints;
        }

        public void TwitchEmissivity(bool on)
        {
            bool changing = !InGame.inGame.IsTheFirstPerson(this)
                && (glowEmitter.Active != on);
            if (changing)
            {
                float targetValue = on ? GlowEmission : 0;

                // Create a twitch to change alpha in baseColor
                TwitchManager.Set<float> set = delegate(float value, Object param)
                {
                    glowEmissivity = value;
                };
                float twitchTime = 0.25f;
                TwitchManager.CreateTwitch<float>(
                    GlowEmissivity,
                    targetValue,
                    set,
                    twitchTime,
                    TwitchCurve.Shape.OvershootInOut);
            }
        }

        protected virtual void MakeGlow(bool on)
        {
            // May be null if this is NullActor.
            if (glowEmitter != null)
            {
                CheckGlowLuz();
                TwitchEmissivity(on);
                if (on)
                {
                    /// If we're first person, the effect is automatic from FPEGlow.
                    if (!InGame.inGame.IsTheFirstPerson(this))
                    {
                        if (!glowEmitter.Active)
                        {
                            glowEmitter.Active = true;
                            if (glowAura == null)
                            {
                                glowAura = InGame.inGame.MakeAura(this);
                            }
                        }
                    }

                    if (glowAura != null)
                    {
                        float scalar = 0.25f;
                        glowAura.TintAura(
                            glowEmitter.Color.X * scalar * GlowAmt,
                            glowEmitter.Color.Y * scalar * GlowAmt,
                            glowEmitter.Color.Z * scalar * GlowAmt);
                    }
                    if (glowLuz != null)
                    {
                        glowLuz.Tint = new Vector3(
                            glowEmitter.Color.X,
                            glowEmitter.Color.Y,
                            glowEmitter.Color.Z);
                    }
                }
                else
                {
                    glowEmitter.Active = false;
                    if (glowAura != null)
                    {
                        glowAura.Die();
                        glowAura = null;
                    }
                    if (glowLuz != null)
                    {
                        glowLuz.Tint = Vector3.Zero;
                    }
                }
            }
        }


        public override bool SetFirstPerson(bool on)
        {
            if (base.SetFirstPerson(on))
            {
                /*
                if (on)
                    Debug.Print("first person true  " + this.uniqueNum.ToString());
                else
                    Debug.Print("first person false " + this.uniqueNum.ToString());
                */

                /// Kill any glow effect.
                MakeGlow(false);
                /// Now create the right one.
                MakeGlow(glowing);

                // Dont' restart emitters if coming out of first person
                // when invisible or ghosted.
                if (on == true || !(Invisible || Ignored || Camouflaged))
                {
                    AttachmentsFirstPerson(on);
                }

                return true;
            }
            return false;
        }

        public Brain Brain
        {
            get { return this.brain; }
            set
            {
                this.brain = value;
                if (this.brain != null)
                {
                    this.brain.GameActor = this;
                }
            }
        }
        /// <summary>
        /// Pulled out of derived classes down to base.
        /// 
        /// </summary>
        public void InitBrainEmpty()
        {
            brain.InitEmpty();
        }

        protected void ShutdownSubsystems(bool revivingTheDead)
        {
            ThoughtBalloonManager.RemoveThoughts(this);
            SetFirstPerson(false);

            Brain.Wipe();

            DisableEmitters();
            DisableAttachments(true);

            KillEmote();

            StopSound();

            // If we're being held, get dropped unless we're undead.  Basically if revivingTheDead is
            // true then that means we were dead and now we're not.  This shouldn't make the actor
            // holding us drop us.  Although it may scare him a bit.
            if (!revivingTheDead)
            {
                if (ActorHoldingThis != null)
                {
                    ActorHoldingThis.DoDropObject(this, ActorHoldingThis.Mute);
                }
            }

            // Drop whatever we're holding
            DoDropObject(null, Mute);

            followPathState.Reset();
            givenSet.Clear();
            touchedSet.Clear();
            eatenSet.Clear();
            missileHitSet.Clear();
            shooterHitSet.Clear();
            testLines.Clear();
            beamedSet.Clear();
            inspectedSet.Clear();
            scannedSet.Clear();
            numMissilesInAir = 0;

            ResetHitPoints();
        }   // end of ShutdownSubsystems()

        public void StartupSubsystems()
        {
            Brain.Reset(0, true, true);

            EnableEmitters();
            EnableAttachments(true);

            numMissilesInAir = 0;

            StartSound();
        }

        /// <summary>
        /// Called to have the actor state to be reset, but not movement.
        /// 
        /// Recentely added the ignoreState flag.  Not sure if there's really
        /// any case where this shouldn't be true.
        /// </summary>
        /// <param name="revivingTheDead">Is this reset due to a dead actor being healed back to life?</param>
        /// <param name="ignoreCurrentState">Ignore the current state of the bot.  Reset regardless.</param>
        public virtual void ResetState(bool revivingTheDead = false, bool ignoreCurrentState = false)
        {
            ShutdownSubsystems(revivingTheDead);

            if (PendingState == State.Active || ignoreCurrentState)
            {
                StartupSubsystems();
            }
        }

        /// <summary>
        /// Called to have the actor reset 
        /// </summary>
        public virtual void Reset()
        {
            ResetState(revivingTheDead: false);
            Movement.Velocity = Vector3.Zero;
        }

        public void OnMissileExpire()
        {
        }
        public void OnMissileHit(GameThing targetHit, Vector3 hitPos, Verbs defaultPayload, int defaultDamage)
        {
            OnProjectileHit(targetHit, hitPos, defaultPayload, defaultDamage);
        }
        public void OnBlipExpire()
        {
        }
        public void OnBlipHit(GameThing targetHit, Vector3 hitPos, Verbs defaultPayload, int defaultDamage)
        {
            OnProjectileHit(targetHit, hitPos, defaultPayload, defaultDamage);
        }
        private void OnProjectileHit(GameThing targetHit, Vector3 hitPos, Verbs defaultPayload, int defaultDamage)
        {
            if (targetHit != null)
            {
                MissileHitTargetParam param = MissileHitTargetParam.Alloc();
                param.defaultDamage = defaultDamage;
                param.defaultPayload = defaultPayload;
                param.hitStrength = defaultDamage;
                param.hitPosition = hitPos;


                SensorTarget target = SensorTargetSpares.Alloc();
                // We don't need a direction and range so just set the target.
                // TODO (scoy) This kind of imples that this should be refactored.
                target.Init(targetHit, direction: Vector3.Zero, range: 0.0f);
                target.Tag = param;

                // This actor (the launcher) may have died since launching the missile.
                // So, adding to this missileHitSet is at best useless and may be very
                // wrong if the actor has already been recycled.  We can test for the 
                // inactive case but there's still the odd case of being recycled.
                // TODO (scoy) Maybe missile hit stuff should be in the missile rather
                // than the launcher?  Will this fix the problem?
                if (CurrentState != State.Inactive)
                {
                    missileHitSet.AddOrFree(target);
                }

                // If target wasn't nulled out...
                if (target.GameThing != null)
                {
                    // Also add ref to shooter set so bot knows it was hit.  It would probably be
                    // nicer if we put the shooter into this list but then that breaks the "me"
                    // filter test.  Deeper rethinking might be worthwhile here.
                    GameActor targetActor = targetHit as GameActor;
                    if (targetActor != null)
                    {
                        target.Shooter = this;
                        targetActor.ShooterHitSet.Add(target);
                    }
                }
            }
        }

        // SGI_MOD - beam related

        public void OnBeamExpire()
        {
        }

        public void OnBeamHit(GameThing targetHit, Vector3 hitPos)
        {
            // We may have died after shooting our missiles.
            if (CurrentState == State.Inactive)
                return;

            // we only process the hit if the action was performed
            if (!ScienceActionPerformed)
                return;

            GameActor targetActor = targetHit as GameActor;
            if (targetHit != null && targetActor != null && !targetActor.Beamed)
            {
                Vector3 actorCenter = Vector3.Transform(BoundingSphere.Center, Movement.LocalMatrix);
                Vector3 thingCenter = Vector3.Transform(targetHit.BoundingSphere.Center, targetHit.Movement.LocalMatrix);
                Vector3 direction = thingCenter - actorCenter;
                float range = direction.Length();

                SensorTarget target = SensorTargetSpares.Alloc();
                target.Init(targetHit, direction, range);
                beamedSet.AddOrFree(target);

                if (targetActor != null)
                {
                    target = SensorTargetSpares.Alloc();
                    target.Init(this, -direction, range);
                    targetActor.beamedSet.AddOrFree(target);
                    if (!targetActor.Beamed)
                    {
                        ExplosionManager.CreateBeamExplosion(targetActor.Movement.Position, 1.2f);
                    }

                    // only mark the rock as beamed if it has been revealed to be an actual rock
                    if (targetActor.Revealed || targetActor.classification.name != "rockUnknown")
                    {
                        targetActor.Beamed = true;
                    }
                    targetActor.Revealed = true;
                }
            }
        }

        // SGI_MOD
        /// <summary>
        /// If we are sensible or not
        /// </summary>

        public override bool CanBeSensed()
        {
            bool bSensible = true;
            bool bIsRock = IsRoverRock(this);

            if (bIsRock)
            {
                if (Beamed || Inspected)
                {
                    bSensible = false;
                }
            }

            return bSensible;
        }

        /// <summary>
        /// omnivision.
        /// </summary>
        public virtual void CreateDevices()
        {
            Vector3 direction;
            float arc;

            if (XmlActorParams.SensorsData != null)
            {
                if (XmlActorParams.SensorsData.Disable)
                    return;

                direction = XmlActorParams.SensorsData.Direction;
                arc = XmlActorParams.SensorsData.Arc;
            }
            else
            {
                //Default to full spherical sensing.
                direction = Vector3.Up;
                arc = MathHelper.TwoPi;
            }

            VisualDevice visual = new VisualDevice();
            AddVisualDevice(visual);
        }

        protected void AddVisualDevice(VisualDevice device)
        {
            VisualDevices.Add(device);
        }

        public override void LoadContent(bool immediate)
        {
			//make sure we load both render objects
            BokuGame.Load(renderObj, immediate);
            BokuGame.Load(renderObjRevealed, immediate);

            if (face != null)
                BokuGame.Load(face, immediate);

        }   // end of GameProp LoadContent()

        public override void InitDeviceResources(GraphicsDevice device)
        {
            InitAnimationSet();
        }

        public override void UnloadContent()
        {
            if (face != null)
                BokuGame.Unload(face);

            BokuGame.Unload(renderObj);
            BokuGame.Unload(renderObjRevealed);
        }   // end of GameProp UnloadContent()

        public void CacheCreatables()
        {
            GameActor source = null;
            if (IsClone)
            {
                source = InGame.inGame.GetCreatable(CreatableId);
            }
            else if (Creatable)
            {
                source = this;
            }
            if (source != null)
            {
                source.ClearCreatableCache();

                List<GameActor> clones = new List<GameActor>();
                InGame.inGame.GetClones(source.CreatableId, clones);
                for (int i = 0; i < clones.Count; ++i)
                {
                    GameActor actor = clones[i] as GameActor;
                    if ((actor != null) && (actor.PendingState != State.Inactive))
                    {
                        source.CreatablesCache.Add(actor);
                    }
                }
            }
        }
        /// <summary>
        /// Clear the cache of creatables cloned from me or my source.
        /// </summary>
        public void ClearCreatableCache()
        {
            if (IsClone)
            {
                GameActor source = InGame.inGame.GetCreatable(CreatableId);
                if (source != null)
                {
                    source.ClearCreatableCache();
                }
            }
            else
            {
                CreatablesCache.Clear();
            }
        }
        /// <summary>
        /// Display lines from me to all my clones.
        /// </summary>
        /// <param name="camera"></param>
        public void DisplayCreatableLines(Camera camera)
        {
            if (IsClone)
            {
                GameActor source = InGame.inGame.GetCreatable(CreatableId);
                if (source != null)
                {
                    source.DisplayCreatableLines(camera);
                }
            }
            else if (CreatablesCache.Count > 0)
            {

                createLines.Clear();
                createColors.Clear();

                Vector3 myPosition = WorldGlowPosition;
                Vector4 myColor = new Vector4(0.0f, 0.8f, 0.0f, 0.0f);
                if (InGame.inGame.IsPickedUp(this))
                    myColor = new Vector4(0.8f, 0.0f, 0.0f, 1.0f);
                else if (InGame.inGame.IsSelected(this))
                    myColor = new Vector4(0.0f, 0.0f, 0.8f, 1.0f);

                float kCountAtMax = 7.0f;
                float kCountAtMin = 30.0f;
                float kMinColorScale = 0.2f;
                float kMaxColorScale = 1.0f;
                float bloom = (CreatablesCache.Count - kCountAtMax) / (kCountAtMin - kCountAtMax);
                bloom = MyMath.Clamp<float>(bloom, 0, 1);
                bloom = kMaxColorScale + bloom * (kMinColorScale - kMaxColorScale);

                double kPulseSecs = 0.25;
                double phase = Time.WallClockTotalSeconds;
                phase = phase / kPulseSecs;
                phase -= (int)phase;

                for (int i = 0; i < CreatablesCache.Count; ++i)
                {
                    GameActor clone = CreatablesCache[i];
                    Vector3 itPosition = clone.WorldGlowPosition;

                    createLines.Add(myPosition);
                    createLines.Add(itPosition);

                    Vector4 itColor = Classification.ColorVector4(clone.ClassColor);
                    createColors.Add(itColor);

                }
                Utils.DrawRunway(camera, createLines, createColors, myColor, (float)phase, bloom);
                createLines.Clear();
                createColors.Clear();
            }
        }
        private static List<Vector3> createLines = new List<Vector3>(2);
        private static List<Vector4> createColors = new List<Vector4>(2);
        private List<GameActor> _creatablesCache;
        private List<GameActor> CreatablesCache
        {
            get { return _creatablesCache ?? (_creatablesCache = new List<GameActor>()); }
        }

        private void ClearLines()
        {
            double t = Time.GameTimeTotalSeconds;
            double life = 3.0;
            int firstLiving;
            for (firstLiving = 0; firstLiving < testLines.Count; ++firstLiving)
            {
                if (t - testLines[firstLiving].time < life)
                    break;
            }
            if (firstLiving > 0)
            {
                testLines.RemoveRange(0, firstLiving);
            }
        }
        private void DisplayLines(Camera camera)
        {
#if TEST_BLOCKED
            MouseTouchHitInfo MouseTouchHitInfo = new MouseTouchHitInfo();
            Vector3 at = WorldCollisionCenter + Movement.LocalMatrix.Right * 20.0f;
            if (Blocked(at, ref MouseTouchHitInfo))
            {
                AddLine(WorldCollisionCenter, MouseTouchHitInfo.Contact, new Vector4(1.0f, 0.0f, 0.0f, 1.0f));
                AddLine(MouseTouchHitInfo.Contact, MouseTouchHitInfo.Contact + MouseTouchHitInfo.Normal, new Vector4(1.0f, 1.0f, 0.0f, 1.0f));
            }
            else
            {
                AddLine(WorldCollisionCenter, at, new Vector4(0.0f, 0.0f, 1.0f, 1.0f));
            }
#endif // TEST_BLOCKED
            if (testLines.Count > 0)
            {
                createLines.Clear();
                createColors.Clear();

                for (int i = 0; i < testLines.Count; ++i)
                {
                    createLines.Add(testLines[i].src);
                    createLines.Add(testLines[i].dst);
                    createColors.Add(testLines[i].color);
                }
                Utils.DrawLines(camera, createLines, createColors);

                createLines.Clear();
                createColors.Clear();
            }
        }
        public void AddLOSLine(Vector3 src, Vector3 dst, Vector4 color)
        {
            if (DisplayLOS || DisplayLOP)
            {
                AddLine(src, dst, color);
            }
        }
        private void AddLine(Vector3 src, Vector3 dst, Vector4 color)
        {
            TestLine line = new TestLine(src, dst, color);
            testLines.Add(line);
        }
        private void AddLine(SimWorld.Collision.MouseTouchHitInfo MouseTouchHitInfo)
        {
            if (MouseTouchHitInfo.Touching)
            {
                AddLine(MouseTouchHitInfo.Contact,
                    MouseTouchHitInfo.Contact + MouseTouchHitInfo.Normal * 2.0f,
                    new Vector4(1.0f, 0.5f, 0.5f, 1.0f));
            }
            else
            {
                AddLine(MouseTouchHitInfo.Contact,
                    MouseTouchHitInfo.Contact + MouseTouchHitInfo.Normal * 2.0f,
                    new Vector4(1.0f, 0.0f, 0.0f, 1.0f));
            }
        }
        private struct TestLine
        {
            public Vector3 src;
            public Vector3 dst;
            public Vector4 color;
            public double time;

            public TestLine(Vector3 src, Vector3 dst, Vector4 color)
            {
                this.src = src;
                this.dst = dst;
                this.color = color;
                this.time = Time.WallClockTotalSeconds;
            }
        }
        private List<TestLine> testLines = new List<TestLine>();

        /// <summary>
        /// Add a visualization line if user has enabled it.
        /// </summary>
        /// <param name="dst"></param>
        public void AddSightLine(GameThing dstThing)
        {
            if (InGame.DebugDisplayLinesOfPerception || DisplayLOP)
            {
                Movement src = Movement;
                Vector4 color = Classification.ColorVector4(ClassColor);
                float life = InGame.DebugDisplayLinesOfPerception
                    ? 0.25f
                    : 0.75f;
                // Shorten up the life span.  Not sure why the lines had varying life spans to begin with.
                // This reduces overdraw and makes the lines look instantaneous.
                life = 0.02f;
                AddSightLine(this, dstThing, color, life);
            }
        }
        /// <summary>
        /// Add a visualization line if user has enabled it.
        /// </summary>
        /// <param name="dst"></param>
        public void AddSoundLine(GameThing dstThing)
        {
            if (InGame.DebugDisplayLinesOfPerception || DisplayLOP)
            {
                Movement src = Movement;
                Vector4 color = Classification.ColorVector4(ClassColor);
                float life = InGame.DebugDisplayLinesOfPerception
                    ? 0.1f
                    : 0.5f;
                // Shorten up the life span.  Not sure why the lines had varying life spans to begin with.
                // This reduces overdraw and makes the lines look instantaneous.
                life = 0.02f;
                AddSoundLine(this, dstThing, color, life);
            }
        }
        /// <summary>
        /// Add a visualization line if user has enabled it.
        /// </summary>
        /// <param name="src"></param>
        /// <param name="dst"></param>
        public static void AddMissileLine(GameThing srcThing, GameThing dstThing)
        {
            if (InGame.DebugDisplayLinesOfPerception)
            {
                AddSightLine(srcThing, dstThing, new Vector4(0.3f, 0.3f, 0.3f, 1.0f), 0.1f);
            }
        }

        /// <summary>
        /// Display all visualization lines for robot sight and hearing.
        /// </summary>
        /// <param name="camera"></param>
        public static void DisplayLinesOfPerception(Camera camera)
        {
            if (_sightLines.Count > 0)
            {
                DisplayDirectLines(camera,
                    new Vector4(1.0f, 0.2f, 0.1f, 1.0f),
                    _sightLines);
            }

            if (_soundLines.Count > 0)
            {
                DisplayDirectLines(camera,
                    new Vector4(0.2f, 1.0f, 0.1f, 1.0f),
                    _soundLines);
            }
        }
        /// <summary>
        /// Update all visualization lines (discard old ones).
        /// </summary>
        public static void UpdateLinesOfPerception()
        {
            UpdateDirectLines(_sightLines);
            UpdateDirectLines(_soundLines);
        }
        /// <summary>
        /// Clear out all visualization lines (e.g. when entering edit mode).
        /// </summary>
        public static void ClearLinesOfPerception()
        {
            _sightLines.Clear();
            _soundLines.Clear();
        }

        /// <summary>
        /// Add a visualization line representing seeing something.
        /// </summary>
        /// <param name="src"></param>
        /// <param name="dst"></param>
        /// <param name="color"></param>
        /// <param name="life"></param>
        public static void AddSightLine(GameThing srcThing, GameThing dstThing, Vector4 color, float life)
        {
            DirectLine line = new DirectLine(srcThing, dstThing, color, life);
            _sightLines.Add(line);
        }
        /// <summary>
        /// Add a visualization line representing hearing something.
        /// </summary>
        /// <param name="src"></param>
        /// <param name="dst"></param>
        /// <param name="color"></param>
        /// <param name="life"></param>
        private static void AddSoundLine(GameThing srcThing, GameThing dstThing, Vector4 color, float life)
        {
            DirectLine line = new DirectLine(srcThing, dstThing, color, life);
            _soundLines.Add(line);
        }
        /// <summary>
        /// Internal helper to render the input set of lines of perception
        /// </summary>
        /// <param name="camera"></param>
        /// <param name="color"></param>
        /// <param name="directLines"></param>
        private static void DisplayDirectLines(Camera camera, Vector4 color, List<DirectLine> directLines)
        {
            createLines.Clear();
            createColors.Clear();

            double kPulseSecs = 0.15;
            double phase = Time.WallClockTotalSeconds;
            phase = phase / kPulseSecs;
            phase -= (int)phase;
            phase = 1.0f - phase;

            if (directLines.Count > 0)
            {
                for (int i = 0; i < directLines.Count; ++i)
                {
                    float age = 1.0f - directLines[i].NormalizedAge;
                    age *= age;
                    age = 0.5f + age * (1.0f - 0.5f);
                    Vector3 srcPos = directLines[i].origSrc + age * (directLines[i].src.Position - directLines[i].origSrc);
                    Vector3 dstPos = directLines[i].origDst + age * (directLines[i].dst.Position - directLines[i].origDst);

                    srcPos.Z += directLines[i].srcEyeOffset;
                    dstPos.Z += directLines[i].dstEyeOffset;

                    createLines.Add(srcPos);
                    createLines.Add(dstPos);
                    createColors.Add(directLines[i].color);
                }
                Utils.DrawRunway(camera, createLines, createColors, color, (float)phase, 1.0f);
            }

            createLines.Clear();
            createColors.Clear();
        }
        /// <summary>
        /// Internal helper to update the input set of lines of perception.
        /// </summary>
        /// <param name="directLines"></param>
        private static void UpdateDirectLines(List<DirectLine> directLines)
        {
            for (int i = directLines.Count - 1; i >= 0; --i)
            {
                if (directLines[i].Dead)
                {
                    directLines.RemoveAt(i);
                }
            }
        }
        /// <summary>
        /// Helper struct to represent a single line of sight or hearing.
        /// </summary>
        private struct DirectLine
        {
            public Vector3 origSrc;
            public Vector3 origDst;
            public Movement src;
            public Movement dst;
            public float srcEyeOffset;
            public float dstEyeOffset;
            public Vector4 color;
            public double death;
            public double birth;

            /// <summary>
            /// Constructor
            /// </summary>
            /// <param name="src"></param>
            /// <param name="dst"></param>
            /// <param name="color"></param>
            /// <param name="life"></param>
            public DirectLine(GameThing srcThing, GameThing dstThing, Vector4 color, float life)
            {
                Debug.Assert(life > 0.0f, "Must have positive life span");
                this.origSrc = srcThing.Movement.Position;
                this.origDst = dstThing.Movement.Position;
                this.src = srcThing.Movement;
                this.dst = dstThing.Movement;
                this.srcEyeOffset = srcThing.EyeOffset;
                this.dstEyeOffset = dstThing.EyeOffset;
                this.color = color;
                this.birth = Time.GameTimeTotalSeconds;
                this.death = this.birth + life;
            }

            /// <summary>
            /// Time since birth
            /// </summary>
            public float Age
            {
                get { return (float)(Time.GameTimeTotalSeconds - birth); }
            }
            /// <summary>
            /// How long is my lifetime.
            /// </summary>
            public float LifeSpan
            {
                get { return (float)(death - birth); }
            }
            /// <summary>
            /// Have I died?
            /// </summary>
            public bool Dead
            {
                get { return Age >= LifeSpan; }
            }
            /// <summary>
            /// Age normalized to 0==birth => 1.0f == death.
            /// </summary>
            public float NormalizedAge
            {
                get { return Age / LifeSpan; }
            }
        }
        /// <summary>
        /// Internal temp lists for rendering DirectLines. Consider them indeterminate state
        /// on entering a function, and effectively destructed on leaving that same func.
        /// </summary>
        private static List<DirectLine> _sightLines = new List<DirectLine>();
        /// <summary>
        /// Internal temp lists for rendering DirectLines. Consider them indeterminate state
        /// on entering a function, and effectively destructed on leaving that same func.
        /// </summary>
        private static List<DirectLine> _soundLines = new List<DirectLine>();


        /// <summary>
        /// Display user visible debug collision information.
        /// </summary>
        /// <param name="camera"></param>
        public void DisplayCollisions(Camera camera)
        {
            DisplayLines(camera);
            if (SRO.DisplayCollisions)
            {
                // Normally fixed position bots use a collection of geometirc prims to represent
                // their collision volume.  Clams however just use a bounding sphere like the 
                // rest of the mobile bots so we need to check to see if this is a Clam.
                if (!Chassis.FixedPosition || Classification.name.Equals("clam"))
                {
                    Vector3 center = WorldCollisionCenter;

                    Vector4 classColor = Classification.ColorVector4(ClassColor);
#if NETFX_CORE
                    // For some reason wireframe rendering doesn't seem to be working on WinRT
                    // so go with a trqansparent sphere instead.
                    classColor.W = 0.4f;
                    Utils.DrawSolidSphere(camera, center, CollisionRadius, classColor);
#else
                    KoiLibrary.GraphicsDevice.RasterizerState = SharedX.RasterStateWireframe;
                    Utils.DrawSolidEllipsoid(camera, center, CollisionRadius * SquashScale, classColor);
                    KoiLibrary.GraphicsDevice.RasterizerState = RasterizerState.CullCounterClockwise;
#endif
                }
            }

            DisplayCollisions_Touch.Clear();
            DisplayCollisions_NoTouch.Clear();
        }
        private static List<Vector3> DisplayCollisions_Touch = new List<Vector3>(200);
        private static List<Vector3> DisplayCollisions_NoTouch = new List<Vector3>(200);

#if DEBUG
        static int whichTest = 0;
        private void TestBlock(Camera camera)
        {
            Vector3 dir = Movement.Facing * 10.0f;
            Vector3 norm = Vector3.Zero;
            Vector3 src = Movement.Position;
            Vector3 dst = src + dir;

            Vector2 minMax = new Vector2(
                0.0f,
                Movement.Position.Z);

            Vector4 maxStep = new Vector4(
                Chassis.EditHeight + Chassis.WaistOffset, // max single step
                Single.MinValue, // max step down
                1.0f, // water depth at which transition land to water occurs
                1.0f); // water depth at which transition water to land occurs

            TestBlock(src, dst, minMax, maxStep);
        }
        public void TestBlock(Vector3 src, Vector3 dst, Vector2 minMax, Vector4 maxStep)
        {

            float srcHeight = SimWorld.Terra.Terrain.GetTerrainAndPathHeight(src);
            float dstHeight = SimWorld.Terra.Terrain.GetTerrainAndPathHeight(dst);
            bool noLand = dstHeight <= 0.0f;
            bool hiLand = dstHeight >= Movement.Position.Z;
            bool onPath = SimWorld.Path.WayPoint.GetHeight(dst, ref dstHeight);

            SimWorld.Terra.Terrain.HitBlock hitBlock = new SimWorld.Terra.Terrain.HitBlock();

            if (srcHeight <= 0.0f)
            {
                minMax.X = -1.0f;
                minMax.Y = 0.0f;
            }
            bool found = false;
            switch (whichTest)
            {
                case 0:
                    {
                        /// Use the general full service interface.
                        found = Terrain.Blocked(
                            src,
                            dst,
                            minMax,
                            maxStep,
                            ref hitBlock,
                            Movement.Altitude
                            );
                    }
                    break;
#if false
                case 1:
                    {
                        /// Presumes that src is not on land, will search from src to dst
                        /// for the first land (including paths) position.
                        found = Terrain.FindLand(
                            src,
                            dst,
                            ref hitBlock);
                    }
                    break;
                case 2:
                    {
                        /// Presumes src is on land (or path), searches toward dst for the
                        /// end of the world.
                        found = Terrain.FindEdgeOfWorld(
                            src,
                            dst,
                            ref hitBlock);
                    }
                    break;
                case 3:
                    {
                        /// Ignores end of world conditions, and just searches for blocking
                        /// walls between src and dst. A wall (or cliff) is considered blocking
                        /// if it is higher than src.Z.
                        found = Terrain.FindWall(
                            src,
                            dst,
                            ref hitBlock);
                    }
                    break;
                case 4:
                    {
                        /// Search from src to dst for the first blocking wall. 
                        /// A wall is considered blocking if it is higher than src.Z.
                        /// The edge of the world is treated as an infinitely high wall.
                        /// If src is off the edge of the world, src is returned as the 
                        /// blocking position.
                        found = Terrain.FindWallOrEdge(
                            src,
                            dst,
                            ref hitBlock);
                    }
                    break;
#endif
            }
            if (found)
            {
                Vector4 color = new Vector4(1.0f, 0.25f, 0.25f, 1.0f);
                AddLine(Movement.Position, hitBlock.Position, color);
                color.Y = 1.0f;
                AddLine(hitBlock.Position, hitBlock.Position + hitBlock.Normal, color);
            }
            else
            {
                Vector4 color = new Vector4(0.25f, 1.0f, 0.25f, 1.0f);
                AddLine(src, dst, color);
                //if (SimWorld.Terra.Terrain.Current != null)
                //{
                //    Debug.Assert(!noLand && !hiLand && !onPath);
                //}
            }
        }
        public static void TestScoy(Camera camera)
        {
            Vector3 src = new Vector3(-24.3606f, 44.23367f, 4.215578f);
            Vector3 dst = new Vector3(-24.36319f, 44.23535f, 4.216258f);

            Vector2 minMax = new Vector2(-1.0f, 3.793968f);
            Vector4 maxStep = new Vector4(
                Single.MaxValue,
                Single.MinValue,
                -1.0f,
                -1.0f);

            Terrain.HitBlock hitBlock = new Terrain.HitBlock();
            if (Terrain.Blocked(src, dst, minMax, maxStep, ref hitBlock, 0.0f))
            {
                Vector4 color = new Vector4(1.0f, 0.25f, 0.25f, 1.0f);
                Utils.DrawLine(camera,
                    src,
                    hitBlock.Position,
                    color);
                color.Y = 1.0f;
                Utils.DrawLine(camera,
                    hitBlock.Position,
                    hitBlock.Position + hitBlock.Normal,
                    color);
            }
            Utils.DrawLine(camera,
                src,
                dst,
                new Vector4(0.25f, 1.0f, 0.25f, 1.0f));
        }

        public override void DebugDisplay(Camera camera)
        {
            if (InGame.inGame.renderEffects != InGame.RenderEffect.Normal)
            {
                return;
            }

            bool doBlock = false;
            if (doBlock)
                TestBlock(camera);
            //TestScoy(camera);

            DisplayCollisions(camera);

            bool doAnything = false;
            if (doAnything)
            {
                bool showBase = false;
                if (showBase)
                {
                    base.DebugDisplay(camera);
                }

                BoundingSphere bound = BoundingSphere;
                bound.Center = Vector3.Transform(bound.Center, Movement.LocalMatrix);
                // Assumes uniform scaling.
                bound.Radius *= Movement.LocalMatrix.Up.Length();

                bool showCollide = true;
                if (showCollide)
                {
                    Vector3 collCenter = WorldCollisionCenter;
                    Utils.DrawSphere(camera, collCenter, WorldCollisionRadius);
                }

                bool showHold = false;
                if (showHold)
                {
                    Utils.DrawAxis(camera, WorldHoldingPosition);
                }
                bool showGrab = false;
                if (showGrab)
                {
                    Utils.DrawSphere(camera, bound.Center, grabRange * ReScale);
                }
                bool showKick = false;
                if (showKick)
                {
                    Utils.DrawSphere(camera, bound.Center, kickRange * ReScale);
                }
                bool showGlow = false;
                if (showGlow)
                {
                    Utils.DrawAxis(camera, glowEmitter.Position);
                }

                bool showPipes = false;
                if (showPipes && Chassis != null && Chassis is PipeChassis)
                {
                    (Chassis as PipeChassis).DebugDisplay(this, camera);
                }
            }
        } // end DebugDisplay
#else
        public override void DebugDisplay(Camera camera)
        {
            DisplayCollisions(camera);
        }
#endif

        private void InitAsCreatableOrNot()
        {
            if (Creatable)
            {
                InGame.inGame.NewCreatable(this);
                InGame.inGame.UnRegisterBrain(this);
            }
            else
            {
                InGame.inGame.UnregisterCreatable(this);
                InGame.inGame.RegisterBrain(this);
            }
        }

        public void InitAsCreatableClone()
        {
            // We want to mark ourselves as bound to the creatable, so set this value
            // directly instead of using the accessor.  Accessor will unregister the
            // creatable, we don't want that.
            localParameters.creatable = false;

            // If the creatable we are bound to has been unregistered, then mark us
            // as an individual.
            if (!InGame.inGame.IsCreatableRegistered(CreatableId))
                CreatableId = Guid.Empty;
        }

        public void CopyBrainToClones()
        {
            // Only replicate our brain if we're a creatable.
            if (!Creatable)
                return;

            List<GameActor> cloneList = new List<GameActor>();
            InGame.inGame.GetClones(CreatableId, cloneList);

            MemoryStream memStream = new MemoryStream();

            XmlSerializer serializer = new XmlSerializer(typeof(Brain));
            serializer.Serialize(memStream, Brain);
            memStream.Flush();

            for (int i = 0; i < cloneList.Count; ++i)
            {
                GameActor curr = cloneList[i] as GameActor;
                memStream.Position = 0;
                curr.Brain = (Brain)serializer.Deserialize(memStream);
            }

            /// Also want to pass the serialized brain to the ActorFactory, so
            /// any recycled versions of this creatable get a chance to re-sync
            /// to brain changes.
            ActorFactory.CopyBrains(CreatableId, memStream, serializer);

#if NETFX_CORE
            memStream.Flush();
            memStream.Dispose();
#else
            memStream.Close();
#endif
        }

        public void InitHitPoints(int hp)
        {
            currHitPoints = prevHitPoints = hp;
        }


        protected override bool IExecuteVerb(GameThing.Verbs verb, GameActor directObject, BaseAction effector, bool quiet)
        {
            bool supports = false;

            switch (verb)
            {
                // do glow
                case Verbs.Glow:
                    directObject = directObject ?? this as GameActor;

                    // Is this the first application this frame?  If
                    // so, update frame counter and clear list.
                    if (Time.FrameCounter != GlowTargetListFrame)
                    {
                        GlowTargetListFrame = Time.FrameCounter;
                        GlowTargetList.Clear();
                    }

                    // If we haven't changed this target's glow this
                    // frame, do so and add it to the list.
                    if (!GlowTargetList.Contains(directObject))
                    {
                        GlowTargetList.Add(directObject);
                        supports = directObject.DoGlow(effector);
                    }

                    break;

                // do shoot [it|...]
                case Verbs.Shoot:
                    supports = DoShoot(effector, directObject, quiet, ProjectileModifier.ProjectileType.Missile);
                    break;

                // do shoot [it|...]
                case Verbs.Shoot2:
                    supports = DoShoot(effector, directObject, quiet, ProjectileModifier.ProjectileType.Blip);
                    break;

                // do beam something
                case Verbs.Beam:
                    supports = DoBeam(effector, directObject, quiet);
                    break;

                // do inspect something
                case Verbs.Inspect:
                    supports = DoInspect(effector, directObject, quiet);
                    break;

                // do scan something
                case Verbs.Scan:
                    supports = DoScan(effector, quiet);
                    break;

                // take a screen shot
                case Verbs.Picture:
                    supports = DoPicture(effector, quiet);
                    break;

                // do create something
                case Verbs.Make:
                    supports = DoMakeItem(effector, quiet);
                    break;

                // do switch
                case Verbs.SwitchTask:
                    supports = DoSwitchTask(effector);
                    break;

                // do express
                case Verbs.Express:
                    if (directObject != null && effector.Reflex.Data.HasModifier("modifier.it"))
                    {
                        supports = directObject.DoExpress(effector);
                    }
                    else
                    {
                        supports = DoExpress(effector);
                    }
                    break;

                // do launch [it|...]
                case Verbs.Launch:
                    supports = DoLaunch(effector, directObject, quiet);
                    break;

                // do push [it|...]
                case Verbs.Push:
                    supports = DoPush(effector, directObject, quiet, false);
                    break;

                // do pull [it|...]
                case Verbs.Pull:
                    supports = DoPush(effector, directObject, quiet, true);
                    break;

                // do eat [it]
                case Verbs.Eat:
                    supports = DoEat(effector, quiet);
                    break;

                // do kick [it] (archived)
                case Verbs.Kick:
                    supports = DoKick(effector, directObject, quiet);
                    break;

                // do grab [it]
                case Verbs.Grab:
                    supports = DoGrabObject(directObject, false, quiet);
                    break;

                // do drop thing i'm carrying
                case Verbs.Drop:
                    supports = DoDropObject(null, quiet);
                    break;

                // do give thing i'm carrying
                case Verbs.Give:
                    supports = DoGiveObject(effector, quiet);
                    break;

                // do open (different for every bot)
                case Verbs.Open:
                    directObject = directObject ?? this as GameActor;

                    // Is this the first application this frame?  If
                    // so, update frame counter and clear list.
                    if (Time.FrameCounter != OpenCloseTargetListFrame)
                    {
                        OpenCloseTargetListFrame = Time.FrameCounter;
                        OpenCloseTargetList.Clear();
                    }

                    // If we haven't changed this target's open/close state this
                    // frame, do so and add it to the list.
                    if (!OpenCloseTargetList.Contains(directObject))
                    {
                        OpenCloseTargetList.Add(directObject);
                        supports = DoOpen(directObject, effector, quiet);
                    }

                    break;

                // do close (different for every bot)
                case Verbs.Close:
                    directObject = directObject ?? this as GameActor;

                    // Is this the first application this frame?  If
                    // so, update frame counter and clear list.
                    if (Time.FrameCounter != OpenCloseTargetListFrame)
                    {
                        OpenCloseTargetListFrame = Time.FrameCounter;
                        OpenCloseTargetList.Clear();
                    }

                    // If we haven't changed this target's open/close state this
                    // frame, do so and add it to the list.
                    if (!OpenCloseTargetList.Contains(directObject))
                    {
                        OpenCloseTargetList.Add(directObject);
                        supports = DoClose(directObject, effector, quiet);
                    }

                    break;

                // do jump
                case Verbs.Jump:
                    if (!TweakImmobile && !TweakImmobileNoRot)
                    {
                        supports = DoJump(directObject, effector, quiet);
                    }
                    break;

                // do change max hitpoints
                case Verbs.MaxHitpointsChange:
                    if (directObject != null && effector.Reflex.Data.HasModifier("modifier.it"))
                    {
                        supports = directObject.DoMaxHitpointsChange(effector);
                    }
                    else
                    {
                        supports = DoMaxHitpointsChange(effector);
                    }
                    break;

                // do change blip damage
                case Verbs.BlipDamageChange:
                    if (directObject != null && effector.Reflex.Data.HasModifier("modifier.it"))
                    {
                        supports = directObject.DoBlipDamageChange(effector);
                    }
                    else
                    {
                        supports = DoBlipDamageChange(effector);
                    }
                    break;

                // do change missile damage
                case Verbs.MissileDamageChange:
                    if (directObject != null && effector.Reflex.Data.HasModifier("modifier.it"))
                    {
                        supports = directObject.DoMissileDamageChange(effector);
                    }
                    else
                    {
                        supports = DoMissileDamageChange(effector);
                    }
                    break;

                case Verbs.BlipReloadTimeChange:
                    if (directObject != null && effector.Reflex.Data.HasModifier("modifier.it"))
                    {
                        supports = directObject.DoBlipReloadTimeChange(effector);
                    }
                    else
                    {
                        supports = DoBlipReloadTimeChange(effector);
                    }
                    break;

                case Verbs.BlipRangeChange:
                    if (directObject != null && effector.Reflex.Data.HasModifier("modifier.it"))
                    {
                        supports = directObject.DoBlipRangeChange(effector);
                    }
                    else
                    {
                        supports = DoBlipRangeChange(effector);
                    }
                    break;

                case Verbs.MissileReloadTimeChange:
                    if (directObject != null && effector.Reflex.Data.HasModifier("modifier.it"))
                    {
                        supports = directObject.DoMissileReloadTimeChange(effector);
                    }
                    else
                    {
                        supports = DoMissileReloadTimeChange(effector);
                    }
                    break;

                case Verbs.MissileRangeChange:
                    if (directObject != null && effector.Reflex.Data.HasModifier("modifier.it"))
                    {
                        supports = directObject.DoMissileRangeChange(effector);
                    }
                    else
                    {
                        supports = DoMissileRangeChange(effector);
                    }
                    break;

                case Verbs.CloseByRangeChange:
                    if (directObject != null && effector.Reflex.Data.HasModifier("modifier.it"))
                    {
                        supports = directObject.DoCloseByRangeChange(effector);
                    }
                    else
                    {
                        supports = DoCloseByRangeChange(effector);
                    }
                    break;

                case Verbs.FarAwayRangeChange:
                    if (directObject != null && effector.Reflex.Data.HasModifier("modifier.it"))
                    {
                        supports = directObject.DoFarAwayRangeChange(effector);
                    }
                    else
                    {
                        supports = DoFarAwayRangeChange(effector);
                    }
                    break;

                case Verbs.HearingRangeChange:
                    if (directObject != null && effector.Reflex.Data.HasModifier("modifier.it"))
                    {
                        supports = directObject.DoHearingRangeChange(effector);
                    }
                    else
                    {
                        supports = DoHearingRangeChange(effector);
                    }
                    break;

                // do modify movement speed 
                case Verbs.MovementSpeedModify:
                    // Copy modifier from reflex to actor.
                    moveSpeedTileModifier = effector.Reflex.MoveSpeedTileModifier;
                    break;

                // do modify turn speed 
                case Verbs.TurningSpeedModify:
                    // Copy modifier from reflex to actor.
                    turnSpeedTileModifier = effector.Reflex.TurnSpeedTileModifier;
                    break;

                // do camouflage [it|...]
                case Verbs.Camouflage:
                    supports = DoCamouflage(effector, directObject, true);
                    break;

                // do uncamouflage [it|...]
                case Verbs.Uncamouflage:
                    supports = DoCamouflage(effector, directObject, false);
                    break;

                // do scale
                case Verbs.ReScale:
                    supports = DoReScale(effector, false);
                    break;

                // do scale
                case Verbs.ReScaleInstant:
                    supports = DoReScale(effector, true);
                    break;

                // do hold distance
                case Verbs.HoldDistance:
                    supports = DoHoldDistance(effector, false);
                    break;

                // do hold distance instant
                case Verbs.HoldDistanceInstant:
                    supports = DoHoldDistance(effector, true);
                    break;

                // do change world lighting
                case Verbs.WorldLightingChange:
                    supports = DoLightRigTransition(effector, false);
                    break;

                case Verbs.WorldLightingChangeInstant:
                    supports = DoLightRigTransition(effector, true);
                    break;

                // do change world sky
                case Verbs.WorldSkyChange:
                    supports = DoWorldSkyChange(effector, false);
                    break;

                case Verbs.WorldSkyChangeInstant:
                    supports = DoWorldSkyChange(effector, true);
                    break;

                // maybe the base class can execute it.
                default:
                    supports = base.IExecuteVerb(verb, directObject, effector, quiet);
                    break;
            }

            return supports;
        }


        private List<ActionSet> movementSets = new List<ActionSet>();

        public void QueueMovementSet(ActionSet actionSet)
        {
            movementSets.Add(actionSet);
        }

        private List<ActionSet> externalMovementSets = new List<ActionSet>();

        public void QueueExternalMovementSet(ActionSet actionSet)
        {
            externalMovementSets.Add(actionSet);
        }

        /// <summary>
        /// This should take the movement inputs from the brain and
        /// boil them down to simple turn/heading directions.
        /// </summary>
        private void ProcessMovementSets()
        {
            ProcessMovementActions();   // Handle new actions.
            /*
            if (!TweakImmobileNoRot)
            {
                ProcessHeadings();
                ProcessTurns();
            }
            */
            movementSets.Clear();
            externalMovementSets.Clear();
            
        }   // end of ProcessMovementSets()

        /// <summary>
        /// Processes the actions sets and puts the results
        /// in DesiredMovement class for use by chassis.
        /// </summary>
        void ProcessMovementActions()
        {
            foreach (ActionSet actionSet in movementSets)
            {
                foreach (BaseAction action in actionSet.Actions)
                {
                    action.Apply(this);
                }
            }
        }   // end of ProcessMovementActions()

        /// <summary>
        /// Given the name, find the next available display number.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        int FindNextDisplayNumber(string name)
        {
            usedNumbers.Clear();

            foreach (GameThing thing in InGame.inGame.gameThingList)
            {
                GameActor actor = thing as GameActor;

                // Skip ourself or the cursor.
                if (actor == this || actor == null)
                    continue;

                if (actor.DisplayName == name)
                {
                    usedNumbers.Add(actor.DisplayNumber);
                }
            }

            int number = 1;
            while (usedNumbers.Contains(number))
            {
                ++number;
            }

            return number;
        }   // end of FindNextDisplayNumber()
        static List<int> usedNumbers = new List<int>();

        //
        //
        // Movement related methods which combine this particular actor's values
        // with the values associated with its chassis.  Note that these values
        // do not include the effect of Quickly or Slowly.
        //
        //

        public float CalcMaxSpeed()
        {
            // movementSpeedModifier and linearAccelerationModifier are based on actor settings.
            // Chassis values are based on a chassis xml data for all actors of this type.

            // Default to 1.
            float maxSpeed = 1.0f;

            // Replace default if the chassis has a value.
            if (XmlActorParams.ChassisData.MaxSpeed.HasValue)
            {
                maxSpeed = XmlActorParams.ChassisData.MaxSpeed.Value;
            }

            // Apply modifier from Settings.
            maxSpeed *= SharedParameters.movementSpeedModifier;

            // Apply modifier from "move speed" tiles.
            maxSpeed *= moveSpeedTileModifier;

            return maxSpeed;
        }   // end of CalcMaxSpeed()

        public float CalcMaxAcceleration()
        {
            // movementSpeedModifier and linearAccelerationModifier are based on actor settings.
            // Chassis values are based on a chassis xml data for all actors of this type.

            // Default to 1.
            float maxAcceleration = 1.0f;

            // Replace default if the chassis has a value.
            if (XmlActorParams.ChassisData.MaxLinearAcceleration.HasValue)
            {
                maxAcceleration = XmlActorParams.ChassisData.MaxLinearAcceleration.Value;
            }

            // Apply modifier from Settings.
            maxAcceleration *= SharedParameters.linearAccelerationModifier;

            // Apply modifier from "move speed" tiles.
            maxAcceleration *= moveSpeedTileModifier;

            return maxAcceleration;
        }   // end of CalcMaxAcceleration()

        public float CalcTurnSpeed()
        {
            float turnSpeed = SharedParameters.turningSpeedModifier;
            if (XmlActorParams.ChassisData.MaxRotationRate.HasValue)
            {
                turnSpeed *= XmlActorParams.ChassisData.MaxRotationRate.Value;
            }

            // Apply modifier from "turn speed" tiles.
            turnSpeed *= turnSpeedTileModifier;

            return turnSpeed;
        }   // end of CalcTurnSpeed()

        public float CalcTurnAcceleration()
        {
            float turnAcceleration = SharedParameters.turningAccelerationModifier;
            if (XmlActorParams.ChassisData.MaxRotationalAcceleration.HasValue)
            {
                turnAcceleration *= XmlActorParams.ChassisData.MaxRotationalAcceleration.Value;
            }

            // Apply modifier from "turn speed" tiles.
            turnAcceleration *= turnSpeedTileModifier;

            return turnAcceleration;
        }   // end of CalcTurnAcceleration()

        public float CalcMaxVerticalSpeed()
        {
            // movementSpeedModifier and linearAccelerationModifier are based on actor settings.
            // Chassis values are based on a chassis xml data for all actors of this type.

            float maxVerticalSpeed = SharedParameters.verticalSpeedModifier;
            if (XmlActorParams.ChassisData.MaxVerticalSpeed.HasValue)
            {
                maxVerticalSpeed *= XmlActorParams.ChassisData.MaxVerticalSpeed.Value;
            }

            return maxVerticalSpeed;
        }   // end of CalcMaxVerticalSpeed()

        public float CalcMaxVerticalAcceleration()
        {
            // movementAccelerationModifier and linearAccelerationModifier are based on actor settings.
            // Chassis values are based on a chassis xml data for all actors of this type.

            float maxVerticalAcceleration = SharedParameters.verticalAccelerationModifier;
            if (XmlActorParams.ChassisData.MaxVerticalAcceleration.HasValue)
            {
                maxVerticalAcceleration *= XmlActorParams.ChassisData.MaxVerticalAcceleration.Value;
            }

            return maxVerticalAcceleration;
        }   // end of CalcMaxVerticalAcceleration()


    }   // end of class GameActor


    /// <summary>
    /// NullActor is used by the brain to poison empty sensor sets, so that a reflex's classification
    /// filters - if any exist - evaluate to false in the absence of any "real" sensed actors. This
    /// failure mechanism becomes necessary when the sensor is not compatible with the hidden count
    /// filter, and may or may not fire in response to sensed actors.
    /// See MouseSensor for usage example.
    /// </summary>
    public class NullActor : GameActor
    {
        static NullActor instance;

        public static NullActor Instance { get { return instance; } }

        static NullActor()
        {
            instance = new NullActor();
        }

        private NullActor()
            : base("null", "null", null, null, null, null)
        {
            movement = new Movement(null);
        }
    }   // end of class NullActor

}   // end of namespace Boku.Base
    
