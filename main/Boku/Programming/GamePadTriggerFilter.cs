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

using KoiX;
using KoiX.Input;

using Boku.Base;
using Boku.Common;
using Boku.Input;

namespace Boku.Programming
{
    /// <summary>
    /// Hybrid filter that provides the source of gamepad button input
    /// 
    /// 
    /// </summary>
    public class GamePadTriggerFilter : Filter
    {
        public enum GamePadTrigger
        {
            LeftTrigger,
            RightTrigger,
        }
        [XmlAttribute]
        public GamePadTrigger trigger;

        protected GamePadSensor.PlayerId playerId = GamePadSensor.PlayerId.Dynamic;

        [XmlIgnore]
        public float triggerValue;

        public override ProgrammingElement Clone()
        {
            GamePadTriggerFilter clone = new GamePadTriggerFilter();
            CopyTo(clone);
            return clone;
        }

        protected void CopyTo(GamePadTriggerFilter clone)
        {
            base.CopyTo(clone);
            clone.trigger = this.trigger;
        }

        public override bool MatchTarget(Reflex reflex, SensorTarget sensorTarget)
        {
            return false;
        }
        public override bool MatchAction(Reflex reflex, out object param)
        {
            GamePadSensor.PlayerId playerIdSensor = (GamePadSensor.PlayerId)reflex.targetSet.Param;

            if (this.playerId != playerIdSensor)
                this.playerId = playerIdSensor;

            GamePadInput pad = null;

            // Get the correct game pad.
            switch (playerId)
            {
                case GamePadSensor.PlayerId.All:
                    pad = GamePadInput.GetGamePad0();
                    break;
                case GamePadSensor.PlayerId.One:
                    pad = GamePadInput.GetGamePad1();
                    break;
                case GamePadSensor.PlayerId.Two:
                    pad = GamePadInput.GetGamePad2();
                    break;
                case GamePadSensor.PlayerId.Three:
                    pad = GamePadInput.GetGamePad3();
                    break;
                case GamePadSensor.PlayerId.Four:
                    pad = GamePadInput.GetGamePad4();
                    break;
            }

            if (trigger == GamePadTrigger.LeftTrigger)
            {
                triggerValue = pad.LeftTrigger;
            }
            else if (trigger == GamePadTrigger.RightTrigger)
            {
                triggerValue = pad.RightTrigger;
            }

            // Return as a parameter a vector that can be used for input to the movement system, so
            // that players can drive and turn bots using gamepad triggers.
            param = new Vector2(0, triggerValue);

            return triggerValue > 0; // only if pressed
        }

        public override void Reset(Reflex reflex)
        {
            this.playerId = GamePadSensor.PlayerId.Dynamic;
            triggerValue = 0;
            base.Reset(reflex);
        }
    }
}
