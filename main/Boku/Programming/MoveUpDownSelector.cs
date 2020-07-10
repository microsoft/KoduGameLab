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
using Boku.SimWorld;
using Boku.SimWorld.Chassis;

namespace Boku.Programming
{
    /// <summary>
    /// Moves a bot up or down in response to gamestick direction.
    /// </summary>
    public class MoveUpDownSelector : Selector
    {
        public override ProgrammingElement Clone()
        {
            MoveUpDownSelector clone = new MoveUpDownSelector();
            CopyTo(clone);
            return clone;
        }

        protected void CopyTo(MoveUpDownSelector clone)
        {
            base.CopyTo(clone);
        }

        public override void Used(bool newUse)
        {
        }

        public override ActionSet ComposeActionSet(Reflex reflex, GameActor gameActor)
        {
            ClearActionSet(actionSet);
            UpdateCanBlend(reflex);

            if (!reflex.targetSet.AnyAction)
                return actionSet;

            Vector2 valueStick = Vector2.Zero;

            if (reflex.targetSet.Param != null && reflex.targetSet.Param is Vector2)
            {
                // Read the gamepad input
                valueStick = (Vector2)reflex.targetSet.Param;
                valueStick.X = 0;
            }
            else
            {
                valueStick = new Vector2(0, 1.0f);
            }

            actionSet.AddAction(Action.AllocVerticalRateAction(reflex, valueStick.Y));

            return actionSet;
        }

        public override bool ActorCompatible(GameActor gameActor)
        {
            if (gameActor == null)
                return true;

            if (gameActor.Chassis is HoverChassis)
                return false;

            if (gameActor.Chassis is BoatChassis)
                return false;

            if (gameActor.Chassis is DynamicPropChassis)
                return false;

            if (gameActor.Chassis is StaticPropChassis)
                return false;

            if (gameActor.Chassis is CursorChassis)
                return false;

            if (gameActor.Chassis is SitAndSpinChassis)
                return false;

            if (gameActor.Chassis is CycleChassis)
                return false;

            if (gameActor.Chassis is RoverChassis)
                return false;

            return true;
        }
    }
}
