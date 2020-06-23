// Uncomment this to dump debug spew when checking compatibility of tiles.
// Beware, this spews a lot of output.
//#define DEBUG_COMPATIBILITY

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

using System.Xml;
using System.Xml.Serialization;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Storage;

using Boku.Base;
using Boku.Common;
using Boku.UI;

namespace Boku.Programming
{
    /// <summary>
    /// Used in the tile compatibility system (aka. ReflexCompatible).
    /// See more retailed documentation at the top of CardSpace.Xml.
    /// </summary>
    public enum BrainCategories
    {
        // Indicates no category, assumed to be zero value.
        NotSpecified,

        // Sensor categories
        ExplicitSubject,
        ExplicitSubjectFilter,  // Why is this here?  ExplicitSubject is added when a
                                // sensor detects gamethings.  When the NotFilter is used
                                // ExplicitSubject is negated since we no longer have a
                                // direct object.  But, on the WHEN side we still might want
                                // to add filters that require an ExpliciteSubject.
                                // Example WHEN See Kodu Not Closeby
                                // See sets ExplicitSubject and Not excludes it.  But since it
                                // is excluded Closeby, which requires ExplicitiSubject, is no
                                // longer valid.
                                // So the solution is to have ExplicitSubjectFilter which is 
                                // set by the sensors, matches the filters, but is not excluded
                                // by the NotFilter.  Note that this should only be used to 
                                // match filters, not actuators or modifiers.
        MoversOnly,             // Used with EndOfPath to prevent Me filter when character doesn't move.
        WhenGamePad,
        WhenMouse,
        WhenTouch,
        WhenKeyBoard,
        WhenMicrobit,
        WhenUserInput,
        WhenSight,
        WhenHearing,
        WhenBump,
        WhenTimer,
        WhenGot,
        WhenScore,
        WhenScoreBucketNeeded,  // This category is added when a scorebucket is needed.  
                                // Once the scorebucket is added, this is removed.
        WhenHealth,
        WhenShotHit,
        WhenHeldBy,
        WhenOnLand,
        WhenOnWater,
        WhenOnPath,
        WhenAlways,
        WhenBeamed,
        WhenInspected,
        WhenScanned,
        WhenEndofPath,

        // Filter categories
        AnythingFilter,
        NotFilter,
        ObjectFilter,
        StaticObjectFilter, // Objects using the StaticPropChassis.
        MeFilter,
        CountFilter,
        NoneFilter,
        FewFilter,
        ManyFilter,
        AnyFilter,
        ClusterFilter,
        ColorFilter,
        ExpressionFilter,
        RangeFilter,
        RelativeFilter,     // Filter on position of target relative to current bot.
        RelativeFilterInFront,  // Instances of relative filters.  Just used to provide exclusion categories in CardSpace.Xml.
        RelativeFilterBehind,
        RelativeFilterToLeft,
        RelativeFilterToRight,
        RelativeFilterOver,
        RelativeFilterUnder,
        LineOfSightFilter,  // Filter on line of sight testing.
        MovingFilter,       // Sense bots that are moving.
        CamouflageFilter,   // Filters on camouflaged bots
        DeadFilter,         // Allows dead bots to be sensed.
        SquashedFilter,     // Allows squashed bots to be sensed.
        PathColorFilter,    // Filters on movement path color
        MissileFilter,
        GamePadFilter,
        GamePadStickFilter,
        GamePadButtonFilter,
        KeyBoardKeyFilter,
        MouseFilter,
        MouseMoveFilter,
        TouchGestureFilter,
        TapGestureFilter,
        TouchButtonFilter,
        TouchGUIButtonFilter,
        MicrobitFilter,
        MicrobitTiltFilter,
        MicrobitButtonFilter,
        MicrobitPinFilter,
        MicrobitShakeFilter,

