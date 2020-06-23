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
using Boku.SimWorld;
using Boku.SimWorld.Chassis;

namespace Boku.Programming
{
    /// <summary>
    /// This stucture is held by the parent reflex, and is passed to each modifier when entering run mode
    /// to gather up all the modifier parameters so that we don't have to repeatedly find them every frame.
    /// (****) Of course this makes modifiers totally useless for anything where their value may be
    /// changing over the course of a run, for instance making Scores or Health a modifier.  Also, the
    /// implementation assumes that modifier values are fixed at compile time.  You can't even have them
    /// be user settable.
    /// </summary>
    public class ModifierParams
    {
        private Classification.Colors color;

        public float speedModifier = 1.0f;                          // Multiplier applied to base chassis speed.  Defaults to 1.0.
        public float Loft;
        public float Strength;
        public Guid CreatableId;
        public PronounModifier.Pronouns Pronoun;
        public ResetModifier.Resets Reset;
        public PitchModifier.PitchDirections Pitch;
        public TurnModifier.TurnDirections Turn;
        public MakeObjectModifier.MakeObjects Make;
        public ObjectModifier.ModifierObjects Item;
        public TaskModifier.TaskIds TaskId;
        public Programming.Directions Direction;
        public Face.FaceState Facial;
        public ExpressModifier.Emitters ExpressEmitter;
        public GameThing.Verbs Verb;
        public MissileChassis.BehaviorFlags MissileBehavior;
        public ConstraintModifier.Constraints Constraints;
        public string SoundUpid;
        public int Points;
        public GamePadSensor.PlayerId PlayerIndex;
        public ScoreBucket ScoreBucket;

        /// <summary>
        /// This should be the accumulated multiplier based on the
        /// effect of any Quickly or Slowly tiles on this reflex.
        /// 
        /// Default value is 1.0.
        /// </summary>
        public float SpeedModifier
        {
            get { return speedModifier; }
            set 
            {
                Debug.Assert(value != 0, "This should never be set to 0.  Default value is 1.0./");
                speedModifier = value; 
            }
        }

        public Classification.Colors Color
        {
            get
            {
                if (HasColor)
                    return color;
                else
                    return Classification.Colors.NotApplicable;
            }
            set
            {
                color = value;
                ScoreBucket = (ScoreBucket)value;
            }
        }


        public bool HasLoft { get { return Loft != 0.0f; } }
        public bool HasStrength { get { return Strength != 0.0f; } }
        public bool HasCreatableId { get { return CreatableId != Guid.Empty; } }
        public bool HasPronoun { get { return Pronoun != PronounModifier.Pronouns.None; } }
        public bool HasReset { get { return Reset != 0; } }
        public bool HasColor { get { return color != Classification.Colors.None;  } }
        public bool HasPitch { get { return Pitch != PitchModifier.PitchDirections.None; } }
        public bool HasTurn { get { return Turn != TurnModifier.TurnDirections.None; } }
        public bool HasMake { get { return Make != MakeObjectModifier.MakeObjects.None; } }
        public bool HasItem { get { return Item != ObjectModifier.ModifierObjects.None; } }
        public bool HasTaskId { get { return TaskId != TaskModifier.TaskIds.SIZEOF; } }
        public bool HasDirection { get { return Direction != Programming.Directions.None; } }
        public bool HasFacial { get { return Facial != Face.FaceState.NotApplicable; } }
        public bool HasExpressEmitter { get { return ExpressEmitter != ExpressModifier.Emitters.NotApplicable; } }
        public bool HasVerb { get { return Verb != GameThing.Verbs.None; } }
        // Unfortunately, there is no value we can assign to missile behavior that indicates "not set".
        //public bool HasMissileBehavior { get { return MissileBehavior != MissileChassis.BehaviorFlags.None; } }
        public bool HasConstraints { get { return Constraints != ConstraintModifier.Constraints.None; } }
        public bool HasSoundUpid { get { return !String.IsNullOrEmpty(SoundUpid); } }
        public bool HasPoints { get { return true; } }
        public bool HasPlayerIndex { get { return PlayerIndex != GamePadSensor.PlayerId.Dynamic; } }
        public bool HasScoreBucket { get { return ScoreBucket != ScoreBucket.NotApplicable; } }


        public void Clear()
        {
            SpeedModifier = 1.0f;
            Loft = 0.0f;
            Strength = 0.0f;
            CreatableId = Guid.Empty;
            Pronoun = PronounModifier.Pronouns.None;
            Reset = ResetModifier.Resets.None;
            Color = Classification.Colors.None;
            Pitch = PitchModifier.PitchDirections.None;
            Turn = TurnModifier.TurnDirections.None;
            Make = MakeObjectModifier.MakeObjects.None;
            Item = ObjectModifier.ModifierObjects.None;
            TaskId = TaskModifier.TaskIds.SIZEOF;
            Direction = Programming.Directions.None;
            Facial = Face.FaceState.NotApplicable;
            ExpressEmitter = ExpressModifier.Emitters.NotApplicable;
            Verb = GameThing.Verbs.None;
            MissileBehavior = MissileChassis.BehaviorFlags.TerrainFollowing;
            Constraints = ConstraintModifier.Constraints.None;
            SoundUpid = String.Empty;
            Points = 0;
            PlayerIndex = GamePadSensor.PlayerId.Dynamic;
            ScoreBucket = ScoreBucket.NotApplicable;
        }
    }

