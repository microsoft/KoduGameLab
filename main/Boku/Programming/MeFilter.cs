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
    public class MeFilter : Filter
    {

        public override bool MatchTarget(Reflex reflex, SensorTarget sensorTarget)
        {
            bool result = reflex.Task.GameActor == sensorTarget.GameThing;
            return result;
        }

        public override bool MatchAction(Reflex reflex, out object param)
        {
            param = null; // doesn't effect match action

            bool match = false;
            if (reflex.targetSet.Nearest != null)
            {
                match = reflex.targetSet.Nearest.GameThing == reflex.Task.GameActor;
            }

            if (reflex.Data.Sensor is TouchSensor)
            {
                match |= reflex.TouchActor == reflex.Task.GameActor;
            }
            else
            {
                match |= reflex.MouseActor == reflex.Task.GameActor;
            }

            if (!match)
            {
                reflex.MousePosition = null;
                reflex.TouchPosition = null;
            }

            return match;
        }

        public override ProgrammingElement Clone()
        {
            MeFilter clone = new MeFilter();
            CopyTo(clone);
            return clone;
        }

        protected void CopyTo(MeFilter clone)
        {
            base.CopyTo(clone);
        }

    }   // end of class Filter

}   // end of namespace Boku.Programming
