using System;
using System.Xml;
using System.Xml.Serialization;

using Microsoft.Xna.Framework;

using Boku;
using Boku.Base;
using Boku.Common;
using Boku.SimWorld;

namespace Boku.Programming
{
    public class NumericFilter : Filter
    {
        [XmlAttribute]
        public float value;

        public override bool MatchTarget(Reflex reflex, SensorTarget sensorTarget)
        {
            return false;
        }

        public override bool MatchAction(Reflex reflex, out object param)
        {
            param = null;
            return true;
        }

        public override ProgrammingElement Clone()
        {
            NumericFilter clone = new NumericFilter();
            CopyTo(clone);
            return clone;
        }

        protected void CopyTo(NumericFilter clone)
        {
            base.CopyTo(clone);
            clone.value = this.value;
        }
    }
}
