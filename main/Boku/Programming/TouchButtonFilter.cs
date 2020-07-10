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
using Boku.Input;

namespace Boku.Programming
{
    /// <summary>
    /// Hybrid filter that provides the source of on-screen touch button input
    /// 
    /// 
    /// </summary>
    public class TouchButtonFilter : Filter
    {
        [XmlAttribute]
        public TouchVirtualController.TouchButtonType button;

        public override ProgrammingElement Clone()
        {
            TouchButtonFilter clone = new TouchButtonFilter();
            CopyTo(clone);
            return clone;
        }

        protected void CopyTo(TouchButtonFilter clone)
        {
            base.CopyTo(clone);
            clone.button = this.button;
        }

        public override bool MatchTarget(Reflex reflex, SensorTarget sensorTarget)
        {
            // When checking a touchButton filter, the target no longer has a valid position, because
            // technically the button has no real place in the gameworld. We thus strip that data
            // from the target.
            sensorTarget.Position = sensorTarget.GameThing.Movement.Position;
            sensorTarget.Range = 0.0f;
            sensorTarget.Direction = Vector3.Zero;
            return true;
        }

        public override bool MatchAction(Reflex reflex, out object param)
        {
            bool result = false;

            if (TouchVirtualController.GetButtonState(button) == ButtonState.Pressed)
            {
                result = true;
            }

            param = null;
            return result;
        }
    }
}
