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
    /// This selector will calculate a vector that is away from all 
    /// of the Action Things and add it as a replusor to the actuators arbitrator.
    /// </summary>
    public class AwayFromAllSelector : Selector
    {
        #region Accessors
        [XmlAttribute]
        public float strength;  // Not really used...

        [XmlAttribute]
        public bool falloff;    // If true, then use the Avoid behaviour.  If false, use Away behaviour.
        #endregion Accessors

        #region Public
        public AwayFromAllSelector()
        {
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

            if (reflex.targetSet.Count > 0)
            {
                // falloff == Avoid, !falloff == Away
                if (falloff)
                {
                    // Avoid.
                    // We want to avoid all actors so we need to loop over the full set.
                    foreach (SensorTarget target in reflex.targetSet)
                    {
                        GameActor targetActor = target.GameThing as GameActor;
                        if (targetActor != null)
                        {
                            actionSet.AddAction(Action.AllocAvoidAction(reflex, targetActor));
                        }
                    }
                }
                else
                {
                    // Away.
                    Vector3 actorPos = gameActor.Movement.Position;
                    Vector3 dir = Vector3.Zero; // The direction we want to flee.

                    // We want to move away from all actors so we need to loop over the full set.
                    foreach (SensorTarget target in reflex.targetSet)
                    {
                        GameActor targetActor = target.GameThing as GameActor;

                        if (targetActor != null)
                        {
                            Vector3 fromTarget = actorPos - targetActor.Movement.Position;
                            float dist = fromTarget.Length();
                            // Normalize fromTarget then divide again by dist.  This gives us a linear
                            // falloff of strength based on distance so that a target that is twice as 
                            // close will have twice the weight on the flee direction.
                            dir += fromTarget / dist / dist;
                        }
                    }

                    bool apply = reflex.ModifyHeading(gameActor, Modifier.ReferenceFrames.World, ref dir);

                    if (apply)
                    {
                        dir.Normalize();
                        actionSet.AddAction(Action.AllocVelocityAction(reflex, dir, autoTurn: true));
                    }
                }   // end else if falloff

            }   // end if targetSet not empty.

            return actionSet;
        }
        public override void Used(bool newUse)
        {
        }

        #endregion Public

        #region Internal

        public override ProgrammingElement Clone()
        {
            AwayFromAllSelector clone = new AwayFromAllSelector();
            CopyTo(clone);
            return clone;
        }

        protected void CopyTo(AwayFromAllSelector clone)
        {
            base.CopyTo(clone);
            clone.strength = this.strength;
            clone.falloff = this.falloff;
        }

        #endregion Internal
    }
}