    /// <summary>
    /// Modifiers represent the mechanism to modifiy or define the output that comes from a selector
    /// before it is handed to the Actuator/Arbitrator.
    /// Can also be losely thought of as an output parameter in some cases.
    /// </summary>
    public abstract class Modifier : ProgrammingElement
    {
        /// <summary>
        /// Specifies which frame of reference a vector is in, if any.
        /// </summary>
        [Flags]
        public enum ReferenceFrames
        {
            // The modifier assumes the vector is in object-local space.
            // (turn left, move forward, etc)
            Local = 1 << 0,
            // The modifier assumes the vector is in world space.
            // (move north, turn west, etc)
            World = 1 << 1,

            All = Local | World
        }

        /// <summary>
        /// Specifies which frame of reference we assume the vector parameter to ModifyHeading will be in.
        /// </summary>
        [XmlAttribute]
        public ReferenceFrames referenceFrame = ReferenceFrames.Local;

        protected void CopyTo(Modifier clone)
        {
            base.CopyTo(clone);
            clone.referenceFrame = this.referenceFrame;
        }

        public virtual void GatherParams(ModifierParams param) { }

        /// <summary>
        /// If this modifier affects the actor's heading, get the new desired heading.
        /// 
        /// In code, this usually returns a bool called "apply" but there's no 
        /// explanation about what that's supposed to be doing. 
        /// </summary>
        /// <param name="reflex"></param>
        /// <param name="gameActor"></param>
        /// <param name="heading"></param>
        /// <returns>???</returns>
        public virtual bool ModifyHeading(Reflex reflex, GameActor gameActor, ref Vector3 heading)
        {
            return true;
        }


        public override bool ReflexCompatible(GameActor actor, ReflexData reflex, ProgrammingElement replacedElement, bool allowArchivedCategories)
        {
            // An actuator must exist before any modifiers may appear.
            if (reflex.Actuator == null || reflex.Actuator is NullActuator)
                return false;

            // Check compatibility with actor.
            {
                if (!this.ActorCompatible(actor))
                    return false;
            }

            // Check modifier instance count
            {
                string selectionUpid = (replacedElement != null) ? replacedElement.upid : null;

                int instanceCount = reflex.GetModifierCount(this.upid);

                // Don't consider the selected one if it's of our type, since we'd be replacing it.
                if (selectionUpid == this.upid)
                    instanceCount -= 1;

                if (instanceCount >= this.MaxInstanceCount)
                    return false;
            }

            // Check modifier class count
            {
                Type selectionType = (replacedElement != null) ? replacedElement.GetType() : null;

                int count = reflex.GetModifierCountByType(this.GetType());

                // Don't consider the selected one if it's of our type, since we'd be replacing it.
                if (selectionType == this.GetType())
                    count -= 1;

                if (count >= this.MaxClassCount)
                    return false;
            }

            return base.ReflexCompatible(actor, reflex, replacedElement, allowArchivedCategories);
        }


        /// <summary>
        /// Check if this modifier can provide a parameter
        /// </summary>
        /// <param name="param"></param>
        /// <returns>true that it provides a parameter</returns>
        public virtual bool ProvideParam(out object param)
        {
            param = null;
            return false;
        }

        public virtual void PostProcessAction(bool firing, Reflex reflex, ref bool action) { }

        /// <summary>
        /// Figure out the name of the score register from the set of modifiers.
        /// If the name could not be determined, a default name is returned.
        /// </summary>
        public static ScoreBucket ScoreBucketFromModifierSet(List<Modifier> modifiers, ref bool specified, ref bool isColorBucket)
        {
            specified = true;
            isColorBucket = true;

            foreach (Modifier modifier in modifiers)
            {
                if (modifier is ColorModifier)
                {
                    ColorModifier colorMod = modifier as ColorModifier;
                    return (ScoreBucket) colorMod.color;
                }

                if (modifier is ScoreBucketModifier)
                {
                    isColorBucket = false;
                    ScoreBucketModifier bucketMod = modifier as ScoreBucketModifier;
                    return bucketMod.bucket;
                }

                // Legacy code support
                if (modifier is TeamModifier)
                {
                    TeamModifier teamMod = modifier as TeamModifier;
                    
                    if (teamMod.team == TeamModifier.Team.B)
                        return (ScoreBucket)Classification.Colors.Green;
                    else
                        return (ScoreBucket)Classification.Colors.Red;
                }
            }

            specified = false;
            return ScoreBucket.NotApplicable;
        }
        public static ScoreBucket ScoreBucketFromModifierSet(List<Modifier> modifiers, ref bool isColorBucket)
        {
            bool specified = false;
            return ScoreBucketFromModifierSet(modifiers, ref specified, ref isColorBucket);
        }
        public static ScoreBucket ScoreBucketFromModifierSet(List<Modifier> modifiers)
        {
            bool specified = false;
            bool isColorBucket = false;
            return ScoreBucketFromModifierSet(modifiers, ref specified, ref isColorBucket);
        }
    }
}
