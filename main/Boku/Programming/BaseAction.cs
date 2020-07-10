// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.


using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Xml.Serialization;

using Microsoft.Xna.Framework;

using Boku.Base;
using Boku.Common;

namespace Boku.Programming
{
    /// <summary>
    /// Actions are the results of individual reflexes being activated where
    /// their action affects movement.  Originally BaseAction->Effector was the base class for
    /// Attractors, Repulsors, and Directions.  Now we have a richer set of classes
    /// derived from BaseAction that support more complicated movement information
    /// travelling from the brain to the chassis.
    /// </summary>
    abstract public class BaseAction
    {
        [Flags]
        public enum SpecialInstruction
        {
            None,

            // TODO (****) Why is this really here?  The only place these are used is for the MoveLeftRightSelector
            // and in that case they are both always used.

            // Then building the bot heading, scale this vector to match the largest vector component in the set.
            // Please tell me why this might be needed?!?
            MatchVectorScale,

            // Don't let the length of this vector be scaled to zero when building the bot heading.
            // Please tell me why this might be needed?!?
            EnforceMinScale,
        }

        #region Members

        /// <summary>
        /// Game thing to affect.
        /// </summary>
        GameThing gameThing;

        /// <summary>
        /// Distance from bot, as measured from center of bot to edge of thing.
        /// 
        /// What thing?  I don't have a clue.
        /// </summary>
        float distance;

        /// <summary>
        /// This action was arbitrated and prioritized.
        /// </summary>
        bool used;

        /// <summary>
        /// This action was acted upon by an actuator.
        /// </summary>
        bool actedOn;

        /// <summary>
        /// The reflex that generated this action.  At a minimum this is used
        /// to get the modifiers from Quickly and Slowly tiles for movement actions.
        /// </summary>
        Reflex reflex;

        /// <summary>
        /// Action direction and strength (length of vector).
        /// 
        /// Actually, this is an arbitrary value whose interpretation depends of the type of the BaseAction.
        /// Need to work to remove this and have Action specific values instead.  Oh, and they should 
        /// have much better names than "value".
        /// </summary>
        protected Vector3 value;

        /// <summary>
        /// If true, will be blended with other actions when composing a heading
        /// </summary>
        bool canBlend;

        public SpecialInstruction specialInstruction;

        #endregion

        #region Accessors

        public bool ActedOn
        {
            get { return actedOn; }
            set
            {
                actedOn = value;
                if (Reflex != null)
                {
                    Reflex.actedOn |= value;
                }
            }
        }

        public Reflex Reflex
        {
            get { return reflex; }
            set { reflex = value; }
        }

        public Vector3 Value
        {
            get { return value; }
            set { this.value = value; }
        }

        public bool CanBlend
        {
            get { return canBlend; }
            set { canBlend = value; }
        }

        public bool Used
        {
            get { return used; }
            set { used = value; }
        }

        public GameThing GameThing
        {
            get { return gameThing; }
            set { gameThing = value; }
        }

        public float Distance
        {
            get { return distance; }
            set { distance = value; }
        }

        #endregion

        #region Public 

        public BaseAction()
        {
        }

        /// <summary>
        /// Free is needed for each typeof Action.  It should reset all the values
        /// in the Action and then add it to the appropriate free list.
        /// </summary>
        abstract public void Free();

        /// <summary>
        /// Applies this action to the given actor.  In particular will (may)
        /// set values in the actor's DesiredMovement class.
        /// </summary>
        /// <param name="desiredMovement"></param>
        abstract public void Apply(GameActor actor);

        public PlayerModifier GetPlayerModifierOrNull()
        {
            for (int i = 0; i < Reflex.Data.Modifiers.Count; i++)
            {
                if (Reflex.Data.Modifiers[i] is PlayerModifier)
                {
                    return ((PlayerModifier)Reflex.Data.Modifiers[i]);
                }
            }
            return null;
        }

        #endregion

        #region Internal
        #endregion

    }   // end of class BaseAction

}   // end of namespace Boku.Programming
