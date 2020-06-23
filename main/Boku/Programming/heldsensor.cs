
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
    /// Senses when the GameActor is being held
    /// 
    /// </summary>
    public class HeldSensor : Sensor
    {
        private SensorTargetSet senseSet = new SensorTargetSet();
        private SensorTargetSet.Enumerator senseSetIter = null;

        public HeldSensor()
        {
            senseSetIter = (SensorTargetSet.Enumerator)senseSet.GetEnumerator();
        }

        public override ProgrammingElement Clone()
        {
            HeldSensor clone = new HeldSensor();
            CopyTo(clone);
            return clone;
        }

        protected void CopyTo(HeldSensor clone)
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
            if (gameActor.ActorHoldingThis != null)
            {
                // presence is only thing important here
                SensorTarget target = SensorTargetSpares.Alloc();
                target.Init(gameActor, gameActor.ActorHoldingThis as GameThing);
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
