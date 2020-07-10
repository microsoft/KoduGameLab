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
    /// <summary>
    /// Detects when we are at the end of a path
    /// 
    /// 
    /// </summary>
    public class EndofPathSensor : Sensor
    {
        #region Members
        private SensorTargetSet senseSet = new SensorTargetSet();
        private SensorTargetSet.Enumerator senseSetIter = null;

        private Random rnd = new Random();

        #endregion Members


        #region Public
        public EndofPathSensor()
        {
            senseSetIter = (SensorTargetSet.Enumerator)senseSet.GetEnumerator();
        }

        public override ProgrammingElement Clone()
        {
            EndofPathSensor clone = new EndofPathSensor();
            CopyTo(clone);
            return clone;
        }

        protected void CopyTo(EndofPathSensor clone)
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
            if (gameActor.ReadyToProcessEOP || gameThing.ReadyToProcessEOP)
            {
                senseSet.Add(gameThing, direction, range);
            }
        }

        public override void FinishUpdate(GameActor gameActor)
        {
        }


        public override void ComposeSensorTargetSet(GameActor gameActor, Reflex reflex)
        {
            List<Filter> filters = reflex.Filters;

            // add normal sightSet of items to the targetset
            //
            senseSetIter.Reset();
            while (senseSetIter.MoveNext())
            {
                SensorTarget target = (SensorTarget)senseSetIter.Current;

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

        #endregion Public
    }
}
