
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
    public class HealthSensor : Sensor
    {
        int frame;

        public override ProgrammingElement Clone()
        {
            HealthSensor clone = new HealthSensor();
            CopyTo(clone);
            return clone;
        }

        protected void CopyTo(HealthSensor clone)
        {
            base.CopyTo(clone);
        }

        public override void Reset(Reflex reflex)
        {
            frame = 0;
            base.Reset(reflex);
        }

        public override void StartUpdate(GameActor gameActor)
        {
            frame += 1;
        }

        public override void ThingUpdate(GameActor gameActor, GameThing gameThing, Vector3 direction, float range)
        {
        }

        public override void FinishUpdate(GameActor gameActor)
        {
        }

        public override void ComposeSensorTargetSet(GameActor gameActor, Reflex reflex)
        {
            reflex.targetSet.Param = Filter.ScoresFromFilterSet(gameActor, reflex);

            reflex.targetSet.Action = TestObjectSet(reflex);
        }

    }   // end of class HealthSensor

}   // end of namespace Boku.Programming
