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
    public class NullActuator : Actuator
    {
        public NullActuator()
        {
            this.upid = ProgrammingElement.upidNull;
        }

        public override ProgrammingElement Clone()
        {
            NullActuator clone = new NullActuator();
            CopyTo(clone);
            return clone;
        }

        protected override bool ActuatorUpdate(Reflex reflex)
        {
            return false;
        }
    }
}
