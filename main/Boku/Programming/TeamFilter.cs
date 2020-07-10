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
using Boku.Input;

namespace Boku.Programming
{
    /// <summary>
    /// Used as a Parameter Filter used by the GameScoredSensor
    /// 
    /// </summary>
    public class TeamFilter : Filter
    {
        public enum Team
        {
            Dynamic, // used by the sensor to signify to use this filter
            A,
            B,
        }
        [XmlAttribute]
        public Team team;

        public override ProgrammingElement Clone()
        {
            TeamFilter clone = new TeamFilter();
            CopyTo(clone);
            return clone;
        }

        protected void CopyTo(TeamFilter clone)
        {
            base.CopyTo(clone);
            clone.team = this.team;
        }

        public override bool MatchTarget(Reflex reflex, SensorTarget sensorTarget)
        {
            return false;
        }

        public override bool MatchAction(Reflex reflex, out object param)
        {
            param = null;
            return true;
        }

    }
}
