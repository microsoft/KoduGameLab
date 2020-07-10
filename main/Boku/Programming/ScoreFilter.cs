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
    /// Filter that returns a positive action when the score value is met or exceeded
    /// 
    /// </summary>
    public class ScoreFilter : Filter
    {
        const int kMaxCount = 10;

        [XmlAttribute]
        public int points;

        public override ProgrammingElement Clone()
        {
            ScoreFilter clone = new ScoreFilter();
            CopyTo(clone);
            return clone;
        }

        protected void CopyTo(ScoreFilter clone)
        {
            base.CopyTo(clone);
            clone.points = this.points;
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
