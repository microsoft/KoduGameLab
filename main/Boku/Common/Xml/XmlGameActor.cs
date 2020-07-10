// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;

using System.Xml;
using System.Xml.Serialization;

using Microsoft.Xna.Framework;

using KoiX;
using KoiX.Text;

using Boku.Common.ParticleSystem;
using Boku.Fx;
using Boku.SimWorld;
using Boku.Audio;
using Boku.Programming;
using Boku.Base;
using Boku.SimWorld.Chassis;

namespace Boku.Common.Xml
{
    /// <summary>
    /// Simple class defining where to attach an effect to a model.
    /// </summary>
    public class AttachPoint
    {
        /// <summary>
        /// Offset from the bone in model space (not bone space).
        /// </summary>
        public Vector3 Offset = Vector3.Zero;

        /// <summary>
        /// The name of the bone to which the effect is attached.
        /// </summary>
        public string Bone = "";
    }

    /// <summary>
    /// Simple class representing a single smoke source on the actor.
    /// </summary>
    public class XmlSmokeSource : BokuShared.XmlData<XmlSmokeSource>
    {
        public bool Active = true;
        public bool Emitting = true;
        public Vector4 Color = new Vector4(0.85f, 0.85f, 0.95f, 1.0f);
        public float PositionJitter = 0.03f;
        public float StartRadius = 0.05f;
        public float EndRadius = 0.3f;
        public float StartAlpha = 1.0f;
        public float MinLifetime = 0.5f;
        public float MaxLifetime = 1.5f;
        public float MaxRotationRate = 3.0f;
        public float EmissionRate = 60.0f;
        public Vector3 Velocity = Vector3.Zero;
        public Vector3 Acceleration = Vector3.Zero;
        public BaseEmitter.Use Usage = BaseEmitter.Use.Regular;
        public bool AutoGen = true;

        public Vector3 Offset = Vector3.Zero;

        public string Bone = "";
    }

    public class XmlModel : BokuShared.XmlData<XmlModel>
    {
        public string SpecialType;
        public string TechniqueExt;
        public float? Shininess;
    }

    /// <summary>
    /// Xml parameterization of a Classification.
    /// </summary>
    public class XmlClassification : BokuShared.XmlData<XmlClassification>
    {
        public Classification.Colors Color;
        public Classification.Physicalities Physicality;
        public Classification.AudioImpression AudioImpression;
        public Classification.AudioVolume AudioVolume;
        public ExpressModifier.Emitters Emitter;
        public Face.FaceState Expression;
    }

    /// <summary>
    /// Xml parameterization of all the chassis types.
    /// </summary>
    public class XmlChassis : BokuShared.XmlData<XmlChassis>
    {
        /// <summary>
        /// The ChassisType as a string.
        /// </summary>
        /// <remarks>
        /// This is used by the XmlSerializer on deserialization.
        /// The setter will attempt to parse the input string as a
        /// ChassisType; if this fails, Type will be set to
        /// ChassisType.Unknown.
        /// The getter will always return the value of Type
        /// cast as a string.
        /// </remarks>
        [XmlAttribute("Type")]
        public string ChassisTypeString
        {
            set
            {
                ChassisType result;
                if (TextHelper.EnumTryParse(value, out result, true))
                {
                    Type = result;
                }
                else
                {
                    Type = ChassisType.Unknown;
                }
            }
            get
            {
                throw new NotSupportedException();
            }
        }

        /// <summary>
        /// The type of chassis.
        /// </summary>
        /// <remarks>
        /// Initial value is "NotApplicable". Will be set to
        /// "Unknown" if ChassisTypeString is set to a value
        /// that is not contained within the ChassisType enum.
        /// </remarks>
        [XmlIgnore]
        public ChassisType Type = ChassisType.NotApplicable;

        // TODO (****) Do we really want nullable types here?
        // Does this make the saved files cleaner?  If so, ok.
        // If not, this just seems like too much crap is dumped here
        // and it makes these values difficult to work with.
        // Are these ever even written or are they read only?  If 
        // read only then they definitely don't need to be nullable.

        //Base
        public float? Gravity;
        public float? MaxSpeed;
        public float? MaxLinearAcceleration;
        public float? MaxLinearDeceleration;
        public float? MaxRotationRate;
        public float? MaxRotationalAcceleration;
        public float? DefaultJumpStrength;
        public float? PreJumpDelay;
        public float? PreLandDelay;
        public float? JumpRate;
        public bool? HasFacingDirection;
        public bool? IgnoreGlassWalls;

