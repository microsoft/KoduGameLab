// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Xml;
using System.Xml.Serialization;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using Boku.Base;
using Boku.Common;
using Boku.SimWorld;


namespace Boku.Programming
{
    /// <summary>
    /// GotObjectSensor fires for things being offered the actor
    /// as well as for the thing the actor is carrying (if any).
    /// Examples:
    /// 
    /// got apple - drop             : Refuse offered apples.
    /// got apple - color me green   : If I'm carrying an apple, paint me green.
    /// got none - color me red      : If I'm not carrying anything, paint me red.
    /// </summary>
    public class GotObjectSensor : Sensor
    {
        SensorTargetSet senseSet = new SensorTargetSet();
        private SensorTargetSet.Enumerator senseSetIter = null;

        public GotObjectSensor()
        {
            senseSetIter = (SensorTargetSet.Enumerator)senseSet.GetEnumerator();
        }

        public override ProgrammingElement Clone()
        {
            GotObjectSensor clone = new GotObjectSensor();
            CopyTo(clone);
            return clone;
        }

        protected void CopyTo(GotObjectSensor clone)
        {
            base.CopyTo(clone);
        }

        public override void Reset(Reflex reflex)
        {
            senseSet.Clear();
            base.Reset(reflex);
        }

        public override void StartUpdate(GameActor gameActor)
        {
            senseSet.Clear();
        }

        public override void ThingUpdate(GameActor gameActor, GameThing gameThing, Vector3 direction, float range)
        {
        }

        public override void FinishUpdate(GameActor gameActor)
        {
            senseSet.Clear();

            if (gameActor.ThingBeingHeldByThisActor != null)
                senseSet.Add(gameActor.ThingBeingHeldByThisActor, Vector3.Zero, 0f);

            // Eating things also triggers the "got" sensor.
            senseSet.Add(gameActor.EatenSet);
        }

        public override void ComposeSensorTargetSet(GameActor gameActor, Reflex reflex)
        {
            List<Filter> filters = reflex.Filters;

            senseSetIter.Reset();
            while (senseSetIter.MoveNext())
            {
                SensorTarget target = (SensorTarget)senseSetIter.Current;

                bool match = true;
                for (int indexFilter = 0; indexFilter < filters.Count; indexFilter++)
                {
                    Filter filter = filters[indexFilter] as Filter;
                    if (!filter.MatchTarget(reflex, target))
                    {
                        match = false;
                        break;
                    }
                }
                if (match)
                {
                    reflex.targetSet.Add(target);
                }
            } // end for

            reflex.targetSet.Action = TestObjectSet(reflex);

        } // end void ComposeSensorTargetSet()
        
    } // end class

} // end namespace Boku.Programming

