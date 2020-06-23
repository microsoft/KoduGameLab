
using System;
using System.Collections.Generic;
using System.Diagnostics;

using System.Xml;
using System.Xml.Serialization;

using Microsoft.Xna.Framework;

using Boku.Base;
using Boku.Common;

namespace Boku.Programming
{
    /// <summary>
    /// Set the distance at which an object circles another.
    /// </summary>
    public class CircleDistanceModifier : Modifier
    {
        [XmlAttribute]
        public int maxCount = 3;

        [XmlAttribute]
        public float radiusScale;

        public override ProgrammingElement Clone()
        {
            CircleDistanceModifier clone = new CircleDistanceModifier();
            CopyTo(clone);
            return clone;
        }

        protected void CopyTo(CircleDistanceModifier clone)
        {
            base.CopyTo(clone);
            clone.radiusScale = this.radiusScale;
        }

    }
}
