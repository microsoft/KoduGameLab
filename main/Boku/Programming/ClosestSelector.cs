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
    /// This will just select the closest Action Thing from the set and add 
    /// that to the actuators arbitrator.
    /// 
    /// this selector is known as “nearest” and is hidden unless there is another valid selector.  
    /// 
    /// It was known as “Use” for a bit.  
    ///
    /// This is not used for WHEN See Apple DO Move Toward
    /// This IS used for WHEN See Apple DO Shoot
    /// 
    /// </summary>
    public class ClosestSelector : Selector
    {
        public ClosestSelector()
        {
        }

        public override ProgrammingElement Clone()
        {
            ClosestSelector clone = new ClosestSelector();
            CopyTo(clone);
            return clone;
        }

        protected void CopyTo(ClosestSelector clone)
        {
            base.CopyTo(clone);
        }

        public override void Reset(Reflex reflex)
        {
            base.Reset(reflex);
        }

        public override ActionSet ComposeActionSet(Reflex reflex, GameActor gameActor)
        {
            ClearActionSet(actionSet);
            UpdateCanBlend(reflex);

            if (!reflex.targetSet.AnyAction)
                return actionSet;

            Vector3 direction = Vector3.Zero;   // This is either the direction to the nearest target or, if no target, the direction we want (forward, etc).
            GameThing gameThing = null;
            float range = 0.0f;

            // TODO (****) This was causing a null ref in the "Green Ghost V Looper" level
            // when the user pressed the mouse button to launch a wisp.  In that case the
            // targetSet has 1 element in it but it's not "valid" so it never goes into 
            // the nearestTargets list, hence targetSet.Nearest is null.
            // Note I also pulled the Finalize call out of the if statement since Finialize(sic)
            // is what creates the nearestTargets list.
            // Still need to investigate the underlying cause.
            // Turns out to be an issue with Ghost.  Ghosted objects are "ignored" so not valid
            // for the Nearest list.  Should probably rethink how invisible and ghosted work.

            if (reflex.targetSet.Nearest != null && !(reflex.targetSet.Nearest.GameThing is NullActor))
            {
                // We have a "real" target.
                // The targetSet is in order by distance.
                // Pick the nearest member of the target set.
                SensorTarget target = reflex.targetSet.Nearest;

                direction = target.Direction;
                gameThing = target.GameThing;
                range = target.Range;
            }
            else
            {
                // No explicit target.  
                // This handles the case of WHEN GamePad AButton DO Shoot
                // where instead of having a target we just have a direction.
                if (reflex.targetSet.Param != null && reflex.targetSet.Param is Vector2)
                {
                    direction = new Vector3((Vector2)reflex.targetSet.Param, 0.0f);
                }
                else
                {
                    direction = Vector3.Zero;
                }
                gameThing = null;
                range = 1.0f;
            }

            bool apply = reflex.ModifyHeading(gameActor, Modifier.ReferenceFrames.World, ref direction);

            if (apply)
            {
                // TODO (****) Probably need a TargetObject Action.
                // radius should be from object
                actionSet.AddActionTarget(Action.AllocAttractor(range, direction, gameThing, reflex), 0.4f);
            }

            return actionSet;
        }
        public override void Used(bool newUse)
        {
        }
    }
    
}
