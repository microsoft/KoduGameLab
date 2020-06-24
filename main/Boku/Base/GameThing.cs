using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;

using System.Xml;
using System.Xml.Serialization;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;

using KoiX;
using KoiX.Input;
using KoiX.Managers;
using KoiX.Text;

using Boku.Common;
using Boku.Common.ParticleSystem;
using Boku.Programming;
using Boku.SimWorld;
using Boku.SimWorld.Terra;
using Boku.Audio;
using Boku.Fx;
using Boku.Animatics;
using Boku.Common.Sharing;
using Boku.Input;

namespace Boku.Base
{
    public abstract class GameThing : GameObject, INeedsDeviceReset
    {
        public enum Verbs
        {
            None,
            Eat,
            Kick,
            Shoot,      // defaults to missile, archived.
            Shoot2,     // defaults to blip.
            Glow,
            Open,
            Close,
            Make,
            Express,
            SwitchTask,
            InlineTask,
            PreviousLevel,  //launch the previous level as defined in world parameters
            NextLevel,  // launch the next level as defined in world parameters
            WaterRaise,  
            WaterLower,  
            SetWater,
            Reset,
            Stun,       // temporary effect
            Kill,       // leaves carcass but can be healed, aka knocked out
            Squash,     // leaves carcass but can be healed
            Vaporize,   // permanent effect and removes carcass
            Vanish,     // permanent effect and removes carcass
            Grab,
            Drop,
            Give,
            Color,
            PlaySound,
            StopSound,
            Jump,
            Say,
            Launch,
            FloatUp,
            FloatDown,
            CameraFollowMe,
            CameraIgnoreMe,
            CameraFirstPerson,
            GameOver,
            GameVictory,
            Damage,
            Heal,
            Score,
            UnScore,
            ScoreSet,
            Beam,
            Inspect,
            Scan,
            Picture,
            Push,               // Only used by fan.
            Pull,               // Only used by fan.
            Camouflage,
            Uncamouflage,
            ReScale,
            ReScaleInstant,
            HoldDistance,
            HoldDistanceInstant,
            MovementSpeedModify,
            TurningSpeedModify,
            WorldLightingChange,
            WorldLightingChangeInstant,
            WorldSkyChange,
            WorldSkyChangeInstant,
            MaxHitpointsChange,
            BlipDamageChange,       // archived.
            MissileDamageChange,    // archived.

            BlipReloadTimeChange,
            BlipRangeChange,
            MissileReloadTimeChange,
            MissileRangeChange,
            CloseByRangeChange,
            FarAwayRangeChange,
            HearingRangeChange,

            MicrobitSay,
            MicrobitLights,
            MicrobitShow,
            MicrobitSetPin,
            MicrobitSetPwmFrequency,
            MicrobitSetPwmDutyCycle,
            SIZEOF
        }


        /// <summary>
        /// For backward compatibility with the depricated verb arbitrator system's behavior.
        /// This controls verb arbitration.
        /// </summary>
        /// <param name="category"></param>
        /// <returns></returns>
        public static bool VerbIsExclusive(Verbs verb)
        {
            switch (verb)
            {
                case Verbs.Eat:
                case Verbs.Kick:
                //case Verbs.Glow:
                //case Verbs.Open:
                //case Verbs.Close:
                case Verbs.Express:
                case Verbs.SwitchTask:
                //case Verbs.InlineTask:    // not needed since Inline is a pre-process.
                case Verbs.Grab:
                case Verbs.Drop:
                case Verbs.Give:
                //case Verbs.Color:
                case Verbs.FloatUp:
                case Verbs.FloatDown:
                case Verbs.MicrobitLights:
                case Verbs.MicrobitShow:
                    return true;

                default:
                    return false;
            }
        }

        protected Classification classification = null;

        protected Classification classificationRevealed = null;

        protected Movement movement = null;
        protected DesiredMovement desiredMovement = new DesiredMovement();

        private BaseChassis chassis = null;

        private BaseChassis _originalChassis = null; // keep a ref to the original chassis so that we can reset the active chassis on recycle.

        private Distortion _selectionAura = null;

        protected bool chassisRegistered = false;

        public enum State
        {
            Active,     // Fully updating and rendered.
            Inactive,   // Not updating and rendered.
            Paused,     // Rendered but not updating.
            Dead,       // Acts like Paused but allows us to differentiate the two. aka  KnockedOut
            Squashed,   // Acts like Paused of Dead but rendering is scaled vertically.
            //Held,       // Active but being held by another actor.
        };
        private State state = State.Inactive;
        private State pendingState = State.Inactive;

        protected bool firstPersonLastFrame = false;    // Was this thing the first person bot the previous frame?
        protected bool firstPerson = false;
        protected bool visible = true;              // Is this thing visible, ie being rendered?

        private GameActor actorHoldingThis = null;   // The actor (if any) currently holding this.  
                                                    // Change via the accessor only, not directly.
        private GameThing thingBeingHeldByThisActor = null; // The thing (if any) being held by this actor.

        protected List<AudioCue> audioCues = new List<AudioCue>();

        protected FBXModel.Setup preRender = null;
        protected Face face = null;

        private Foley.CollisionSound collisionSound = Foley.CollisionSound.plasticSoft;     // All bots default to sounding plastic.

        private float heightOffset = 0;

        // When a character closes, this is where we save what its
        // HeightOffset was before closing so we can restore it
        // upon open.
        // Note, for now this only applies to the Turtle.
        private float prevHeightOffset = 0;

        /// <summary>
        /// The following are a bit of a hack since the current arbitration system isn't flexible
        /// enough to deal with the issue.  The problem is that we want the color (and glow)
        /// actuators to be able to fire multiple times per frame EXCEPT on the same target.
        /// So, we keep a list of the targets which have had their color changed and the frame
        /// number when that change happened.
        /// 
        /// We don't want to allow multiple applications on the same target because of kode like this:
        /// 
        /// WHEN DO Color Me Red
        /// WHEN DO Color Me Blue
        /// 
        /// In this case, only the first line should ever get applied.  But if I have:
        ///
        /// WHEN See Kodu DO Color It Red
        /// WHEN See Rock DO Color It Blue
        /// 
        /// I want both to run.
        /// 
        /// Additionally there's an issue with changing a characters color multiple times per
        /// frame.  Because of the way the Twitch system works, if you continuously change the
        /// color of a character it will appear to never change.  What is happening is that the 
        /// new twitches always start with the existing color.  If a new twitch is created every
        /// frame or multiple times per frame then the color is never moved from it's starting value.
        /// 
        /// These need to be 'protected' since DoGlow is on GameActor which DoColor is on GameThing.
        /// No clue why.
        /// </summary>
        protected List<GameThing> ColorTargetList = new List<GameThing>();
        protected int ColorTargetListFrame = -1;
        protected List<GameThing> GlowTargetList = new List<GameThing>();
        protected int GlowTargetListFrame = -1;
        /// <summary>
        /// Apply same pattern as above to Open/Close verbs.  We only want one command 
        /// to be applied per target, per frame.
        /// </summary>
        protected List<GameThing> OpenCloseTargetList = new List<GameThing>();
        protected int OpenCloseTargetListFrame = -1;

        // Version is a monotonically increase number which can be used to track
        // changes in behaviour to characters.  Note you should never look for a
        // specific version, rather you should use > or >= for comparisons.  This is
        // important since this can be used to hack around a varieity of changes.
        //
        // The first use is a good example of how to use this.  Originally props (coin, star, heart, ammo)
        // appeared with a fixed rotation rate.  This could not be changed or reversed.  Also, 
        // the starting rotation could not be set in the editor.  For version >= 1 we now allow
        // the "Turn" verb to be programmed on these objects and no default turning is enabled.
        //
        // version 0 -- historical...
        // version 1 -- Change props from constant rotation to Kode controlled rotation.  (coin, star, heart, ammo)
        //
        public const int CurrentVersion = 1;    // Used by ActorFactory to reset value.
        private int version = CurrentVersion;

        #region Accessors

        public int Version
        {
            get { return version; }
            set { version = value; }    // Needed for serialization.
        }

        public FBXModel.Setup PreRender
        {
            get { return preRender; }
        }
        public Face Face
        {
            get { return face; }
        }
        public virtual AnimatorList Animators
        {
            get { return null; }
        }

        public Movement Movement
        {
            get { return movement; }
            set { movement = value; }
        }

        public DesiredMovement DesiredMovement
        {
            get { return desiredMovement; }
            set { desiredMovement = value; }
        }

        public List<AudioCue> AudioCues
        {
            get { return audioCues; }
        }

        /// <summary>
        /// Controls the bounciness of the object during collisions.
        /// 1.0 = perfect bounce
        /// 0.0 = no bounce
        /// </summary>
        public virtual float CoefficientOfRestitution
        {
            get { return 0.8f; }
            set { }
        }