        RotateGestureFilter,
        SwipeGestureFilter,
        SlideGestureFilter,
        PlayerFilter,
        TerrainFilter,
        WaterFilter,
        TimerFilter,
        ScoreFilter,
        ScorePresent,           // There's at least one score (or health) in the reflex so now we can allow comparison operators. 
        ScoreCompareFilter,
        SoundFilter,
        SaidFilter,
        HealthCompareFilter,
        HealthFilter,           // Should act like a scorebucketfilter but specific to health.
        MaxHealthFilter,        // archived
        ScoreBucketFilter,
        RandomFilter,
        PercentFilter,
        SettingsFilter,             // Generic type used for all SettingsFilter tiles (MaxHealth, BlipDamage, MissileDamage...)
        CursorFilter,
        MultipleDirectionFilter,    // Used for WASD and arrow keyboard filters and sticks on gamepad.
                                    // We use this to filter out things like "Forward" after WHEN Keyboard WASD DO Move ...
        DirectionFilter,
        DirectionUpFilter,
        DirectionDownFilter,
        DirectionLeftFilter,
        DirectionRightFilter,
        RotationFilter,
        ClockwiseFilter,
        CounterclockwiseFilter,
        StrengthFilter,
        StronglyFilter,
        WeaklyFilter,
        NamedFilter,            // Dynamic filter for user-named actors.

        // Actuator categories
        NoOnce,                 // Used to prevent the Once tile from showing up too soon (or at all).
                                // NoOnce is listed in the Once tile's Exclusions.
                                // Sample usage:  The Score actuator sets NoOnce as a Catagory, the
                                // ScoreBucket Modifiers Negate NoOnce.  The result is that the Once
                                // tile only shows up after the user picks a ScoreBucket.
        DoMovement,
        DoTurning,
        DoNewTurning,
        DoEat,
        DoSay,
        DoOpen,
        DoClose,
        DoLaunch,
        DoPush,
        DoSwitch,
        DoNextLevel,
        DoPreviousLevel,
        DoWaterRaise,
        DoWaterLower,
        DoSetWater,
        DoCreate,
        DoExpress,
        DoColor,
        DoGlow,
        DoSound,
        DoPlaySound,
        DoStopSound,
        DoJump,
        DoShoot,
        DoCombat,
        DoDamage,
        DoHeal,
        DoZap,
        DoPop,
        DoBoom,
        DoVanish,
        DoSquash,
        DoHolding,
        DoGrab,
        DoDrop,
        DoGive,
        DoEndGame,
        DoGameOver,
        DoVictory,
        DoReset,
        DoScoring,
        DoScore,
        DoSetScore,
        DoUnscore,
        DoCamera,
        DoCameraFollowMe,
        DoCameraIgnoreMe,
        DoCameraFirstPerson,
        DoBeam,
        DoInspect,
        DoScan,
        DoPicture,
        DoCamouflage,
        DoReScale,
        DoReScaleInstant,
        DoHoldDistance,
        DoHoldDistanceInstant,
        DoMovementSpeedModify,
        DoTurningSpeedModify,
        DoWorldLightingChange,
        DoWorldLightingChangeInstant,
        DoWorldSkyChange,
        DoWorldSkyChangeInstant,
        DoMaxHitpointsChange,
        DoBlipDamageChange,     // Archived.
        DoMissileDamageChange,  // Archived.

        DoBlipReloadTimeChange,
        DoBlipRangeChange,
        DoMissileReloadTimeChange,
        DoMissileRangeChange,
        DoCloseByRangeChange,
        DoFarAwayRangeChange,
        DoHearingRangeChange,

        DoMicrobitSay,
        DoMicrobitLights,
        DoMicrobitShow,
        DoMicrobitSetPin,
        DoMicrobitSetPwmFrequency,
        DoMicrobitSetPwmDutyCycle,

        // Selector categories
        ExplicitSelector,
        NonCardinalSelector,
        FollowPathSelector,
        CircleSelector,
        TurnSelector,
        ForwardSelector,
        WanderSelector,
        UpDownSelector,

