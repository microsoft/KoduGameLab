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
    /// Hybrid filter that provides the source of the gamepad stick input
    /// 
    /// </summary>
    public class GamePadStickFilter : Filter
    {
        public enum GamePadStick
        {
            Left,
            Right,
        }
        [XmlAttribute]
        public GamePadStick stick;

        [XmlIgnore]
        public Vector2 stickPosition;

        protected List<StickCommand> stickCommands = new List<StickCommand>(1);
        protected bool wasChanged = false;
        protected GamePadSensor.PlayerId playerId = GamePadSensor.PlayerId.Dynamic;

        public override ProgrammingElement Clone()
        {
            GamePadStickFilter clone = new GamePadStickFilter();
            CopyTo(clone);
            return clone;
        }

        protected void CopyTo(GamePadStickFilter clone)
        {
            base.CopyTo(clone);
            clone.stick = this.stick;
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

        public override bool MatchAction(Reflex reflex, out object param)
        {

            GamePadSensor.PlayerId playerIdSensor = (GamePadSensor.PlayerId)reflex.targetSet.Param;
            if (this.playerId != playerIdSensor)
            {
                this.playerId = playerIdSensor;
                UpdateCommands();
            }

            bool isPitch = (reflex.Selector is MoveDownSelector)
                            || (reflex.Selector is MoveUpDownSelector)
                            || (reflex.Selector is MoveUpSelector);
            bool isYaw = !isPitch && reflex.Data.IsMovement();
                            
            bool match = false;
            param = null;
            if (this.stickCommands.Count != 0)
            {
                this.stickPosition = Vector2.Zero;
                for (int indexCommand = 0; indexCommand < this.stickCommands.Count; indexCommand++)
                {
                    StickCommand command = this.stickCommands[indexCommand] as StickCommand;
                    command.Update();
                    // Bias stick input toward center if pushing forward.
                    Vector2 stick = command.Position;

                    if (isPitch)
                    {
                        bool invert = GamePadInput.InvertYAxis(command.playerIndex);
                        if (invert)
                            stick.Y = -stick.Y;
                    }
                    if (isYaw)
                    {
                        bool invert = GamePadInput.InvertXAxis(command.playerIndex);
                        if (invert)
                            stick.X = -stick.X;
                    }

                    // 5% flat deadzone.
                    Vector2 s;
                    s.X = stick.X > 0 ? Math.Max(stick.X * 1.05f - 0.05f, 0) : Math.Min(stick.X * 1.05f + 0.05f, 0);
                    s.Y = stick.Y > 0 ? Math.Max(stick.Y * 1.05f - 0.05f, 0) : Math.Min(stick.Y * 1.05f + 0.05f, 0);
                    stickPosition += s;

                    // If a stick is being used for input in a bot program, don't blend it into gamepad0.
                    if (command is Input.GamePadRightThumbStick)
                    {
                        GamePadInput.GetGamePad(GamePadInput.LogicalToGamePad(command.playerIndex)).RightStickIgnoreForGamePad0();
                    }
                    else if (command is Input.GamePadLeftThumbStick)
                    {
                        GamePadInput.GetGamePad(GamePadInput.LogicalToGamePad(command.playerIndex)).LeftStickIgnoreForGamePad0();
                    }
                }

                param = this.stickPosition;
                match = (this.stickPosition != Vector2.Zero); // only if not centered
            }
            
            return match;
        }

        protected void UpdateCommands()
        {
            this.stickCommands.Clear();

            // Note that we're getting the pads via logical mapping.
            GamePadInput pad1 = GamePadInput.GetGamePad(GamePadInput.LogicalToGamePad(PlayerIndex.One));
            GamePadInput pad2 = GamePadInput.GetGamePad(GamePadInput.LogicalToGamePad(PlayerIndex.Two));
            GamePadInput pad3 = GamePadInput.GetGamePad(GamePadInput.LogicalToGamePad(PlayerIndex.Three));
            GamePadInput pad4 = GamePadInput.GetGamePad(GamePadInput.LogicalToGamePad(PlayerIndex.Four));

            switch (this.stick)
            {
                case GamePadStick.Left:
                    if (this.playerId == GamePadSensor.PlayerId.All)
                    {
                        //add pad 1 if it was touched, we're using virtual controller, or the other pads aren't present
                        if (pad1.EverTouched || InGame.ShowVirtualController || (!pad2.EverTouched && !pad3.EverTouched && !pad4.EverTouched))
                        {
                            this.stickCommands.Add(new GamePadLeftThumbStick(PlayerIndex.One));
                        }
                        if (pad2.EverTouched)
                        {
                            this.stickCommands.Add(new GamePadLeftThumbStick( PlayerIndex.Two ) );
                        }
                        if (pad3.EverTouched)
                        {
                            this.stickCommands.Add(new GamePadLeftThumbStick( PlayerIndex.Three ) );
                        }
                        if (pad4.EverTouched)
                        {
                            this.stickCommands.Add(new GamePadLeftThumbStick(PlayerIndex.Four));
                        }
                    }
                    else
                    {
                        this.stickCommands.Add(new GamePadLeftThumbStick((PlayerIndex)this.playerId));
                    }
                    break;

                case GamePadStick.Right:
                    if (this.playerId == GamePadSensor.PlayerId.All)
                    {
                        //add pad 1 if it was touched, we're using virtual controller, or the other pads aren't present
                        if (pad1.EverTouched || InGame.ShowVirtualController || (!pad2.EverTouched && !pad3.EverTouched && !pad4.EverTouched))
                        {
                            this.stickCommands.Add(new GamePadRightThumbStick(PlayerIndex.One));
                        }
                        if (pad2.EverTouched)
                        {
                            this.stickCommands.Add(new GamePadRightThumbStick(PlayerIndex.Two));
                        }
                        if (pad3.EverTouched)
                        {
                            this.stickCommands.Add(new GamePadRightThumbStick(PlayerIndex.Three));
                        }
                        if (pad4.EverTouched)
                        {
                            this.stickCommands.Add(new GamePadRightThumbStick(PlayerIndex.Four));
                        }
                    }
                    else
                    {
                        this.stickCommands.Add(new GamePadRightThumbStick((PlayerIndex)this.playerId));
                    }
                    break;
            }

        }
    }
}
