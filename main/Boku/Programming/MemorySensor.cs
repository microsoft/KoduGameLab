// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.


using System;
using System.Collections;
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
    /// Senses GameThings that have been remembered
    /// 
    /// this sensor is archived.  
    /// The currently plan is to move its functionality into other sensors 
    /// as inherent functionality rather than expose the complexity.
    /// </summary>
    public class MemorySensor : Sensor
    {
        public override ProgrammingElement Clone()
        {
            MemorySensor clone = new MemorySensor();
            CopyTo(clone);
            return clone;
        }

        protected void CopyTo(MemorySensor clone)
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
        }
    }
}