        // Modifier categories
        SpeedModifier,
        SlowlyModifier,
        QuicklyModifier,
        StrengthModifier,
        WeaklyModifier,
        StronglyModifier,
        AngleModifier,
        LowAngleModifier,
        HighAngleModifier,
        TaskModifier,
        CombatModifier,
        ColorModifier,
        PathColorModifier,
        ScoreModifier,
        ScoreBucketModifier,
        HealthModifier,
        MaxHealthModifier,
        RandomModifier,
        PercentModifier,
        SettingsModifier,               // Shell around individual actor settings used like scores.
        SoundModifier,
        ConstrainNorthSouthModifier,
        ConstrainEastWestModifier,
        ContrainFreezeModifier,         // Note the typo.  This cannot be fixed without breaking all levels that ever used this...
        RelativeDirectionModifier,
        AbsoluteDirectionModifier,
        CardinalDirectionModifier,
        NorthModifier,
        SouthModifier,
        EastModifier,
        WestModifier,
        UpDownModifier,
        TurnModifier,
        RelativeTurnModifier,
        AbsoluteTurnModifier,
        CircleFarModifier,
        CircleNearModifier,
        MissileModifier,
        TrackingModifier,
        ResetModifier,
        ResetWorldModifier,
        ResetScoreModifier,
        ResetExpressModifier,
        ResetHealthModifier,
        ResetGlowModifier,
        PronounModifier,
        MeModifier,
        ItModifier,

        DelayedExplicitSubject,     // What this is trying to enable is to all "It" in situations like WHEN DO Move Toward It
                                    // In this case we don't want the It to be offered until after the Toward is placed.
                                    // So we have this new category which is added by Move.
                                    // This is XmlExcluded by It which means that normally this will prevent It from showing up after Move.
                                    // But, Toward, Avoid, and Away now XmlNegate this thus allowing the It to show up after them.

        OnceModifier,
        ObjectModifier,
        CreatableModifier,
        ExpressFaceModifier,
        ExpressParticleModifier,
        ExpressNoneModifier,
        MicrobitPatternModifier,
        OnOffModifier,
        FrequencyUnitsModifier,

        SIZEOF
    }

    /// <summary>
    /// This represents the base for all programming pieces.
    /// </summary>
    public abstract class ProgrammingElement : Atom
    {
        /// <summary>
        /// If true, do not show in pie selector and automatically attach to compatible reflexes if no other of its class is present.
        /// </summary>
        [XmlAttribute]
        public bool hiddenDefault;

        public const string RootGroup = "root";
        /// <summary>
        /// used for display grouping hints
        /// </summary>
        [XmlAttribute]
        public string group = RootGroup;

        [XmlIgnore]
        public CardSpace.Group groupObj;

        /// <summary>
        /// Used for ranking reflex help examples.
        /// Space-delimited list of strings.
        /// </summary>
        [XmlAttribute]
        public string[] helpGroups = new string[0];

        /// <summary>
        /// The key that defines if this specific element is visible to the user and when its visible to the user
        /// </summary>
        [XmlAttribute]
        public bool archived;

        #region ReflexCompatible

        private int categoryCount;

        private int inclusionCount;

        private int exclusionCount;

        private int negationCount;

        private int inputCount;

        private int negOutputCount;

        private int archivedInclusionCount;

        private BitArray categories = new BitArray((int)BrainCategories.SIZEOF);

        private BitArray inclusions = new BitArray((int)BrainCategories.SIZEOF);

        private BitArray exclusions = new BitArray((int)BrainCategories.SIZEOF);

        private BitArray negations = new BitArray((int)BrainCategories.SIZEOF);

        private BitArray inputs = new BitArray((int)SensorOutputType.SIZEOF);

        private BitArray negOutputs = new BitArray((int)SensorOutputType.SIZEOF);

        private BitArray archivedInclusions = new BitArray((int)BrainCategories.SIZEOF);

        [XmlIgnore]
        public int CategoryCount { get { return categoryCount; } }

        [XmlIgnore]
        public int InclusionCount { get { return inclusionCount; } }

        [XmlIgnore]
        public int ExclusionCount { get { return exclusionCount; } }

        [XmlIgnore]
        public int NegationCount { get { return negationCount; } }

        [XmlIgnore]
        public int InputCount { get { return inputCount; } }

        [XmlIgnore]
        public int NegOutputCount { get { return negOutputCount; } }

        [XmlIgnore]
        public int ArchivedInclusionCount { get { return archivedInclusionCount; } }

        [XmlIgnore]
        public BitArray Categories { get { return categories; } }

        [XmlIgnore]
        public BitArray Inclusions { get { return inclusions; } }

        [XmlIgnore]
        public BitArray Exclusions { get { return exclusions; } }

