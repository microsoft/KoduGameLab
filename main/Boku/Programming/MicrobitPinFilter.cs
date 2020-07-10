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
    /// Hybrid filter that provides the source of Microbit pin input.
    /// 
    /// 
    /// </summary>
    public class MicrobitPinFilter : Filter, IMicrobitTile
    {
        public enum MicrobitPin
        {
            Pin1,   // P0
            Pin2,   // P1
            Pin3,   // P2
        }

        [XmlAttribute]
        public MicrobitPin pin;

        protected GamePadSensor.PlayerId playerId = GamePadSensor.PlayerId.Dynamic;

        public override ProgrammingElement Clone()
        {
            MicrobitPinFilter clone = new MicrobitPinFilter();
            CopyTo(clone);
            return clone;
        }

        protected void CopyTo(MicrobitPinFilter clone)
        {
            base.CopyTo(clone);
            clone.pin = this.pin;
        }

        public override bool MatchTarget(Reflex reflex, SensorTarget sensorTarget)
        {
            return false;
        }
        public override bool MatchAction(Reflex reflex, out object param)
        {
            float result = 0;

            // See if there's a filter defining which player we should be.  If not, use pad0.
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
                switch (pin)
                {
                    case MicrobitPin.Pin1:
                        result = bit.ReadPinValue(0, Microbit.EPinOperatingMode.Digital);
                        break;
                    case MicrobitPin.Pin2:
                        result = bit.ReadPinValue(1, Microbit.EPinOperatingMode.Digital);
                        break;
                    case MicrobitPin.Pin3:
                        result = bit.ReadPinValue(2, Microbit.EPinOperatingMode.Digital);
                        break;
                }
            }
#endif

            // Return as a parameter a vector that can be used for input to the movement system, so
            // that players can drive and turn bots using gamepad buttons.
            param = new Vector2(0, result / 255.0f);

            return result != 0;
        }

        public override void Reset(Reflex reflex)
        {
            base.Reset(reflex);
        }

    }

}   // end of namespace Boku.Programming