        public float? SpinRate;                 // DynamicProp
        public float? Density;                  // DynamicProp
        public float? TumbleRadius;             // DynamicProp
        public bool? Tumbles;                   // DynamicProp
        public float? SlopeThreshold;           // Puck
        public float? Radius;                   // Puck
        public float? RotationRate;             // Saucer, Puck        
        public float? MaxVerticalAcceleration;  // FloatInAir
        public float? MaxVerticalSpeed;         // FloatInAir
        public float? MaxAltitude;              // FloatInAir
        public float? SlopeAttenuation;         // Hover
        public float? HullDraft;                // Swim, Boat
        public float? MinDepth;                 // Swim
        public float? MaxPitch;                 // Swim
        public float? MaxPitchRate;             // Swim
        public float? FlexAmplitude;            // Swim
        public float? Overshoot;                // Swim
        public PipeChassis.PipeTypeEnum? PipeType;// Pipe

        
        /// <summary>
        /// Assigns each applicable member of the given chassis
        /// the value of the respective member in this instance
        /// provided the latter member has been initialized.
        /// </summary>
        /// <param name="chassis">
        /// Should be the same type as this instances' Type
        /// member. Otherwise, chassis specific settings (such as 
        /// the PuckChassis' Radius member) will not be set.
        /// </param>
        public void CopyTo(BaseChassis chassis)
        {
            if (Gravity != null)
                chassis.Gravity = Gravity.Value;
            if (MaxSpeed != null)
                chassis.MaxSpeed = MaxSpeed.Value;
            if (MaxLinearAcceleration != null)
                chassis.MaxLinearAcceleration = MaxLinearAcceleration.Value;
            if (MaxLinearDeceleration != null)
                chassis.MaxLinearDeceleration = MaxLinearDeceleration.Value;
            if (MaxRotationRate != null)
                chassis.MaxRotationRate = MaxRotationRate.Value;
            if (MaxRotationalAcceleration != null)
                chassis.MaxRotationalAcceleration = MaxRotationalAcceleration.Value;
            if (DefaultJumpStrength != null)
                chassis.DefaultJumpStrength = DefaultJumpStrength.Value;
            if (PreJumpDelay != null)
                chassis.PreJumpDelay = PreJumpDelay.Value;
            if (PreLandDelay != null)
                chassis.PreLandDelay = PreLandDelay.Value;
            if (JumpRate != null)
                chassis.JumpRate = JumpRate.Value;
            if (HasFacingDirection != null)
                chassis.HasFacingDirection = HasFacingDirection.Value;
            if (IgnoreGlassWalls != null)
                chassis.IgnoreGlassWalls = IgnoreGlassWalls.Value;
            if (Density != null)
                chassis.Density = Density.Value;

            switch (Type)
            {
                case ChassisType.Boat:
                    var boatChassis = chassis as BoatChassis;
                    if (boatChassis == null)
                        break;
                    if (HullDraft != null)
                        boatChassis.HullDraft = HullDraft.Value;
                    break;
                case ChassisType.DynamicProp:
                    var dynamicPropChassis = chassis as DynamicPropChassis;
                    if (dynamicPropChassis == null)
                        break;
                    if (SpinRate != null)
                        dynamicPropChassis.SpinRate = SpinRate.Value;
                    if (TumbleRadius != null)
                        dynamicPropChassis.TumbleRadius = TumbleRadius.Value;
                    if (Tumbles != null)
                        dynamicPropChassis.Tumbles = Tumbles.Value;
                    break;
                case ChassisType.FloatInAir:
                    var floatInAirChassis = chassis as FloatInAirChassis;
                    if (floatInAirChassis == null)
                        break;
                    if (MaxVerticalAcceleration != null)
                        floatInAirChassis.MaxVerticalAcceleration = MaxVerticalAcceleration.Value;
                    if (MaxVerticalSpeed != null)
                        floatInAirChassis.MaxVerticalSpeed = MaxVerticalSpeed.Value;
                    if (MaxAltitude != null)
                        floatInAirChassis.MaxAltitude = MaxAltitude.Value;
                    break;
                case ChassisType.Hover:
                    var hoverChassis = chassis as HoverChassis;
                    if (hoverChassis == null)
                        break;
                    if (SlopeAttenuation != null)
                        hoverChassis.SlopeAttenuation = SlopeAttenuation.Value;
                    break;
                case ChassisType.HoverSwim:
                    var hoverSwimChassis = chassis as HoverSwimChassis;
                    if (hoverSwimChassis == null)
                        break;
                    break;
                case ChassisType.Puck:
                    var puckChassis = chassis as PuckChassis;
                    if (puckChassis == null)
                        break;
                    if (SlopeThreshold != null)
                        puckChassis.SlopeThreshold = SlopeThreshold.Value;
                    if (Radius != null)
                        puckChassis.Radius = Radius.Value;
                    if (RotationRate != null)
                        puckChassis.RotationRate = RotationRate.Value;
                    break;
                case ChassisType.Saucer:
                    var saucerChassis = chassis as SaucerChassis;
                    if (saucerChassis == null)
                        break;
                    if (RotationRate != null)
                        saucerChassis.RotationRate = RotationRate.Value;
                    if (SlopeThreshold != null)
                        saucerChassis.SlopeThreshold = SlopeThreshold.Value;
                    break;
                case ChassisType.Swim:
                    var swimChassis = chassis as SwimChassis;
                    if (swimChassis == null)
                        break;
                    if (HullDraft != null)
                        swimChassis.HullDraft = HullDraft.Value;
                    if (MinDepth != null)
                        swimChassis.MinDepth = MinDepth.Value;
                    if (MaxPitch != null)
                        swimChassis.MaxPitch = MaxPitch.Value;
                    if (FlexAmplitude != null)
                        swimChassis.FlexAmplitude = FlexAmplitude.Value;
                    break;
                case ChassisType.Pipe:
                    var pipeChassis = chassis as PipeChassis;
                    if (PipeType != null)
                        pipeChassis.PipeType = PipeType.Value;
                    break;
            }
        }
    }

