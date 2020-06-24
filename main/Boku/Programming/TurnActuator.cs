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
    /// Marshals desired turns coming from the brain to the actor.
    /// 
    /// (scoy) Looks like all this really does is queue the movement sets which are created by the TurnSelector.
    /// Not really sure why TurnSelector exists at all.  Feels like it should all be here.
    /// </summary>
    public class TurnActuator : Actuator
    {
        public override ProgrammingElement Clone()
        {
            TurnActuator clone = new TurnActuator();
            CopyTo(clone);
            return clone;
        }

        protected void CopyTo(TurnActuator clone)
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

        public override bool ActorCompatible(GameActor gameActor)
        {
            if (gameActor != null && !gameActor.Chassis.HasFacingDirection)
                return false;

            return base.ActorCompatible(gameActor);
        }
    }
}