        [XmlIgnore]
        public BitArray Negations { get { return negations; } }

        [XmlIgnore]
        public BitArray Inputs { get { return inputs; } }

        [XmlIgnore]
        public BitArray NegOutputs { get { return negOutputs; } }

        [XmlIgnore]
        public BitArray ArchivedInclusions { get { return archivedInclusions; } }

        [XmlArray]
        [XmlArrayItem("Category")]
        public List<BrainCategories> XmlCategories = new List<BrainCategories>();

        [XmlArray]
        [XmlArrayItem("Category")]
        public List<BrainCategories> XmlInclusions = new List<BrainCategories>();

        [XmlArray]
        [XmlArrayItem("Category")]
        public List<BrainCategories> XmlExclusions = new List<BrainCategories>();

        [XmlArray]
        [XmlArrayItem("Category")]
        public List<BrainCategories> XmlNegations = new List<BrainCategories>();

        [XmlArray]
        [XmlArrayItem("Input")]
        public List<SensorOutputType> XmlInputs = new List<SensorOutputType>();

        [XmlArray]
        [XmlArrayItem("Output")]
        public List<SensorOutputType> XmlNegOutputs = new List<SensorOutputType>();

        [XmlArray]
        [XmlArrayItem("Category")]
        public List<BrainCategories> XmlArchivedInclusions = new List<BrainCategories>();

        [XmlElement]
        public int MaxInstanceCount = 1;

        [XmlElement]
        public int MaxClassCount = 1;

        [XmlAttribute]
        public string[] mountkey; // list of bots this element works on

        [XmlAttribute]
        public string[] mountlock; // list of bots this element will not work

        // Private scratch variables used by ReflexCompatible.
        private static BitArray scratchMyCategories = new BitArray((int)BrainCategories.SIZEOF);
        private static BitArray scratchCategories = new BitArray((int)BrainCategories.SIZEOF);
        private static BitArray scratchNegations = new BitArray((int)BrainCategories.SIZEOF);
        private static BitArray scratchOutputs = new BitArray((int)SensorOutputType.SIZEOF);
        private static BitArray scratchNegOutputs = new BitArray((int)SensorOutputType.SIZEOF);

        #endregion ReflexCompatible

        /// <summary>
        /// Create a cloned instance of this ProgramingElement
        /// It should not retain any state that is specific to the reflex, task, brain, or game actor
        /// that it is currently being used in
        /// </summary>
        /// <returns></returns>
        public abstract ProgrammingElement Clone();


        protected void CopyTo(ProgrammingElement clone)
        {
            base.CopyTo(clone);
            clone.hiddenDefault = this.hiddenDefault;
            clone.group = this.group;
            clone.groupObj = this.groupObj;
            clone.helpGroups = this.helpGroups;
            clone.archived = this.archived;
            clone.mountkey = this.mountkey;
            clone.mountlock = this.mountlock;
            clone.XmlCategories = this.XmlCategories;
            clone.XmlInclusions = this.XmlInclusions;
            clone.XmlExclusions = this.XmlExclusions;
            clone.XmlNegations = this.XmlNegations;
            clone.XmlInputs = this.XmlInputs;
            clone.XmlNegOutputs = this.XmlNegOutputs;
            clone.XmlArchivedInclusions = this.XmlArchivedInclusions;
            clone.categories = this.categories;
            clone.inclusions = this.inclusions;
            clone.exclusions = this.exclusions;
            clone.negations = this.negations;
            clone.inputs = this.inputs;
            clone.negOutputs = this.negOutputs;
            clone.archivedInclusions = this.archivedInclusions;
            clone.categoryCount = this.categoryCount;
            clone.exclusionCount = this.exclusionCount;
            clone.inclusionCount = this.inclusionCount;
            clone.negationCount = this.negationCount;
            clone.inputCount = this.inputCount;
            clone.negOutputCount = this.negOutputCount;
            clone.archivedInclusionCount = this.archivedInclusionCount;
            clone.MaxInstanceCount = this.MaxInstanceCount;
            clone.MaxClassCount = this.MaxClassCount;
        }