    /// <summary>
    /// Xml parameterization of CurbFeeler data
    /// </summary>
    public class XmlCurbFeelers : BokuShared.XmlData<XmlCurbFeelers>
    {
        public float Radius
        {
            set
            {
                Nose = value;
                Width = value;
            }
            get
            {
                if (Nose == Width)
                    return Nose;
                else
                    return float.NaN;
            }
        }
        public float Nose = 0;
        public float Width = 0;
        public float Height = 0;
        public bool ExcludeBackwardFeeler = false;
    }

    /// <summary>
    /// Xml data for the actor's holding position
    /// </summary>
    public class XmlHoldingPosition : BokuShared.XmlData<XmlHoldingPosition>
    {
        public float X;
        public bool XOffsetByBoundingSphereRadius;
        public float Y;
        public bool YOffsetByBoundingSphereRadius;
        public float Z;
        public bool ZOffsetByBoundingSphereRadius;
    }

    /// <summary>
    /// Xml data for the actor's glow emitter
    /// </summary>
    public class XmlGlowEmitter : BokuShared.XmlData<XmlGlowEmitter>
    {
        public float StartRadius;
        public float EndRadius;
    }

    public class XmlAltitudeOptions : BokuShared.XmlData<XmlAltitudeOptions>
    {
        public bool IgnoreTerrainFeatures = false;
        public bool IgnoreWaves = true;
    }

    public class XmlSpecialActions : BokuShared.XmlData<XmlSpecialActions>
    {
        public class OpenClose
        {
            /// <summary>
            /// Is the actor invulnerable when closed?
            /// </summary>
            public bool Invulnerable;
            public GameActor.OpenCloseModes DefaultPosition;
            public float? HeightOffset;
            /// <summary>
            /// Does the actor freeze when closed?
            /// </summary>
            public bool Freeze;
        }

        public class FloatUpDown
        {
        }

        public class Jump
        {
        }

        [XmlElement("OpenClose")]
        public OpenClose OpenCloseData = null;
        [XmlElement("UpDown")]
        public FloatUpDown FloatUpDownData = null;
        [XmlElement("Jump")]
        public Jump JumpData = null;
    }

    public class XmlSensors : BokuShared.XmlData<XmlSensors>
    {
        public Vector3 Direction;
        public float Arc;
        public bool Disable;
    }

    //Xml data for special types

    public class XmlSharedIdle : BokuShared.XmlData<XmlSharedIdle>
    {
        public string ActiveAnimationName;
    }

    /// <summary>
    /// Xml parameterization of a GameActor. Other specifics to be migrated in.
    /// </summary>
    public class XmlGameActor : BokuShared.XmlData<XmlGameActor>
    {
        private static List<XmlGameActor> allActors = new List<XmlGameActor>();