        /// <summary>
        /// Kinetic Friction constant, 0=frictionless, 1=sandpaper
        /// </summary>
        public virtual float Friction
        {
            get { return 0; }
            set { }
        }

        /// <summary>
        /// Kilograms as if it matters.
        /// </summary>
        public virtual float Mass
        {
            get { return 1.0f; }
            set { }
        }

        /// <summary>
        /// The is an offset to the height added by the user in the editor.
        /// Clamped to the object's minimum height.
        /// How this is used is object dependent.
        /// </summary>
        public float HeightOffset
        {
            get { return heightOffset; }
            set { heightOffset = Math.Max(value, MinHeight - DefaultEditHeight); }
        }

        /// <summary>
        /// When a character closes, this is where we save what its
        /// HeightOffset was before closing so we can restore it
        /// upon open.
        /// Note, for now this only applies to the Turtle.
        /// </summary>
        public float PrevHeightOffset
        {
            get { return prevHeightOffset; }
            set { prevHeightOffset = value; }
        }

        /// <summary>
        /// Allows the setting of HeightOffset with any of the normal limiting.
        /// This should ONLY be used during ResetSim when we know we have good
        /// values.
        /// </summary>
        public float HeightOffsetNoLimit
        {
            set { heightOffset = value; }
        }

        /// <summary>
        /// This is the minimum height of the object.  An object at 
        /// this height should appear to be sitting on the ground.
        /// </summary>
        public virtual float MinHeight
        {
            get { return 0; }
        }

        /// <summary>
        /// This is the default height of the object in the editor.
        /// </summary>
        public virtual float DefaultEditHeight
        {
            get { return 0.2f; } // only the cursor uses this.
            set { }
        }
        /// <summary>
        /// This is the height of the object in the editor.  This combines
        /// the default edit height plus any user specified offset.
        /// </summary>
        public float EditHeight
        {
            get { return DefaultEditHeight + HeightOffset; }
        }

        /// <summary>
        /// This is the offset from the bot's height used for terrain collision testing.
        /// Defaults to 0.
        /// </summary>
        public virtual float WaistOffset
        {
            get { return 0; }
        }

        /// <summary>
        /// This is the offset in Z from the bot's origin used to play the eye when in first person mode.
        /// Defaults to 0.1.
        /// </summary>
        public virtual float EyeOffset
        {
            get { return 0.1f; }
        }


        /// <summary>
        /// Whether this bot stays above water or can fly down into it.
        /// </summary>
        public virtual bool StayAboveWater
        {
            get { return false; }
            set { }
        }

        /// <summary>
        /// Override for making this bot not visible.
        /// </summary>
        public virtual bool Invisible
        {
            get { return false; }
            set { }
        }

        /// <summary>
        /// True if other bots can't detect or interact with this bot.
        /// </summary>
        public virtual bool Ignored
        {
            get { return false; }
            set { }
        }

        /// <summary>
        /// True if other bots can't detect or interact with this bot but bot can collide
        /// </summary>
        public virtual bool Camouflaged
        {
            get { return false; }
            set { }
        }

        public virtual bool Revealed
        {
            get { return false; }
            set { }
        }

        public virtual Classification.Colors ReachedEOP
        {
            get { return Classification.Colors.None; }
            set { }
        }

        public virtual bool ReadyToProcessEOP
        {
            get { return false; }
            set { }
        }

        public abstract bool Mute { get; set; }

        public abstract bool Invulnerable { get; set; }