        public override void OnLoad()
        {
            categories.SetAll(false);
            inclusions.SetAll(false);
            exclusions.SetAll(false);
            negations.SetAll(false);
            inputs.SetAll(false);
            negOutputs.SetAll(false);
            archivedInclusions.SetAll(false);

            categoryCount = 0;
            inclusionCount = 0;
            exclusionCount = 0;
            negationCount = 0;
            inputCount = 0;
            negOutputCount = 0;
            archivedInclusionCount = 0;

            foreach (BrainCategories cat in XmlCategories)
            {
                categories.Set((int)cat, true);
                categoryCount += 1;
            }
            foreach (BrainCategories cat in XmlInclusions)
            {
                inclusions.Set((int)cat, true);
                inclusionCount += 1;
            }
            foreach (BrainCategories cat in XmlExclusions)
            {
                exclusions.Set((int)cat, true);
                exclusionCount += 1;
            }
            foreach (BrainCategories cat in XmlNegations)
            {
                negations.Set((int)cat, true);
                negationCount += 1;
            }
            foreach (SensorOutputType type in XmlInputs)
            {
                inputs.Set((int)type, true);
                inputCount += 1;
            }
            foreach (SensorOutputType type in XmlNegOutputs)
            {
                negOutputs.Set((int)type, true);
                negOutputCount += 1;
            }
            foreach (BrainCategories cat in XmlArchivedInclusions)
            {
                archivedInclusions.Set((int)cat, true);
                archivedInclusionCount += 1;
            }
        }

