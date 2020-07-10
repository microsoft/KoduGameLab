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
    /// Senses when a timer has expired
    /// 
    /// It acts more of the manager of TimerFilters rather than the true input of the sensor
    /// 
    /// this sensor is one of the few FastPoll sensors.  
    /// This means that it will be updated on every Update rather than 
    /// on every brain reaction cycle (about every 0.33 seconds).
    /// </summary>
    public class TimerSensor : Sensor
    {
        public override ProgrammingElement Clone()
        {
            TimerSensor clone = new TimerSensor();
            CopyTo(clone);
            return clone;
        }

        protected void CopyTo(TimerSensor clone)
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
            reflex.targetSet.Action = TestObjectSet(reflex);
        }
    }
}
