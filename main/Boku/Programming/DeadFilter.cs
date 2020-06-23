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
    public class DeadFilter : Filter
    {
        public override ProgrammingElement Clone()
        {
            DeadFilter clone = new DeadFilter();
            CopyTo(clone);
            return clone;
        }

        protected void CopyTo(DeadFilter clone)
        {
            base.CopyTo(clone);
        }

        public override bool MatchTarget(Reflex reflex, SensorTarget sensorTarget)
        {
            bool match = sensorTarget.GameThing.CurrentState == GameThing.State.Dead || sensorTarget.GameThing.PendingState == GameThing.State.Dead;
            return match;
        }

        public override bool MatchAction(Reflex reflex, out object param)
        {
            param = null;
            return true;
        }
    }
}