        /// <summary>
        /// Called on the programming element to test if the this element is compatible
        /// </summary>
        /// <param name="gameActor"></param>
        /// <returns></returns>
        public virtual bool ActorCompatible(GameActor gameActor)
        {
            Debug.Assert(mountkey == null || mountlock == null, "Can't have both!  Fix in CardSpace.Xml.");

            bool compatible = true;
            // check mountkey for inclusions
            if (gameActor != null && this.mountkey != null && this.mountkey.Length > 0)
            {
                compatible = false;
                // a single match means compatible
                for (int indexKey = 0; indexKey < this.mountkey.Length; indexKey++)
                {
                    if (gameActor.Classification.name == mountkey[indexKey])
                    {
                        compatible = true;
						break;
                    }
                }
            }
            // check the mountlock for exclusions
            if (compatible)
            {
                if (gameActor != null && this.mountlock != null && this.mountlock.Length > 0)
                {
                    // a single match means not compatible
                    for (int indexLock = 0; indexLock < this.mountlock.Length; indexLock++)
                    {
                        if (gameActor.Classification.name == mountlock[indexLock])
                        {
                            compatible = false;
                        }
                    }
                }
            }
            return compatible;
        }
        /// <summary>
        /// Check if this programming element is compatible within the given reflex and optionally replacing the given element
        /// </summary>
        /// <param name="reflex">reflex to check if it works within</param>
        /// <param name="replacedElement">optional, if provided this will replace it</param>
        /// <returns>true if compatible</returns>
        public bool ReflexCompatible(Reflex reflex, ProgrammingElement replacedElement, bool allowArchivedCategories)
        {
            return ReflexCompatible(reflex.Task.Brain.GameActor, reflex.Data, replacedElement, allowArchivedCategories);
        }
        public virtual bool ReflexCompatible(GameActor actor, ReflexData reflex, ProgrammingElement replacedElement, bool allowArchivedCategories)
        {
            // Check actor compatibility
            {
                if (!this.ActorCompatible(actor))
                    return false;
            }

            // Build datatype bitmask
            {
                string selectionUpid = (replacedElement != null) ? replacedElement.upid : null;

                scratchOutputs.SetAll(false);
                scratchNegOutputs.SetAll(false);

                // Set sensor bits
                if (reflex.Sensor != null)
                {
                    scratchOutputs.Or(reflex.Sensor.Outputs);
                }
                else
                {
                    // if no sensor, simulate a boolean output type (for "always" behavior).
                    scratchOutputs.Set((int)SensorOutputType.Boolean, true);
                }
                // Set filter bits
                foreach (Filter filter in reflex.Filters)
                {
                    // Don't consider the selected one, since we'd be replacing it.
                    if (filter.upid == selectionUpid)
                    {
                        selectionUpid = null;
                        continue;
                    }

                    scratchOutputs.Or(filter.Outputs);
                    scratchNegOutputs.Or(filter.NegOutputs);
                }
                // Set actuator bits
                if (reflex.Actuator != null)
                {
                    scratchNegOutputs.Or(reflex.Actuator.NegOutputs);
                }
                // Set selector bits
                if (reflex.Selector != null)
                {
                    scratchNegOutputs.Or(reflex.Selector.NegOutputs);
                }
                // Set modifier bits
                foreach (Modifier modifier in reflex.Modifiers)
                {
                    // Don't consider the selected one, since we'd be replacing it.
                    if (modifier.upid == selectionUpid)
                    {
                        selectionUpid = null;
                        continue;
                    }

                    scratchNegOutputs.Or(modifier.NegOutputs);
                }
            }

            // If this element is on the do side, remove negated outputs.
            // TODO (****) I just noticed that "this is Filter" is here even though filters
            // shouldn't be on the DO side.  Is this a bug or do we need this?
            if (this is Actuator || this is Selector || this is Modifier || this is Filter)
            {
                scratchOutputs.And(scratchNegOutputs.Not());
            }

            // Check datatype compatibility
            {
                if (this.InputCount > 0 && !MatchesAnyBit(scratchOutputs, this.Inputs))
                    return false;
            }

            // Build category bitmask
            int scratchExclusionCount = 0;
            {
                string selectionUpid = (replacedElement != null) ? replacedElement.upid : null;

                scratchCategories.SetAll(false);
                scratchNegations.SetAll(false);

                // Set sensor bits
                if (reflex.Sensor != null && reflex.Sensor.upid != selectionUpid)
                {
                    scratchCategories.Or(reflex.Sensor.Categories);
                    scratchNegations.Or(reflex.Sensor.Negations);
                }
                // Set actuator bits
                if (reflex.Actuator != null && reflex.Actuator.upid != selectionUpid)
                {
                    scratchCategories.Or(reflex.Actuator.Categories);
                    scratchNegations.Or(reflex.Actuator.Negations);
                }
                // Set selector bits
                if (reflex.Selector != null && reflex.Selector.upid != selectionUpid)
                {
                    scratchCategories.Or(reflex.Selector.Categories);
                    scratchNegations.Or(reflex.Selector.Negations);
                }
                // Set filter bits
                foreach (Filter filter in reflex.Filters)
                {
                    // Don't consider the selected modifier, since we'd be replacing it.
                    // Don't consider any filters after the selection either, since we need to consider only everything to its left when replacing.
                    if (filter.upid == selectionUpid)
                    {
                        selectionUpid = null;
                        break;
                    }

                    scratchCategories.Or(filter.Categories);
                    scratchNegations.Or(filter.Negations);

                    scratchExclusionCount += Math.Max(scratchExclusionCount, filter.ExclusionCount);
                }
                // Set modifier bits
                foreach (Modifier modifier in reflex.Modifiers)
                {
                    // Don't consider the selected modifier, since we'd be replacing it.
                    // Don't consider any modifiers after the selection either, since we need to consider only everything to its left when replacing.
                    if (modifier.upid == selectionUpid)
                    {
                        selectionUpid = null;
                        break;
                    }

                    scratchCategories.Or(modifier.Categories);
                    scratchNegations.Or(modifier.Negations);

                    scratchExclusionCount = Math.Max(scratchExclusionCount, modifier.ExclusionCount);
                }
            }

            // If this element is on the "do" side, remove negated categories.
            // Let Negations work on WHEN side also...
            //if (this is Actuator || this is Selector || this is Modifier)
            {
                scratchNegations.Not(); // Invert the negations.
                scratchCategories.And(scratchNegations);
            }

            // Build my inclusion bitmask
            {
                scratchMyCategories.SetAll(false);
                scratchMyCategories.Or(this.Inclusions);
                if (allowArchivedCategories)
                    scratchMyCategories.Or(this.ArchivedInclusions);
            }

#if DEBUG_COMPATIBILITY
            if (this.upid == "modifier.it")
            {
                scratchNegations.Not(); // Restore negations so they dump correctly.

                num++;
                Debug.Print(num.ToString());
                Debug.Print(this.upid);
                DumpCategories(this.Inclusions, "  inclusions");
                DumpCategories(this.Exclusions, "  exclusions");
                DumpCategories(scratchCategories, "  scratch");
                DumpCategories(scratchMyCategories, "  scratchMy");
                DumpCategories(scratchNegations, "  scratchNegations");
            }
#endif

            // Check category compatibility
            {
                if (this.InclusionCount > 0 && !MatchesAnyBit(scratchCategories, scratchMyCategories))
                    return false;
                if (this.ExclusionCount > 0 && MatchesAnyBit(scratchCategories, this.Exclusions))
                    return false;
            }

            return true;
        }

#if DEBUG_COMPATIBILITY
        int num = 0;

