
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
    /// this modifier acts like a parameter and provides a score amount to the actuator
    /// </summary>
    public class ScoreModifier : Modifier
    {
        const int kMaxCount = 10;

        [XmlAttribute]
        public int points;

        public override ProgrammingElement Clone()
        {
            ScoreModifier clone = new ScoreModifier();
            CopyTo(clone);
            return clone;
        }

        protected void CopyTo(ScoreModifier clone)
        {
            base.CopyTo(clone);
            clone.points = this.points;
        }

        public override void GatherParams(ModifierParams param)
        {
            // WTF?  HasPoints is hard-coded to always return true.  Why does it do this and what was it meant to do?
            if (!param.HasPoints)
                param.Points = this.points;
        }

    }
}
