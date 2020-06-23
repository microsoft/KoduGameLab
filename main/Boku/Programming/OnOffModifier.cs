
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

using Boku;
using Boku.Base;
using Boku.Common;
using Boku.SimWorld;

namespace Boku.Programming
{
    /// <summary>
    /// this modifier acts like a parameter and provides the on/off state to the actuator
    /// </summary>
    public class OnOffModifier : Modifier
    {
        public enum States
        {
            On,
            Off,
        }

        [XmlAttribute]
        public States state = States.Off;

        public override ProgrammingElement Clone()
        {
            OnOffModifier clone = new OnOffModifier();
            CopyTo(clone);
            return clone;
        }

        protected void CopyTo(OnOffModifier clone)
        {
            base.CopyTo(clone);
            clone.state = this.state;
        }

    }
}
