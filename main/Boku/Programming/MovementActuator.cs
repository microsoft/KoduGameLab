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
    /// Marshals movements from the brain to the GameActor (heading, speed, and facing direction).
    /// </summary>
    public class MovementActuator : Actuator
    {
        public override ProgrammingElement Clone()
        {
            MovementActuator clone = new MovementActuator();
            CopyTo(clone);
            return clone;
        }

        protected void CopyTo(MovementActuator clone)
        {
            base.CopyTo(clone);
        }

        public override void Reset(Reflex reflex)
        {
            base.Reset(reflex);
        }

        protected override bool ActuatorUpdate(Reflex reflex)
        {
            reflex.Task.GameActor.QueueMovementSet(this.actionSet);
            return true;
        }
    }
}
