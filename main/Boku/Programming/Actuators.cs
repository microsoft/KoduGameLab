// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;

using System.Xml;
using System.Xml.Serialization;

using Microsoft.Xna.Framework;


using Boku.Base;
using Boku.Common;

namespace Boku.Programming
{
    /// <summary>
    /// Actuator represents the base class for all actuator types
    /// it is the output consumer for the programming model and applies
    /// the ouput to the GameActor or other
    /// </summary>
    public abstract class Actuator : ProgrammingElement
    {
        [XmlAttribute]
        public string autoDefaultSelectorUpid;

        [XmlAttribute]
        public string gamepadDefaultSelectorUpid;

        [XmlAttribute]
        public PronounModifier.Pronouns defaultPronoun = PronounModifier.Pronouns.Me;
        

        /// <summary>
        /// The maximum frequency (per second) at which this actuator can fire.
        /// </summary>
        [XmlAttribute]
        public int maxTriggerRate;

        [XmlIgnore]
        public bool IsMovement { get { return Categories.Get((int)BrainCategories.DoMovement) || Categories.Get((int)BrainCategories.DoTurning); } }

        [XmlIgnore]
        public bool IsTurning { get { return Categories.Get((int)BrainCategories.DoNewTurning); } }


        private double lastTriggerTime;     // in total game seconds.
        private float timerDelaySeconds;    // 1.0/maxTriggerRate
        private int actuatorUpdateDepth;    // to avoid recursion


        /// Returns true if the actuator was acted on.
        protected abstract bool ActuatorUpdate(Reflex reflex);

        protected void CopyTo(Actuator clone)
        {
            base.CopyTo(clone);
            clone.autoDefaultSelectorUpid = this.autoDefaultSelectorUpid;
            clone.gamepadDefaultSelectorUpid = this.gamepadDefaultSelectorUpid;
            clone.maxTriggerRate = this.maxTriggerRate;
            clone.defaultPronoun = this.defaultPronoun;
        }

        /// <summary>
        /// This is called to have the Actuator reset any state it has
        /// primarily to have the paired arbitrator reset
        /// </summary>
        public override void Reset(Reflex reflex)
        {
            // do not reset actuatorUpdateDepth here, let it unwind naturally.
            // actuatorUpdateDepth = 0;

            if (maxTriggerRate > 0)
                timerDelaySeconds = 1.0f / maxTriggerRate;
            else
                timerDelaySeconds = 0;
            lastTriggerTime = 0;

            actionSet = null;

            base.Reset(reflex);
        }

        /// <summary>
        /// This is called on every update cycle to have this actuator apply its output to its target
        /// </summary>
        /// <param name="gameActor"></param>
        public void Update(Reflex reflex)
        {
            // Stun disables actuators
            if (reflex.Task.Brain.Stunned)
                return;

            // Avoid recursion into this function
            if (actuatorUpdateDepth > 0)
                return;

            actuatorUpdateDepth += 1;

            double currTime = Time.GameTimeTotalSeconds;

            if (currTime >= lastTriggerTime + timerDelaySeconds)
            {
                if (ActuatorUpdate(reflex))
                {
                    lastTriggerTime = currTime;
                }
                else
                {
                    // For some reason we weren't able to do the action.  So, 
                    // if this reflex has a once modifier, decrement it so 
                    // we'll try again.
                    // The only case where I know this currently happens is when
                    // a bot tries to display a fullscreen "say" verb when it's
                    // already displaying another.  This allows multiple fullscreen
                    // says to be displayed sequentially.
                    for (int i = 0; i < reflex.Modifiers.Count; i++)
                    {
                        if (reflex.Modifiers[i].upid == "modifier.once")
                        {
                            OnceModifier om = reflex.Modifiers[i] as OnceModifier;
                            om.Fired = false;
                        }
                    }
                }
            }

            actuatorUpdateDepth -= 1;
        }

        protected static List<BaseAction> scratchActionList = new List<BaseAction>();

        protected ActionSet actionSet;

        public void AttachActionSet(ActionSet actionSet)
        {
            this.actionSet = actionSet;
        }

        protected void ComposeActions(List<BaseAction> effList)
        {
            if (actionSet != null)
            {
                for (int i = 0; i < actionSet.Actions.Count; ++i)
                {
                    effList.Add(actionSet.Actions[i] as BaseAction);
                }
            }
        }
    }
}
