// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

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
    class ActionArbitrator : Arbitrator
    {
        private Dictionary<Reflex, ActionSet> itemSets = new Dictionary<Reflex, ActionSet>();
        private Reflex lastReflex = null;

        public override void Reset()
        {
            itemSets.Clear();
            lastReflex = null;
        }
        public override void AttachActionSet(Reflex reflex, ActionSet set)
        {
            // remove previous set
            itemSets.Remove(reflex);
            if (set != null)
            {
                // add new set
                itemSets.Add(reflex, set);
            }
        }

        public override Vector3 ComposeHeading()
        {
            return Vector3.Zero;
        }

        protected override void ComposePrioritizedEffectors(List<Effector> effList)
        {
            Effector actionEffector = null;
            Object actionObject = null;
            Reflex reflex = null;
#if PRIORITYDISTANCE
            // find the nearest attractor
            foreach (KeyValuePair<Reflex, ActionSet> pair in itemSets)
            {
                Action testTarget = pair.Value.Closest();

                if (testTarget != null && (actionTarget == null || actionTarget.distance > testTarget.distance))
                {
                    actionTarget = testTarget;
                    actionObject = pair.Key;
                }
            }
#else
            // find the highest priority action
            ActionSet prioritizedSet = null;
            foreach (KeyValuePair<Reflex, ActionSet> pair in itemSets)
            {
                ActionSet testSet = pair.Value;
                Effector testEffector = pair.Value.ClosestAction();

                if (testEffector != null)
                {
                    if (prioritizedSet == null || testSet.priority < prioritizedSet.priority)
                    {
                        prioritizedSet = testSet;
                        actionEffector = testEffector;
                        actionObject = pair.Key;
                    }
                }                
            }
#endif
            if (actionEffector != null)
            {
                actionEffector.used = true;
                // let the reflex know it was prioritized and used
                reflex = actionObject as Reflex;
                if (reflex != null)
                {
                    reflex.Used(reflex != lastReflex);
                }
                effList.Add(actionEffector);
                return;
            }

            lastReflex = reflex;
        }

        protected override void ComposeAllEffectors(List<Effector> effList)
        {
            foreach (KeyValuePair<Reflex, ActionSet> pair in itemSets)
            {
                for (int i = 0; i < pair.Value.Actions.Count; ++i)
                {
                    Effector eff = pair.Value.Actions[i] as Effector;
                    if (eff != null)
                        effList.Add(eff);
                }
            }
        }
    }
}
