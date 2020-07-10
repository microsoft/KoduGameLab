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
    public class NullSensor : Sensor
    {
        public NullSensor()
        {
            this.upid = ProgrammingElement.upidNull;
        }

        public override ProgrammingElement Clone()
        {
            NullSensor clone = new NullSensor();
            CopyTo(clone);
            return clone;
        }

        protected void CopyTo(NullSensor clone)
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