        #region Members
        [XmlIgnore]
        private const string Prefix = @"Content\Xml\Actors\";
        [XmlIgnore]
        private const string Suffix = @".xml";

        private List<XmlSmokeSource> smokeSources = new List<XmlSmokeSource>();

        private List<SurfaceSet> surfaceSets = new List<SurfaceSet>();

        private XmlModel modelData = null;

        private Face.FaceParams faceParams = null;

        private AttachPoint glowPosition = new AttachPoint();

        private Vector3 collisionCenter = Vector3.Zero;

        private bool hasCushion = false;
        private float touchOffset = 0.0f;
        private float waistOffset = 0.0f;         // This is the offset in Z from the bot's height used for terrain collision testing.
        private float eyeOffset = 0.1f;           // This is the offset in Z from the bot's origin used to play the eye when in first person mode.
        private float minHeight = 0;
        private float defaultEditHeight = 0;

        private float cost = 1.0f;

        private Foley.CollisionSound collisionSound = Foley.CollisionSound.plasticSoft; // All bots default to sounding plastic.

        private bool isBot = false;
        private bool isProp = false;
        private bool isBuilding = false;

        private bool canReveal = false;

        private float? friction = null;
        private float? coefficientOfRestitution = null;
        private float? mass = null;
        private bool? stayAboveWater = null;

        private float? grabRange = null;
        private float? kickRange = null;
        private float? scanRange = null;
        private float? beamRange = null;
        private float? inspectRange = null;
        private float? clusterRange = null;

        private float? scanDelay = null;
        private float? beamDelay = null;
        private float? inspectDelay = null;
        private int? scanRings = null;
        private float? roverRotationAcc = null;
        private float? roverRotationRate = null;
        private float? roverFOV = null;
        private double? minPitch = null;
        private double? maxPitch = null;
        private double? minRoll = null;
        private double? maxRoll = null;
        private float? roverHillClimbSpeed = null;
        private float? roverHillStartPitch = null;
        private float? roverHillEndPitch = null;

        private XmlGlowEmitter glowEmitterData = null;

        private GameActor.MovementDomain domain = GameActor.MovementDomain.Air;

        private XmlAltitudeOptions altitudeOptions = new XmlAltitudeOptions();

        private XmlSpecialActions specialActions = new XmlSpecialActions();

        private string idleSoundName = null;
        private string moveSoundName = null;
        private string cursorSoundName = null;
        private string jumpSoundName = null;
        private string shootSoundName = null;
        private string rapidFireSoundName = null;
        private string twirlSoundName = null;
        private string animateSoundName = null;
        private string openSoundName = null;
        private string closeSoundName = null;

        private string ent1SoundName = null;
        private string ent2SoundName = null;
        private string ent3SoundName = null;
        private string ent4SoundName = null;
        private string ent5SoundName = null;
        private string ent6SoundName = null;
        private string ent7SoundName = null;
        private string ent8SoundName = null;
        private string ent9SoundName = null;
        private string ent10SoundName = null;
        private string ent11SoundName = null;
        private string ent12SoundName = null;

        private XmlClassification classificationData = new XmlClassification();

        private XmlChassis chassisData = new XmlChassis();

        private XmlCurbFeelers curbFeelersData = null;

        private XmlHoldingPosition holdingPositionData = null;

        private Vector3? createdItemOffset = null;

        private XmlSensors sensorsData = null;

        private XmlSharedIdle sharedIdleData = null;

        private ExamplePage defaultPage = null;

        #endregion Members

        #region Accessors
        [XmlArrayItem(typeof(XmlSmokeSource))]
        public List<XmlSmokeSource> SmokeSources
        {
            get { return smokeSources; }
            set { smokeSources = value; }
        }

        [XmlArrayItem(typeof(SurfaceSet))]
        public List<SurfaceSet> SurfaceSets
        {
            get { return surfaceSets; }
            set { surfaceSets = value; }
        }

        [XmlElement("Model")]
        public XmlModel ModelData
        {
            get { return modelData; }
            set { modelData = value; }
        }

        /// <summary>
        /// Enough information to create and initialize the face.
        /// </summary>
        public Face.FaceParams FaceParams
        {
            get { return faceParams; }
            set { faceParams = value; }
        }

        /// <summary>
        /// Where the RKO glow emanates from. Is also useful as sort of the
        /// "mental source" for the bot.
        /// </summary>
        public AttachPoint GlowPosition
        {
            get { return glowPosition; }
            set { glowPosition = value; }
        }

