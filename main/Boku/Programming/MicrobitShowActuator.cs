using System;
using System.Collections.Generic;
using System.Text;

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
    /// Shows patterns of lights on the Microbit's 5x5 LED display.
    /// </summary>
    public class MicrobitShowActuator : Actuator, IMicrobitTile
    {
        public override ProgrammingElement Clone()
        {
            MicrobitShowActuator clone = new MicrobitShowActuator();
            CopyTo(clone);
            return clone;
        }

        protected void CopyTo(MicrobitShowActuator clone)
        {
            base.CopyTo(clone);
        }

        public override void Reset(Reflex reflex)
        {
            base.Reset(reflex);
        }

        protected override bool ActuatorUpdate(Reflex reflex)
        {
            return true;
        }
    }
}
