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
    /// Hybrid filter that provides the value of the Microbit tilt input from the accelerometer.
    /// 
    /// </summary>
    public class MicrobitTiltFilter : Filter, IMicrobitTile
    {
        [XmlIgnore]
        public Vector2 tiltPosition;    // 2d tilt value ignoring gravity...

        protected GamePadSensor.PlayerId playerId = GamePadSensor.PlayerId.Dynamic;

        protected List<StickCommand> stickCommands = new List<StickCommand>(1);
        protected bool wasChanged = false;

        public override ProgrammingElement Clone()
        {
            MicrobitTiltFilter clone = new MicrobitTiltFilter();
            CopyTo(clone);
            return clone;
        }

        protected void CopyTo(MicrobitTiltFilter clone)
        {
            base.CopyTo(clone);
        }

        public override void Reset(Reflex reflex)
        {
            wasChanged = false;
            base.Reset(reflex);
        }

        public override bool MatchTarget(Reflex reflex, SensorTarget sensorTarget)
        {
            return false;
        }

        Vector2 DeadZone(Vector2 v, float deadzone = 0.01f)
        {
            float mag = v.Length();
            if (mag == 0)
                return Vector2.Zero;
            Vector2 norm = v / mag;
            if (mag > deadzone)
            {
                mag -= deadzone;
                mag = mag / (1.0f - deadzone);
                return norm * mag;
            }
            else
            {
                return Vector2.Zero;
            }

        }

        public override bool MatchAction(Reflex reflex, out object param)
        {
            UpdateCommands();

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

            bool match = false;
            param = null;
            this.tiltPosition = Vector2.Zero;

#if !NETFX_CORE
            // TODO @*******: use the player# to get the right Microbit, or blended from all if no player#.
            Microbit bit = MicrobitExtras.GetMicrobitOrNull(playerId);
            if (bit != null)
            {
                tiltPosition = new Vector2(bit.State.Acc.Y, -bit.State.Acc.X);
                tiltPosition = DeadZone(tiltPosition * tiltPosition * new Vector2(Math.Sign(tiltPosition.X), Math.Sign(tiltPosition.Y)), 0.01f);
            }
#endif

            param = this.tiltPosition;
            match = (this.tiltPosition != Vector2.Zero); // only if not centered

            return match;

        }   // end of MatchAction()

        protected void UpdateCommands()
        {
            this.stickCommands.Clear();
        }   // end of UpdateCommands()

    }   // end of class MicrobitTiltFilter()

}   // end of namespace Boku.Programming