        /// <summary>
        /// Specifies the offset of the health bar (and thought balloon) referenced from the GlowPosition.
        /// </summary>
        public Vector3 HealthBarOffset
        {
            get;
            set;
        }

        /// <summary>
        /// Center of the collision sphere in local space.
        /// </summary>
        public Vector3 CollisionCenter
        {
            get { return collisionCenter; }
            set { collisionCenter = value; }
        }
        /// <summary>
        /// World space radius of the collision sphere.
        /// </summary>
        public float CollisionRadius 
        { 
            get; 
            set; 
        }

        /// <summary>
        /// Whether this bot senses things it glides over.
        /// </summary>
        public bool HasCushion
        {
            get { return hasCushion; }
            set { hasCushion = value; }
        }
        /// <summary>
        /// Adjustment to default TouchCushion for touching things you glide over.
        /// </summary>
        public float TouchOffset
        {
            get { return touchOffset; }
            set { touchOffset = value; }
        }

        /// <summary>
        /// This is the offset from the bot's height used for terrain collision testing.
        /// Defaults to 0.
        /// </summary>
        public float WaistOffset
        {
            get { return waistOffset; }
            set { waistOffset = value; }
        }

        /// <summary>
        /// This is the offset in Z from the bot's origin used to play the eye when in first person mode.
        /// Defaults to 0.1.
        /// </summary>
        public float EyeOffset
        {
            get { return eyeOffset; }
            set { eyeOffset = value; }
        }

        /// <summary>
        /// This is the minimum height of the object.  An object at 
        /// this height should appear to be sitting on the ground.
        /// </summary>
        public float MinHeight
        {
            get { return minHeight; }
            set { minHeight = value; }
        }

        /// <summary>
        /// This is the default height of the object in the editor.
        /// </summary>
        public float DefaultEditHeight
        {
            get { return defaultEditHeight; }
            set { defaultEditHeight = value; }
        }

        /// <summary>
        /// "cost" estimate for instancing an actor of this type.
        /// </summary>
        public float Cost
        {
            get { return cost; }
            set { cost = value; }
        }

        /// <summary>
        /// Collision sound used for an actor of this type.
        /// </summary>
        public Foley.CollisionSound CollisionSound
        {
            get { return collisionSound; }
            set { collisionSound = value; }
        }

        /// <summary>
        /// Is this actor classified as a bot?
        /// </summary>
        public bool IsBot
        {
            get { return isBot; }
            set { isBot = value; }
        }

        /// <summary>
        /// Is this actor classified as a prop?
        /// </summary>
        public bool IsProp
        {
            get { return isProp; }
            set { isProp = value; }
        }

        /// <summary>
        /// Is this actor classified as a building?
        /// </summary>
        public bool IsBuilding
        {
            get { return isBuilding; }
            set { isBuilding = value; }
        }

        /// <summary>
        /// Is this actor revealable?
        /// </summary>
        public bool CanReveal
        {
            get { return canReveal; }
            set { canReveal = value; }
        }

        /// <summary>
        /// Controls the bounciness of the object during collisions.
        /// </summary>
        public float? CoefficientOfRestitution
        {
            get { return coefficientOfRestitution; }
            set { coefficientOfRestitution = value; }
        }
        /// <summary>
        /// Kinetic Friction constant, 0=frictionless, 1=sandpaper
        /// </summary>
        public float? Friction
        {
            get { return friction; }
            set { friction = value; }
        }
        /// <summary>
        /// The mass in Kilograms
        /// </summary>
        public float? Mass
        {
            get { return mass; }
            set { mass = value; }
        }
        /// <summary>
        /// Whether this actor navigates on top of the water
        /// or along the terrain below
        /// </summary>
        public bool? StayAboveWater
        {
            get { return stayAboveWater; }
            set { stayAboveWater = value; }
        }

