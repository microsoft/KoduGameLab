
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

namespace Boku.Programming
{
    /// <summary>
    /// Senses when something is given to the GameActor
    /// 
    /// This sensor demonstrates another of the few Event based sensors.  
    /// It will only get updated when an actual item is given to the game actor.
    /// </summary>
    public class GivenObjectSensor : Sensor
    {
        public override ProgrammingElement Clone()
        {
            GivenObjectSensor clone = new GivenObjectSensor();
            CopyTo(clone);
            return clone;
        }

        protected void CopyTo(GivenObjectSensor clone)
        {
            base.CopyTo(clone);
        }

        public override void Reset(Reflex reflex)
        {
            base.Reset(reflex);
        }

        public override void StartUpdate(GameActor gameActor)
        {
        }

        public override void ThingUpdate(GameActor gameActor, GameThing gameThing, Vector3 direction, float range)
        {
        }

        public override void FinishUpdate(GameActor gameActor)
        {
        }


        public override void ComposeSensorTargetSet(GameActor gameActor, Reflex reflex)
        {
            List<Filter> filters = reflex.Filters;

            SensorTargetSet.Enumerator senseSetIter = (SensorTargetSet.Enumerator)gameActor.GivenSet.GetEnumerator();
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
            }

            reflex.targetSet.Action = TestObjectSet(reflex);
        }
    }
}
