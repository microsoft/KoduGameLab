
using System;
using System.Collections;
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

namespace Boku.Programming
{
    /// <summary>
    /// Filters GameThings based upon their classification properties
    /// 
    /// 
    /// </summary>
    public class ClassificationFilter : Filter
    {
        public Classification classification;

        [XmlElement]
        public ClassificationType MatchType;

        private MatchCall onMatch = null;
        private MatchCall OnMatch
        {
            get { return (onMatch ?? (onMatch = this.classification.MatchByType(MatchType))); }
        }

        public override ProgrammingElement Clone()
        {
            ClassificationFilter clone = new ClassificationFilter();
            CopyTo(clone);
            return clone;
        }

        protected void CopyTo(ClassificationFilter clone)
        {
            base.CopyTo(clone);
            clone.classification = this.classification;
            clone.MatchType = this.MatchType;
        }

        public override bool MatchTarget(Reflex reflex, SensorTarget sensorTarget)
        {
            if (sensorTarget.GameThing is NullActor)
            {
                return false;
            }

            bool match = true;  // By default, don't filter.

            // Skip if no sensor category 
            if (MatchType != ClassificationType.None)
            {
                switch(MatchType) 
                {
                    case ClassificationType.Object:
                        if (string.IsNullOrEmpty(classification.name))
                        {
                            match = true;
                        }
                        else if (classification.name == "bot")      // Handle "Any Bots" tile.
                        {
                            GameActor actor = sensorTarget.GameThing as GameActor;
                            if (actor == null || !actor.IsBot)
                            {
                                match = false;
                            }
                        }
                        else if (classification.name == "building") // Handle "Any Building" tile.
                        {
                            GameActor actor = sensorTarget.GameThing as GameActor;
                            if (actor == null || !actor.IsBuilding)
                            {
                                match = false;
                            }
                        }
                        else
                        {
                            match = classification.name == sensorTarget.Classification.name;
                        }
                        break;
                    case ClassificationType.Color:
                        match = classification.color == sensorTarget.Classification.color || classification.color == sensorTarget.Classification.glowColor;
                        break;
                    case ClassificationType.PathColor:
                        //path color only matches if the color matches the last end of path color this actor has seen
                        if (sensorTarget.GameThing is GameActor)
                        {
                            match = classification.color == (sensorTarget.GameThing as GameActor).ReachedEOP;
                        }
                        else
                        {
                            match = false;
                        }
                        
                        break;
                    case ClassificationType.Expression:
                        match =
                            (classification.expression != Boku.SimWorld.Face.FaceState.NotApplicable && classification.expression == sensorTarget.Classification.expression) ||
                            (classification.emitter != ExpressModifier.Emitters.NotApplicable && classification.emitter == sensorTarget.Classification.emitter);
                        break;
                    case ClassificationType.Moving:
                        match = sensorTarget.Movement.Velocity.LengthSquared() > 0.01f;
                        break;
                    case ClassificationType.Camouflage:
                        match = sensorTarget.GameThing.Camouflaged;
                        break;
                    default:
                        // If this fires you should probably put in a new case statement for ht6e new category.
                        Debug.Assert(false);
                        match = OnMatch(sensorTarget.Classification);
                        break;
                }
                // TODO (****) Remove this once we're sure the changes are 
                // good so that we get decent perf in debug, too.
                /*
                if (classification.name != "building" && classification.name != "bot")
                {
                    Debug.Assert(match == OnMatch(sensorTarget.Classification), "Tell *** to fix.");
                }
                */
            }

            return match;
        }

        public override bool MatchAction(Reflex reflex, out object param)
        {
            // doesn't affect action, only filters objects
            param = null;
            return true;
        }
    }
}
