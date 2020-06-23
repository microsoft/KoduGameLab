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
    public class ScoreBucketFilter : Filter
    {
        [XmlAttribute]
        public ScoreBucket bucket;

        [XmlIgnore]
        public bool isPrivate = false;  // Is this a private score?

        [XmlAttribute]
        public Classification.Colors color
        {
            get { return (Classification.Colors)bucket; }
            set { bucket = (ScoreBucket)value; }
        }

        public override ProgrammingElement Clone()
        {
            ScoreBucketFilter clone = new ScoreBucketFilter();
            CopyTo(clone);
            return clone;
        }

        protected void CopyTo(ScoreBucketFilter clone)
        {
            base.CopyTo(clone);
            clone.bucket = this.bucket;

            clone.isPrivate = this.upid.Contains("local");
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
