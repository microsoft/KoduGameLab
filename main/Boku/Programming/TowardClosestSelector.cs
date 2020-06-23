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

namespace Boku.Programming
{
    /// <summary>
    /// this selector will find the closest Action Thing and 
    /// calculate a vector toward it and hand this to the actuator’s arbitrator.
    /// 
    /// It is known also as Toward
    /// </summary>
    public class TowardClosestSelector : Selector
    {
        [XmlAttribute]
        public float strength;

        public TowardClosestSelector()
        {
        }

        public override ProgrammingElement Clone()
        {
            TowardClosestSelector clone = new TowardClosestSelector();
            CopyTo(clone);
            return clone;
        }

        protected void CopyTo(TowardClosestSelector clone)
        {
            base.CopyTo(clone);
            clone.strength = this.strength;
        }

        public override void Reset(Reflex reflex)
        {
            ClearActionSet(actionSet);
            base.Reset(reflex);
        }

        public override ActionSet ComposeActionSet(Reflex reflex, GameActor gameActor)
        {
            ClearActionSet(actionSet);
            UpdateCanBlend(reflex);

            if (!reflex.targetSet.AnyAction)
                return actionSet;

            SensorTarget target = reflex.targetSet.Nearest;
            if (target != null)
            {
                // Calculate a vector toward target.
                Vector3 value = target.Direction;

                bool apply = reflex.ModifyHeading(gameActor, Modifier.ReferenceFrames.World, ref value);

                if (apply)
                {
                    actionSet.AddAction(Action.AllocTargetLocationAction(reflex, target.Position, autoTurn: true));
                }
            }

            return actionSet;
        }

        public override void Used(bool newUse)
        {
        }

    }
}
