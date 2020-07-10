// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

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
    /// this selector will find the furthest Action Thing and 
    /// calculate a vector toward it and hand this to the actuator�s arbitrator.
    /// 
    /// It is known also as Toward
    /// </summary>
    public class TowardFurthestSelector : Selector
    {
        [XmlAttribute]
        public float strength;

        public TowardFurthestSelector()
        {
        }

        public override ProgrammingElement Clone()
        {
            TowardFurthestSelector clone = new TowardFurthestSelector();
            CopyTo(clone);
            return clone;
        }

        protected void CopyTo(TowardFurthestSelector clone)
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

            if (reflex.targetSet.Count > 0)
            {
                if (reflex.targetSet.CullToFurthest(gameActor, BlockedFrom))
                {
                    // the targetSet is in order by distance
                    SensorTarget target = reflex.targetSet.Furthest;

                    // calculate a vector toward target
                    Vector3 value = target.Direction;

                    bool apply = reflex.ModifyHeading(gameActor, Modifier.ReferenceFrames.World, ref value);

                    if (apply)
                    {
                        // radius should be from object
                        actionSet.AddAttractor(AllocAttractor(target.Range, value, target.GameThing, reflex), 0.4f);
                    }
                }
            }

            return actionSet;
        }

        public override void Used(bool newUse)
        {
        }

    }
}
