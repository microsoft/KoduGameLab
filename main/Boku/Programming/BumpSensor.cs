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

namespace Boku.Programming
{
    /// <summary>
    /// Senses when something collides with the GameActor
    /// 
    /// This is exposed as the �Touch� sensor.  
    /// Currently very simple collision test is done against the Bump Devices 
    /// that the actor exposes.  This work should get redone and use a true 
    /// collision system to trigger this sensor rather than the current polling.
    /// </summary>

    public class BumpSensor : Sensor
    {
        public override ProgrammingElement Clone()
        {
            BumpSensor clone = new BumpSensor();
            CopyTo(clone);
            return clone;
        }

        protected void CopyTo(BumpSensor clone)
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

            SensorTargetSet.Enumerator touchedSetIter = gameActor.TouchedSetIter;
            touchedSetIter.Reset();
            while (touchedSetIter.MoveNext())
            {
                SensorTarget target = (SensorTarget)touchedSetIter.Current;

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
        }
    }
}
