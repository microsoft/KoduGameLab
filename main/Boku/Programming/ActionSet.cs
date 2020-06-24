
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Xml.Serialization;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Storage;

using Boku.Base;

namespace Boku.Programming
{
    public class ActionSet
    {
        public int priority = -1;

        public List<BaseAction> actions = new List<BaseAction>();

        public List<BaseAction> Actions
        {
            get { return actions; }
        }

        /// <summary>
        /// TODO (scoy)
        /// Generic add method for actions.  Once all new actions go 
        /// through this, all the other Add* methods should be removed.
        /// </summary>
        /// <param name="action"></param>
        public void AddAction(BaseAction action)
        {
            Actions.Add(action);
        }   // end of AddAction()

        public void AddActionTarget(BaseAction eff, float unitRadius = 0)
        {
            // make it distance from edge not center
            eff.Distance -= unitRadius;
            Actions.Add(eff);
        }
        /// <summary>
        /// Add attractors in order from closest to furthest
        /// include distance from bot, 
        /// vector effect, 
        /// and radius that the effect starts to fade
        /// </summary>
        /// <param name="distance"></param>
        /// <param name="value"></param>
        /// <param name="unitRadius"></param>
        public void AddAttractor(Attractor eff, float unitRadius = 0)
        {
            Debug.Assert(!eff.Used);
            // make it distance from edge not center
            eff.Distance -= unitRadius;
            Actions.Add(eff);
        }

        public Attractor ClosestAttractor()
        {
            for (int i = 0; i < actions.Count; ++i)
            {
                if (actions[i] is Attractor)
                    return actions[i] as Attractor;
            }

            return null;
        }
    }   // end of class ActionSet


}   // end of namespace Boku.Programming
