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
    /// Senses when the rover has beamed something
    /// </summary>

    public class BeamedSensor : Sensor
    {
        public override ProgrammingElement Clone()
        {
            BeamedSensor clone = new BeamedSensor();
            CopyTo(clone);
            return clone;
        }

        protected void CopyTo(BeamedSensor clone)
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

            SensorTargetSet.Enumerator beamedSetIter = gameActor.BeamedSetIter;
            beamedSetIter.Reset();
            while (beamedSetIter.MoveNext())
            {
                SensorTarget target = (SensorTarget)beamedSetIter.Current;

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
