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
    /// Hybrid filter that provides the source of a timer input
    /// 
    /// It contains the definition of the amount of time
    /// </summary>
    public class TimerFilter : Filter
    {
        const int kMaxCount = 10;

        [XmlAttribute]
        public double seconds;

        public override ProgrammingElement Clone()
        {
            TimerFilter clone = new TimerFilter();
            CopyTo(clone);
            return clone;
        }

        protected void CopyTo(TimerFilter clone)
        {
            base.CopyTo(clone);
            clone.seconds = this.seconds;
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