        public BaseChassis Chassis
        {
            get { return chassis; }
            set 
            {
                if (chassis != value)
                {
                    if (chassis != null)
                    {
                        chassis.Bind(null);
                    }
                    
                    Debug.Assert(value != null, "Obviously assuming a non-null value being set");

                    chassis = value;
                    chassis.Bind(this); /// must be bound after chassis is set to value.
                    if (chassis.FixedPosition)
                    {
                        if ((movement == null) || !(movement is FixedMovement))
                        {
                            movement = new FixedMovement(this);
                        }
                    }
                    else
                    {
                        if ((movement == null) || (movement is FixedMovement))
                        {
                            movement = new Movement(this);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// The GameActor which is currently holding this thing.
        /// </summary>
        public GameActor ActorHoldingThis
        {
            get { return actorHoldingThis; }
            set
            {
                Debug.Assert(value != this);
                actorHoldingThis = value;
                if (value != null)
                {
                    // Held things may not also hold things.
                    DoDropObject(null, Mute);
                }
            }
        }

        /// <summary>
        /// The GameThing currently being held by this actor.
        /// </summary>
        public GameThing ThingBeingHeldByThisActor
        {
            get { return thingBeingHeldByThisActor; }
            protected set { thingBeingHeldByThisActor = value; }
        }

        /// <summary>
        /// Get the "active" classification for this thing.  
        /// Warning, this can mess up.  For instance, if you set the color
        /// on this before revealing, the color won't carry over.
        /// </summary>
        public Classification Classification
        {
            get { return Revealed ? classificationRevealed : classification; }
        }

        public virtual string DisplayNameNumber
        {
            get;
            set;
        }
        public virtual float TopOffset
        {
            get { return 0.0f; }
        }

        public virtual float ReScale
        {
            get { return 1.0f; }
            set { }
        }
        public virtual float HoldDistance
        {
            get { return 1.0f; }
            set { }
        }
        public virtual Vector3 HealthBarOffset
        {
            get;
            set;
        }
        public virtual Vector3 WorldHealthBarOffset
        {
            get;
            set;
        }
        public virtual Vector3 ThoughtBalloonOffset
        {
            get;
            set;
        }
        public virtual Vector3 WorldThoughtBalloonOffset
        {
            get;
            set;
        }
        public abstract BoundingSphere BoundingSphere
        {
            get;
        }
        /// <summary>
        /// Local space collision center.
        /// </summary>
        public virtual Vector3 CollisionCenter
        {
            get { return Vector3.Zero; }
        }
        /// <summary>
        /// Local space collision radius.
        /// </summary>
        public virtual float CollisionRadius
        {
            get { return 0.0f; }
        }
        /// <summary>
        /// Local space collision sphere.
        /// </summary>
        public BoundingSphere CollisionSphere
        {
            get { return new BoundingSphere(CollisionCenter, CollisionRadius); }
        }
        /// <summary>
        /// Return the collision center transformed into world space
        /// </summary>
        public Vector3 WorldCollisionCenter
        {
            get 
            {
                GameActor actor = this as GameActor;
                if(actor == null)
                {
                    return Vector3.Transform(CollisionCenter, Movement.LocalMatrix);
                }
                else
                {
                    Vector3 center = CollisionCenter * actor.SquashScale.Z;
                    center = Vector3.Transform(center, Movement.LocalMatrix);
                    return center;
                }
            }
        }
        /// <summary>
        /// Currently ignore scale, so WorldCollisionRadius == CollisionRadius.
        /// Good to use this anyway, in case we decide to support scale.
        /// </summary>
        public float WorldCollisionRadius
        {
            get { return CollisionRadius; }
        }
        /// <summary>
        /// Collision sphere transformed into world space.
        /// </summary>
        public BoundingSphere WorldCollisionSphere
        {
            get { return new BoundingSphere(WorldCollisionCenter, WorldCollisionRadius); }
        }
        public abstract RenderObject RenderObject
        {
            get;
        }
        public State CurrentState
        {
            get { return state; }
            set { state = value; }
        }

        public State PendingState
        {
            get { return pendingState; }
            set {
                if (pendingState != value)
                {
                    pendingState = value;
                    BokuGame.objectListDirty = true;    // Force refresh.
                }
            }
        }

        /// <summary>
        /// Is this thing visible ie being rendered?
        /// </summary>
        public virtual bool Visible
        {
            get { return visible; }
            set { visible = value; }
        }

        /// <summary>
        /// True if this bot thinks it's in first person.
        /// </summary>
        public bool FirstPerson
        {
            get { return firstPerson; }
        }

        public bool ChassisRegistered
        {
            get { return chassisRegistered; }
            set { chassisRegistered = value; }
        }

        public virtual Classification.Colors ClassColor
        {
            get { return Classification.Color; }
            set {
                classification.Color = value;
                classificationRevealed.Color = value;
            }
        }

        /// <summary>
        /// This should come out of the ActorXml file
        /// </summary>
        public virtual float Cost
        {
            get { return 0.0f; }
        }

        /// <summary>
        /// What category of sound does this bot make when it collides.
        /// This should come out of the ActorXml file.
        /// </summary>
        public virtual Foley.CollisionSound CollisionSound
        {
            get { return collisionSound; }
        }

        #endregion


        public GameThing(
            string classificationName,
            BaseChassis chassis)
            : this(classificationName, classificationName, chassis)
        {
        }

        public GameThing(
            string classificationName,
            string classificationRevealedName,
            BaseChassis chassis)
            : base()
        {
            this.classification = new Classification(classificationName);
            this.classificationRevealed = new Classification(classificationRevealedName);
            this.Chassis = _originalChassis = chassis;
        }

        /// <summary>
        /// Reinitialize members that can change at runtime back to their initial
        /// values. DO NOT ALLOCATE ANYTHING IN THIS FUNCTION. This function exists
        /// so that ActorFactory can reinitialize recycled actors. ActorFactory
        /// exists so that we can avoid allocating new memory when the need for a
        /// new actor arises. Allocating memory in this function would defeat the
        /// entire reason for ActorFactory's existence.
        /// </summary>
        public virtual void InitDefaults(bool revivingDeadActor)
        {
            // These values copied from constructor initializers above.
            state = State.Inactive;
            pendingState = State.Inactive;
            firstPersonLastFrame = false;
            firstPerson = false;
            visible = true;
            heightOffset = 0;

            Chassis = _originalChassis;

            movement.InitDefaults();

            chassis.InitDefaults();

            // Glow is a state which needs to be always reset but is slipping through
            // the cracks so clear it here.
            Classification.GlowColor = Base.Classification.Colors.NotApplicable;
        }

        /// <summary>
        /// Notification this thing is about to be put into the scene. 
        /// If that's okay, return true
        /// else false and the addition will be aborted.
        /// </summary>
        /// <returns></returns>
        public virtual bool EnterScene()
        {
            return true;
        }
        /// <summary>
        /// Notification this thing is about to be removed from the world.
        /// </summary>
        public virtual void ExitScene()
        {
        }

        protected virtual void Register()
        {
            // TODO (scoy) We should only be registering objects that are currently moving.
            // Non-moving objects don't need their chassis ticked every frame. 
            InGame.inGame.RegisterChassis(this);
        }

        protected virtual void UnRegister()
        {
            InGame.inGame.UnRegisterChassis(this);
        }

        public void MakeSelected(bool on, Vector4 color)
        {
            if (on)
            {
                if (_selectionAura == null)
                {
                    _selectionAura = InGame.inGame.MakeAura(this);
                }
                _selectionAura.TintAura(color.X, color.Y, color.Z);
            }
            else
            {
                if (_selectionAura != null)
                {
                    _selectionAura.Die();
                    _selectionAura = null;
                }
            }
        }

        /// <summary>
        /// called to check if the object is NOT deactivated and NOT about to be removed
        /// </summary>
        /// <returns></returns>
        public abstract bool IsAlive();

        // SGI_MOD
        /// <summary>
        /// If we are sensible or not
        /// </summary>
        public virtual bool CanBeSensed()
        {
            return true;
        }

        /// <summary>
        /// Hook to allow gamething to react when the edit cursor is placed on/under it.
        /// Defaults to do nothing.
        /// </summary>
        public virtual void ReactToCursor()
        {
        }

        /// <summary>
        /// Returns the altitude (absolute Z position) for this object 
        /// when in edit mode.  For some bots (boats) this should be 
        /// overridden to also take into account the water level.
        /// </summary>
        public virtual float GetPreferredAltitude()
        {
            // Default to ground.
            float altitude = Chassis.EditHeight + Terrain.GetTerrainAndPathHeight(Movement.Position);

            return altitude;
        }

        public virtual void SetAltitude()
        {
            Movement.Altitude = GetPreferredAltitude();
        }

        /// <summary>
        /// Set whether I am the First Person thing. Returns true if
        /// that's a change, else false.
        /// </summary>
        /// <param name="on"></param>
        /// <returns></returns>
        public virtual bool SetFirstPerson(bool on)
        {
            if (firstPerson != on)
            {
                Visible = !on;
                firstPerson = on;

                return true;
            }
            return false;
        }

        /// <summary>
        /// Set the classification color of this object. Virtual to allow
        /// pass through notification that color has changed.
        /// </summary>
        /// <param name="color"></param>
        public virtual void SetColor(Classification.Colors color)
        {
            classification.SetColor(color);
            classificationRevealed.SetColor(color);
        }

        #region ExecuteVerb

        /// <summary>
        /// Execute the given verb.
        /// </summary>
        /// <param name="verb"></param>
        /// <param name="directObject"></param>
        /// <param name="effector"></param>
        public bool ExecuteVerb(Verbs verb, GameActor directObject, BaseAction effector)
        {
            return ExecuteVerb(verb, directObject, effector, false);
        }

        /// <summary>
        /// Execute the given verb.
        /// </summary>
        /// <param name="verb"></param>
        /// <param name="directObject"></param>
        /// <param name="effector"></param>
        /// <param name="quiet"></param>
        public bool ExecuteVerb(Verbs verb, GameActor directObject, BaseAction effector, bool quiet)
        {
            // Note the Mute setting should only affect built in sounds, not programmed sounds!
            // That is why we don't OR in Mute to quiet here.

            return IExecuteVerb(verb, directObject, effector, quiet);
        }

        protected virtual bool IExecuteVerb(Verbs verb, GameActor directObject, BaseAction effector, bool quiet)
        {
            bool supports = false;

            switch (verb)
            {
                // do stun [me|it]
                case Verbs.Stun:
                    directObject = directObject ?? this as GameActor;
                    supports = directObject.DoStun(effector, quiet, this);
                    break;

                // do kill [me|it]
                case Verbs.Kill:
                    directObject = directObject ?? this as GameActor;
                    supports = directObject.DoKill(effector, quiet, this);
                    break;

                // do squash [me|it]
                case Verbs.Squash:
                    directObject = directObject ?? this as GameActor;
                    supports = directObject.DoSquash(effector, quiet, this);
                    break;

                // do vaporize [me|it]
                case Verbs.Vaporize:
                    directObject = directObject ?? this as GameActor;
                    supports = directObject.DoVaporize(effector, quiet, this);
                    break;

                // do vanish [me|it]
                case Verbs.Vanish:
                    directObject = directObject ?? this as GameActor;
                    supports = directObject.DoVanish(effector, quiet, this);
                    break;

                // do damage [me|it]
                case Verbs.Damage:
                    {
                        directObject = directObject ?? this as GameActor;
                        bool died = false;
                        supports = directObject.DoDamage(effector, quiet, -1, this, out died);
                    }
                    break;

                // do heal [me|it]
                case Verbs.Heal:
                    {
                        directObject = directObject ?? this as GameActor;
                        bool died = false;
                        supports = directObject.DoDamage(effector, quiet, 1, this, out died);
                    }
                    break;

                // do color [me|it]
                case Verbs.Color:
                    directObject = directObject ?? this as GameActor;

                    // Is this the first application this frame?  If
                    // so, update frame counter and clear list.
                    if (Time.FrameCounter != ColorTargetListFrame)
                    {
                        ColorTargetListFrame = Time.FrameCounter;
                        ColorTargetList.Clear();
                    }

                    // If we haven't changed this target's color this
                    // frame, do so and add it to the list.
                    if (!ColorTargetList.Contains(directObject))
                    {
                        ColorTargetList.Add(directObject);
                        supports = directObject.DoColor(effector, quiet);
                    }

                    break;

                // do reset [me|it|...]
                case Verbs.Reset:
                    directObject = directObject ?? this as GameActor;
                    supports = directObject.DoReset(effector, quiet);
                    break;

                // do play sound
                case Verbs.PlaySound:
                    supports = DoPlaySound(effector, quiet);
                    break;

                // do stop sound
                case Verbs.StopSound:
                    supports = DoStopSound(effector, quiet);
                    break;

                // do say something
                case Verbs.Say:
                    supports = DoSay(effector, quiet);
                    break;

                // do camera-first-person
                case Verbs.CameraFirstPerson:
                    supports = DoCameraFirstPerson();
                    break;

                // do camera-follow-me
                case Verbs.CameraFollowMe:
                    supports = DoCameraFollowMe();
                    break;

                // do camera-ignore-me
                case Verbs.CameraIgnoreMe:
                    supports = DoCameraIgnoreMe();
                    break;

                // do game-over
                case Verbs.GameOver:
                    supports = DoGameOver();
                    break;

                // do game-victory [...]
                case Verbs.GameVictory:
                    supports = DoGameVictory(effector);
                    break;

                // do score [...]
                case Verbs.Score:
                    supports = DoScore(effector, 1);
                    break;

                // do unscore [...]
                case Verbs.UnScore:
                    supports = DoScore(effector, -1);
                    break;

                // do score [...]
                case Verbs.ScoreSet:
                    supports = DoScore(effector, 0);
                    break;

                case Verbs.PreviousLevel:
                    supports = DoPreviousLevel();
                    break;

                case Verbs.NextLevel:
                    supports = DoNextLevel(effector);
                    break;

                case Verbs.SetWater:
                    supports = DoSetWater(effector);
                    break;

                case Verbs.WaterRaise:
                    supports = DoWaterRaise(effector);
                    break;

                case Verbs.WaterLower:
                    supports = DoWaterLower(effector);
                    break;

                case Verbs.MicrobitSay:
                    supports = DoMicrobitSay(effector);
                    break;

                case Verbs.MicrobitLights:
                    supports = DoMicrobitLights(effector);
                    break;

                case Verbs.MicrobitShow:
                    supports = DoMicrobitShow(effector);
                    break;

                case Verbs.MicrobitSetPin:
                    supports = DoMicrobitSetPin(effector);
                    break;

                case Verbs.MicrobitSetPwmFrequency:
                    supports = DoMicrobitSetPwmFrequency(effector);
                    break;

                case Verbs.MicrobitSetPwmDutyCycle:
                    supports = DoMicrobitSetPwmDutyCycle(effector);
                    break;

                default:
                    return false;
            }

            return supports;
        }

        #endregion

        #region Verb Handlers (and supporting functions)

        private void ShowFailedLinkDialog(string messageKey)        
        {
            //show a warning - the level is missing
            ModularMessageDialog.ButtonHandler errorHandlerA = delegate(ModularMessageDialog dialog)
            {
                //close the dialog
                dialog.Deactivate();
            };

            string errorText = Strings.Localize(messageKey);
            string errorLabelA = Strings.Localize("textDialog.ok");
            ModularMessageDialogManager.Instance.AddDialog(errorText, errorHandlerA, errorLabelA);
        }

        public bool DoPreviousLevel()
        {
            //pause game things so we don't get infinite dialogs while we process this
            InGame.inGame.PauseAllGameThings();

            if (InGame.XmlWorldData != null &&
                InGame.XmlWorldData.LinkedFromLevel != null &&
                !BokuGame.bokuGame.loadLevelMenu.LoadingFromString)
            {
                //check if the level hasn't been saved
                if (InGame.IsLevelDirty || InGame.AutoSaved)
                {
                    //handler for "yes" - load first level
                    ModularMessageDialog.ButtonHandler handlerA = delegate(ModularMessageDialog dialog)
                    {
                        //close the dialog
                        dialog.Deactivate();

                        InGame.InGameSaveDelegate OnSaveComplete = delegate()
                        {
                            DoPreviousLevel();
                        };

                        //save the level and then re-call DoNextLevel
                        InGame.inGame.ShowInGameSaveDialog(null, OnSaveComplete);
                    };

                    //handler for "no" - load selected level
                    ModularMessageDialog.ButtonHandler handlerB = delegate(ModularMessageDialog dialog)
                    {
                        //they cancelled out, return to edit mode
                        SceneManager.SwitchToScene("EditWorldScene");

                        //close the dialog
                        dialog.Deactivate();
                    };

                    string text = Strings.Localize("inGame.saveRequiredBeforeLink");
                    string labelA = Strings.Localize("textDialog.yes");
                    string labelB = Strings.Localize("textDialog.no");
                    ModularMessageDialogManager.Instance.AddDialog(text, handlerA, labelA, handlerB, labelB);

                    return true;
                }

                if (BokuGame.bokuGame.loadLevelMenu.SkipToLevel((Guid)InGame.XmlWorldData.LinkedFromLevel, (BokuShared.Genres)InGame.XmlWorldData.genres))
                {
                    return true;
                }
            }

            //show a warning - the level is missing
            ShowFailedLinkDialog("inGame.previousLevelNotFound");

            return false;
        }

        /// <summary>
        /// Called druing RunSim when a NextLevel tile is executed.
        /// This has 2 corner cases that need to be handled well:
        /// 1)  If the link is null of invalid (level not there) then
        ///     we need to give a warning, restore the level into it's
        ///     start state, and go into edit mode.
        /// 2)  If the current level is dirty or has been autosaved then
        ///     we need to give the user the opportunity to save it since
        ///     it will be replaced.  If the user chooses to save it then
        ///     we need to be sure to save the start state of the level
        ///     and not it's current state since actors may have been 
        ///     created or destroyed during the run.  After the save we
        ///     can then load and run the linked level.
        ///     
        /// TODO (scoy) Currently, on failure this leave the game in
        /// RunSim mode but all GameThings have been paused so we're 
        /// kind of stuck.  It's not a huge problem but it's also not
        /// very clean.  A return to edit mode might be better.
        /// 
        /// TODO (scoy) This really doens't seem like it should be a 
        /// method on GameThing.  Think about refactoring it.
        /// </summary>
        /// <returns></returns>
        public bool DoNextLevel(BaseAction effector)
        {
            InGame.inGame.LinkingLevels = false;

            //pause game things so we don't get infinite dialogs while we process this
            InGame.inGame.PauseAllGameThings();

            GamePadInput.ClearAllWasPressedState();
            GamePadInput.IgnoreAllUntilReleased();
            KeyboardInput.ClearAllWasPressedState();
            KeyboardInput.IgnoreAllUntilReleased();

            // This is normally patched up on load but if the user just edited the linked
            // to level in world params, we need to grab the level from XmlWorldData.
            Guid? targetLevel = null;
            if (effector.Reflex.Data.ParamString == null)
            {
                targetLevel = InGame.XmlWorldData.LinkedToLevel;
                effector.Reflex.Data.ParamString = InGame.XmlWorldData.LinkedToLevel.ToString();
            }
            else
            {
                targetLevel = new Guid(effector.Reflex.Data.ParamString);               
            }

            if (InGame.XmlWorldData!=null && 
                targetLevel != null && 
                !BokuGame.bokuGame.loadLevelMenu.LoadingFromString)
            {
                //check if the level hasn't been saved
                if (InGame.IsLevelDirty || InGame.AutoSaved)
                {
                    InGame.inGame.LinkingLevels = true;

                    //handler for "yes" - load first level
                    ModularMessageDialog.ButtonHandler handlerA = delegate(ModularMessageDialog dialog)
                    {
                        //close the dialog
                        dialog.Deactivate();

                        // If the user chooses to save the level, then we call DoNextLevel()
                        // recursively with the expectation that it should succeed this time.
                        InGame.InGameSaveDelegate OnSaveComplete = delegate()
                        {
                            DoNextLevel(effector);
                        };

                        //save the level and then re-call DoNextLevel
                        InGame.inGame.ShowInGameSaveDialog(null, OnSaveComplete);
                    };

                    //handler for "no" - load selected level
                    ModularMessageDialog.ButtonHandler handlerB = delegate(ModularMessageDialog dialog)
                    {
                        //they cancelled out, return to edit mode
                        SceneManager.SwitchToScene("EditWorldScene");

                        //close the dialog
                        dialog.Deactivate();
                    };

                    string text = Strings.Localize("inGame.saveRequiredBeforeLink");
                    string labelA = Strings.Localize("textDialog.yes");
                    string labelB = Strings.Localize("textDialog.no");
                    ModularMessageDialogManager.Instance.AddDialog(text, handlerA, labelA, handlerB, labelB);

                    return true;
                }
                
                // Level is up to date - try to skip to next level.
                // This is the normal, success path.
                if (BokuGame.bokuGame.loadLevelMenu.SkipToLevel((Guid)InGame.XmlWorldData.LinkedToLevel, (BokuShared.Genres)InGame.XmlWorldData.genres))
                {
                    return true;
                }
            }

            //show a warning - the level is missing
            ShowFailedLinkDialog("inGame.nextLevelNotFound");

            return false;
        }   // end of DoNextLevel()

        private const float WaterDefaultTransitionTime = 2.0f;
        public bool DoSetWater(BaseAction effector)
        {
            //make sure we have a valid position and water index
            int newWaterType = effector.Reflex.SetWaterTypeIndex;

            if (this is GameActor &&
                movement != null &&
                newWaterType >= 0 &&
                newWaterType < Water.Types.Count)
            {
                Vector2 terrainPosition = new Vector2(movement.Position.X, movement.Position.Y);

                Water water = Terrain.GetWater(terrainPosition);

                //transition over time
                Vector3 speed = new Vector3(1.0f, 0.0f, 0.0f);
                effector.Reflex.ModifyHeading(this as GameActor, Modifier.ReferenceFrames.All, ref speed);

                //faster => speed will be larger, but in this case, faster => smaller transition time.  therefore, divide by speed
                float transitionTime = WaterDefaultTransitionTime / speed.Length();

                if (water != null)
                {
                    water.TransitionToWaterType(newWaterType, transitionTime);
                }
            }

            return true;
        }

        private const float WaterDefaultRaiseLowerSpeed = 0.25f;
        public bool DoWaterRaise(BaseAction effector)
        {
            if (this is GameActor && movement != null)
            {
                //apply quickly/slowly to raise/lower speeds
                Vector3 speed = new Vector3(1.0f, 0.0f, 0.0f);
                effector.Reflex.ModifyHeading(this as GameActor, Modifier.ReferenceFrames.All, ref speed);

                Vector2 terrainPosition = new Vector2(movement.Position.X, movement.Position.Y);
                InGame.inGame.Terrain.RenderToHeightMap(
                    terrainPosition,
                    5.0f,
                    Terrain.EditMode.WaterRaise,
                    WaterDefaultRaiseLowerSpeed*speed.Length());
            }

            return true;
        }

        public bool DoWaterLower(BaseAction effector)
        {
            if (this is GameActor && movement != null)
            {
                //apply quickly/slowly to raise/lower speeds
                Vector3 speed = new Vector3(1.0f, 0.0f, 0.0f);
                effector.Reflex.ModifyHeading(this as GameActor, Modifier.ReferenceFrames.All, ref speed);

                Vector2 terrainPosition = new Vector2(movement.Position.X, movement.Position.Y);
                InGame.inGame.Terrain.RenderToHeightMap(
                    terrainPosition,
                    5.0f,
                    Terrain.EditMode.WaterLower,
                    WaterDefaultRaiseLowerSpeed * speed.Length());
            }

            return true;
        }

        struct CalculatedScore
        {
            public int Value;

            // Of the following two values, only one will be used at a time.\
            // These are the target score/setting that we're calculating.
            ScoreBucket targetBucket;   // Which score we're setting.
            string targetSettingName;   // Setting we are changing.

            bool privateScore;
            public int OpCount;                 // Total number of tiles making up the score.

            public ScoreBucket TargetBucket
            {
                get { return targetBucket; }
            }

            public string TargetSettingName
            {
                get { return targetSettingName; }
            }

            public bool PrivateScore
            {
                get { return privateScore; }
            }

            public void SetTarget(ScoreBucket targetBucket, bool privateScore)
            {
                if (TargetBucket == ScoreBucket.NotApplicable && TargetSettingName == null)
                {
                    this.targetBucket = targetBucket;
                    this.privateScore = privateScore;
                }
            }

            public void SetTarget(string targetSettingName)
            {
                if (TargetBucket == ScoreBucket.NotApplicable && TargetSettingName == null)
                {
                    this.targetSettingName = targetSettingName;
                }
            }

            public CalculatedScore(int i = 0)
            {
                Value = 0;
                targetBucket = ScoreBucket.NotApplicable;
                targetSettingName = null;
                OpCount = 0;
                privateScore = false;
            }
        }   // end of struct CalculatedScore

        /// <summary>
        /// Calculates scores accumulated by modifiers ie on the DO side of the reflex.
        /// </summary>
        /// <param name="reflex"></param>
        /// <param name="wantTargetScoreBucket">If true, we assume the first scoreBucket or settingsModifier is the target so it's value is not included in the sum.</param>
        /// <param name="defaultPointsBase"></param>
        /// <returns></returns>
        CalculatedScore CalculateModifierScore(Reflex reflex, bool wantTargetScoreBucket, int defaultPointsBase)
        {
            // If the score includes things like MaxHealth or Private scores, we want to use
            // those from the actor running the kode, not the potential "it" target.  For instance:
            // WHEN Bumped Cycle DO Damage privateScoreA It
            // When I bump into a cycle I want to damage the cycle by the amount of MY privateScoreA, not it's privateScoreA.
            GameActor actor = reflex.Task.GameActor;
            List<Modifier> modifiers = reflex.Modifiers;
            CalculatedScore score = new CalculatedScore();

            Modifier specialMod = null;         // Special tile found.  Currently this will either be Random or %.
            int pointsPostSpecial = 0;          // Points found after special tile.
            int pointsPreSpecial = 0;           // Points found before the special tile.
            bool pointsPreSpecialSet = false;   // Did we find any points modifiers for the pointsPreSpecial value?

            foreach (Modifier modifier in modifiers)
            {
                ScoreBucketModifier sbm = modifier as ScoreBucketModifier;
                SettingsModifier sm = modifier as SettingsModifier;

                // Target bucket?
                if (wantTargetScoreBucket)
                {
                    if (sbm != null)
                    {
                        score.SetTarget(sbm.bucket, sbm.isPrivate);
                        wantTargetScoreBucket = false;
                        continue;
                    }
                    if (sm != null)
                    {
                        score.SetTarget(sm.name);
                        wantTargetScoreBucket = false;
                        continue;
                    }
                }

                if (sbm != null)
                {
                    if (specialMod == null)
                    {
                        pointsPreSpecial += Scoreboard.GetScore(actor, sbm);
                        pointsPreSpecialSet = true;
                        score.OpCount += 1;
                    }
                    else
                    {
                        pointsPostSpecial += Scoreboard.GetScore(actor, sbm);
                        score.OpCount += 1;
                    }
                    continue;
                }

                if (sm != null)
                {
                    int points = GetSettingsValue(actor, sm.name);
                    if (specialMod == null)
                    {
                        pointsPreSpecial += points;
                        pointsPreSpecialSet = true;
                        score.OpCount += 1;
                    }
                    else
                    {
                        pointsPostSpecial += points;
                        score.OpCount += 1;
                    }
                    continue;
                }

                if (modifier is ScoreModifier)
                {
                    ScoreModifier scoreModifier = modifier as ScoreModifier;
                    if (specialMod == null)
                    {
                        pointsPreSpecial += scoreModifier.points;
                        pointsPreSpecialSet = true;
                        score.OpCount += 1;
                    }
                    else
                    {
                        pointsPostSpecial += scoreModifier.points;
                        score.OpCount += 1;
                    }
                }

                if (modifier is HealthModifier)
                {
                    if (specialMod == null)
                    {
                        pointsPreSpecial += actor.HitPoints;
                        pointsPreSpecialSet = true;
                        score.OpCount += 1;
                    }
                    else
                    {
                        pointsPostSpecial += actor.HitPoints;
                        score.OpCount += 1;
                    }
                }

                if (modifier is RandomModifier || modifier is PercentModifier)
                {
                    Debug.Assert(specialMod == null, "We don't allow two special tiles in the same statement.");
                    specialMod = modifier;
                }
            }

            if (!pointsPreSpecialSet)
            {
                pointsPreSpecial = defaultPointsBase;
            }

            int pointsTotal = pointsPreSpecial;

            // Special modifiers?
            if (specialMod != null)
            {
                // Add in random, may be negative.
                if (specialMod is RandomModifier)
                {
                    if (pointsPostSpecial >= 0)
                    {
                        pointsTotal += BokuGame.bokuGame.rnd.Next(pointsPostSpecial);
                    }
                    else
                    {
                        pointsTotal -= BokuGame.bokuGame.rnd.Next(-pointsPostSpecial);
                    }
                    score.OpCount += 1;
                }
                else if (specialMod is PercentModifier)
                {
                    pointsTotal = (int)Math.Round((float)pointsPreSpecial / 100.0f * (float)pointsPostSpecial);
                    score.OpCount += 1;
                }
            }

            score.Value = pointsTotal;

            // This is a bit of a hack to force a default value of 1 for scores
            // where the user has programmed something like DO Score+ Red 
            // without giving any info about how many points they want to add.
            // While 0 seems like a more CS oriented default, for younger kids
            // having this default to 1 makes learning about scores easier.
            // Note that we only want to do this for incrementing or decrementing
            // scores, not for setting them.
            if (score.TargetBucket != ScoreBucket.NotApplicable && score.OpCount == 0 && (reflex.actuatorUpid == "actuator.score" || reflex.actuatorUpid == "actuator.unscore"))
            {
                score.Value = 1;
            }

            return score;
        }   // end of CalculateModifierScore()

        /// <summary>
        /// Gets the current value of an actor's setting by name.
        /// </summary>
        /// <param name="actor"></param>
        /// <param name="settingsName"></param>
        /// <returns></returns>
        public int GetSettingsValue(GameActor actor, string settingsName)
        {
            int result = 0;

            switch(settingsName)
            {
                case "maxhealth":
                    result = actor.MaxHitPoints;
                    break;
                case "blipdamage":
                    result = actor.BlipDamage;
                    break;
                case "missiledamage":
                    result = actor.MissileDamage;
                    break;
                default:
                    Debug.Assert(false, "Typo???");
                    break;
            }

            return result;
        }   // end of GetSettingsValue()

        /// <summary>
        /// Set teh value of an actor's setting by name.
        /// </summary>
        /// <param name="actor"></param>
        /// <param name="settingsName"></param>
        /// <param name="value"></param>
        public void SetSettingsValue(GameActor actor, string settingsName, int value)
        {
            switch (settingsName)
            {
                case "maxhealth":
                    actor.MaxHitPoints = value;
                    break;
                case "blipdamage":
                    actor.BlipDamage = value;
                    break;
                case "missiledamage":
                    actor.MissileDamage = value;
                    break;
                default:
                    Debug.Assert(false, "Typo???");
                    break;
            }
        }   // end of SetSettingsValue()

        /// <summary>
        /// Do the "score" action which sets a score or a setting.
        /// </summary>
        /// <param name="effector"></param>
        /// <param name="scalar">-1, 0, or 1, used to differentiate deccrement, set, and increment</param>
        /// <returns></returns>
        public bool DoScore(BaseAction effector, int scalar)
        {
            GameThing targetThing = (effector.GameThing != null) ? effector.GameThing : effector.Reflex.Task.GameActor;
            GameActor actor = effector.Reflex.Task.GameActor;   // Actor who's scores we want to affect.
            CalculatedScore score = CalculateModifierScore(effector.Reflex, true, scalar == 0 ? 1 : 0);
            
            // If no target was set, default to Red.
            if (score.TargetBucket == ScoreBucket.NotApplicable && score.TargetSettingName == null)
            {
                score.SetTarget((ScoreBucket)(Classification.Colors.Red), privateScore: false);
            }

            // Scalar used to either set, add to, or subtract from, score.
            // scalar == 0 means just set score.
            if (scalar == 0)
            {
                if (score.TargetBucket != ScoreBucket.NotApplicable)
                {
                    // Score
                    if (score.PrivateScore)
                    {
                        actor.localScores.SetScore(score.TargetBucket, score.Value);
                    }
                    else
                    {
                        Scoreboard.SetScore(score.TargetBucket, score.Value, targetThing);
                    }
                }
                else
                {
                    // Setting
                    SetSettingsValue(actor, score.TargetSettingName, score.Value);
                }
            }
            else
            {
                score.Value *= scalar;  // Will be either 1 or -1.  So, it's just setting the sign.
                if (score.TargetBucket != ScoreBucket.NotApplicable)
                {
                    // Score
                    if (score.PrivateScore)
                    {
                        score.Value += actor.localScores.GetScore(score.TargetBucket);
                        actor.localScores.SetScore(score.TargetBucket, score.Value);
                    }
                    else
                    {
                        Scoreboard.SetScore(score.TargetBucket, Scoreboard.GetGlobalScore(score.TargetBucket) + score.Value, targetThing);
                    }
                }
                else
                {
                    // Setting
                    int curValue = GetSettingsValue(actor, score.TargetSettingName);
                    SetSettingsValue(actor, score.TargetSettingName, curValue + score.Value);
                }
            }

            return true;
        }   // end of DoScore()

        public bool DoGameOver()
        {
            InGame.inGame.GameOver = true;
            return true;
        }

        public bool DoGameVictory(BaseAction effector)
        {
            bool bucketSpecified = false;
            bool isColorBucket = false;

            ScoreBucket bucket = Modifier.ScoreBucketFromModifierSet(effector.Reflex.Modifiers, ref bucketSpecified, ref isColorBucket);

            PlayerModifier playerMod = effector.Reflex.GetModifierByType(typeof(PlayerModifier)) as PlayerModifier;

            if (bucketSpecified && isColorBucket)
                InGame.inGame.VictoryTeam = (Classification.Colors)bucket;
            else if (playerMod != null)
                InGame.inGame.VictoryPlayer = playerMod.playerIndex;
            else
                InGame.inGame.VictoryWinner = true;

            return true;
        }

        public bool DoCameraIgnoreMe()
        {
            CameraInfo.AddIgnoreMe(this as GameActor);
            return true;
        }

        public bool DoCameraFollowMe()
        {
            CameraInfo.AddFollowMe(this as GameActor);
            return true;
        }

        public bool DoCameraFirstPerson()
        {
            CameraInfo.AddFirstPerson(this as GameActor);
            return true;
        }

        protected bool DoSay(BaseAction effector, bool quiet)
        {
            quiet |= this.Mute;

            Debug.Assert(effector != null);

            Classification.Colors color;

            if (effector.Reflex.ModifierParams.HasColor)
                color = effector.Reflex.ModifierParams.Color;
            else
                color = Classification.Colors.Black;

            // Check for random color...
            if (color == Classification.Colors.NotApplicable)
            {
                color = Classification.RandomColor();
            }

            Vector4 borderColor = Classification.ColorVector4(color);

            string text = effector.Reflex.Data.sayString;
            int mode = effector.Reflex.Data.sayMode;
            TextHelper.Justification justification = effector.Reflex.Data.sayJustification;

            Debug.Assert((text != null), "Improper parameter?");
            if (text != null)
            {
                if (mode == 0)
                {
                    // Since we're going into a modal dialog, clear the input state
                    // for buttons A & B otherwise we'll just exit right back out.
                    GamePadInput pad = GamePadInput.GetGamePad0();
                    pad.ButtonA.ClearAllWasPressedState();
                    pad.ButtonB.ClearAllWasPressedState();

                    Debug.Assert(false);
                    /*
                    // Give the text to the small display first,  if it
                    // indicates that it is overflowing then give the
                    // message to the scrollable version instead.
                    InGame.inGame.shared.smallTextDisplay.Activate(this as GameActor, text, justification, useBackgroundThumbnail: true, useRtCoords: false);
                    if (InGame.inGame.shared.smallTextDisplay.Overflow)
                    {
                        InGame.inGame.shared.smallTextDisplay.Deactivate();
                        InGame.inGame.shared.scrollableTextDisplay.Activate(this as GameActor, text, justification, useBackgroundThumbnail: true, useRtCoords: false);
                    }
                    */
                }
                else
                {
                    int prevLine = effector.Reflex.Data.sayLine;

                    // Split the text, but only at \n
                    string line = null;

                    // Picks lines based on mode, either sequentially or randomly.
                    if (mode == 1)
                    {
                        // Pick next line sequentially.  We want to skip blank lines.
                        int i = effector.Reflex.Data.sayLine;
                        // If there's lots of blanks we may need to check up to Count times.
                        // After that we must just have all blanks.
                        for (int l = 0; l < effector.Reflex.SayStrings.Count; l++)
                        {
                            line = effector.Reflex.SayStrings[i];
                            i = (i + 1) % effector.Reflex.SayStrings.Count;
                            effector.Reflex.Data.sayLine = i;

                            // Found a good one?
                            if (line != null && line != "" && line != "\n")
                                break;
                        }
                    }
                    else
                    {
                        if (effector.Reflex.SayStrings.Count > 0)
                        {
                            // Pick random line but ignore what we previously showed.
                            int i = 0;
                            do
                            {
                                i = BokuGame.bokuGame.rnd.Next(effector.Reflex.SayStrings.Count);
                            } while (effector.Reflex.SayStrings.Count > 1 && i == effector.Reflex.Data.sayLine);
                            effector.Reflex.Data.sayLine = i;
                            line = effector.Reflex.SayStrings[i];

                            // If line is blank, just search linearly for next non-blank one.
                            if (line == null || line == "" || line == "\n")
                            {
                                for (int l = 0; l < effector.Reflex.SayStrings.Count; l++)
                                {
                                    line = effector.Reflex.SayStrings[i];
                                    i = (i + 1) % effector.Reflex.SayStrings.Count;
                                    effector.Reflex.Data.sayLine = i;

                                    // Found a good one?
                                    if (line != null && line != "" && line != "\n")
                                        break;
                                }
                            }
                        }

                    }
                    
                    // Display the line we picked.
                    // Create the thought balloon.  If ignored, roll back the line we're on.
                    if (!ThoughtBalloonManager.CreateThoughtBalloon(this, line, borderColor))
                    {
                        effector.Reflex.Data.sayLine = prevLine;
                    }

                }

            }

            return true;
        }

        protected bool DoPlaySound(BaseAction effector, bool quiet)
        {
            if (BokuGame.Audio.Enabled)
            {
                // Note the Mute setting should only affect built in sounds, not programmed sounds!
                // That is why we don't OR in Mute to quiet here.

                if (!quiet && effector.Reflex.ModifierParams.HasSoundUpid)
                {
                    string soundUpid = effector.Reflex.ModifierParams.SoundUpid;

                    if (!String.IsNullOrEmpty(soundUpid))
                    {
                        BokuGame.Audio.Play(soundUpid, this, audioCues);
                    }
                }
            }

            return true;
        }

        protected bool DoStopSound(BaseAction effector, bool quiet)
        {
            if (BokuGame.Audio.Enabled)
            {
                quiet |= this.Mute;

                if (effector.Reflex.ModifierParams.HasSoundUpid)
                {
                    string soundUpid = effector.Reflex.ModifierParams.SoundUpid;

                    if (!String.IsNullOrEmpty(soundUpid))
                    {
                        BokuGame.Audio.Stop(soundUpid, audioCues);
                    }
                }
            }

            return true;
        }

        public void OnAudioCueComplete(AudioCue cue)
        {
            audioCues.Remove(cue);
        }

        protected AudioCue FindActiveAudioCue(string name)
        {
            if (!String.IsNullOrEmpty(name))
            {
                for (int i = 0; i < audioCues.Count; ++i)
                {
                    AudioCue cue = audioCues[i];

                    if (cue.Name.Equals(name))
                    {
                        return cue;
                    }
                }
            }
            return null;
        }

        protected bool DoColor(BaseAction effector, bool quiet)
        {
            quiet |= this.Mute;

            bool supports = false;
            Classification.Colors color = Classification.Colors.SIZEOF;

            if (effector.Reflex.ModifierParams.HasColor)
            {
                color = effector.Reflex.ModifierParams.Color;

                if (color == Classification.Colors.NotApplicable)
                {
                    // reflex specified a random color
                    color = Classification.RandomColor();
                }
            }

            if (color == Classification.Colors.SIZEOF)
            {
                // A color wasn't specified in the reflex.
                if (effector.GameThing != null)
                {
                    // Color me the color of the sensed actor.
                    color = effector.GameThing.Classification.Color;
                    supports = true;
                }
            }
            else
            {
                // A color was specified in the reflex.
                SetColor(color);
                supports = true;
            }

            return supports;
        }

        /// <summary>
        /// temporary effect
        /// If override, call this base first then modify state as Reset may do that also 
        /// </summary>
        /// <returns></returns>
        protected virtual bool DoStun(BaseAction effector, bool quiet, GameThing executor)
        {
            quiet |= this.Mute;

            bool supports = true;

            if (DistortionManager.EnabledSM3)
            {
                if (InGame.inGame.IsTheFirstPerson(this))
                {
                    /// Cool first person effect.
                    FirstPersonEffectMgr.FPEDistort.Pulse(this);
                }
                else
                {
                    InGame.inGame.DistortionZap(this, quiet);
                }
            }
            else
            {
                ExplosionManager.CreateExplosion(Movement.Position + new Vector3(0, 0, BoundingSphere.Radius), 2f);
            }

            if (!quiet)
            {
                Foley.PlayStun(this);
            }

            return supports;
        }


        /// <summary>
        /// Permanent effect, leave a carcass
        /// If override, call this base first then modify state as Reset may do that also
        /// </summary>
        /// <returns></returns>
        protected virtual bool DoKill(BaseAction effector, bool quiet, GameThing executor)
        {
            if (Invulnerable && executor != this)
                return false;

            quiet |= this.Mute;

            bool supports;

            // Don't attempt to kill ourselves if we're already dead or dying.
            if (this.CurrentState == State.Dead || this.PendingState == State.Dead || this.CurrentState == State.Squashed || this.PendingState == State.Squashed)
            {
                supports = false;
            }
            else
            {
                supports = true;

                ExplosionManager.CreateExplosion(Movement.Position, 0.6f);

                if (!quiet)
                {
                    Foley.PlayPop(this);
                }
            }

            return supports;
        }

        /// <summary>
        /// Flatten character.
        /// If override, call this base first then modify state as Reset may do that also
        /// </summary>
        /// <returns></returns>
        protected virtual bool DoSquash(BaseAction effector, bool quiet, GameThing executor)
        {
            if (Invulnerable && executor != this)
                return false;

            quiet |= this.Mute;

            bool supports = false;

            // Don't attempt to squash ourselves if we're already dead or dying.
            if (this.CurrentState == State.Dead || this.PendingState == State.Dead || this.CurrentState == State.Squashed || this.PendingState == State.Squashed)
            {
                supports = false;
            }
            else
            {
                // Are we close enough?
                bool inRange = false;
                GameActor squashActor = effector != null ? effector.GameThing as GameActor : null;

                if (squashActor != null && !(squashActor is NullActor))
                {
                    Vector3 squashActorPos = squashActor.WorldCollisionCenter;

                    GameActor actor = executor as GameActor;
                    if (actor != null)
                    {
                        float totalGrabRange = actor.TotalGrabRange(squashActor);

                        Vector3 delta = actor.ToClosest(squashActorPos);
                        float distance = delta.Length();

                        inRange = distance <= totalGrabRange;
                    }
                }
                else
                {
                    // If squashActor is null then we must be squashing ourself triggered by a sensor
                    // that's not looking at another character, eg button press rather than bump.
                    // In this case, inRange is always true.
                    inRange = true;
                }

                if (inRange)
                {
                    supports = true;

                    if (!quiet)
                    {
                        // TODO (scoy) Need to find a cool squash sound.
                        Foley.PlayPop(this);
                    }
                }
            }

            return supports;
        }   // end of DoSquash()

        /// <summary>
        /// Permanent effect, remove from game
        /// If override, call this base first then modify state as Reset may do that also
        /// </summary>
        /// <returns></returns>
        protected virtual bool DoVaporize(BaseAction effector, bool quiet, GameThing executor)
        {
            if (Invulnerable && executor != this)
                return false;

            quiet |= this.Mute;

            bool supports = true;

            // Create explosion visual before deactivation so that the position is valid.
            ExplosionManager.CreateExplosion(Movement.Position, 1.2f);

            // Shutdown character and queue for removal from scene.
            Deactivate();

            if (!quiet)
            {
                // Play explosion sound after deactivation so that the sound isn't stopped in the deactivation step.
                Foley.PlayBoom(this);
            }

            return supports;
        }

        /// <summary>
        /// Permanent effect, remove from game.
        /// No associated visual or audio effects.
        /// </summary>
        /// <returns></returns>
        protected virtual bool DoVanish(BaseAction effector, bool quiet, GameThing executor)
        {
            if (Invulnerable && executor != this)
                return false;

            quiet |= this.Mute;

            bool supports = true;

            // No audio/visuals
            Deactivate();

            if (!quiet)
            {
                Foley.PlayVanish(this);
            }

            return supports;
        }

        /// <summary>
        /// Used for both damage and healing...
        /// </summary>
        /// <param name="effector"></param>
        /// <param name="quiet"></param>
        /// <param name="scalar"></param>
        /// <param name="executor"></param>
        /// <param name="died"></param>
        /// <returns></returns>
        public bool DoDamage(BaseAction effector, bool quiet, int scalar, GameThing executor, out bool died)
        {
            died = false;

            if (Invulnerable && executor != this)
                return false;

            CalculatedScore damageDelta = CalculateModifierScore(effector.Reflex, false, 0); 

            /// If this is from a missile hit, we've alreday played the "effect".
            /// But we still want an effect if the damage was because of a timer
            /// or button press or something.
            bool doEffect = !(effector.Reflex.Sensor is MissileHitSensor);

            // Damage or Heal the actor
            return DoDamage(damageDelta.Value * scalar, GameThing.Verbs.Vaporize, doEffect, quiet, executor, out died);
        }   // end of DoDamage()


        public bool DoMicrobitSay(BaseAction effector)
        {
            // Get the player number, if its on the rule.
            PlayerModifier playerModifier = effector.GetPlayerModifierOrNull();
            GamePadSensor.PlayerId playerId = (playerModifier != null) ? playerModifier.playerIndex : GamePadSensor.PlayerId.All;

            string str = String.Join(" ", effector.Reflex.SayStrings);

#if !NETFX_CORE
            if (playerId == GamePadSensor.PlayerId.All)
            {
                foreach (var bit in MicrobitManager.Microbits.Values)
                {
                    bit.ScrollText(str);
                }
            }
            else
            {
                Microbit bit = MicrobitExtras.GetMicrobitOrNull(playerId);
                if (bit != null)
                {
                    bit.ScrollText(str);
                }
            }
#endif
            return true;
        }

        public bool DoMicrobitLights(BaseAction effector)
        {
            // Get the player number, if its on the rule.
            PlayerModifier playerModifier = effector.GetPlayerModifierOrNull();
            GamePadSensor.PlayerId playerId = (playerModifier != null) ? playerModifier.playerIndex : GamePadSensor.PlayerId.All;

            // TODO Microbit
            // Calc "power" for lights.
            // The power is the sum of the modifier elements on the reflex and is clamped to 0..100 range.
            int power = 0;
            foreach (Modifier modifier in effector.Reflex.Modifiers)
            {
                // Is this points?
                ScoreModifier scoreModifier = modifier as ScoreModifier;
                if(scoreModifier != null)
                {
                    power += scoreModifier.points;
                }

                // Is this a scorebucket?
                ScoreBucketModifier scoreBucketModifier = modifier as ScoreBucketModifier;
                if (scoreBucketModifier != null)
                {
                    power += Scoreboard.GetScore(this as GameActor, scoreBucketModifier);
                }

                // Is this health?
                HealthModifier healthModifier = modifier as HealthModifier;
                if (healthModifier != null)
                {
                    GameActor actor = this as GameActor;
                    if (actor != null)
                    {
                        power += actor.HitPoints;
                    }
                }

                MaxHealthModifier maxHealthModifier = modifier as MaxHealthModifier;
                if (maxHealthModifier != null)
                {
                    GameActor actor = this as GameActor;
                    if (actor != null)
                    {
                        power += actor.MaxHitPoints;
                    }
                }

            }
            power = (int)MathHelper.Clamp(power, 0, 100);

            switch(effector.Reflex.actuatorUpid)
            {
                case "actuator.microbit.block":
                    // TODO Microbit
                    // Light all lights with power for brightness.
                    break;

                case "actuator.microbit.sequential":
                    // TODO Microbit
                    // Sequentially turn on lights to represent power value;
                    break;
            }

            return true;
        }   // end of DoMicrobitLights()

        /// <summary>
        /// Shows a pattern of lights on the Microbit's 5x5 display.
        /// Note, this tile needs to be followed by 1 or more MicrobitPatternModifier
        /// tiles which containt the patterns to display.
        /// 
        /// This tile, run by itself will not change the display.
        /// </summary>
        /// <param name="effector"></param>
        /// <returns></returns>
        public bool DoMicrobitShow(BaseAction effector)
        {
            // Get the player number, if its on the rule.
            PlayerModifier playerModifier = effector.GetPlayerModifierOrNull();
            GamePadSensor.PlayerId playerId = (playerModifier != null) ? playerModifier.playerIndex : GamePadSensor.PlayerId.All;

#if !NETFX_CORE
            List<MicroBitDisplayFrame> frames = new List<MicroBitDisplayFrame>();
            foreach (var pattern in effector.Reflex.MicrobitPatterns)
            {
                MicroBitDisplayFrame frame = new MicroBitDisplayFrame(pattern.LEDs, pattern.Duration, pattern.Brightness);
                frames.Add(frame);
            }

            if (playerId == GamePadSensor.PlayerId.All)
            {
                foreach (var bit in MicrobitManager.Microbits.Values)
                {
                    bit.PrintDisplayFrames(frames);
                }
            }
            else
            {
                Microbit bit = MicrobitExtras.GetMicrobitOrNull(playerId);
                if (bit != null)
                {
                    bit.PrintDisplayFrames(frames);
                }
            }
#endif
            return true;
        }

        public bool DoMicrobitSetPin(BaseAction effector)
        {
            // Get the player number, if its on the rule.
            PlayerModifier playerModifier = effector.GetPlayerModifierOrNull();
            GamePadSensor.PlayerId playerId = (playerModifier != null) ? playerModifier.playerIndex : GamePadSensor.PlayerId.All;

            int pin = int.Parse(effector.Reflex.Actuator.upid[effector.Reflex.Actuator.upid.Length - 1].ToString());
            pin -= 1;

#if !NETFX_CORE
            Microbit bit = MicrobitExtras.GetMicrobitOrNull(playerId);
            if (bit != null)
            {
                int value = 0;
                foreach (var modifier in effector.Reflex.Modifiers)
                {
                    if (modifier.upid == "modifier.on")
                    {
                        value = 1;
                    }
                    else if (modifier.upid == "modifier.off")
                    {
                        value = 0;
                    }
                }

                bit.SetPinValue(pin, value, Microbit.EPinOperatingMode.Digital);
            }
#endif
            return true;
        }

        public bool DoMicrobitSetPwmFrequency(BaseAction effector)
        {
            // Get the player number, if its on the rule.
            PlayerModifier playerModifier = effector.GetPlayerModifierOrNull();
            GamePadSensor.PlayerId playerId = (playerModifier != null) ? playerModifier.playerIndex : GamePadSensor.PlayerId.All;

            // Parse pin number from upid.
            int pin = int.Parse(effector.Reflex.Actuator.upid[effector.Reflex.Actuator.upid.Length - 1].ToString());
            pin -= 1;

#if !NETFX_CORE
            // Get the microbit.
            Microbit bit = MicrobitExtras.GetMicrobitOrNull(playerId);
            if (bit != null)
            {
                // Calculate the frequency value from the score and score bucket modifiers present.
                CalculatedScore score = CalculateModifierScore(effector.Reflex, false, 0);

                // If no value was found on the rule, default to value of 1.
                if (score.OpCount == 0)
                {
                    score.Value = 1;
                }

                // Get units
                int multiplier = 1;
                NumericModifier kHzModifier = effector.Reflex.GetModifier<NumericModifier>("modifier.frequencyunits.khz");
                if (kHzModifier != null)
                {
                    multiplier = (int)kHzModifier.value;
                }

                // Set the frequency.
                bit.SetPinPwmFrequency(pin, score.Value, multiplier);
            }
#endif
            return true;
        }

        public bool DoMicrobitSetPwmDutyCycle(BaseAction effector)
        {
            // Get the player number, if its on the rule.
            PlayerModifier playerModifier = effector.GetPlayerModifierOrNull();
            GamePadSensor.PlayerId playerId = (playerModifier != null) ? playerModifier.playerIndex : GamePadSensor.PlayerId.All;

            // Parse pin number from upid.
            int pin = int.Parse(effector.Reflex.Actuator.upid[effector.Reflex.Actuator.upid.Length - 1].ToString());
            pin -= 1;

#if !NETFX_CORE
            // Get the microbit.
            Microbit bit = MicrobitExtras.GetMicrobitOrNull(playerId);
            if (bit != null)
            {
                // Calculate the frequency value from the score and score bucket modifiers present.
                CalculatedScore score = CalculateModifierScore(effector.Reflex, false, 0);

                // If no value was found on the rule, default to value of 50%.
                if (score.OpCount == 0)
                {
                    score.Value = 50;
                }

                // Clamp the value to a valid percentage value.
                score.Value = MyMath.Clamp(score.Value, 0, 100);

                // Set the pulse width.
                bit.SetPinPwmDutyCycle(pin, score.Value / 100.0f);
            }
#endif
            return true;
        }

        #endregion

        #region Stubbed Verb Handlers

        public virtual bool DoDamage(int amount, Verbs killVerb, bool doEffect, bool quiet, GameThing executor, out bool died)
        {
            died = false;
            return false;
        }

        public virtual bool DoDropObject(GameThing dropThing, bool quiet)
        {
            return false;
        }

        #endregion

        #region Verb Collateral Handlers

        public void DoGrabbed(bool quiet)
        {
            quiet |= this.Mute;

            Chassis.Moving = false;
        }

        public void DoDropped(bool quiet)
        {
            quiet |= this.Mute;

            Chassis.Moving = true;
        }

        public bool DoEaten(bool quiet)
        {
            quiet |= this.Mute;

            DoDropObject(null, quiet);

            Deactivate();

            return true;
        }

        public bool DoKicked(GameThing kicker, Vector3 velocity, bool quiet)
        {
            quiet |= this.Mute;

            if (Classification.physicality == Classification.Physicalities.Static)
            {
                return false;
            }

            if (!quiet)
            {
                Foley.PlayKick(kicker);
            }

            /// Make the transfer of velocity in the kicker's frame of reference.
            velocity += kicker.Movement.Velocity;

            Movement.Velocity = velocity;
            Chassis.Moving = true;

            // If Boku did the kicking have Boku track where he's kicked the thing.
            GameActor actor = kicker as GameActor;
            if (actor != null)
            {
                Vector3 target = kicker.Movement.Position + new Vector3(velocity.X, velocity.Y, 0.0f);
                actor.DirectGaze(target);
            }

            return true;
        }

        #endregion


        /// <summary>
        /// Necessary for surviving device reset.  Any parts of the object that
        /// are device dependent should be created here.
        /// </summary>
        /// <param name="graphics"></param>
        public abstract void LoadContent(bool immediate);
        public abstract void InitDeviceResources(GraphicsDevice device);

        /// <summary>
        /// Necessary for surviving device reset.  Any parts of the object that
        /// are device dependent should be destroyed here.
        /// </summary>
        public abstract void UnloadContent();

        /// <summary>
        /// Recreate render targets
        /// </summary>
        /// <param name="graphics"></param>
        public virtual void DeviceReset(GraphicsDevice device)
        {
        }

        [Conditional("DEBUG")]
        public virtual void DebugDisplay(Camera camera)
        {
            bool doAnything = false;
            if (doAnything)
            {
                BoundingSphere bound = BoundingSphere;
                bound.Center = Vector3.Transform(bound.Center, Movement.LocalMatrix);
                // Assumes uniform scaling.
                bound.Radius *= Movement.LocalMatrix.Up.Length();

                bool showBound = false;
                if (showBound)
                {
                    Utils.DrawSphere(camera, bound.Center, bound.Radius);
                }
                bool showAxis = true;
                if (showAxis)
                {
                    Utils.DrawAxes(camera, Movement.LocalMatrix);
                }
            }
        } // end DebugDisplay
    }
}
