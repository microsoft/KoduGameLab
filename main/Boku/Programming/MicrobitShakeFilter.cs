using System;
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
    public class MicrobitShakeFilter : Filter, IMicrobitTile
    {
        GamePadSensor.PlayerId playerId = GamePadSensor.PlayerId.Dynamic;

        int prevGeneration = 0;
        Vector3 prevAccel = Vector3.Zero;

        public override bool MatchTarget(Reflex reflex, SensorTarget sensorTarget)
        {
            return false;
        }

        public override bool MatchAction(Reflex reflex, out object param)
        {
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

            bool shaken = false;

#if !NETFX_CORE
            Microbit bit = MicrobitExtras.GetMicrobitOrNull(playerId);
            if (bit != null)
            {
                int currGeneration = bit.State.Generation;

                if (currGeneration != 0 && currGeneration != prevGeneration)
                {
                    Vector3 currAccel = bit.State.Acc;
                    Vector3 accelDiff = currAccel - prevAccel;
                    prevAccel = currAccel;
                    prevGeneration = currGeneration;
                    float strength = accelDiff.Length();

                    // Default range [1, 2]
                    float minStrength = 1;
                    float maxStrength = 2;  // 2G - This is the maximum length of micro:bit's accelerometer vector (configured in kodu-microbit.hex).

                    int stronglyCount = reflex.Data.GetFilterCount("filter.strongly");
                    // Range adjustment upward to [1.5, 2]
                    minStrength += stronglyCount * 0.166f;
                    int weaklyCount = reflex.Data.GetFilterCount("filter.weakly");
                    // Range adjustment downward to [0.075, 0.2]
                    minStrength -= weaklyCount * 0.308f;
                    maxStrength -= weaklyCount * 0.6f;

                    shaken = (strength >= minStrength && strength <= maxStrength);
                    //System.Diagnostics.Debug.WriteLine(String.Format("range [{0},{1}], value {2}, {3}", minStrength, maxStrength, strength, shaken ? "SHAKE!" : ""));
                }
            }
#endif

            param = shaken;

            return shaken;

        }

        public override ProgrammingElement Clone()
        {
            MicrobitShakeFilter clone = new MicrobitShakeFilter();
            CopyTo(clone);
            return clone;
        }
        protected void CopyTo(MicrobitButtonFilter clone)
        {
            base.CopyTo(clone);
        }
    }
}
