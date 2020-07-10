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
    /// Hybrid filter that provides the source of Microbit button input.
    /// 
    /// 
    /// </summary>
    public class MicrobitButtonFilter : Filter, IMicrobitTile
    {
        public enum MicrobitButton
        {
            Left,
            Right,
        }

        [XmlAttribute]
        public MicrobitButton button;

        protected GamePadSensor.PlayerId playerId = GamePadSensor.PlayerId.Dynamic;

        public override ProgrammingElement Clone()
        {
            MicrobitButtonFilter clone = new MicrobitButtonFilter();
            CopyTo(clone);
            return clone;
        }

        protected void CopyTo(MicrobitButtonFilter clone)
        {
            base.CopyTo(clone);
            clone.button = this.button;
        }

        public override bool MatchTarget(Reflex reflex, SensorTarget sensorTarget)
        {
            return false;
        }
        public override bool MatchAction(Reflex reflex, out object param)
        {
            bool result = false;

            // See if there's a filter defining which player we should be.  If not, use bit0.
            if (playerId == GamePadSensor.PlayerId.Dynamic)
            {
                playerId = GamePadSensor.PlayerId.All;

                ReflexData data = reflex.Data;
                for (int i = 0; i < data.Filters.Count; i++)
                {
                    if (data.Filters[i] is PlayerFilter)
                    {
                        playerId = ((PlayerFilter)data.Filters[i]).playerIndex;
                    }
                }
            }

#if !NETFX_CORE
            Microbit bit = MicrobitExtras.GetMicrobitOrNull(playerId);
            if (bit != null)
            {
                // Get the correct button.
                switch (button)
                {
                    case MicrobitButton.Left:
                        result = bit.State.ButtonA.IsPressed();
                        break;
                    case MicrobitButton.Right:
                        result = bit.State.ButtonB.IsPressed();
                        break;
                }
            }
#endif

            // Return as a parameter a vector that can be used for input to the movement system, so
            // that players can drive and turn bots using gamepad buttons.
            param = new Vector2(0, 1);

            return result;
        }

        public override void Reset(Reflex reflex)
        {
            base.Reset(reflex);
        }

    }

}   // end of namespace Boku.Programming
