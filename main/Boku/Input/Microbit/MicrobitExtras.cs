// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;
using System.Text;
using System.IO;
using Microsoft.Xna.Framework;

#if !NETFX_CORE
using System.IO.Ports;
#endif

using Boku.Common;
using Boku.Common.Xml;
using Boku.Programming;

namespace Boku.Programming
{
    /// <summary>
    /// Microbit programming tiles implement this interface so that we can
    /// easily identify them in code and do things like conditionally exclude
    /// them from the tile picker based on game settings.
    /// </summary>
    public interface IMicrobitTile
    {
        // This space intentionally left blank.
    }
}

namespace Boku.Input
{
    public class MicrobitExtras
    {
        static string[] microbitActuatorUpids = {
            "actuator.microbit.block",
            "actuator.microbit.say",
            "actuator.microbit.sequential",
            "actuator.microbit.setpin1",
            "actuator.microbit.setpin2",
            "actuator.microbit.setpin3",
            "actuator.microbit.setpwmdutycycle.pin1",
            "actuator.microbit.setpwmdutycycle.pin2",
            "actuator.microbit.setpwmdutycycle.pin3",
            "actuator.microbit.setpwmfrequency.pin1",
            "actuator.microbit.setpwmfrequency.pin2",
            "actuator.microbit.setpwmfrequency.pin3",
            "actuator.microbit.show.old",
            "actuator.microbit.show",
        };

        public static bool IsMicrobitTile(ProgrammingElement progElement)
        {
            if (progElement == null)
                return false;
            if (progElement is IMicrobitTile)
                return true;
            foreach (string upid in microbitActuatorUpids)
            {
                if (progElement.upid == upid)
                    return true;
            }
            return false;
        }

#if !NETFX_CORE
        public static Microbit GetMicrobitOrNull(GamePadSensor.PlayerId playerId)
        {
            // TODO @*******: Decide how to handle the PlayerId.All case, for now use player one.
            if (playerId == GamePadSensor.PlayerId.All)
            {
                playerId = GamePadSensor.PlayerId.One;
            }

            Microbit microbit;
            MicrobitManager.Microbits.TryGetValue((int)playerId, out microbit);
            return microbit;
        }
#endif
    }
}