        /// <summary>
        /// The unscaled, default grab range
        /// </summary>
        public float? GrabRange
        {
            get { return grabRange; }
            set { grabRange = value; }
        }
        /// <summary>
        /// The unscaled, default kick range
        /// </summary>
        public float? KickRange
        {
            get { return kickRange; }
            set { kickRange = value; }
        }
        /// <summary>
        /// The unscaled, default beam range
        /// </summary>
        public float? BeamRange
        {
            get { return beamRange; }
            set { beamRange = value; }
        }
        /// <summary>
        /// The unscaled, default beam range
        /// </summary>
        public float? InspectRange
        {
            get { return inspectRange; }
            set { inspectRange = value; }
        }
        /// <summary>
        /// The unscaled, default cluster range
        /// </summary>
        public float? ClusterRange
        {
            get { return clusterRange; }
            set { clusterRange = value; }
        }
        /// <summary>
        /// The unscaled, default beam delay
        /// </summary>
        public float? BeamDelay
        {
            get { return beamDelay; }
            set { beamDelay= value; }
        }
        /// <summary>
        /// The unscaled, default beam delay
        /// </summary>
        public float? InspectDelay
        {
            get { return inspectDelay; }
            set { inspectDelay = value; }
        }
        /// <summary>
        /// The unscaled, default beam delay
        /// </summary>
        public float? ScanDelay
        {
            get { return scanDelay; }
            set { scanDelay = value; }
        }
        /// <summary>
        /// The unscaled, default beam delay
        /// </summary>
        public int? ScanRings
        {
            get { return scanRings; }
            set { scanRings= value; }
        }
        /// <summary>
        /// The unscaled, default rover rotation acceleration
        /// </summary>
        public float? RoverRotationAcc
        {
            get { return roverRotationAcc; }
            set { roverRotationAcc = value; }
        }
        /// <summary>
        /// The unscaled, default rover rotation rate
        /// </summary>
        public float? RoverRotationRate
        {
            get { return roverRotationRate; }
            set { roverRotationRate = value; }
        }
        /// <summary>
        /// The unscaled, default rover rotation fov
        /// </summary>
        public float? RoverFOV
        {
            get { return roverFOV; }
            set { roverFOV= value; }
        }
        /// <summary>
        /// The unscaled, default min pitch
        /// </summary>
        public double? MinPitch
        {
            get { return minPitch; }
            set { minPitch = value; }
        }
        /// <summary>
        /// The unscaled, default max pitch
        /// </summary>
        public double? MaxPitch
        {
            get { return maxPitch; }
            set { maxPitch = value; }
        }
        /// <summary>
        /// The unscaled, default min roll
        /// </summary>
        public double? MinRoll
        {
            get { return minRoll; }
            set { minRoll = value; }
        }
        /// <summary>
        /// The unscaled, default max roll
        /// </summary>
        public double? MaxRoll
        {
            get { return maxRoll; }
            set { maxRoll = value; }
        }
        /// <summary>
        /// The unscaled, the slowest speed of which the rover can climb hills
        /// </summary>
        public float? RoverHillClimbSpeed
        {
            get { return roverHillClimbSpeed; }
            set { roverHillClimbSpeed = value; }
        }
        /// <summary>
        /// The unscaled, the min pitch at which we start applying slow down
        /// </summary>
        public float? RoverHillStartPitch
        {
            get { return roverHillStartPitch; }
            set { roverHillStartPitch = value; }
        }
        /// <summary>
        /// The unscaled, the end pitch at which the slow down is maxed
        /// </summary>
        public float? RoverHillEndPitch
        {
            get { return roverHillEndPitch; }
            set { roverHillEndPitch = value; }
        }


        /// <summary>
        /// The unscaled, default scan range
        /// </summary>
        public float? ScanRange
        {
            get { return scanRange; }
            set { scanRange = value; }
        }

        /// <summary>
        /// The default data for the GlowEmitter
        /// </summary>
        [XmlElement("GlowEmitter")]
        public XmlGlowEmitter GlowEmitterData
        {
            get { return glowEmitterData; }
            set { glowEmitterData = value; }
        }

        public GameActor.MovementDomain Domain
        {
            get { return domain; }
            set { domain = value; }
        }

        public XmlAltitudeOptions AltitudeOptions
        {
            get { return altitudeOptions; }
            set { altitudeOptions = value; }
        }

        [XmlElement("Actions")]
        public XmlSpecialActions SpecialActions
        {
            get { return specialActions; }
            set { specialActions = value; }
        }

