
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
    /// This modifier acts like a parameter and provides a numeric amount to the actuator.
    /// </summary>
    public class HealthModifier : Modifier
    {
        [XmlAttribute]
        public int points;

        public override ProgrammingElement Clone()
        {
            HealthModifier clone = new HealthModifier();
            CopyTo(clone);
            return clone;
        }

        protected void CopyTo(HealthModifier clone)
        {
            base.CopyTo(clone);
            clone.points = this.points;
        }

        public override void GatherParams(ModifierParams param)
        {
            // We don't have the actor at this point so we have no clue what the actor's
            // health is.  Just default to 0.
            // TODO Microbit Not sure how to fix this.
            // WTF?  HasPoints is hard-coded to always return true.  Why does it do this and what was it meant to do?
            if (!param.HasPoints)
            {
                param.Points = 0;
            }
        }

    }   // end of class HealthModifier

}   // end of namespace Boku.Programming
