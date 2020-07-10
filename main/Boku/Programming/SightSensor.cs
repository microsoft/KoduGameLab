// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.


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
using Boku.SimWorld.Terra;

namespace Boku.Programming
{
    public class VisualDevice
    {
        #region Members

        #endregion

        #region Accessors

        #endregion

    }   // end of class VisualDevice

    /// <summary>
    /// Senses thing by sight.  Option filters can be used to filter based
    /// on position relative to bot and for terrain occlusion.
    /// </summary>
    public class SightSensor : Sensor
    {
        #region Members

        private SensorTargetSet sightSet = new SensorTargetSet();
        protected const float SightZError = 0.3f;

        #endregion

        #region Public 

        public SightSensor()
        {
        }

        public override ProgrammingElement Clone()
        {
            SightSensor clone = new SightSensor();
            CopyTo(clone);
            return clone;
        }

        protected void CopyTo(SightSensor clone)
        {
            base.CopyTo(clone);
        }

        public override void Reset(Reflex reflex)
        {
            sightSet.Clear();
            base.Reset(reflex);
        }
        
        public override void StartUpdate(GameActor gameActor)
        {
            sightSet.Clear();
        }   // end of StartUpdate()

        public override void ThingUpdate(GameActor gameActor, GameThing gameThing, Vector3 direction, float range)
        {
            // Add object to sight set.  Note, the line of sight visibility
            // test will be done as a seperate filter.
            sightSet.Add(gameThing, direction, range);
        }   // end of ThingUpdate()

        public override void FinishUpdate(GameActor gameActor)
        {
        }

        public override void ComposeSensorTargetSet(GameActor gameActor, Reflex reflex)
        {
            List<Filter> filters = reflex.Filters;

            foreach(SensorTarget target in sightSet)
            {
                // Don't see things we are holding.
                if (target.GameThing == gameActor.ThingBeingHeldByThisActor)
                    continue;

                bool match = true;
                bool cursorFilterPresent = false;

                for (int indexFilter = 0; indexFilter < filters.Count; indexFilter++)
                {
                    Filter filter = filters[indexFilter] as Filter;
                    ClassificationFilter cursorFilter = filter as ClassificationFilter;
                    if (cursorFilter != null && cursorFilter.classification.IsCursor)
                    {
                        cursorFilterPresent = true;
                    }

                    if (!filter.MatchTarget(reflex, target))
                    {
                        match = false;
                        break;
                    }
                }
                if (match)
                {
                    if (!target.Classification.IsCursor || cursorFilterPresent)
                    {
                        reflex.targetSet.Add(target);
                    }
                }
            }


            reflex.targetSet.Action = TestObjectSet(reflex);

            if (reflex.targetSet.Action)
            {
                SensorTarget nearest = reflex.targetSet.Nearest;
                if (nearest != null)
                {
                    gameActor.AddSightLine(nearest.GameThing);
                }
            }
            else
            {
                reflex.targetSet.Clear();
            }
            
        }   // end of ComposeSensorTargetSet()

        #endregion

    }   // end of class SightSensor

}   // end of namespace Boku.Programming