        // Sounds associated with actions and/or animations.
        public string IdleSoundName
        {
            get { return idleSoundName; }
            set { idleSoundName = value; }
        }
        public string MoveSoundName
        {
            get { return moveSoundName; }
            set { moveSoundName = value; }
        }
        public string CursorSoundName
        {
            get { return cursorSoundName; }
            set { cursorSoundName = value; }
        }
        public string JumpSoundName
        {
            get { return jumpSoundName; }
            set { jumpSoundName = value; }
        }
        public string ShootSoundName
        {
            get { return shootSoundName; }
            set { shootSoundName = value; }
        }
        public string RapidFireSoundName
        {
            get { return rapidFireSoundName; }
            set { rapidFireSoundName = value; }
        }
        public string TwirlSoundName
        {
            get { return twirlSoundName; }
            set { twirlSoundName = value; }
        }
        public string AnimateSoundName
        {
            get { return animateSoundName; }
            set { animateSoundName = value; }
        }
        public string OpenSoundName
        {
            get { return openSoundName; }
            set { openSoundName = value; }
        }
        public string CloseSoundName
        {
            get { return closeSoundName; }
            set { closeSoundName = value; }
        }
        public string Ent1SoundName
        {
            get { return ent1SoundName; }
            set { ent1SoundName = value; }
        }
        public string Ent2SoundName
        {
            get { return ent2SoundName; }
            set { ent2SoundName = value; }
        }
        public string Ent3SoundName
        {
            get { return ent3SoundName; }
            set { ent3SoundName = value; }
        }
        public string Ent4SoundName
        {
            get { return ent4SoundName; }
            set { ent4SoundName = value; }
        }
        public string Ent5SoundName
        {
            get { return ent5SoundName; }
            set { ent5SoundName = value; }
        }
        public string Ent6SoundName
        {
            get { return ent6SoundName; }
            set { ent6SoundName = value; }
        }
        public string Ent7SoundName
        {
            get { return ent7SoundName; }
            set { ent7SoundName = value; }
        }
        public string Ent8SoundName
        {
            get { return ent8SoundName; }
            set { ent8SoundName = value; }
        }
        public string Ent9SoundName
        {
            get { return ent9SoundName; }
            set { ent9SoundName = value; }
        }
        public string Ent10SoundName
        {
            get { return ent10SoundName; }
            set { ent10SoundName = value; }
        }
        public string Ent11SoundName
        {
            get { return ent11SoundName; }
            set { ent11SoundName = value; }
        }
        public string Ent12SoundName
        {
            get { return ent12SoundName; }
            set { ent12SoundName = value; }
        }

        /// <summary>
        /// The default classification data
        /// </summary>
        [XmlElement("Classification")]
        public XmlClassification ClassificationData
        {
            get { return classificationData; }
            set { classificationData = value; }
        }

        /// <summary>
        /// The default chassis data
        /// </summary>
        [XmlElement("Chassis")]
        public XmlChassis ChassisData
        {
            get { return chassisData; }
            set { chassisData = value; }
        }

        /// <summary>
        /// The curb feelers data
        /// </summary>
        [XmlElement("Feelers")]
        public XmlCurbFeelers CurbFeelersData
        {
            get { return curbFeelersData; }
            set { curbFeelersData = value; }
        }

        /// <summary>
        /// The data for the holding position
        /// </summary>
        [XmlElement("HoldingPosition")]
        public XmlHoldingPosition HoldingPositionData
        {
            get { return holdingPositionData; }
            set { holdingPositionData = value; }
        }

        /// <summary>
        /// Custom offset from me to created item in local space.
        /// </summary>
        [XmlElement("CreatedItemOffset")]
        public Vector3? CreatedItemOffset
        {
            get { return createdItemOffset; }
            set { createdItemOffset = value; }
        }

        [XmlElement("Sensors")]
        public XmlSensors SensorsData
        {
            get { return sensorsData; }
            set { sensorsData = value; }
        }


        [XmlElement("SharedIdle")]
        public XmlSharedIdle SharedIdleData
        {
            get { return sharedIdleData; }
            set { sharedIdleData = value; }
        }

        [XmlElement("DefaultPage")]
        public ExamplePage DefaultPage
        {
            get { return defaultPage; }
            set { defaultPage = value; }
        }

        #endregion Accessors

        #region Public
        /// <summary>
        /// There will generally be zero, one, or two SurfaceSets in an object,
        /// so this search isn't currently worth optimizing. Besides, it only happens
        /// at load.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public SurfaceSet FindSet(string name)
        {
            for (int i = 0; i < SurfaceSets.Count; ++i)
            {
                if (SurfaceSets[i].Name == name)
                {
                    return SurfaceSets[i];
                }
            }
            return null;
        }

        public static void RebindSurfacesAllActors()
        {
            BokuGame.bokuGame.LoadSurfaces();
            foreach (XmlGameActor actor in allActors)
            {
                actor.RefreshFromXML();
                actor.RebindSurfaces();
            }
        }