        private void DumpCategories(BitArray array, string label)
        {
#if !NETFX_CORE
            Debug.Print(label);
#endif
            for (int i = 0; i < array.Length; i++)
            {
                if (array.Get(i) == true)
                {
#if !NETFX_CORE
                    Debug.Print(((BrainCategories)i).ToString());
#endif
                }
            }
#if !NETFX_CORE
            Debug.Print("");
#endif
        }
#endif

        /// <summary>
        /// Check if this programming element is compatible with the other element
        /// This is often used by other other elements from within their ReflexCompatible
        /// </summary>
        /// <param name="other">other elemement to check</param>
        /// <returns>true if compatible</returns>
        public bool ElementCompatible(ProgrammingElement other)
        {
            if (this.InclusionCount > 0 && !other.MatchesAnyCategory(this.Inclusions))
                return false;
            if (this.ExclusionCount > 0 && other.MatchesAnyCategory(this.Exclusions))
                return false;

            return true;
        }

        /// <summary>
        /// Called on this element when it should be reset, usually due to task switch 
        /// </summary>
        public virtual void Reset(Reflex reflex)
        {
        }

        /// <summary>
        /// Test category sets.
        /// </summary>
        /// <param name="otherCategories"></param>
        /// <returns>true if any category in the supplied set exists in our set of categories.</returns>
        public bool MatchesAnyCategory(BitArray otherCategories)
        {
            return MatchesAnyBit(otherCategories, Categories);
        }

        /// <summary>
        /// Test category sets.
        /// </summary>
        /// <param name="left"></param>
        /// <param name="right"></param>
        /// <returns>true if any categories match</returns>
        public static bool MatchesAnyBit(BitArray left, BitArray right)
        {
            Debug.Assert(left.Length == right.Length);

            for (int i = 0; i < left.Length; ++i)
            {
                if (left.Get(i) && right.Get(i))
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Called once when the UI selects (highlights) this tile.
        /// </summary>
        [XmlIgnore]
        public UiSelector.HighlightDelegate OnHighlightDel;

        /// <summary>
        /// Called each frame while this tile is selected (highlighted in UI).
        /// </summary>
        [XmlIgnore]
        public UiSelector.HighlightDelegate WhileHighlitDel;

        /// <summary>
        /// Called once when this tile goes from highlighted to unhighlighted.
        /// </summary>
        [XmlIgnore]
        public UiSelector.HighlightDelegate OnUnHighlightDel;

        /// <summary>
        /// Helpfull operand enums in base clase for consistency and solve an XML serialization issue
        /// </summary>
        public enum Operand
        {
            NotApplicapble,
            NotEqual,
            LessThan,
            LessThanOrEqual,
            Equal,
            GreaterThanOrEqual,
            GreaterThan,
        }
        public static bool OperandCompare<T>(T lhs, Operand operand, T rhs) where T : IComparable<T> 
        {
            bool match = false;
            int comp = lhs.CompareTo(rhs);
            switch (operand)
            {
                case Operand.NotEqual:
                    match = (comp != 0);
                    break;
                case Operand.LessThan:
                    match = (comp < 0);
                    break;
                case Operand.LessThanOrEqual:
                    match = (comp <= 0);
                    break;
                case Operand.Equal:
                    match = (comp == 0);
                    break;
                case Operand.GreaterThanOrEqual:
                    match = (comp >= 0);
                    break;
                case Operand.GreaterThan:
                    match = (comp > 0);
                    break;
            }
            return match;
        }
    }
}
