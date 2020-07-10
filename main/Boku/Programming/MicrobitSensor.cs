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

namespace Boku.Programming
{
    /// <summary>
    /// Senses when the Microbit is used for input.
    /// 
    /// This sensor acts more like a manager than the true source of the sensor event.
    /// It will request the MicrobitTilt and MicrobitButton filters to provide 
    /// the actual input.  This sensor demonstrates a break in the normal use of 
    /// the model but demonstrates how other elements can be used to solve problems.
    /// 
    /// This is modelled directly on the Microbit sensor
    /// </summary>
    public class MicrobitSensor : Sensor, IMicrobitTile
    {
        public MicrobitSensor()
        {
        }

        public override ProgrammingElement Clone()
        {
            MicrobitSensor clone = new MicrobitSensor();
            CopyTo(clone);
            return clone;
        }

        protected void CopyTo(MicrobitSensor clone)
        {
            base.CopyTo(clone);
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

            reflex.targetSet.Action = TestObjectSet(reflex);
        }

        private new bool TestObjectSet(Reflex reflex)
        {
            List<Filter> filters = reflex.Filters;

            bool match = true;
            object param;
            for (int indexFilter = 0; indexFilter < filters.Count; indexFilter++)
            {
                Filter filter = filters[indexFilter] as Filter;
                if (!filter.MatchAction(reflex, out param))
                {
                    match = false;
                    break;
                }
                if (param != null && param is Vector2)
                {
                    reflex.targetSet.Param = param;
                }
            }

            if (reflex.Data.GetFilterCountByType(typeof(NotFilter)) > 0)
                match = !match;

            match = PostProcessAction(match, reflex);

            return match;
        }
    }
}
