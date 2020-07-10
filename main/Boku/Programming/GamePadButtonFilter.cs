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
    /// Hybrid filter that provides the source of gamepad button input
    /// 
    /// 
    /// </summary>
    public class GamePadButtonFilter : Filter
    {
        public enum GamePadButton
        {
            A,
            B,
            X,
            Y,
            LeftTrigger, // archived
            RightTrigger, // archived
        }

        [XmlAttribute]
        public GamePadButton button;

        [XmlIgnore]
        public GamePadInput.Button ButtonState; // Locally cached game pad button ref.

        protected GamePadSensor.PlayerId playerId = GamePadSensor.PlayerId.Dynamic;

        public override ProgrammingElement Clone()
        {
            GamePadButtonFilter clone = new GamePadButtonFilter();
            CopyTo(clone);
            return clone;
        }

        protected void CopyTo(GamePadButtonFilter clone)
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

            // Only jump through hoops the first time to get the pad.
            if (ButtonState == null)
            {
                GamePadInput pad = null;  

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

                // Get the correct game pad button.
                switch (button)
                {
                    case GamePadButton.A:
                        ButtonState = pad.ButtonA;
                        break;
                    case GamePadButton.B:
                        ButtonState = pad.ButtonB;
                        break;
                    case GamePadButton.X:
                        ButtonState = pad.ButtonX;
                        break;
                    case GamePadButton.Y:
                        ButtonState = pad.ButtonY;
                        break;
                    case GamePadButton.LeftTrigger:
                        ButtonState = pad.LeftTriggerButton;
                        break;
                    case GamePadButton.RightTrigger:
                        ButtonState = pad.RightTriggerButton;
                        break;
                }

            }

            result = ButtonState.IsPressed;

            // Return as a parameter a vector that can be used for input to the movement system, so
            // that players can drive and turn bots using gamepad buttons.
            param = new Vector2(0, 1);

            return result;
        }

        public override void Reset(Reflex reflex)
        {
            this.playerId = GamePadSensor.PlayerId.Dynamic;
            base.Reset(reflex);
        }

    }
}
