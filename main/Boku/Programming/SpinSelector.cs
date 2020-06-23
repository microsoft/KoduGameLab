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
    /// this selector will spin the GameActor clockwise
    /// 
    /// 
    /// 
    /// ARCHIVED!!!
    /// 
    /// 
    /// 
    /// </summary>
    public class SpinSelector : Selector
    {
        public SpinSelector()
        {
        }

        public override ProgrammingElement Clone()
        {
            SpinSelector clone = new SpinSelector();
            CopyTo(clone);
            return clone;
        }

        protected void CopyTo(SpinSelector clone)
        {
            base.CopyTo(clone);
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

            float angle = (float)Math.Acos(gameActor.Movement.Facing.X);
            if (gameActor.Movement.Facing.Y < 0.0f)
            {
                angle = MathHelper.TwoPi - angle;
            }
            angle += MathHelper.PiOver4; // rotate 45
            angle = (angle + (float)Math.PI * 2.0f) % ((float)Math.PI * 2.0f);

            // calculate a vector toward target
            Vector3 value = new Vector3();
            value.X = (float)Math.Cos(angle);
            value.Y = (float)Math.Sin(angle);
            value.Z = 0.0f;
            value *= 0.001f; // remove any forward velocity


            bool apply = reflex.ModifyHeading(gameActor, Modifier.ReferenceFrames.World, ref value);

            if (apply)
            {
                // radius should be from object
                actionSet.AddAttractor(Action.AllocAttractor(0.0f, value, null, reflex), 0.0f);
            }

            // unused // this.used = false; // will get reset if truely used
            return actionSet;
        }

        public override void Used(bool newUse)
        {
            // unused // this.used = true;
        }

    }
}