        private void RebindSurfaces()
        {
            foreach (SurfaceSet set in SurfaceSets)
            {
                set.Bind(BokuGame.bokuGame.Surfaces);
            }
        }

        private string XmlFileName { get; set; }

        /// <summary>
        /// Load a named resource, assuming file naming convention.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public static XmlGameActor Deserialize(string name)
        {
            XmlGameActor xmlActor = null;
            string xmlFileName = Prefix + name + Suffix;

            if (Storage4.FileExists(xmlFileName, StorageSource.TitleSpace))
            {
                Stream stream = Storage4.OpenRead(xmlFileName, StorageSource.TitleSpace);
                XmlSerializer serializer = new XmlSerializer(typeof(XmlGameActor));
                xmlActor = serializer.Deserialize(stream) as XmlGameActor;
                Storage4.Close(stream);
                xmlActor.XmlFileName = xmlFileName;
                if (xmlActor != null)
                {
                    xmlActor.OnLoad();
                    allActors.Add(xmlActor);
                }
            }
            else
            {
                //Debug.Assert(false, "Missing actor file");
            }
            return xmlActor;
        }

        #region refresh logic for design tools

#if NETFX_CORE
        public void RefreshFromXML()
        {
            Debug.Assert(false, "Is this needed during runtime?");
        }
#else

        public void RefreshFromXML()
        {
            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.Load(System.IO.Directory.GetCurrentDirectory() + XmlFileName);

            // note on persistence: the surface sets are in subnodes of the root xml document
            // each surface set record will hold pointers to these subnodes
            // when we save, we copy date back to the subnodes and then write the whole doc
            // back to the file.
            foreach (XmlNode node in xmlDoc.ChildNodes)
            {
                // look for the actor - should be one per file
                if (node.Name == "XmlGameActor")
                {
                    foreach (XmlNode actorNode in node.ChildNodes)
                    {
                        // we currently rely on the surface sets being in the same order, as there is no
                        // way for the user to reorder them and they should always be in load order
                        if (actorNode.Name == "SurfaceSets")
                        {
                            int curSurfSet = 0;
                            foreach (XmlNode surfSetNode in actorNode.ChildNodes)
                            {
                                if (surfSetNode.Name == "SurfaceSet")
                                {
                                    SurfaceSet curSet = this.SurfaceSets[curSurfSet];
                                    RefreshSurfaceSet(surfSetNode, curSet);
                                    curSurfSet++;
                                }
                            }
                        }
                    }
                }
            }
        }
        
        // Given an xml node and a surface set, parse the xml node and 
        // update approprate fields of the surface set.
        public void RefreshSurfaceSet(XmlNode surfSetXML, SurfaceSet surfSet)
        {
            // first get the properties of the surface set
            foreach (XmlNode node in surfSetXML.ChildNodes)
            {
                if (node.Name == "Name")
                {
                    string name = node.InnerText;
                    // keeping this around for validation; not doing anything with it at the moment.
                }
                if (node.Name == "SurfaceNames")
                {
                    string[] names = new string[8];
                    int curName = 0;
                    // now we're going to accumulate the names
                    // of all the surfaces applied to the slots in this
                    // model - note that order is paramount - different slots
                    // correspond to different body parts of the bot
                    foreach (XmlNode surfNameNode in node.ChildNodes)
                    {
                        System.Diagnostics.Debug.Assert(surfNameNode.Name == "string",
                                                        "Expected string parsing SurfaceNames in XmlActor");
                        names[curName++] = surfNameNode.InnerText;
                    }
                    surfSet.SurfaceNames = names;
                }
                else if (node.Name == "BumpDetailName")
                {
                    surfSet.BumpDetailName = node.InnerText;
                }
                else if (node.Name == "DirtMapName")
                {
                    surfSet.DirtMapName = node.InnerText;
                }
            }
        }
#endif
        #endregion

        /// <summary>
        /// Save a named resource, using file naming convention. 
        /// Currently unused, actor xml's are hand made.
        /// </summary>
        /// <param name="name"></param>
        public void Serialize(string name)
        {
            string xmlFileName = Prefix + name + Suffix;
            base.Save(xmlFileName, XnaStorageHelper.Instance);
        }
        #endregion Public

        #region Internal
        protected override bool OnLoad()
        {
            RebindSurfaces();

            return true;
        }
        #endregion Internal
    }
}
