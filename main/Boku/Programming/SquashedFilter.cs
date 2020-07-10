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
    public class SquashedFilter : Filter
    {
        public override ProgrammingElement Clone()
        {
            SquashedFilter clone = new SquashedFilter();
            CopyTo(clone);
            return clone;
        }

        protected void CopyTo(SquashedFilter clone)
        {
            base.CopyTo(clone);
        }

        public override bool MatchTarget(Reflex reflex, SensorTarget sensorTarget)
        {
            bool match = sensorTarget.GameThing.CurrentState == GameThing.State.Squashed || sensorTarget.GameThing.PendingState == GameThing.State.Squashed;
            return match;
        }

        public override bool MatchAction(Reflex reflex, out object param)
        {
            param = null;
            return true;
        }
    }   // end of class SquashedFilter
}
