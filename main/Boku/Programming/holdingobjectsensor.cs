
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

namespace Boku.Programming
{
    /// <summary>
    /// Senses when the GameActor is holding something
    /// 
    /// </summary>
    public class HoldingObjectSensor : Sensor
    {
        private SensorTargetSet senseSet = new SensorTargetSet();
        private SensorTargetSet.Enumerator senseSetIter = null;

        public HoldingObjectSensor()
        {
            senseSetIter = (SensorTargetSet.Enumerator)senseSet.GetEnumerator();
        }

        public override ProgrammingElement Clone()
        {
            HoldingObjectSensor clone = new HoldingObjectSensor();
            CopyTo(clone);
            return clone;
        }

        protected void CopyTo(HoldingObjectSensor clone)
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
            if (gameActor.ThingBeingHeldByThisActor != null)
            {
                // presence is only thing important here
                SensorTarget target = SensorTargetSpares.Alloc();
                target.Init(gameActor, gameActor.ThingBeingHeldByThisActor);
                senseSet.AddOrFree(target);
            }
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
            }

            reflex.targetSet.Action = TestObjectSet(reflex);
        }
    }
}
